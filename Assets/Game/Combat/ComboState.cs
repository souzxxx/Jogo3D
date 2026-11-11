using System;

namespace Quintessence.Combat
{
    [Serializable]
    public struct ComboStep
    {
        public float Damage;
        /// Tempo total do golpe em segundos.
        public float Duration;
        /// Momento do golpe em que o dano sai.
        public float HitTime;
        public float Knockback;

        public ComboStep(float damage, float duration, float hitTime, float knockback)
        {
            Damage = damage; Duration = duration; HitTime = hitTime; Knockback = knockback;
        }
    }

    /// Regra do combo: encadeia golpes se o ataque for pedido de novo depois do hit, e zera se o jogador demora.
    public class ComboState
    {
        readonly ComboStep[] steps;
        readonly float resetTime;
        int index = -1, next;
        float t, sinceEnd;
        bool hitDone, buffered;

        public ComboState(ComboStep[] steps, float resetTime = 0.5f)
        {
            if (steps == null || steps.Length == 0) throw new ArgumentException("combo sem golpes");
            this.steps = steps;
            this.resetTime = resetTime;
        }

        public bool IsAttacking => index >= 0;
        public int CurrentIndex => index;
        public ComboStep Current => steps[Math.Max(0, index)];
        public int StepCount => steps.Length;
        /// Depois do hit o golpe pode ser cancelado em dash ou troca de elemento.
        public bool PastHit => IsAttacking && hitDone;

        /// Devolve true se um golpe comecou agora. Durante um golpe, guarda o pedido para encadear.
        public bool TryAttack()
        {
            if (IsAttacking)
            {
                if (hitDone) buffered = true;
                return false;
            }
            Start(next);
            return true;
        }

        void Start(int i)
        {
            index = i; t = 0f; hitDone = false; buffered = false;
        }

        /// hit: o dano sai neste tick. chained: o proximo golpe do combo comecou neste tick.
        public void Tick(float dt, out bool hit, out bool chained)
        {
            hit = false; chained = false;
            if (!IsAttacking)
            {
                if (next != 0 && (sinceEnd += dt) >= resetTime) next = 0;
                return;
            }
            t += dt;
            if (!hitDone && t >= steps[index].HitTime) { hitDone = true; hit = true; }
            if (t < steps[index].Duration) return;

            bool last = index == steps.Length - 1;
            if (buffered && !last) { Start(index + 1); chained = true; return; }
            next = last ? 0 : index + 1;
            sinceEnd = 0f;
            index = -1;
        }

        public void Cancel()
        {
            if (!IsAttacking) return;
            next = 0; sinceEnd = 0f; index = -1; buffered = false;
        }
    }
}
