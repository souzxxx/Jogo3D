using Quintessence.Run;
using UnityEngine;

namespace Quintessence.Game
{
    /// Guarda o progresso permanente no PlayerPrefs (no WebGL vai para o armazenamento do navegador).
    public static class MetaStore
    {
        public const string DefaultKey = "quintessence.meta";
        static string key = DefaultKey;
        static MetaProgress current;

        public static MetaProgress Current => current ??= MetaProgress.Parse(PlayerPrefs.GetString(key, ""));

        public static void Save()
        {
            PlayerPrefs.SetString(key, Current.Serialize());
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            current = new MetaProgress();
            Save();
        }

        /// Troca o save usado (os testes usam um proprio, para nao mexer no do jogador).
        public static void UseKey(string newKey)
        {
            key = newKey;
            current = null;
        }
    }
}
