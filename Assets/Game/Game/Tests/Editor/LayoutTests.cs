using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Quintessence.Run;
using UnityEditor;
using UnityEngine;

namespace Quintessence.Game.Tests
{
    public class LayoutPickerTests
    {
        [Test] public void SameSeedAndRoomGiveSameLayout()
        {
            for (int node = 0; node < 20; node++)
                Assert.AreEqual(LayoutPicker.Pick(1234, node, 6), LayoutPicker.Pick(1234, node, 6));
        }

        [Test] public void AlwaysInRange([NUnit.Framework.Range(1, 8)] int count)
        {
            for (int seed = 1; seed < 200; seed += 7)
                for (int node = 0; node < 30; node++)
                    Assert.That(LayoutPicker.Pick(seed, node, count), Is.InRange(0, count - 1));
        }

        [Test] public void NoLayoutsMeansNone() => Assert.AreEqual(-1, LayoutPicker.Pick(5, 3, 0));

        [Test] public void ARunSeesSeveralDifferentLayouts()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var seen = new HashSet<int>();
                for (int node = 0; node < 16; node++) seen.Add(LayoutPicker.Pick(seed, node, 6));
                Assert.GreaterOrEqual(seen.Count, 3, "semente " + seed + " repete demais");
            }
        }

        [Test] public void OnlyCombatRoomsUseLayouts()
        {
            Assert.IsTrue(LayoutPicker.UsesLayout(RoomType.Combat));
            Assert.IsTrue(LayoutPicker.UsesLayout(RoomType.Elite));
            Assert.IsFalse(LayoutPicker.UsesLayout(RoomType.Boss));
            Assert.IsFalse(LayoutPicker.UsesLayout(RoomType.Shrine));
            Assert.IsFalse(LayoutPicker.UsesLayout(RoomType.Master));
        }

        [Test] public void WeightedPick_RespectsZeroAndProportion()
        {
            Assert.AreEqual(-1, LayoutPicker.PickWeighted(1, 1, new[] { 0, 0 }));
            Assert.AreEqual(-1, LayoutPicker.PickWeighted(1, 1, new int[0]));
            var weights = new[] { 1, 0, 3, 0 };
            var count = new int[4];
            for (int seed = 0; seed < 400; seed++)
                for (int node = 0; node < 10; node++)
                {
                    int i = LayoutPicker.PickWeighted(seed, node, weights);
                    Assert.AreEqual(i, LayoutPicker.PickWeighted(seed, node, weights), "deterministico");
                    count[i]++;
                }
            Assert.AreEqual(0, count[1]); Assert.AreEqual(0, count[3], "peso zero nunca sai");
            float ratio = count[2] / (float)count[0];
            Assert.That(ratio, Is.InRange(2.4f, 3.7f), "peso 3 sai cerca de tres vezes mais que peso 1");
        }
    }

    /// Confere os assets de layout de verdade: nada bloqueia a entrada, as portas ou sai da sala.
    public class LayoutAssetTests
    {
        static List<RoomLayout> All() => AssetDatabase.FindAssets("t:RoomLayout", new[] { "Assets/Game/Game/Layouts" })
            .Select(g => AssetDatabase.LoadAssetAtPath<RoomLayout>(AssetDatabase.GUIDToAssetPath(g))).ToList();

        [Test] public void ThereAreAtLeastSixLayouts() => Assert.GreaterOrEqual(All().Count, 6);

        [Test] public void EveryStretchHasLayoutsOfItsOwn_ThatStayOutOfTheOthers()
        {
            var all = All();
            foreach (var b in new[] { Biome.Ashes, Biome.Crypt, Biome.Garden })
            {
                var home = all.Where(l => l.biomes != null && l.biomes.Contains(b)).ToList();
                Assert.GreaterOrEqual(home.Count, 2, Biomes.Name(b) + " tem pelo menos dois arranjos proprios");
                foreach (var l in home)
                {
                    Assert.AreEqual(3, l.WeightFor(b), l.displayName);
                    foreach (var other in new[] { Biome.Ashes, Biome.Crypt, Biome.Garden }.Where(o => !l.biomes.Contains(o)))
                        Assert.AreEqual(0, l.WeightFor(other), l.displayName + " fora de " + Biomes.Name(other));
                }
            }
            Assert.GreaterOrEqual(all.Count(l => l.biomes == null || l.biomes.Count == 0), 20, "a maioria vale para qualquer trecho");
            Assert.AreEqual(1, all.First(l => l.biomes == null || l.biomes.Count == 0).WeightFor(Biome.Crypt));
        }

        [Test] public void EveryLayoutHasNamedPiecesWithPrefabs()
        {
            foreach (var l in All())
            {
                Assert.IsNotEmpty(l.displayName);
                Assert.GreaterOrEqual(l.pieces.Count, 3, l.displayName);
                foreach (var p in l.pieces) Assert.IsNotNull(p.prefab, l.displayName + " tem peca sem prefab");
            }
        }

        [Test] public void NothingBlocksTheEntranceTheDoorsOrLeavesTheRoom()
        {
            var entry = new Vector3(0, 0, -5.5f);
            foreach (var l in All())
                foreach (var p in l.pieces)
                {
                    string where = $"{l.displayName}: {p.prefab.name} em {p.position}";
                    Assert.Greater(Vector3.Distance(p.position, entry), 3f, where + " esta em cima da entrada");
                    Assert.Less(p.position.z, 4.2f, where + " esta na faixa das portas");
                    Assert.Less(Mathf.Abs(p.position.x), 8.6f, where + " esta fora da sala");
                    Assert.Greater(p.position.z, -6.8f, where + " esta fora da sala");
                }
        }

        [Test] public void CoverPiecesCarveTheNavMesh()
        {
            foreach (var l in All())
                foreach (var p in l.pieces)
                {
                    if (p.prefab.GetComponent<Quintessence.Enemies.EmberVent>() != null) continue;
                    if (p.prefab.GetComponent<Quintessence.Enemies.SpikeTrap>() != null) continue; // armadilha no chao: anda-se por cima
                    if (p.prefab.GetComponent<CorruptionPool>() != null) continue; // poca no chao: inimigo atravessa e se queima
                    var carve = p.prefab.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                    Assert.IsNotNull(carve, p.prefab.name + " sem NavMeshObstacle: os inimigos atravessariam");
                    Assert.IsTrue(carve.carving);
                    Assert.IsNotNull(p.prefab.GetComponent<Collider>(), p.prefab.name + " sem colisor");
                }
        }

        [Test] public void SomeLayoutsAreSafe_ForTheFirstRoom()
        {
            var all = All();
            Assert.GreaterOrEqual(all.Count(l => !l.HasHazard), 4, "a primeira sala precisa de opcoes sem perigo");
            Assert.GreaterOrEqual(all.Count(l => l.HasHazard), 4);
        }
    }
}
