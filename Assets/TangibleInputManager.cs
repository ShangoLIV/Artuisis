// TangibleInputManager.cs – Unity 2023  +  TuioUnityClient v2.x
// Attracteur / Répulseur + “règle-force” (2 symboles) pour Strength01

using System;
using System.Collections.Generic;
using UnityEngine;
using Caillou;                       // TokenData, TokenManager, etc.
using TuioNet.Tuio11;               // Tuio11Object
using TuioUnity.Common;             // Tuio11Dispatcher
using TuioNet.Common;               // TuioSessionBehaviour

[DefaultExecutionOrder(-40)]        // avant le swarm (-50) mais après trackers
public class TangibleInputManager : MonoBehaviour
{
    /* ──────────────────────────── Réglages Inspector ───────────────────────── */

    [Header("Session TUIO 1.1 (drag & drop)")]
    [SerializeField] TuioSessionBehaviour tuioSessionBehaviour;   // objet “Tuio 1.1 Session”

    [Header("Prefabs cailloux")]
    [SerializeField] GameObject attractorPrefab;
    [SerializeField] GameObject repulsorPrefab;

    [Header("IDs fiduciaires — cailloux")]
    public int attractorId = 100;
    public int repulsorId  = 101;

    [Header("IDs fiduciaires — règle-force (deux symboles)")]
    public int strengthAId = 102;     // premier symbole
    public int strengthBId = 103;     // second  symbole

    [Header("ID fiduciaire - vitesse simu")]
    public int vitesseId = 1064;
    
    [Header("Rayon d’influence (m)")]
    public float minRange = 1f;
    public float maxRange = 8f;

    [Header("Force d’influence")]
    public float minStrength = 0f;
    public float maxStrength = 1f;
    [Tooltip("Distance cible entre les 2 symboles (m)")]
    public float pairDistance = 0.15f;         // 15 cm
    [Tooltip("Tolérance (± m)")]
    public float pairTolerance = 0.04f;        // ± 4 cm
    [Tooltip("Un demi-tour (π rad) = force min ↔ max")]
    public bool clockwiseIncreases = true;
    [Tooltip("Tolérance pour sélectionner un caillou (m)")]
    public float selectionThreshold = 0.02f;   // 2 cm
    [Header("Plage vitesse (m/s)")]
    public float minSpeed = 0.01f;
    public float maxSpeed = 1f;
    [Header("Valeur par défaut du rayon")]
    public float defaultRange = 3f;

    /* ────────────────────────────────── internes ───────────────────────────── */
    Tuio11Dispatcher dispatcher;
    readonly Dictionary<long,(GameObject go, TokenData data)> tokens = new();
    const int kMaxTokens = 20;
    SwarmManager swarmMgr;
    private EditorParametersInterface parametersInterface;
    class RingState
    {
        public long idA, idB;
        public float angleOffset;
        public float startStrength;
        public TokenData target;
        public bool isInitialised;
    }
    private readonly Dictionary<long, float> tokenAngleOffset = new();
    private readonly Dictionary<(long, long), RingState> rings = new();
    // offset et valeur de départ mémorisés quand l’anneau se pose sur un token
    public  float radiusMargin = 0.2f;
    
    private float ringStrengthStart;

    private float speedAngleOffset;
    private float InitialSpeed;
    /*  règle-force */
    readonly Dictionary<long,Tuio11Object> ringMarkers = new();   // stocke A et B
    TokenData selectedToken;
    float lastAngle;      // rad

    /* ======================================================================== */
    void Awake()
    {
        /* Vérif session ----------------------------------------------------- */
        if (tuioSessionBehaviour == null)
        {
            Debug.LogError("[TangibleInput] Glisse l’objet « Tuio 1.1 Session » "
                           + "dans le champ Session.", this);
            enabled = false; return;
        }
        dispatcher = tuioSessionBehaviour.TuioDispatcher as Tuio11Dispatcher;
        if (dispatcher == null) { enabled = false; return; }

        /* Abonnements ------------------------------------------------------- */
        dispatcher.OnObjectAdd    += (_,o) => HandleTuio(o, true);
        dispatcher.OnObjectUpdate += (_,o) => HandleTuio(o, false);
        dispatcher.OnObjectRemove += (_,o) => OnRemove(o.SessionId);

        swarmMgr = FindAnyObjectByType<SwarmManager>();
        parametersInterface = FindAnyObjectByType<EditorParametersInterface>();
    }

