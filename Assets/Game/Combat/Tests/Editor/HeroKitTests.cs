using NUnit.Framework;
using Quintessence.Core;

namespace Quintessence.Combat.Tests
{
    public class HeroKitTests
    {
        [Test] public void EveryHero_HasAThreeHitCombo_ThatEndsStronger()
        {
            foreach (var e in Elements.All)
            {
                var k = HeroKit.Of(e);
                Assert.AreEqual(3, k.Steps.Length, e.ToString());
                Assert.Greater(k.Steps[2].Damage, k.Steps[0].Damage, e + ": o terceiro golpe fecha mais forte");
                foreach (var s in k.Steps) Assert.Less(s.HitTime, s.Duration, e + ": o dano sai antes do golpe acabar");
                Assert.IsNotEmpty(k.Style);
            }
        }

        [Test] public void EveryHero_HasItsOwnQuintessenceBurst()
        {
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var e in Elements.All)
            {
                var b = HeroKit.Of(e).Burst;
                Assert.IsNotEmpty(b.Name, e.ToString());
                Assert.IsTrue(names.Add(b.Name), "cada heroi tem a sua: " + b.Name);
                Assert.Greater(b.Damage, 0f);
                Assert.GreaterOrEqual(b.Radius, 4f, "pega o bando em volta");
                Assert.IsTrue(b.Applies && b.StatusTime > 0f, e + ": deixa um efeito");
            }
            Assert.Greater(HeroKit.Of(ElementType.Water).Burst.Heal, 0f, "a da Liora cura");
            Assert.AreEqual(StatusType.Stun, HeroKit.Of(ElementType.Earth).Burst.Status, "a do Brann atordoa");
            Assert.Greater(HeroKit.Of(ElementType.Air).Burst.Knockback, HeroKit.Of(ElementType.Fire).Burst.Knockback, "a do Vey empurra mais longe");
        }

        [Test] public void Kits_AreDifferent_ButDealSimilarDamagePerSecond()
        {
            float lo = float.MaxValue, hi = 0f;
            foreach (var e in Elements.All)
            {
                var k = HeroKit.Of(e);
                float dps = k.ComboDamage / k.ComboTime;
                lo = System.Math.Min(lo, dps); hi = System.Math.Max(hi, dps);
            }
            Assert.Less(hi / lo, 1.35f, "nenhum heroi bate muito mais por segundo que outro no golpe basico");
            Assert.Less(HeroKit.Of(ElementType.Air).ComboTime, HeroKit.Of(ElementType.Earth).ComboTime, "Vey e mais rapido que Brann");
            Assert.Greater(HeroKit.Of(ElementType.Water).Range, HeroKit.Of(ElementType.Fire).Range, "o chicote alcanca mais que as garras");
            Assert.Greater(HeroKit.Of(ElementType.Earth).Arc, HeroKit.Of(ElementType.Water).Arc, "o martelo varre mais largo que o chicote");
        }
    }
}
