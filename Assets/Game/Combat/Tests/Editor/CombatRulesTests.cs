using NUnit.Framework;

namespace Quintessence.Combat.Tests
{
    public class DashStateTests
    {
        [Test] public void DashLastsItsDuration()
        {
            var d = new DashState(0.15f, 0.6f, 1);
            Assert.IsTrue(d.TryStart());
            d.Tick(0.1f); Assert.IsTrue(d.IsDashing);
            d.Tick(0.1f); Assert.IsFalse(d.IsDashing);
        }

        [Test] public void CannotDashAgainUntilCooldown()
        {
            var d = new DashState(0.15f, 0.6f, 1);
            d.TryStart(); d.Tick(0.3f);
            Assert.IsFalse(d.TryStart());
            d.Tick(0.31f);
            Assert.IsTrue(d.TryStart());
        }

        [Test] public void TwoChargesAllowTwoDashesInARow()
        {
            var d = new DashState(0.15f, 0.6f, 2);
            Assert.IsTrue(d.TryStart()); d.Tick(0.16f);
            Assert.IsTrue(d.TryStart()); d.Tick(0.16f);
            Assert.IsFalse(d.TryStart());
        }

        [Test] public void CannotStartWhileDashing()
        {
            var d = new DashState(0.15f, 0.6f, 2);
            d.TryStart();
            Assert.IsFalse(d.TryStart());
        }
    }

    public class ComboStateTests
    {
        static ComboState Make() => new(new[]
        {
            new ComboStep(6, 0.3f, 0.1f, 1), new ComboStep(6, 0.3f, 0.1f, 1), new ComboStep(10, 0.4f, 0.2f, 2),
        }, 0.5f);

        static int Run(ComboState c, float seconds, bool hold, out int chains)
        {
            int hits = 0; chains = 0;
            for (float t = 0; t < seconds; t += 0.01f)
            {
                if (hold) c.TryAttack();
                c.Tick(0.01f, out bool hit, out bool chained);
                if (hit) hits++;
                if (chained) chains++;
            }
            return hits;
        }

        [Test] public void SingleAttackHitsOnceAndEnds()
        {
            var c = Make();
            Assert.IsTrue(c.TryAttack());
            Assert.AreEqual(1, Run(c, 0.5f, false, out _));
            Assert.IsFalse(c.IsAttacking);
        }

        [Test] public void HoldingChainsAllThreeThenLoops()
        {
            var c = Make();
            Assert.AreEqual(3, Run(c, 1.02f, true, out int chains));
            Assert.AreEqual(2, chains);
            c.TryAttack();
            Assert.AreEqual(0, c.CurrentIndex, "depois do finalizador o combo recomeca");
        }

        [Test] public void PressBeforeHitDoesNotBuffer()
        {
            var c = Make();
            c.TryAttack();
            c.Tick(0.05f, out _, out _);
            c.TryAttack();
            Run(c, 0.3f, false, out int chains);
            Assert.AreEqual(0, chains);
        }

        [Test] public void ComboContinuesIfPressedSoonAndResetsIfLate()
        {
            var c = Make();
            c.TryAttack(); Run(c, 0.35f, false, out _);
            c.TryAttack();
            Assert.AreEqual(1, c.CurrentIndex);
            Run(c, 0.35f, false, out _);
            Run(c, 0.6f, false, out _);
            c.TryAttack();
            Assert.AreEqual(0, c.CurrentIndex);
        }

        [Test] public void CancelStopsTheAttack()
        {
            var c = Make();
            c.TryAttack(); c.Cancel();
            Assert.IsFalse(c.IsAttacking);
            Assert.AreEqual(0, Run(c, 0.5f, false, out _));
        }
    }
}
