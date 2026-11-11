using NUnit.Framework;
using UnityEngine;

namespace Quintessence.Core.Tests
{
    public class HealthPoolTests
    {
        [Test] public void StartsFull() { var p = new HealthPool(100); Assert.AreEqual(100, p.Current); Assert.IsFalse(p.IsDead); }

        [Test] public void DamageIsClampedToCurrent()
        {
            var p = new HealthPool(30);
            Assert.AreEqual(30, p.Damage(50));
            Assert.IsTrue(p.IsDead);
            Assert.AreEqual(0, p.Damage(10), "morto nao leva dano");
        }

        [Test] public void InvulnerableTakesNothing()
        {
            var p = new HealthPool(100) { Invulnerable = true };
            Assert.AreEqual(0, p.Damage(40));
            Assert.AreEqual(100, p.Current);
        }

        [Test] public void MultiplierScalesDamage()
        {
            var p = new HealthPool(100) { DamageTakenMultiplier = 1.25f };
            Assert.AreEqual(25, p.Damage(20), 0.001f);
        }

        [Test] public void NegativeDamageIsIgnored() { var p = new HealthPool(100); Assert.AreEqual(0, p.Damage(-5)); Assert.AreEqual(100, p.Current); }

        [Test] public void HealIsClampedAndDeadDoesNotHeal()
        {
            var p = new HealthPool(100);
            p.Damage(10);
            Assert.AreEqual(10, p.Heal(999));
            p.Damage(100);
            Assert.AreEqual(0, p.Heal(50));
            p.Revive(0.3f);
            Assert.AreEqual(30, p.Current, 0.001f);
        }

        [Test] public void SetMaxKeepsRatio()
        {
            var p = new HealthPool(100);
            p.Damage(50);
            p.SetMax(200);
            Assert.AreEqual(100, p.Current, 0.001f);
        }
    }

    public class HealthTests
    {
        GameObject go; Health health;

        [SetUp] public void SetUp()
        {
            GameEvents.Clear();
            go = new GameObject("target");
            health = go.AddComponent<Health>();
            health.Configure(50, Team.Enemy);
        }

        [TearDown] public void TearDown() { Object.DestroyImmediate(go); GameEvents.Clear(); }

        [Test] public void SameTeamDoesNoDamage()
        {
            Assert.AreEqual(0, health.TakeDamage(new DamageInfo(10, ElementType.Fire, Team.Enemy)));
            Assert.AreEqual(50, health.Current);
        }

        [Test] public void DamageRaisesLocalAndGlobalEvents()
        {
            float local = 0, global = 0;
            health.Damaged += (_, a) => local = a;
            GameEvents.DamageDealt += (_, _, a) => global = a;
            health.TakeDamage(new DamageInfo(10, ElementType.Water, Team.Player));
            Assert.AreEqual(10, local);
            Assert.AreEqual(10, global);
        }

        [Test] public void DiesOnceAndOnlyOnce()
        {
            int deaths = 0;
            GameEvents.Died += (_, _) => deaths++;
            health.TakeDamage(new DamageInfo(999, ElementType.None, Team.Player));
            health.TakeDamage(new DamageInfo(999, ElementType.None, Team.Player));
            Assert.IsTrue(health.IsDead);
            Assert.AreEqual(1, deaths);
        }
    }
}
