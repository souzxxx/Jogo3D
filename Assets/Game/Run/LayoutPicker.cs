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

        public static bool UsesLayout(RoomType type) => type == RoomType.Combat || type == RoomType.Elite || type == RoomType.Trial;
    }
}
