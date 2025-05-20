using System.Collections.Generic;
using UnityEngine;

namespace Caillou
{
    /// <summary>
    /// Stocke la liste des cailloux actifs dans la scène
    /// et fournit un accès en lecture rapide.
    /// </summary>
    public class TokenManager : MonoBehaviour
    {
        // ---------- Singleton ----------
        public static TokenManager Instance { get; private set; }

        private readonly List<TokenData> activeTokens = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Si tu veux le conserver entre scènes :
            // DontDestroyOnLoad(gameObject);
        }

        // ---------- API publique ----------
        public IReadOnlyList<TokenData> GetActiveTokens() => activeTokens;

        public void AddToken(TokenData t)
        {
            if (!activeTokens.Contains(t)) activeTokens.Add(t);
        }

        public void RemoveToken(TokenData t)
        {
            activeTokens.Remove(t);
        }
    }
}