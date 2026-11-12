using System;

namespace Quintessence.Run
{
    public enum TrialKind { Clock, Unscathed, Grounded }

    /// Provacao: sala de luta com um desafio no letreiro. Cumprir da um talento extra e Essencia;
    /// falhar nao tranca a sala, so perde o premio.
    public static class Trials
    {
        public const int MaxHits = 3;
        public const int BonusEssence = 15;
        /// Golpes que chegam juntos (varios projeteis, tique de poca) contam como um so dentro desta janela.
        public const float HitWindow = 0.6f;

        public static TrialKind Pick(int seed, int nodeId)
        {
            unchecked
            {
                uint h = (uint)(seed * 31 + nodeId * 17);
                h ^= h >> 7; h *= 2654435761u; h ^= h >> 13;
                return (TrialKind)(h % 3);
            }
        }

        /// Tempo para limpar: 12 s mais 24 por onda (duas ondas: 60 s; tres: 84 s).
        public static float TimeLimit(int waves) => 12f + 24f * Math.Max(1, waves);

        public static string Name(TrialKind k) => k switch { TrialKind.Clock => "Contra o relógio", TrialKind.Unscathed => "Ileso", _ => "Pés no Chão" };

        public static string Goal(TrialKind k, float limit) => k switch
        {
            TrialKind.Clock => $"limpe a sala em {limit:0} segundos",
            TrialKind.Unscathed => $"limpe a sala tomando no máximo {MaxHits} golpes",
            _ => "limpe a sala sem usar o dash",
        };

        /// Ainda da para cumprir (o relogio nao estourou, os golpes nao passaram do limite).
        public static bool Holding(TrialKind k, float elapsed, int hits, float limit, int dashes = 0) => k switch
        {
            TrialKind.Clock => elapsed <= limit,
            TrialKind.Unscathed => hits <= MaxHits,
            _ => dashes == 0,
        };

        /// O que o letreiro da sala mostra durante a luta.
        public static string Status(TrialKind k, float elapsed, int hits, float limit, int dashes = 0)
        {
            if (!Holding(k, elapsed, hits, limit, dashes)) return "perdida";
            return k switch
            {
                TrialKind.Clock => $"{Math.Max(0f, limit - elapsed):0} s",
                TrialKind.Unscathed => $"golpes {hits}/{MaxHits}",
                _ => "sem dash",
            };
        }
    }
}
