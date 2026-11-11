using System.Collections.Generic;
using Quintessence.Core;

namespace Quintessence.Combat
{
    /// Elementos despertos, elemento atual, recarga da troca e o Golpe de Troca.
    public class LoadoutState
    {
        public const float SwitchCooldown = 0.4f;
        public const float StrikeWindow = 0.3f;
        public const float StrikeBonus = 1.5f;

        readonly List<ElementType> awakened = new();
        int current;
        float switchCd, window;

        public LoadoutState(ElementType starting) { awakened.Add(starting); }

        public ElementType Current => awakened[current];
        public IReadOnlyList<ElementType> Awakened => awakened;
        public bool HasAll => awakened.Count >= 4;
        /// O proximo golpe sai com bonus.
        public bool StrikeReady { get; private set; }

        public bool Awaken(ElementType e)
        {
            if (e == ElementType.None || awakened.Contains(e)) return false;
            awakened.Add(e);
            return true;
        }

        /// dir: -1 anterior, +1 proximo.
        public bool TrySwitch(int dir)
        {
            if (dir == 0 || awakened.Count < 2 || switchCd > 0f) return false;
            current = ((current + dir) % awakened.Count + awakened.Count) % awakened.Count;
            switchCd = SwitchCooldown;
            if (window > 0f) StrikeReady = true;
            window = 0f;
            return true;
        }

        /// Chamar quando um golpe acerta: abre a janela do Golpe de Troca.
        public void NotifyHitLanded() => window = StrikeWindow;

        public bool ConsumeStrike()
        {
            bool r = StrikeReady;
            StrikeReady = false;
            return r;
        }

        public void Tick(float dt)
        {
            if (switchCd > 0f) switchCd -= dt;
            if (window > 0f) window -= dt;
        }
    }
}
