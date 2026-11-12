using System.Linq;
using NUnit.Framework;

namespace Quintessence.Run.Tests
{
    public class BiomeTests
    {
        [Test] public void RunStartsInAshes_AndEndsInTheHeart([Values(5, 8, 10, 12)] int layers)
        {
            Assert.AreEqual(Biome.Ashes, Biomes.For(0, layers));
            Assert.AreEqual(Biome.Heart, Biomes.For(layers - 1, layers));
        }

        [Test] public void BiomesNeverGoBackwards([Values(5, 8, 10, 12)] int layers)
        {
            for (int l = 1; l < layers; l++) Assert.GreaterOrEqual((int)Biomes.For(l, layers), (int)Biomes.For(l - 1, layers));
        }

        [Test] public void ALongRunCrossesEveryBiome([Values(8, 10, 12)] int layers)
        {
            var seen = Enumerable.Range(0, layers).Select(l => Biomes.For(l, layers)).Distinct().ToList();
            Assert.AreEqual(4, seen.Count);
        }

        [Test] public void EveryBiomeHasAName()
        {
            foreach (Biome b in System.Enum.GetValues(typeof(Biome))) Assert.IsNotEmpty(Biomes.Name(b));
            Assert.AreEqual("III", Biomes.Roman(3));
        }

        [Test] public void NewRoomTypes_ShowUpInSomeMaps_AndHaveNames()
        {
            bool treasure = false, pact = false;
            for (int seed = 1; seed <= 60; seed++)
                foreach (var n in RunMapGenerator.Generate(seed, 10, 3).Nodes)
                {
                    treasure |= n.Type == RoomType.Treasure;
                    pact |= n.Type == RoomType.Pact;
                }
            Assert.IsTrue(treasure, "nenhum mapa com Tesouro");
            Assert.IsTrue(pact, "nenhum mapa com Pacto");
            Assert.AreEqual("Tesouro", RoomInfo.Name(RoomType.Treasure));
            Assert.AreEqual("Pacto", RoomInfo.Name(RoomType.Pact));
            Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Treasure, 3).Count);
            Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Pact, 3).Count);
        }
    }
}
