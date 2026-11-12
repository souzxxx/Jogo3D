using System;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Charge: investida em linha larga. Ring: anel de erupcoes em volta dele (perto do chefe ou bem longe e seguro).
    /// Blink: some e reaparece num ponto avisado longe do heroi, com uma explosao no lugar.
    public enum BossAttack { Slam, Fan, Eruption, Charge, Ring, Blink }

    /// Regras do chefe sem Unity de cena: fases, rodizio de ataques e o escudo da ultima fase.
    public static class BossRules
    {
        public const int Phases = 3;
        /// Quanto cada golpe no escudo carrega a Quintessencia (a barra tem 100).
        public const float ShieldCharge = 4f;

        /// 0, 1 ou 2 conforme a vida restante (terços).
        public static int PhaseFor(float healthFraction) =>
            healthFraction > 2f / 3f ? 0 : healthFraction > 1f / 3f ? 1 : 2;

        static readonly BossAttack[][] cycles =
        {
            new[] { BossAttack.Slam, BossAttack.Slam, BossAttack.Fan },
            new[] { BossAttack.Slam, BossAttack.Charge, BossAttack.Fan, BossAttack.Eruption },
            new[] { BossAttack.Fan, BossAttack.Ring, BossAttack.Eruption, BossAttack.Charge, BossAttack.Slam, BossAttack.Eruption },
        };

        /// Tecela das Cinzas: luta de longe, se teletransporta e levanta aneis.
        static readonly BossAttack[][] weaver =
        {
            new[] { BossAttack.Fan, BossAttack.Eruption, BossAttack.Fan, BossAttack.Blink },
            new[] { BossAttack.Blink, BossAttack.Fan, BossAttack.Ring, BossAttack.Eruption },
            new[] { BossAttack.Blink, BossAttack.Ring, BossAttack.Fan, BossAttack.Eruption, BossAttack.Blink, BossAttack.Fan },
        };

        /// Ataque numero index da fase. Longe demais para a Pancada, troca pelo Leque.
        /// pattern: 0 = Guardiao das Cinzas, 1 = Tecela das Cinzas.
        public static BossAttack Next(int phase, int index, bool inSlamRange, int pattern = 0)
        {
            var set = pattern == 1 ? weaver : cycles;
            var cycle = set[Mathf.Clamp(phase, 0, set.Length - 1)];
            var a = cycle[((index % cycle.Length) + cycle.Length) % cycle.Length];
            return a == BossAttack.Slam && !inSlamRange ? BossAttack.Fan : a;
        }

        /// Pontos do anel: count circulos igualmente espacados a radius do centro.
        public static Vector3[] RingPoints(Vector3 center, float radius, int count, float startDegrees = 0f)
        {
            var pts = new Vector3[Math.Max(1, count)];
            for (int i = 0; i < pts.Length; i++)
                pts[i] = center + Quaternion.Euler(0f, startDegrees + 360f * i / pts.Length, 0f) * Vector3.forward * radius;
            return pts;
        }

        /// Direcoes do leque, simetricas em torno de forward.
        public static Vector3[] FanDirections(Vector3 forward, int count, float spreadDegrees)
        {
            var dirs = new Vector3[Math.Max(1, count)];
            for (int i = 0; i < dirs.Length; i++)
            {
                float angle = dirs.Length == 1 ? 0f : (i - (dirs.Length - 1) / 2f) * spreadDegrees;
                dirs[i] = Quaternion.Euler(0f, angle, 0f) * forward;
            }
            return dirs;
        }
    }

    /// Escudo da ultima fase: so cai com um golpe durante a Quintessencia.
    /// Se o heroi nao tem como ativa-la (menos de quatro elementos), cai sozinho por tempo: nunca vira beco sem saida.
    public class BossShield
    {
        readonly float timeout;
        float left;
        public bool Up { get; private set; }
        public bool NeedsQuintessence { get; private set; }

        public BossShield(float timeout) { this.timeout = timeout; }

        public void Raise(bool heroCanUseQuintessence)
        {
            Up = true;
            NeedsQuintessence = heroCanUseQuintessence;
            left = timeout;
        }

        /// Um golpe acertou o escudo. Devolve true se ele quebrou.
        public bool Hit(bool quintessenceActive)
        {
            if (!Up || !quintessenceActive) return false;
            Up = false;
            return true;
        }

        /// Devolve true se o escudo caiu por tempo neste tick.
        public bool Tick(float dt)
        {
            if (!Up || NeedsQuintessence) return false;
            left -= dt;
            if (left > 0f) return false;
            Up = false;
            return true;
        }
    }
}
