namespace Quintessence.Run
{
    /// Escolhe o layout de cada sala de combate. A mesma semente e a mesma sala dao sempre o mesmo layout.
    public static class LayoutPicker
    {
        public static int Pick(int seed, int nodeId, int count)
        {
            if (count <= 0) return -1;
            unchecked
            {
                uint h = (uint)seed * 73856093u ^ (uint)(nodeId + 1) * 19349663u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return (int)(h % (uint)count);
            }
        }

        /// Sorteio com peso (0 = fora). A mesma semente e a mesma sala dao sempre o mesmo indice. -1 se nada tem peso.
        public static int PickWeighted(int seed, int nodeId, System.Collections.Generic.IReadOnlyList<int> weights)
        {
            int total = 0;
            foreach (int w in weights) total += System.Math.Max(0, w);
            if (total <= 0) return -1;
            int roll = Pick(seed, nodeId, total);
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= System.Math.Max(0, weights[i]);
                if (roll < 0) return i;
            }
            return weights.Count - 1;
        }

        public static bool UsesLayout(RoomType type) => type == RoomType.Combat || type == RoomType.Elite || type == RoomType.Trial || type == RoomType.Siege;
    }
}
