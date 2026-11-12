using NUnit.Framework;
using Quintessence.Core;

namespace Quintessence.Run.Tests
{
    public class HeroPassiveTests
    {
        [Test] public void EveryHero_HasNameTitleAndText()
        {
            foreach (var e in Elements.All)
            {
                Assert.IsNotEmpty(HeroPassives.HeroName(e));
                Assert.IsNotEmpty(HeroPassives.Title(e));
                Assert.IsNotEmpty(HeroPassives.Describe(e));
            }
        }

        [Test] public void EachPassive_ChangesSomethingDifferent()
        {
            var rax = new HeroModifiers(); HeroPassives.Apply(ElementType.Fire, rax);
            var liora = new HeroModifiers(); HeroPassives.Apply(ElementType.Water, liora);
            var brann = new HeroModifiers(); HeroPassives.Apply(ElementType.Earth, brann);
            var vey = new HeroModifiers(); HeroPassives.Apply(ElementType.Air, vey);
            Assert.Greater(rax.BasicDamageMult, 1f);
            Assert.Greater(liora.HealOnRoomClear, 0f);
            Assert.Greater(brann.MaxHealthAdd, 0f);
            Assert.Greater(vey.MoveSpeedMult, 1f);
            Assert.Less(vey.DashCooldownMult, 1f);
            Assert.Greater(vey.Evasion, 0f, "Vey esquiva");
            Assert.Greater(vey.DodgeHeal, 0f, "e cada esquiva cura");
            Assert.AreEqual(0f, rax.DodgeHeal);
            Assert.AreEqual(0f, brann.Evasion);
            Assert.AreEqual(1f, brann.BasicDamageMult);
            Assert.AreEqual(0f, rax.MaxHealthAdd);
            Assert.Less(vey.MaxHealthAdd, brann.MaxHealthAdd, "Vey ganha um pouco de vida, menos que Brann");
        }
    }
}
