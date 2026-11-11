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
        }

        [TearDown]
        public void RestoreSave()
        {
            PlayerPrefs.DeleteKey(TestSaveKey);
            MetaStore.UseKey(MetaStore.DefaultKey);
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

            menu.OnPick(ElementType.Water);
            for (int i = 0; i < 30 && SceneManager.GetActiveScene().name != GameFlow.RunScene; i++) yield return null;
            Assert.AreEqual(GameFlow.RunScene, SceneManager.GetActiveScene().name, "escolher o elemento deveria carregar a run");
            for (int i = 0; i < 10; i++) yield return null;
            var p = Object.FindFirstObjectByType<PlayerController>();
            Assert.AreEqual(ElementType.Water, p.Element);
            Assert.AreEqual(1, p.Loadout.Awakened.Count, "a run comeca com um elemento so");
            var director = Object.FindFirstObjectByType<RunDirector>();
            Assert.AreEqual(8, director.Run.Map.LayerCount);
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
            hp.Pool.ExtraDamageTakenMultiplier = 0.4f;
            for (float t = 1.2f; t > 0f; t -= Time.deltaTime) yield return Step();

            // pausa: o tempo para e volta
            director.SetPaused(true);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale); Assert.IsTrue(hud.PauseVisible);
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
                    if (d < 4.5f + reach && player.SkillCooldown01(player.Element) <= 0f) input.PressSkill();
                    if (sinceSwitch > 1.3f) { sinceSwitch = 0f; input.PressSwitch(1); }
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
            hp.Pool.ExtraDamageTakenMultiplier = 0.4f; // confere a loja, nao a dificuldade
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
            Assert.AreEqual(RoomType.Shop, director.Run.Current.Type, "nao chegou a loja");
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
            Object.FindFirstObjectByType<PlayerController>().Teleport(new Vector3(0.5f, 0, -3.8f), Vector3.forward);
            for (int k = 0; k < 60; k++) yield return null;
            Assert.IsNotNull(GameObject.Find("Merchant"), "a loja tem um mercador");
            Assert.GreaterOrEqual(director.Interactables.Count(i => i.Prompt.StartsWith("comprar")), 2);
            Shot(cam, "docs/img/loja_banca.png");
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
