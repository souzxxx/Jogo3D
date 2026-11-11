using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quintessence.Core
{
    public enum StatusType { Burn, Soaked, Stun, Frozen, Rooted, ArmorBroken, Blinded }

    public static class ElementColors
    {
        public static Color Of(ElementType e) => e switch
        {
            ElementType.Fire => new Color(1f, 0.38f, 0.08f),
            ElementType.Water => new Color(0.15f, 0.6f, 1f),
            ElementType.Earth => new Color(0.72f, 0.5f, 0.2f),
            ElementType.Air => new Color(0.65f, 1f, 0.85f),
            _ => Color.white,
        };
    }

    /// Regra dos status (sem Unity): duracoes, tique da Queimadura e o que cada um impede.
    public class StatusState
    {
        public const float BurnInterval = 0.5f;
        public const float BurnDamage = 3f;
        static readonly int Count = Enum.GetValues(typeof(StatusType)).Length;

        readonly float[] time = new float[Count];
        float burnAcc;

        public bool Has(StatusType t) => time[(int)t] > 0f;
        public float Remaining(StatusType t) => time[(int)t];

        /// Aplica ou renova (nao acumula). Devolve true se o status nao estava ativo.
        public bool Apply(StatusType t, float duration)
        {
            bool isNew = !Has(t);
            time[(int)t] = Math.Max(time[(int)t], duration);
            if (isNew && t == StatusType.Burn) burnAcc = 0f;
            return isNew;
        }

        public bool Clear(StatusType t)
        {
            bool had = Has(t);
            time[(int)t] = 0f;
            return had;
        }

        /// Devolve quantos tiques de Queimadura sairam; preenche os status que acabaram.
        public int Tick(float dt, List<StatusType> ended = null)
        {
            int ticks = 0;
            if (Has(StatusType.Burn))
            {
                burnAcc += dt;
                while (burnAcc >= BurnInterval - 1e-4f) { burnAcc -= BurnInterval; ticks++; }
            }
            for (int i = 0; i < Count; i++)
            {
                if (time[i] <= 0f) continue;
                time[i] -= dt;
                if (time[i] > 1e-4f) continue;
                time[i] = 0f;
                ended?.Add((StatusType)i);
            }
            return ticks;
        }

        public bool CanAct => !Has(StatusType.Stun) && !Has(StatusType.Frozen);
        public float MoveMultiplier =>
            Has(StatusType.Stun) || Has(StatusType.Frozen) || Has(StatusType.Rooted) ? 0f : Has(StatusType.Soaked) ? 0.7f : 1f;
        public float DamageTakenMultiplier => Has(StatusType.ArmorBroken) ? 1.25f : 1f;
        /// Chance de errar o ataque (Vapor).
        public float MissChance => Has(StatusType.Blinded) ? 0.5f : 0f;
    }

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
