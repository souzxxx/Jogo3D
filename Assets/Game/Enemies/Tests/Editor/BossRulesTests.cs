using NUnit.Framework;
using UnityEngine;

namespace Quintessence.Enemies.Tests
{
    public class BossRulesTests
    {
        [Test] public void ThreePhasesByThirdsOfHealth()
        {
            Assert.AreEqual(0, BossRules.PhaseFor(1f));
            Assert.AreEqual(0, BossRules.PhaseFor(0.67f));
            Assert.AreEqual(1, BossRules.PhaseFor(0.66f));
            Assert.AreEqual(1, BossRules.PhaseFor(0.34f));
            Assert.AreEqual(2, BossRules.PhaseFor(0.33f));
            Assert.AreEqual(2, BossRules.PhaseFor(0f));
        }

        [Test] public void LaterPhasesUseMoreAttackKinds()
        {
            bool Uses(int phase, BossAttack a)
            {
                for (int i = 0; i < 12; i++) if (BossRules.Next(phase, i, true) == a) return true;
                return false;
            }
            Assert.IsFalse(Uses(0, BossAttack.Eruption), "a primeira fase ensina so Pancada e Leque");
            Assert.IsTrue(Uses(1, BossAttack.Eruption));
            Assert.IsTrue(Uses(2, BossAttack.Eruption) && Uses(2, BossAttack.Fan) && Uses(2, BossAttack.Slam));
        }

        [Test] public void NeverSlamsWhenHeroIsOutOfReach()
        {
            for (int phase = 0; phase < 3; phase++)
                for (int i = 0; i < 12; i++)
                    Assert.AreNotEqual(BossAttack.Slam, BossRules.Next(phase, i, false));
        }

        [Test] public void FanIsSymmetricAroundForward()
        {
            var dirs = BossRules.FanDirections(Vector3.forward, 5, 20f);
            Assert.AreEqual(5, dirs.Length);
            Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, dirs[2]), 0.01f);
            Assert.AreEqual(40f, Vector3.Angle(Vector3.forward, dirs[0]), 0.01f);
            Assert.AreEqual(40f, Vector3.Angle(Vector3.forward, dirs[4]), 0.01f);
            Assert.AreEqual(-dirs[0].x, dirs[4].x, 0.001f);
        }
    }

    public class BossShieldTests
    {
        [Test] public void OnlyBreaksDuringQuintessence()
        {
            var s = new BossShield(12f);
            s.Raise(true);
            Assert.IsFalse(s.Hit(false));
            Assert.IsTrue(s.Up);
            Assert.IsTrue(s.Hit(true));
            Assert.IsFalse(s.Up);
        }

        [Test] public void DoesNotTimeOutWhenHeroCanUseQuintessence()
        {
            var s = new BossShield(12f);
            s.Raise(true);
            Assert.IsFalse(s.Tick(60f));
            Assert.IsTrue(s.Up);
        }

        [Test] public void TimesOutWhenHeroCannotUseQuintessence_NoDeadEnd()
        {
            var s = new BossShield(12f);
            s.Raise(false);
            Assert.IsFalse(s.Tick(11.9f));
            Assert.IsTrue(s.Tick(0.2f));
            Assert.IsFalse(s.Up);
        }

        [Test] public void HitsOnABrokenShieldDoNothing()
        {
            var s = new BossShield(12f);
            Assert.IsFalse(s.Hit(true), "escudo ainda nao levantou");
            s.Raise(true); s.Hit(true);
            Assert.IsFalse(s.Hit(true));
        }

        [Test] public void RingPoints_AreEvenlySpacedAtTheRadius()
        {
            var pts = BossRules.RingPoints(new UnityEngine.Vector3(2, 0, -1), 5f, 8);
            Assert.AreEqual(8, pts.Length);
            foreach (var p in pts) Assert.AreEqual(5f, UnityEngine.Vector3.Distance(p, new UnityEngine.Vector3(2, 0, -1)), 0.001f);
            Assert.AreEqual(UnityEngine.Vector3.Distance(pts[0], pts[1]), UnityEngine.Vector3.Distance(pts[3], pts[4]), 0.001f);
        }

        [Test] public void ChargeAndRing_OnlyComeAfterTheFirstPhase()
        {
            for (int i = 0; i < 12; i++)
            {
                var a = BossRules.Next(0, i, true);
                Assert.IsTrue(a != BossAttack.Charge && a != BossAttack.Ring, "a primeira fase ensina so o basico");
            }
            bool charge = false, ring = false;
            for (int i = 0; i < 12; i++) { charge |= BossRules.Next(1, i, true) == BossAttack.Charge; ring |= BossRules.Next(2, i, true) == BossAttack.Ring; }
            Assert.IsTrue(charge, "a investida entra na segunda fase");
            Assert.IsTrue(ring, "o anel entra na ultima");
        }

        [Test] public void Weaver_FightsFromRange_AndBlinks()
        {
            bool blink = false, slam = false, charge = false;
            for (int phase = 0; phase < 3; phase++)
                for (int i = 0; i < 12; i++)
                {
                    var a = BossRules.Next(phase, i, true, 1);
                    blink |= a == BossAttack.Blink; slam |= a == BossAttack.Slam; charge |= a == BossAttack.Charge;
                }
            Assert.IsTrue(blink, "a Tecela se teletransporta");
            Assert.IsFalse(slam || charge, "e nao luta corpo a corpo");
            Assert.AreEqual(BossRules.Next(1, 3, true), BossRules.Next(1, 3, true, 0), "o Guardiao continua igual");
        }

        [Test] public void Queen_FloodsWithEruptions_AndBlinksOnlyFromPhaseTwo()
        {
            int eruptions = 0;
            for (int i = 0; i < 4; i++)
            {
                var a = BossRules.Next(0, i, true, 2);
                Assert.AreNotEqual(BossAttack.Blink, a, "na primeira fase ela nao some");
                if (a == BossAttack.Eruption) eruptions++;
            }
            Assert.GreaterOrEqual(eruptions, 2, "a Erupcao e o ataque dela");
            bool blink = false;
            for (int i = 0; i < 6; i++) blink |= BossRules.Next(1, i, true, 2) == BossAttack.Blink;
            Assert.IsTrue(blink, "da segunda fase em diante se teletransporta");
            Assert.AreEqual(BossAttack.Fan, BossRules.Next(0, 2, false, 2), "longe demais para a Pancada: leque");
        }
    }
}