    /* ======================================================================== */
    #region  gestion TUIO  ------------------------------------------------------------------
    void HandleTuio(Tuio11Object o, bool isAdd)
    {
        /*Debug.Log($"TUIO {o.SymbolId}  {(isAdd ? "ADD" : "UPDATE")}  "
                  + $"pos=({o.Position.X:F3},{o.Position.Y:F3}) angle={o.Angle:F2} rad",
            this);*/
        /* 1. Les deux symboles de la règle-force --------------------------- */
        if (o.SymbolId == strengthAId || o.SymbolId == strengthBId)
        {
            ringMarkers[o.SessionId] = o;  
            ProcessRings();           // traite la rotation
            return;                                   // on ne crée pas de caillou
        }

        if (o.SymbolId == vitesseId)
        {
            if (isAdd) OnAddSpeed(o);
            else UpdateSpeed(o);
            return;
        }

        /* 2. Création des cailloux ----------------------------------------- */
        if (isAdd) OnAddCaillou(o);
        else       OnUpdateCaillou(o);
    }
    
    /*––––––––– Pairage A+B puis appel Add / Update –––––––––*/
    void ProcessRings()
    {
        foreach (var m in ringMarkers.Values)
        {
            
            // cherche le mate de l'autre ID
            int mateId = m.SymbolId == strengthAId ? strengthBId : strengthAId;
            var mate = FindMate(m, mateId);
            if (mate == null) continue;

            var key = MakeKey(m.SessionId, mate.SessionId);

            // distance A-B
            float d = Vector3.Distance(ToWorld(m), ToWorld(mate));
            //Debug.Log($"pair {m.SymbolId}/{mate.SymbolId}  dist={d:F2}  "
            //          + $"valid={(Mathf.Abs(d - pairDistance) <= pairTolerance)}");

            if (Mathf.Abs(d - pairDistance) > pairTolerance) { rings.Remove(key); continue; }

            if (!rings.ContainsKey(key))
                OnStrengthAdd(m, mate);              // création
            else
                OnStrengthUpdate(m, mate, rings[key]); // variation continue
            
        }
        
    }

/* Mate = premier marqueur qui correspond à l’ID demandé */
    Tuio11Object FindMate(Tuio11Object src, int mateId)
    {
        foreach (var m in ringMarkers.Values)
            if (m.SymbolId == mateId && m.SessionId != src.SessionId) return m;
        return null;
    }


    /* ---------------------- cailloux -------------------------------------- */
    void OnAddCaillou(Tuio11Object o)
    {
        if (tokens.Count >= kMaxTokens) return;

        TokenPolarity pol;
        GameObject prefab;
        if      (o.SymbolId == attractorId) { pol = TokenPolarity.Attractor; prefab = attractorPrefab; }
        else if (o.SymbolId == repulsorId)  { pol = TokenPolarity.Repulsor;  prefab = repulsorPrefab;  }
        else return;

        var go = Instantiate(prefab);
        if (go.TryGetComponent(out DraggableToken dt)) dt.enabled = false;

        var data = new TokenData(pol, Vector3.zero, 3f, 1f, 1f);
        TokenManager.Instance.AddToken(data);
        tokens[o.SessionId] = (go, data);
        tokenAngleOffset[o.SessionId] = o.Angle;  // on se souviendra de cet angle
        data.Range = defaultRange;                // valeur fixe de départ
        UpdateRange(o, go, data);                 // position & range corrects

    }

    void OnUpdateCaillou(Tuio11Object o)
    {
        if (!tokens.TryGetValue(o.SessionId, out var t)) return;
        UpdateRange(o, t.go, t.data);
    }

    void UpdateRange(Tuio11Object o, GameObject go, TokenData data)
    {
        // position

        Vector2 uv = new(o.Position.X, 1f - o.Position.Y); // Y inversé
        go.transform.position = TableToWorld(uv);
        data.Position = go.transform.position;
         
        float delta = ShortestDeltaRad(tokenAngleOffset[o.SessionId], o.Angle); // rad ∈ [-π;π]

        float t = Mathf.Clamp01(Mathf.Abs(delta) / Mathf.PI);
        data.Range = Mathf.Lerp(minRange, maxRange, t);
    }

    void OnAddSpeed(Tuio11Object o)
    {
        speedAngleOffset = (float)o.Angle;                          // rad
        InitialSpeed     = swarmMgr.GetSwarmData()
            .GetParameters()
            .GetMaxSpeed();               // vitesse courante
    }

    void UpdateSpeed(Tuio11Object o)
    {
        float delta = ShortestDeltaRad(speedAngleOffset, (float)o.Angle); // -π..π
        float t     = Mathf.Clamp(delta / Mathf.PI, -1f, 1f);            // -1..+1

        // sens horaire ↑ / anti-horaire ↓
        float newSpeed = Mathf.Clamp(InitialSpeed + t * (maxSpeed - minSpeed),
            minSpeed, maxSpeed);

        parametersInterface.SetMaxSpeed(newSpeed);

        // debug visuel : petit disque rose autour du marqueur
        Vector2 uv = new((float)o.Position.X, 1f - (float)o.Position.Y);
        DebugExtension.DrawCircle(TableToWorld(uv) + Vector3.up*0.01f,
            Vector3.up, Color.magenta, 0.05f, 32, 0f);
        Debug.Log("speed" + swarmMgr.GetSwarmData().GetParameters().GetMaxSpeed());
    }
    /* ---------------------- remove ---------------------------------------- */
    void OnRemove(long sid)
    {
        // si c'était un marqueur règle-force
        ringMarkers.Remove(sid);
        if (tokens.TryGetValue(sid, out var t))
        {
            TokenManager.Instance.RemoveToken(t.data);
            Destroy(t.go);
            tokens.Remove(sid);
        }
        if (selectedToken != null && selectedToken == t.data) selectedToken = null;
    }
    #endregion
    /* ======================================================================== */
    #region  logique règle-force  ----------------------------------------------
    
