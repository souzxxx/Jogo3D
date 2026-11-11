using System;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.UI
{
    /// Tela de titulo: escolher o elemento inicial comeca a run.
    public class TitleMenu : MonoBehaviour
    {
        public Action<ElementType> OnPick;
        Canvas canvas;

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
                var label = (RectTransform)b.transform.Find("Label");
                label.offsetMin = new Vector2(0, 60); label.offsetMax = new Vector2(0, -110);
                label.GetComponent<UnityEngine.UI.Text>().fontSize = 40;
                UiKit.Text(UiKit.Rect("Note", b.transform, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(240, 64)), notes[i], 23, UiKit.Paper, TextAnchor.MiddleCenter, false);
            }
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
