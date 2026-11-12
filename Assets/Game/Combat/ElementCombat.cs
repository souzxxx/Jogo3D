using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Entrega um golpe elemental: dano, marca e, se a marca for de outro elemento, a reacao.
    public static class ElementCombat
    {
        public const float ShatterBonus = 1.3f;
        static readonly Collider[] buffer = new Collider[48];
        static readonly HashSet<Health> seen = new();

        /// Devolve o dano aplicado ao alvo direto.
        /// forceReactionWith: na Quintessencia o golpe reage como se o alvo ja estivesse marcado com esse elemento.
        public static float Deliver(Health target, DamageInfo info, ReactionTable table, float reactionPower = 1f,
            ElementType forceReactionWith = ElementType.None, HeroModifiers mods = null)
        {
            // Congelado: o proximo golpe direto estilhaca (+30%) e encerra o gelo. O gelo sai depois do dano:
            // quem olha o status durante o golpe (o escudo do Escudeiro) ainda ve o alvo congelado
            bool shatter = !info.IsReaction && target.TryGetComponent(out StatusHolder st) && st.State.Has(StatusType.Frozen);
            if (shatter) info.Amount *= ShatterBonus;

            float applied = target.TakeDamage(info);
            if (shatter && target.TryGetComponent(out StatusHolder frozen))
            {
                frozen.Clear(StatusType.Frozen);
                GameEvents.RaiseShattered(target);
            }
            if (applied <= 0f || info.Element == ElementType.None || info.IsReaction) return applied;

            if (!target.TryGetComponent(out ElementMark mark))
            {
                if (target.IsDead && forceReactionWith == ElementType.None) return applied;
                mark = target.gameObject.AddComponent<ElementMark>();
            }

            if (forceReactionWith != ElementType.None && forceReactionWith != info.Element)
            {
                if (mark.State.TryLock())
                {
                    if (mark.State.HasMark) { mark.State.Clear(); GameEvents.RaiseMarkCleared(target); }
                    React(target, forceReactionWith, info, table, reactionPower, mods);
                }
                return applied;
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
                    React(target, consumed, info, table, reactionPower, mods);
                    break;
            }
            return applied;
        }

        /// Fusao: a skill de um terceiro elemento sobre uma area de reacao. Fere e aplica o efeito em volta e consome a area.
        public static bool Fuse(DamageZone zone, ElementType third, Team team, GameObject source, float power = 1f)
        {
            if (zone == null || !zone.Fusable) return false;
            var kind = FusionRules.Find(zone.PairA, zone.PairB, third);
            if (kind == FusionKind.None) return false;
            var st = FusionRules.Stats(kind);
            var center = zone.transform.position;
            float radius = zone.Radius * st.radiusMult + 0.5f;
            GameEvents.RaiseReaction(new ReactionEvent
            {
                Mark = zone.PairA, Trigger = third, Point = center, Target = null, Source = source, Name = FusionRules.Name(kind), Radius = radius,
            });
            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Collide);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.Team == team || h.IsDead || !seen.Add(h)) continue;
                var dir = h.transform.position - center; dir.y = 0f;
                dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
                if (st.knockback < 0f) dir = -dir; // Ciclone puxa para o centro
                h.TakeDamage(new DamageInfo(st.damage * power, st.element, team, source)
                {
                    IsReaction = true, Point = h.transform.position + Vector3.up * 1.2f, Direction = dir, Knockback = Mathf.Abs(st.knockback),
                });
                if (!h.IsDead) StatusHolder.GetOrAdd(h).Apply(st.status, st.statusTime, team, source);
            }
            // sai da lista de areas ja (o Destroy so vale no fim do quadro): nada a renova no mesmo quadro
            zone.enabled = false;
            if (Application.isPlaying) Object.Destroy(zone.gameObject); else Object.DestroyImmediate(zone.gameObject);
            return true;
        }

        static void React(Health target, ElementType markElement, DamageInfo info, ReactionTable table, float power, HeroModifiers mods = null)
        {
            var r = table != null ? table.Find(markElement, info.Element) : null;
            if (r == null) return;
            // talento de reacao: aquele par bate mais forte e pega uma area maior
            float pair = mods != null ? mods.ReactionBonus(markElement, info.Element) : 1f;
            power *= pair;
            float radius = r.radius * (1f + (pair - 1f) * 0.5f);
            var center = target.transform.position;
            GameEvents.RaiseReaction(new ReactionEvent
            {
                Mark = markElement, Trigger = info.Element, Point = center,
                Target = target.gameObject, Source = info.Source, Name = r.name, Radius = radius,
            });

            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Collide);
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
                if (h.IsDead) continue;
                if (r.appliesStatus) StatusHolder.GetOrAdd(h).Apply(r.status, r.statusDuration, info.SourceTeam, info.Source);
                if (r.appliesSecondStatus) StatusHolder.GetOrAdd(h).Apply(r.secondStatus, r.secondStatusDuration, info.SourceTeam, info.Source);
            }

            if (r.zoneDuration <= 0f) return;
            // varios alvos da mesma reacao lado a lado: uma area so (renovada), nao uma pilha que viraria varias Fusoes
            var existing = DamageZone.FindPaired(center, radius, markElement, info.Element);
            if (existing != null) { existing.Refresh(r.zoneDuration); return; }
            var zone = DamageZone.Spawn(center, radius, r.zoneDuration, r.zoneInterval, r.zoneDamage * power,
                info.Element, info.SourceTeam, info.Source, table, fromReaction: true);
            if (r.zoneAppliesStatus) zone.WithStatus(r.zoneStatus, r.zoneStatusDuration);
            zone.WithPair(markElement, info.Element);
        }
    }
}
