using System;

namespace Quintessence.Run
{
    public enum TrialKind { Clock, Unscathed }

    /// Provacao: sala de luta com um desafio no letreiro. Cumprir da um talento extra e Essencia;
    /// falhar nao tranca a sala, so perde o premio.
    public static class Trials
    {
        public const int MaxHits = 3;
        public const int BonusEssence = 15;
        /// Golpes que chegam juntos (varios projeteis, tique de poca) contam como um so dentro desta janela.
        public const float HitWindow = 0.6f;

        public static TrialKind Pick(int seed, int nodeId) => (((seed * 31) + nodeId * 17) & 1) == 0 ? TrialKind.Clock : TrialKind.Unscathed;

        /// Tempo para limpar: 12 s mais 24 por onda (duas ondas: 60 s; tres: 84 s).
        public static float TimeLimit(int waves) => 12f + 24f * Math.Max(1, waves);

        public static string Name(TrialKind k) => k == TrialKind.Clock ? "Contra o relógio" : "Ileso";

        public static string Goal(TrialKind k, float limit) => k == TrialKind.Clock
            ? $"limpe a sala em {limit:0} segundos"
            : $"limpe a sala tomando no máximo {MaxHits} golpes";

        /// Ainda da para cumprir (o relogio nao estourou, os golpes nao passaram do limite).
        public static bool Holding(TrialKind k, float elapsed, int hits, float limit) =>
            k == TrialKind.Clock ? elapsed <= limit : hits <= MaxHits;

        /// O que o letreiro da sala mostra durante a luta.
        public static string Status(TrialKind k, float elapsed, int hits, float limit)
        {
            if (!Holding(k, elapsed, hits, limit)) return "perdida";
            return k == TrialKind.Clock ? $"{Math.Max(0f, limit - elapsed):0} s" : $"golpes {hits}/{MaxHits}";
        }
    }
}
