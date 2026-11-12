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
        readonly Tile[] skillTiles = new Tile[4];
        Tile dashTile, quintTile;
        float[] lastCd = new float[4];
        float skillLabelLeft;
        readonly Image[] dashPips = new Image[2];
        float lagHp = 1f;
        Health boss;
        bool bossShield, bossNeedsQuint;
        int bossPhase = 1;
        string bossTitle = "";
        IReadOnlyList<Boon> boons;

        Text levelText;
        Image xpFill;
        RectTransform xpRoot, levelPanel;
        Progression progress;
        TalentBook talents;

        /// Liga a barra de nivel a run.
        public void SetProgress(Progression p, TalentBook book)
        {
            progress = p; talents = book;
            xpRoot.gameObject.SetActive(p != null);
        }

        int[] ranks;
        /// Graus das skills (do Santuario), para a barra mostrar II e III.
        public void SetRanks(int[] skillRanks) => ranks = skillRanks;

        public bool LevelUpVisible => levelPanel != null && levelPanel.gameObject.activeSelf;

        static string TalentIcon(Talent t) => t.Second != ElementType.None ? "boon_reaction" : t.Element != ElementType.None ? SkillIcon[(int)t.Element] : t.Effect switch
        {
            TalentEffect.MaxHealth => "boon_health",
            TalentEffect.Essence => "essence",
            TalentEffect.SkillRefundOnKill => "boon_skill",
            TalentEffect.Finisher => "boon_crit",
            _ => "boon_damage",
        };

        /// Subiu de nivel: tres talentos, escolhe um (clique, 1/2/3 ou controle).
        public void ShowLevelUp(int level, IReadOnlyList<Talent> offer, Action<int> pick, int rerolls = 0, Action reroll = null)
        {
            if (levelPanel == null) levelPanel = Overlay("LevelUp", new Color(0.03f, 0.02f, 0.06f, 0.84f));
            foreach (Transform child in levelPanel) Destroy(child.gameObject);
            levelPanel.gameObject.SetActive(offer != null);
            if (offer == null) return;
            levelPanel.SetAsLastSibling();
            fade.transform.SetAsLastSibling();
            var center = new Vector2(0.5f, 0.5f);
            var title = UiKit.Text(UiKit.Rect("Title", levelPanel, center, new Vector2(0, 330), new Vector2(1400, 100)), "NÍVEL " + level, 84, new Color(0.9f, 0.76f, 0.42f));
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Sub", levelPanel, center, new Vector2(0, 258), new Vector2(1400, 40)), "Escolha um talento", 30, UiKit.Dim, TextAnchor.MiddleCenter, false);
            for (int i = 0; i < offer.Count; i++)
            {
                var t = offer[i];
                int index = i;
                var c = t.Element == ElementType.None ? new Color(0.85f, 0.72f, 0.4f)
                    : t.Second != ElementType.None ? Color.Lerp(ElementColors.Of(t.Element), ElementColors.Of(t.Second), 0.5f) : ElementColors.Of(t.Element);
                var card = UiKit.Box("Card" + i, levelPanel, center, new Vector2((i - (offer.Count - 1) / 2f) * 400f, -30), new Vector2(360, 470), 0.96f);
                UiKit.Image(UiKit.Rect("Band", card, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(344, 8)), null, c);
                var disc = UiKit.Rect("Disc", card, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(120, 120));
                UiKit.Image(disc, UiKit.Circle, Color.Lerp(c, new Color(0.05f, 0.04f, 0.08f), 0.55f));
                UiKit.Image(UiKit.Stretch("Ring", disc), UiKit.Ring, Color.Lerp(c, UiKit.Cream, 0.3f));
                UiKit.Image(UiKit.Rect("Icon", disc, center, Vector2.zero, new Vector2(74, 74)), UiKit.Icon(TalentIcon(t)), UiKit.Cream);
                string where = t.Second != ElementType.None ? "REAÇÃO · " + t.Tag.ToUpperInvariant() : t.Element == ElementType.None ? "CORPO" : SkillName[(int)t.Element].ToUpperInvariant();
                UiKit.Text(UiKit.Rect("Where", card, new Vector2(0.5f, 1), new Vector2(0, -168), new Vector2(330, 26)), where, 19, Color.Lerp(c, UiKit.Cream, 0.3f));
                UiKit.Text(UiKit.Rect("Name", card, new Vector2(0.5f, 1), new Vector2(0, -198), new Vector2(330, 44)), t.Name, 32, UiKit.Cream);
                var desc = UiKit.Text(UiKit.Rect("Desc", card, new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(310, 96)), t.Description, 23, UiKit.Paper, TextAnchor.MiddleCenter, false);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                int rank = talents != null ? talents.Rank(t.Id) : 0;
                if (t.MaxRank > 1)
                    UiKit.Text(UiKit.Rect("Rank", card, new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(330, 26)), $"Grau {rank + 1} de {t.MaxRank}", 19, UiKit.Dim, TextAnchor.MiddleCenter, false);
                var b = UiKit.Button(card, $"Escolher  [{i + 1}]", new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 60), UiKit.Primary, () => pick(index));
                if (i == 0) UiKit.Focus(b.gameObject);
            }
            if (rerolls > 0 && reroll != null)
                UiKit.Button(levelPanel, $"Trocar as opções  [T]  ·  restam {rerolls}", center, new Vector2(0, -330), new Vector2(520, 56), UiKit.Secondary, reroll);
        }

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
            FpsCounter.Attach(canvas);
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

            // nivel e experiencia, logo acima da vida
            var xpBox = UiKit.Box("Level", root, new Vector2(0, 0), new Vector2(40, 124), new Vector2(470, 36));
            levelText = UiKit.Text(UiKit.Rect("Text", xpBox, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(110, 28)), "NÍVEL 1", 20, UiKit.Cream, TextAnchor.MiddleLeft);
            var xpBar = UiKit.Rect("Bar", xpBox, new Vector2(0, 0.5f), new Vector2(126, 0), new Vector2(326, 14));
            UiKit.Image(xpBar, UiKit.Panel, new Color(0.02f, 0.02f, 0.04f, 0.9f)).type = Image.Type.Sliced;
            xpFill = Bar(xpBar, new Color(0.82f, 0.68f, 0.36f));
            xpBox.gameObject.SetActive(false);
            xpRoot = xpBox;

            // barra de habilidades
            var bar = UiKit.Box("Abilities", root, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(820, 132));
            // quatro skills, uma por elemento, nas teclas 1 a 4; cadeado nas que ainda nao despertaram
            for (int i = 0; i < 4; i++)
                skillTiles[i] = MakeTile(bar, new Vector2(-306 + i * 98, 8), SkillIcon[i + 1], (i + 1).ToString());
            UiKit.Image(UiKit.Rect("Divider", bar, center, new Vector2(88, 0), new Vector2(2, 96)), null, new Color(1, 1, 1, 0.12f));

            dashTile = MakeTile(bar, new Vector2(176, 8), "dash", "ESPAÇO");
            quintTile = MakeTile(bar, new Vector2(290, 8), "quintessence", "R");
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
            KeyCap(promptBox, new Vector2(-220, 0), "E");
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
            TickBanner();
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

            if (progress != null)
            {
                SetBar(xpFill, progress.Normalized);
                levelText.text = "NÍVEL " + progress.Level;
            }

            var awakened = hero.AwakenedElements;
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                bool has = false;
                foreach (var a in awakened) if (a == e) has = true;
                var c = ElementColors.Of(e);
                var t = skillTiles[i];
                float cd = has ? hero.SkillCooldown01(e) : 0f;
                bool usable = has && cd <= 0f;
                t.Icon.sprite = UiKit.Icon(has ? SkillIcon[i + 1] : "lock");
                t.Icon.color = !has ? new Color(1, 1, 1, 0.28f) : usable ? Color.Lerp(c, UiKit.Cream, 0.3f) : new Color(1, 1, 1, 0.35f);
                t.Cooldown.fillAmount = cd;
                t.Ring.color = usable ? Color.Lerp(c, UiKit.Cream, 0.35f) : new Color(1, 1, 1, 0.15f);
                t.Key.color = has ? UiKit.Cream : UiKit.Dim;
                int rank = ranks != null && i + 1 < ranks.Length ? ranks[i + 1] : 1;
                t.Key.text = rank > 1 ? $"{i + 1} · {RunState.Roman(rank)}" : (i + 1).ToString();
                // acabou de usar: mostra o nome da skill por um instante
                if (has && cd > lastCd[i] + 0.5f) { skillLabel.text = SkillName[i + 1]; skillLabel.color = Color.Lerp(c, UiKit.Cream, 0.4f); skillLabelLeft = 1.4f; }
                lastCd[i] = cd;
            }
            if (skillLabelLeft > 0f && (skillLabelLeft -= Time.unscaledDeltaTime) <= 0f) skillLabel.text = "";

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

        RectTransform bannerRoot;
        Text bannerTitle, bannerSub;
        Image bannerBack;
        float bannerLeft;
        public string LastBanner { get; private set; } = "";

        /// Letreiro grande no alto da tela que some sozinho (novo trecho da run).
        public void ShowBanner(string title, string sub, Color color, float seconds = 3.2f)
        {
            if (bannerRoot == null)
            {
                bannerRoot = UiKit.Rect("Banner", canvas.transform, new Vector2(0.5f, 1), new Vector2(0, -210), new Vector2(1400, 160));
                bannerBack = UiKit.Image(UiKit.Rect("Back", bannerRoot, new Vector2(0.5f, 1), new Vector2(0, 14), new Vector2(1900, 170)), UiKit.VerticalVignette, new Color(0.02f, 0.02f, 0.04f, 0.7f));
                bannerSub = UiKit.Text(UiKit.Rect("Sub", bannerRoot, new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(1400, 36)), "", 26, UiKit.Dim);
                bannerTitle = UiKit.Text(UiKit.Rect("Title", bannerRoot, new Vector2(0.5f, 1), new Vector2(0, -38), new Vector2(1400, 90)), "", 72, UiKit.Cream);
                bannerTitle.font = GameFonts.Title;
                bannerRoot.SetSiblingIndex(0);
            }
            LastBanner = title;
            bannerTitle.text = title; bannerSub.text = sub;
            bannerTitle.color = color;
            bannerLeft = seconds;
            bannerRoot.gameObject.SetActive(true);
        }

        void TickBanner()
        {
            if (bannerRoot == null || !bannerRoot.gameObject.activeSelf) return;
            bannerLeft -= Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(bannerLeft / 0.8f);
            var c = bannerTitle.color; c.a = a; bannerTitle.color = c;
            var d = bannerSub.color; d.a = a; bannerSub.color = d;
            if (bannerBack != null) bannerBack.color = new Color(0.02f, 0.02f, 0.04f, 0.7f * a);
            if (bannerLeft <= 0f) bannerRoot.gameObject.SetActive(false);
        }


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
                UiKit.Image(UiKit.Rect("Icon", rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28)), UiKit.Icon(BoonEffects.Icon(list[i].stat)), Color.Lerp(c, UiKit.Cream, 0.35f));
            }
        }

        public void ShowPrompt(string text)
        {
            promptText.text = text.StartsWith("E — ") ? text.Substring(4) : text;
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
            bool hasBoons = boons != null && boons.Count > 0;
            // com bencaos, o painel sobe um pouco para a lista caber embaixo
            float top = hasBoons ? 110f : 30f;
            UiKit.Box("Box", pausePanel, center, new Vector2(0, top), new Vector2(780, 640), 0.94f);
            var title = UiKit.Text(UiKit.Rect("Title", pausePanel, center, new Vector2(0, top + 250), new Vector2(700, 80)), "PAUSADO", 64, UiKit.Cream);
            title.font = GameFonts.Title;
            UiKit.Focus(UiKit.Button(pausePanel, "Continuar", center, new Vector2(0, top + 150), new Vector2(640, 70), UiKit.Primary, resume).gameObject);
            UiOptions.Build(pausePanel, new Vector2(0, top - 20));
            UiKit.Button(pausePanel, "Menu principal", center, new Vector2(0, top - 235), new Vector2(640, 64), UiKit.Secondary, menu);
            if (hasBoons)
            {
                UiKit.Text(UiKit.Rect("BoonsTitle", pausePanel, center, new Vector2(0, top - 350), new Vector2(900, 36)), "BÊNÇÃOS", 26, UiKit.Dim);
                bool two = boons.Count > 4;
                int rows = two ? (boons.Count + 1) / 2 : boons.Count;
                for (int i = 0; i < boons.Count; i++)
                {
                    float x = two ? (i < rows ? -430f : 430f) : 0f;
                    int row = two && i >= rows ? i - rows : i;
                    UiKit.Text(UiKit.Rect("Boon" + i, pausePanel, center, new Vector2(x, top - 388 - row * 30), new Vector2(840, 30)),
                        boons[i].name + " — " + boons[i].description, 22, UiKit.Cream, TextAnchor.MiddleCenter, false);
                }
            }
            UiKit.Text(UiKit.Rect("Keys", pausePanel, new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(1700, 30)),
                "WASD mover · mouse mira · clique ataca · 1 2 3 4 skills · Espaço dash · R Quintessência · E interagir · I inventário · Tab mapa",
                20, UiKit.Dim, TextAnchor.MiddleCenter, false);
        }

        public bool ResultVisible => resultPanel.gameObject.activeSelf;

        RectTransform inventoryPanel;
        public bool InventoryVisible => inventoryPanel != null && inventoryPanel.gameObject.activeSelf;

        /// Inventario (tecla I): skills despertas, atributos somados na run, bencaos e Essencia.
        public void ShowInventory(bool show, RunState run = null)
        {
            if (inventoryPanel == null)
            {
                if (!show) return;
                inventoryPanel = Overlay("Inventory", new Color(0.03f, 0.02f, 0.06f, 0.86f));
            }
            inventoryPanel.gameObject.SetActive(show);
            if (!show) return;
            foreach (Transform child in inventoryPanel) Destroy(child.gameObject);
            var center = new Vector2(0.5f, 0.5f);
            var box = UiKit.Box("Box", inventoryPanel, center, new Vector2(0, 10), new Vector2(1320, 820), 0.95f);
            var title = UiKit.Text(UiKit.Rect("Title", box, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(900, 70)), "INVENTÁRIO", 56, UiKit.Paper);
            title.font = GameFonts.Title;

            if (hero != null && hero.AwakenedElements.Count > 0)
            {
                var first = hero.AwakenedElements[0];
                UiKit.Text(UiKit.Rect("Hero", box, new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(1200, 30)),
                    $"{HeroPassives.HeroName(first)}  ·  {HeroPassives.Style(first)}  ·  {HeroPassives.Title(first)}: {HeroPassives.Describe(first)}", 22, Color.Lerp(ElementColors.Of(first), UiKit.Cream, 0.5f), TextAnchor.MiddleCenter, false);
            }

            // coluna da esquerda: as quatro skills
            UiKit.Text(UiKit.Rect("H1", box, new Vector2(0, 1), new Vector2(60, -130), new Vector2(560, 34)), "SKILLS ELEMENTAIS", 24, UiKit.Dim, TextAnchor.MiddleLeft);
            string[] role =
            {
                "", "Cone de fogo. Queima. O ataque.", "Onda que empurra e Encharca. Cura você e limpa status.",
                "Ergue um pilar que atordoa e quebra armadura. Bloqueia o próximo golpe.", "Rajada em volta que empurra tudo. Recarrega o dash.",
            };
            for (int i = 0; i < 4; i++)
            {
                var e = Elements.All[i];
                bool has = false;
                if (hero != null) foreach (var a in hero.AwakenedElements) if (a == e) has = true;
                var c = ElementColors.Of(e);
                var row = UiKit.Box("Skill" + i, box, new Vector2(0, 1), new Vector2(60, -176 - i * 104), new Vector2(580, 92), 0.5f);
                var disc = UiKit.Rect("Disc", row, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(64, 64));
                UiKit.Image(disc, UiKit.Circle, has ? Color.Lerp(c, new Color(0.05f, 0.04f, 0.08f), 0.4f) : Slate);
                UiKit.Image(UiKit.Rect("Icon", disc, center, Vector2.zero, new Vector2(42, 42)), UiKit.Icon(has ? SkillIcon[i + 1] : "lock"), has ? UiKit.Cream : new Color(1, 1, 1, 0.3f));
                UiKit.Text(UiKit.Rect("Name", row, new Vector2(0, 0.5f), new Vector2(94, 20), new Vector2(470, 34)),
                    $"{i + 1}  ·  {SkillName[i + 1]}" + (has && run != null && run.SkillRank[i + 1] > 1 ? " " + RunState.Roman(run.SkillRank[i + 1]) : ""), 28, has ? UiKit.Cream : UiKit.Dim, TextAnchor.MiddleLeft);
                var note = UiKit.Text(UiKit.Rect("Role", row, new Vector2(0, 0.5f), new Vector2(94, -18), new Vector2(470, 44)),
                    has ? role[i + 1] : "Desperte este elemento num Santuário.", 20, has ? UiKit.Paper : UiKit.Dim, TextAnchor.MiddleLeft, false);
                note.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            // as seis reacoes: claras as que o heroi ja pode detonar (tem os dois elementos)
            UiKit.Text(UiKit.Rect("ReactH", box, new Vector2(0, 1), new Vector2(60, -590), new Vector2(580, 28)),
                "REAÇÕES  ·  uma skill marca, outra de outro elemento detona", 19, UiKit.Dim, TextAnchor.MiddleLeft, false);
            (string name, ElementType a, ElementType b)[] pairs =
            {
                ("Vapor", ElementType.Fire, ElementType.Water), ("Magma", ElementType.Fire, ElementType.Earth), ("Tempestade", ElementType.Fire, ElementType.Air),
                ("Lama", ElementType.Water, ElementType.Earth), ("Nevasca", ElementType.Water, ElementType.Air), ("Estilhaços", ElementType.Earth, ElementType.Air),
            };
            string[] short1 = { "", "Fogo", "Água", "Terra", "Ar" };
            for (int i = 0; i < pairs.Length; i++)
            {
                var (rname, ra, rb) = pairs[i];
                bool known = false, knownB = false;
                if (hero != null) foreach (var x in hero.AwakenedElements) { if (x == ra) known = true; if (x == rb) knownB = true; }
                bool can = known && knownB;
                var pos = new Vector2(60 + (i % 3) * 196, -624 - (i / 3) * 30);
                UiKit.Image(UiKit.Rect("Dot" + i, box, new Vector2(0, 1), pos + new Vector2(6, -2), new Vector2(10, 10)), UiKit.Circle,
                    can ? Color.Lerp(ElementColors.Of(ra), ElementColors.Of(rb), 0.5f) : new Color(1, 1, 1, 0.15f));
                UiKit.Text(UiKit.Rect("React" + i, box, new Vector2(0, 1), pos + new Vector2(18, 0), new Vector2(180, 26)),
                    $"{rname} · {short1[(int)ra]}+{short1[(int)rb]}", 17, can ? UiKit.Cream : UiKit.Dim, TextAnchor.MiddleLeft, false);
            }

            // coluna da direita: atributos e bencaos
            UiKit.Text(UiKit.Rect("H2", box, new Vector2(0, 1), new Vector2(700, -130), new Vector2(560, 34)), "ATRIBUTOS DA RUN", 24, UiKit.Dim, TextAnchor.MiddleLeft);
            var lines = new List<string>();
            if (hero != null)
            {
                var m = hero.Modifiers;
                lines.Add($"Vida: {Mathf.CeilToInt(hero.Health.Current)} / {Mathf.RoundToInt(hero.Health.Max)}");
                if (run != null) lines.Add($"Essência: {run.Essence}");
                lines.Add($"Dano: {m.DamageAll * 100f:0}%");
                lines.Add($"Reações: {m.ReactionPower * 100f:0}%");
                lines.Add($"Crítico: +{m.CritChanceAdd * 100f:0}%");
                lines.Add($"Velocidade: {m.MoveSpeedMult * 100f:0}%");
                lines.Add($"Recarga das skills: {m.SkillCooldownMult * 100f:0}%");
                lines.Add($"Recarga do dash: {m.DashCooldownMult * 100f:0}%");
            }
            for (int i = 0; i < lines.Count; i++)
                UiKit.Text(UiKit.Rect("Stat" + i, box, new Vector2(0, 1), new Vector2(700 + (i % 2) * 290, -172 - (i / 2) * 34), new Vector2(280, 30)),
                    lines[i], 22, UiKit.Cream, TextAnchor.MiddleLeft, false);
            UiKit.Text(UiKit.Rect("H3", box, new Vector2(0, 1), new Vector2(700, -330), new Vector2(560, 34)),
                boons != null && boons.Count > 0 ? "BÊNÇÃOS" : "BÊNÇÃOS — nenhuma ainda", 24, UiKit.Dim, TextAnchor.MiddleLeft);
            if (boons != null)
                for (int i = 0; i < boons.Count && i < 10; i++)
                {
                    var c = boons[i].patron == ElementType.None ? new Color(0.85f, 0.72f, 0.4f) : ElementColors.Of(boons[i].patron);
                    float y = -374 - i * 38;
                    var medal = UiKit.Rect("Medal" + i, box, new Vector2(0, 1), new Vector2(700, y), new Vector2(32, 32));
                    UiKit.Image(medal, UiKit.Circle, new Color(0.07f, 0.06f, 0.1f));
                    UiKit.Image(UiKit.Rect("Icon", medal, center, Vector2.zero, new Vector2(22, 22)), UiKit.Icon(BoonEffects.Icon(boons[i].stat)), Color.Lerp(c, UiKit.Cream, 0.35f));
                    UiKit.Text(UiKit.Rect("Boon" + i, box, new Vector2(0, 1), new Vector2(744, y), new Vector2(540, 32)),
                        boons[i].name + " — " + boons[i].description, 21, UiKit.Cream, TextAnchor.MiddleLeft, false);
                }
            if (talents != null && talents.Count > 0)
            {
                var seen = new List<string>();
                var parts = new List<string>();
                foreach (var t in talents.Taken)
                {
                    if (seen.Contains(t.Id)) continue;
                    seen.Add(t.Id);
                    parts.Add(t.MaxRank > 1 ? $"{t.Name} {talents.Rank(t.Id)}/{t.MaxRank}" : t.Name);
                }
                var tl = UiKit.Text(UiKit.Rect("Talents", box, new Vector2(0, 1), new Vector2(60, -702), new Vector2(1200, 60)),
                    $"TALENTOS (nível {progress?.Level ?? 1}):  " + string.Join("  ·  ", parts), 21, UiKit.Cream, TextAnchor.UpperLeft, false);
                tl.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            UiKit.Text(UiKit.Rect("Close", box, new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(900, 30)), "I ou Esc fecha", 20, UiKit.Dim, TextAnchor.MiddleCenter, false);
        }

        RectTransform tutorialBox, tutorialFill;
        Text tutorialTitle, tutorialText;

        /// Caixa do treino no topo: passo atual, o que fazer e quanto falta.
        public void ShowTutorial(int number, int total, string title, string text, float progress)
        {
            if (tutorialBox == null)
            {
                tutorialBox = UiKit.Box("Tutorial", canvas.transform, new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(980, 150), 0.9f);
                tutorialBox.SetAsFirstSibling(); // fica por baixo da pausa e do resultado
                tutorialTitle = UiKit.Text(UiKit.Rect("Title", tutorialBox, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(900, 44)), "", 34, new Color(0.88f, 0.74f, 0.42f));
                tutorialText = UiKit.Text(UiKit.Rect("Text", tutorialBox, new Vector2(0.5f, 1), new Vector2(0, -62), new Vector2(900, 64)), "", 25, UiKit.Cream, TextAnchor.MiddleCenter, false);
                tutorialText.horizontalOverflow = HorizontalWrapMode.Wrap;
                var track = UiKit.Rect("Track", tutorialBox, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(300, 6));
                UiKit.Image(track, null, new Color(1f, 1f, 1f, 0.12f));
                tutorialFill = UiKit.Rect("Fill", track, new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 6));
                UiKit.Image(tutorialFill, null, new Color(0.88f, 0.74f, 0.42f));
            }
            tutorialBox.gameObject.SetActive(true);
            tutorialTitle.text = $"{number}/{total}  ·  {title}";
            tutorialText.text = text;
            tutorialFill.sizeDelta = new Vector2(300f * Mathf.Clamp01(progress), 6);
        }

        public void ShowTutorialEnd(Action start, Action menu)
        {
            if (tutorialBox != null) tutorialBox.gameObject.SetActive(false);
            resultPanel.gameObject.SetActive(true);
            foreach (Transform child in resultPanel) Destroy(child.gameObject);
            var center = new Vector2(0.5f, 0.5f);
            var title = UiKit.Text(UiKit.Rect("Title", resultPanel, center, new Vector2(0, 200), new Vector2(1500, 120)), "TREINO CONCLUÍDO", 92, new Color(0.9f, 0.76f, 0.42f));
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Stats", resultPanel, center, new Vector2(0, 70), new Vector2(1400, 120)),
                "Marque com uma skill e detone com a de outro elemento.\nNa run você começa com uma skill só e desperta as outras nos Santuários.", 30, UiKit.Cream, TextAnchor.MiddleCenter, false);
            UiKit.Focus(UiKit.Button(resultPanel, "Começar a run", center, new Vector2(0, -60), new Vector2(380, 70), UiKit.Primary, start).gameObject);
            UiKit.Button(resultPanel, "Menu principal", center, new Vector2(0, -150), new Vector2(380, 70), UiKit.Secondary, menu);
        }

        public void ShowResult(bool victory, string stats, Action again, Action menu)
        {
            resultPanel.gameObject.SetActive(true);
            foreach (Transform child in resultPanel) Destroy(child.gameObject);
            var center = new Vector2(0.5f, 0.5f);
            var title = UiKit.Text(UiKit.Rect("Title", resultPanel, center, new Vector2(0, 200), new Vector2(1400, 120)),
                victory ? "VITÓRIA" : "VOCÊ CAIU", 104, victory ? new Color(0.9f, 0.76f, 0.42f) : new Color(0.78f, 0.3f, 0.28f));
            title.font = GameFonts.Title;
            UiKit.Text(UiKit.Rect("Stats", resultPanel, center, new Vector2(0, 62), new Vector2(1500, 180)), stats, 28, UiKit.Cream, TextAnchor.MiddleCenter, false);
            UiKit.Focus(UiKit.Button(resultPanel, "Jogar de novo", center, new Vector2(0, -95), new Vector2(380, 70), UiKit.Primary, again).gameObject);
            UiKit.Button(resultPanel, "Menu principal", center, new Vector2(0, -185), new Vector2(380, 70), UiKit.Secondary, menu);
        }
    }
}
