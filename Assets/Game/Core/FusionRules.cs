

namespace Quintessence.Core
{
    public enum FusionKind { None, Obsidian, Wildfire, Cyclone, Geyser }

    /// Fusao: a skill de um terceiro elemento lancada sobre a area que uma reacao deixou no chao.
    /// Magma (Fogo + Terra) e Vapor (Fogo + Agua) sao as reacoes que deixam area.
    public static class FusionRules
    {
        static bool Pair(ElementType a, ElementType b, ElementType x, ElementType y) => (a == x && b == y) || (a == y && b == x);

        public static FusionKind Find(ElementType a, ElementType b, ElementType third)
        {
            if (third == ElementType.None || third == a || third == b) return FusionKind.None;
            bool magma = Pair(a, b, ElementType.Fire, ElementType.Earth), vapor = Pair(a, b, ElementType.Fire, ElementType.Water);
            if (magma && third == ElementType.Water) return FusionKind.Obsidian;
            if (magma && third == ElementType.Air) return FusionKind.Wildfire;
            if (vapor && third == ElementType.Air) return FusionKind.Cyclone;
            if (vapor && third == ElementType.Earth) return FusionKind.Geyser;
            return FusionKind.None;
        }

        public static string Name(FusionKind k) => k switch
        {
            FusionKind.Obsidian => "Obsidiana", FusionKind.Wildfire => "Incêndio", FusionKind.Cyclone => "Ciclone", FusionKind.Geyser => "Gêiser", _ => "",
        };

        public static string Describe(FusionKind k) => k switch
        {
            FusionKind.Obsidian => "Magma + Água: a lava endurece e atordoa todos dentro",
            FusionKind.Wildfire => "Magma + Ar: o fogo se espalha numa área maior e queima",
            FusionKind.Cyclone => "Vapor + Ar: puxa todos para o centro e cega",
            FusionKind.Geyser => "Vapor + Terra: arremessa para fora e encharca",
            _ => "",
        };

        /// Numeros de cada fusao: dano, raio relativo a area, status e duracao, empurrao (negativo puxa para o centro).
        public static (float damage, float radiusMult, StatusType status, float statusTime, float knockback, ElementType element) Stats(FusionKind k) => k switch
        {
            FusionKind.Obsidian => (22f, 1.3f, StatusType.Stun, 1.8f, 0f, ElementType.Earth),
            FusionKind.Wildfire => (14f, 1.8f, StatusType.Burn, 4f, 3f, ElementType.Fire),
            FusionKind.Cyclone => (14f, 1.6f, StatusType.Blinded, 2.5f, -9f, ElementType.Air),
            FusionKind.Geyser => (20f, 1.4f, StatusType.Soaked, 4f, 12f, ElementType.Water),
            _ => (0f, 1f, StatusType.Burn, 0f, 0f, ElementType.None),
        };
    }
}
