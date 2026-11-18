using System;
using Quintessence.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Quintessence.UI
{
    /// Opcoes que aparecem na pausa e no titulo: volume de musica e de efeitos, tela cheia, tremor de tela, dificuldade,
    /// graficos leves para PC fraco e a tabela de controles.
    public static class UiOptions
    {
        /// Monta os controles em volta de pos (ancorados no centro de parent). Ocupa 660 x 380.
        public static void Build(Transform parent, Vector2 pos)
        {
            Stepper(parent, pos + new Vector2(-170, 45), "MÚSICA", () => AudioPrefs.Music, v => AudioPrefs.Music = v);
            Stepper(parent, pos + new Vector2(170, 45), "EFEITOS", () => AudioPrefs.Effects, v => AudioPrefs.Effects = v);
            UnityEngine.UI.Text label = null;
            UnityEngine.UI.Text shakeLabel = null;
            var s = UiKit.Button(parent, ShakeLabel(), new Vector2(0.5f, 0.5f), pos + new Vector2(165, -85), new Vector2(310, 60), UiKit.Secondary, () =>
            {
                FxPrefs.ScreenShake = !FxPrefs.ScreenShake;
                shakeLabel.text = ShakeLabel();
            });
            shakeLabel = s.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            shakeLabel.fontSize = 22;
            var b = UiKit.Button(parent, FullscreenLabel(), new Vector2(0.5f, 0.5f), pos + new Vector2(-165, -85), new Vector2(310, 60), UiKit.Secondary, () =>
            {
                Screen.fullScreen = !Screen.fullScreen;
                // o navegador so troca no proximo quadro; o rotulo mostra o que foi pedido
                label.text = Screen.fullScreen ? "Tela cheia: não" : "Tela cheia: sim";
            });
            label = b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            label.fontSize = 24;
            UnityEngine.UI.Text difficultyLabel = null;
            var d = UiKit.Button(parent, DifficultyLabel(), new Vector2(0.5f, 0.5f), pos + new Vector2(0, -152), new Vector2(640, 52), UiKit.Secondary, () =>
            {
                GamePrefs.Relaxed = !GamePrefs.Relaxed;
                difficultyLabel.text = DifficultyLabel();
            });
            d.name = "Difficulty";
            difficultyLabel = d.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            difficultyLabel.fontSize = 22;
            UnityEngine.UI.Text gfxLabel = null;
            var g = UiKit.Button(parent, GfxLabel(), new Vector2(0.5f, 0.5f), pos + new Vector2(-165, -219), new Vector2(310, 52), UiKit.Secondary, () =>
            {
                GfxPrefs.Light = !GfxPrefs.Light;
                gfxLabel.text = GfxLabel();
            });
            g.name = "Graphics";
            gfxLabel = g.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            gfxLabel.fontSize = 22;
            UnityEngine.UI.Button c = null;
            c = UiKit.Button(parent, "Controles", new Vector2(0.5f, 0.5f), pos + new Vector2(165, -219), new Vector2(310, 52), UiKit.Secondary, () => ShowControls(parent, c.gameObject));
            c.name = "Controls";
            c.transform.Find("Label").GetComponent<UnityEngine.UI.Text>().fontSize = 22;
        }

        static string GfxLabel() => GfxPrefs.Light ? "Gráficos: leves" : "Gráficos: completos";

        static readonly (string action, string keys, string pad)[] Bindings =
        {
            ("Mover", "W A S D", "analógico esquerdo"),
            ("Mirar", "mouse", "analógico direito"),
            ("Golpe básico (segure para o combo)", "clique esquerdo", "RT ou X"),
            ("Skills de Fogo, Água, Terra e Ar", "1  2  3  4", "direcional"),
            ("Repetir a última skill", "clique direito", "LT"),
            ("Desviar (dash)", "Espaço", "A"),
            ("Quintessência (com os quatro elementos)", "R", "Y"),
            ("Interagir: portas, baús, totens", "E", "B"),
            ("Inventário", "I", "Select"),
            ("Mapa (segure)", "Tab", "R3 (analógico direito)"),
            ("Pausa e opções", "Esc", "Start"),
        };

        /// Tabela de teclas e botoes do controle, por cima do painel que a abriu (some junto com ele).
        public static void ShowControls(Transform owner, GameObject returnFocus = null)
        {
            var center = new Vector2(0.5f, 0.5f);
            var old = owner.Find("ControlsSheet");
            if (old != null) Object.Destroy(old.gameObject);
            // o painel fica dentro de quem abriu (pausa ou caixa de opcoes do titulo): fechar a pausa leva ele junto
            var sheet = UiKit.Rect("ControlsSheet", owner, center, Vector2.zero, new Vector2(5000, 5000));
            UiKit.Image(sheet, null, new Color(0.03f, 0.02f, 0.06f, 0.92f), true);
            var canvas = owner.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
            var box = UiKit.Box("Box", sheet, center, Vector2.zero, new Vector2(1240, 760), 1f);
            box.position = canvas.position; // centro da tela, mesmo com a caixa de quem abriu fora do centro
            var title = UiKit.Text(UiKit.Rect("Title", box, center, new Vector2(0, 318), new Vector2(900, 80)), "CONTROLES", 60, UiKit.Cream);
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("HeadKeys", box, center, new Vector2(150, 252), new Vector2(330, 34)), "TECLADO E MOUSE", 22, UiKit.Dim);
            UiKit.Text(UiKit.Rect("HeadPad", box, center, new Vector2(440, 252), new Vector2(300, 34)), "CONTROLE", 22, UiKit.Dim);
            for (int i = 0; i < Bindings.Length; i++)
            {
                float y = 206 - i * 44;
                if (i % 2 == 0) UiKit.Image(UiKit.Rect("Row" + i, box, center, new Vector2(0, y), new Vector2(1160, 42)), null, new Color(1f, 1f, 1f, 0.04f));
                UiKit.Text(UiKit.Rect("Action" + i, box, center, new Vector2(-300, y), new Vector2(520, 40)), Bindings[i].action, 24, UiKit.Cream, TextAnchor.MiddleLeft, false);
                UiKit.Text(UiKit.Rect("Keys" + i, box, center, new Vector2(150, y), new Vector2(330, 40)), Bindings[i].keys, 24, new Color(0.9f, 0.76f, 0.42f));
                UiKit.Text(UiKit.Rect("Pad" + i, box, center, new Vector2(440, y), new Vector2(300, 40)), Bindings[i].pad, 22, UiKit.Cream, TextAnchor.MiddleCenter, false);
            }
            UiKit.Text(UiKit.Rect("Tip", box, center, new Vector2(0, -292), new Vector2(1160, 34)),
                "Uma skill marca o inimigo com o elemento dela; a skill de outro elemento detona a reação.", 22, UiKit.Dim, TextAnchor.MiddleCenter, false);
            var back = UiKit.Button(box, "Voltar", center, new Vector2(0, -340), new Vector2(300, 60), UiKit.Primary, () => { Object.Destroy(sheet.gameObject); if (returnFocus != null) UiKit.Focus(returnFocus); });
            // no controle, o direcional nao pode levar o foco para os botoes escondidos atras da tabela
            back.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            UiKit.Focus(back.gameObject);
        }

        static string DifficultyLabel() => GamePrefs.Relaxed ? "Dificuldade: Tranquila (35% menos dano)" : "Dificuldade: Normal";

        static string ShakeLabel() => FxPrefs.ScreenShake ? "Tremor e vibração: sim" : "Tremor e vibração: não";

        static string FullscreenLabel() => Screen.fullScreen ? "Tela cheia: sim" : "Tela cheia: não";

        /// Controle em dez passos: rotulo, botao de menos, valor, botao de mais.
        public static void Stepper(Transform parent, Vector2 pos, string label, Func<float> get, Action<float> set)
        {
            var center = new Vector2(0.5f, 0.5f);
            var box = UiKit.Box("Volume_" + label, parent, center, pos, new Vector2(300, 150));
            UiKit.Text(UiKit.Rect("Label", box, center, new Vector2(0, 44), new Vector2(280, 34)), label, 26, UiKit.Dim);
            var value = UiKit.Text(UiKit.Rect("Value", box, center, new Vector2(0, -18), new Vector2(100, 50)), AudioPrefs.Step(get()).ToString(), 44, UiKit.Cream);
            void Change(int delta)
            {
                set(Mathf.Clamp(AudioPrefs.Step(get()) + delta, 0, 10) / 10f);
                value.text = AudioPrefs.Step(get()).ToString();
            }
            UiKit.Button(box, "−", center, new Vector2(-96, -18), new Vector2(64, 64), UiKit.Secondary, () => Change(-1));
            UiKit.Button(box, "+", center, new Vector2(96, -18), new Vector2(64, 64), UiKit.Secondary, () => Change(1));
        }
    }
}
