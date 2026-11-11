using NUnit.Framework;
using Quintessence.Core;

namespace Quintessence.Combat.Tests
{
    public class QuintessenceStateTests
    {
        [Test] public void TenOwnReactionsFillTheMeter()
        {
            var q = new QuintessenceState();
            for (int i = 0; i < 9; i++) q.AddReaction();
            Assert.IsFalse(q.Ready);
            q.AddReaction();
            Assert.IsTrue(q.Ready);
        }

        [Test] public void NeedsAllFourElements()
        {
            var q = new QuintessenceState();
            q.AddMeter(100);
            Assert.IsFalse(q.TryActivate(false));
            Assert.IsTrue(q.TryActivate(true));
            Assert.IsTrue(q.Active);
            Assert.AreEqual(0f, q.Meter);
        }

        [Test] public void LastsEightSecondsAndDoesNotChargeWhileActive()
        {
            var q = new QuintessenceState();
            q.AddMeter(100); q.TryActivate(true);
            q.AddReaction();
            Assert.AreEqual(0f, q.Meter, "nao enche durante a Quintessencia");
            Assert.IsFalse(q.Tick(7.9f));
            Assert.IsTrue(q.Tick(0.2f));
            Assert.IsFalse(q.Active);
        }

        [Test] public void PartnerReactionsGiveLess()
        {
            var q = new QuintessenceState();
            q.AddReaction(false);
            Assert.AreEqual(3f, q.Meter);
        }
    }

    public class HealthPoolS3Tests
    {
        [Test] public void BlockAbsorbsExactlyOneHit()
        {
            var p = new HealthPool(100) { Blocks = 1 };
            Assert.AreEqual(0, p.Damage(30)); Assert.IsTrue(p.LastBlocked);
            Assert.AreEqual(30, p.Damage(30)); Assert.IsFalse(p.LastBlocked);
        }

        [Test] public void QuintessenceHalvesDamageOnTopOfStatus()
        {
            var p = new HealthPool(100) { DamageTakenMultiplier = 1.25f, ExtraDamageTakenMultiplier = 0.5f };
            Assert.AreEqual(12.5f, p.Damage(20), 0.001f);
        }
    }
}
