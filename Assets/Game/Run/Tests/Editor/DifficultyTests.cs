using NUnit.Framework;

namespace Quintessence.Run.Tests
{
    public class DifficultyTests
    {
        [Test]
        public void Enemies_Get_Tougher_Every_Layer()
        {
            for (int depth = 1; depth < 10; depth++)
            {
                var a = DifficultyCurve.For(RoomType.Combat, depth - 1);
                var b = DifficultyCurve.For(RoomType.Combat, depth);
                Assert.Greater(b.Health, a.Health);
                Assert.Greater(b.Damage, a.Damage);
            }
        }

        [Test]
        public void First_Room_Uses_The_Sheet_And_The_Last_Layer_Is_Bounded()
        {
            var first = DifficultyCurve.For(RoomType.Combat, 0);
            Assert.AreEqual(1f, first.Health, 0.001f);
            Assert.AreEqual(DifficultyCurve.EarlyDamage(0), first.Damage, 0.001f);
            Assert.Less(first.Damage, 1f, "a primeira sala bate mais fraco: aquecimento");
            Assert.AreEqual(1f, DifficultyCurve.EarlyDamage(2), "do terceiro em diante a rampa some");
            var last = DifficultyCurve.For(RoomType.Combat, 7);
            Assert.AreEqual(1f + 7 * DifficultyCurve.HealthPerLayer, last.Health, 0.001f);
            Assert.AreEqual(1f + 7 * DifficultyCurve.DamagePerLayer, last.Damage, 0.001f);
            Assert.LessOrEqual(DifficultyCurve.For(RoomType.Combat, 9).Damage, 1.65f, "o dano nao dispara nem na ultima camada de uma run de 10");
        }

        [Test]
        public void Combat_Never_Changes_Speed_Or_Size()
        {
            for (int depth = 0; depth < 10; depth++)
            {
                var d = DifficultyCurve.For(RoomType.Combat, depth);
                Assert.AreEqual(1f, d.Speed);
                Assert.AreEqual(1f, d.Cooldown);
                Assert.AreEqual(1f, d.Size);
            }
        }

        [TestCase(RoomType.Boss)]
        [TestCase(RoomType.Shop)]
        [TestCase(RoomType.Shrine)]
        [TestCase(RoomType.Rest)]
        [TestCase(RoomType.Master)]
        public void Other_Rooms_Are_Untouched(RoomType type)
        {
            var d = DifficultyCurve.For(type, 5, EliteModifier.Fierce);
            Assert.AreEqual(1f, d.Health);
            Assert.AreEqual(1f, d.Damage);
            Assert.AreEqual(1f, d.Speed);
        }

        [Test]
        public void Each_Elite_Trait_Changes_Only_Its_Own_Number()
        {
            var plain = DifficultyCurve.For(RoomType.Combat, 3);
            var swift = DifficultyCurve.For(RoomType.Elite, 3, EliteModifier.Swift);
            var sturdy = DifficultyCurve.For(RoomType.Elite, 3, EliteModifier.Sturdy);
            var fierce = DifficultyCurve.For(RoomType.Elite, 3, EliteModifier.Fierce);
            Assert.Greater(swift.Speed, 1f); Assert.Less(swift.Cooldown, 1f);
            Assert.AreEqual(plain.Health, swift.Health, 0.001f); Assert.AreEqual(plain.Damage, swift.Damage, 0.001f);
            Assert.AreEqual(plain.Health * DifficultyCurve.SturdyHealth, sturdy.Health, 0.001f); Assert.AreEqual(plain.Damage, sturdy.Damage, 0.001f);
            Assert.AreEqual(plain.Damage * DifficultyCurve.FierceDamage, fierce.Damage, 0.001f); Assert.AreEqual(plain.Health, fierce.Health, 0.001f);
            Assert.Greater(swift.Size, 1f, "inimigo de elite e um pouco maior, para dar para ver");
        }

        [Test]
        public void Elite_Trait_Is_Stable_And_All_Five_Show_Up()
        {
            var seen = new System.Collections.Generic.HashSet<EliteModifier>();
            for (int seed = 1; seed < 40; seed++)
            for (int node = 0; node < 20; node++)
            {
                var m = DifficultyCurve.PickElite(seed, node);
                Assert.AreNotEqual(EliteModifier.None, m);
                Assert.AreEqual(m, DifficultyCurve.PickElite(seed, node));
                seen.Add(m);
                Assert.IsNotEmpty(DifficultyCurve.Name(m));
                Assert.IsNotEmpty(DifficultyCurve.Describe(m));
            }
            Assert.AreEqual(5, seen.Count);
        }

        [Test]
        public void Volatile_And_Warded_Change_The_Enemies()
        {
            var v = DifficultyCurve.For(RoomType.Elite, 4, EliteModifier.Volatile);
            Assert.IsTrue(v.Volatile);
            Assert.AreEqual(0, v.Wards);
            var w = DifficultyCurve.For(RoomType.Elite, 4, EliteModifier.Warded);
            Assert.AreEqual(1, w.Wards);
            Assert.IsFalse(w.Volatile);
            Assert.IsFalse(DifficultyCurve.For(RoomType.Combat, 4).Volatile, "sala comum nao tem traco");
        }
    }
}
