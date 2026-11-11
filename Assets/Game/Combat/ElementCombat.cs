using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Entrega um golpe elemental: dano, marca e, se a marca for de outro elemento, a reacao.
    public static class ElementCombat
    {
        static readonly Collider[] buffer = new Collider[48];
        static readonly HashSet<Health> seen = new();

        /// Devolve o dano aplicado ao alvo direto.
        public static float Deliver(Health target, DamageInfo info, ReactionTable table, float reactionPower = 1f)
        {
            float applied = target.TakeDamage(info);
            if (applied <= 0f || info.Element == ElementType.None || info.IsReaction) return applied;

            if (!target.TryGetComponent(out ElementMark mark))
            {
                if (target.IsDead) return applied;
                mark = target.gameObject.AddComponent<ElementMark>();
            }
            if (target.IsDead && !mark.State.HasMark) return applied;

            switch (mark.State.Hit(info.Element, out var consumed))
            {
                case MarkOutcome.Applied:
                case MarkOutcome.Refreshed:
                    if (target.IsDead) mark.State.Clear();
                    else GameEvents.RaiseMarkApplied(info.Element, target);
                    break;
                case MarkOutcome.Reaction:
                    GameEvents.RaiseMarkCleared(target);
                    React(target, consumed, info, table, reactionPower);
                    break;
            }
            return applied;
        }

        static void React(Health target, ElementType markElement, DamageInfo info, ReactionTable table, float power)
        {
            var r = table != null ? table.Find(markElement, info.Element) : null;
            if (r == null) return;
            var center = target.transform.position;
            GameEvents.RaiseReaction(new ReactionEvent
            {
                Mark = markElement, Trigger = info.Element, Point = center,
                Target = target.gameObject, Source = info.Source, Name = r.name, Radius = r.radius,
            });

            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, r.radius, buffer, ~0, QueryTriggerInteraction.Collide);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.Team == info.SourceTeam || h.IsDead || !seen.Add(h)) continue;
                var dir = h.transform.position - center;
                dir.y = 0f;
                dir = dir.sqrMagnitude > 0.01f ? dir.normalized : info.Direction;
                h.TakeDamage(new DamageInfo(r.damage * power, info.Element, info.SourceTeam, info.Source)
                {
                    IsReaction = true, Point = h.transform.position + Vector3.up * 1.2f, Direction = dir, Knockback = r.knockback,
                });
                if (r.appliesStatus && !h.IsDead)
                    StatusHolder.GetOrAdd(h).Apply(r.status, r.statusDuration, info.SourceTeam, info.Source);
            }
        }
    }
}
