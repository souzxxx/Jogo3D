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

        /// Inimigos vivos na cena (posicoes), para o Pick.
        public static List<Vector3> EnemyPositions()
        {
            buffer.Clear();
            foreach (var h in Health.Active)
                if (h != null && h.Team == Team.Enemy && !h.IsDead) buffer.Add(h.transform.position);
            return buffer;
        }
    }
}
