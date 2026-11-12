using Quintessence.Run;
using Quintessence.UI;
using UnityEngine;

namespace Quintessence.Game
{
    /// Cena de titulo: liga o menu ao inicio da run e acende no altar o pedestal do elemento sob o mouse.
    public class TitleController : MonoBehaviour
    {
        const float Dim = 1.4f, Lit = 10f;
        TitleMenu menu;
        readonly Light[] lights = new Light[4];
        Light quint;
        Camera cam;
        Vector3 camHome;
        readonly Animator[] heroes = new Animator[4];
        int lastHovered = -1;
        float cheerLeft;
        /// Heroi que esta comemorando agora (para os testes).
        public int Cheering { get; private set; } = -1;

        void Start()
        {
            menu = FindFirstObjectByType<TitleMenu>();
            if (menu == null) menu = new GameObject("TitleMenu").AddComponent<TitleMenu>();
            menu.Ascension = Ascension.Clamp(GameFlow.Ascension, MetaStore.Current.Wins);
            menu.OnPick = e => { GameFlow.Ascension = Ascension.Clamp(menu.Ascension, MetaStore.Current.Wins); GameFlow.StartRun(e); };
            menu.OnTutorial = GameFlow.StartTutorial;
            var save = RunSaveStore.Load();
            if (save != null) menu.ShowContinue(save.Summary, () => GameFlow.ContinueRun());
            menu.BindMeta(MetaStore.Current, MetaStore.Save, MetaStore.Reset, () => MetaStore.Current);
            for (int i = 0; i < 4; i++)
            {
                var go = GameObject.Find("ElementLight" + i);
                lights[i] = go != null ? go.GetComponent<Light>() : null;
            }
            var q = GameObject.Find("QuintLight");
            quint = q != null ? q.GetComponent<Light>() : null;
            cam = Camera.main;
            if (cam != null) camHome = cam.transform.position;
            for (int i = 0; i < 4; i++)
            {
                var h = GameObject.Find("TitleHero" + i);
                heroes[i] = h != null ? h.GetComponent<Animator>() : null;
            }
        }

        /// Intensidade atual da luz de um pedestal (para os testes).
        public float LightOf(int element) => lights[element] != null ? lights[element].intensity : 0f;

        void Update()
        {
            if (menu == null) return;
            int hovered = menu.HoveredElement;
            float dt = Time.unscaledDeltaTime;
            // o heroi da carta sob o mouse comemora uma vez; depois volta a esperar
            if (hovered != lastHovered)
            {
                if (lastHovered >= 0 && heroes[lastHovered] != null) heroes[lastHovered].CrossFadeInFixedTime("Idle", 0.2f);
                if (hovered >= 0 && heroes[hovered] != null) { heroes[hovered].CrossFadeInFixedTime("Cheer", 0.15f); cheerLeft = 1.6f; }
                Cheering = hovered;
                lastHovered = hovered;
            }
            if (cheerLeft > 0f && (cheerLeft -= dt) <= 0f && Cheering >= 0 && heroes[Cheering] != null)
                heroes[Cheering].CrossFadeInFixedTime("Idle", 0.25f);
            for (int i = 0; i < 4; i++)
                if (lights[i] != null) lights[i].intensity = Mathf.Lerp(lights[i].intensity, i == hovered ? Lit : Dim, 1f - Mathf.Exp(-8f * dt));
            // o quinto respira sozinho: e o que ainda falta alcancar
            if (quint != null) quint.intensity = 0.9f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f);
            // a camera balanca devagar, para a tela nao ficar parada
            if (cam != null) cam.transform.position = camHome + new Vector3(Mathf.Sin(Time.unscaledTime * 0.25f) * 0.35f, Mathf.Sin(Time.unscaledTime * 0.19f) * 0.12f, 0f);
        }
    }
}
