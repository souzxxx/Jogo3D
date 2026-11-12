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

        [Test] public void BiomeChangesTheMix_ButNotTheHeadcount()
        {
            foreach (var type in new[] { RoomType.Combat, RoomType.Elite })
                for (int d = 0; d < 10; d++)
                {
                    int plain = EncounterPlanner.Plan(type, d).Sum(w => w.Total);
                    foreach (Biome b in System.Enum.GetValues(typeof(Biome)))
                        Assert.AreEqual(plain, EncounterPlanner.Plan(type, d, b).Sum(w => w.Total), $"{type} {d} {b}");
                }
            int Ranged(Biome b) => EncounterPlanner.Plan(RoomType.Combat, 4, b).Sum(w => w.Ranged + w.Lobber);
            int Heavy(Biome b) => EncounterPlanner.Plan(RoomType.Combat, 4, b).Sum(w => w.Brute + w.Charger);
            Assert.Greater(Ranged(Biome.Crypt), Ranged(Biome.Ashes), "a Cripta tem mais quem ataca de longe");
            Assert.Greater(Heavy(Biome.Garden), Heavy(Biome.Ashes), "o Jardim tem mais quem bate pesado");
        }

        [Test] public void ElementalsBelongToTheirBiome_AndSwarmsComeFromTheThirdRoom()
        {
            int Count(Biome b, int d, System.Func<EncounterPlanner.WavePlan, int> f) => EncounterPlanner.Plan(RoomType.Combat, d, b).Sum(f);
            Assert.Greater(Count(Biome.Ashes, 2, w => w.Ember), 0, "Brasa Viva nos Saloes de Cinza");
            Assert.AreEqual(0, Count(Biome.Crypt, 4, w => w.Ember));
            Assert.Greater(Count(Biome.Garden, 7, w => w.Sentinel), 0, "Sentinela no Jardim de Pedra");
            Assert.AreEqual(0, Count(Biome.Ashes, 2, w => w.Sentinel));
            Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Combat, 1).Sum(w => w.Swarm), "sem bando nas duas primeiras salas");
            Assert.AreEqual(3, EncounterPlanner.Plan(RoomType.Combat, 2).Sum(w => w.Swarm));
            foreach (Biome b in System.Enum.GetValues(typeof(Biome)))
                for (int d = 0; d < 10; d++)
                    foreach (var w in EncounterPlanner.Plan(RoomType.Combat, d, b))
                        Assert.IsTrue(w.Melee >= 0 && w.Ranged >= 0, "nenhuma contagem negativa");
        }

        [Test] public void Escudeiro_TakesTheSpotOfARastejante_FromTheFourthRoom()
        {
            Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Combat, EncounterPlanner.GuardDepth - 1).Sum(w => w.Guard), "nao aparece cedo");
            for (int d = EncounterPlanner.GuardDepth; d < 10; d++)
            {
                Assert.AreEqual(1, EncounterPlanner.Plan(RoomType.Combat, d)[0].Guard, "sala comum " + d);
                Assert.AreEqual(1, EncounterPlanner.Plan(RoomType.Elite, d)[0].Guard, "Elite " + d);
                Assert.AreEqual(1, EncounterPlanner.Plan(RoomType.Combat, d, Biome.Crypt).Sum(w => w.Guard), "o trecho nao tira o Escudeiro");
            }
            Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Boss, 9).Sum(w => w.Guard));
            var w3 = EncounterPlanner.Plan(RoomType.Combat, EncounterPlanner.GuardDepth)[0];
            Assert.GreaterOrEqual(w3.Melee, 1, "continua havendo Rastejante na onda");
        }

        [Test] public void Crypt_BringsTheDrowned_InPlaceOfAThrower()
        {
            bool seen = false;
            for (int d = 0; d < 10; d++)
            {
                var plain = EncounterPlanner.Plan(RoomType.Combat, d);
                var crypt = EncounterPlanner.Plan(RoomType.Combat, d, Biome.Crypt);
                Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Combat, d, Biome.Ashes).Sum(w => w.Drowned), "so na Cripta");
                Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Combat, d, Biome.Garden).Sum(w => w.Drowned));
                Assert.AreEqual(plain.Sum(w => w.Total), crypt.Sum(w => w.Total), "troca, nao soma");
                Assert.LessOrEqual(crypt.Sum(w => w.Drowned), 1);
                seen |= crypt.Sum(w => w.Drowned) > 0;
            }
            Assert.IsTrue(seen, "a Cripta tem Afogado em alguma profundidade");
        }
    }
}
