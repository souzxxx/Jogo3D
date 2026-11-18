using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Ajuda de mira do controle: o analogico aponta mais ou menos; se houver um inimigo vivo dentro de um cone estreito
    /// em volta dessa direcao, a mira vai nele. Pesa angulo e distancia (o mais alinhado ganha, o perto desempata).
    public static class AimAssist
    {
        public const float MaxAngle = 28f, MaxDistance = 9f;

        /// Melhor alvo para quem esta em from apontando para dir (no chao). Nulo se nenhum serve.
        public static Vector3? Pick(Vector3 from, Vector3 dir, IEnumerable<Vector3> targets, float maxAngle = MaxAngle, float maxDistance = MaxDistance)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return null;
            dir.Normalize();
            Vector3? best = null;
            float bestScore = float.MaxValue;
            foreach (var t in targets)
            {
                var to = t - from; to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.3f || dist > maxDistance) continue;
                float angle = Vector3.Angle(dir, to);
                if (angle > maxAngle) continue;
                float score = angle / maxAngle + 0.35f * dist / maxDistance;
                if (score < bestScore) { bestScore = score; best = t; }
            }
            return best;
        }

        static readonly List<Vector3> buffer = new();

        /// Inimigos vivos que o heroi em from enxerga (posicoes), para o Pick. So criaturas: barril, cristal e pilar tambem
        /// tem vida do time inimigo, mas a mira nao deve preferi-los (o boneco do treino entra, para o treino ensinar a mira).
        /// Inimigo atras de parede ou pilar fica de fora.
        public static List<Vector3> EnemyPositions(Vector3 from)
        {
            buffer.Clear();
            var eye = from + Vector3.up * 1.1f;
            foreach (var h in Health.Active)
            {
                if (h == null || h.Team != Team.Enemy || h.IsDead || !IsCreature(h)) continue;
                var p = h.transform.position;
                if ((p - from).sqrMagnitude > MaxDistance * MaxDistance) continue;
                if (Physics.Linecast(eye, p + Vector3.up * 1.1f, out var hit, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<Health>() != h && hit.collider.GetComponentInParent<PlayerController>() == null) continue;
                buffer.Add(p);
            }
            return buffer;
        }

        static bool IsCreature(Health h) =>
            h.GetComponent<UnityEngine.AI.NavMeshAgent>() != null || h.GetComponent("BossBrain") != null || h.GetComponent<DummyTarget>() != null;
    }
}
