using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Quintessence.Run.Tests
{
    public class RunMapTests
    {
        static IEnumerable<List<MapNode>> Paths(RunMap map)
        {
            var stack = new Stack<List<MapNode>>();
            stack.Push(new List<MapNode> { map.Start });
            while (stack.Count > 0)
            {
                var path = stack.Pop();
                var last = path[^1];
                if (last.Next.Count == 0) { yield return path; continue; }
                foreach (int id in last.Next) stack.Push(new List<MapNode>(path) { map.Get(id) });
            }
        }

        [Test] public void StartsWithCombatAndEndsWithBoss([Range(1, 40)] int seed)
        {
            var map = RunMapGenerator.Generate(seed);
            Assert.AreEqual(RoomType.Combat, map.Start.Type);
            var last = map.Layer(map.LayerCount - 1);
            Assert.AreEqual(1, last.Count);
            Assert.AreEqual(RoomType.Boss, last[0].Type);
        }

        [Test] public void NoDeadEnds_EveryPathReachesTheBoss([Range(1, 40)] int seed)
        {
            var map = RunMapGenerator.Generate(seed);
            foreach (var path in Paths(map))
            {
                Assert.AreEqual(RoomType.Boss, path[^1].Type, "caminho sem saida");
                Assert.AreEqual(map.LayerCount, path.Count, "todo caminho atravessa todas as camadas");
            }
        }

        [Test] public void EveryRoomIsReachable([Range(1, 40)] int seed)
        {
            var map = RunMapGenerator.Generate(seed);
            var reached = new HashSet<int>(Paths(map).SelectMany(p => p).Select(n => n.Id));
            Assert.AreEqual(map.Nodes.Count, reached.Count);
        }

        [Test] public void EveryPathHasThreeShrines([Range(1, 40)] int seed)
        {
            var map = RunMapGenerator.Generate(seed);
            foreach (var path in Paths(map))
                Assert.AreEqual(3, path.Count(n => n.Type == RoomType.Shrine), "comeca com 1 elemento, termina com 4");
        }

        [Test] public void SameSeedSameMap()
        {
            var a = RunMapGenerator.Generate(7);
            var b = RunMapGenerator.Generate(7);
            Assert.AreEqual(a.Nodes.Count, b.Nodes.Count);
            for (int i = 0; i < a.Nodes.Count; i++)
            {
                Assert.AreEqual(a.Nodes[i].Type, b.Nodes[i].Type);
                CollectionAssert.AreEqual(a.Nodes[i].Next, b.Nodes[i].Next);
            }
        }

        [Test] public void ThereIsARealChoice([Range(1, 40)] int seed)
        {
            var map = RunMapGenerator.Generate(seed);
            Assert.Greater(Paths(map).Count(), 1);
        }

        [Test] public void ShortRunStillValid()
        {
            var map = RunMapGenerator.Generate(3, 5, 1);
            foreach (var path in Paths(map))
            {
                Assert.AreEqual(5, path.Count);
                Assert.AreEqual(1, path.Count(n => n.Type == RoomType.Shrine));
            }
        }
    }

    public class RunStateTests
    {
        [Test] public void CanOnlyAdvanceToConnectedRooms()
        {
            var map = RunMapGenerator.Generate(5);
            var run = new RunState(map);
            int far = map.Layer(3)[0].Id;
            Assert.IsFalse(run.Advance(far));
            int next = run.Choices[0].Id;
            Assert.IsTrue(run.Advance(next));
            Assert.AreEqual(1, run.Depth);
            Assert.IsTrue(run.Visited.Contains(next));
        }

        [Test] public void WalkingAnyChoicesReachesBoss()
        {
            var run = new RunState(RunMapGenerator.Generate(11));
            int guard = 0;
            while (!run.AtBoss && guard++ < 50) Assert.IsTrue(run.Advance(run.Choices[^1].Id));
            Assert.IsTrue(run.AtBoss);
            Assert.AreEqual(0, run.Choices.Count);
        }

        [Test] public void EssenceNeverGoesDown()
        {
            var run = new RunState(RunMapGenerator.Generate(1));
            run.AddEssence(10); run.AddEssence(-50);
            Assert.AreEqual(10, run.Essence);
        }
    }

    public class EncounterPlannerTests
    {
        static int Total(RoomType t, int d) => EncounterPlanner.Plan(t, d).Sum(w => w.Total);

        [Test] public void DeeperRoomsAreNeverEasier()
        {
            for (int d = 0; d < 7; d++)
            {
                Assert.GreaterOrEqual(Total(RoomType.Combat, d + 1), Total(RoomType.Combat, d));
                Assert.GreaterOrEqual(Total(RoomType.Elite, d + 1), Total(RoomType.Elite, d));
            }
        }

        [Test] public void EliteIsHarderAndPaysMore()
        {
            for (int d = 3; d < 7; d++)
            {
                Assert.Greater(Total(RoomType.Elite, d), Total(RoomType.Combat, d));
                Assert.Greater(EncounterPlanner.EssenceReward(RoomType.Elite, d), EncounterPlanner.EssenceReward(RoomType.Combat, d));
            }
        }

        [Test] public void NonCombatRoomsHaveNoWaves()
        {
            Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Shrine, 3).Count);
            Assert.AreEqual(0, EncounterPlanner.Plan(RoomType.Rest, 3).Count);
        }

        [Test] public void NoEmptyWaves()
        {
            foreach (RoomType t in new[] { RoomType.Combat, RoomType.Elite, RoomType.Boss })
                for (int d = 0; d < 8; d++)
                    foreach (var w in EncounterPlanner.Plan(t, d)) Assert.Greater(w.Total, 0);
        }
    }
}
