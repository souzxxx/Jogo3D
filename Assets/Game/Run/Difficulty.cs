using System;

namespace Quintessence.Run
{
    /// O que deixa uma sala de Elite diferente: os inimigos ganham um traco, alem de virem em tres ondas.
    public enum EliteModifier { None, Swift, Sturdy, Fierce, Volatile, Warded, Splitting, Thorny }

    /// Multiplicadores aplicados aos inimigos de uma sala. 1 = como esta na ficha do inimigo.
    public struct Difficulty
    {
        public float Health, Damage, Speed, Cooldown, Size;
        /// Ao morrer, deixa uma explosao avisada no chao.
        public bool Volatile;
        /// Golpes que cada inimigo bloqueia ao nascer.
        public int Wards;
        /// Ao cair, vira dois Cinzentos (Elite Divisores).
        public bool Splits;
        /// Fracao do golpe basico devolvida ao heroi (Elite Espinhosos).
        public float Thorns;
        public static Difficulty Base => new() { Health = 1f, Damage = 1f, Speed = 1f, Cooldown = 1f, Size = 1f };
    }

    public static class DifficultyCurve
    {
        public const float SturdyHealth = 1.35f, FierceDamage = 1.2f, SwiftSpeed = 1.2f, SwiftCooldown = 0.85f, ThornsFraction = 0.15f;

        /// Vida e dano dos inimigos crescem com a profundidade: a primeira sala usa a ficha do inimigo e cada camada
        /// soma 9% de vida e 6,5% de dano. O tempo de aviso dos golpes nunca muda.
        public const float HealthPerLayer = 0.09f, DamagePerLayer = 0.065f;
        /// As duas primeiras salas sao o aquecimento: o inimigo bate 75% e depois 87% do normal (quem esta aprendendo apanha mais).
        public static float EarlyDamage(int depth) => depth <= 0 ? 0.75f : depth == 1 ? 0.87f : 1f;

        public static Difficulty For(RoomType type, int depth, EliteModifier modifier = EliteModifier.None)
        {
            var d = Difficulty.Base;
            if (type != RoomType.Combat && type != RoomType.Elite) return d;
            depth = Math.Max(0, depth);
            d.Health = 1f + HealthPerLayer * depth;
            d.Damage = (1f + DamagePerLayer * depth) * EarlyDamage(depth);
            if (type != RoomType.Elite) return d;
            d.Size = 1.12f;
            switch (modifier)
            {
                case EliteModifier.Swift: d.Speed = SwiftSpeed; d.Cooldown = SwiftCooldown; break;
                case EliteModifier.Sturdy: d.Health *= SturdyHealth; break;
                case EliteModifier.Fierce: d.Damage *= FierceDamage; break;
                case EliteModifier.Volatile: d.Volatile = true; break;
                case EliteModifier.Warded: d.Wards = 1; d.Health *= 1.15f; break;
                case EliteModifier.Splitting: d.Splits = true; d.Health *= 0.85f; break;
                case EliteModifier.Thorny: d.Thorns = ThornsFraction; break;
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
                return (EliteModifier)(1 + h % 7);
            }
        }

        public static string Name(EliteModifier m) => m switch
        {
            EliteModifier.Swift => "Velozes",
            EliteModifier.Sturdy => "Resistentes",
            EliteModifier.Fierce => "Ferozes",
            EliteModifier.Volatile => "Voláteis",
            EliteModifier.Warded => "Protegidos",
            EliteModifier.Splitting => "Divisores",
            EliteModifier.Thorny => "Espinhosos",
            _ => "",
        };

        public static string Describe(EliteModifier m) => m switch
        {
            EliteModifier.Swift => "andam e atacam mais rápido",
            EliteModifier.Sturdy => "têm 35% mais vida",
            EliteModifier.Fierce => "causam 20% mais dano",
            EliteModifier.Volatile => "explodem ao morrer: saia do círculo",
            EliteModifier.Warded => "o primeiro golpe em cada um é bloqueado",
            EliteModifier.Splitting => "ao cair, cada um vira dois Cinzentos: área resolve",
            EliteModifier.Thorny => "devolvem 15% do golpe básico: use as skills",
            _ => "",
        };
    }
}
