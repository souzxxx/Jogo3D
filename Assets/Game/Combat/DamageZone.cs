using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Area no chao que causa dano elemental e/ou status em intervalos (rastro de fogo, poca de Magma, nuvem de Vapor).
    public class DamageZone : MonoBehaviour
    {
        float radius, life, interval, damage, tick, statusDuration;
        ElementType element;
        Team team;
        GameObject source;
        ReactionTable table;
        bool fromReaction, appliesStatus;
        StatusType status;
        HeroModifiers mods;
        static readonly List<DamageZone> active = new();
        /// Areas no chao agora (as de reacao podem virar fusao).
        public static IReadOnlyList<DamageZone> Active => active;
        /// Par de elementos da reacao que criou a area (None = area que nao e de reacao).
        public ElementType PairA { get; private set; }
        public ElementType PairB { get; private set; }
        public float Radius => radius;
        public bool Fusable => PairA != ElementType.None;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        public DamageZone WithPair(ElementType a, ElementType b) { PairA = a; PairB = b; return this; }
        static readonly Collider[] buffer = new Collider[32];
        static readonly HashSet<Health> seen = new();

        public static DamageZone Spawn(Vector3 point, float radius, float duration, float interval, float damage,
            ElementType element, Team team, GameObject source, ReactionTable table, bool fromReaction = false)
        {
            var go = new GameObject("Zone_" + element);
            go.transform.position = point;
            var z = go.AddComponent<DamageZone>();
            z.radius = radius; z.life = duration; z.interval = interval; z.damage = damage; z.tick = 0.1f;
            z.element = element; z.team = team; z.source = source; z.table = table; z.fromReaction = fromReaction;
            GameEvents.RaiseZoneSpawned(new ZoneEvent { Point = point, Radius = radius, Duration = duration, Element = element });
            return z;
        }

        /// Zona criada pelo heroi: as reacoes que ela detonar usam os bonus dele (forca de reacao e talentos de par).
        public DamageZone WithModifiers(HeroModifiers modifiers) { mods = modifiers; return this; }

        public DamageZone WithStatus(StatusType type, float duration)
        {
            appliesStatus = true; status = type; statusDuration = duration;
            return this;
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
                if (damage > 0f)
                    ElementCombat.Deliver(h, new DamageInfo(damage, element, team, source)
                        { Point = h.transform.position + Vector3.up * 1.2f, IsReaction = fromReaction }, table,
                        mods != null ? mods.ReactionPower : 1f, ElementType.None, mods);
                if (appliesStatus && !h.IsDead) StatusHolder.GetOrAdd(h).Apply(status, statusDuration, team, source);
            }
        }
    }
}
