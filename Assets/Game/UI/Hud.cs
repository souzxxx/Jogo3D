using System;
using System.Collections.Generic;
using Quintessence.Core;
using Quintessence.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Quintessence.UI
{
    /// HUD da run. Embaixo: vida a esquerda e a barra de habilidades no centro (os quatro elementos, skill, dash,
    /// Quintessencia, cada um com o icone e a tecla). Em cima: sala, bencaos, essencia e a barra do chefe.
    /// Tambem monta mapa, pausa, resultado e o aviso de interacao.
    public class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        static readonly string[] ElementIcon = { "", "fire", "water", "earth", "air" };
        static readonly string[] SkillIcon = { "", "skill_fire", "skill_water", "skill_earth", "skill_air" };
        static readonly string[] SkillName = { "", "Labareda", "Maré", "Pilar de Pedra", "Vendaval" };
        static readonly Color Slate = new(0.2f, 0.19f, 0.26f);

        Canvas canvas;
        IHeroStatus hero;
        Image hpFill, hpLag, fade, roomIcon, bossFill;
        Text hpText, essenceText, roomText, promptText, bossName, bossHint, skillLabel, quintLabel;
        RectTransform promptBox, mapPanel, resultPanel, pausePanel, mapContent, bossBox, boonRow;
        readonly Slot[] slots = new Slot[4];
        Tile skillTile, dashTile, quintTile;
        readonly Image[] dashPips = new Image[2];
        float lagHp = 1f;
        Health boss;
        bool bossShield, bossNeedsQuint;
        int bossPhase = 1;
        string bossTitle = "";
        IReadOnlyList<Boon> boons;

        class Slot { public RectTransform Root; public Image Disc, Icon, Cooldown, Ring; }
        class Tile { public RectTransform Root; public Image Back, Icon, Cooldown, Ring; public Text Key; }

        void Awake()
        {
            Instance = this;
            UiKit.EnsureEventSystem();
            Build();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnEnable()
        {
            GameEvents.BossSpawned += OnBoss;
            GameEvents.BossPhaseChanged += OnBossPhase;
            GameEvents.BossShieldChanged += OnBossShield;
        }

        void OnDisable()
        {
            GameEvents.BossSpawned -= OnBoss;
            GameEvents.BossPhaseChanged -= OnBossPhase;
            GameEvents.BossShieldChanged -= OnBossShield;
        }

        void OnBoss(Health b, string name) { boss = b; bossTitle = name; bossPhase = 1; bossShield = false; }
        void OnBossPhase(int phase, int total) => bossPhase = phase;
        void OnBossShield(bool up, bool needsQuintessence) { bossShield = up; bossNeedsQuint = needsQuintessence; }

        /// Para gravacoes: desenha o HUD pela camera (o modo Overlay nao aparece em captura de camera).
        public void UseCamera(Camera cam)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }

        // ---------- montagem ----------

        void Build()
        {
            canvas = UiKit.Canvas("HudCanvas", 10, transform);
            var root = canvas.transform;
            var center = new Vector2(0.5f, 0.5f);

            // vida
            var hpBox = UiKit.Box("Health", root, new Vector2(0, 0), new Vector2(40, 40), new Vector2(470, 76));
            UiKit.Image(UiKit.Rect("Heart", hpBox, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(46, 46)), UiKit.Icon("heart"), new Color(0.78f, 0.25f, 0.24f));
            var hpBar = UiKit.Rect("Bar", hpBox, new Vector2(0, 0.5f), new Vector2(76, 0), new Vector2(376, 30));
            var hpBack = UiKit.Image(hpBar, UiKit.Panel, new Color(0.02f, 0.02f, 0.04f, 0.9f));
            hpBack.type = Image.Type.Sliced;
            hpLag = Bar(hpBar, new Color(0.9f, 0.8f, 0.68f, 0.9f));
            hpFill = Bar(hpBar, new Color(0.72f, 0.22f, 0.2f));
            hpText = UiKit.Text(UiKit.Stretch("Text", hpBar), "", 22, UiKit.Cream);

            // barra de habilidades
            var bar = UiKit.Box("Abilities", root, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(820, 132));
            KeyCap(bar, new Vector2(-376, 8), "Q");
            for (int i = 0; i < 4; i++)
            {
                var s = new Slot { Root = UiKit.Rect("Element" + i, bar, center, new Vector2(-300 + i * 84, 8), new Vector2(72, 72)) };
                UiKit.Image(s.Root, UiKit.Circle, new Color(0.02f, 0.02f, 0.04f, 0.9f));
                s.Disc = UiKit.Image(UiKit.Rect("Disc", s.Root, center, Vector2.zero, new Vector2(64, 64)), UiKit.Circle, Slate);
                s.Icon = UiKit.Image(UiKit.Rect("Icon", s.Root, center, Vector2.zero, new Vector2(42, 42)), UiKit.Icon(ElementIcon[i + 1]), UiKit.Cream);
                s.Cooldown = Radial(UiKit.Rect("Cooldown", s.Root, center, Vector2.zero, new Vector2(64, 64)), UiKit.Circle);
                s.Ring = UiKit.Image(UiKit.Rect("Ring", s.Root, center, Vector2.zero, new Vector2(86, 86)), UiKit.Ring, UiKit.Cream);
                slots[i] = s;
            }
            KeyCap(bar, new Vector2(28, 8), "E");
            UiKit.Image(UiKit.Rect("Divider", bar, center, new Vector2(74, 0), new Vector2(2, 96)), null, new Color(1, 1, 1, 0.12f));

            skillTile = MakeTile(bar, new Vector2(138, 8), "skill_fire", "BOTÃO DIR.");
            dashTile = MakeTile(bar, new Vector2(238, 8), "dash", "ESPAÇO");
            quintTile = MakeTile(bar, new Vector2(338, 8), "quintessence", "R");
            for (int i = 0; i < 2; i++)
                dashPips[i] = UiKit.Image(UiKit.Rect("Pip" + i, dashTile.Root, new Vector2(0.5f, 1), new Vector2((i - 0.5f) * 20f, 8), new Vector2(14, 8)), null, UiKit.Cream);
            skillLabel = UiKit.Text(UiKit.Rect("SkillName", bar, new Vector2(0.5f, 1), new Vector2(0, 30), new Vector2(820, 30)), "", 24, UiKit.Cream);
            quintLabel = UiKit.Text(UiKit.Rect("QuintHint", bar, new Vector2(0.5f, 1), new Vector2(0, 62), new Vector2(820, 30)), "", 24, UiKit.Cream);

            // sala e bencaos
            var roomBox = UiKit.Box("Room", root, new Vector2(0, 1), new Vector2(40, -40), new Vector2(420, 60));
            roomIcon = UiKit.Image(UiKit.Rect("Icon", roomBox, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(40, 40)), UiKit.Icon("room_combat"), UiKit.Cream);
            roomText = UiKit.Text(UiKit.Rect("Text", roomBox, new Vector2(0, 0.5f), new Vector2(68, 0), new Vector2(340, 40)), "", 28, UiKit.Cream, TextAnchor.MiddleLeft);
            boonRow = UiKit.Rect("Boons", root, new Vector2(0, 1), new Vector2(44, -112), new Vector2(600, 44));

            // essencia
            var essBox = UiKit.Box("Essence", root, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(190, 60));
            UiKit.Image(UiKit.Rect("Icon", essBox, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(38, 38)), UiKit.Icon("essence"), new Color(0.85f, 0.72f, 0.4f));
            essenceText = UiKit.Text(UiKit.Rect("Text", essBox, new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(120, 40)), "0", 30, UiKit.Cream, TextAnchor.MiddleRight);

            // chefe
            bossBox = UiKit.Box("Boss", root, new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(920, 40));
            UiKit.Image(UiKit.Rect("Skull", bossBox, new Vector2(0, 0.5f), new Vector2(-52, 6), new Vector2(60, 60)), UiKit.Icon("room_boss"), new Color(0.85f, 0.6f, 0.3f));
            var bossBar = UiKit.Stretch("Bar", bossBox);
            bossBar.offsetMin = new Vector2(6, 6); bossBar.offsetMax = new Vector2(-6, -6);
            bossFill = Bar(bossBar, new Color(0.8f, 0.42f, 0.16f));
            for (int i = 1; i < 3; i++)
                UiKit.Image(UiKit.Rect("Mark" + i, bossBox, new Vector2(i / 3f, 0.5f), Vector2.zero, new Vector2(4, 40)), null, new Color(0.02f, 0.02f, 0.04f, 0.85f));
            bossName = UiKit.Text(UiKit.Rect("Name", bossBox, new Vector2(0.5f, 1), new Vector2(0, 4), new Vector2(920, 40)), "", 30, UiKit.Cream);
            ((RectTransform)bossName.transform).pivot = new Vector2(0.5f, 0);
            bossHint = UiKit.Text(UiKit.Rect("Hint", bossBox, new Vector2(0.5f, 0), new Vector2(0, -8), new Vector2(920, 34)), "", 26, new Color(0.78f, 0.84f, 0.95f));
            ((RectTransform)bossHint.transform).pivot = new Vector2(0.5f, 1);
            bossBox.gameObject.SetActive(false);

            // aviso de interacao: tecla + texto
            promptBox = UiKit.Box("Prompt", root, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(520, 60));
            KeyCap(promptBox, new Vector2(-220, 0), "F");
            promptText = UiKit.Text(UiKit.Rect("Text", promptBox, center, new Vector2(24, 0), new Vector2(420, 40)), "", 28, UiKit.Cream);
            promptBox.gameObject.SetActive(false);

            mapPanel = Overlay("Map", new Color(0.03f, 0.02f, 0.06f, 0.86f));
            UiKit.Text(UiKit.Rect("Title", mapPanel, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(800, 70)), "MAPA", 56, UiKit.Cream);
            mapContent = UiKit.Rect("Content", mapPanel, center, new Vector2(0, -20), new Vector2(1500, 600));
            resultPanel = Overlay("Result", new Color(0.03f, 0.02f, 0.06f, 0.9f));
            pausePanel = Overlay("Pause", new Color(0.03f, 0.02f, 0.06f, 0.86f));

            fade = UiKit.Image(UiKit.Stretch("Fade", root), null, new Color(0, 0, 0, 0));
        }

        Tile MakeTile(RectTransform parent, Vector2 pos, string icon, string key)
        {
            var center = new Vector2(0.5f, 0.5f);
            var t = new Tile { Root = UiKit.Rect("Tile_" + icon, parent, center, pos, new Vector2(80, 80)) };
            t.Back = UiKit.Image(t.Root, UiKit.Panel, new Color(0.02f, 0.02f, 0.04f, 0.9f));
            t.Back.type = Image.Type.Sliced;
            t.Icon = UiKit.Image(UiKit.Rect("Icon", t.Root, center, Vector2.zero, new Vector2(56, 56)), UiKit.Icon(icon), UiKit.Cream);
            t.Cooldown = Radial(UiKit.Stretch("Cooldown", t.Root), UiKit.Panel);
            t.Ring = UiKit.Image(UiKit.Stretch("Ring", t.Root), UiKit.Frame, UiKit.Cream);
            t.Ring.type = Image.Type.Sliced;
            var k = UiKit.Rect("Key", t.Root, new Vector2(0.5f, 0), new Vector2(0, -6), new Vector2(120, 22));
            k.pivot = new Vector2(0.5f, 1);
            t.Key = UiKit.Text(k, key, 17, UiKit.Dim);
            return t;
        }

        static Image Radial(RectTransform rt, Sprite sprite)
        {
            var img = UiKit.Image(rt, sprite, new Color(0.02f, 0.02f, 0.05f, 0.78f));
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.fillOrigin = (int)Image.Origin360.Top;
            img.fillClockwise = false;
            img.fillAmount = 0f;
            return img;
        }

        static void KeyCap(RectTransform parent, Vector2 pos, string key)
        {
            var rt = UiKit.Rect("Key" + key, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(40, 40));
            var bg = UiKit.Image(rt, UiKit.Panel, new Color(0.85f, 0.8f, 0.7f, 0.16f));
            bg.type = Image.Type.Sliced;
            UiKit.Text(UiKit.Stretch("T", rt), key, 22, UiKit.Cream);
        }

        static Image Bar(RectTransform parent, Color color)
        {
            var rt = UiKit.Stretch("Fill", parent);
            var img = UiKit.Image(rt, null, color);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchorMin = new Vector2(0, 0);
            return img;
        }

        static void SetBar(Image bar, float value)
        {
            var rt = (RectTransform)bar.transform;
            float full = ((RectTransform)rt.parent).rect.width - 8f;
            rt.sizeDelta = new Vector2(full * Mathf.Clamp01(value), -8f);
            rt.anchoredPosition = new Vector2(4, 0);
        }

        RectTransform Overlay(string name, Color color)
        {
            var rt = UiKit.Stretch(name, canvas.transform);
            UiKit.Image(rt, null, color, true);
            rt.gameObject.SetActive(false);
            return rt;
        }

        // ---------- atualizacao ----------

        void LateUpdate()
        {
            if (hero == null || (hero as UnityEngine.Object) == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                hero = go != null ? go.GetComponent<IHeroStatus>() : null;
                if (hero == null) return;
            }

            bool showBoss = boss != null && !boss.IsDead;
            if (bossBox.gameObject.activeSelf != showBoss) bossBox.gameObject.SetActive(showBoss);
            if (showBoss)
            {
                SetBar(bossFill, boss.Current / boss.Max);
                bossFill.color = bossShield ? new Color(0.55f, 0.62f, 0.75f) : new Color(0.8f, 0.42f, 0.16f);
                bossName.text = $"{bossTitle.ToUpperInvariant()}  ·  FASE {bossPhase}/3";
                bossHint.text = !bossShield ? "" : bossNeedsQuint
                    ? (hero.QuintessenceReady ? "ESCUDO — aperte R e ataque!" : hero.QuintessenceActive ? "ESCUDO — ataque agora!" : "ESCUDO — cause reações nos reforços para encher a Quintessência")
                    : "ESCUDO — aguente firme, ele vai ceder";
            }

            float hp = hero.Health.Max > 0f ? hero.Health.Current / hero.Health.Max : 0f;
            lagHp = hp > lagHp ? hp : Mathf.MoveTowards(lagHp, hp, Time.unscaledDeltaTime * 0.6f);
            SetBar(hpFill, hp);
            SetBar(hpLag, lagHp);
            hpText.text = $"{Mathf.CeilToInt(hero.Health.Current)} / {Mathf.RoundToInt(hero.Health.Max)}";

            var awakened = hero.AwakenedElements;
            var current = hero.CurrentElement;
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                bool has = false;
                foreach (var a in awakened) if (a == e) has = true;
                bool isCurrent = has && current == e;
                var c = ElementColors.Of(e);
                var s = slots[i];
                s.Disc.color = has ? (isCurrent ? c : Color.Lerp(c, Slate, 0.45f)) : Slate;
                s.Icon.sprite = UiKit.Icon(has ? ElementIcon[i + 1] : "lock");
                s.Icon.color = has ? UiKit.Cream : new Color(1, 1, 1, 0.28f);
                s.Cooldown.fillAmount = has && !isCurrent ? hero.SkillCooldown01(e) : 0f;
                s.Ring.enabled = isCurrent;
                s.Root.localScale = Vector3.one * (isCurrent ? 1.16f : 1f);
            }

            // skill do elemento atual
            int ci = (int)current;
            float cd = hero.SkillCooldown01(current);
            skillTile.Icon.sprite = UiKit.Icon(SkillIcon[Mathf.Clamp(ci, 0, 4)]);
            skillTile.Icon.color = cd > 0f ? new Color(1, 1, 1, 0.35f) : Color.Lerp(ElementColors.Of(current), UiKit.Cream, 0.35f);
            skillTile.Cooldown.fillAmount = cd;
            skillTile.Ring.color = cd > 0f ? new Color(1, 1, 1, 0.15f) : new Color(UiKit.Cream.r, UiKit.Cream.g, UiKit.Cream.b, 0.7f);
            skillLabel.text = SkillName[Mathf.Clamp(ci, 0, 4)];

            // dash
            bool canDash = hero.DashCharges > 0;
            dashTile.Icon.color = canDash ? UiKit.Cream : new Color(1, 1, 1, 0.3f);
            dashTile.Ring.color = canDash ? new Color(UiKit.Cream.r, UiKit.Cream.g, UiKit.Cream.b, 0.7f) : new Color(1, 1, 1, 0.15f);
            dashTile.Cooldown.fillAmount = 0f;
            for (int i = 0; i < dashPips.Length; i++)
            {
                dashPips[i].enabled = hero.DashMaxCharges > 1 && i < hero.DashMaxCharges;
                dashPips[i].color = i < hero.DashCharges ? UiKit.Cream : new Color(1, 1, 1, 0.2f);
            }

            // quintessencia: o escurecimento do icone e o medidor (some conforme enche)
            bool all = awakened.Count >= 4;
            bool on = hero.QuintessenceActive, ready = hero.QuintessenceReady;
            var prism = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.5f, 1f), 0.35f, 1f);
            quintTile.Icon.sprite = UiKit.Icon(all ? "quintessence" : "lock");
            quintTile.Icon.color = !all ? new Color(1, 1, 1, 0.28f) : on || ready ? prism : new Color(1, 1, 1, 0.5f);
            quintTile.Cooldown.fillAmount = !all ? 0f : on ? 1f - hero.Quintessence01 : ready ? 0f : 1f - hero.Quintessence01;
            quintTile.Ring.color = on || ready ? prism : new Color(1, 1, 1, 0.15f);
            quintTile.Root.localScale = Vector3.one * (ready ? 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f) : 1f);
            quintLabel.text = on ? "QUINTESSÊNCIA" : ready ? "Quintessência pronta — aperte R" : "";
            quintLabel.color = prism;
            quintTile.Key.text = all ? "R" : $"{awakened.Count}/4 ELEMENTOS";
        }

        // ---------- API usada pela run ----------

        public void SetRoom(string text) => roomText.text = text;

        public void SetRoom(string text, RoomType type)
        {
            roomText.text = text;
            roomIcon.sprite = UiKit.Icon(RoomInfo.Icon(type));
            roomIcon.color = Color.Lerp(RoomInfo.Color(type), UiKit.Cream, 0.4f);
        }

        public void SetEssence(int amount) => essenceText.text = amount.ToString();
        public void SetFade(float alpha) => fade.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));

        /// Uma medalha por bencao, na cor do mestre que a deu. Os nomes aparecem na pausa.
        public void SetBoons(IReadOnlyList<Boon> list)
        {
            boons = list;
            foreach (Transform child in boonRow) Destroy(child.gameObject);
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i].patron == ElementType.None ? new Color(0.85f, 0.72f, 0.4f) : ElementColors.Of(list[i].patron);
                var rt = UiKit.Rect("Boon" + i, boonRow, new Vector2(0, 0.5f), new Vector2(i * 46f, 0), new Vector2(40, 40));
                UiKit.Image(rt, UiKit.Circle, new Color(0.07f, 0.06f, 0.1f, 0.85f));
                UiKit.Image(UiKit.Rect("Icon", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28)), UiKit.Icon("boon"), Color.Lerp(c, UiKit.Cream, 0.25f));
            }
        }

        public void ShowPrompt(string text)
        {
            promptText.text = text.StartsWith("F — ") ? text.Substring(4) : text;
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
                    Line(pos[n.Id], pos[next], walked ? UiKit.Cream : open ? new Color(1, 1, 1, 0.55f) : new Color(1, 1, 1, 0.14f), walked ? 5f : 3f);
                }
            var center = new Vector2(0.5f, 0.5f);
            foreach (var n in map.Nodes)
            {
                bool visited = run.Visited.Contains(n.Id), here = n.Id == run.Current.Id, choice = choices.Contains(n.Id);
                var c = RoomInfo.Color(n.Type);
                float alpha = here || choice ? 1f : visited ? 0.6f : 0.38f;
                float size = n.Type == RoomType.Boss ? 92 : 68;
                var node = UiKit.Rect("Node" + n.Id, mapContent, center, pos[n.Id], Vector2.one * size);
                UiKit.Image(node, UiKit.Circle, new Color(0.05f, 0.04f, 0.08f, 1f));
                UiKit.Image(UiKit.Rect("Disc", node, center, Vector2.zero, Vector2.one * (size - 8)), UiKit.Circle, new Color(c.r, c.g, c.b, alpha));
                UiKit.Image(UiKit.Rect("Icon", node, center, Vector2.zero, Vector2.one * (size * 0.56f)), UiKit.Icon(RoomInfo.Icon(n.Type)), new Color(UiKit.Cream.r, UiKit.Cream.g, UiKit.Cream.b, Mathf.Max(alpha, 0.5f)));
                if (here) UiKit.Image(UiKit.Rect("Here", node, center, Vector2.zero, Vector2.one * (size + 22)), UiKit.Ring, UiKit.Cream);
                var label = UiKit.Rect("Label", node, new Vector2(0.5f, 0), new Vector2(0, -8), new Vector2(240, 28));
                label.pivot = new Vector2(0.5f, 1);
                UiKit.Text(label, here ? "VOCÊ ESTÁ AQUI" : RoomInfo.Name(n.Type), 21, new Color(UiKit.Cream.r, UiKit.Cream.g, UiKit.Cream.b, Mathf.Max(alpha, 0.5f)));
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
            var center = new Vector2(0.5f, 0.5f);
            UiKit.Text(UiKit.Rect("Title", pausePanel, center, new Vector2(0, 300), new Vector2(800, 80)), "PAUSADO", 72, UiKit.Cream);
            UiKit.Button(pausePanel, "Continuar", center, new Vector2(0, 180), new Vector2(380, 70), new Color(0.24f, 0.4f, 0.55f), resume);
            UiKit.Button(pausePanel, "Menu principal", center, new Vector2(0, 90), new Vector2(380, 70), new Color(0.3f, 0.28f, 0.36f), menu);
            if (boons != null && boons.Count > 0)
            {
                UiKit.Text(UiKit.Rect("BoonsTitle", pausePanel, center, new Vector2(0, -10), new Vector2(900, 40)), "BÊNÇÃOS", 30, UiKit.Dim);
                for (int i = 0; i < boons.Count; i++)
                    UiKit.Text(UiKit.Rect("Boon" + i, pausePanel, center, new Vector2(0, -56 - i * 34), new Vector2(1100, 32)),
                        boons[i].name + " — " + boons[i].description, 24, UiKit.Cream, TextAnchor.MiddleCenter, false);
            }
            UiKit.Text(UiKit.Rect("Keys", pausePanel, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(1700, 30)),
                "WASD mover · mouse mira · clique ataca · botão direito skill · Espaço dash · Q/E troca elemento · R Quintessência · F interagir · Tab mapa",
                22, UiKit.Dim, TextAnchor.MiddleCenter, false);
        }

        public bool ResultVisible => resultPanel.gameObject.activeSelf;

        public void ShowResult(bool victory, string stats, Action again, Action menu)
        {
            resultPanel.gameObject.SetActive(true);
            foreach (Transform child in resultPanel) Destroy(child.gameObject);
            var center = new Vector2(0.5f, 0.5f);
            var title = UiKit.Text(UiKit.Rect("Title", resultPanel, center, new Vector2(0, 200), new Vector2(1400, 120)),
                victory ? "VITÓRIA" : "VOCÊ CAIU", 104, victory ? new Color(0.9f, 0.76f, 0.42f) : new Color(0.78f, 0.3f, 0.28f));
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Stats", resultPanel, center, new Vector2(0, 70), new Vector2(1400, 120)), stats, 30, UiKit.Cream, TextAnchor.MiddleCenter, false);
            UiKit.Button(resultPanel, "Jogar de novo", center, new Vector2(0, -60), new Vector2(380, 70), new Color(0.24f, 0.4f, 0.55f), again);
            UiKit.Button(resultPanel, "Menu principal", center, new Vector2(0, -150), new Vector2(380, 70), new Color(0.3f, 0.28f, 0.36f), menu);
        }
    }
}
