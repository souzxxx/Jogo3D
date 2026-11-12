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
        public Action OnTutorial;
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

        RectTransform sheet, quintPip;
        readonly UiHover[] cards = new UiHover[4];

        /// Carta de elemento sob o mouse (0 a 3), ou -1. A cena usa isto para acender o pedestal daquele elemento.
        public int HoveredElement
        {
            get
            {
                if (RefugeOpen || SheetOpen) return -1;
                for (int i = 0; i < cards.Length; i++) if (cards[i] != null && cards[i].Over) return i;
                return -1;
            }
        }

        void Start() => FocusFirstCard();
        void FocusFirstCard() { if (cards[0] != null) UiKit.Focus(cards[0].gameObject); }

        void Update()
        {
            if (quintPip == null) return;
            // o quinto ponto do emblema respira devagar
            float k = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 1.6f);
            quintPip.localScale = new Vector3(k, k, 1f);
        }

        public bool SheetOpen => sheet != null && sheet.gameObject.activeSelf;

        /// Painel simples por cima do titulo (opcoes, creditos): titulo, conteudo e Voltar.
        RectTransform OpenSheet(string heading, float height)
        {
            foreach (Transform child in sheet) Destroy(child.gameObject);
            sheet.gameObject.SetActive(true);
            UiKit.Image(UiKit.Stretch("Shade", sheet), null, new Color(0.02f, 0.02f, 0.04f, 0.95f), true);
            var box = UiKit.Box("Box", sheet, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, height), 1f);
            var title = UiKit.Text(UiKit.Rect("Title", box, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 70)), heading, 56, UiKit.Paper);
            title.font = GameFonts.Title;
            UiKit.Focus(UiKit.Button(box, "Voltar", new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(300, 60), UiKit.Primary, () => { sheet.gameObject.SetActive(false); FocusFirstCard(); }).gameObject);
            return box;
        }

        public void ShowOptions()
        {
            var box = OpenSheet("OPÇÕES", 560);
            UiOptions.Build(box, new Vector2(0, 10));
        }

        public void ShowCredits()
        {
            var box = OpenSheet("CRÉDITOS", 760);
            void Line(float y, string text, int size, Color color, bool bold = false) =>
                UiKit.Text(UiKit.Rect("Line", box, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(920, size + 10)), text, size, color, TextAnchor.MiddleCenter, bold);
            Line(-130, "Quintessence 3D", 34, new Color(0.88f, 0.74f, 0.42f), true);
            Line(-172, "Projeto de Jogos Digitais e Interação · Insper", 24, UiKit.Dim);
            Line(-236, "EQUIPE", 24, UiKit.Dim, true);
            Line(-274, "Leonardo de Souza · combate e elementos", 26, UiKit.Cream);
            Line(-308, "Enzo Canavero · inimigos, chefe e salas", 26, UiKit.Cream);
            Line(-342, "Nicolas Volf · run, progresso e interface", 26, UiKit.Cream);
            Line(-376, "Miguel Damasio · visual, feedback e som", 26, UiKit.Cream);
            Line(-440, "ASSETS", 24, UiKit.Dim, true);
            Line(-478, "Modelos e animações: KayKit, de Kay Lousberg (CC0)", 24, UiKit.Cream);
            Line(-510, "Ícones: game-icons.net, de Lorc, Delapouite e outros (CC BY 3.0)", 24, UiKit.Cream);
            Line(-542, "Fontes: Lilita One, Titillium Web e Cinzel Decorative (OFL)", 24, UiKit.Cream);
            Line(-574, "Efeitos sonoros e música: sintetizados pela equipe", 24, UiKit.Cream);
            Line(-622, "Feito com Unity 6", 22, UiKit.Dim);
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
                var disc = UiKit.Rect("Disc", row, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(60, 60));
                UiKit.Image(disc, UiKit.Circle, new Color(0.16f, 0.14f, 0.2f));
                UiKit.Image(UiKit.Rect("Icon", disc, center, Vector2.zero, new Vector2(40, 40)), UiKit.Icon(MetaInfo.Icon(u)), UiKit.Cream);
                UiKit.Text(UiKit.Rect("Name", row, new Vector2(0, 0.5f), new Vector2(92, 16), new Vector2(210, 36)), MetaInfo.Name(u), 32, UiKit.Paper, TextAnchor.MiddleLeft);
                for (int k = 0; k < max; k++)
                    UiKit.Image(UiKit.Rect("Pip" + k, row, new Vector2(0, 0.5f), new Vector2(94 + k * 24, -20), new Vector2(16, 16)),
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
                        can ? UiKit.Primary : UiKit.Disabled, () => Buy(u));
                    var t = b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
                    t.fontSize = 23;
                    if (!can) t.color = UiKit.Dim;
                }
            }
            UiKit.Focus(UiKit.Button(box, "Voltar", new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(300, 60), UiKit.Primary, () => { ShowRefuge(false); FocusFirstCard(); }).gameObject);
            var wipe = UiKit.Button(box, confirmReset ? "Apagar mesmo?" : "Apagar progresso", new Vector2(1, 0), new Vector2(-50, 62), new Vector2(250, 46),
                confirmReset ? UiKit.Danger : UiKit.Secondary, () =>
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
            // escuro so em cima e embaixo: o altar dos cinco aparece no meio da tela
            UiKit.Image(UiKit.Stretch("Shade", root), UiKit.VerticalVignette, new Color(0.03f, 0.02f, 0.06f, 1f));
            var logo = UiKit.Text(UiKit.Rect("Title", root, new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1700, 150)), "QUINTESSENCE", 124, UiKit.Paper);
            logo.font = GameFonts.Title;
            // emblema: os quatro elementos em volta do quinto, ligados por um fio
            var emblem = UiKit.Rect("Emblem", root, new Vector2(0.5f, 1), new Vector2(0, -186), new Vector2(520, 44));
            UiKit.Image(UiKit.Rect("Thread", emblem, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 2)), null, new Color(0.93f, 0.88f, 0.78f, 0.35f));
            float[] xs = { -150, -75, 75, 150 };
            for (int i = 0; i < 4; i++)
            {
                var pip = UiKit.Rect("Pip" + i, emblem, new Vector2(0.5f, 0.5f), new Vector2(xs[i], 0), new Vector2(20, 20));
                UiKit.Image(pip, UiKit.Circle, Color.Lerp(ElementColors.Of(Elements.All[i]), UiKit.Cream, 0.15f));
            }
            quintPip = UiKit.Rect("Quint", emblem, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38));
            UiKit.Image(quintPip, UiKit.Ring, UiKit.Paper);
            UiKit.Image(UiKit.Rect("Core", quintPip, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16)), UiKit.Circle, UiKit.Paper);
            UiKit.Text(UiKit.Rect("Sub", root, new Vector2(0.5f, 1), new Vector2(0, -222), new Vector2(1400, 44)),
                "Quatro heróis, quatro elementos. Desperte os outros três e alcance o quinto estado.", 28, UiKit.Cream, TextAnchor.MiddleCenter, false);
            UiKit.Text(UiKit.Rect("Pick", root, new Vector2(0.5f, 0.5f), new Vector2(0, -138), new Vector2(1000, 40)), "ESCOLHA SEU HERÓI", 26, UiKit.Dim);

            string[] names = { "Rax", "Liora", "Brann", "Vey" };
            string[] notes = { "Fogo · garras rápidas\n+15% no combo básico", "Água · chicote longo\ncura ao limpar a sala", "Terra · martelo pesado\n+20 de vida máxima", "Ar · adagas velozes\nmais veloz, dash rápido" };
            string[] iconNames = { "fire", "water", "earth", "air" };
            var mid = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                var c = ElementColors.Of(e);
                // carta escura com o elemento so no contorno, no medalhao e no filete: a cor entra como acento
                var body = Color.Lerp(new Color(0.09f, 0.08f, 0.12f), c, 0.16f);
                var b = UiKit.Button(root, "", mid, new Vector2((i - 1.5f) * 290f, -268), new Vector2(256, 216), body, () => OnPick?.Invoke(e), Color.Lerp(c, UiKit.Cream, 0.15f));
                b.name = "Card_" + names[i];
                b.GetComponent<UiHover>().hoverScale = 1.06f;
                cards[i] = b.GetComponent<UiHover>();
                Destroy(b.transform.Find("Sheen").gameObject); // o brilho de botao nao cabe numa carta alta
                var card = b.transform;
                UiKit.Image(UiKit.Rect("Halo", card, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(84, 84)), UiKit.Circle, new Color(c.r, c.g, c.b, 0.16f));
                var orb = UiKit.Rect("Orb", card, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(72, 72));
                UiKit.Image(orb, UiKit.Circle, Color.Lerp(c, new Color(0.05f, 0.04f, 0.08f), 0.35f));
                UiKit.Image(UiKit.Stretch("Rim", orb), UiKit.Ring, Color.Lerp(c, UiKit.Cream, 0.45f));
                UiKit.Image(UiKit.Rect("Icon", orb, mid, Vector2.zero, new Vector2(44, 44)), UiKit.Icon(iconNames[i]), UiKit.Cream);
                UiKit.Text(UiKit.Rect("Name", card, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(240, 48)), names[i], 38, UiKit.Cream);
                UiKit.Image(UiKit.Rect("Rule", card, new Vector2(0.5f, 1), new Vector2(0, -148), new Vector2(72, 4)), null, Color.Lerp(c, UiKit.Cream, 0.2f));
                UiKit.Text(UiKit.Rect("Note", card, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(240, 58)), notes[i], 21, UiKit.Paper, TextAnchor.MiddleCenter, false);
            }
            var open = UiKit.Button(root, "REFÚGIO", new Vector2(0.5f, 0), new Vector2(-330, 74), new Vector2(400, 58), UiKit.Primary, () => ShowRefuge(true));
            void Small(string label, float x, Action act) =>
                UiKit.Button(root, label, new Vector2(0.5f, 0), new Vector2(x, 74), new Vector2(200, 58), UiKit.Secondary, act)
                    .transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 26;
            Small("TREINO", -10, () => OnTutorial?.Invoke());
            Small("OPÇÕES", 210, ShowOptions);
            Small("CRÉDITOS", 430, ShowCredits);
            refugeLabel = open.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            refugeLabel.fontSize = 26;
            refuge = UiKit.Stretch("Refuge", root);
            refuge.gameObject.SetActive(false);
            sheet = UiKit.Stretch("Sheet", root);
            sheet.gameObject.SetActive(false);
            UpdateRefugeLabel();
            UiKit.Text(UiKit.Rect("Keys", root, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1600, 30)),
                "WASD mover · mouse mira · clique ataca · 1 2 3 4 skills · Espaço dash · R Quintessência · E interagir · I inventário  —  também funciona com controle", 22, UiKit.Dim, TextAnchor.MiddleCenter, false);
        }

        public void UseCamera(Camera cam)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }
    }
}
