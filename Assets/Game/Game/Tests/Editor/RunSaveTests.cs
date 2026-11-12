using NUnit.Framework;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Game.Tests
{
    public class RunSaveTests
    {
        const string Key = "quintessence.run.edittest";

        [SetUp] public void SetUp() { RunSaveStore.UseKey(Key); RunSaveStore.Clear(); }
        [TearDown] public void TearDown() { RunSaveStore.Clear(); RunSaveStore.UseKey(RunSaveStore.DefaultKey); }

        [Test] public void Modifiers_RoundTrip_Exactly()
        {
            var m = new HeroModifiers();
            m.DamageAll = 1.37f; m.AddElementDamage(ElementType.Water, 0.3f); m.AddSkillRange(ElementType.Air, 0.25f);
            m.ScaleSkillCooldown(ElementType.Fire, 0.81f); m.AddReactionBonus(ElementType.Fire, ElementType.Earth, 0.4f);
            m.DashChargesAdd = 1; m.FireTrailSeconds = 2.5f;
            var back = new HeroModifiers();
            Assert.IsTrue(back.FromJson(m.ToJson()));
            Assert.AreEqual(m.ToJson(), back.ToJson());
            Assert.AreEqual(1.3f, back.Damage(ElementType.Water) / back.DamageAll, 0.0001f);
            Assert.AreEqual(1.4f, back.ReactionBonus(ElementType.Earth, ElementType.Fire), 0.0001f);
        }

        [Test] public void BrokenModifiersText_ChangesNothing()
        {
            var m = new HeroModifiers();
            string clean = m.ToJson();
            Assert.IsFalse(m.FromJson(""));
            m.FromJson("{ nao e json");
            Assert.AreEqual(clean, m.ToJson());
        }

        [Test] public void Save_Load_And_Clear()
        {
            Assert.IsNull(RunSaveStore.Load());
            RunSaveStore.Save(new RunSave { hero = 2, seed = 9, layers = 10, shrines = 3, current = 4, depth = 2, maxHealth = 100, health = 60 });
            var s = RunSaveStore.Load();
            Assert.IsNotNull(s);
            Assert.AreEqual(4, s.current);
            StringAssert.Contains("Liora", s.Summary);
            StringAssert.Contains("3/10", s.Summary);
            RunSaveStore.Clear();
            Assert.IsNull(RunSaveStore.Load());
        }

        [Test] public void BrokenSave_IsTreatedAsNoSave()
        {
            PlayerPrefs.SetString(Key, "{\"version\":1,\"hero\":9}");
            Assert.IsNull(RunSaveStore.Load(), "heroi invalido");
            PlayerPrefs.SetString(Key, "lixo");
            Assert.IsNull(RunSaveStore.Load());
            PlayerPrefs.SetString(Key, "{\"version\":99,\"hero\":1,\"layers\":10,\"maxHealth\":100}");
            Assert.IsNull(RunSaveStore.Load(), "versao desconhecida");
        }
    }
}
