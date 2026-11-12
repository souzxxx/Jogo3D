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
        public static int Layers = 10;
        public static int Shrines = 3;
        /// A proxima cena de run abre a sala de treino em vez do mapa.
        public static bool Tutorial;
        /// Testes: ao subir de nivel o primeiro talento oferecido e pego sozinho, sem abrir a escolha.
        public static bool AutoPickTalents;

        public static void StartRun(ElementType element)
        {
            StartElement = element;
            Tutorial = false;
            Seed = Random.Range(1, int.MaxValue);
            GameClock.SetPaused(false);
            SceneManager.LoadScene(RunScene);
        }

        public static void StartTutorial()
        {
            StartElement = ElementType.Fire;
            Tutorial = true;
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
}
