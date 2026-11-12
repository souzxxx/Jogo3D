using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quintessence.Core
{
    /// Status de um personagem. Inimigos e heroi leem State.MoveMultiplier / CanAct / MissChance.
    [RequireComponent(typeof(Health))]
    public class StatusHolder : MonoBehaviour
    {
        public StatusState State { get; } = new();
        Health health;
        Team burnTeam;
        GameObject burnSource;
        readonly List<StatusType> ended = new();

        public static StatusHolder GetOrAdd(Health h) =>
            h.TryGetComponent(out StatusHolder s) ? s : h.gameObject.AddComponent<StatusHolder>();

        void Awake() => health = GetComponent<Health>();

        public void Apply(StatusType type, float duration, Team sourceTeam, GameObject source = null)
        {
            if (health == null) health = GetComponent<Health>();
            if (health.IsDead) return;
            if (TryGetComponent(out ElementalAffinity affinity) && affinity.Immune(type)) return;
            State.Apply(type, duration);
            if (type == StatusType.Burn) { burnTeam = sourceTeam; burnSource = source; }
            health.Pool.DamageTakenMultiplier = State.DamageTakenMultiplier;
            GameEvents.RaiseStatusApplied(health, type, duration);
        }

        public void Clear(StatusType type)
        {
            if (!State.Clear(type)) return;
            health.Pool.DamageTakenMultiplier = State.DamageTakenMultiplier;
            GameEvents.RaiseStatusEnded(health, type);
        }

        public void ClearNegative()
        {
            foreach (StatusType t in Enum.GetValues(typeof(StatusType)))
                if (State.Clear(t)) GameEvents.RaiseStatusEnded(health, t);
            health.Pool.DamageTakenMultiplier = 1f;
        }

        void Update()
        {
            if (health.IsDead) { if (AnyActive()) ClearNegative(); return; }
            ended.Clear();
            int ticks = State.Tick(Time.deltaTime, ended);
            for (int i = 0; i < ticks && !health.IsDead; i++)
                health.TakeDamage(new DamageInfo(StatusState.BurnDamage, ElementType.Fire, burnTeam, burnSource)
                    { IsReaction = true, Point = transform.position + Vector3.up * 1.2f });
            foreach (var t in ended) GameEvents.RaiseStatusEnded(health, t);
            if (ended.Count > 0) health.Pool.DamageTakenMultiplier = State.DamageTakenMultiplier;
        }

        bool AnyActive()
        {
            foreach (StatusType t in Enum.GetValues(typeof(StatusType))) if (State.Has(t)) return true;
            return false;
        }
    }
}
