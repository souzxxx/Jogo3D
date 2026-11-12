using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Elite Voláteis: o inimigo morto deixa um circulo avisado que explode logo depois. Fere so o heroi.
    public class VolatileBurst : MonoBehaviour
    {
        float radius, left, damage;
        static readonly Collider[] buffer = new Collider[16];

        public static VolatileBurst Spawn(Vector3 point, float radius, float delay, float damage)
        {
            var go = new GameObject("VolatileBurst");
            go.transform.position = point;
            var b = go.AddComponent<VolatileBurst>();
            b.radius = radius; b.left = delay; b.damage = damage;
            GameEvents.RaiseTelegraph(new TelegraphEvent { Point = point, Direction = Vector3.forward, Radius = radius, Duration = delay, Source = go });
            return b;
        }

        void Update()
        {
            if ((left -= Time.deltaTime) > 0f) return;
            var center = transform.position;
            GameEvents.RaiseExploded(center, radius);
            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.Team != Team.Player || h.IsDead) continue;
                var dir = h.transform.position - center; dir.y = 0f;
                if (dir.magnitude > radius + 0.3f) continue;
                h.TakeDamage(new DamageInfo(damage, ElementType.Fire, Team.Enemy, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 1.2f, Direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward, Knockback = 5f,
                });
                break;
            }
            Destroy(gameObject);
        }
    }
}
