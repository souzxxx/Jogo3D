using NUnit.Framework;
using Quintessence.Core;

namespace Quintessence.Run.Tests
{
    public class MetaTests
    {
        [Test]
        public void New_Progress_Is_Empty_And_Buys_Nothing()
        {
            var p = new MetaProgress();
            Assert.AreEqual(0, p.Shards);
            foreach (MetaUpgrade u in System.Enum.GetValues(typeof(MetaUpgrade)))
            {
                Assert.AreEqual(0, p.Level(u));
                Assert.IsFalse(p.TryBuy(u), "sem Fragmentos nao compra");
            }
        }

        [Test]
        public void Run_Reward_Counts_Rooms_And_Victory()
        {
            Assert.AreEqual(0, MetaProgress.Reward(0, false));
            Assert.AreEqual(4 * MetaProgress.ShardsPerRoom, MetaProgress.Reward(4, false));
            Assert.AreEqual(8 * MetaProgress.ShardsPerRoom + MetaProgress.ShardsForVictory, MetaProgress.Reward(8, true));
            Assert.AreEqual(0, MetaProgress.Reward(-3, false));
        }

        [Test]
        public void EndRun_Adds_Shards_And_Tracks_Records()
        {
            var p = new MetaProgress();
            Assert.AreEqual(12, p.EndRun(4, false));
            p.EndRun(2, false);
            p.EndRun(8, true);
            Assert.AreEqual(3, p.Runs);
            Assert.AreEqual(1, p.Wins);
            Assert.AreEqual(8, p.BestRooms);
            Assert.AreEqual(12 + 6 + 39, p.Shards);
        }

        [Test]
        public void Buying_Spends_Raises_Level_And_Gets_Pricier()
        {
            var p = new MetaProgress();
            p.AddShards(100);
            int first = p.Cost(MetaUpgrade.Vigor);
            Assert.IsTrue(p.TryBuy(MetaUpgrade.Vigor));
            Assert.AreEqual(100 - first, p.Shards);
            Assert.AreEqual(1, p.Level(MetaUpgrade.Vigor));
            Assert.Greater(p.Cost(MetaUpgrade.Vigor), first);
        }

        [Test]
        public void Cannot_Pass_Max_Level_Or_Go_Negative()
        {
            var p = new MetaProgress();
            p.AddShards(100000);
            for (int i = 0; i < 20; i++) p.TryBuy(MetaUpgrade.Purse);
            Assert.AreEqual(MetaInfo.MaxLevel(MetaUpgrade.Purse), p.Level(MetaUpgrade.Purse));
            Assert.IsTrue(p.Maxed(MetaUpgrade.Purse));
            var poor = new MetaProgress();
            poor.AddShards(MetaInfo.Cost(MetaUpgrade.Power, 0) - 1);
            Assert.IsFalse(poor.TryBuy(MetaUpgrade.Power));
            Assert.GreaterOrEqual(poor.Shards, 0);
            poor.AddShards(-50);
            Assert.GreaterOrEqual(poor.Shards, 0);
        }

        [Test]
        public void Upgrades_Change_The_Hero_At_Run_Start()
        {
            var p = new MetaProgress();
            p.AddShards(100000);
            p.TryBuy(MetaUpgrade.Vigor); p.TryBuy(MetaUpgrade.Vigor);
            p.TryBuy(MetaUpgrade.Power);
            p.TryBuy(MetaUpgrade.Purse);
            p.TryBuy(MetaUpgrade.Focus);
            p.TryBuy(MetaUpgrade.Recovery);
            var m = new HeroModifiers();
            p.ApplyTo(m);
            Assert.AreEqual(16f, m.MaxHealthAdd, 0.001f);
            Assert.AreEqual(1.04f, m.DamageAll, 0.001f);
            Assert.AreEqual(1.1f, m.QuintessenceGainMult, 0.001f);
            Assert.AreEqual(0.03f, m.HealOnRoomClear, 0.001f);
            Assert.AreEqual(8, p.StartEssence);
        }

