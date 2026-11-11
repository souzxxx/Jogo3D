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
            // tons contidos, no mesmo registro dos modelos do pack (nada de neon)
            ElementType.Fire => new Color(0.86f, 0.38f, 0.18f),
            ElementType.Water => new Color(0.27f, 0.52f, 0.74f),
            ElementType.Earth => new Color(0.63f, 0.47f, 0.27f),
            ElementType.Air => new Color(0.6f, 0.8f, 0.68f),
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
}
