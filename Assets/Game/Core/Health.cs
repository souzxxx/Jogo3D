using System;
using UnityEngine;

namespace Quintessence.Core
{
    /// Componente que trata um golpe antes de ele virar dano (devolve true: o golpe para ali).
    public interface IDamageIntercept
    {
        bool Intercept(in DamageInfo info);
    }

    /// Vida de qualquer coisa que apanha (heroi, inimigo, pilar). Avisa por eventos locais e pelo GameEvents.
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] Team team = Team.Enemy;

        HealthPool pool;
        public HealthPool Pool => pool ??= new HealthPool(maxHealth);

        static readonly System.Collections.Generic.List<Health> active = new();
        /// Tudo que tem vida e esta ativo na cena (a mira do controle procura alvos aqui, sem varrer a cena a cada quadro).
        public static System.Collections.Generic.IReadOnlyList<Health> Active => active;
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

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
            // quem quer tratar o golpe antes da vida (barril de polvora molhando com Agua) fica com ele
            if (TryGetComponent(out IDamageIntercept intercept) && intercept.Intercept(info)) return 0f;
            float amount = info.Amount;
            if (TryGetComponent(out ElementalAffinity affinity)) amount *= affinity.Multiplier(info.Element);
            if (TryGetComponent(out FrontGuard guard)) amount *= guard.Multiplier(info);
            float applied = Pool.Damage(amount);
            if (applied <= 0f)
            {
                if (Pool.LastBlocked) GameEvents.RaiseHitBlocked(this, info);
                else if (Pool.LastEvaded) GameEvents.RaiseEvaded(this, info);
                else if (Pool.LastDodged) GameEvents.RaiseDodged(this, info);
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
