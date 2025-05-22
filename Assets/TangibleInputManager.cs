	// TangibleInputManager.cs – Unity 2023  +  TuioUnityClient v2.x
using System.Collections.Generic;
using UnityEngine;

// ► composant Unity (MonoBehaviour) ◄
using Caillou;
using TuioNet.Common;

// ► classes "données" côté TuioNet ◄
using TuioNet.Tuio11;
using TuioUnity.Common; // Tuio11Object, Tuio11Dispatcher

public class TangibleInputManager : MonoBehaviour
{
    /* -------- Réglages Inspector -------- */
    [Header("Session TUIO 1.1 (drag & drop)")]
    [SerializeField] private TuioSessionBehaviour tuioSessionBehaviour;      // ← glisse l’objet “Tuio 1.1 Session”

    [Header("Prefabs")]
    [SerializeField] private GameObject attractorPrefab; // visuel attracteur
    [SerializeField] private GameObject repulsorPrefab;  // visuel répulseur

    [Header("Fiducial IDs")]
    public int attractorId = 100;
    public int repulsorId  = 101;

    [Header("Influence radius (m)")]
    public float minRange = 1f;
    public float maxRange = 8f;
    public float angleOffset = 0f;      // rad – décale 0 rad si nécessaire

    /* -------- internes -------- */
    private Tuio11Dispatcher dispatcher;                   // événements Add / Update / Remove
    private readonly Dictionary<long,(GameObject go, TokenData data)> tokens = new();
    private const int kMaxTokens = 10;
    private SwarmManager swarmMgr;                         // mis en cache

    /* ====================================================== */
    private void Awake()
    {
        /* 1. Vérifie que la session est assignée ----------- */
        if (tuioSessionBehaviour == null)
        {
            Debug.LogError("[TangibleInput] Champ « Session » vide ! "
              + "Ajoute GameObject ▸ TUIO ▸ Tuio 1.1 Session dans la scène, "
              + "puis glisse-le ici.", this);
            enabled = false;
            return;
        }

        /* 2. Récupère le dispatcher 1.1 -------------------- */
        dispatcher = tuioSessionBehaviour.TuioDispatcher as Tuio11Dispatcher;
        if (dispatcher == null)
        {
            Debug.LogError("[TangibleInput] Dispatcher 1.1 introuvable ; "
              + "assure-toi que la session est bien en mode TUIO 1.1.");
            enabled = false;
            return;
        }

        /* 3. Branche les événements ------------------------ */
        dispatcher.OnObjectAdd    += (_,o) => OnAdd(o);
        dispatcher.OnObjectUpdate += (_,o) => OnUpdate(o);
        dispatcher.OnObjectRemove += (_,o) => OnRemove(o.SessionId);

        /* 4. Cache le SwarmManager ------------------------- */
        swarmMgr = FindAnyObjectByType<SwarmManager>();
    }

    /* -------------------- ADD -------------------- */
    private void OnAdd(Tuio11Object o)
    {
        if (tokens.Count >= kMaxTokens) return;

        TokenPolarity pol;
        GameObject prefab;
        if      (o.SymbolId == attractorId) { pol = TokenPolarity.Attractor; prefab = attractorPrefab; }
        else if (o.SymbolId == repulsorId)  { pol = TokenPolarity.Repulsor;  prefab = repulsorPrefab;  }
        else return;                                 // ID inconnu → on ignore

        var go = Instantiate(prefab);
        if (go.TryGetComponent(out DraggableToken dt)) dt.enabled = false;

        var data = new TokenData(pol, Vector3.zero, 3f, 1f, 1f);
        TokenManager.Instance.AddToken(data);

        tokens[o.SessionId] = (go, data);
        UpdateToken(o, go, data);                    // position initiale
    }

    /* ------------------- UPDATE ------------------ */
    private void OnUpdate(Tuio11Object o)
    {
        if (tokens.TryGetValue(o.SessionId, out var t))
            UpdateToken(o, t.go, t.data);
    }

    private void UpdateToken(Tuio11Object o, GameObject go, TokenData data)
    {
        Vector2 uv = new((float)o.Position.X, -((float)o.Position.Y-1));
        go.transform.position = TableToWorld(uv);
        data.Position         = go.transform.position;

        float angle = Normalize(o.Angle + angleOffset);        // rad ∈ [0,2π[
        data.Range  = Mathf.Lerp(minRange, maxRange,
                                 angle / (2f*Mathf.PI));
    }

    /* ------------------- REMOVE ------------------ */
    private void OnRemove(long sid)
    {
        if (!tokens.TryGetValue(sid, out var t)) return;
        TokenManager.Instance.RemoveToken(t.data);
        Destroy(t.go);
        tokens.Remove(sid);
    }

    /* ------------------ Helpers ------------------ */
    private Vector3 TableToWorld(Vector2 uv)
    {
        if (swarmMgr == null) return new Vector3(uv.x, 0f, uv.y); // fallback
        var p = swarmMgr.GetSwarmData().GetParameters();
        return new Vector3(uv.x * p.GetMapSizeX(), 0f,
                           uv.y * p.GetMapSizeZ());
    }

    private static float Normalize(float a)
    {
        const float TAU = Mathf.PI * 2f;
        a %= TAU; return a < 0 ? a + TAU : a;
    }
}
