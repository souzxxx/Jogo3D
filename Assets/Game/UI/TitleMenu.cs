using System;
using Quintessence.Core;
using Quintessence.Run;
using UnityEngine;

namespace Quintessence.UI
{
    /// Tela de titulo: escolher o elemento inicial comeca a run.
    public class TitleMenu : MonoBehaviour
    {
        public Action<ElementType> OnPick;
        Canvas canvas;
        RectTransform refuge;
        UnityEngine.UI.Text refugeLabel;
        MetaProgress meta;
        Action save, reset;
        Func<MetaProgress> reload;
        bool confirmReset;

        public bool RefugeOpen => refuge != null && refuge.gameObject.activeSelf;

        /// Liga o Refugio ao progresso salvo. save grava depois de cada compra; reset apaga tudo.
        public void BindMeta(MetaProgress progress, Action onSave, Action onReset, Func<MetaProgress> onReload)
        {
            meta = progress; save = onSave; reset = onReset; reload = onReload;
            UpdateRefugeLabel();
        }

        void UpdateRefugeLabel()
        {
            if (refugeLabel != null && meta != null) refugeLabel.text = $"REFÚGIO  ·  {meta.Shards} Fragmentos";
        }

        public void ShowRefuge(bool show)
        {
            confirmReset = false;
            refuge.gameObject.SetActive(show);
            if (show) BuildRefuge();
            UpdateRefugeLabel();
        }

        /// Compra um nivel pelo painel (os botoes chamam isto; os testes tambem).
        public bool Buy(MetaUpgrade u)
        {
            if (meta == null || !meta.TryBuy(u)) { Sfx.Play("block"); return false; }
            Sfx.Play("pickup");
            save?.Invoke();
            if (RefugeOpen) BuildRefuge();
            UpdateRefugeLabel();
            return true;
        }

        void BuildRefuge()
        {
            foreach (Transform child in refuge) Destroy(child.gameObject);
            if (meta == null) return;
            var center = new Vector2(0.5f, 0.5f);
            UiKit.Image(UiKit.Stretch("Shade", refuge), null, new Color(0.02f, 0.02f, 0.04f, 0.95f), true);
            var box = UiKit.Box("Box", refuge, center, Vector2.zero, new Vector2(1180, 860), 1f);
            var title = UiKit.Text(UiKit.Rect("Title", box, new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(900, 70)), "REFÚGIO", 60, UiKit.Paper);
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Sub", box, new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(1000, 36)),
                "Melhorias que ficam para todas as runs. Cada sala vencida rende 3 Fragmentos; vencer o Guardião, mais 15.", 23, UiKit.Dim, TextAnchor.MiddleCenter, false);
            UiKit.Text(UiKit.Rect("Wallet", box, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(900, 40)),
                $"{meta.Shards} Fragmentos   ·   runs: {meta.Runs}   ·   vitórias: {meta.Wins}   ·   recorde: {meta.BestRooms} salas", 27, new Color(0.88f, 0.74f, 0.42f));

