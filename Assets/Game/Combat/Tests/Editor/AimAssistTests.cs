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

        /// Playtest de 18/11 no controle: "so fixa de muito longe". O golpe e corpo a corpo: o inimigo colado, mesmo um
        /// pouco fora da linha do analogico, ganha do alinhado la longe.
        [Test]
        public void Close_Enemy_In_The_Cone_Beats_A_Far_Aligned_One()
        {
            var aligned = new Vector3(7f, 0f, 0f);
            var nearButOff = new Vector3(2f, 0f, 1f); // ~26 graus, a 2,2 m
            Assert.AreEqual(nearButOff, AimAssist.Pick(Vector3.zero, Vector3.right, new[] { nearButOff, aligned }).Value);
        }

        [Test]
        public void Cone_Opens_Up_Close_And_Closes_Far_Away()
        {
            var right = Vector3.right;
            Assert.IsTrue(AimAssist.Pick(Vector3.zero, right, new[] { Quaternion.Euler(0, 60f, 0) * right * 1.5f }).HasValue, "colado a 60 graus: pega");
            Assert.IsFalse(AimAssist.Pick(Vector3.zero, right, new[] { Quaternion.Euler(0, 60f, 0) * right * 7f }).HasValue, "longe a 60 graus: nao");
            // o muito torto perto perde para o alinhado a media distancia
            var aligned = new Vector3(5f, 0f, 0f);
            Assert.AreEqual(aligned, AimAssist.Pick(Vector3.zero, right, new[] { Quaternion.Euler(0, 65f, 0) * right * 2f, aligned }).Value);
        }

        [Test]
        public void Idle_Pick_Finds_The_Nearest_In_Front_But_Not_Behind()
        {
            var forward = Vector3.forward;
            Vector3 side = new(2f, 0f, 0.3f), behind = new(0f, 0f, -1.5f), far = new(0f, 0f, 8f);
            Assert.AreEqual(side, AimAssist.Pick(Vector3.zero, forward, new[] { side, far }, AimAssist.IdleAngle, AimAssist.IdleDistance).Value);
            Assert.IsFalse(AimAssist.Pick(Vector3.zero, forward, new[] { behind, far }, AimAssist.IdleAngle, AimAssist.IdleDistance).HasValue);
        }
    }
}
