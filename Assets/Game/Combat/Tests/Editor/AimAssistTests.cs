using NUnit.Framework;
using Quintessence.Combat;
using UnityEngine;

namespace Quintessence.Combat.Tests
{
    public class AimAssistTests
    {
        [Test]
        public void Picks_The_Enemy_Close_To_The_Stick_Direction()
        {
            var targets = new[] { new Vector3(5f, 0f, 1f), new Vector3(-4f, 0f, 0f) };
            var pick = AimAssist.Pick(Vector3.zero, Vector3.right, targets);
            Assert.IsTrue(pick.HasValue);
            Assert.AreEqual(targets[0], pick.Value);
        }

        [Test]
        public void Ignores_Enemies_Outside_The_Cone_Or_Too_Far()
        {
            Assert.IsFalse(AimAssist.Pick(Vector3.zero, Vector3.right, new[] { new Vector3(0f, 0f, 5f) }).HasValue, "90 graus fora");
            Assert.IsFalse(AimAssist.Pick(Vector3.zero, Vector3.right, new[] { new Vector3(20f, 0f, 0f) }).HasValue, "longe demais");
            Assert.IsFalse(AimAssist.Pick(Vector3.zero, Vector3.zero, new[] { new Vector3(3f, 0f, 0f) }).HasValue, "sem direcao");
        }

        [Test]
        public void Prefers_The_Better_Aligned_Over_The_Nearer()
        {
            var aligned = new Vector3(7f, 0f, 0f);
            var nearButOff = new Vector3(2f, 0f, 1f); // ~26 graus
            Assert.AreEqual(aligned, AimAssist.Pick(Vector3.zero, Vector3.right, new[] { nearButOff, aligned }).Value);
        }
    }
}
