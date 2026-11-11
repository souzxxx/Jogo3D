using System;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    public enum MarkOutcome { None, Applied, Refreshed, Reaction }

    /// Regra da marca elemental: o primeiro elemento marca, o mesmo renova, outro consome e reage.
    public class MarkState
    {
        public const float Duration = 4f;
        public const float ReactionLock = 0.5f;

        public ElementType Element { get; private set; }
        float timeLeft, lockLeft;
        public bool HasMark => Element != ElementType.None;

        public MarkOutcome Hit(ElementType e, out ElementType consumed)
        {
            consumed = ElementType.None;
            if (e == ElementType.None) return MarkOutcome.None;
            if (!HasMark) { Element = e; timeLeft = Duration; return MarkOutcome.Applied; }
            if (Element == e) { timeLeft = Duration; return MarkOutcome.Refreshed; }
            if (lockLeft > 0f) return MarkOutcome.None;
            consumed = Element;
            Element = ElementType.None;
            timeLeft = 0f;
            lockLeft = ReactionLock;
            return MarkOutcome.Reaction;
        }

        /// Devolve true quando a marca expira neste tick.
        public bool Tick(float dt)
        {
            lockLeft = Math.Max(0f, lockLeft - dt);
            if (!HasMark) return false;
            timeLeft -= dt;
            if (timeLeft > 0f) return false;
            Element = ElementType.None;
            return true;
        }

        public void Clear() { Element = ElementType.None; timeLeft = 0f; }

        /// Para reacoes forcadas (Quintessencia): respeita a mesma trava de 0,5 s por alvo.
        public bool TryLock()
        {
            if (lockLeft > 0f) return false;
            lockLeft = ReactionLock;
            return true;
        }
    }
}
