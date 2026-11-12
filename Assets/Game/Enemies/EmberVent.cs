using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Perigo do cenario: respiradouro de brasa. De tempos em tempos avisa um circulo e entra em erupcao.
    /// Queima quem estiver em cima, heroi ou inimigo: da para usar contra o bando.
    public class EmberVent : MonoBehaviour
    {
        [SerializeField] float interval = 4.5f;
        [SerializeField] float windup = 1.1f;
        [SerializeField] float radius = 1.9f;
        [SerializeField] float damage = 6f;

        float timer;
        bool warned;
        static readonly Collider[] buffer = new Collider[24];
        readonly HashSet<Health> seen = new();

        void OnEnable() => timer = Random.Range(1.5f, interval);

        void Update()
        {
            timer -= Time.deltaTime;
            if (!warned && timer <= windup)
            {
                warned = true;
                GameEvents.RaiseTelegraph(new TelegraphEvent
                {
                    Point = transform.position, Direction = Vector3.forward, Radius = radius, Duration = Mathf.Max(0f, timer), Source = gameObject,
                });
            }
            if (timer > 0f) return;
            timer = interval;
            warned = false;
            Erupt();
        }

        void OnDisable() => GameEvents.RaiseTelegraphCancelled(gameObject);

        void Erupt()
        {
            GameEvents.RaiseExploded(transform.position, radius);
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.IsDead || !seen.Add(h)) continue;
                var flat = h.transform.position - transform.position;
                flat.y = 0f;
                if (flat.magnitude > radius + 0.3f) continue;
                // o dano entra pelo time oposto ao do alvo, para valer nos dois lados
                var from = h.Team == Team.Player ? Team.Enemy : Team.Player;
                h.TakeDamage(new DamageInfo(damage, ElementType.Fire, from, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 1.2f, Direction = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward,
                    Knockback = 4f, IsReaction = true,
                });
            }
        }
    }
}
