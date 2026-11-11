using Quintessence.Core;
using Quintessence.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quintessence.Game
{
    /// Fluxo entre as cenas e a configuracao da proxima run.
    public static class GameFlow
    {
        public const string TitleScene = "Title";
        public const string RunScene = "Run";

        public static ElementType StartElement = ElementType.Fire;
        public static int Seed = 1;
        public static int Layers = 8;
        public static int Shrines = 3;

        public static void StartRun(ElementType element)
        {
            StartElement = element;
            Seed = Random.Range(1, int.MaxValue);
            GameClock.SetPaused(false);
            SceneManager.LoadScene(RunScene);
        }

        public static void RestartRun() => StartRun(StartElement);

        public static void ToTitle()
        {
            GameClock.SetPaused(false);
            SceneManager.LoadScene(TitleScene);
        }
    }

    /// Cena de titulo: liga o menu ao inicio da run.
    public class TitleController : MonoBehaviour
    {
        void Start()
        {
            var menu = FindFirstObjectByType<TitleMenu>();
            if (menu == null) menu = new GameObject("TitleMenu").AddComponent<TitleMenu>();
            menu.OnPick = GameFlow.StartRun;
        }
    }
}
