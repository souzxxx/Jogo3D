using System;
using UnityEngine;

namespace Quintessence.Core
{
    /// Vida de qualquer coisa que apanha (heroi, inimigo, pilar). Avisa por eventos locais e pelo GameEvents.
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] Team team = Team.Enemy;

        HealthPool pool;
        public HealthPool Pool => pool ??= new HealthPool(maxHealth);

        public Team Team => team;
        public bool IsDead => Pool.IsDead;
        public float Current => Pool.Current;
        public float Max => Pool.Max;

        /// (golpe, dano aplicado)
        public event Action<DamageInfo, float> Damaged;
        public event Action<float> Healed;
        public event Action<DamageInfo> Died;

        public void Configure(float max, Team newTeam)
        {
            maxHealth = max;
            team = newTeam;
            pool = new HealthPool(max);
        }

        public float TakeDamage(in DamageInfo info)
        {
            if (info.SourceTeam == team) return 0f;
            float applied = Pool.Damage(info.Amount);
            if (applied <= 0f)
            {
                if (Pool.LastBlocked) GameEvents.RaiseHitBlocked(this, info);
                return 0f;
            }
            Damaged?.Invoke(info, applied);
            GameEvents.RaiseDamageDealt(this, info, applied);
            if (Pool.IsDead)
            {
                Died?.Invoke(info);
                GameEvents.RaiseDied(this, info);
            }
            return applied;
        }

        public float Heal(float amount)
        {
            float applied = Pool.Heal(amount);
            if (applied > 0f) Healed?.Invoke(applied);
            return applied;
        }
    }
}
