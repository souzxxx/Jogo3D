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

        /// Sair so faz sentido no executavel (Windows/Mac): no WebGL o Application.Quit nao fecha a aba.
        public static bool QuitAvailable => !Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer;

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
        readonly UnityEngine.UI.Text[] heroMasteryLabels = new UnityEngine.UI.Text[4];

        void UpdateRefugeLabel()
        {
            // vitorias de cada heroi no canto da carta dele
            for (int i = 0; i < heroWinLabels.Length; i++)
                if (heroWinLabels[i] != null && meta != null)
                {
                    int w = meta.HeroWins(Elements.All[i]);
                    heroWinLabels[i].text = w > 0 ? (w == 1 ? "1 vitória" : $"{w} vitórias") : "";
                    int m = meta.MasteryLevel(Elements.All[i]);
                    if (heroMasteryLabels[i] != null) heroMasteryLabels[i].text = m > 0 ? $"Maestria {m}" : "";
                }
            if (refugeLabel != null && meta != null) refugeLabel.text = $"REFÚGIO  ·  {meta.Shards} Fragmentos";
            if (firstTimeHint != null) firstTimeHint.gameObject.SetActive(FirstTime);
            if (treinoEdge != null && !FirstTime) treinoEdge.color = treinoEdgeColor;
        }

        UnityEngine.UI.Text firstTimeHint;
        UnityEngine.UI.Image treinoEdge;
        Color treinoEdgeColor;
        /// Ninguem jogou ainda neste computador: o titulo convida para o treino.
        public bool FirstTime => meta != null && meta.Runs == 0;

        public void ShowRefuge(bool show)
        {
            confirmReset = false;
            refuge.gameObject.SetActive(show);
            if (show) { refuge.SetAsLastSibling(); BuildRefuge(); }
            else SyncOverlayBlock();
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
        readonly UnityEngine.UI.Text[] noteLabels = new UnityEngine.UI.Text[4];
        readonly string[] noteTexts = new string[4];
        /// Carta que ja pediu confirmacao (ha run salva): -1 nenhuma.
        public int ArmedCard { get; private set; } = -1;

        /// Clique numa carta. Com run salva, o primeiro clique so avisa que a run nova apaga a salva
        /// (como o "Recomecar run" da pausa); o segundo na mesma carta comeca.
        public void PickCard(ElementType e)
        {
            int i = System.Array.IndexOf(Elements.All, e);
            if (RunInProgress && i >= 0 && ArmedCard != i)
            {
                if (ArmedCard >= 0 && noteLabels[ArmedCard] != null)
                {
                    noteLabels[ArmedCard].text = noteTexts[ArmedCard];
                    noteLabels[ArmedCard].color = UiKit.Paper;
                }
                ArmedCard = i;
                if (noteLabels[i] != null)
                {
                    noteLabels[i].text = "Run nova apaga a salva.\nEscolha de novo para jogar";
                    noteLabels[i].color = new Color(0.95f, 0.62f, 0.55f);
                }
                return;
            }
            OnPick?.Invoke(e);
        }

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

        UnityEngine.UI.Text keysText;
        bool keysPad;

        /// Linha de comandos no pe do titulo: a do teclado ou a do controle, conforme o que o jogador usou por ultimo.
        public static string KeysLine(bool gamepad) => gamepad
            ? "analógico esquerdo move · direito mira · X ataca · LB LT RT RB skills · A dash · Y Quintessência · B interagir · Select inventário"
            : "WASD mover · mouse mira · clique ataca · 1 2 3 4 skills · Espaço dash · R Quintessência · E interagir · I inventário  —  também funciona com controle";

        /// No titulo nao existe o leitor de controles do heroi: quem mexeu por ultimo (controle ou teclado e mouse) decide os textos.
        void TrackInputMode()
        {
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.1f || pad.rightStick.ReadValue().sqrMagnitude > 0.1f
                || pad.dpad.ReadValue().sqrMagnitude > 0.1f || pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.startButton.isPressed))
                InputMode.Gamepad = true;
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null && (m.delta.ReadValue().sqrMagnitude > 4f || m.leftButton.isPressed)) InputMode.Gamepad = false;
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null && k.anyKey.isPressed) InputMode.Gamepad = false;
            if (keysText != null && keysPad != InputMode.Gamepad) { keysPad = InputMode.Gamepad; keysText.text = KeysLine(keysPad); }
        }

        void Update()
        {
            TrackInputMode();
            if (treinoEdge != null && FirstTime)
                treinoEdge.color = new Color(0.95f, 0.78f, 0.4f, 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f));
            // Esc (ou B/Start no controle) fecha o painel aberto e volta para as cartas
            bool back = (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                || (UnityEngine.InputSystem.Gamepad.current != null && (UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame || UnityEngine.InputSystem.Gamepad.current.startButton.wasPressedThisFrame));
            if (back) CloseOverlay();
            if ((SheetOpen || RefugeOpen) != (blocked.Count > 0)) SyncOverlayBlock();
            if (quintPip == null) return;
            // o quinto ponto do emblema respira devagar
            float k = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 1.6f);
            quintPip.localScale = new Vector3(k, k, 1f);
        }

        public bool SheetOpen => sheet != null && sheet.gameObject.activeSelf;

        /// Fecha o painel que estiver aberto (Grimorio, Feitos, Opcoes, Creditos ou Refugio). Devolve se havia algum.
        public bool CloseOverlay()
        {
            if (SheetOpen) { sheet.gameObject.SetActive(false); SyncOverlayBlock(); FocusFirstCard(); return true; }
            if (RefugeOpen) { ShowRefuge(false); SyncOverlayBlock(); FocusFirstCard(); return true; }
            return false;
        }

        /// Painel simples por cima do titulo (opcoes, creditos): titulo, conteudo e Voltar.
        RectTransform OpenSheet(string heading, float height, float width = 1000f)
        {
            foreach (Transform child in sheet) Destroy(child.gameObject);
            sheet.gameObject.SetActive(true);
            sheet.SetAsLastSibling();
            UiKit.Image(UiKit.Stretch("Shade", sheet), null, new Color(0.02f, 0.02f, 0.04f, 0.95f), true);
            var box = UiKit.Box("Box", sheet, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height), 1f);
            var title = UiKit.Text(UiKit.Rect("Title", box, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 70)), heading, 56, UiKit.Paper);
            title.font = GameFonts.Title;
            UiKit.Focus(UiKit.Button(box, "Voltar", new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(300, 60), UiKit.Primary, () => { sheet.gameObject.SetActive(false); SyncOverlayBlock(); FocusFirstCard(); }).gameObject);
            SyncOverlayBlock();
            return box;
        }

        public void ShowOptions()
        {
            var box = OpenSheet("OPÇÕES", 640); // mais baixa e com os controles subindo: sobrava um vao vazio sob o titulo
            UiOptions.Build(box, new Vector2(0, 70));
        }

        /// Grimorio: herois, inimigos (e como lidar com cada um) e as seis reacoes.
        public void ShowGrimoire()
        {
            var box = OpenSheet("GRIMÓRIO", 1000, 1640);
            var top = new Vector2(0, 1);
            void Column(float x, string heading, (string name, string text, Color color)[] rows, float step)
            {
                UiKit.Text(UiKit.Rect("H_" + heading, box, top, new Vector2(x, -120), new Vector2(500, 32)), heading, 24, UiKit.Dim, TextAnchor.MiddleLeft);
                for (int i = 0; i < rows.Length; i++)
                {
                    float y = -160 - i * step;
                    UiKit.Text(UiKit.Rect("N", box, top, new Vector2(x, y), new Vector2(500, 28)), rows[i].name, 22, rows[i].color, TextAnchor.MiddleLeft);
                    // o texto encolhe se nao couber no espaco ate o proximo nome (antes vazava por cima dele)
                    UiKit.Fit(UiKit.Text(UiKit.Rect("T", box, top, new Vector2(x, y - 26), new Vector2(500, step - 26)), rows[i].text, 18, UiKit.Paper, TextAnchor.UpperLeft, false), 13, 18);
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
            // maestria: cada run da experiencia ao heroi usado; os niveis valem so para ele
            UiKit.Text(UiKit.Rect("MasteryH", box, top, new Vector2(70, -560), new Vector2(500, 30)), "MAESTRIA · POR HERÓI", 22, UiKit.Dim, TextAnchor.MiddleLeft);
            for (int i = 1; i <= Mastery.MaxLevel; i++)
                UiKit.Text(UiKit.Rect("Mastery" + i, box, top, new Vector2(70, -560 - i * 30), new Vector2(500, 28)),
                    $"{i}. {Mastery.Perk(i)}", 18, UiKit.Paper, TextAnchor.MiddleLeft, false);
            UiKit.Text(UiKit.Rect("MasteryNote", box, top, new Vector2(70, -560 - 6 * 30 - 4), new Vector2(500, 28)),
                "Cada sala vencida, cada 10 abates e a vitória dão experiência.", 16, UiKit.Dim, TextAnchor.MiddleLeft, false);
            Column(600, "INIMIGOS", new (string, string, Color)[]
            {
                ("Rastejante", "Vem em grupo. Área e reações limpam o bando.", foe),
                ("Atirador", "Tiro em linha; recua se você chega perto. Feche com o dash.", foe),
                ("Bruto", "Armadura: leva metade do dano. O Pilar de Pedra quebra.", foe),
                ("Explosivo", "Corre e explode. Saia do círculo; a explosão fere o bando.", foe),
                ("Ceifador", "Avisa uma linha e atravessa a sala. Saia e bata na volta.", foe),
                ("Arremessador", "Joga a bomba onde você está. Saia do círculo.", foe),
                ("Xamã", "Cura os aliados feridos (aviso verde). Derrube primeiro.", foe),
                ("Escudeiro", "Escudo na frente. Contorne, congele, enraíze ou use reações.", foe),
                ("Porta-Estandarte", "Quem está no círculo dele bate mais e corre mais. Derrube primeiro.", foe),
                ("Espectro", "Avisa um círculo nas suas costas e aparece ali. Não fique parado.", foe),
                ("Cinzentos", "Bando pequeno e rápido. Skills de área resolvem.", foe),
                ("Elementais", "Brasa Viva (Fogo), Afogado (Água) e Sentinela (Terra).", foe),
                ("Campeão e chefes", "Elites: Carrasco, Sacerdotisa, Colosso. Fim: Guardião, Tecelã, Rainha.", gold),
            }, 53);
            Column(1130, "REAÇÕES", new (string, string, Color)[]
            {
                ("Vapor · Fogo + Água", "Nuvem: quem está dentro erra metade dos ataques.", Color.Lerp(ElementColors.Of(ElementType.Fire), ElementColors.Of(ElementType.Water), 0.5f)),
                ("Magma · Fogo + Terra", "Poça de lava que queima por 4 segundos.", Color.Lerp(ElementColors.Of(ElementType.Fire), ElementColors.Of(ElementType.Earth), 0.5f)),
                ("Tempestade de Fogo · Fogo + Ar", "A maior explosão; deixa todos Queimando.", Color.Lerp(ElementColors.Of(ElementType.Fire), ElementColors.Of(ElementType.Air), 0.5f)),
                ("Lama · Água + Terra", "Enraíza e encharca: ninguém sai do lugar.", Color.Lerp(ElementColors.Of(ElementType.Water), ElementColors.Of(ElementType.Earth), 0.5f)),
                ("Nevasca · Água + Ar", "Congela; o próximo golpe estilhaça com +30%.", Color.Lerp(ElementColors.Of(ElementType.Water), ElementColors.Of(ElementType.Air), 0.5f)),
                ("Estilhaços · Terra + Ar", "Lascas em área grande que empurram longe.", Color.Lerp(ElementColors.Of(ElementType.Earth), ElementColors.Of(ElementType.Air), 0.5f)),
            }, 74);
            UiKit.Text(UiKit.Rect("Hint", box, new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(1500, 30)),
                "Uma skill marca o inimigo; a skill de outro elemento detona a reação. O golpe básico não tem elemento.", 20, UiKit.Dim, TextAnchor.MiddleCenter, false);
            // o cenario tambem entra no jogo dos elementos (rodadas 21, 29-31): no espaco livre sob a maestria
            UiKit.Text(UiKit.Rect("SceneryH", box, top, new Vector2(70, -778), new Vector2(500, 28)), "CENÁRIO", 22, UiKit.Dim, TextAnchor.MiddleLeft);
            UiKit.Fit(UiKit.Text(UiKit.Rect("Scenery", box, top, new Vector2(70, -806), new Vector2(500, 60)),
                "Água apaga velas e respiradouros e molha a pólvora; Fogo reacende; o vento apaga velas. A poça e a erupção marcam quem está em cima.", 16, UiKit.Paper, TextAnchor.UpperLeft, false), 14, 16);
            // fusoes: abaixo das reacoes
            UiKit.Text(UiKit.Rect("FusH", box, new Vector2(0, 1), new Vector2(1130, -606), new Vector2(500, 30)), "FUSÕES · TERCEIRO ELEMENTO", 22, UiKit.Dim, TextAnchor.MiddleLeft);
            var fusions = new[] { FusionKind.Obsidian, FusionKind.Wildfire, FusionKind.Cyclone, FusionKind.Geyser, FusionKind.Kiln, FusionKind.DustStorm };
            for (int i = 0; i < fusions.Length; i++)
            {
                // ate duas linhas por fusao: com uma so, a mais longa encolhia a ponto de nao dar para ler
                UiKit.Fit(UiKit.Text(UiKit.Rect("Fus" + i, box, new Vector2(0, 1), new Vector2(1130, -640 - i * 38), new Vector2(490, 46)),
                    $"{FusionRules.Name(fusions[i])}: {FusionRules.Describe(fusions[i])}", 16, UiKit.Paper, TextAnchor.UpperLeft, false), 13, 16);
            }
        }

        /// Feitos: as dez metas permanentes, cumpridas ou nao, e o que cada uma paga.
        public void ShowFeats()
        {
            var box = OpenSheet("FEITOS", 900, 1200);
            int done = meta != null ? meta.FeatCount : 0;
            UiKit.Text(UiKit.Rect("Count", box, new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(1000, 32)),
                $"{done} de {Feats.Count} cumpridos  ·  cada feito paga {Feats.Reward} Fragmentos uma vez", 22, UiKit.Dim, TextAnchor.MiddleCenter, false);
            var gold = new Color(0.88f, 0.74f, 0.42f);
            for (int i = 0; i < Feats.Count; i++)
            {
                var f = (Feat)i;
                bool has = meta != null && meta.Has(f);
                float x = i < 5 ? -280f : 280f, y = -190 - (i % 5) * 112;
                var row = UiKit.Box("Feat" + i, box, new Vector2(0.5f, 1), new Vector2(x, y), new Vector2(520, 96), has ? 0.9f : 0.5f);
                var disc = UiKit.Rect("Disc", row, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(56, 56));
                UiKit.Image(disc, has ? UiKit.Circle : UiKit.Ring, has ? gold : UiKit.Dim);
                UiKit.Text(UiKit.Rect("Name", row, new Vector2(0, 0.5f), new Vector2(86, 16), new Vector2(420, 34)), Feats.Name(f), 26, has ? UiKit.Paper : UiKit.Dim, TextAnchor.MiddleLeft);
                UiKit.Text(UiKit.Rect("Desc", row, new Vector2(0, 0.5f), new Vector2(86, -18), new Vector2(420, 30)), Feats.Describe(f), 20, has ? UiKit.Cream : UiKit.Dim, TextAnchor.MiddleLeft, false);
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
            Line(-510, "Heróis, chefes, inimigos, NPCs e adereços: modelados pela equipe no Blender sobre o KayKit", 24, UiKit.Cream);
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
            var box = UiKit.Box("Box", refuge, center, Vector2.zero, new Vector2(1180, 1030), 1f);
            var title = UiKit.Text(UiKit.Rect("Title", box, new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(900, 70)), "REFÚGIO", 60, UiKit.Paper);
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Sub", box, new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(1000, 36)),
                "Melhorias que ficam para todas as runs. Cada sala vencida rende 3 Fragmentos; vencer o chefe final, mais 15.", 23, UiKit.Dim, TextAnchor.MiddleCenter, false);
            UiKit.Text(UiKit.Rect("Wallet", box, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(900, 40)),
                $"{meta.Shards} Fragmentos   ·   runs: {meta.Runs}   ·   vitórias: {meta.Wins}   ·   recorde: {meta.BestRooms} salas", 27, new Color(0.88f, 0.74f, 0.42f));
            if (RunInProgress)
                UiKit.Text(UiKit.Rect("Note", box, new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(1000, 28)),
                    "Há uma run salva: as compras valem a partir da próxima run nova.", 20, new Color(0.9f, 0.76f, 0.42f), TextAnchor.MiddleCenter, false);
            string HeroLine(ElementType e) => $"{HeroPassives.HeroName(e)}: maestria {meta.MasteryLevel(e)}, {meta.HeroWins(e)} {(meta.HeroWins(e) == 1 ? "vitória" : "vitórias")}";
            var heroLine = UiKit.Text(UiKit.Rect("HeroWins", box, new Vector2(0.5f, 1), new Vector2(0, -192), new Vector2(1100, 28)),
                $"{HeroLine(ElementType.Fire)}   ·   {HeroLine(ElementType.Water)}   ·   {HeroLine(ElementType.Earth)}   ·   {HeroLine(ElementType.Air)}", 21, UiKit.Dim, TextAnchor.MiddleCenter, false);
            UiKit.Fit(heroLine, 14, 21);

            for (int i = 0; i < MetaInfo.Count; i++)
            {
                var u = (MetaUpgrade)i;
                int level = meta.Level(u), max = MetaInfo.MaxLevel(u);
                var row = UiKit.Box("Row" + i, box, new Vector2(0.5f, 1), new Vector2(0, -232 - i * 80), new Vector2(1080, 74), 0.5f);
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
                    // apagado: diz quanto falta, dentro do proprio botao (antes so o preco, sem dizer por que nao dava)
                    int missing = Mathf.Max(0, meta.Cost(u) - meta.Shards);
                    var b = UiKit.Button(row, can ? $"{meta.Cost(u)} Fragmentos" : $"{meta.Cost(u)} Fragmentos\nfaltam {missing}", new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(210, 56),
                        can ? UiKit.Primary : UiKit.Disabled, () => { keepFocus = "Buy" + (int)u; Buy(u); });
                    b.name = "Buy" + (int)u;
                    var t = b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
                    t.fontSize = can ? 23 : 19;
                    t.lineSpacing = 0.85f;
                    if (!can) t.color = UiKit.Dim;
                }
            }
            var back = UiKit.Button(box, "Voltar", new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(300, 60), UiKit.Primary, () => { ShowRefuge(false); FocusFirstCard(); });
            var wipe = UiKit.Button(box, confirmReset ? "Apagar mesmo?" : "Apagar progresso", new Vector2(1, 0), new Vector2(-50, 62), new Vector2(250, 46),
                confirmReset ? UiKit.Danger : UiKit.Secondary, () =>
                {
                    if (!confirmReset) { confirmReset = true; keepFocus = "Wipe"; BuildRefuge(); return; }
                    confirmReset = false;
                    reset?.Invoke();
                    if (reload != null) meta = reload();
                    BuildRefuge();
                    UpdateRefugeLabel();
                });
            wipe.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 21;
            wipe.name = "Wipe";
            // no controle, o foco fica no botao que foi apertado (comprar de novo, confirmar o apagar); senao no Voltar
            Transform again = null;
            if (keepFocus != null) foreach (var t in box.GetComponentsInChildren<Transform>()) if (t.name == keepFocus) { again = t; break; }
            keepFocus = null;
            UiKit.Focus(again != null ? again.gameObject : back.gameObject);
            SyncOverlayBlock();
        }

        string keepFocus;
        readonly System.Collections.Generic.List<UnityEngine.UI.Selectable> blocked = new();
        /// Botoes do titulo desligados enquanto um painel esta aberto (para os testes).
        public int BlockedBehindOverlay => blocked.Count;

        /// Com Grimorio, Feitos, Opcoes, Creditos ou Refugio aberto, os botoes do titulo por baixo ficam desligados:
        /// o direcional do controle navegava ate uma carta escondida sob o painel e o A comecava a run.
        void SyncOverlayBlock()
        {
            if (canvas == null) return;
            bool open = SheetOpen || RefugeOpen;
            if (!open)
            {
                foreach (var sel in blocked) if (sel != null) sel.interactable = true;
                blocked.Clear();
                return;
            }
            foreach (var sel in canvas.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
            {
                var t = sel.transform;
                if ((sheet != null && t.IsChildOf(sheet)) || (refuge != null && t.IsChildOf(refuge))) continue;
                if (!sel.interactable) continue;
                sel.interactable = false;
                blocked.Add(sel);
            }
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
            string[] notes = { "Fogo · garras rápidas\n+15% no combo básico", "Água · chicote longo\ncura ao limpar a sala", "Terra · martelo pesado\n+20 de vida máxima", "Ar · adagas velozes\nesquiva 25% e cura" };
            string[] iconNames = { "fire", "water", "earth", "air" };
            var mid = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                var c = ElementColors.Of(e);
                // carta escura com o elemento so no contorno, no medalhao e no filete: a cor entra como acento
                var body = Color.Lerp(new Color(0.09f, 0.08f, 0.12f), c, 0.16f);
                var b = UiKit.Button(root, "", mid, new Vector2((i - 1.5f) * 290f, -268), new Vector2(256, 216), body, () => PickCard(e), Color.Lerp(c, UiKit.Cream, 0.15f));
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
                UiKit.Text(UiKit.Rect("Name", card, new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(240, 48)), names[i], 38, UiKit.Cream);
                UiKit.Image(UiKit.Rect("Rule", card, new Vector2(0.5f, 1), new Vector2(0, -138), new Vector2(72, 4)), null, Color.Lerp(c, UiKit.Cream, 0.2f));
                noteLabels[i] = UiKit.Text(UiKit.Rect("Note", card, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(236, 54)), notes[i], 21, UiKit.Paper, TextAnchor.MiddleCenter, false);
                noteTexts[i] = notes[i];
                UiKit.Fit(noteLabels[i], 15, 21);
                heroWinLabels[i] = UiKit.Text(UiKit.Rect("Wins", card, new Vector2(1, 1), new Vector2(-38, -18), new Vector2(70, 22)), "", 17, new Color(0.9f, 0.76f, 0.42f), TextAnchor.MiddleRight, false);
                heroMasteryLabels[i] = UiKit.Text(UiKit.Rect("Mastery", card, new Vector2(0, 1), new Vector2(12, -18), new Vector2(80, 22)), "", 17, UiKit.Cream, TextAnchor.MiddleLeft, false);
                UiKit.Fit(heroMasteryLabels[i], 12, 17);
            }
            var open = UiKit.Button(root, "REFÚGIO", new Vector2(0.5f, 0), new Vector2(-560, 74), new Vector2(340, 58), UiKit.Primary, () => ShowRefuge(true));
            void Small(string label, float x, Action act) =>
                UiKit.Button(root, label, new Vector2(0.5f, 0), new Vector2(x, 74), new Vector2(200, 58), UiKit.Secondary, act)
                    .transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 26;
            Small("GRIMÓRIO", -270, ShowGrimoire);
            Small("FEITOS", -50, ShowFeats);
            Small("TREINO", 170, () => OnTutorial?.Invoke());
            // primeira vez no jogo (nenhuma run): o contorno do treino pulsa em ambar e um convite aparece em cima
            var treino = root.Find("Button_TREINO");
            treinoEdge = treino != null ? treino.Find("Edge").GetComponent<UnityEngine.UI.Image>() : null;
            treinoEdgeColor = treinoEdge != null ? treinoEdge.color : Color.white;
            firstTimeHint = UiKit.Text(UiKit.Rect("FirstTime", root, new Vector2(0.5f, 0), new Vector2(170, 132), new Vector2(420, 30)), // 132: em 124 encostava no botao
                "Primeira vez? Comece pelo treino", 22, new Color(0.9f, 0.76f, 0.42f), TextAnchor.MiddleCenter, false);
            Small("OPÇÕES", 390, ShowOptions);
            Small("CRÉDITOS", 610, ShowCredits);
            // na build de PC nao ha aba para fechar: Sair no canto oposto ao Continuar (no navegador e no editor ele nao aparece)
            if (QuitAvailable)
            {
                var quit = UiKit.Button(root, "SAIR", new Vector2(0, 1), new Vector2(110, -50), new Vector2(160, 54), UiKit.Secondary, Application.Quit);
                quit.name = "Quit";
                quit.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 24;
            }
            refugeLabel = open.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            refugeLabel.fontSize = 26;
            refuge = UiKit.Stretch("Refuge", root);
            refuge.gameObject.SetActive(false);
            sheet = UiKit.Stretch("Sheet", root);
            sheet.gameObject.SetActive(false);
            UpdateRefugeLabel();
            keysText = UiKit.Text(UiKit.Rect("Keys", root, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1600, 30)), KeysLine(InputMode.Gamepad), 22, UiKit.Dim, TextAnchor.MiddleCenter, false);
            keysPad = InputMode.Gamepad;
        }

        public void UseCamera(Camera cam)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }
    }
}
