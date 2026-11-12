using System;

namespace Quintessence.Run
{
    /// O que deixa uma sala de Elite diferente: os inimigos ganham um traco, alem de virem em tres ondas.
    public enum EliteModifier { None, Swift, Sturdy, Fierce }

    /// Multiplicadores aplicados aos inimigos de uma sala. 1 = como esta na ficha do inimigo.
    public struct Difficulty
    {
        public float Health, Damage, Speed, Cooldown, Size;
        public static Difficulty Base => new() { Health = 1f, Damage = 1f, Speed = 1f, Cooldown = 1f, Size = 1f };
    }

    public static class DifficultyCurve
    {
        /// Vida e dano dos inimigos crescem com a profundidade: a primeira sala usa a ficha do inimigo e cada camada
        /// soma 9% de vida e 6,5% de dano. O tempo de aviso dos golpes nunca muda.
        public const float HealthPerLayer = 0.09f, DamagePerLayer = 0.065f;

        public static Difficulty For(RoomType type, int depth, EliteModifier modifier = EliteModifier.None)
        {
            var d = Difficulty.Base;
            if (type != RoomType.Combat && type != RoomType.Elite) return d;
            depth = Math.Max(0, depth);
            d.Health = 1f + HealthPerLayer * depth;
            d.Damage = 1f + DamagePerLayer * depth;
            if (type != RoomType.Elite) return d;
            d.Size = 1.12f;
            switch (modifier)
            {
                case EliteModifier.Swift: d.Speed = 1.25f; d.Cooldown = 0.8f; break;
                case EliteModifier.Sturdy: d.Health *= 1.5f; break;
                case EliteModifier.Fierce: d.Damage *= 1.3f; break;
            }
            return d;
        }

        /// O traco de uma sala de Elite e fixo para a semente e a sala: a porta ja pode anunciar o que vem.
        public static EliteModifier PickElite(int seed, int nodeId)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u ^ (uint)(nodeId + 1) * 40503u;
                h ^= h >> 13; h *= 2246822519u; h ^= h >> 16;
                return (EliteModifier)(1 + h % 3);
            }
        }

        public static string Name(EliteModifier m) => m switch
        {
            EliteModifier.Swift => "Velozes",
            EliteModifier.Sturdy => "Resistentes",
            EliteModifier.Fierce => "Ferozes",
            _ => "",
        };

        public static string Describe(EliteModifier m) => m switch
        {
            EliteModifier.Swift => "andam e atacam mais rápido",
            EliteModifier.Sturdy => "têm 50% mais vida",
            EliteModifier.Fierce => "causam 30% mais dano",
            _ => "",
        };
    }
}
