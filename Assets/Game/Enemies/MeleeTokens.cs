using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quintessence.Enemies
{
    /// Vez de atacar corpo a corpo: no maximo Max inimigos avisando ou batendo no heroi ao mesmo tempo; os outros esperam.
    /// Em grupo (Brasas, Rastejantes, Cinzentos) varios erguiam a arma no mesmo instante e o heroi levava 3-4 golpes juntos.
    /// A vez volta no golpe, no cancelamento (atordoado no aviso), na morte e quando o inimigo some (OnDisable/troca de cena).
    public static class MeleeTokens
    {
        public const int Max = 3;
        static readonly HashSet<Object> holders = new();

        /// Quantas vezes um inimigo teve de esperar a vez (para os testes e a sonda).
        public static int Waits { get; private set; }
        public static int Held { get { Prune(); return holders.Count; } }

        static void Prune() => holders.RemoveWhere(h => h == null);

        /// Pega a vez (ou ja tem). Falso: ha Max atacando; o inimigo continua andando e tenta de novo depois.
        public static bool TryTake(Object who)
        {
            if (who == null) return false;
            Prune();
            if (holders.Contains(who)) return true;
            if (holders.Count >= Max) { Waits++; return false; }
            holders.Add(who);
            return true;
        }

        public static bool Has(Object who) => who != null && holders.Contains(who);

        public static void Release(Object who) { if (who != null) holders.Remove(who); }

        public static void Clear() { holders.Clear(); Waits = 0; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Boot()
        {
            Clear();
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
        }

        static void OnScene(Scene s, LoadSceneMode m) => holders.Clear();
    }
}
