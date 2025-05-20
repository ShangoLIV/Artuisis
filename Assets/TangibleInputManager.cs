// TangibleInputManager.cs
// MultiTaction 557D  •  Unity 2023.1.x  •  TuioUnityClient v2.x
//
// Instancie deux types de cailloux : attracteur (prefab A) et répulseur (prefab B)
// selon l’ID du marqueur Codice reçu en TUIO 1.1.

using System.Collections.Generic;
using UnityEngine;
using TuioUnity;                         // package InteractiveScape – v2.x
using Caillou;
[DefaultExecutionOrder(-50)]             // s’exécute avant la logique du swarm
public class TangibleInputManager : MonoBehaviour
{
    /* ---------- Réglages dans l’Inspector ---------- */

    [Header("Prefabs par polarité")]
    public GameObject attractorPrefab;   // prefab “Caillou Attracteur”
    public GameObject repulsorPrefab;    // prefab “Caillou Répulseur”

    [Header("Plage du rayon d’influence (mètres)")]
    public float minRange = 1f;
    public float maxRange = 8f;

    [Header("Orientation de référence")]
    [Tooltip("Décalage rad si 0 rad du marqueur ≠ bord nord")]
    public float angleOffset = 0f;

    [Header("Association marqueur → polarité")]
    public int attractorId = 100;
    public int repulsorId  = 101;

    [Header("Limite simultanée")]
    public int maxTokens = 10;

    /* ---------- Champs privés ---------- */

    Tuio11Dispatcher dispatcher;
    readonly Dictionary<long,(GameObject go, TokenData data)> tokens = new();

    /* ===================================================================== */
    #region Unity lifecycle
    void Awake()
    {
        dispatcher = FindObjectOfType<Tuio11Dispatcher>();
        if (dispatcher == null)
        {
            Debug.LogError("[TangibleInputManager] Ajoute ‘GameObject ▸ TUIO ▸ Tuio 1.1 Session’ dans la scène !");
            enabled = false;
            return;
        }

        dispatcher.OnObjectAdd    += OnAdd;
        dispatcher.OnObjectUpdate += OnUpdate;
        dispatcher.OnObjectRemove += o => OnRemove(o.SessionID);
    }
    #endregion

    /* ===================================================================== */
    #region Handlers TUIO
    void OnAdd(Tuio11Object o)
    {
        if (tokens.Count >= maxTokens) return;  // limite souple

        // Choix polarité + prefab
        TokenPolarity pol;
        GameObject prefab;
        if      (o.ClassID == attractorId) { pol = TokenPolarity.Attractor; prefab = attractorPrefab; }
        else if (o.ClassID == repulsorId)  { pol = TokenPolarity.Repulsor;  prefab = repulsorPrefab;  }
        else                               return; // ID inconnu → ignoré

        // Instancie
        var go = Instantiate(prefab);
        go.GetComponent<DraggableToken>().enabled = false; // désactive le drag souris

        // Crée la data
        var data = new TokenData(pol, Vector3.zero, 3f, 1f, 1f);
        TokenManager.Instance.AddToken(data);

        // Enregistre
        tokens[o.SessionID] = (go, data);
        UpdateToken(o, go, data);              // position + range initiales
    }

    void OnUpdate(Tuio11Object o)
    {
        if (!tokens.TryGetValue(o.SessionID, out var t)) return;
        UpdateToken(o, t.go, t.data);
    }

    void OnRemove(long id)
    {
        if (!tokens.TryGetValue(id, out var t)) return;
        TokenManager.Instance.RemoveToken(t.data);
        Destroy(t.go);
        tokens.Remove(id);
    }
    #endregion

    /* ===================================================================== */
    #region Core
    void UpdateToken(Tuio11Object o, GameObject go, TokenData data)
    {
        Vector2 uv = o.Position;                           // 0–1
        go.transform.position = TableToWorld(uv);
        data.Position         = go.transform.position;

        float angle = Normalize(o.AngleRad + angleOffset); // rad [0,2π[
        data.Range  = Mathf.Lerp(minRange, maxRange, angle / (2f * Mathf.PI));
    }
    #endregion

    /* ===================================================================== */
    #region Helpers
    static float Normalize(float a)
    {
        const float TAU = Mathf.PI * 2f;
        a %= TAU;
        return a < 0f ? a + TAU : a;
    }

    Vector3 TableToWorld(Vector2 uv)
    {
        var p = SwarmManager.Instance.GetSwarmData().GetParameters();
        return new Vector3(uv.x * p.GetMapSizeX(), 0f, uv.y * p.GetMapSizeZ());
    }
    #endregion
}
