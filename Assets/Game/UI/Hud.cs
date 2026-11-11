using System;
using System.Collections.Generic;
using Quintessence.Core;
using Quintessence.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    /// HUD da run: vida, roda de elementos com recarga da skill, cargas de dash, medidor de Quintessencia,
    /// essencia, sala atual, aviso de interacao, mapa, pausa e tela de resultado. Monta a si mesmo.
    public class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        Canvas canvas;
        IHeroStatus hero;
        Image hpFill, hpLag, quintFill, fade;
        Text hpText, quintText, essenceText, roomText, promptText;
        RectTransform promptBox, mapPanel, resultPanel, pausePanel, mapContent;
        readonly Slot[] slots = new Slot[4];
        readonly Image[] dashPips = new Image[2];
        float lagHp = 1f;

        class Slot { public RectTransform Root; public Image Icon, Cooldown, Ring; }

        void Awake()
        {
            Instance = this;
            UiKit.EnsureEventSystem();
            Build();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// Para gravacoes: desenha o HUD pela camera (o modo Overlay nao aparece em captura de camera).
        public void UseCamera(Camera cam)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }

        void Build()
        {
            canvas = UiKit.Canvas("HudCanvas", 10, transform);
            var root = canvas.transform;

            // vida, embaixo a esquerda
            var hp = UiKit.Rect("Health", root, new Vector2(0, 0), new Vector2(48, 48), new Vector2(430, 34));
            UiKit.Image(hp, null, UiKit.Ink);
            hpLag = Bar(hp, new Color(1f, 0.85f, 0.75f, 0.9f));
            hpFill = Bar(hp, new Color(0.9f, 0.22f, 0.25f));
            hpText = UiKit.Text(UiKit.Stretch("Text", hp), "", 22, UiKit.Paper);
            var hpLabel = UiKit.Rect("Label", hp, new Vector2(0, 1), new Vector2(2, 6), new Vector2(200, 24));
            hpLabel.pivot = new Vector2(0, 0);
            UiKit.Text(hpLabel, "VIDA", 20, UiKit.Dim, TextAnchor.LowerLeft);

            // roda de elementos, embaixo no centro
            var wheel = UiKit.Rect("Elements", root, new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(420, 96));
            for (int i = 0; i < 4; i++)
            {
                var s = new Slot { Root = UiKit.Rect("Slot" + i, wheel, new Vector2(0.5f, 0.5f), new Vector2((i - 1.5f) * 100f, 0), new Vector2(72, 72)) };
                UiKit.Image(s.Root, UiKit.Circle, UiKit.Ink);
                s.Icon = UiKit.Image(UiKit.Rect("Icon", s.Root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56)), UiKit.Circle, Color.gray);
                s.Cooldown = UiKit.Image(UiKit.Rect("Cooldown", s.Root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56)), UiKit.Circle, new Color(0.02f, 0.02f, 0.05f, 0.8f));
                s.Cooldown.type = Image.Type.Filled;
                s.Cooldown.fillMethod = Image.FillMethod.Radial360;
                s.Cooldown.fillOrigin = (int)Image.Origin360.Top;
                s.Cooldown.fillClockwise = false;
                s.Ring = UiKit.Image(UiKit.Rect("Ring", s.Root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88)), UiKit.Ring, Color.white);
                slots[i] = s;
            }
            Hint(wheel, new Vector2(-222, 0), "Q");
            Hint(wheel, new Vector2(222, 0), "E");

            // cargas de dash
            for (int i = 0; i < 2; i++)
                dashPips[i] = UiKit.Image(UiKit.Rect("Dash" + i, wheel, new Vector2(0.5f, 0), new Vector2((i - 0.5f) * 30f, -30), new Vector2(22, 10)), null, UiKit.Paper);

            // quintessencia, acima da roda
            var q = UiKit.Rect("Quintessence", root, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(380, 16));
            UiKit.Image(q, null, UiKit.Ink);
            quintFill = Bar(q, Color.white);
            quintText = UiKit.Text(UiKit.Rect("Text", q, new Vector2(0.5f, 1), new Vector2(0, 4), new Vector2(380, 26)), "", 20, UiKit.Paper);
            ((RectTransform)quintText.transform).pivot = new Vector2(0.5f, 0);

            // essencia e sala, em cima
            essenceText = UiKit.Text(UiKit.Rect("Essence", root, new Vector2(1, 1), new Vector2(-48, -40), new Vector2(360, 40)), "Essência 0", 30, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleRight);
            roomText = UiKit.Text(UiKit.Rect("Room", root, new Vector2(0, 1), new Vector2(48, -40), new Vector2(700, 40)), "", 30, UiKit.Paper, TextAnchor.MiddleLeft);
            UiKit.Text(UiKit.Rect("Keys", root, new Vector2(1, 0), new Vector2(-48, 40), new Vector2(700, 26)),
                "WASD mover · clique ataca · clique dir. skill · Espaço dash · R Quintessência · Tab mapa · Esc pausa", 19, UiKit.Dim, TextAnchor.MiddleRight, false);

            // aviso de interacao
            promptBox = UiKit.Rect("Prompt", root, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(560, 54));
            UiKit.Image(promptBox, null, UiKit.Ink);
            promptText = UiKit.Text(UiKit.Stretch("Text", promptBox), "", 28, UiKit.Paper);
            promptBox.gameObject.SetActive(false);

            mapPanel = Panel("Map", new Color(0.03f, 0.02f, 0.06f, 0.82f));
            UiKit.Text(UiKit.Rect("Title", mapPanel, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(800, 60)), "MAPA", 48, UiKit.Paper);
            mapContent = UiKit.Rect("Content", mapPanel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 600));
            resultPanel = Panel("Result", new Color(0.03f, 0.02f, 0.06f, 0.9f));
            pausePanel = Panel("Pause", new Color(0.03f, 0.02f, 0.06f, 0.85f));

            // escurecimento entre salas, por cima de tudo
            fade = UiKit.Image(UiKit.Stretch("Fade", root), null, new Color(0, 0, 0, 0));
        }

        static Image Bar(RectTransform parent, Color color)
        {
            var rt = UiKit.Stretch("Fill", parent);
            rt.offsetMin = new Vector2(3, 3); rt.offsetMax = new Vector2(-3, -3);
            var img = UiKit.Image(rt, null, color);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 0);
            return img;
        }

        static void SetBar(Image bar, float value)
        {
            var rt = (RectTransform)bar.transform;
            float full = ((RectTransform)rt.parent).rect.width - 6f;
            rt.sizeDelta = new Vector2(full * Mathf.Clamp01(value), -6f);
            rt.anchoredPosition = new Vector2(3, 0);
        }

        static void Hint(RectTransform parent, Vector2 pos, string key)
        {
            var rt = UiKit.Rect("Key" + key, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(38, 38));
            UiKit.Image(rt, null, UiKit.Ink);
            UiKit.Text(UiKit.Stretch("T", rt), key, 22, UiKit.Paper);
        }

        RectTransform Panel(string name, Color color)
        {
            var rt = UiKit.Stretch(name, canvas.transform);
            UiKit.Image(rt, null, color, true);
            rt.gameObject.SetActive(false);
            return rt;
        }

        void LateUpdate()
        {
            if (hero == null || (hero as UnityEngine.Object) == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                hero = go != null ? go.GetComponent<IHeroStatus>() : null;
                if (hero == null) return;
            }

            float hp = hero.Health.Max > 0f ? hero.Health.Current / hero.Health.Max : 0f;
            lagHp = hp > lagHp ? hp : Mathf.MoveTowards(lagHp, hp, Time.unscaledDeltaTime * 0.6f);
            SetBar(hpFill, hp);
            SetBar(hpLag, lagHp);
            hpText.text = $"{Mathf.CeilToInt(hero.Health.Current)} / {Mathf.RoundToInt(hero.Health.Max)}";

            var awakened = hero.AwakenedElements;
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                bool has = false;
                foreach (var a in awakened) if (a == e) has = true;
                bool current = has && hero.CurrentElement == e;
                var c = ElementColors.Of(e);
                slots[i].Icon.color = has ? c : new Color(0.2f, 0.19f, 0.26f);
                slots[i].Cooldown.fillAmount = has ? hero.SkillCooldown01(e) : 0f;
                slots[i].Ring.enabled = current;
                slots[i].Ring.color = Color.Lerp(c, Color.white, 0.5f);
                slots[i].Root.localScale = Vector3.one * (current ? 1.22f : has ? 1f : 0.82f);
            }

            for (int i = 0; i < dashPips.Length; i++)
            {
                dashPips[i].enabled = i < hero.DashMaxCharges;
                dashPips[i].color = i < hero.DashCharges ? UiKit.Paper : new Color(1, 1, 1, 0.2f);
            }

            bool all = awakened.Count >= 4;
            var prism = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.6f, 1f), 0.5f, 1f);
            SetBar(quintFill, hero.Quintessence01);
            quintFill.color = hero.QuintessenceActive || hero.QuintessenceReady ? prism : new Color(0.8f, 0.8f, 0.9f);
            quintText.text = hero.QuintessenceActive ? "QUINTESSÊNCIA"
                : hero.QuintessenceReady ? "R — QUINTESSÊNCIA"
                : all ? "" : $"Quintessência: desperte os 4 elementos ({awakened.Count}/4)";
            quintText.color = hero.QuintessenceReady || hero.QuintessenceActive ? prism : UiKit.Dim;
            quintText.fontSize = hero.QuintessenceReady || hero.QuintessenceActive ? 26 : 19;
        }

        // ---------- API usada pela run ----------

        public void SetRoom(string text) => roomText.text = text;
        public void SetEssence(int amount) => essenceText.text = "Essência " + amount;
        public void SetFade(float alpha) => fade.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));

        public void ShowPrompt(string text)
        {
            promptText.text = text;
            if (!promptBox.gameObject.activeSelf) promptBox.gameObject.SetActive(true);
        }

        public void HidePrompt() { if (promptBox.gameObject.activeSelf) promptBox.gameObject.SetActive(false); }

        public bool MapVisible => mapPanel.gameObject.activeSelf;

        public void ShowMap(RunState run, bool show)
        {
            if (show == mapPanel.gameObject.activeSelf) return;
            mapPanel.gameObject.SetActive(show);
            if (!show) return;
            foreach (Transform child in mapContent) Destroy(child.gameObject);
            var map = run.Map;
            var pos = new Dictionary<int, Vector2>();
            float w = mapContent.sizeDelta.x, h = mapContent.sizeDelta.y;
            for (int layer = 0; layer < map.LayerCount; layer++)
            {
                var nodes = map.Layer(layer);
                for (int i = 0; i < nodes.Count; i++)
                    pos[nodes[i].Id] = new Vector2(-w / 2 + w * layer / (map.LayerCount - 1f), (i - (nodes.Count - 1) / 2f) * (h / 3.2f));
            }
            var choices = new HashSet<int>(run.Current.Next);
            foreach (var n in map.Nodes)
                foreach (int next in n.Next)
                {
                    bool walked = run.Visited.Contains(n.Id) && run.Visited.Contains(next);
                    bool open = n.Id == run.Current.Id;
                    Line(pos[n.Id], pos[next], walked ? UiKit.Paper : open ? new Color(1, 1, 1, 0.6f) : new Color(1, 1, 1, 0.16f), walked ? 5f : 3f);
                }
            foreach (var n in map.Nodes)
            {
                bool visited = run.Visited.Contains(n.Id), here = n.Id == run.Current.Id, choice = choices.Contains(n.Id);
                var c = RoomInfo.Color(n.Type);
                float alpha = here || choice ? 1f : visited ? 0.55f : 0.3f;
                var node = UiKit.Rect("Node" + n.Id, mapContent, new Vector2(0.5f, 0.5f), pos[n.Id], Vector2.one * (n.Type == RoomType.Boss ? 78 : 56));
                UiKit.Image(node, UiKit.Circle, new Color(c.r, c.g, c.b, alpha));
                if (here) UiKit.Image(UiKit.Rect("Here", node, new Vector2(0.5f, 0.5f), Vector2.zero, node.sizeDelta + Vector2.one * 26), UiKit.Ring, Color.white);
                var label = UiKit.Rect("Label", node, new Vector2(0.5f, 0), new Vector2(0, -8), new Vector2(220, 26));
                label.pivot = new Vector2(0.5f, 1);
                UiKit.Text(label, here ? "VOCÊ · " + RoomInfo.Name(n.Type) : RoomInfo.Name(n.Type), 21, new Color(1, 1, 1, Mathf.Max(alpha, 0.45f)));
            }
        }

        void Line(Vector2 a, Vector2 b, Color color, float thickness)
        {
            var d = b - a;
            var rt = UiKit.Rect("Line", mapContent, new Vector2(0.5f, 0.5f), (a + b) / 2f, new Vector2(d.magnitude, thickness));
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            UiKit.Image(rt, null, color);
        }

        public bool PauseVisible => pausePanel.gameObject.activeSelf;

        public void ShowPause(bool show, Action resume = null, Action menu = null)
        {
            pausePanel.gameObject.SetActive(show);
            if (!show) return;
            foreach (Transform child in pausePanel) Destroy(child.gameObject);
            UiKit.Text(UiKit.Rect("Title", pausePanel, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(800, 80)), "PAUSADO", 64, UiKit.Paper);
            UiKit.Button(pausePanel, "Continuar", new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(380, 70), new Color(0.2f, 0.45f, 0.7f), resume);
            UiKit.Button(pausePanel, "Menu principal", new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(380, 70), new Color(0.3f, 0.28f, 0.4f), menu);
        }

        public bool ResultVisible => resultPanel.gameObject.activeSelf;

        public void ShowResult(bool victory, string stats, Action again, Action menu)
        {
            resultPanel.gameObject.SetActive(true);
            foreach (Transform child in resultPanel) Destroy(child.gameObject);
            UiKit.Text(UiKit.Rect("Title", resultPanel, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1200, 100)),
                victory ? "VITÓRIA" : "VOCÊ CAIU", 84, victory ? new Color(1f, 0.85f, 0.4f) : new Color(0.95f, 0.3f, 0.3f));
            UiKit.Text(UiKit.Rect("Stats", resultPanel, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1200, 120)), stats, 30, UiKit.Paper, TextAnchor.MiddleCenter, false);
            UiKit.Button(resultPanel, "Jogar de novo", new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(380, 70), new Color(0.2f, 0.45f, 0.7f), again);
            UiKit.Button(resultPanel, "Menu principal", new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(380, 70), new Color(0.3f, 0.28f, 0.4f), menu);
        }
    }
}
