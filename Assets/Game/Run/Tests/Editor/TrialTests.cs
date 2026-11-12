using System.Linq;
using NUnit.Framework;

namespace Quintessence.Run.Tests
{
    public class TrialTests
    {
        [Test] public void Rules_ClockAndUnscathed()
        {
            Assert.AreEqual(60f, Trials.TimeLimit(2), 0.01f);
            Assert.AreEqual(84f, Trials.TimeLimit(3), 0.01f);
            Assert.AreEqual(36f, Trials.TimeLimit(0), 0.01f, "sala sem onda conta como uma");
            Assert.IsTrue(Trials.Holding(TrialKind.Clock, 59.9f, 50, 60f), "no relogio os golpes nao contam");
            Assert.IsFalse(Trials.Holding(TrialKind.Clock, 60.1f, 0, 60f));
            Assert.IsTrue(Trials.Holding(TrialKind.Unscathed, 999f, Trials.MaxHits, 60f), "ileso nao tem relogio");
            Assert.IsFalse(Trials.Holding(TrialKind.Unscathed, 1f, Trials.MaxHits + 1, 60f));
            Assert.AreEqual("42 s", Trials.Status(TrialKind.Clock, 17.6f, 0, 60f));
            Assert.AreEqual("golpes 1/3", Trials.Status(TrialKind.Unscathed, 0f, 1, 60f));
            Assert.AreEqual("perdida", Trials.Status(TrialKind.Unscathed, 0f, 4, 60f));
            Assert.IsTrue(Trials.Holding(TrialKind.Grounded, 999f, 99, 60f, 0), "Pés no Chão: tempo e golpes não contam");
            Assert.IsFalse(Trials.Holding(TrialKind.Grounded, 1f, 0, 60f, 1), "um dash e a Provação se perde");
            Assert.AreEqual("sem dash", Trials.Status(TrialKind.Grounded, 0f, 0, 60f, 0));
            foreach (TrialKind k in System.Enum.GetValues(typeof(TrialKind)))
            {
                Assert.IsNotEmpty(Trials.Name(k));
                Assert.IsNotEmpty(Trials.Goal(k, 60f));
            }
        }

        [Test] public void Pick_IsDeterministic_AndUsesBothKinds()
        {
            var kinds = Enumerable.Range(0, 20).Select(id => Trials.Pick(7, id)).ToList();
            Assert.AreEqual(kinds, Enumerable.Range(0, 20).Select(id => Trials.Pick(7, id)).ToList());
            Assert.IsTrue(kinds.Contains(TrialKind.Clock) && kinds.Contains(TrialKind.Unscathed) && kinds.Contains(TrialKind.Grounded), "os tres desafios saem");
        }

        [Test] public void Map_PlacesTrials_MidRun_AtMostOnePerLayer()
        {
            int total = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var map = RunMapGenerator.Generate(seed, 10, 3);
                foreach (var n in map.Nodes.Where(n => n.Type == RoomType.Trial))
                {
                    total++;
                    Assert.GreaterOrEqual(n.Layer, 2, "nao aparece logo no comeco");
                    Assert.Less(n.Layer, map.LayerCount - 1);
                    Assert.AreEqual(RewardType.Essence, n.Reward);
                }
                foreach (var layer in map.Nodes.GroupBy(n => n.Layer))
                    Assert.LessOrEqual(layer.Count(n => n.Type == RoomType.Trial), 1, $"semente {seed}, camada {layer.Key}");
            }
            Assert.Greater(total, 60, "aparece com frequencia razoavel (cerca de uma a cada run ou duas)");
            Assert.AreEqual("Provação", RoomInfo.Name(RoomType.Trial));
            Assert.IsTrue(LayoutPicker.UsesLayout(RoomType.Trial), "a Provacao e uma sala de luta com arranjo");
            Assert.AreEqual(EncounterPlanner.EssenceReward(RoomType.Combat, 4), EncounterPlanner.EssenceReward(RoomType.Trial, 4));
        }
    }
}
