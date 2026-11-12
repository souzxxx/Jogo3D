using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Run.Tests
{
    public class BoonTests
    {
        static BoonCatalog Catalog()
        {
            var c = ScriptableObject.CreateInstance<BoonCatalog>();
            c.FillDefaults();
            return c;
        }

        [Test] public void CatalogHasTwentyUniqueBoons()
        {
            var c = Catalog();
            Assert.AreEqual(29, c.boons.Count);
            Assert.AreEqual(29, c.boons.Select(b => b.id).Distinct().Count());
            foreach (var b in c.boons) { Assert.IsNotEmpty(b.name); Assert.IsNotEmpty(b.description); Assert.Greater(b.value, 0f); }
        }

        [Test] public void EveryBoonChangesSomething()
        {
            foreach (var b in Catalog().boons)
            {
                var m = new HeroModifiers();
                BoonEffects.Apply(m, b);
                bool changed = m.DamageAll != 1f || m.ReactionPower != 1f || m.CritChanceAdd != 0f || m.MoveSpeedMult != 1f
                    || m.DashCooldownMult != 1f || m.SkillCooldownMult != 1f || m.MaxHealthAdd != 0f || m.QuintessenceGainMult != 1f
                    || m.HealOnReaction != 0f || m.HealOnRoomClear != 0f || m.SwitchStrikeAdd != 0f || m.StatusDurationMult != 1f
                    || m.BasicDamageMult != 1f || m.FinisherMult != 1f || m.XpMult != 1f || m.EssenceMult != 1f || m.BurningBonus != 0f
                    || m.WaterHealAdd != 0f || m.EarthThorns != 0f || m.AirHasteSeconds != 0f || m.SkillRefundOnKill != 0f
                    || Elements.All.Any(e => m.Damage(e) != 1f);
                Assert.IsTrue(changed, b.name + " nao muda nada");
            }
        }

        [Test] public void ElementDamageBoonOnlyAffectsItsElement()
        {
            var m = new HeroModifiers();
            BoonEffects.Apply(m, Catalog().boons.First(b => b.id == "fire_dmg"));
            Assert.AreEqual(1.2f, m.Damage(ElementType.Fire), 0.001f);
            Assert.AreEqual(1f, m.Damage(ElementType.Water), 0.001f);
        }

        [Test] public void CooldownBoonsStackWithoutReachingZero()
        {
            var m = new HeroModifiers();
            var skill = Catalog().boons.Where(b => b.stat == BoonStat.SkillCooldown).ToList();
            foreach (var b in skill) BoonEffects.Apply(m, b);
            Assert.AreEqual(0.85f * 0.9f, m.SkillCooldownMult, 0.001f);
            Assert.Greater(m.SkillCooldownMult, 0f);
        }

        [Test] public void OfferIsThreeDistinctAndRespectsAwakenedElements([NUnit.Framework.Range(1, 30)] int seed)
        {
            var offer = BoonPicker.Offer(Catalog().boons, new HashSet<string>(), new[] { ElementType.Fire }, new System.Random(seed));
            Assert.AreEqual(3, offer.Count);
            Assert.AreEqual(3, offer.Select(b => b.id).Distinct().Count());
            foreach (var b in offer) Assert.IsTrue(b.patron == ElementType.None || b.patron == ElementType.Fire, b.name + " e de um elemento nao desperto");
        }

        [Test] public void NeverOffersWhatWasAlreadyTaken()
        {
            var c = Catalog();
            var taken = new HashSet<string>(c.boons.Take(c.boons.Count - 2).Select(b => b.id));
            var offer = BoonPicker.Offer(c.boons, taken, Elements.All, new System.Random(1));
            Assert.AreEqual(2, offer.Count, "so sobraram duas");
            foreach (var b in offer) Assert.IsFalse(taken.Contains(b.id));
        }

        [Test] public void ResetClearsModifiers()
        {
            var m = new HeroModifiers();
            foreach (var b in Catalog().boons) BoonEffects.Apply(m, b);
            m.Reset();
            Assert.AreEqual(1f, m.Damage(ElementType.Fire));
            Assert.AreEqual(0f, m.MaxHealthAdd);
            Assert.AreEqual(1f, m.SkillCooldownMult);
        }
    }
}
