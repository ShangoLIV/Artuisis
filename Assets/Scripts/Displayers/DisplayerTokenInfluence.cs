using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dessine, pour chaque puck actif, un disque plein représentant
/// son rayon d’influence et colorié selon sa force :
///     • bleu (strength = 0)
///     • rouge (strength = 1)
/// </summary>
public class DisplayerTokenInfluence : Displayer
{
    [Header("Disque")]
    [Range(3,100)] public int edge = 24;          // segments du disque
    public Material material;

    Mesh mesh;
    static readonly Color cold = new (0.0f, 0.4f, 1f);   // bleu
    static readonly Color hot  = new (1f, 0.2f, 0.1f);   // rouge

    void Start()
    {
        mesh = new Mesh();
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

        var tokens = Caillou.TokenManager.Instance?.GetActiveTokens();
        if (tokens == null || tokens.Count == 0) return;

        List<Vector3> vertices  = new();
        List<Color>   colors    = new();
        List<int>     triangles = new();

        foreach (var tok in tokens)
        {
            // 1.  ~couleur~  (interpolation bleu→rouge)
            Color c = Color.Lerp(cold, hot, tok.Strength01);

            // 2.  génère disque plein centré sur tok.Position
            int vStart = vertices.Count;
            vertices.Add(tok.Position + Vector3.up * 0.01f);        // centre
            colors  .Add(c);

            float step = 2 * Mathf.PI / edge;
            for (int i = 0; i <= edge; i++)
            {
                float angle = i * step;
                Vector3 pt = new(
                    tok.Position.x + Mathf.Cos(angle) * tok.Range,
                    tok.Position.y,
                    tok.Position.z + Mathf.Sin(angle) * tok.Range);
                vertices.Add(pt);
                colors.Add(c);
            }

            // triangles  (fan)
            for (int i = 1; i <= edge; i++)
            {
                triangles.Add(vStart);             // centre
                triangles.Add(vStart + i);         // i
                triangles.Add(vStart + i + 1);     // i+1
            }
        }

        mesh.Clear();
        mesh.indexFormat        = vertices.Count > 65000 ?
                                  UnityEngine.Rendering.IndexFormat.UInt32 :
                                  UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
    }
}
