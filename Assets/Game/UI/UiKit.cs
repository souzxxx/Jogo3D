using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Quintessence.UI
{
    /// Pecas para montar interface por codigo: canvas, texto, imagem, botao e sprites redondos gerados.
    public static class UiKit
    {
        public static readonly Color Ink = new(0.05f, 0.04f, 0.09f, 0.88f);
        public static readonly Color Paper = new(0.93f, 0.91f, 0.96f);
        public static readonly Color Dim = new(0.62f, 0.59f, 0.72f);
        public static readonly Color Cream = new(0.93f, 0.88f, 0.78f);

        static Sprite circle, ring, panel, frame;
        static readonly System.Collections.Generic.Dictionary<string, Sprite> icons = new();

        /// Painel de cantos arredondados (9-slice) e o contorno dele.
        public static Sprite Panel => panel != null ? panel : panel = MakeRounded(64, 16, 0f);
        public static Sprite Frame => frame != null ? frame : frame = MakeRounded(64, 16, 3f);

        /// Icone de Assets/Resources/Icons (silhueta branca, para tingir).
        public static Sprite Icon(string name)
        {
            if (icons.TryGetValue(name, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Icons/" + name);
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : Circle;
            icons[name] = s;
            return s;
        }

        static Sprite MakeRounded(int size, float radius, float outline)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float h = size / 2f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // distancia com sinal ate a borda de uma caixa arredondada
                float qx = Mathf.Abs(x + 0.5f - h) - (h - radius), qy = Mathf.Abs(y + 0.5f - h) - (h - radius);
                float d = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float a = Mathf.Clamp01(-d);
                if (outline > 0f) a *= Mathf.Clamp01(d + outline);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * (radius + 4));
        }

        /// Painel escuro arredondado com contorno claro discreto.
        public static RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float alpha = 0.82f)
        {
            var rt = Rect(name, parent, anchor, pos, size);
            var bg = Image(rt, Panel, new Color(0.07f, 0.06f, 0.1f, alpha));
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            var edge = Image(Stretch("Edge", rt), Frame, new Color(0.85f, 0.8f, 0.7f, 0.22f));
            edge.type = UnityEngine.UI.Image.Type.Sliced;
            return rt;
        }
        public static Font Font => Quintessence.Core.GameFonts.Display;
        public static Sprite Circle => circle != null ? circle : circle = MakeCircle(96, 0f);
        public static Sprite Ring => ring != null ? ring : ring = MakeCircle(96, 0.8f);

        static Sprite MakeCircle(int size, float inner)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size / 2f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                float a = Mathf.Clamp01(r - d);
                if (inner > 0f) a *= Mathf.Clamp01(d - inner * r);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Canvas Canvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        /// Retangulo preso a um ponto (anchor = pivot).
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(RectTransform rt, Sprite sprite, Color color, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Text(RectTransform rt, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, bool bold = true)
        {
            var t = rt.gameObject.AddComponent<Text>();
            // bold = fonte de destaque (numeros, titulos, rotulos); false = fonte de texto corrido
            t.font = bold ? Quintessence.Core.GameFonts.Display : Quintessence.Core.GameFonts.Body;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.02f, 0.02f, 0.05f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Button Button(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Action onClick)
        {
            var rt = Rect("Button_" + label, parent, anchor, pos, size);
            var img = Image(rt, null, color, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colors;
            b.onClick.AddListener(() => onClick?.Invoke());
            var txt = Stretch("Label", rt);
            Text(txt, label, 30, Paper);
            return b;
        }
    }
}
