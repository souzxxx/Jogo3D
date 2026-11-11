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
    }

    /// Confere os assets de layout de verdade: nada bloqueia a entrada, as portas ou sai da sala.
    public class LayoutAssetTests
    {
        static List<RoomLayout> All() => AssetDatabase.FindAssets("t:RoomLayout", new[] { "Assets/Game/Game/Layouts" })
            .Select(g => AssetDatabase.LoadAssetAtPath<RoomLayout>(AssetDatabase.GUIDToAssetPath(g))).ToList();

        [Test] public void ThereAreAtLeastSixLayouts() => Assert.GreaterOrEqual(All().Count, 6);

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
                    var carve = p.prefab.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                    Assert.IsNotNull(carve, p.prefab.name + " sem NavMeshObstacle: os inimigos atravessariam");
                    Assert.IsTrue(carve.carving);
                    Assert.IsNotNull(p.prefab.GetComponent<Collider>(), p.prefab.name + " sem colisor");
                }
        }
    }
}
