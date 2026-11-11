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
        public void First_Rooms_Are_Below_The_Sheet_And_Late_Rooms_Above()
        {
            var first = DifficultyCurve.For(RoomType.Combat, 0);
            Assert.Less(first.Health, 1f);
            Assert.Less(first.Damage, 1f);
            var late = DifficultyCurve.For(RoomType.Combat, 6);
            Assert.Greater(late.Health, 1f);
            Assert.Greater(late.Damage, 1f);
            Assert.LessOrEqual(late.Damage, 1.3f, "o dano nao dispara: a run tem 8 camadas");
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
            Assert.AreEqual(plain.Health * 1.5f, sturdy.Health, 0.001f); Assert.AreEqual(plain.Damage, sturdy.Damage, 0.001f);
            Assert.AreEqual(plain.Damage * 1.3f, fierce.Damage, 0.001f); Assert.AreEqual(plain.Health, fierce.Health, 0.001f);
            Assert.Greater(swift.Size, 1f, "inimigo de elite e um pouco maior, para dar para ver");
        }

        [Test]
        public void Elite_Trait_Is_Stable_And_All_Three_Show_Up()
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
            Assert.AreEqual(3, seen.Count);
        }
    }
}
