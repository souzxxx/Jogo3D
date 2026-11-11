using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Area no chao que causa dano elemental em intervalos (rastro de fogo, poca de lava).
    public class DamageZone : MonoBehaviour
    {
        float radius, life, interval, damage, tick;
        ElementType element;
        Team team;
        GameObject source;
        ReactionTable table;
        static readonly Collider[] buffer = new Collider[32];
        static readonly HashSet<Health> seen = new();

        public static DamageZone Spawn(Vector3 point, float radius, float duration, float interval, float damage,
            ElementType element, Team team, GameObject source, ReactionTable table)
        {
            var go = new GameObject("Zone_" + element);
            go.transform.position = point;
            var z = go.AddComponent<DamageZone>();
            z.radius = radius; z.life = duration; z.interval = interval; z.damage = damage; z.tick = 0.1f;
            z.element = element; z.team = team; z.source = source; z.table = table;
            GameEvents.RaiseZoneSpawned(new ZoneEvent { Point = point, Radius = radius, Duration = duration, Element = element });
            return z;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            tick -= dt;
            if (tick <= 0f) { tick += interval; Pulse(); }
            if (life <= 0f) Destroy(gameObject);
        }

        void Pulse()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, radius, buffer, ~0, QueryTriggerInteraction.Collide);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.Team == team || h.IsDead || !seen.Add(h)) continue;
                ElementCombat.Deliver(h, new DamageInfo(damage, element, team, source)
                    { Point = h.transform.position + Vector3.up * 1.2f }, table);
            }
        }
    }
}
