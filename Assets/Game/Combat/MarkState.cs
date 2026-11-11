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
    }

    /// Marca elemental de um alvo. Adicionada sozinha no primeiro golpe elemental.
    [RequireComponent(typeof(Health))]
    public class ElementMark : MonoBehaviour
    {
        public MarkState State { get; } = new();
        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        void OnDestroy() { if (health != null) health.Died -= OnDied; }

        void OnDied(DamageInfo _)
        {
            if (!State.HasMark) return;
            State.Clear();
            GameEvents.RaiseMarkCleared(health);
        }

        void Update()
        {
            if (State.Tick(Time.deltaTime)) GameEvents.RaiseMarkCleared(health);
        }
    }
}
