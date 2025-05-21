// DisplayerTokenInfluence.cs  – fix lecture clip
using System.Collections.Generic;
using UnityEngine;
using Caillou;                               // <-- pour TokenData

public class DisplayerTokenInfluence : Displayer
{
    [Header("Disque")]
    [Range(3,100)] public int edge = 24;
    public Material material;

    Mesh mesh;
    static readonly Color cold = new (0.0f, 0.4f, 1f);
    static readonly Color hot  = new (1f, 0.2f, 0.1f);

    void Awake()                             // Start → Awake (plus tôt)
    {
        mesh = new Mesh { name = "TokenInfluenceMesh" };
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    public override void ClearVisual() => mesh.Clear();

    public override void DisplayVisual(SwarmData swarmData)
    {
        ClearVisual();

        // 1️⃣  On essaye d’abord de prendre les tokens contenus dans la frame
        IReadOnlyList<TokenData> tokens = null;
        if (swarmData != null &&                         // clip ou temps réel
            swarmData.GetTokens() is { Count: >0 } frameTokens)
        {
            tokens = frameTokens;
        }
        else
        {
            // Sinon on se rabat sur les cailloux actifs de la scène
            tokens = TokenManager.Instance?.GetActiveTokens();
        }

        if (tokens == null || tokens.Count == 0) return;

        // 2️⃣  Construction du disque
        var vertices  = new List<Vector3>();
        var colors    = new List<Color>();
        var triangles = new List<int>();

        foreach (var tok in tokens)
        {
            Color c = Color.Lerp(cold, hot, tok.Strength01);

            int vStart = vertices.Count;
            vertices.Add(tok.Position + Vector3.up * 0.01f);   // centre
            colors  .Add(c);

            float step = 2 * Mathf.PI / edge;
            for (int i = 0; i <= edge; i++)
            {
                float a  = i * step;
                Vector3 p = new(
                    tok.Position.x + Mathf.Cos(a) * tok.Range,
                    tok.Position.y,
                    tok.Position.z + Mathf.Sin(a) * tok.Range);
                vertices.Add(p);
                colors.Add(c);
            }
            for (int i = 1; i <= edge; i++)
            {
                triangles.Add(vStart);
                triangles.Add(vStart + i);
                triangles.Add(vStart + i + 1);
            }
        }

        mesh.indexFormat = vertices.Count > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
    }
}
