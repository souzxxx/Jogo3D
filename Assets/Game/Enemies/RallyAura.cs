using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Porta-Estandarte: enquanto vive, os inimigos em volta batem mais forte e andam mais rapido. Derrube-o primeiro.
    [RequireComponent(typeof(Health))]
    public class RallyAura : MonoBehaviour
    {
        [SerializeField] float radius = 6f;
        [SerializeField] float damageMult = 1.25f;
        [SerializeField] float speedMult = 1.15f;
        const float Pulse = 0.4f;
        float tick;
        Health health;
        static readonly Collider[] buffer = new Collider[32];
        readonly HashSet<EnemyBrain> seen = new();
        public float Radius => radius;
        /// Quantos aliados a ultima pulsacao fortaleceu (para os testes).
        public int LastRallied { get; private set; }

        public void Configure(float r, float damage, float speed)
        {
            radius = r; damageMult = damage; speedMult = speed;
            var marker = GetComponent<AuraSource>() ?? gameObject.AddComponent<AuraSource>();
            marker.Configure(r);
        }

        void Awake() => health = GetComponent<Health>();

        void Update()
        {
            if (health == null || health.IsDead) return;
            if ((tick -= Time.deltaTime) > 0f) return;
            tick = Pulse;
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            LastRallied = 0;
            for (int i = 0; i < n; i++)
            {
                var brain = buffer[i].GetComponentInParent<EnemyBrain>();
                if (brain == null || brain.gameObject == gameObject || !seen.Add(brain)) continue;
                if (brain.TryGetComponent(out Health h) && h.IsDead) continue;
                // um pouco mais que o pulso: quem sai do circulo perde o bonus logo
                brain.Rally(Pulse + 0.2f, damageMult, speedMult);
                LastRallied++;
            }
        }
    }
}
