using UnityEngine;

namespace Quintessence.Core
{
    /// Tudo que um golpe carrega. Quem causa dano monta um DamageInfo e entrega a um IDamageable.
    public struct DamageInfo
    {
        public float Amount;
        public ElementType Element;
        public Team SourceTeam;
        public GameObject Source;
        public Vector3 Point;
        public Vector3 Direction;
        public float Knockback;
        public bool IsCrit;
        /// Dano causado por uma reacao elemental (nao aplica marca nem dispara outra reacao).
        public bool IsReaction;
        /// Nao conta como abate do heroi (quem desmorona no fim do Cerco): sem XP, sem recarga, nao explode nem se divide.
        public bool NoCredit;

        public DamageInfo(float amount, ElementType element, Team sourceTeam, GameObject source = null)
        {
            Amount = amount;
            Element = element;
            SourceTeam = sourceTeam;
            Source = source;
            Point = default;
            Direction = default;
            Knockback = 0f;
            IsCrit = false;
            IsReaction = false;
            NoCredit = false;
        }
    }

    public interface IDamageable
    {
        Team Team { get; }
        bool IsDead { get; }
        /// Devolve o dano realmente aplicado (0 se invulneravel, morto ou do mesmo time).
        float TakeDamage(in DamageInfo info);
    }
}
