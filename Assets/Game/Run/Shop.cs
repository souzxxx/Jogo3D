using System.Collections.Generic;
using Quintessence.Core;

namespace Quintessence.Run
{
    public enum ShopItemKind { Heal, MaxHealth, Boon }

    public class ShopItem
    {
        public ShopItemKind Kind;
        public Boon Boon;
        public int Price;
        public string Name;
        public string Description;
    }

    /// O que a Loja vende e por quanto. Precos sobem com a profundidade, junto com a essencia que as salas pagam.
    public static class ShopPlanner
    {
        public const float HealFraction = 0.4f;
        public const float MaxHealthGain = 15f;

        public static int HealPrice(int depth) => 18 + 2 * depth;
        public static int MaxHealthPrice(int depth) => 28 + 2 * depth;
        public static int BoonPrice(int depth) => 32 + 3 * depth;

        /// Sempre uma cura e um aumento de vida; mais ate duas bencaos que o heroi ainda nao tem.
        public static List<ShopItem> Stock(IReadOnlyList<Boon> catalog, ICollection<string> taken, IReadOnlyList<ElementType> awakened,
            System.Random rng, int depth)
        {
            var items = new List<ShopItem>
            {
                new() { Kind = ShopItemKind.Heal, Price = HealPrice(depth), Name = "Poção", Description = "Cura 40% da vida" },
                new() { Kind = ShopItemKind.MaxHealth, Price = MaxHealthPrice(depth), Name = "Elixir", Description = "+15 de vida máxima" },
            };
            foreach (var b in BoonPicker.Offer(catalog, taken, awakened, rng, 2))
                items.Add(new ShopItem { Kind = ShopItemKind.Boon, Boon = b, Price = BoonPrice(depth), Name = b.name, Description = b.description });
            return items;
        }
    }
}
