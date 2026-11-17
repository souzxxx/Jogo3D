#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Run;
using Quintessence.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Game.PlayTests
{
    /// Rubrica 4 (estados e erros): ida e volta entre titulo, run, pausa, resultado e treino sem ficar preso,
    /// inclusive nos momentos ocupados (cartao de capitulo, troca de sala).
    public class FlowTests
    {
        const string Key = "quintessence.meta.flowtest";

        [SetUp]
        public void UseTestSave()
        {
            PlayerPrefs.DeleteKey(Key);
            MetaStore.UseKey(Key);
            RunSaveStore.UseKey(Key + ".run");
            RunSaveStore.Clear();
            Tips.UseKey(Key + ".tips");
            Tips.ResetAll();
            GameFlow.Resume = null;
            GameFlow.AutoPickTalents = true;
            HealthPool.ForceEvade = false;
            GamePrefs.Force = false;
        }

        [TearDown]
        public void RestoreSave()
        {
            PlayerPrefs.DeleteKey(Key);
            RunSaveStore.Clear();
            MetaStore.UseKey(MetaStore.DefaultKey);
            RunSaveStore.UseKey(RunSaveStore.DefaultKey);
            Tips.ResetAll();
            Tips.UseKey(Tips.DefaultKey);
            GameFlow.Resume = null;
            // o teste termina passando pelo treino: os outros testes abrem a cena de run direto e herdariam o modo treino
            GameFlow.Tutorial = false;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Ascension = 0;
            GameFlow.AutoPickTalents = false;
            HealthPool.ForceEvade = null;
            GamePrefs.Force = null;
            GameClock.SetPaused(false);
            Time.timeScale = 1f;
            Time.captureFramerate = 0;
        }

        static IEnumerator WaitFor(System.Func<bool> done, float seconds, string what)
        {
            // tempo real: o resultado e as trocas de cena esperam em tempo real, e gravando quadro a quadro o jogo anda mais rapido que ele
            for (float t = seconds; t > 0f && !done(); t -= Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(done(), what);
        }

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>();

        [UnityTest, Timeout(600000)]
        public IEnumerator Exits_And_Returns_Never_Strand_The_Player()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 5; GameFlow.Layers = 5; GameFlow.Shrines = 1;

            // 1) morre durante o cartao de capitulo da primeira sala (diretor ocupado): o resultado aparece mesmo assim
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var director = Find<RunDirector>();
            var hud = Find<Hud>();
            var hero = Find<PlayerController>().GetComponent<Health>();
            for (int k = 0; k < 6; k++) yield return null;
            Assert.IsTrue(director.Busy, "a primeira sala ainda deveria estar abrindo (cartao de capitulo)");
            hero.TakeDamage(new DamageInfo(99999f, ElementType.None, Team.Enemy));
            yield return WaitFor(() => hud.ResultVisible, 8f, "morrer durante a troca de sala deveria levar ao resultado");
            Assert.IsTrue(director.Finished);
            Assert.IsNull(RunSaveStore.Load(), "run encerrada nao deixa Continuar para tras");

            // 2) Menu principal no resultado: o titulo abre, sem Continuar, e escolher um heroi comeca uma run nova
            GameFlow.ToTitle();
            yield return WaitFor(() => Find<TitleMenu>() != null, 8f, "o titulo deveria abrir");
            for (int k = 0; k < 3; k++) yield return null;
            Assert.IsNull(GameObject.Find("Continue"), "sem run salva, o titulo nao oferece Continuar");
            Assert.IsFalse(GameClock.Paused);
            Assert.AreEqual(1f, Time.timeScale);
            Find<TitleMenu>().OnPick(ElementType.Water);
            yield return WaitFor(() => Find<RunDirector>() != null && !Find<RunDirector>().Busy, 12f, "a run nova deveria abrir a primeira sala");
            director = Find<RunDirector>();
            Assert.IsFalse(director.Finished);
            Assert.AreEqual(ElementType.Water, GameFlow.StartElement);

            // 3) pausa no meio da sala e sai pelo menu: o titulo oferece Continuar, que volta a mesma sala com o tempo andando
            int room = director.Run.Depth;
            director.SetPaused(true);
            yield return null;
            Assert.IsTrue(GameClock.Paused);
            Assert.IsTrue(Find<Hud>().PauseVisible);
            GameFlow.ToTitle();
            yield return WaitFor(() => Find<TitleMenu>() != null, 8f, "sair pela pausa deveria abrir o titulo");
            for (int k = 0; k < 3; k++) yield return null;
            Assert.IsFalse(GameClock.Paused, "o titulo nao herda a pausa");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsNotNull(GameObject.Find("Continue"), "com a run salva, o titulo oferece Continuar");
            Assert.IsTrue(GameFlow.ContinueRun());
            yield return WaitFor(() => Find<RunDirector>() != null && !Find<RunDirector>().Busy, 12f, "continuar deveria reabrir a sala");
            director = Find<RunDirector>();
            Assert.AreEqual(room, director.Run.Depth, "continuar volta para a mesma sala");
            Assert.IsFalse(director.Finished);
            Assert.IsFalse(GameClock.Paused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(Find<PlayerController>().GetComponent<Health>().IsDead);

            // 4) treino e volta ao titulo
            GameFlow.StartTutorial(ElementType.Earth);
            yield return WaitFor(() => Find<TutorialDirector>() != null, 12f, "o treino deveria abrir");
            for (int k = 0; k < 10; k++) yield return null;
            GameFlow.ToTitle();
            yield return WaitFor(() => Find<TitleMenu>() != null, 8f, "do treino deveria dar para voltar ao titulo");
            Assert.IsFalse(GameClock.Paused);
            Assert.IsNotNull(GameObject.Find("Continue"), "o treino nao apaga a run salva");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
        }
    }
}
#endif
