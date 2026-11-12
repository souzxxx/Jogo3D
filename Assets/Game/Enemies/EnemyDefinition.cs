using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Explode: avisa um circulo em volta de si, explode (fere heroi e outros inimigos) e morre.
    /// Charge: avisa uma linha e corre por ela. Lob: avisa um circulo onde o heroi esta e a bomba cai ali.
    /// Heal: cura os aliados feridos em volta; sem ninguem para curar, atira.
    public enum EnemyAttackKind { Melee, Projectile, Explode, Charge, Lob, Heal }

    /// Numeros de um tipo de inimigo.
    [CreateAssetMenu(menuName = "Quintessence/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        public string displayName = "Inimigo";
        public float maxHealth = 30f;
        public float moveSpeed = 4f;

        [Header("Armadura")]
        [Tooltip("Enquanto tiver armadura, leva so esta fracao do dano. Cai com Armadura Quebrada (finalizador de Terra).")]
        public bool armored;
        [Range(0.05f, 1f)] public float armoredDamageTaken = 0.4f;

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
        [Tooltip("So para Explode: dano nos outros inimigos pegos na explosao.")]
        public float explosionDamageToEnemies = 12f;

        [Header("Elemental")]
        [Tooltip("Resiste a este elemento e e fraco ao que o vence. None = comum.")]
        public ElementType affinity = ElementType.None;
        [Tooltip("Nao e empurrado (Sentinela).")]
        public bool immovable;
        [Tooltip("Ao morrer, estoura em brasas num circulo avisado (Brasa Viva).")]
        public bool burstOnDeath;

        [Header("Escudo")]
        [Tooltip("Arco a frente coberto pelo escudo, em graus. 0 = sem escudo (Escudeiro).")]
        public float guardArc;
        [Range(0f, 1f)] public float guardDamageTaken = 0.15f;
        [Tooltip("Graus por segundo que o inimigo gira para encarar o heroi. Baixo = da para contornar.")]
        public float turnSpeed = 540f;

        [Header("Investida")]
        public float chargeDistance = 8f;
        public float chargeSpeed = 22f;
        [Tooltip("Meia largura do corredor da investida.")]
        public float chargeWidth = 0.9f;

        [Tooltip("So para Lob: segundos da poca que o orbe deixa no chao (0 = sem poca). Afogado.")]
        public float puddleSeconds;

        [Header("Cura")]
        public float healAmount = 14f;
        public float healRadius = 6f;

        [Header("Projetil")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 11f;
        public float projectileRange = 16f;
        [Tooltip("O atirador recua se o heroi chegar mais perto que isso.")]
        public float keepDistance = 6f;
    }
}
