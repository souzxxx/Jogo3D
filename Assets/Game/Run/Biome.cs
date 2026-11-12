using System;

namespace Quintessence.Run
{
    /// Os trechos da descida. Cada um muda a luz e a cor da corrupcao; o ultimo e a sala do chefe.
    public enum Biome { Ashes, Crypt, Garden, Heart }

    public static class Biomes
    {
        /// layer: camada da sala (0 = primeira). layers: total de camadas da run, contando a do chefe.
        public static Biome For(int layer, int layers)
        {
            if (layers <= 1 || layer >= layers - 1) return Biome.Heart;
            int span = layers - 1;
            return (Biome)Math.Min(2, Math.Max(0, layer) * 3 / span);
        }

        public static int Act(Biome b) => (int)b + 1;

        public static string Name(Biome b) => b switch
        {
            Biome.Ashes => "Salões de Cinza",
            Biome.Crypt => "Cripta Afogada",
            Biome.Garden => "Jardim de Pedra",
            _ => "Coração das Cinzas",
        };

        public static string Roman(int n) => n switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", _ => n.ToString() };
    }
}