        [Test]
        public void Save_Round_Trips()
        {
            var p = new MetaProgress();
            p.EndRun(8, true); p.EndRun(3, false);
            p.TryBuy(MetaUpgrade.Vigor); p.TryBuy(MetaUpgrade.Purse);
            var q = MetaProgress.Parse(p.Serialize());
            Assert.AreEqual(p.Serialize(), q.Serialize());
            Assert.AreEqual(p.Shards, q.Shards);
            Assert.AreEqual(1, q.Level(MetaUpgrade.Vigor));
            Assert.AreEqual(2, q.Runs);
            Assert.AreEqual(8, q.BestRooms);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("lixo")]
        [TestCase("v9;1;2;3;4;1,1,1,1,1")]
        [TestCase("v1;abc;;x;y;z")]
        [TestCase("v1;10;2")]
        public void Broken_Save_Never_Throws(string text)
        {
            MetaProgress p = null;
            Assert.DoesNotThrow(() => p = MetaProgress.Parse(text));
            Assert.GreaterOrEqual(p.Shards, 0);
            foreach (MetaUpgrade u in System.Enum.GetValues(typeof(MetaUpgrade)))
                Assert.That(p.Level(u), Is.InRange(0, MetaInfo.MaxLevel(u)));
        }

        [Test]
        public void Tampered_Save_Is_Clamped()
        {
            var p = MetaProgress.Parse("v1;-500;3;99;-2;99,-4,2");
            Assert.AreEqual(0, p.Shards);
            Assert.AreEqual(3, p.Wins, "vitorias nunca passam do numero de runs");
            Assert.AreEqual(0, p.BestRooms);
            Assert.AreEqual(MetaInfo.MaxLevel(MetaUpgrade.Vigor), p.Level(MetaUpgrade.Vigor));
            Assert.AreEqual(0, p.Level(MetaUpgrade.Power));
            Assert.AreEqual(2, p.Level(MetaUpgrade.Purse));
            Assert.AreEqual(0, p.Level(MetaUpgrade.Focus), "nivel que falta no texto comeca em zero");
        }

        [Test] public void Wisdom_RaisesXp_AndInsight_GivesRerolls()
        {
            var p = new MetaProgress();
            p.AddShards(999);
            Assert.AreEqual(0, p.Rerolls);
            Assert.IsTrue(p.TryBuy(MetaUpgrade.Wisdom));
            Assert.IsTrue(p.TryBuy(MetaUpgrade.Insight));
            Assert.IsTrue(p.TryBuy(MetaUpgrade.Insight));
            Assert.IsFalse(p.TryBuy(MetaUpgrade.Insight), "Intuicao tem dois niveis");
            Assert.AreEqual(2, p.Rerolls);
            var m = new Quintessence.Core.HeroModifiers();
            p.ApplyTo(m);
            Assert.AreEqual(1.08f, m.XpMult, 0.001f);
            var back = MetaProgress.Parse(p.Serialize());
            Assert.AreEqual(2, back.Rerolls);
            Assert.AreEqual(1, back.Level(MetaUpgrade.Wisdom));
        }

        [Test] public void OldSaves_WithFiveUpgrades_StillLoad()
        {
            var p = MetaProgress.Parse("v1;40;3;1;7;2,1,0,3,1");
            Assert.AreEqual(40, p.Shards);
            Assert.AreEqual(2, p.Level(MetaUpgrade.Vigor));
            Assert.AreEqual(0, p.Level(MetaUpgrade.Wisdom));
            Assert.AreEqual(0, p.Rerolls);
        }

        [Test] public void Victories_AreCounted_PerHero_AndSurviveSaving()
        {
            var p = new MetaProgress();
            p.EndRun(10, true, 1f, ElementType.Water);
            p.EndRun(10, true, 1f, ElementType.Water);
            p.EndRun(4, false, 1f, ElementType.Fire);
            Assert.AreEqual(2, p.HeroWins(ElementType.Water));
            Assert.AreEqual(0, p.HeroWins(ElementType.Fire), "derrota nao conta");
            var back = MetaProgress.Parse(p.Serialize());
            Assert.AreEqual(2, back.HeroWins(ElementType.Water));
            Assert.AreEqual(2, back.Wins);
            Assert.AreEqual(0, MetaProgress.Parse("v1;40;3;1;7;2,1,0,3,1").HeroWins(ElementType.Water), "save antigo, sem o campo novo");
        }
    }
}
