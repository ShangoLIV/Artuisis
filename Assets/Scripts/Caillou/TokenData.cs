using UnityEngine;

namespace Caillou
{
    /// <summary>Polarité du caillou : attire (+) ou repousse (–) les agents.</summary>
    public enum TokenPolarity { Attractor, Repulsor }

    /// <summary>Données sérialisables d’un caillou tangible.</summary>
    [System.Serializable]
    public class TokenData
    {
        public TokenPolarity Polarity;               // Blanc ↔ Noir

        // --- MOD v2 : Vector3 → SerializableVector3 pour la sérialisation ---
        [SerializeField] private SerializableVector3 position;

        /// <summary>Position monde exposée en Vector3 classique.</summary>
        public Vector3 Position
        {
            get => position;           // cast implicite vers Vector3
            set => position = value;   // cast implicite vers SerializableVector3
        }

        public float Range;                               // Rayon d’influence (m)
        [Range(0f, 1f)] public float Strength01;           // Intensité LED (0-1)
        public float HitRadius;                    // Rayon d’impact agents

        public float GetSignedStrength() =>
            (Polarity == TokenPolarity.Attractor ? +1f : -1f) * Strength01;

        public TokenData(TokenPolarity polarity, Vector3 pos,
            float range, float strength01, float hitRadius)
        {
            Polarity   = polarity;
            Position   = pos;                 // passe par le setter
            Range      = range;
            Strength01 = Mathf.Clamp01(strength01);
            HitRadius  = hitRadius;
        }
    }
}