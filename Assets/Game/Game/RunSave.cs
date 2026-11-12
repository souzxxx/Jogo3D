using System;
using Quintessence.Core;
using Quintessence.Run;
using UnityEngine;

namespace Quintessence.Game
{
    [Serializable]
    public class SavedBoon
    {
        public string id, name, description;
        public int patron, stat;
        public float value;
    }

    /// Foto de uma run em andamento, tirada ao entrar em cada sala. Continuar volta para o comeco daquela sala.
    [Serializable]
    public class RunSave
    {
        public const int Version = 1;
        public int version = Version;
        public int hero, seed, layers, shrines, ascension;
        public int current, depth;
        public int[] visited = new int[0];
        public int essence, roomsCleared, kills, rerolls, pacts;
        public int[] ranks = new int[0];
        public int level = 1, xp, pending, totalXp;
        public string[] talents = new string[0];
        public SavedBoon[] boons = new SavedBoon[0];
        public int[] awakened = new int[0];
        public float health, maxHealth, baseMaxHealth, curse = 1f, elapsed;
        public string modifiers = "";

        public string Summary => $"{HeroPassives.HeroName((ElementType)hero)} · sala {depth + 1}/{layers}" + (ascension > 0 ? $" · Ascensão {ascension}" : "");
    }

    /// Guarda a run em andamento no PlayerPrefs (no WebGL, no navegador). Save quebrado vira "sem save", nunca erro.
    public static class RunSaveStore
    {
        public const string DefaultKey = "quintessence.run";
        static string key = DefaultKey;

        /// Troca a chave (os testes usam uma propria).
        public static void UseKey(string newKey) => key = newKey;

        public static RunSave Load()
        {
            var text = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var s = JsonUtility.FromJson<RunSave>(text);
                if (s == null || s.version != RunSave.Version || s.layers < 3 || s.hero < 1 || s.hero > 4 || s.maxHealth <= 0f) return null;
                return s;
            }
            catch (Exception) { return null; }
        }

        public static void Save(RunSave save)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
