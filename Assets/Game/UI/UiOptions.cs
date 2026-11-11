using System;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.UI
{
    /// Opcoes que aparecem na pausa e no titulo: volume de musica e de efeitos, e tela cheia.
    public static class UiOptions
    {
        /// Monta os tres controles em volta de pos (ancorados no centro de parent). Ocupa 660 x 240.
        public static void Build(Transform parent, Vector2 pos)
        {
            Stepper(parent, pos + new Vector2(-170, 45), "MÚSICA", () => AudioPrefs.Music, v => AudioPrefs.Music = v);
            Stepper(parent, pos + new Vector2(170, 45), "EFEITOS", () => AudioPrefs.Effects, v => AudioPrefs.Effects = v);
            UnityEngine.UI.Text label = null;
            var b = UiKit.Button(parent, FullscreenLabel(), new Vector2(0.5f, 0.5f), pos + new Vector2(0, -85), new Vector2(640, 60), UiKit.Secondary, () =>
            {
                Screen.fullScreen = !Screen.fullScreen;
                // o navegador so troca no proximo quadro; o rotulo mostra o que foi pedido
                label.text = Screen.fullScreen ? "Tela cheia: não" : "Tela cheia: sim";
            });
            label = b.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            label.fontSize = 26;
        }

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
