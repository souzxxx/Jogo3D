using System;

namespace Quintessence.Combat
{
    /// Medidor e estado da Quintessencia: enche com reacoes, exige os quatro elementos, dura 8 s.
    public class QuintessenceState
    {
        public const float Max = 100f;
        public const float PerReaction = 10f;
        public const float PerPartnerReaction = 3f;
        public const float Duration = 8f;
        public const float DamageTaken = 0.5f;

        public float Meter { get; private set; }
        public bool Active { get; private set; }
        float left;

        public bool Ready => !Active && Meter >= Max;
        /// Ativa: tempo restante. Inativa: quanto do medidor esta cheio.
        public float Normalized => Active ? left / Duration : Meter / Max;

        public void AddReaction(bool own = true) => AddMeter(own ? PerReaction : PerPartnerReaction);

        public void AddMeter(float amount)
        {
            if (Active || amount <= 0f) return;
            Meter = Math.Min(Max, Meter + amount);
        }

        public bool TryActivate(bool hasAllElements)
        {
            if (!hasAllElements || !Ready) return false;
            Active = true;
            left = Duration;
            Meter = 0f;
            return true;
        }

        /// Devolve true no tick em que a Quintessencia acaba.
        public bool Tick(float dt)
        {
            if (!Active) return false;
            left -= dt;
            if (left > 0f) return false;
            Active = false;
            left = 0f;
            return true;
        }
    }
}
