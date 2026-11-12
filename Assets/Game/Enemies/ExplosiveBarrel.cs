using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Barril de polvora: qualquer golpe acende. Estoura depois de um instante e fere todos em volta,
    /// bem mais os inimigos que o heroi. Um barril acende o outro.
    [RequireComponent(typeof(Health))]
    public class ExplosiveBarrel : MonoBehaviour
    {
        [SerializeField] float radius = 3.2f;
        [SerializeField] float damageToEnemies = 26f;
        [SerializeField] float damageToHero = 10f;
        [SerializeField] float fuse = 0.22f;

        Health health;
        bool lit;
        static readonly Collider[] buffer = new Collider[32];
        public float Radius => radius;
        public bool Lit => lit;

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += _ => Light();
        }

        void Light()
        {
            if (lit) return;
            lit = true;
            GameEvents.RaiseTelegraph(new TelegraphEvent { Point = transform.position, Direction = Vector3.forward, Radius = radius, Duration = fuse, Source = gameObject });
            Invoke(nameof(Explode), fuse);
        }

        void Explode()
        {
            var center = transform.position;
            GameEvents.RaiseExploded(center, radius);
            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            var seen = new HashSet<Health>();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h == health || h.IsDead || !seen.Add(h)) continue;
                var dir = h.transform.position - center; dir.y = 0f;
                if (dir.magnitude > radius + 0.4f) continue;
                bool hero = h.Team == Team.Player;
                h.TakeDamage(new DamageInfo(hero ? damageToHero : damageToEnemies, ElementType.Fire, hero ? Team.Enemy : Team.Player, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 1.2f, Direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward,
                    Knockback = 7f, IsReaction = true,
                });
            }
            Destroy(gameObject);
        }
    }
}
