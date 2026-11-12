using UnityEngine;

namespace Quintessence.Enemies
{
    /// Numeros do chefe.
    [CreateAssetMenu(menuName = "Quintessence/Boss Definition")]
    public class BossDefinition : ScriptableObject
    {
        public string displayName = "Guardião das Cinzas";
        public float maxHealth = 600f;
        public float moveSpeed = 2.6f;
        [Tooltip("Pausa entre ataques em cada fase.")]
        public float[] cooldownByPhase = { 1.7f, 1.3f, 1.0f };

        [Header("Pancada: circulo a frente")]
        public float slamDamage = 18f;
        public float slamRadius = 3.2f;
        public float slamOffset = 2.6f;
        public float slamWindup = 0.9f;
        public float slamRange = 5f;

        [Header("Leque de brasas: projeteis em leque")]
        public GameObject projectilePrefab;
        public float fanDamage = 9f;
        public int fanCount = 3;
        public int fanCountLastPhase = 5;
        public float fanSpread = 22f;
        public float fanWindup = 0.8f;
        public float projectileSpeed = 10f;
        public float projectileRange = 18f;

        [Header("Erupcao: circulos perto do heroi")]
        public float eruptionDamage = 12f;
        public float eruptionRadius = 2f;
        public int eruptionCount = 4;
        public float eruptionWindup = 1.1f;
        public float eruptionScatter = 4.5f;

        [Header("Investida: linha larga, depois ele atravessa por ela")]
        public float chargeDamage = 16f;
        public float chargeWidth = 1.7f;
        public float chargeLength = 13f;
        public float chargeWindup = 1.05f;
        public float chargeSpeed = 26f;

        [Header("Anel de cinzas: circulos em volta dele")]
        public float ringDamage = 11f;
        public float ringCircleRadius = 2.1f;
        public float ringRadius = 5.7f;
        public int ringCount = 8;
        public float ringWindup = 1.15f;

        [Header("Escudo da ultima fase")]
        [Tooltip("Desligado no mini-chefe: a ultima fase so acelera, sem escudo.")]
        public bool lastPhaseShield = true;
        [Tooltip("Sem os quatro elementos nao ha Quintessencia: o escudo cai sozinho depois deste tempo, para nao travar a luta.")]
        public float shieldTimeout = 12f;
        public GameObject[] addPrefabs;
        public float addInterval = 8f;
        public int maxAdds = 4;
        public int addsPerPhaseChange = 2;
    }
}
