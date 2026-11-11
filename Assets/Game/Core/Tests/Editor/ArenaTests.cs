using NUnit.Framework;
using UnityEngine;

namespace Quintessence.Core.Tests
{
    public class ArenaTests
    {
        [Test]
        public void Inside_The_Room_Is_Fine()
        {
            Assert.IsTrue(Arena.Contains(Vector3.zero));
            Assert.IsTrue(Arena.Contains(new Vector3(9.5f, 0.05f, -7.5f)));
            Assert.IsTrue(Arena.Contains(new Vector3(0, 2f, 7.15f)), "em frente a porta");
        }

        [TestCase(30f, 0f, 0f)]
        [TestCase(0f, 0f, -30f)]
        [TestCase(0f, -20f, 0f)]
        [TestCase(0f, 50f, 0f)]
        [TestCase(float.NaN, 0f, 0f)]
        public void Outside_Fallen_Or_Broken_Position_Is_Out(float x, float y, float z) =>
            Assert.IsFalse(Arena.Contains(new Vector3(x, y, z)));

        [TestCase(30f, 0f, 0f)]
        [TestCase(-40f, -9f, 25f)]
        [TestCase(float.NaN, 0f, 0f)]
        public void Clamp_Always_Lands_Inside_On_The_Floor(float x, float y, float z)
        {
            var p = Arena.Clamp(new Vector3(x, y, z));
            Assert.IsTrue(Arena.Contains(p));
            Assert.AreEqual(0f, p.y);
        }

        [Test]
        public void Stall_Fires_Only_After_The_Limit_Without_Progress()
        {
            var t = new StallTimer(10f);
            for (int i = 0; i < 9; i++) Assert.IsFalse(t.Tick(1f, true));
            Assert.IsTrue(t.Tick(1f, true));
            Assert.AreEqual(1, t.Fired);
            Assert.IsFalse(t.Tick(1f, true), "depois de disparar recomeca a contar");
        }

        [Test]
        public void Progress_Or_No_Fight_Resets_The_Count()
        {
            var t = new StallTimer(10f);
            t.Tick(9f, true);
            t.NoteProgress();
            Assert.IsFalse(t.Tick(9f, true));
            t.Tick(0.5f, false);
            Assert.IsFalse(t.Tick(9f, true), "sala sem luta nao acumula");
            Assert.AreEqual(0, t.Fired);
        }

        [Test]
        public void Negative_Time_Never_Counts()
        {
            var t = new StallTimer(5f);
            Assert.IsFalse(t.Tick(-100f, true));
            Assert.IsFalse(t.Tick(4f, true));
        }
    }
}
