using System;

namespace Quintessence.Core
{
    /// Regra de vida pura (sem Unity), usada pelo componente Health e testavel em EditMode.
    public class HealthPool
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool Invulnerable { get; set; }
        /// Multiplicador de dano recebido (ex.: Armadura Quebrada = 1.25, Quintessencia = 0.5).
        public float DamageTakenMultiplier { get; set; } = 1f;
        /// Segundo multiplicador, independente do de status (ex.: Quintessencia = 0.5).
        public float ExtraDamageTakenMultiplier { get; set; } = 1f;
        /// Golpes que serao totalmente bloqueados (dash de Terra da 1).
        public int Blocks { get; set; }
        /// O ultimo Damage() foi anulado por um bloqueio.
        public bool LastBlocked { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Normalized => Max <= 0f ? 0f : Current / Max;

        public HealthPool(float max)
        {
            Max = Math.Max(1f, max);
            Current = Max;
        }

        /// Devolve o dano aplicado. Nunca passa da vida atual; morto ou invulneravel devolve 0.
        public float Damage(float amount)
        {
            LastBlocked = false;
            if (IsDead || Invulnerable || amount <= 0f) return 0f;
            if (Blocks > 0) { Blocks--; LastBlocked = true; return 0f; }
            float applied = Math.Min(Current, amount * Math.Max(0f, DamageTakenMultiplier) * Math.Max(0f, ExtraDamageTakenMultiplier));
            Current -= applied;
            return applied;
        }

        /// Devolve a cura aplicada. Morto nao cura (use Revive).
        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f) return 0f;
            float applied = Math.Min(Max - Current, amount);
            Current += applied;
            return applied;
        }

        public void Revive(float fraction = 1f)
        {
            Current = Math.Max(1f, Max * Math.Min(1f, Math.Max(0f, fraction)));
        }

        /// Muda a vida maxima mantendo a proporcao atual.
        public void SetMax(float max)
        {
            float ratio = Normalized;
            Max = Math.Max(1f, max);
            Current = IsDead ? 0f : Math.Max(1f, Max * ratio);
        }
    }
}
