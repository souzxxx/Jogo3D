using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Projetil reto. Acerta o primeiro alvo do outro time e some em parede. O pilar de pedra do heroi bloqueia.
    public class Projectile : MonoBehaviour
    {
        Vector3 direction;
        float speed, damage, knockback, left, radius = 0.35f;
        Team team;
        ElementType element;
        GameObject source;
        static readonly Collider[] buffer = new Collider[16];

        public static Projectile Spawn(GameObject prefab, Vector3 position, Vector3 direction, float speed, float range,
            float damage, float knockback, ElementType element, Team team, GameObject source)
        {
            var go = prefab != null ? Instantiate(prefab, position, Quaternion.LookRotation(direction)) : new GameObject("Projectile");
            go.transform.position = position;
            var p = go.TryGetComponent(out Projectile existing) ? existing : go.AddComponent<Projectile>();
            p.direction = direction.normalized; p.speed = speed; p.left = range; p.damage = damage; p.knockback = knockback;
            p.element = element; p.team = team; p.source = source;
            return p;
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            transform.position += direction * step;
            left -= step;
            if (left <= 0f) { Destroy(gameObject); return; }

            int n = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h != null)
                {
                    if (h.Team == team) continue; // atravessa aliados
                    h.TakeDamage(new DamageInfo(damage, element, team, source)
                        { Point = transform.position, Direction = direction, Knockback = knockback });
                }
                Destroy(gameObject);
                return;
            }
        }
    }
}
