using System;

namespace Quintessence.Combat
{
    /// Regra da dash: duracao, cargas e recarga. Sem Unity, testavel.
    public class DashState
    {
        public float Duration = 0.15f;
        public float Cooldown = 0.6f;
        public int MaxCharges = 1;

        float timeLeft, recharge;
        int charges;

        public DashState() { charges = MaxCharges; }
        public DashState(float duration, float cooldown, int maxCharges)
        {
            Duration = duration; Cooldown = cooldown; MaxCharges = maxCharges; charges = maxCharges;
        }

        public bool IsDashing => timeLeft > 0f;
        public int Charges => charges;

        public bool TryStart()
        {
            if (IsDashing || charges <= 0) return false;
            if (charges == MaxCharges) recharge = Cooldown;
            charges--;
            timeLeft = Duration;
            return true;
        }

        public void Tick(float dt)
        {
            if (timeLeft > 0f) timeLeft = Math.Max(0f, timeLeft - dt);
            if (charges >= MaxCharges) return;
            recharge -= dt;
            if (recharge > 0f) return;
            charges++;
            recharge = Cooldown;
        }

        /// Troca o numero de cargas (ex.: dash de Ar tem 2).
        public void SetMaxCharges(int max)
        {
            MaxCharges = Math.Max(1, max);
            charges = Math.Min(charges, MaxCharges);
        }
    }
}
