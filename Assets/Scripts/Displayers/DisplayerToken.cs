using System.Collections.Generic;
using UnityEngine;
using Caillou;                 // accès TokenData & TokenPolarity

/// <summary>
/// Displayer visuel des cailloux (tokens) quand un clip est lu / simulé.
/// Instancie un petit prefab par caillou et l’actualise à chaque frame.
/// </summary>
public class TokenDisplayer : Displayer
{
    #region Inspector
    [Header("Prefabs visuels")]
    [Tooltip("Prefab pour un attracteur (blanc, +)")]
    [SerializeField] private GameObject attractorPrefab;

    [Tooltip("Prefab pour un répulseur (noir, –)")]
    [SerializeField] private GameObject repulsorPrefab;
    #endregion

    private readonly List<GameObject> pool = new();   // objet visuel par caillou

    #region Displayer overrides -----------------------------------------------------------------
    /// <summary>
    /// Affiche la frame : positionne, oriente et colore chaque caillou.
    /// </summary>
    public override void DisplayVisual(SwarmData frame)
    {
        IReadOnlyList<TokenData> tokens = frame.GetTokens();
        EnsurePoolSize(tokens.Count);

        // --- Affiche chaque caillou actif -----------------------------------------------------
        int i = 0;
        foreach (var tok in tokens)
        {
            GameObject go = pool[i++];
            go.SetActive(true);

            // Choix prefab : si le GameObject courant ne correspond pas, on le remplace.
            // => permet d’alterner attracteur / répulseur sans recréer la pool au complet.
            bool shouldBeAttractor = (tok.Polarity == TokenPolarity.Attractor);
            bool isAttractorGO     = go.name.Contains("Attractor");
            if (shouldBeAttractor != isAttractorGO)
            {
                // Remplacer par le bon prefab
                Vector3 oldPos = go.transform.position;
                Quaternion oldRot = go.transform.rotation;
                Transform parent = go.transform.parent;
                go.SetActive(false);
                pool[i-1] = Instantiate(shouldBeAttractor ? attractorPrefab : repulsorPrefab, parent);
                Destroy(go);
                go = pool[i-1];
                go.transform.position = oldPos;
                go.transform.rotation = oldRot;
            }

            go.transform.position   = tok.Position;
        }

        // --- Cache le surplus -------------------------------------------------------------
        for (; i < pool.Count; i++) pool[i].SetActive(false);
    }

    /// <summary>
    /// Cache tous les cailloux (appelé quand l’affichage est coupé ou avant nettoyage).
    /// </summary>
    public override void ClearVisual()
    {
        foreach (var go in pool) go.SetActive(false);
    }
    #endregion

    #region Pool helpers -----------------------------------------------------------------------
    void EnsurePoolSize(int needed)
    {
        while (pool.Count < needed)
        {
            // Par défaut on crée un attracteur ; si la frame attend un répulseur il sera remplacé
            GameObject go = Instantiate(attractorPrefab, transform);
            go.name = "AttractorToken";
            pool.Add(go);
        }
    }
    #endregion
}
