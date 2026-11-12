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
        /// Nivel de Ascensao da proxima run (0 = normal).
        public static int Ascension;

        /// Run salva que a proxima cena de run deve retomar (nula = run nova).
        public static RunSave Resume;

        /// Retoma a run salva, do comeco da sala em que ela parou. Sem save valido, nao faz nada.
        public static bool ContinueRun()
        {
            var save = RunSaveStore.Load();
            if (save == null) return false;
            Resume = save;
            StartElement = (ElementType)save.hero;
            Seed = save.seed; Layers = save.layers; Shrines = save.shrines; Ascension = save.ascension;
            Tutorial = false;
            GameClock.SetPaused(false);
            SceneManager.LoadScene(RunScene);
            return true;
        }

        public static void StartRun(ElementType element)
        {
            // run nova descarta a salva
            Resume = null;
            RunSaveStore.Clear();
            StartElement = element;
            Tutorial = false;
            Seed = Random.Range(1, int.MaxValue);
            GameClock.SetPaused(false);
            SceneManager.LoadScene(RunScene);
        }

        /// Treino com o heroi dado (o da ultima carta em foco no titulo).
        public static void StartTutorial() => StartTutorial(ElementType.Fire);

        public static void StartTutorial(ElementType hero)
        {
            StartElement = hero == ElementType.None ? ElementType.Fire : hero;
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
