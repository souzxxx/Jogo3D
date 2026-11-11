using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    public enum EnemyAttackKind { Melee, Projectile }

    /// Numeros de um tipo de inimigo.
    [CreateAssetMenu(menuName = "Quintessence/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        public string displayName = "Inimigo";
        public float maxHealth = 30f;
        public float moveSpeed = 4f;

        [Header("Ataque")]
        public EnemyAttackKind attack = EnemyAttackKind.Melee;
        public float damage = 8f;
        public float knockback = 3f;
        [Tooltip("Distancia do heroi em que o inimigo comeca a preparar o golpe.")]
        public float attackRange = 2.2f;
        [Tooltip("Tempo de aviso antes do golpe sair. Nunca menos que 0,4 s.")]
        [Min(0.4f)] public float telegraphTime = 0.55f;
        public float recoverTime = 0.5f;
        public float cooldown = 1.4f;
        public ElementType element = ElementType.None;

        [Header("Corpo a corpo")]
        [Tooltip("Centro da area do golpe, a frente do inimigo.")]
        public float strikeOffset = 1.5f;
        public float strikeRadius = 1.5f;

        [Header("Projetil")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 11f;
        public float projectileRange = 16f;
        [Tooltip("O atirador recua se o heroi chegar mais perto que isso.")]
        public float keepDistance = 6f;
    }
}
