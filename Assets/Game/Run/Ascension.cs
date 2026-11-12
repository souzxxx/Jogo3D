using System;

namespace Quintessence.Run
{
    /// Niveis de desafio para depois da primeira vitoria: cada vitoria libera mais um.
    /// Cada nivel deixa os inimigos mais fortes e paga mais Fragmentos.
    public static class Ascension
    {
        public const int Max = 5;
        public static int Unlocked(int wins) => Math.Min(Max, Math.Max(0, wins));
        public static int Clamp(int level, int wins) => Math.Min(Unlocked(wins), Math.Max(0, level));
        public static float HealthMult(int level) => 1f + 0.12f * Math.Max(0, level);
        public static float DamageMult(int level) => 1f + 0.1f * Math.Max(0, level);
        public static float ShardMult(int level) => 1f + 0.25f * Math.Max(0, level);
        public static string Describe(int level) => level <= 0
            ? "Sem ascensão: o jogo como ele é"
            : $"Inimigos com +{(HealthMult(level) - 1f) * 100f:0}% de vida e +{(DamageMult(level) - 1f) * 100f:0}% de dano · +{(ShardMult(level) - 1f) * 100f:0}% de Fragmentos";
    }
}
