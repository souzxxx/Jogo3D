using NUnit.Framework;

namespace Quintessence.Enemies.Tests
{
    public class EnemyFsmTests
    {
        static EnemyFsm Ready()
        {
            var f = new EnemyFsm(0.5f, 0.4f, 1.4f, 2f, 0.7f);
            f.Tick(0.8f, 10f, true);
            Assert.AreEqual(EnemyPhase.Chase, f.Phase);
            return f;
        }

        [Test] public void DoesNothingWhileSpawning()
        {
            var f = new EnemyFsm(0.5f, 0.4f, 1.4f, 2f, 0.7f);
            Assert.AreEqual(EnemyAction.None, f.Tick(0.3f, 1f, true));
            Assert.IsFalse(f.CanMove);
        }

        [Test] public void TelegraphsBeforeStriking()
        {
            var f = Ready();
            Assert.AreEqual(EnemyAction.None, f.Tick(0.1f, 5f, true), "longe demais");
            Assert.AreEqual(EnemyAction.StartWindup, f.Tick(0.1f, 1.5f, true));
            Assert.IsFalse(f.CanMove, "parado enquanto avisa");
            Assert.AreEqual(EnemyAction.None, f.Tick(0.4f, 1.5f, true));
            Assert.AreEqual(EnemyAction.Strike, f.Tick(0.15f, 1.5f, true));
        }

        [Test] public void StrikeHappensEvenIfHeroLeft_ButCooldownThenApplies()
        {
            var f = Ready();
            f.Tick(0.1f, 1f, true);
            Assert.AreEqual(EnemyAction.Strike, f.Tick(0.6f, 50f, true), "o golpe avisado sai mesmo se o heroi fugiu");
            f.Tick(0.5f, 1f, true);
            Assert.AreEqual(EnemyPhase.Chase, f.Phase);
            Assert.AreEqual(EnemyAction.None, f.Tick(0.1f, 1f, true), "em recarga");
            f.Tick(1f, 50f, true);
            Assert.AreEqual(EnemyAction.StartWindup, f.Tick(0.1f, 1f, true));
        }

        [Test] public void StunDuringWindupCancelsTheAttack()
        {
            var f = Ready();
            f.Tick(0.1f, 1f, true);
            Assert.AreEqual(EnemyAction.Cancel, f.Tick(0.1f, 1f, false));
            Assert.AreEqual(EnemyAction.None, f.Tick(0.1f, 1f, false), "atordoado nao comeca golpe");
        }

        [Test] public void DeadEnemyNeverActs()
        {
            var f = Ready();
            f.Tick(0.1f, 1f, true);
            Assert.IsTrue(f.Kill(), "morreu no meio do aviso");
            Assert.AreEqual(EnemyAction.None, f.Tick(1f, 1f, true));
        }
    }

    public class WaveStateTests
    {
        [Test] public void NothingBeforeStart()
        {
            var w = new WaveState(new[] { 2 });
            Assert.AreEqual(-1, w.Tick(1f));
        }

        [Test] public void NextWaveOnlyAfterPreviousDiesAndDelay()
        {
            var w = new WaveState(new[] { 2, 3 }, 1f);
            w.Start();
            Assert.AreEqual(0, w.Tick(0.1f));
            Assert.AreEqual(-1, w.Tick(5f), "ainda ha inimigos vivos");
            w.NotifyDeath(); w.NotifyDeath();
            Assert.AreEqual(-1, w.Tick(0.5f), "intervalo entre ondas");
            Assert.AreEqual(1, w.Tick(0.6f));
            Assert.AreEqual(3, w.Alive);
            Assert.IsFalse(w.Cleared);
        }

        [Test] public void ClearedWhenLastWaveDies()
        {
            var w = new WaveState(new[] { 1 }, 1f);
            w.Start();
            w.Tick(0.1f);
            w.NotifyDeath();
            w.Tick(0.1f);
            Assert.IsTrue(w.Cleared);
            Assert.AreEqual(-1, w.Tick(10f));
        }

        [Test] public void ExtraDeathsDoNotBreakTheCount()
        {
            var w = new WaveState(new[] { 1, 1 }, 0f);
            w.Start(); w.Tick(0.1f);
            w.NotifyDeath(); w.NotifyDeath(); w.NotifyDeath();
            Assert.AreEqual(1, w.Tick(0.1f));
            Assert.AreEqual(1, w.Alive);
        }

        [Test] public void WaveState_CountsEnemiesBornMidWave()
        {
            var w = new WaveState(new[] { 2 }, 0f);
            w.Start();
            Assert.AreEqual(0, w.Tick(0.1f));
            w.NotifyDeath();
            w.AddAlive(2); // um Divisor caiu e virou dois
            w.NotifyDeath();
            Assert.AreEqual(-1, w.Tick(0.1f));
            Assert.IsFalse(w.Cleared, "ainda ha dois vivos");
            w.NotifyDeath(); w.NotifyDeath();
            w.Tick(0.1f);
            Assert.IsTrue(w.Cleared);
            w.AddAlive(3);
            Assert.AreEqual(0, w.Alive, "sala limpa nao reabre");
        }

        [Test] public void Siege_RepeatsWaves_UntilForcedClear()
        {
            var w = new WaveState(new[] { 1, 2 }, 0f) { Repeat = true };
            w.Start();
            Assert.AreEqual(0, w.Tick(0.1f)); w.NotifyDeath();
            Assert.AreEqual(1, w.Tick(0.1f)); w.NotifyDeath(); w.NotifyDeath();
            Assert.AreEqual(0, w.Tick(0.1f), "depois da ultima, volta para a primeira");
            Assert.IsFalse(w.Cleared);
            Assert.AreEqual(3, w.Spawned);
            w.ForceClear();
            Assert.IsTrue(w.Cleared);
            Assert.AreEqual(0, w.Alive);
            Assert.AreEqual(-1, w.Tick(0.1f), "encerrado, nada mais nasce");
            var empty = new WaveState(new int[0]) { Repeat = true };
            empty.Start();
            Assert.AreEqual(-1, empty.Tick(0.1f));
            Assert.IsTrue(empty.Cleared, "sala sem onda limpa na hora, mesmo no Cerco");
        }
    }
}
