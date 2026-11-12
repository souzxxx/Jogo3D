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
        /// Nivel de Ascensao escolhido no titulo.
        public int Ascension { get; set; }

        public void BindMeta(MetaProgress progress, Action onSave, Action onReset, Func<MetaProgress> onReload)
        {
            meta = progress; save = onSave; reset = onReset; reload = onReload;
            UpdateRefugeLabel();
            BuildAscension();
        }

        /// Botao de continuar a run salva, no canto de cima. label: heroi e sala.
        /// Ha uma run salva (o Refugio avisa que as compras so valem na proxima run nova).
        public bool RunInProgress { get; private set; }

        public void ShowContinue(string label, Action onContinue)
        {
            if (canvas == null) return;
            var root = canvas.transform;
            var old = root.Find("Continue");
            if (old != null) Destroy(old.gameObject);
            if (onContinue == null) return;
            RunInProgress = true;
            var box = UiKit.Rect("Continue", root, new Vector2(1, 1), new Vector2(-150, -150), new Vector2(290, 96));
            var b = UiKit.Button(box, "CONTINUAR RUN", new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(284, 60), UiKit.Primary, onContinue);
            b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 26;
            UiKit.Text(UiKit.Rect("Info", box, new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(300, 28)), label, 19, UiKit.Cream, TextAnchor.MiddleCenter, false);
            // abaixo do Refugio e das folhas: aberto um painel, o botao nao fica por cima
            if (refuge != null) box.SetSiblingIndex(refuge.GetSiblingIndex());
        }

        /// Seletor de Ascensao: aparece depois da primeira vitoria (precisa do save, por isso nasce no BindMeta).
        void BuildAscension()
        {
            if (canvas == null) return;
            var root = canvas.transform;
            var old = root.Find("Ascension");
            if (old != null) Destroy(old.gameObject);
            var mid = new Vector2(0.5f, 0.5f);
            int unlocked = meta != null ? Quintessence.Run.Ascension.Unlocked(meta.Wins) : 0;
            if (unlocked <= 0) { Ascension = 0; return; }
            Ascension = Mathf.Clamp(Ascension, 0, unlocked);
            var asc = UiKit.Rect("Ascension", root, new Vector2(0.5f, 0), new Vector2(0, 122), new Vector2(860, 32));
            if (refuge != null) asc.SetSiblingIndex(refuge.GetSiblingIndex());
            var label = UiKit.Text(UiKit.Rect("Label", asc, mid, Vector2.zero, new Vector2(740, 30)), "", 20, UiKit.Cream, TextAnchor.MiddleCenter, false);
            void Refresh() => label.text = $"ASCENSÃO {Ascension}/{unlocked}  ·  {Quintessence.Run.Ascension.Describe(Ascension)}";
            UiKit.Button(asc, "−", new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(44, 30), UiKit.Secondary, () => { Ascension = Mathf.Max(0, Ascension - 1); Refresh(); });
            UiKit.Button(asc, "+", new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(44, 30), UiKit.Secondary, () => { Ascension = Mathf.Min(unlocked, Ascension + 1); Refresh(); });
            Refresh();
        }

        readonly UnityEngine.UI.Text[] heroWinLabels = new UnityEngine.UI.Text[4];

        void UpdateRefugeLabel()
        {
            // vitorias de cada heroi no canto da carta dele
            for (int i = 0; i < heroWinLabels.Length; i++)
                if (heroWinLabels[i] != null && meta != null)
                {
                    int w = meta.HeroWins(Elements.All[i]);
                    heroWinLabels[i].text = w > 0 ? (w == 1 ? "1 vitória" : $"{w} vitórias") : "";
                }
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
        RectTransform OpenSheet(string heading, float height, float width = 1000f)
        {
            foreach (Transform child in sheet) Destroy(child.gameObject);
            sheet.gameObject.SetActive(true);
            UiKit.Image(UiKit.Stretch("Shade", sheet), null, new Color(0.02f, 0.02f, 0.04f, 0.95f), true);
            var box = UiKit.Box("Box", sheet, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height), 1f);
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

        /// Grimorio: herois, inimigos (e como lidar com cada um) e as seis reacoes.
        public void ShowGrimoire()
        {
            var box = OpenSheet("GRIMÓRIO", 940, 1640);
            var top = new Vector2(0, 1);
            void Column(float x, string heading, (string name, string text, Color color)[] rows, float step)
            {
                UiKit.Text(UiKit.Rect("H_" + heading, box, top, new Vector2(x, -120), new Vector2(500, 32)), heading, 24, UiKit.Dim, TextAnchor.MiddleLeft);
                for (int i = 0; i < rows.Length; i++)
                {
                    float y = -160 - i * step;
                    UiKit.Text(UiKit.Rect("N", box, top, new Vector2(x, y), new Vector2(500, 30)), rows[i].name, 23, rows[i].color, TextAnchor.MiddleLeft);
                    var t = UiKit.Text(UiKit.Rect("T", box, top, new Vector2(x, y - 30), new Vector2(500, step - 30)), rows[i].text, 18, UiKit.Paper, TextAnchor.UpperLeft, false);
                    t.horizontalOverflow = HorizontalWrapMode.Wrap;
                }
            }
            string[] element = { "", "Fogo", "Água", "Terra", "Ar" };
            var heroes = new (string, string, Color)[4];
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                heroes[i] = ($"{HeroPassives.HeroName(e)} · {element[(int)e]}",
                    $"{HeroPassives.Style(e)}. {HeroPassives.Title(e)}: {HeroPassives.Describe(e)}.", Color.Lerp(ElementColors.Of(e), UiKit.Cream, 0.35f));
            }
            var foe = new Color(0.86f, 0.6f, 0.5f);
            var gold = new Color(0.88f, 0.74f, 0.42f);
            Column(70, "HERÓIS", heroes, 92);
            Column(600, "INIMIGOS", new (string, string, Color)[]
            {
                ("Rastejante", "Vem em grupo. Área e reações limpam o bando.", foe),
                ("Atirador", "Tiro em linha; recua se você chega perto. Feche com o dash.", foe),
                ("Bruto", "Armadura: leva 40% do dano. O Pilar de Pedra quebra.", foe),
                ("Explosivo", "Corre e explode. Saia do círculo; a explosão fere o bando.", foe),
                ("Ceifador", "Avisa uma linha e atravessa a sala. Saia e bata na volta.", foe),
                ("Arremessador", "Joga a bomba onde você está. Saia do círculo.", foe),
                ("Xamã", "Cura os aliados feridos (aviso verde). Derrube primeiro.", foe),
                ("Campeão, Carrasco e chefes", "Fecham as Elites. No fim: o Guardião (corpo a corpo) ou a Tecelã (de longe).", gold),
            }, 74);
            Column(1130, "REAÇÕES", new (string, string, Color)[]
            {
                ("Vapor · Fogo + Água", "Nuvem: quem está dentro erra metade dos ataques.", Color.Lerp(ElementColors.Of(ElementType.Fire), ElementColors.Of(ElementType.Water), 0.5f)),
                ("Magma · Fogo + Terra", "Poça de lava que queima por 4 segundos.", Color.Lerp(ElementColors.Of(ElementType.Fire), ElementColors.Of(ElementType.Earth), 0.5f)),
                ("Tempestade de Fogo · Fogo + Ar", "A maior explosão; deixa todos Queimando.", Color.Lerp(ElementColors.Of(ElementType.Fire), ElementColors.Of(ElementType.Air), 0.5f)),
                ("Lama · Água + Terra", "Enraíza e encharca: ninguém sai do lugar.", Color.Lerp(ElementColors.Of(ElementType.Water), ElementColors.Of(ElementType.Earth), 0.5f)),
                ("Nevasca · Água + Ar", "Congela; o próximo golpe estilhaça com +30%.", Color.Lerp(ElementColors.Of(ElementType.Water), ElementColors.Of(ElementType.Air), 0.5f)),
                ("Estilhaços · Terra + Ar", "Lascas em área grande que empurram longe.", Color.Lerp(ElementColors.Of(ElementType.Earth), ElementColors.Of(ElementType.Air), 0.5f)),
            }, 80);
            UiKit.Text(UiKit.Rect("Hint", box, new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(1500, 30)),
                "Uma skill marca o inimigo; a skill de outro elemento detona a reação. O golpe básico não tem elemento.", 20, UiKit.Dim, TextAnchor.MiddleCenter, false);
            // fusoes: abaixo das reacoes
            UiKit.Text(UiKit.Rect("FusH", box, new Vector2(0, 1), new Vector2(1130, -650), new Vector2(500, 30)), "FUSÕES · TERCEIRO ELEMENTO", 22, UiKit.Dim, TextAnchor.MiddleLeft);
            var fusions = new[] { FusionKind.Obsidian, FusionKind.Wildfire, FusionKind.Cyclone, FusionKind.Geyser };
            for (int i = 0; i < fusions.Length; i++)
            {
                var f = UiKit.Text(UiKit.Rect("Fus" + i, box, new Vector2(0, 1), new Vector2(1130, -684 - i * 30), new Vector2(500, 28)),
                    $"{FusionRules.Name(fusions[i])}: {FusionRules.Describe(fusions[i])}", 16, UiKit.Paper, TextAnchor.MiddleLeft, false);
                f.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
        }

        public void ShowCredits()
        {
            var box = OpenSheet("CRÉDITOS", 800);
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
            Line(-510, "Heróis, Guardião, inimigos e adereços: modelados pela equipe no Blender sobre o KayKit", 24, UiKit.Cream);
            Line(-542, "Ícones: game-icons.net, de Lorc, Delapouite e outros (CC BY 3.0)", 24, UiKit.Cream);
            Line(-574, "Fontes: Lilita One, Titillium Web e Cinzel Decorative (OFL)", 24, UiKit.Cream);
            Line(-606, "Efeitos sonoros e música: sintetizados pela equipe", 24, UiKit.Cream);
            Line(-648, "Feito com Unity 6", 22, UiKit.Dim);
        }

        void BuildRefuge()
        {
            foreach (Transform child in refuge) Destroy(child.gameObject);
            if (meta == null) return;
            var center = new Vector2(0.5f, 0.5f);
            UiKit.Image(UiKit.Stretch("Shade", refuge), null, new Color(0.02f, 0.02f, 0.04f, 0.95f), true);
            var box = UiKit.Box("Box", refuge, center, Vector2.zero, new Vector2(1180, 980), 1f);
            var title = UiKit.Text(UiKit.Rect("Title", box, new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(900, 70)), "REFÚGIO", 60, UiKit.Paper);
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Sub", box, new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(1000, 36)),
                "Melhorias que ficam para todas as runs. Cada sala vencida rende 3 Fragmentos; vencer o Guardião, mais 15.", 23, UiKit.Dim, TextAnchor.MiddleCenter, false);
            UiKit.Text(UiKit.Rect("Wallet", box, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(900, 40)),
                $"{meta.Shards} Fragmentos   ·   runs: {meta.Runs}   ·   vitórias: {meta.Wins}   ·   recorde: {meta.BestRooms} salas", 27, new Color(0.88f, 0.74f, 0.42f));
            if (RunInProgress)
                UiKit.Text(UiKit.Rect("Note", box, new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(1000, 28)),
                    "Há uma run salva: as compras valem a partir da próxima run nova.", 20, new Color(0.9f, 0.76f, 0.42f), TextAnchor.MiddleCenter, false);
            UiKit.Text(UiKit.Rect("HeroWins", box, new Vector2(0.5f, 1), new Vector2(0, -192), new Vector2(900, 28)),
                $"Vitórias por herói:   Rax {meta.HeroWins(ElementType.Fire)}   ·   Liora {meta.HeroWins(ElementType.Water)}   ·   Brann {meta.HeroWins(ElementType.Earth)}   ·   Vey {meta.HeroWins(ElementType.Air)}", 21, UiKit.Dim, TextAnchor.MiddleCenter, false);

            for (int i = 0; i < MetaInfo.Count; i++)
            {
                var u = (MetaUpgrade)i;
                int level = meta.Level(u), max = MetaInfo.MaxLevel(u);
                var row = UiKit.Box("Row" + i, box, new Vector2(0.5f, 1), new Vector2(0, -240 - i * 86), new Vector2(1080, 78), 0.5f);
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
                heroWinLabels[i] = UiKit.Text(UiKit.Rect("Wins", card, new Vector2(1, 1), new Vector2(-38, -18), new Vector2(70, 22)), "", 17, new Color(0.9f, 0.76f, 0.42f), TextAnchor.MiddleRight, false);
            }
            var open = UiKit.Button(root, "REFÚGIO", new Vector2(0.5f, 0), new Vector2(-440, 74), new Vector2(360, 58), UiKit.Primary, () => ShowRefuge(true));
            void Small(string label, float x, Action act) =>
                UiKit.Button(root, label, new Vector2(0.5f, 0), new Vector2(x, 74), new Vector2(200, 58), UiKit.Secondary, act)
                    .transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 26;
            Small("GRIMÓRIO", -140, ShowGrimoire);
            Small("TREINO", 80, () => OnTutorial?.Invoke());
            Small("OPÇÕES", 300, ShowOptions);
            Small("CRÉDITOS", 520, ShowCredits);
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