    void OnStrengthAdd(Tuio11Object a, Tuio11Object b)
    {
        var key = MakeKey(a.SessionId, b.SessionId);
        if (rings.ContainsKey(key)) return;               // déjà créé

        Vector3 pA = ToWorld(a);
        Vector3 pB = ToWorld(b);
        float radius  = Vector3.Distance(pA, pB) * .5f + radiusMargin;
        Vector3 centre = (pA + pB) * .5f;

        var token = FindTokenInDisk(centre, radius);
        if (token == null) return;

        float angle0 = Mathf.Atan2((pB - pA).z, (pB - pA).x);

        rings[key] = new RingState {
            idA=a.SessionId, idB=b.SessionId,
            angleOffset   = angle0,
            startStrength = token.Strength01,
            target        = token,
            isInitialised = true          // ← offset mémorisé
        };

        //Debug.Log($"[Ring] init on token {token}  angle0={angle0:F2}  S0={token.Strength01:F2}");
    }

    void OnStrengthUpdate(Tuio11Object a, Tuio11Object b, RingState r)
{
    bool locked = false;
    // angle courant du segment
    float angle = Mathf.Atan2((ToWorld(b) - ToWorld(a)).z,
                              (ToWorld(b) - ToWorld(a)).x);
    angle -= r.startStrength * 2 * Mathf.PI;
    
    // Δθ signé le plus court  (–0 .. +2π)
    float delta = ShortestDeltaRad(r.angleOffset, angle) + Mathf.PI;
    float t = delta / Mathf.PI;
    Debug.Log(t);
    if (t > 1.7)
    {
        r.target.Strength01 = 1f;
    }
    else if (t > 1.3f)
    {
        r.target.Strength01 = 0.5f;
    }
    else if (t > 1f)
    {
        r.target.Strength01 = 0f;
    }
    else
    {
        r.target.Strength01 = Mathf.Lerp(0f, 1f,1-t);    
    }
    Debug.Log(r.target.Strength01);
    /* ----- debug visuel ----- */
    Vector3 centre = (ToWorld(a) + ToWorld(b)) * .5f;
    float   radius = Vector3.Distance(ToWorld(a), ToWorld(b))*0.5f + radiusMargin;
    DebugExtension.DrawCircle(centre + Vector3.up*0.02f, Vector3.up, Color.cyan, radius, 64, 0f);
}


    TokenData FindTokenInDisk(Vector3 centre, float radius)
    {
        float rSq = radius * radius;
        foreach (var t in tokens.Values)
        {
            Vector3 c = t.data.Position;             // déjà dans le monde
            float dSq = (c.x - centre.x) * (c.x - centre.x)
                        + (c.z - centre.z) * (c.z - centre.z);
            if (dSq <= rSq) return t.data;
        }
        return null;
    }


    #endregion
    /* ======================================================================== */
    #region helper  ------------------------------------------------------------------
    
    Vector3 TableToWorld(Vector2 uv)
    {
        if (swarmMgr == null) return new Vector3(uv.x, 0f, uv.y);
        var p = swarmMgr.GetSwarmData().GetParameters();
        return new Vector3(uv.x * p.GetMapSizeX(), 0f, uv.y * p.GetMapSizeZ());
    }
    
    Vector3 ToWorld(Tuio11Object o)       // UV (0-1) → mètres monde
    {
        Vector2 uv = new(o.Position.X, 1f - o.Position.Y);
        var p = swarmMgr.GetSwarmData().GetParameters();
        return new Vector3(uv.x * p.GetMapSizeX(), 0f, uv.y * p.GetMapSizeZ());
    }
    
    (long,long) MakeKey(long a, long b)   => a < b ? (a,b) : (b,a);
    static float ShortestDeltaRad(float fromRad, float toRad)
    {
        // Mathf.DeltaAngle travaille en degrés
        float deltaDeg = Mathf.DeltaAngle(fromRad * Mathf.Rad2Deg, toRad * Mathf.Rad2Deg);
        return deltaDeg * Mathf.Deg2Rad;            // rad signé ∈ [-π, +π]
    }
    #endregion
    /* ======================================================================== */
}

