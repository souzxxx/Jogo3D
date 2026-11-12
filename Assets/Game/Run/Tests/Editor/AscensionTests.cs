using NUnit.Framework;

namespace Quintessence.Run.Tests
{
    public class AscensionTests
    {
        [Test] public void NothingUnlocked_BeforeTheFirstWin()
        {
            Assert.AreEqual(0, Ascension.Unlocked(0));
            Assert.AreEqual(0, Ascension.Clamp(3, 0));
        }

        [Test] public void EachWin_UnlocksOneLevel_UpToTheMax()
        {
            Assert.AreEqual(1, Ascension.Unlocked(1));
            Assert.AreEqual(Ascension.Max, Ascension.Unlocked(99));
            Assert.AreEqual(2, Ascension.Clamp(5, 2));
            Assert.AreEqual(0, Ascension.Clamp(-3, 4));
        }

        [Test] public void HigherLevels_AreHarder_AndPayMore()
        {
            Assert.AreEqual(1f, Ascension.HealthMult(0));
            Assert.AreEqual(1f, Ascension.DamageMult(0));
            Assert.AreEqual(1f, Ascension.ShardMult(0));
            for (int l = 1; l <= Ascension.Max; l++)
            {
                Assert.Greater(Ascension.HealthMult(l), Ascension.HealthMult(l - 1));
                Assert.Greater(Ascension.DamageMult(l), Ascension.DamageMult(l - 1));
                Assert.Greater(Ascension.ShardMult(l), Ascension.ShardMult(l - 1));
                Assert.IsNotEmpty(Ascension.Describe(l));
            }
        }

        [Test] public void AscensionMultipliesTheShardReward()
        {
            var a = new MetaProgress(); var b = new MetaProgress();
            int plain = a.EndRun(8, true);
            int more = b.EndRun(8, true, Ascension.ShardMult(2));
            Assert.Greater(more, plain);
        }
    }
}