            for (int i = 0; i < MetaInfo.Count; i++)
            {
                var u = (MetaUpgrade)i;
                int level = meta.Level(u), max = MetaInfo.MaxLevel(u);
                var row = UiKit.Box("Row" + i, box, new Vector2(0.5f, 1), new Vector2(0, -232 - i * 94), new Vector2(1080, 84), 0.5f);
                var disc = UiKit.Rect("Disc", row, new Vector2(0, 0.5f), new Vector2(52, 0), new Vector2(60, 60));
                UiKit.Image(disc, UiKit.Circle, new Color(0.16f, 0.14f, 0.2f));
                UiKit.Image(UiKit.Rect("Icon", disc, center, Vector2.zero, new Vector2(40, 40)), UiKit.Icon(MetaInfo.Icon(u)), UiKit.Cream);
                UiKit.Text(UiKit.Rect("Name", row, new Vector2(0, 0.5f), new Vector2(104, 16), new Vector2(210, 36)), MetaInfo.Name(u), 32, UiKit.Paper, TextAnchor.MiddleLeft);
                for (int k = 0; k < max; k++)
                    UiKit.Image(UiKit.Rect("Pip" + k, row, new Vector2(0, 0.5f), new Vector2(106 + k * 24, -20), new Vector2(16, 16)),
                        k < level ? UiKit.Circle : UiKit.Ring, k < level ? new Color(0.88f, 0.74f, 0.42f) : UiKit.Dim);
                string now = level > 0 ? "Agora: " + MetaInfo.Describe(u, level) : "Ainda sem efeito";
                string next = level < max ? "Próximo: " + MetaInfo.Describe(u, level + 1) : "Nível máximo";
                UiKit.Text(UiKit.Rect("Now", row, new Vector2(0, 0.5f), new Vector2(330, 16), new Vector2(500, 30)), now, 23, UiKit.Cream, TextAnchor.MiddleLeft, false);
                UiKit.Text(UiKit.Rect("Next", row, new Vector2(0, 0.5f), new Vector2(330, -16), new Vector2(500, 30)), next, 23, UiKit.Dim, TextAnchor.MiddleLeft, false);
                if (level >= max)
                    UiKit.Text(UiKit.Rect("Max", row, new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(210, 50)), "COMPLETO", 24, UiKit.Dim);
                else
                {
                    bool can = meta.CanBuy(u);
                    var b = UiKit.Button(row, $"{meta.Cost(u)} Fragmentos", new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(210, 56),
                        can ? new Color(0.5f, 0.38f, 0.16f) : new Color(0.2f, 0.19f, 0.24f), () => Buy(u));
                    var t = b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
                    t.fontSize = 23;
                    if (!can) t.color = UiKit.Dim;
                }
            }
            UiKit.Button(box, "Voltar", new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(300, 60), new Color(0.24f, 0.4f, 0.55f), () => ShowRefuge(false));
            var wipe = UiKit.Button(box, confirmReset ? "Apagar mesmo?" : "Apagar progresso", new Vector2(1, 0), new Vector2(-50, 62), new Vector2(250, 46),
                confirmReset ? new Color(0.55f, 0.2f, 0.18f) : new Color(0.2f, 0.19f, 0.24f), () =>
                {
                    if (!confirmReset) { confirmReset = true; BuildRefuge(); return; }
                    confirmReset = false;
                    reset?.Invoke();
                    if (reload != null) meta = reload();
                    BuildRefuge();
                    UpdateRefugeLabel();
                });
            wipe.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 21;
        }

        void Awake()
        {
            UiKit.EnsureEventSystem();
            canvas = UiKit.Canvas("TitleCanvas", 10, transform);
            var root = canvas.transform;
            UiKit.Image(UiKit.Stretch("Shade", root), null, new Color(0.03f, 0.02f, 0.06f, 0.55f));
            var logo = UiKit.Text(UiKit.Rect("Title", root, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1600, 150)), "QUINTESSENCE", 128, UiKit.Paper);
            logo.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Sub", root, new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(1400, 50)),
                "Comece com um elemento. Desperte os outros três. Alcance o quinto estado.", 34, UiKit.Dim, TextAnchor.MiddleCenter, false);
            UiKit.Text(UiKit.Rect("Pick", root, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1000, 50)), "ESCOLHA O ELEMENTO INICIAL", 34, UiKit.Paper);

            string[] names = { "Fogo", "Água", "Terra", "Ar" };
            string[] notes = { "Agressivo\ndano contínuo", "Alcance\ne controle", "Pesado\nquebra armadura", "Mobilidade\ne crítico" };
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                var c = ElementColors.Of(e);
                var pos = new Vector2((i - 1.5f) * 300f, -150);
                var b = UiKit.Button(root, names[i], new Vector2(0.5f, 0.5f), pos, new Vector2(260, 260), new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.95f), () => OnPick?.Invoke(e));
                var orb = UiKit.Rect("Orb", b.transform, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(92, 92));
                UiKit.Image(orb, UiKit.Circle, c);
                string[] iconNames = { "fire", "water", "earth", "air" };
                UiKit.Image(UiKit.Rect("Icon", orb, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60)), UiKit.Icon(iconNames[i]), UiKit.Cream);
                var label = (RectTransform)b.transform.Find("Label");
                label.offsetMin = new Vector2(0, 60); label.offsetMax = new Vector2(0, -110);
                label.GetComponent<UnityEngine.UI.Text>().fontSize = 40;
                UiKit.Text(UiKit.Rect("Note", b.transform, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(240, 64)), notes[i], 23, UiKit.Paper, TextAnchor.MiddleCenter, false);
            }
            var open = UiKit.Button(root, "REFÚGIO", new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(520, 62), new Color(0.3f, 0.24f, 0.14f, 0.95f), () => ShowRefuge(true));
            refugeLabel = open.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            refugeLabel.fontSize = 28;
            refuge = UiKit.Stretch("Refuge", root);
            refuge.gameObject.SetActive(false);
            UpdateRefugeLabel();
            UiKit.Text(UiKit.Rect("Keys", root, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1600, 30)),
                "WASD mover · mouse mira · clique ataca · clique dir. skill · Espaço dash · Q/E troca elemento · R Quintessência · F interagir", 22, UiKit.Dim, TextAnchor.MiddleCenter, false);
        }

        public void UseCamera(Camera cam)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }
    }
}
