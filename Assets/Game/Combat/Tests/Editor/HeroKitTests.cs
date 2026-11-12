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
