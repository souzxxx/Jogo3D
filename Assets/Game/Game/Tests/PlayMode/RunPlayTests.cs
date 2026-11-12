#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Enemies;
using Quintessence.Run;
using Quintessence.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Game.PlayTests
{
    /// Joga o jogo de ponta a ponta com um heroi roteirizado: titulo -> run curta -> vitoria.
    public class RunPlayTests
    {
        const string FrameDir = "Logs/frames/run";
        PlayerController player;
        ScriptedInput input;
        Health hp;
        Camera cam;
        RenderTexture rt;
        Texture2D tex;
        int frame, saved;
        Vector3 dodgeDir;
        float dodgeLeft;

        const string TestSaveKey = "quintessence.meta.test";

        // todo teste daqui usa um save proprio e zerado: nao le nem grava o progresso de quem joga nesta maquina
        [SetUp]
        public void UseTestSave()
        {
            PlayerPrefs.DeleteKey(TestSaveKey);
            MetaStore.UseKey(TestSaveKey);
            GameFlow.AutoPickTalents = true;
            RunSaveStore.UseKey(TestSaveKey + ".run");
            RunSaveStore.Clear();
            Tips.UseKey(TestSaveKey + ".tips");
            Tips.ResetAll();
            GameFlow.Resume = null;
        }

        [TearDown]
        public void RestoreSave()
        {
            PlayerPrefs.DeleteKey(TestSaveKey);
            MetaStore.UseKey(MetaStore.DefaultKey);
            GameFlow.AutoPickTalents = false;
            RunSaveStore.Clear();
            RunSaveStore.UseKey(RunSaveStore.DefaultKey);
            Tips.ResetAll();
            Tips.UseKey(Tips.DefaultKey);
            GameFlow.Resume = null;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Title_Starts_A_Run_With_The_Picked_Element()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            GameEvents.Clear(); // assinantes de testes anteriores apontam para objetos ja destruidos
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Title.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<TitleMenu>();
            Assert.IsNotNull(menu, "cena de titulo sem menu");
            Assert.IsNotNull(menu.OnPick, "o menu deveria estar ligado ao inicio da run");
            menu.UseCamera(Camera.main);
            yield return null;
            Shot(Camera.main, "docs/img/titulo.png");

            // passar o mouse numa carta acende o pedestal daquele elemento no altar
            var title = Object.FindFirstObjectByType<TitleController>();
            Assert.AreEqual(-1, menu.HoveredElement);
            float before = title.LightOf(0);
            GameObject.Find("Card_Rax").GetComponent<UiHover>().OnPointerEnter(null);
            Assert.AreEqual(0, menu.HoveredElement);
            for (int i = 0; i < 40; i++) yield return null;
            Assert.Greater(title.LightOf(0), before + 2f, "o pedestal de Fogo deveria acender");
            Assert.AreEqual(0, title.Cheering, "o Rax comemora quando a carta dele esta sob o mouse");
            Assert.Less(title.LightOf(1), before + 1f, "os outros ficam como estavam");
            Shot(Camera.main, "docs/img/titulo_fogo.png");
            GameObject.Find("Card_Rax").GetComponent<UiHover>().OnPointerExit(null);

            menu.ShowOptions();
            yield return null; yield return null;
            Assert.IsTrue(menu.SheetOpen);
            Shot(Camera.main, "docs/img/opcoes.png");
            menu.ShowCredits();
            yield return null; yield return null;
            Shot(Camera.main, "docs/img/creditos.png");
            menu.ShowGrimoire();
            yield return null; yield return null;
            Assert.IsTrue(menu.SheetOpen);
            Shot(Camera.main, "docs/img/grimorio.png");

            menu.OnPick(ElementType.Water);
            for (int i = 0; i < 30 && SceneManager.GetActiveScene().name != GameFlow.RunScene; i++) yield return null;
            Assert.AreEqual(GameFlow.RunScene, SceneManager.GetActiveScene().name, "escolher o elemento deveria carregar a run");
            for (int i = 0; i < 10; i++) yield return null;
            var p = Object.FindFirstObjectByType<PlayerController>();
            Assert.AreEqual(ElementType.Water, p.Element);
            Assert.AreEqual(1, p.Loadout.Awakened.Count, "a run comeca com um elemento so");
            var director = Object.FindFirstObjectByType<RunDirector>();
            Assert.AreEqual(8, director.Run.Map.LayerCount);
            Assert.AreEqual(ElementType.Water, p.GetComponent<HeroSkin>().Shown, "escolher a carta da Liora traz a Liora");
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Refuge_Upgrades_Are_Saved_And_Change_The_Next_Run()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.Layers = 5; GameFlow.Shrines = 1;
            GameEvents.Clear();
            // save proprio do teste: o do jogador nao e tocado
            const string key = TestSaveKey;
            PlayerPrefs.DeleteKey(key);
            MetaStore.UseKey(key);
            MetaStore.Current.EndRun(8, true); // 39 Fragmentos
            MetaStore.Save();

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Title.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<TitleMenu>();
            menu.UseCamera(Camera.main);
            menu.ShowRefuge(true);
            Assert.IsTrue(menu.RefugeOpen);
            Assert.IsTrue(menu.Buy(MetaUpgrade.Vigor), "39 Fragmentos pagam o primeiro Vigor");
            Assert.IsTrue(menu.Buy(MetaUpgrade.Purse));
            Assert.IsTrue(menu.Buy(MetaUpgrade.Power));
            Assert.IsFalse(menu.Buy(MetaUpgrade.Recovery), "sobraram 9: nao paga o Fôlego");
            yield return null;
            yield return null;
            Shot(Camera.main, "docs/img/refugio.png");
            menu.ShowRefuge(false);
            // com uma vitoria no save, o titulo oferece a Ascensao
            yield return null;
            Assert.IsNotNull(GameObject.Find("Ascension"), "depois da primeira vitoria o titulo mostra a Ascensao");
            menu.Ascension = 1;
            Shot(Camera.main, "docs/img/titulo_ascensao.png");

            // como se o jogo tivesse sido fechado e aberto: le de novo do disco
            MetaStore.UseKey(key);
            Assert.AreEqual(1, MetaStore.Current.Level(MetaUpgrade.Vigor), "a compra ficou salva");
            Assert.AreEqual(9, MetaStore.Current.Shards);

            menu.OnPick(ElementType.Fire);
            for (int i = 0; i < 30 && SceneManager.GetActiveScene().name != GameFlow.RunScene; i++) yield return null;
            for (int i = 0; i < 10; i++) yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var p = Object.FindFirstObjectByType<PlayerController>();
            var health = p.GetComponent<Health>();
            Assert.AreEqual(108f, health.Max, 0.01f, "Vigor 1: +8 de vida maxima");
            Assert.AreEqual(108f, health.Current, 0.01f);
            Assert.AreEqual(8, director.Run.Essence, "Bolsa 1: comeca com 8 de Essencia");
            Assert.AreEqual(1.04f, p.Modifiers.DamageAll, 0.001f);
            Assert.AreEqual(1, GameFlow.Ascension, "a Ascensao escolhida no titulo vale para a run");
            GameFlow.Ascension = 0;

            // save estragado nao derruba o jogo: volta zerado
            PlayerPrefs.SetString(key, "v1;###;;quebrado");
            MetaStore.UseKey(key);
            Assert.AreEqual(0, MetaStore.Current.Shards);
            PlayerPrefs.DeleteKey(key);
            MetaStore.UseKey(key);
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Master_Room_Offers_Boons_And_Taking_One_Changes_The_Hero()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 5; GameFlow.Shrines = 1;
            // procura uma semente em que a segunda sala pode ser um Mestre
            int seed = 0;
            for (int s = 1; s < 500 && seed == 0; s++)
            {
                var m = RunMapGenerator.Generate(s, 5, 1);
                foreach (int id in m.Start.Next) if (m.Get(id).Type == RoomType.Master) seed = s;
            }
            Assert.AreNotEqual(0, seed, "nenhuma semente com Mestre na segunda sala");
            GameFlow.Seed = seed;

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            player = Object.FindFirstObjectByType<PlayerController>();
            hp = player.GetComponent<Health>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            input = player.gameObject.AddComponent<ScriptedInput>();
            player.Input = input;
            cam = Camera.main;
            hud.UseCamera(cam);
            GameEvents.TelegraphStarted += ConsiderDodge;

            hp.Pool.ExtraDamageTakenMultiplier = 0.5f; // confere as bencaos; o roteiro troca golpes parado e nao serve de regua de dificuldade
            bool shot = false;
            float limit = 120f, sinceInteract = 0f;
            while (director.Run.Boons.Count == 0 && !director.Finished && limit > 0f)
            {
                float dt = Time.deltaTime;
                limit -= dt; sinceInteract += dt;
                var target = Nearest();
                input.AttackHeld = false;
                if (director.Busy) input.Move = Vector2.zero;
                else if (dodgeLeft > 0f) { dodgeLeft -= dt; input.Move = new Vector2(dodgeDir.x, dodgeDir.z); }
                else if (target != null)
                {
                    var to = target.transform.position - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.AimPoint = target.transform.position;
                    input.Move = d > 2.6f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    input.AttackHeld = d < 3.6f;
                    if (d < 4.5f && player.AnySkillReady) input.PressSkill();
                }
                else if (director.Interactables.Count > 0)
                {
                    // na primeira sala escolhe a porta do Mestre; na sala do Mestre pega a primeira bencao
                    var pick = director.Interactables[0];
                    foreach (var it in director.Interactables) if (it.Prompt.Contains("Mestre")) pick = it;
                    if (director.Run.Current.Type == RoomType.Master && !shot)
                    {
                        shot = true;
                        input.Move = Vector2.zero;
                        for (int i = 0; i < 20; i++) yield return null;
                        Shot(cam, "docs/img/bencaos.png");
                    }
                    var to = pick.Position - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.Move = d > 1.4f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    if (d < 2.2f && sinceInteract > 0.4f) { sinceInteract = 0f; input.PressInteract(); }
                }
                else input.Move = Vector2.zero;
                Unstick(dt);
                yield return null;
            }
            input.Move = Vector2.zero;
            GameEvents.TelegraphStarted -= ConsiderDodge;

            Assert.IsTrue(shot, "nao chegou a sala do Mestre");
            Assert.AreEqual(1, director.Run.Boons.Count, "deveria ter aceitado uma bencao");
            var check = new HeroModifiers();
            BoonEffects.Apply(check, director.Run.Boons[0]);
            var mods = player.Modifiers;
            Assert.IsTrue(mods.DamageAll == check.DamageAll && mods.ReactionPower == check.ReactionPower && mods.MaxHealthAdd == check.MaxHealthAdd
                && mods.CritChanceAdd == check.CritChanceAdd && mods.Damage(ElementType.Fire) == check.Damage(ElementType.Fire),
                "os modificadores do heroi deveriam refletir a bencao aceita");
            Assert.AreEqual(100f + mods.MaxHealthAdd, hp.Max, 0.01f);
            yield return null;
            Assert.Greater(director.Interactables.Count, 0, "depois de escolher, as portas abrem");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Short_Run_From_First_Room_To_Victory()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            if (Directory.Exists(FrameDir)) Directory.Delete(FrameDir, true);
            Directory.CreateDirectory(FrameDir);
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;

            GameEvents.Clear(); // assinantes de testes anteriores apontam para objetos ja destruidos
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(director); Assert.IsNotNull(hud); Assert.IsNotNull(player);
            hp = player.GetComponent<Health>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            input = player.gameObject.AddComponent<ScriptedInput>();
            player.Input = input;
            input.AimPoint = player.transform.position + Vector3.forward;
            cam = Camera.main;
            hud.UseCamera(cam);
            rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            GameEvents.TelegraphStarted += ConsiderDodge;

            Assert.AreEqual(1, player.Loadout.Awakened.Count, "a run comeca com um elemento so");
            Assert.AreEqual(RoomType.Combat, director.Run.Current.Type);
            // Este teste confere o FLUXO da run, nao o balanceamento: a run curta chega ao chefe provisorio com so 2 elementos
            // e o roteiro joga mal, entao ele leva 40% do dano. Dificuldade se mede jogando de verdade.
            hp.Pool.ExtraDamageTakenMultiplier = 0.3f;
            for (float t = 1.2f; t > 0f; t -= Time.deltaTime) yield return Step();

            // pausa: o tempo para e volta
            director.SetPaused(true);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale); Assert.IsTrue(hud.PauseVisible);
            yield return null;
            Shot(cam, "docs/img/pausa.png");
            director.SetPaused(false);
            // inventario: para o jogo e mostra skills, atributos e bencaos
            director.SetInventory(true);
            yield return null; yield return null;
            Assert.IsTrue(hud.InventoryVisible); Assert.AreEqual(0f, Time.timeScale);
            Shot(cam, "docs/img/inventario.png");
            director.SetInventory(false);
            Assert.IsFalse(hud.InventoryVisible); Assert.AreEqual(1f, Time.timeScale);
            director.SetPaused(true);
            director.SetPaused(false);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale); Assert.IsFalse(hud.PauseVisible);

            // mapa
            hud.ShowMap(director.Run, true);
            yield return null;
            Shot(cam, "docs/img/mapa.png");
            hud.ShowMap(director.Run, false);

            bool sawDoors = false, sawShrine = false, shrineShot = false;
            int maxDepth = 0;
            float limit = 420f, sinceSwitch = 0f, sinceInteract = 0f;
            while (!director.Finished && limit > 0f)
            {
                float dt = Time.deltaTime;
                limit -= dt; sinceSwitch += dt; sinceInteract += dt;
                maxDepth = Mathf.Max(maxDepth, director.Run.Depth);
                if (director.Run.Current.Type == RoomType.Shrine) sawShrine = true;
                var target = Nearest();
                input.AttackHeld = false;
                if (director.Busy) input.Move = Vector2.zero;
                else if (dodgeLeft > 0f) { dodgeLeft -= dt; input.Move = new Vector2(dodgeDir.x, dodgeDir.z); }
                else if (target != null)
                {
                    var to = target.transform.position - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.AimPoint = target.transform.position;
                    float reach = target.GetComponent<BossBrain>() != null ? 1f : 0f; // o chefe e largo
                    input.Move = d > 2.6f + reach ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    input.AttackHeld = d < 3.6f + reach;
                    if (d < 4.5f + reach && player.AnySkillReady) input.PressSkill();
                }
                else if (director.Interactables.Count > 0)
                {
                    if (!sawDoors)
                    {
                        input.Move = Vector2.zero;
                        for (int i = 0; i < 25; i++) yield return Step();
                        Shot(cam, "docs/img/portas.png");
                    }
                    if (director.Run.Current.Type == RoomType.Shrine && !shrineShot)
                    {
                        shrineShot = true;
                        input.Move = Vector2.zero;
                        for (int i = 0; i < 25; i++) yield return Step();
                        Shot(cam, "docs/img/santuario.png");
                    }
                    sawDoors = true;
                    var goal = director.Interactables[0].Position;
                    var to = goal - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.AimPoint = goal;
                    input.Move = d > 1.4f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    if (d < 2.2f && sinceInteract > 0.4f) { sinceInteract = 0f; input.PressInteract(); }
                }
                else input.Move = Vector2.zero;
                Unstick(dt);
                yield return Step();
            }
            input.Move = Vector2.zero; input.AttackHeld = false;
            GameEvents.TelegraphStarted -= ConsiderDodge;

            Assert.IsTrue(director.Finished, "a run nao terminou no tempo limite");
            Assert.IsTrue(director.Victory, $"o heroi caiu na sala {director.Run.Depth + 1} ({director.Run.Current.Type})");
            Assert.IsTrue(sawDoors, "as portas deveriam abrir depois de limpar a sala");
            Assert.IsTrue(sawShrine, "o caminho passa por um santuario");
            Assert.AreEqual(4, maxDepth, "chegou a ultima camada");
            Assert.AreEqual(2, player.Loadout.Awakened.Count, "o santuario desperta um elemento");
            Assert.AreEqual(5, director.Run.RoomsCleared);
            Assert.Greater(director.Run.Essence, 0);
            for (float t = 3f; t > 0f; t -= Time.unscaledDeltaTime) yield return Step();
            Assert.IsTrue(hud.ResultVisible, "a tela de resultado deveria aparecer");
            Assert.AreEqual(1, director.NewAscension, "a primeira vitoria libera a Ascensao 1");
            Shot(cam, "docs/img/vitoria.png");

            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
        }

        Vector3 lastPos;
        float stuck, sidestep;
        Vector2 sideDir;

        /// O roteiro anda em linha reta ate o alvo; com cobertura na sala ele pode encostar numa peca.
        /// Se ficar parado tentando andar, da um passo de lado e tenta de novo.
        void Unstick(float dt)
        {
            var pos = player.transform.position;
            bool trying = input.Move.sqrMagnitude > 0.2f;
            if (sidestep > 0f) { sidestep -= dt; input.Move = sideDir; }
            else if (trying && (pos - lastPos).magnitude < 0.03f)
            {
                stuck += dt;
                if (stuck > 0.35f)
                {
                    stuck = 0f; sidestep = 0.55f;
                    sideDir = (Random.value < 0.5f ? 1f : -1f) * new Vector2(-input.Move.y, input.Move.x);
                }
            }
            else stuck = 0f;
            lastPos = pos;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Shop_Sells_For_Essence_And_Refuses_When_Broke()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 5; GameFlow.Shrines = 1;
            // procura uma semente com o caminho Combate -> Combate -> Loja
            int seed = 0, first = -1, shop = -1;
            for (int sd = 1; sd < 3000 && seed == 0; sd++)
            {
                var m = RunMapGenerator.Generate(sd, 5, 1);
                foreach (int a in m.Start.Next)
                {
                    if (m.Get(a).Type != RoomType.Combat) continue;
                    foreach (int b in m.Get(a).Next)
                        if (m.Get(b).Type == RoomType.Shop && seed == 0) { seed = sd; first = a; shop = b; }
                }
            }
            Assert.AreNotEqual(0, seed, "nenhuma semente com Combate -> Combate -> Loja");
            GameFlow.Seed = seed;

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            player = Object.FindFirstObjectByType<PlayerController>();
            hp = player.GetComponent<Health>();
            hp.Pool.ExtraDamageTakenMultiplier = 0.1f; // confere a loja, nao a dificuldade: este roteiro troca golpes parado
            player.GetComponent<PlayerInputReader>().enabled = false;
            input = player.gameObject.AddComponent<ScriptedInput>();
            player.Input = input;
            cam = Camera.main;
            hud.UseCamera(cam);
            GameEvents.TelegraphStarted += ConsiderDodge;

            float limit = 200f, sinceInteract = 0f;
            while (director.Run.Current.Type != RoomType.Shop && !director.Finished && limit > 0f)
            {
                float dt = Time.deltaTime;
                limit -= dt; sinceInteract += dt;
                var target = Nearest();
                input.AttackHeld = false;
                if (director.Busy) input.Move = Vector2.zero;
                else if (dodgeLeft > 0f) { dodgeLeft -= dt; input.Move = new Vector2(dodgeDir.x, dodgeDir.z); }
                else if (target != null)
                {
                    var to = target.transform.position - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.AimPoint = target.transform.position;
                    input.Move = d > 2.6f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    input.AttackHeld = d < 3.6f;
                    if (d < 4.5f && player.AnySkillReady) input.PressSkill();
                }
                else if (director.Interactables.Count > 0)
                {
                    // as portas saem na mesma ordem das escolhas do mapa: pega a que leva ao caminho da loja
                    int want = director.Run.Depth == 0 ? first : shop;
                    int idx = director.Run.Choices.FindIndex(n => n.Id == want);
                    var door = director.Interactables[Mathf.Clamp(idx, 0, director.Interactables.Count - 1)];
                    var to = door.Position - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.Move = d > 1.4f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    if (d < 2.2f && sinceInteract > 0.4f) { sinceInteract = 0f; input.PressInteract(); }
                }
                else input.Move = Vector2.zero;
                Unstick(dt);
                yield return null;
            }
            input.Move = Vector2.zero; input.AttackHeld = false;
            GameEvents.TelegraphStarted -= ConsiderDodge;
            Assert.AreEqual(RoomType.Shop, director.Run.Current.Type, $"nao chegou a loja (vida {hp.Current:0}, fim {director.Finished}, sala {director.Run.Depth + 1}, tempo restante {limit:0})");
            for (int k = 0; k < 3000 && director.Busy; k++) yield return null;
            for (int k = 0; k < 20; k++) yield return null;
            Shot(cam, "docs/img/loja.png");

            var wares = director.Interactables.Where(i => i.Prompt.StartsWith("comprar")).ToList();
            Assert.GreaterOrEqual(wares.Count, 2, "a loja vende pelo menos pocao e elixir");
            Assert.Greater(director.Interactables.Count, wares.Count, "as portas ja estao abertas: da para sair sem comprar");

            // sem essencia: recusa e nada muda
            director.Run.TrySpend(director.Run.Essence);
            int before = director.Interactables.Count;
            float maxBefore = hp.Max;
            wares[0].Use();
            yield return null;
            Assert.AreEqual(before, director.Interactables.Count, "sem essencia o item continua a venda");
            Assert.AreEqual(0, director.Run.Essence);

            // com essencia: compra tudo, a essencia cai pelo preco e os efeitos entram
            director.Run.AddEssence(500);
            // parte de vida cheia e tira um pedaco, para a cura ter o que curar sem depender de como a luta foi
            hp.Heal(hp.Max);
            hp.TakeDamage(new DamageInfo(100, ElementType.None, Team.Enemy));
            float hurt = hp.Current;
            Assert.Greater(hurt, 0f); Assert.Less(hurt, hp.Max);
            int boonsBefore = director.Run.Boons.Count;
            foreach (var w in wares) w.Use();
            yield return null;
            Assert.Less(director.Run.Essence, 500, "comprar gasta essencia");
            Assert.Greater(director.Run.Essence, 0);
            Assert.Greater(hp.Current, hurt, "a pocao cura");
            Assert.AreEqual(maxBefore + 15f + (player.Modifiers.MaxHealthAdd - 15f), hp.Max, 0.01f, "o elixir aumenta a vida maxima");
            Assert.AreEqual(boonsBefore + wares.Count - 2, director.Run.Boons.Count, "cada bencao comprada entra na run");
            Assert.AreEqual(0, director.Interactables.Count(i => i.Prompt.StartsWith("comprar")), "item comprado sai da mesa");
            Assert.Greater(director.Interactables.Count, 0, "as portas continuam la");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Shop_Has_A_Stall_And_A_Merchant()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            director.ShowShop();
            Object.FindFirstObjectByType<PlayerController>().Teleport(new Vector3(-0.5f, 0, -3.6f), Vector3.forward);
            for (int k = 0; k < 60; k++) yield return null;
            Assert.IsNotNull(GameObject.Find("Merchant"), "a loja tem um mercador");
            var seller = director.ShopMerchant;
            Assert.IsNotNull(seller, "o mercador reage ao heroi");
            Assert.IsTrue(seller.Greeted, "com o heroi perto, ele cumprimenta");
            var wares = director.Interactables.Where(i => i.Prompt.StartsWith("comprar")).ToList();
            director.Run.TrySpend(director.Run.Essence);
            wares[0].Use();
            Assert.AreEqual("Falta Essência...", seller.LastLine);
            director.Run.AddEssence(500);
            wares[0].Use();
            Assert.AreEqual("Boa escolha!", seller.LastLine);
            // as demais continuam a venda para a foto

            Assert.GreaterOrEqual(director.Interactables.Count(i => i.Prompt.StartsWith("comprar")), 1);
            director.Run.TrySpend(director.Run.Essence - 30); // fica com 30: da para ver preco pago e preco fora do alcance
            director.RefreshPrices();
            for (int k = 0; k < 20; k++) yield return null;
            Shot(cam, "docs/img/loja_banca.png");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Level_Up_Offers_Talents_And_The_Pick_Changes_The_Hero()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.AutoPickTalents = false;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            var hero = Object.FindFirstObjectByType<PlayerController>();
            Assert.AreEqual(1, director.Run.Progress.Level);
            Assert.IsFalse(director.LevelUpOpen);

            // experiencia de sobra para um nivel: a escolha abre sozinha e o jogo para
            director.Run.Progress.AddXp(director.Run.Progress.XpToNext);
            for (int k = 0; k < 10 && !director.LevelUpOpen; k++) yield return null;
            Assert.IsTrue(director.LevelUpOpen, "subir de nivel abre a escolha de talento");
            Assert.IsTrue(GameClock.Paused, "o jogo para durante a escolha");
            Assert.IsTrue(hud.LevelUpVisible);
            Assert.AreEqual(3, director.TalentOffer.Count);
            foreach (var t in director.TalentOffer)
                Assert.IsTrue(t.Element == ElementType.None || t.Element == ElementType.Fire, "so talentos de corpo ou do elemento desperto: " + t.Id);
            for (int k = 0; k < 4; k++) yield return null;
            Shot(cam, "docs/img/nivel_talentos.png");
            // pausado na escolha, comando de jogo nao vira acao (a tecla 1 escolhe o talento, nao solta a Labareda)
            var scripted = hero.gameObject.AddComponent<ScriptedInput>();
            hero.GetComponent<PlayerInputReader>().enabled = false;
            var realInput = hero.Input;
            hero.Input = scripted;
            scripted.PressElementSkill(1);
            scripted.PressDash();
            for (int k = 0; k < 5; k++) yield return null;
            Assert.IsFalse(hero.IsCasting, "pausado, a skill nao sai");
            Assert.IsFalse(hero.IsDashing, "pausado, o dash nao sai");
            Assert.AreEqual(0f, hero.SkillCooldown01(ElementType.Fire), "a recarga da Labareda nao foi gasta");
            hero.Input = realInput;

            var picked = director.TalentOffer[1];
            director.PickTalent(1);
            yield return null;
            Assert.IsFalse(director.LevelUpOpen);
            Assert.IsFalse(GameClock.Paused, "escolheu: o jogo volta");
            Assert.AreEqual(1, director.Run.Talents.Rank(picked.Id));
            Assert.AreEqual(0, director.Run.Progress.PendingPicks);
            Assert.AreEqual(2, director.Run.Progress.Level);

            // talentos mudam o heroi de verdade: Vigor aumenta a vida, Asas da a segunda carga de dash
            float before = hero.Health.Max;
            director.Run.Talents.Take(TalentCatalog.Find("body_health"), hero.Modifiers);
            hero.Awaken(ElementType.Air);
            director.Run.Talents.Take(TalentCatalog.Find("air_dash"), hero.Modifiers);
            director.Run.Progress.AddXp(director.Run.Progress.XpToNext);
            for (int k = 0; k < 10 && !director.LevelUpOpen; k++) yield return null;
            director.PickTalent(0);
            for (int k = 0; k < 5; k++) yield return null;
            Assert.Greater(hero.Health.Max, before, "Vigor aumenta a vida maxima");
            Assert.AreEqual(2, hero.DashMaxCharges, "Asas da a segunda carga de dash");
            GameFlow.AutoPickTalents = true;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Each_Starting_Element_Brings_Its_Own_Hero()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            foreach (var e in Elements.All)
            {
                GameEvents.Clear();
                GameFlow.StartElement = e;
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                var director = Object.FindFirstObjectByType<RunDirector>();
                var hud = Object.FindFirstObjectByType<Hud>();
                cam = Camera.main;
                hud.UseCamera(cam);
                Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
                for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
                var hero = Object.FindFirstObjectByType<PlayerController>();
                var skin = hero.GetComponent<HeroSkin>();
                Assert.IsNotNull(skin, "o heroi tem os quatro modelos");
                Assert.AreEqual(e, skin.Shown, "o modelo e o do elemento inicial");
                Assert.AreEqual("Model_" + HeroSkin.NameOf(e), skin.Model.name);
                Assert.IsTrue(skin.Model.activeInHierarchy);
                Assert.AreEqual(1, hero.GetComponentsInChildren<Animator>().Length, "so um modelo visivel por vez");
                Assert.Greater(skin.MagicParts, 0, HeroSkin.NameOf(e) + " tem uma parte magica que muda de cor");
                Assert.AreEqual(skin.Model.GetComponent<Animator>(), hero.GetComponentInChildren<Animator>());
                hero.Teleport(new Vector3(0, 0.05f, -1f), new Vector3(0.35f, 0, -1f).normalized);
                var scripted = hero.gameObject.AddComponent<ScriptedInput>();
                hero.GetComponent<PlayerInputReader>().enabled = false;
                hero.Input = scripted;
                scripted.AimPoint = hero.transform.position + new Vector3(0.35f, 0, -1f);
                for (int k = 0; k < 20; k++) yield return null;
                Shot(cam, $"docs/img/heroi_{HeroSkin.NameOf(e).ToLowerInvariant()}.png");
                // de perto, para conferir o encaixe das pecas
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null) follow.enabled = false;
                var home = (cam.transform.position, cam.transform.rotation, cam.fieldOfView);
                cam.transform.position = hero.transform.position + new Vector3(1.6f, 2.6f, -4.6f);
                cam.transform.LookAt(hero.transform.position + Vector3.up * 1.3f);
                cam.fieldOfView = 40f;
                yield return null;
                Shot(cam, $"docs/img/heroi_{HeroSkin.NameOf(e).ToLowerInvariant()}_perto.png");
                // ataque e corrida: as pecas acompanham os ossos
                scripted.AttackHeld = true;
                for (int k = 0; k < 9; k++) yield return null;
                Shot(cam, $"docs/img/heroi_{HeroSkin.NameOf(e).ToLowerInvariant()}_golpe.png");
                scripted.AttackHeld = false;
                (cam.transform.position, cam.transform.rotation, cam.fieldOfView) = home;
                if (follow != null) follow.enabled = true;
            }
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Elementals_And_The_Swarm()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Water;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            director.ApplyLayout(-1);
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, -4f), Vector3.forward);
            GameObject Load(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/" + n + ".prefab");
            var ember = Object.Instantiate(Load("BrasaViva"), new Vector3(-2.6f, 0, 0.6f), Quaternion.Euler(0, 160, 0)).GetComponent<Health>();
            var sentinel = Object.Instantiate(Load("Sentinela"), new Vector3(2.8f, 0, 1.2f), Quaternion.Euler(0, 200, 0)).GetComponent<Health>();
            for (int i = 0; i < 3; i++) Object.Instantiate(Load("Cinzento"), new Vector3(-0.8f + i * 0.8f, 0, 3.5f), Quaternion.Euler(0, 180, 0));
            for (int k = 0; k < 40; k++) yield return null;
            StatusHolder.GetOrAdd(ember).Apply(StatusType.Rooted, 60f, Team.Player, hero.gameObject);
            Assert.AreEqual(Tips.Text("affinity"), hud.LastTip, "o primeiro elemental traz a dica de afinidade");

            // Brasa Viva: resiste a Fogo, e fraca a Agua, nao queima
            float full = ember.Current;
            ember.TakeDamage(new DamageInfo(10f, ElementType.Fire, Team.Player, hero.gameObject));
            Assert.AreEqual(full - 5f, ember.Current, 0.01f, "resiste ao Fogo");
            ember.TakeDamage(new DamageInfo(10f, ElementType.Water, Team.Player, hero.gameObject));
            Assert.AreEqual(full - 21f, ember.Current, 0.01f, "fraca a Agua");
            StatusHolder.GetOrAdd(ember).Apply(StatusType.Burn, 3f, Team.Player, hero.gameObject);
            Assert.IsFalse(ember.GetComponent<StatusHolder>().State.Has(StatusType.Burn));

            // Sentinela: nao anda e nao e empurrada
            var at = sentinel.transform.position;
            sentinel.TakeDamage(new DamageInfo(1f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.forward, Knockback = 20f });
            for (int k = 0; k < 40; k++) yield return null;
            Assert.Less(Vector3.Distance(at, sentinel.transform.position), 0.3f, "a Sentinela fica onde esta");
            foreach (var n in new[] { "brasa_head", "brasa_body", "brasa_weapon" })
                Assert.IsTrue(ember.GetComponentsInChildren<Transform>(true).Any(t => t.name == n), "falta a peca " + n);
            Assert.IsTrue(sentinel.GetComponentsInChildren<Transform>(true).Any(t => t.name == "sentinela_base"), "a Sentinela tem o pedestal");
            Assert.Less(Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).First(e => e.Definition.displayName == "Cinzento").transform.Find("Model").lossyScale.x, 0.8f, "Cinzento e pequeno");

            hud.ShowBanner("", "", Color.clear, 0.01f);
            for (int k = 0; k < 3; k++) yield return null;
            Shot(cam, "docs/img/inimigos_elementais.png");

            // morta, a Brasa estoura num circulo avisado
            ember.TakeDamage(new DamageInfo(9999f, ElementType.Water, Team.Player, hero.gameObject));
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<VolatileBurst>(), "a Brasa Viva estoura ao morrer");
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Third_Skill_On_Magma_Makes_A_Fusion()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            director.ApplyLayout(-1);
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Awaken(ElementType.Earth);
            hero.Awaken(ElementType.Water);
            hero.Teleport(new Vector3(0, 0.05f, -3f), Vector3.forward);
            var scripted = hero.gameObject.AddComponent<ScriptedInput>();
            hero.GetComponent<PlayerInputReader>().enabled = false;
            hero.Input = scripted;
            var brutePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Bruto.prefab");
            var brute = Object.Instantiate(brutePrefab, new Vector3(0, 0, 0.5f), Quaternion.Euler(0, 180, 0));
            yield return null;
            StatusHolder.GetOrAdd(brute.GetComponent<Health>()).Apply(StatusType.Rooted, 60f, Team.Player, hero.gameObject);
            for (int k = 0; k < 30; k++) yield return null; // termina de nascer
            scripted.AimPoint = brute.transform.position;
            string last = "", started = "", denied = "";
            GameEvents.ReactionTriggered += e => last = e.Name;
            hero.SkillStarted += e => started += e + " ";
            hero.SkillDenied += e => denied += e + " ";
            scripted.PressElementSkill(1); // Labareda marca de Fogo
            for (int k = 0; k < 36; k++) { scripted.AimPoint = brute.transform.position; yield return null; }
            for (int k = 0; k < 60 && hero.IsCasting; k++) yield return null;
            scripted.PressElementSkill(3); // Pilar de Pedra detona: Magma
            for (int k = 0; k < 36 && last != "Magma"; k++) yield return null;
            Assert.AreEqual("Magma", last, "Fogo + Terra fazem Magma");
            Assert.IsTrue(DamageZone.Active.Any(z => z.Fusable), "o Magma deixa a poca no chao");
            // a pausa de impacto da reacao corre em tempo real: no teste ela dura muitos quadros
            for (int k = 0; k < 1500 && hero.IsCasting; k++) yield return null; // termina o Pilar antes de pedir a proxima skill
            var pool = DamageZone.Active.First(z => z.Fusable);
            scripted.AimPoint = pool.transform.position;
            scripted.PressElementSkill(2); // Mare sobre a poca: Obsidiana
            for (int k = 0; k < 1500 && hero.Fusions == 0; k++) yield return null;
            Assert.AreEqual(1, hero.Fusions, $"a terceira skill funde (recarga da Agua {hero.SkillCooldown01(ElementType.Water):0.00}, distancia {Vector3.Distance(hero.transform.position, pool != null ? pool.transform.position : Vector3.zero):0.0}, poca viva {pool != null}, ultima reacao {last}, raio {(pool != null ? pool.Radius : -1f)}, lancadas {started}, negadas {denied}, elementos {string.Join(",", hero.Loadout.Awakened)}, pode agir {!hero.TryGetComponent(out StatusHolder sh) || sh.State.CanAct})");
            Assert.AreEqual("Obsidiana", last);
            Assert.IsFalse(DamageZone.Active.Any(z => z.Fusable), "a fusao consome a poca");
            hud.ShowBanner("", "", Color.clear, 0.01f);
            hud.ShowTip("", 0.01f);
            for (int k = 0; k < 3; k++) yield return null;
            Shot(cam, "docs/img/fusao_obsidiana.png");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Weaver_Keeps_Distance_And_Blinks()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Water;
            GameFlow.Seed = 4; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            Assert.IsTrue(director.WeaverRun, "semente par: o chefe final e a Tecela");
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, -2f), Vector3.forward);
            director.ApplyLayout(-1);
            director.DressBiome(Biome.Heart);
            director.PlaceBiomeProps(Biome.Heart, 1);
            string name = "";
            GameEvents.BossSpawned += (_, n) => name = n;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Tecela.prefab");
            Assert.IsNotNull(prefab);
            var weaver = Object.Instantiate(prefab, new Vector3(0, 0, 2.5f), Quaternion.Euler(0, 180, 0)).GetComponent<BossBrain>();
            for (int k = 0; k < 40; k++) yield return null;
            Assert.AreEqual("Tecelã das Cinzas", name);
            foreach (var n in new[] { "weaver_head", "weaver_mantle", "weaver_core", "weaver_staff", "weaver_shards" })
                Assert.IsTrue(weaver.GetComponentsInChildren<Transform>(true).Any(t => t.name == n), "falta a peca " + n);
            for (int k = 0; k < 900 && weaver.Blinks == 0; k++) yield return null;
            Assert.Greater(weaver.Blinks, 0, "ela se teletransporta");
            for (int k = 0; k < 60; k++) yield return null;
            Assert.Greater(Vector3.Distance(weaver.transform.position, hero.transform.position), 4f, "e guarda distancia do heroi");
            // so para a foto: ela parada a frente do heroi, sem o letreiro
            weaver.enabled = false;
            weaver.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(hero.transform.position + new Vector3(1.5f, 0, 4.5f));
            weaver.transform.rotation = Quaternion.Euler(0, 200, 0);
            hud.ShowBanner("", "", Color.clear, 0.01f);
            for (int k = 0; k < 6; k++) yield return null;
            Shot(cam, "docs/img/chefe_tecela.png");
            weaver.enabled = true;
            var hp = weaver.GetComponent<Health>();
            hp.TakeDamage(new DamageInfo(hp.Max * 0.75f, ElementType.None, Team.Player, hero.gameObject));
            for (int k = 0; k < 30; k++) yield return null;
            Assert.AreEqual(2, weaver.Phase);
            Assert.IsTrue(weaver.Shield.Up, "chefe final: a ultima fase levanta o escudo");
            Object.Destroy(weaver.gameObject);
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator MiniBoss_Has_Phases_But_No_Shield()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Earth;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Teleport(new Vector3(-1.2f, 0.05f, -2.2f), Vector3.forward);
            director.ApplyLayout(-1);
            director.PlaceBiomeProps(Biome.Garden, 2);
            director.DressBiome(Biome.Garden);
            string name = "";
            GameEvents.BossSpawned += (_, n) => name = n;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Carrasco.prefab");
            Assert.IsNotNull(prefab, "o Carrasco existe");
            var mini = Object.Instantiate(prefab, new Vector3(2.6f, 0, 1.6f), Quaternion.Euler(0, 210, 0)).GetComponent<BossBrain>();
            for (int k = 0; k < 45; k++) yield return null;
            Assert.AreEqual("Carrasco", name, "ele se anuncia como chefe (barra no alto)");
            Assert.AreEqual("CARRASCO", hud.LastBanner, "e com o nome no letreiro");
            Assert.IsNotNull(mini.transform.Find("Model").GetComponentInChildren<Renderer>());
            var hp = mini.GetComponent<Health>();
            Assert.Less(hp.Max, 400f, "mini-chefe: bem menos vida que o Guardiao");
            // so para a foto: para o Carrasco onde esta e tira o letreiro do trecho da frente
            mini.enabled = false;
            hud.ShowBanner("", "", Color.clear, 0.01f);
            for (int k = 0; k < 4; k++) yield return null;
            Shot(cam, "docs/img/mini_chefe.png");
            mini.enabled = true;
            // leva ate a ultima fase: ela acelera mas nao levanta escudo
            hp.TakeDamage(new DamageInfo(hp.Max * 0.75f, ElementType.None, Team.Player, hero.gameObject));
            for (int k = 0; k < 30; k++) yield return null;
            Assert.AreEqual(2, mini.Phase, "terceira fase");
            Assert.IsFalse(mini.Shield.Up, "o Carrasco nao tem o escudo da Quintessencia");
            float before = hp.Current;
            hp.TakeDamage(new DamageInfo(5f, ElementType.None, Team.Player, hero.gameObject));
            Assert.Less(hp.Current, before, "na ultima fase ele continua levando dano");
            hp.TakeDamage(new DamageInfo(9999f, ElementType.None, Team.Player, hero.gameObject));
            Assert.IsTrue(hp.IsDead);
            // a morte de chefe tem uma pausa dramatica em tempo real (no teste ela dura centenas de quadros)
            for (int k = 0; k < 3000 && Object.FindFirstObjectByType<BossBrain>() != null; k++) yield return null;
            Assert.IsNull(Object.FindFirstObjectByType<BossBrain>(), "morto, ele some");
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Run_Is_Saved_And_Can_Be_Continued()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Air;
            GameFlow.Seed = 5; GameFlow.Layers = 6; GameFlow.Shrines = 1; GameFlow.Ascension = 0;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            Assert.IsNotNull(RunSaveStore.Load(), "entrar na primeira sala ja grava a run");
            StringAssert.Contains("tecla 4", Object.FindFirstObjectByType<Hud>().LastTip, "a primeira sala mostra a dica de controles com a tecla da skill do Vey");
            Assert.IsTrue(Tips.WasSeen("start"));
            var hero = Object.FindFirstObjectByType<PlayerController>();
            var hp = hero.GetComponent<Health>();

            // a run anda: elemento novo, talento, grau de skill, pacto, Essencia, dano levado
            hero.Awaken(ElementType.Water);
            director.Run.Talents.Take(TalentCatalog.Find("body_health"), hero.Modifiers);
            director.Run.Deepen(ElementType.Air, hero.Modifiers);
            director.ShowPact();
            yield return null;
            director.Run.AddEssence(77);
            director.Interactables.First(i => i.Prompt.Contains("Sangue")).Use();
            hp.TakeDamage(new DamageInfo(23f, ElementType.None, Team.Enemy));
            director.Run.Advance(director.Run.Choices[0].Id);
            director.Run.Kills = 9;
            director.SaveRun();
            // dano levado depois de entrar na sala tambem fica no save (sair e continuar nao devolve a vida)
            hp.TakeDamage(new DamageInfo(11f, ElementType.None, Team.Enemy));
            director.SyncSavedHealth();
            Assert.AreEqual(hp.Current, RunSaveStore.Load().health, 0.01f);
            int room = director.Run.Current.Id, essence = director.Run.Essence;
            float life = hp.Current, max = hp.Max, damage = hero.Modifiers.DamageAll, range = hero.Modifiers.SkillRange(ElementType.Air);
            float airCd = hero.Modifiers.SkillCooldown(ElementType.Air);
            string mods = hero.Modifiers.ToJson();

            // o jogador fecha a aba: no titulo aparece Continuar
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Title.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int k = 0; k < 5; k++) yield return null;
            Assert.IsNotNull(GameObject.Find("Continue"), "com run salva, o titulo oferece continuar");
            Object.FindFirstObjectByType<TitleMenu>().UseCamera(Camera.main);
            yield return null;
            Shot(Camera.main, "docs/img/titulo_continuar.png");

            Assert.IsTrue(GameFlow.ContinueRun());
            for (int k = 0; k < 60 && SceneManager.GetActiveScene().name != GameFlow.RunScene; k++) yield return null;
            for (int k = 0; k < 4; k++) yield return null;
            director = Object.FindFirstObjectByType<RunDirector>();
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            hero = Object.FindFirstObjectByType<PlayerController>();
            hp = hero.GetComponent<Health>();
            Assert.IsTrue(director.Resumed);
            Assert.AreEqual(ElementType.Air, hero.GetComponent<HeroSkin>().Shown, "volta o mesmo heroi (Vey)");
            Assert.AreEqual(room, director.Run.Current.Id, "volta para a sala em que parou");
            Assert.AreEqual(essence, director.Run.Essence);
            Assert.AreEqual(2, hero.Loadout.Awakened.Count, "com a Agua desperta");
            Assert.AreEqual(1, director.Run.Talents.Rank("body_health"));
            Assert.AreEqual(2, director.Run.SkillRank[(int)ElementType.Air]);
            Assert.IsTrue(director.Run.Boons.Any(b => b.name == "Pacto de Sangue"), "o pacto continua valendo");
            Assert.AreEqual(9, director.Run.Kills);
            Assert.AreEqual(max, hp.Max, 0.01f);
            Assert.AreEqual(life, hp.Current, 0.01f, "a vida de quando entrou na sala");
            Assert.AreEqual(damage, hero.Modifiers.DamageAll, 0.0001f, "o +25% do pacto nao some nem dobra");
            Assert.AreEqual(range, hero.Modifiers.SkillRange(ElementType.Air), 0.0001f);
            Assert.AreEqual(airCd, hero.Modifiers.SkillCooldown(ElementType.Air), 0.0001f);
            Assert.AreEqual(mods, hero.Modifiers.ToJson(), "os modificadores voltam iguais");

            // a run termina: o save some
            hp.Pool.Invulnerable = false;
            hp.TakeDamage(new DamageInfo(99999f, ElementType.None, Team.Enemy));
            for (int k = 0; k < 3000 && RunSaveStore.Load() != null; k++) yield return null;
            Assert.IsNull(RunSaveStore.Load(), "acabou a run, nao ha o que continuar");
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Hazards_Treasure_Pact_And_Biomes()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            var hero = Object.FindFirstObjectByType<PlayerController>();
            var heroHp = hero.GetComponent<Health>();
            Assert.AreEqual(Biome.Ashes, director.CurrentBiome, "a run comeca nos Saloes de Cinza");
            Assert.IsNotEmpty(hud.LastBanner, "o primeiro trecho e anunciado");

            // barril de polvora: um golpe acende, a explosao fere o inimigo ao lado e acende o outro barril
            var barrelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Game/Prefabs/Obstacles/ExplosiveBarrel.prefab");
            var minionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Rastejante.prefab");
            director.ApplyLayout(-1); // sem os perigos do layout da sala na conta
            yield return null;
            hero.Teleport(new Vector3(-7f, 0.05f, -5f), Vector3.forward);
            var b1 = Object.Instantiate(barrelPrefab, new Vector3(3f, 0, 1f), Quaternion.identity);
            var b2 = Object.Instantiate(barrelPrefab, new Vector3(5.2f, 0, 1f), Quaternion.identity);
            var minion = Object.Instantiate(minionPrefab, new Vector3(3f, 0, 2.6f), Quaternion.identity).GetComponent<Health>();
            StatusHolder.GetOrAdd(minion).Apply(StatusType.Rooted, 30f, Team.Player, hero.gameObject);
            int blasts = 0;
            GameEvents.Exploded += (_, _) => blasts++;
            yield return null;
            float before = minion.Current, heroBefore = heroHp.Current;
            b1.GetComponent<Health>().TakeDamage(new DamageInfo(50, ElementType.None, Team.Player, hero.gameObject));
            Assert.IsTrue(b1.GetComponent<ExplosiveBarrel>().Lit);
            for (int k = 0; k < 6; k++) yield return null;
            Shot(cam, "docs/img/perigo_barril.png");
            for (float t = 3f; t > 0f && blasts < 2; t -= Time.deltaTime) yield return null;
            Assert.AreEqual(2, blasts, "um barril acende o outro");
            Assert.IsTrue(minion.IsDead || minion.Current < before - 10f, "a explosao fere os inimigos em volta");
            Assert.AreEqual(heroBefore, heroHp.Current, "heroi longe nao leva nada");
            for (int k = 0; k < 5; k++) yield return null;
            Assert.IsTrue(b1 == null && b2 == null, "barris estourados somem");
            foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);

            // armadilha de espinhos: avisa e fura quem esta em cima
            var trapPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Game/Prefabs/Obstacles/SpikeTrap.prefab");
            var trap = Object.Instantiate(trapPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<SpikeTrap>();
            hero.Teleport(new Vector3(0.3f, 0.05f, 0.2f), Vector3.forward);
            int warnings = 0;
            GameEvents.TelegraphStarted += e => { if (e.Source == trap.gameObject) warnings++; };
            heroBefore = heroHp.Current;
            for (float t = 8f; t > 0f && trap.Strikes == 0; t -= Time.deltaTime) yield return null;
            Assert.AreEqual(1, warnings, "a armadilha avisa antes de subir");
            Assert.Less(heroHp.Current, heroBefore, "quem fica em cima e furado");
            yield return null;
            Shot(cam, "docs/img/perigo_espinhos.png");
            Object.Destroy(trap.gameObject);
            heroHp.Heal(999);

            // poca de corrupcao: fere e queima quem esta dentro
            var poolPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Game/Prefabs/Obstacles/CorruptionPool.prefab");
            var pool = Object.Instantiate(poolPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<CorruptionPool>();
            hero.Teleport(new Vector3(0.2f, 0.05f, 0.1f), Vector3.forward);
            heroBefore = heroHp.Current;
            for (int k = 0; k < 60 && pool.Pulses < 2; k++) yield return null;
            Assert.Less(heroHp.Current, heroBefore, "quem pisa na poca se fere");
            Assert.AreEqual(StatusType.Burn, pool.Status, "nos Saloes de Cinza ela queima");
            Shot(cam, "docs/img/perigo_poca.png");
            Object.Destroy(pool.gameObject);
            heroHp.Heal(999);

            // tesouro: abrir paga Essencia
            director.ShowTreasure();
            hero.Teleport(new Vector3(0, 0.05f, -2.4f), Vector3.forward);
            for (int k = 0; k < 20; k++) yield return null;
            Shot(cam, "docs/img/sala_tesouro.png");
            int essence = director.Run.Essence;
            director.Interactables.First(i => i.Prompt.StartsWith("abrir")).Use();
            Assert.Greater(director.Run.Essence, essence + 20);
            Assert.IsFalse(director.Interactables.Any(i => i.Prompt.StartsWith("abrir")), "o bau so abre uma vez");

            // pacto: ganho e custo de verdade, e so um por sala
            director.ShowPact();
            hero.Teleport(new Vector3(0, 0.05f, -3.2f), Vector3.forward);
            for (int k = 0; k < 20; k++) yield return null;
            Shot(cam, "docs/img/sala_pacto.png");
            var pacts = director.Interactables.Where(i => i.Prompt.StartsWith("selar")).ToList();
            Assert.AreEqual(3, pacts.Count);
            // Ganancia sem Essencia: recusa e nada muda
            director.Run.TrySpend(director.Run.Essence);
            pacts[2].Use();
            Assert.AreEqual(0, director.PactsTaken);
            float maxBefore = heroHp.Max, dmgBefore = hero.Modifiers.DamageAll;
            pacts[0].Use();
            Assert.AreEqual(1, director.PactsTaken);
            Assert.Less(heroHp.Max, maxBefore - 5f, "Pacto de Sangue cobra vida maxima");
            Assert.Greater(hero.Modifiers.DamageAll, dmgBefore + 0.2f, "e paga em dano");
            Assert.LessOrEqual(heroHp.Current, heroHp.Max);
            yield return null;
            Assert.IsFalse(director.Interactables.Any(i => i.Prompt.StartsWith("selar")), "um pacto por sala");
            Assert.IsTrue(director.Run.Boons.Any(b => b.name == "Pacto de Sangue"), "o pacto aparece no inventario");

            // salas sem luta tem cenario proprio
            director.ShowRest();
            hero.Teleport(new Vector3(0, 0.05f, -2f), Vector3.forward);
            for (int k = 0; k < 20; k++) yield return null;
            Assert.AreEqual("RestDecor", director.CurrentDecor);
            Assert.IsNotNull(GameObject.Find("bed_decorated"), "o Descanso tem camas");
            Shot(cam, "docs/img/sala_descanso.png");
            director.ShowMaster();
            for (int k = 0; k < 20; k++) yield return null;
            Assert.AreEqual("MasterDecor", director.CurrentDecor);
            Assert.IsNotNull(director.MasterNpc, "a sala do Mestre tem o Mestre");
            for (int k = 0; k < 20 && !director.MasterNpc.Greeted; k++) yield return null;
            Assert.IsTrue(director.MasterNpc.Greeted, "ele cumprimenta o heroi");
            Shot(cam, "docs/img/sala_mestre.png");
            director.ShowTreasure();
            for (int k = 0; k < 20; k++) yield return null;
            Assert.AreEqual("TreasureDecor", director.CurrentDecor);
            Assert.IsNull(GameObject.Find("bed_decorated"), "o cenario da sala anterior some");
            Shot(cam, "docs/img/sala_tesouro.png");
            director.ShowPact();
            for (int k = 0; k < 20; k++) yield return null;
            Shot(cam, "docs/img/sala_pacto.png");

            // trechos: cada um troca o clima da sala
            director.ShowShrine();
            hero.Teleport(new Vector3(0, 0.05f, -4.5f), Vector3.forward);
            foreach (Biome b in System.Enum.GetValues(typeof(Biome)))
            {
                director.ApplyLayout(-1);
                director.PlaceBiomeProps(b, 3);
                Assert.AreEqual(4, director.BiomePropsPlaced, "cada trecho tem adereços proprios nas bordas da sala");
                director.DressBiome(b, true);
                Assert.AreEqual(b, director.CurrentBiome);
                if (b != Biome.Ashes) Assert.AreEqual(Biomes.Name(b).ToUpperInvariant(), hud.LastBanner);
                for (int k = 0; k < 15; k++) yield return null;
                Shot(cam, $"docs/img/bioma_{(int)b + 1}.png");
            }
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Elite_Room_Applies_Its_Trait_And_Depth_Scales_Enemies()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            player = Object.FindFirstObjectByType<PlayerController>();
            player.GetComponent<Health>().Pool.Invulnerable = true; // confere os numeros dos inimigos, nao a luta
            cam = Camera.main;
            hud.UseCamera(cam);
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;

            // primeira sala (profundidade 0): inimigos com os numeros da ficha
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            var early = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert.Greater(early.Length, 0, "a primeira sala deveria ter inimigos");
            foreach (var e in early)
            {
                Assert.AreEqual(e.Definition.maxHealth, e.GetComponent<Health>().Max, 0.01f, "sala 1: a vida da ficha");
                Assert.AreEqual(1f, e.Scaling.Damage, 0.001f);
            }

            // Elite Resistentes na profundidade 4
            foreach (var e in early) Object.Destroy(e.gameObject);
            yield return null;
            director.StartEncounter(RoomType.Elite, 4, EliteModifier.Sturdy);
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            for (int k = 0; k < 25; k++) yield return null;
            var elite = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert.Greater(elite.Length, 0);
            float want = (1f + DifficultyCurve.HealthPerLayer * 4) * DifficultyCurve.SturdyHealth;
            foreach (var e in elite)
            {
                Assert.AreEqual(e.Definition.maxHealth * want, e.GetComponent<Health>().Max, 0.05f, "Resistentes: mais vida sobre a da profundidade");
                Assert.AreEqual(1f, e.Scaling.Speed, 0.001f);
            }
            Assert.AreEqual(EliteModifier.Sturdy, director.CurrentTrait);
            Shot(cam, "docs/img/elite.png");

            // Elite Velozes: mais rapidos, mesma vida da profundidade
            foreach (var e in elite) Object.Destroy(e.gameObject);
            yield return null;
            director.StartEncounter(RoomType.Elite, 4, EliteModifier.Swift);
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            var swift = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert.Greater(swift.Length, 0);
            foreach (var e in swift)
            {
                Assert.AreEqual(DifficultyCurve.SwiftSpeed, e.Scaling.Speed, 0.001f);
                Assert.AreEqual(e.Definition.moveSpeed * DifficultyCurve.SwiftSpeed, e.GetComponent<UnityEngine.AI.NavMeshAgent>().speed, 0.3f);
                Assert.AreEqual(e.Definition.maxHealth * (1f + DifficultyCurve.HealthPerLayer * 4), e.GetComponent<Health>().Max, 0.05f);
            }
            // Protegidos: o primeiro golpe em cada um e bloqueado
            foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
            yield return null;
            director.StartEncounter(RoomType.Elite, 4, EliteModifier.Warded);
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            var warded = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert.Greater(warded.Length, 0);
            var wh = warded[0].GetComponent<Health>();
            Assert.AreEqual(1, wh.Pool.Blocks, "Protegidos nascem com um bloqueio");
            float full = wh.Current;
            wh.TakeDamage(new DamageInfo(5f, ElementType.None, Team.Player));
            Assert.AreEqual(full, wh.Current, 0.01f, "o primeiro golpe e bloqueado");
            wh.TakeDamage(new DamageInfo(5f, ElementType.None, Team.Player));
            Assert.Less(wh.Current, full, "o segundo entra");

            // Voláteis: quem morre deixa uma explosao avisada no chao
            foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
            yield return null;
            director.StartEncounter(RoomType.Elite, 4, EliteModifier.Volatile);
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            for (int k = 0; k < 25; k++) yield return null;
            var vol = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).First(e => e.Definition.attack != EnemyAttackKind.Explode);
            int blasts = 0;
            GameEvents.Exploded += (_, _) => blasts++;
            vol.GetComponent<Health>().TakeDamage(new DamageInfo(9999f, ElementType.None, Team.Player));
            yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<VolatileBurst>(), "o Volatil deixa o circulo");
            for (int k = 0; k < 3000 && blasts == 0; k++) yield return null; // a camera lenta do abate dura muitos quadros no teste
            Assert.Greater(blasts, 0, "e o circulo explode");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Tutorial_Teaches_Every_Step_Until_Quintessence()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Title.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var menu = Object.FindFirstObjectByType<TitleMenu>();
            Assert.IsNotNull(menu.OnTutorial, "o titulo deveria ter o botao de treino ligado");
            menu.OnTutorial();
            for (int i = 0; i < 30 && SceneManager.GetActiveScene().name != GameFlow.RunScene; i++) yield return null;
            for (int i = 0; i < 10; i++) yield return null;
            var tut = Object.FindFirstObjectByType<TutorialDirector>();
            Assert.IsNotNull(tut, "a cena de run deveria abrir o treino");
            var hud = Object.FindFirstObjectByType<Hud>();
            player = Object.FindFirstObjectByType<PlayerController>();
            hp = player.GetComponent<Health>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            input = player.gameObject.AddComponent<ScriptedInput>();
            player.Input = input;
            cam = Camera.main;
            hud.UseCamera(cam);
            Assert.AreEqual(0, Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length, "treino nao tem inimigo que revida");
            var dummies = Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None);
            Assert.AreEqual(3, dummies.Length);
            Assert.AreEqual(1, player.Loadout.Awakened.Count);

            System.Func<TutorialStep, float, IEnumerator> until = WaitStep;
            IEnumerator WaitStep(TutorialStep step, float seconds)
            {
                for (float t = seconds; t > 0f && tut.State.Step == step; t -= Time.deltaTime) yield return null;
                Assert.AreNotEqual(step, tut.State.Step, "o treino travou em " + step);
            }
            Transform Closest() => dummies.Where(d => !d.GetComponent<Health>().IsDead)
                .OrderBy(d => Vector3.Distance(d.transform.position, player.transform.position)).First().transform;
            IEnumerator Approach()
            {
                for (float t = 6f; t > 0f; t -= Time.deltaTime)
                {
                    var to = Closest().position - player.transform.position; to.y = 0;
                    input.AimPoint = Closest().position;
                    if (to.magnitude < 2.4f) break;
                    input.Move = new Vector2(to.x, to.z).normalized;
                    yield return null;
                }
                input.Move = Vector2.zero;
            }

            // bater antes da hora nao pula a licao de mover
            Assert.AreEqual(TutorialStep.Move, tut.State.Step);
            input.Move = new Vector2(1, 0);
            yield return until(TutorialStep.Move, 6f);
            input.Move = Vector2.zero;
            Shot(cam, "docs/img/treino.png");

            Assert.AreEqual(TutorialStep.Dash, tut.State.Step);
            input.Move = new Vector2(-1, 0);
            input.PressDash();
            yield return until(TutorialStep.Dash, 3f);
            input.Move = Vector2.zero;

            yield return Approach();
            input.AttackHeld = true;
            yield return until(TutorialStep.Attack, 8f);
            input.AttackHeld = false;

            // skill de Fogo (tecla 1) num boneco: marca
            for (float t = 1.5f; t > 0f; t -= Time.deltaTime) yield return null;
            yield return Approach();
            var marked = Closest();
            input.AimPoint = marked.position;
            input.PressElementSkill(1);
            yield return until(TutorialStep.Skill, 4f);
            Assert.AreEqual(2, player.Loadout.Awakened.Count, "o treino desperta a segunda skill para a reacao");

            // skill de Agua (tecla 2) no mesmo boneco: reacao
            for (float t = 0.8f; t > 0f; t -= Time.deltaTime) yield return null;
            for (float t = 12f; t > 0f && tut.State.Step == TutorialStep.React; t -= Time.deltaTime)
            {
                var to = marked.position - player.transform.position; to.y = 0;
                input.AimPoint = marked.position;
                input.Move = to.magnitude > 3f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
                // se a marca expirou, o Fogo marca de novo assim que recarregar
                if (to.magnitude <= 3.2f) input.PressElementSkill(player.SkillCooldown01(ElementType.Water) <= 0f ? 2 : 1);
                yield return null;
            }
            input.Move = Vector2.zero;
            Assert.AreNotEqual(TutorialStep.React, tut.State.Step, "o treino travou na reacao");
            Shot(cam, "docs/img/treino_reacao.png");

            Assert.AreEqual(4, player.Loadout.Awakened.Count, "para a Quintessencia o treino entrega os quatro elementos");
            Assert.IsTrue(player.QuintessenceReady);
            for (float t = 0.5f; t > 0f; t -= Time.deltaTime) yield return null;
            input.PressQuintessence();
            yield return until(TutorialStep.Quintessence, 3f);
            Assert.IsTrue(tut.State.Finished);
            for (float t = 8f; t > 0f && !hud.ResultVisible; t -= Time.deltaTime) yield return null;
            Assert.IsTrue(hud.ResultVisible, "no fim o treino oferece comecar a run ou voltar ao menu");
            Assert.AreEqual(hp.Max, hp.Current, 0.01f, "ninguem bate no heroi no treino");
            Shot(cam, "docs/img/treino_fim.png");
            GameFlow.Tutorial = false;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Nothing_Traps_The_Player()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.Tutorial = false;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            var spawner = Object.FindFirstObjectByType<WaveSpawner>();
            player = Object.FindFirstObjectByType<PlayerController>();
            hp = player.GetComponent<Health>();
            hp.Pool.Invulnerable = true;
            player.GetComponent<PlayerInputReader>().enabled = false;
            player.Input = player.gameObject.AddComponent<ScriptedInput>();
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            for (int k = 0; k < 40; k++) yield return null; // termina a animacao de nascer

            // 1) heroi cai para fora da sala: volta para a entrada
            player.Teleport(new Vector3(0, -30f, 0), Vector3.forward);
            for (int k = 0; k < 5; k++) yield return null;
            Assert.IsTrue(Arena.Contains(player.transform.position), "heroi fora da sala deveria voltar");
            Assert.AreEqual(1, director.HeroRescues);

            // 2) inimigo jogado para fora da sala e do NavMesh: volta sozinho
            var enemies = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert.Greater(enemies.Length, 0);
            var lost = enemies[0];
            lost.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            lost.transform.position = new Vector3(60, 0, 60);
            for (int k = 0; k < 40; k++) yield return null;
            Assert.IsTrue(Arena.Contains(lost.transform.position), "inimigo fora da sala deveria voltar");
            Assert.IsTrue(lost.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh);
            Assert.GreaterOrEqual(lost.Rescues, 1);

            // 3) luta travada (inimigos parados longe, ninguem leva dano): sao trazidos para perto do heroi
            spawner.SetStallSeconds(3f);
            foreach (var e in enemies)
            {
                e.enabled = false; // nem anda, nem ataca, nem se resgata sozinho
                e.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                e.transform.position = new Vector3(9.5f, 0, 7.5f);
            }
            player.Teleport(new Vector3(-7f, 0, -6f), Vector3.forward);
            for (int k = 0; k < 150 && spawner.StallRescues == 0; k++) yield return null;
            Assert.GreaterOrEqual(spawner.StallRescues, 1, "luta parada deveria disparar o resgate");
            yield return null;
            foreach (var e in enemies)
                Assert.Less(Vector3.Distance(e.transform.position, player.transform.position), 9f, "inimigo deveria ter vindo para perto do heroi");
            spawner.SetStallSeconds(25f);

            // 4) perder o foco da janela pausa; continuar devolve o tempo
            director.SendMessage("OnApplicationFocus", false);
            Assert.IsTrue(GameClock.Paused, "perder o foco deveria pausar");
            Assert.AreEqual(0f, Time.timeScale);
            director.SetPaused(false);
            Assert.IsFalse(GameClock.Paused);
            Assert.AreEqual(1f, Time.timeScale);

            // 5) Santuario sem elemento para despertar: abre as portas e paga Essencia em vez de travar
            foreach (var e in Elements.All) player.Awaken(e);
            int essence = director.Run.Essence;
            director.ShowShrine();
            yield return null;
            Assert.Greater(director.Interactables.Count, 0, "sem elemento para despertar, as portas abrem");
            Assert.IsTrue(director.Interactables.All(i => i.Prompt.StartsWith("entrar")));
            Assert.Greater(director.Run.Essence, essence);

            // 6) morrer mostra o resultado, e jogar de novo comeca limpo
            hp.Pool.Invulnerable = false;
            var killer = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Ceifador.prefab");
            var who = Object.Instantiate(killer, new Vector3(9f, 0, 6f), Quaternion.identity);
            who.GetComponent<EnemyBrain>().enabled = false;
            hp.TakeDamage(new DamageInfo(99999, ElementType.None, Team.Enemy, who));
            Assert.IsTrue(hp.IsDead);
            Assert.AreEqual("Ceifador", director.KilledBy, "a derrota diz quem derrubou o heroi");
            Assert.IsTrue(director.Finished, "a morte do heroi deveria encerrar a run");
            for (float t = 5f; t > 0f && !hud.ResultVisible; t -= Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(hud.ResultVisible, "morrer deveria levar a tela de resultado");
            Assert.IsFalse(director.Victory);
            GameFlow.RestartRun();
            yield return null;
            for (int k = 0; k < 60 && Object.FindFirstObjectByType<RunDirector>() == director; k++) yield return null;
            for (int k = 0; k < 10; k++) yield return null;
            var again = Object.FindFirstObjectByType<RunDirector>();
            var hero = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
            Assert.IsFalse(again.Finished);
            Assert.IsFalse(hero.IsDead);
            Assert.AreEqual(hero.Max, hero.Current, 0.01f);
            Assert.IsFalse(GameClock.Paused);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, again.Run.RoomsCleared);
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Every_Layout_Loads_With_Its_Pieces()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            // sem inimigos atrapalhando a foto
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            Assert.GreaterOrEqual(director.LayoutCount, 6);
            // espera a sala terminar de abrir (o escurecimento de entrada usa tempo real)
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            Assert.IsFalse(director.Busy, "a primeira sala nao terminou de abrir");
            // visao da sala inteira para as fotos (a camera do jogo segue o heroi)
            var follow = cam.GetComponent<FollowCamera>();
            if (follow != null) follow.enabled = false;
            var overview = Quaternion.Euler(52f, 0f, 0f);
            cam.transform.SetPositionAndRotation(new Vector3(0, 0, 0.6f) - overview * Vector3.forward * 46f, overview);
            hud.ShowBanner("", "", Color.clear, 0.01f);
            hud.ShowTip("", 0.01f);
            for (int i = 0; i < director.LayoutCount; i++)
            {
                director.ApplyLayout(i);
                for (int k = 0; k < 12; k++) yield return null;
                Assert.IsNotEmpty(director.CurrentLayout);
                var root = GameObject.Find("Layout");
                Assert.IsNotNull(root);
                Assert.GreaterOrEqual(root.transform.childCount, 3, director.CurrentLayout);
                Shot(cam, $"docs/img/layout_{i + 1}.png");
            }
            director.ApplyLayout(-1);
            yield return null;
            Assert.AreEqual("", director.CurrentLayout);
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        void ConsiderDodge(TelegraphEvent e)
        {
            if (player == null) return;
            var p = player.transform.position;
            var side = new Vector3(-e.Direction.z, 0f, e.Direction.x);
            bool threatened;
            if (e.IsLine)
            {
                var rel = p - e.Point;
                float along = Vector3.Dot(rel, e.Direction);
                threatened = along > 0f && along < e.Length && Mathf.Abs(Vector3.Dot(rel, side)) < e.Radius + 1.2f;
            }
            else threatened = Vector3.Distance(p, e.Point) < e.Radius + 1.2f;
            if (!threatened) return;
            dodgeDir = Vector3.Dot(side, -p) >= 0f ? side : -side;
            dodgeLeft = 0.4f;
            input.PressDash();
        }

        Health Nearest()
        {
            Health best = null;
            float bestD = float.MaxValue;
            var boss = Object.FindFirstObjectByType<BossBrain>();
            if (boss != null && !boss.GetComponent<Health>().IsDead) return boss.GetComponent<Health>();
            foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                var h = e.GetComponent<Health>();
                if (h.IsDead) continue;
                float d = Vector3.Distance(e.transform.position, player.transform.position);
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }

        void Render(Camera c)
        {
            if (rt == null)
            {
                rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            }
            c.targetTexture = rt;
            c.Render();
            c.targetTexture = null;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
        }

        void Shot(Camera c, string path)
        {
            Render(c);
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        IEnumerator Step()
        {
            yield return null;
            if (frame++ % 3 != 0 || cam == null) yield break;
            Render(cam);
            File.WriteAllBytes($"{FrameDir}/f{saved++:00000}.jpg", tex.EncodeToJPG(85));
        }
    }
}
#endif
