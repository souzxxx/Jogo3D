using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Run.Tests
{
    public class ShopTests
    {
        static List<Boon> Catalog()
        {
            var c = ScriptableObject.CreateInstance<BoonCatalog>();
            c.FillDefaults();
            return c.boons;
        }

        [Test] public void AlwaysSellsHealAndMaxHealth_PlusUpToTwoBoons([NUnit.Framework.Range(1, 20)] int seed)
        {
            var stock = ShopPlanner.Stock(Catalog(), new HashSet<string>(), new[] { ElementType.Fire }, new System.Random(seed), 3);
            Assert.AreEqual(1, stock.Count(i => i.Kind == ShopItemKind.Heal));
            Assert.AreEqual(1, stock.Count(i => i.Kind == ShopItemKind.MaxHealth));
            Assert.AreEqual(2, stock.Count(i => i.Kind == ShopItemKind.Boon));
            foreach (var i in stock) { Assert.Greater(i.Price, 0); Assert.IsNotEmpty(i.Name); Assert.IsNotEmpty(i.Description); }
        }

        [Test] public void NeverSellsABoonTheHeroAlreadyHas()
        {
            var catalog = Catalog();
            var taken = new HashSet<string>(catalog.Select(b => b.id));
            var stock = ShopPlanner.Stock(catalog, taken, Elements.All, new System.Random(1), 2);
            Assert.AreEqual(0, stock.Count(i => i.Kind == ShopItemKind.Boon));
            Assert.AreEqual(2, stock.Count, "sem bencao sobrando, ainda vende cura e vida");
        }

        [Test] public void PricesGrowWithDepth()
        {
            for (int d = 0; d < 7; d++)
            {
                Assert.Greater(ShopPlanner.HealPrice(d + 1), ShopPlanner.HealPrice(d));
                Assert.Greater(ShopPlanner.BoonPrice(d + 1), ShopPlanner.BoonPrice(d));
            }
        }

        [Test] public void AShopIsAffordableByTheTimeItAppears()
        {
            // a Loja aparece da camada 2 em diante; ate la o jogador limpou pelo menos uma sala de combate
            int earned = EncounterPlanner.EssenceReward(RoomType.Combat, 0) + EncounterPlanner.EssenceReward(RoomType.Combat, 1);
            Assert.GreaterOrEqual(earned, ShopPlanner.HealPrice(2), "com duas salas de combate da para comprar ao menos a pocao");
        }

        [Test] public void SpendingNeverGoesNegative()
        {
            var run = new RunState(RunMapGenerator.Generate(1));
            run.AddEssence(30);
            Assert.IsFalse(run.TrySpend(31));
            Assert.AreEqual(30, run.Essence);
            Assert.IsTrue(run.TrySpend(30));
            Assert.AreEqual(0, run.Essence);
            Assert.IsFalse(run.TrySpend(-5));
        }

        [Test] public void ShopsOnlyAppearFromTheThirdRoomOn([NUnit.Framework.Range(1, 40)] int seed)
        {
            var map = RunMapGenerator.Generate(seed);
            foreach (var n in map.Nodes.Where(n => n.Type == RoomType.Shop)) Assert.GreaterOrEqual(n.Layer, 2);
        }

        [Test] public void SomeRunsHaveAShop()
        {
            int with = Enumerable.Range(1, 60).Count(s => RunMapGenerator.Generate(s).Nodes.Any(n => n.Type == RoomType.Shop));
            Assert.Greater(with, 15, "loja rara demais");
        }
    }
}
