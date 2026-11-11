namespace Quintessence.Core
{
    /// Os quatro elementos. None = golpe sem elemento (nao marca, nao reage).
    public enum ElementType { None = 0, Fire = 1, Water = 2, Earth = 3, Air = 4 }

    public enum Team { Player, Enemy }

    public static class Elements
    {
        public static readonly ElementType[] All = { ElementType.Fire, ElementType.Water, ElementType.Earth, ElementType.Air };
    }
}
