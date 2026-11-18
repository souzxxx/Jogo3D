using NUnit.Framework;
using UnityEngine;

namespace Quintessence.Enemies.Tests
{
    /// Rodada 25: no maximo tres inimigos corpo a corpo atacando o heroi ao mesmo tempo; a vez sempre volta.
    public class MeleeTokensTests
    {
        GameObject[] enemies;

        [SetUp]
        public void SetUp()
        {
            MeleeTokens.Clear();
            enemies = new GameObject[5];
            for (int i = 0; i < enemies.Length; i++) enemies[i] = new GameObject("Inimigo" + i);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var e in enemies) if (e != null) Object.DestroyImmediate(e);
            MeleeTokens.Clear();
        }

        [Test]
        public void At_Most_Three_Attack_And_The_Fourth_Waits()
        {
            for (int i = 0; i < MeleeTokens.Max; i++) Assert.IsTrue(MeleeTokens.TryTake(enemies[i]));
            Assert.IsFalse(MeleeTokens.TryTake(enemies[3]), "o quarto espera a vez");
            Assert.AreEqual(1, MeleeTokens.Waits);
            Assert.IsTrue(MeleeTokens.TryTake(enemies[0]), "quem ja tem a vez continua com ela");
            Assert.AreEqual(MeleeTokens.Max, MeleeTokens.Held);
        }

        [Test]
        public void Releasing_After_The_Strike_Lets_The_Next_One_In()
        {
            for (int i = 0; i < MeleeTokens.Max; i++) MeleeTokens.TryTake(enemies[i]);
            MeleeTokens.Release(enemies[1]);
            Assert.IsTrue(MeleeTokens.TryTake(enemies[3]), "a vez devolvida no golpe passa para o proximo");
            Assert.IsFalse(MeleeTokens.Has(enemies[1]));
        }

        [Test]
        public void A_Destroyed_Enemy_Never_Holds_The_Turn_Forever()
        {
            for (int i = 0; i < MeleeTokens.Max; i++) MeleeTokens.TryTake(enemies[i]);
            Object.DestroyImmediate(enemies[2]);
            Assert.IsTrue(MeleeTokens.TryTake(enemies[4]), "inimigo destruido sem devolver nao segura a vez");
        }

        [Test]
        public void Clear_Frees_Every_Turn()
        {
            for (int i = 0; i < MeleeTokens.Max; i++) MeleeTokens.TryTake(enemies[i]);
            MeleeTokens.Clear();
            Assert.AreEqual(0, MeleeTokens.Held);
            Assert.IsTrue(MeleeTokens.TryTake(enemies[3]));
        }
    }
}
