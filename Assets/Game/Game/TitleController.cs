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

        void Start()
        {
            menu = FindFirstObjectByType<TitleMenu>();
            if (menu == null) menu = new GameObject("TitleMenu").AddComponent<TitleMenu>();
            menu.Ascension = Ascension.Clamp(GameFlow.Ascension, MetaStore.Current.Wins);
            menu.OnPick = e => { GameFlow.Ascension = Ascension.Clamp(menu.Ascension, MetaStore.Current.Wins); GameFlow.StartRun(e); };
            menu.OnTutorial = GameFlow.StartTutorial;
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
        }

        /// Intensidade atual da luz de um pedestal (para os testes).
        public float LightOf(int element) => lights[element] != null ? lights[element].intensity : 0f;

        void Update()
        {
            if (menu == null) return;
            int hovered = menu.HoveredElement;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < 4; i++)
                if (lights[i] != null) lights[i].intensity = Mathf.Lerp(lights[i].intensity, i == hovered ? Lit : Dim, 1f - Mathf.Exp(-8f * dt));
            // o quinto respira sozinho: e o que ainda falta alcancar
            if (quint != null) quint.intensity = 0.9f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f);
            // a camera balanca devagar, para a tela nao ficar parada
            if (cam != null) cam.transform.position = camHome + new Vector3(Mathf.Sin(Time.unscaledTime * 0.25f) * 0.35f, Mathf.Sin(Time.unscaledTime * 0.19f) * 0.12f, 0f);
        }
    }
}
