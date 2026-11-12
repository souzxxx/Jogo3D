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
            // a esquiva do Vey e sorteada: nos testes o golpe sempre entra (senao um golpe fatal falha 15% das vezes)
            HealthPool.ForceEvade = false;
            GamePrefs.Force = false;
        }

        [TearDown]
        public void RestoreSave()
        {
            PlayerPrefs.DeleteKey(TestSaveKey);
            MetaStore.UseKey(MetaStore.DefaultKey);
            GameFlow.AutoPickTalents = false;
            HealthPool.ForceEvade = null;
            GamePrefs.Force = null;
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
            menu.ShowFeats();
            yield return null; yield return null;
            Assert.IsTrue(menu.SheetOpen);
            Assert.IsNotNull(GameObject.Find("Feat9"), "os dez feitos aparecem");
            Shot(Camera.main, "docs/img/feitos.png");
            Assert.IsTrue(menu.CloseOverlay(), "Esc fecha o painel");
            Assert.IsFalse(menu.SheetOpen);
            Assert.IsFalse(menu.CloseOverlay(), "sem painel aberto, nada a fechar");
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
            Assert.Contains(Feat.FirstWin, director.NewFeats, "a primeira vitoria cumpre o feito");
            Assert.IsTrue(MetaStore.Current.Has(Feat.FirstWin));
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
            // Clarividencia (Refugio): a escolha passa a mostrar quatro talentos, e a tecla 4 escolhe o quarto
            MetaStore.Current.AddShards(100);
            Assert.IsTrue(MetaStore.Current.TryBuy(MetaUpgrade.Clarity));
            Assert.AreEqual(3, director.Run.TalentChoices, "comprada com a run em andamento, so vale na proxima");
            director.Run.TalentChoices = MetaStore.Current.TalentChoices; // como numa run nova
            director.Run.Progress.AddXp(director.Run.Progress.XpToNext);
            for (int k = 0; k < 10 && !director.LevelUpOpen; k++) yield return null;
            Assert.AreEqual(4, director.TalentOffer.Count, "quatro opcoes com a Clarividencia");
            for (int k = 0; k < 4; k++) yield return null;
            Shot(cam, "docs/img/nivel_quatro.png");
            director.PickTalent(3);
            Assert.IsFalse(director.LevelUpOpen);
            GameFlow.AutoPickTalents = true;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Hero_Mastery_Brings_Its_Perks_Into_The_Run()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.Seed = 3; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            GameFlow.AutoPickTalents = false;
            GameEvents.Clear();
            Assert.AreEqual(4, MetaStore.Current.AddHeroXp(ElementType.Earth, 80) + MetaStore.Current.AddHeroXp(ElementType.Earth, 60), "140 de experiencia: maestria 4");
            MetaStore.Save();
            GameFlow.StartElement = ElementType.Earth;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            hud.UseCamera(Camera.main);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            var hero = Object.FindFirstObjectByType<PlayerController>();
            var hp = hero.GetComponent<Health>();
            Assert.AreEqual(100f + 20f + 10f, hp.Max, 0.01f, "Brann: 100 + 20 da passiva + 10 da maestria 1");
            Assert.AreEqual(1.08f, hero.Modifiers.DamageAll, 0.001f, "maestria 3: +8% de dano");
            Assert.AreEqual(1, director.Run.Rerolls, "maestria 4: uma troca de talentos");
            Assert.AreEqual(0, director.Run.Essence, "a Essencia inicial so vem na maestria 5");
            // maestria 2: a run abre com uma escolha de talento
            for (int k = 0; k < 2000 && !director.LevelUpOpen; k++) yield return null;
            Assert.IsTrue(director.LevelUpOpen, "a maestria 2 oferece um talento logo no comeco");
            Assert.IsTrue(hud.LevelUpVisible);
            Shot(Camera.main, "docs/img/maestria_inicio.png");
            director.RerollTalents();
            Assert.AreEqual(0, director.Run.Rerolls, "a troca da maestria funciona no painel");
            director.PickTalent(0);
            Assert.IsFalse(director.LevelUpOpen);
            Assert.AreEqual(1, director.Run.Talents.Count);
            Assert.AreEqual(0, director.Run.Progress.PendingPicks);
            Assert.AreEqual(1, director.Run.Progress.Level, "o talento da maestria nao mexe no nivel");
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

        [UnityTest, Timeout(300000)]
        public IEnumerator Escudeiro_Blocks_From_The_Front_But_Not_From_Behind()
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
            director.ApplyLayout(-1);
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, -2.4f), Vector3.forward);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Escudeiro.prefab");
            Assert.IsNotNull(prefab, "o prefab do Escudeiro existe");
            var guardBody = Object.Instantiate(prefab, new Vector3(0, 0, 0.2f), Quaternion.Euler(0, 180, 0));
            var h = guardBody.GetComponent<Health>();
            var guard = guardBody.GetComponent<FrontGuard>();
            Assert.IsNotNull(guard, "o Escudeiro traz o escudo");
            Assert.IsTrue(guardBody.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Skeleton_Shield_Large_B"), "segura o escudo do pack");
            Assert.IsTrue(guardBody.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Skeleton_Blade"), "e a lamina");
            for (int k = 0; k < 40; k++) yield return null;

            // de frente: o golpe do heroi bate no escudo
            var scripted = hero.gameObject.AddComponent<ScriptedInput>();
            hero.GetComponent<PlayerInputReader>().enabled = false;
            hero.Input = scripted;
            scripted.AimPoint = guardBody.transform.position;
            float before = h.Current;
            scripted.AttackHeld = true;
            for (int k = 0; k < 400 && guard.Blocks == 0; k++) yield return null;
            scripted.AttackHeld = false;
            Assert.Greater(guard.Blocks, 0, "golpe de frente bate no escudo");
            Assert.Less(before - h.Current, 4f, "e quase nao tira vida");
            for (int k = 0; k < 30; k++) yield return null;
            hud.ShowBanner("", "", Color.clear, 0.01f);
            Shot(cam, "docs/img/inimigo_escudeiro.png");
            {
                // de perto, de frente, para conferir escudo e lamina
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null) follow.enabled = false;
                guardBody.GetComponent<EnemyBrain>().enabled = false;
                guardBody.transform.rotation = Quaternion.LookRotation(Vector3.back);
                hero.Teleport(new Vector3(-6f, 0.05f, -3f), Vector3.forward);
                var at = guardBody.transform.position;
                cam.transform.position = at + guardBody.transform.forward * 5.2f + guardBody.transform.right * 2.2f + Vector3.up * 2.6f;
                cam.transform.LookAt(at + Vector3.up * 1.1f);
                float fov = cam.fieldOfView;
                cam.fieldOfView = 40f;
                for (int k = 0; k < 3; k++) yield return null;
                Shot(cam, "docs/img/escudeiro_perto.png");
                cam.fieldOfView = fov;
                if (follow != null) follow.enabled = true;
                hero.Teleport(guardBody.transform.position + new Vector3(0, 0.05f, -2.4f), Vector3.forward);
                yield return null;
            }

            // pelas costas: dano inteiro
            guardBody.GetComponent<EnemyBrain>().enabled = false;
            guardBody.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            before = h.Current;
            h.TakeDamage(new DamageInfo(20f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.forward });
            Assert.AreEqual(before - 20f, h.Current, 0.01f, "pelas costas entra inteiro");
            // de frente de novo, mas com reacao: atravessa
            guardBody.transform.rotation = Quaternion.LookRotation(Vector3.back);
            before = h.Current;
            h.TakeDamage(new DamageInfo(20f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.forward });
            Assert.AreEqual(before - 3f, h.Current, 0.01f, "de frente leva 15%");
            before = h.Current;
            h.TakeDamage(new DamageInfo(10f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.forward, IsReaction = true });
            Assert.AreEqual(before - 10f, h.Current, 0.01f, "reacao atravessa o escudo");
            // congelado, o golpe de frente estilhaca inteiro (+30%) e so depois o gelo sai
            h.Heal(h.Max);
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<ReactionTable>("Assets/Game/Combat/Data/Reactions.asset");
            StatusHolder.GetOrAdd(h).Apply(StatusType.Frozen, 5f, Team.Player, hero.gameObject);
            before = h.Current;
            ElementCombat.Deliver(h, new DamageInfo(10f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.forward }, table);
            Assert.AreEqual(before - 10f * ElementCombat.ShatterBonus, h.Current, 0.01f, "congelado nao segura o escudo: estilhaca inteiro");
            Assert.IsFalse(h.GetComponent<StatusHolder>().State.Has(StatusType.Frozen), "o gelo sai depois do golpe");
            // um golpe que vem pelas costas entra inteiro mesmo com quem bateu na frente (o Pilar nasce atras do alvo)
            before = h.Current;
            h.TakeDamage(new DamageInfo(10f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.back });
            Assert.AreEqual(before - 10f, h.Current, 0.01f, "vale a direcao do golpe, nao a posicao do heroi");
            // enraizado, o escudo cai
            StatusHolder.GetOrAdd(h).Apply(StatusType.Rooted, 5f, Team.Player, hero.gameObject);
            before = h.Current;
            h.TakeDamage(new DamageInfo(10f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.forward });
            Assert.AreEqual(before - 10f, h.Current, 0.01f, "enraizado nao segura o escudo");
            Assert.AreEqual(100f, guardBody.GetComponent<EnemyBrain>().Definition.turnSpeed, 0.01f, "gira devagar: da para contornar");
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Trial_Room_Gives_A_Talent_When_Passed_And_Nothing_When_Lost()
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
            var hp = Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>();
            int IdFor(TrialKind kind) { for (int i = 0; ; i++) if (Trials.Pick(GameFlow.Seed, i) == kind) return i; }
            // dificuldade Tranquila: o heroi recebe 35% menos dano, e vale na hora (pode ser trocada na pausa)
            GamePrefs.Force = true;
            yield return null;
            Assert.AreEqual(GamePrefs.RelaxedDamageTaken, hp.Pool.AssistMultiplier, 0.001f);
            float full = hp.Current;
            hp.TakeDamage(new DamageInfo(20f, ElementType.None, Team.Enemy));
            Assert.AreEqual(full - 20f * GamePrefs.RelaxedDamageTaken, hp.Current, 0.01f);
            GamePrefs.Force = false;
            yield return null;
            Assert.AreEqual(1f, hp.Pool.AssistMultiplier, 0.001f);
            hp.Heal(999f);
            for (int k = 0; k < 30; k++) yield return null;

            // Ileso: tres golpes ainda cumprem (dois juntos contam um)
            director.StartTrial(new MapNode { Id = IdFor(TrialKind.Unscathed), Type = RoomType.Trial }, 2);
            Assert.IsTrue(director.TrialActive);
            Assert.AreEqual(TrialKind.Unscathed, director.TrialKind);
            for (int k = 0; k < 20; k++) yield return null;
            StringAssert.Contains("Provação · golpes 0/3", hud.RoomLabel);
            Shot(cam, "docs/img/provacao.png");
            hp.TakeDamage(new DamageInfo(4f, ElementType.None, Team.Enemy));
            hp.TakeDamage(new DamageInfo(4f, ElementType.None, Team.Enemy));
            Assert.AreEqual(1, director.TrialHits, "dois golpes no mesmo instante contam um");
            for (int k = 0; k < 30; k++) yield return null;
            hp.TakeDamage(new DamageInfo(4f, ElementType.None, Team.Enemy));
            for (int k = 0; k < 30; k++) yield return null;
            hp.TakeDamage(new DamageInfo(4f, ElementType.None, Team.Enemy));
            Assert.AreEqual(3, director.TrialHits);
            int talents = director.Run.Talents.Count, essence = director.Run.Essence;
            GameEvents.RaiseRoomCleared();
            for (int k = 0; k < 120 && director.Run.Talents.Count == talents; k++) yield return null;
            Assert.AreEqual(true, director.LastTrialPassed);
            Assert.IsFalse(director.TrialActive);
            Assert.AreEqual(talents + 1, director.Run.Talents.Count, "cumprida, a Provacao da um talento extra");
            Assert.IsTrue(MetaStore.Current.Has(Feat.Trial), "o feito Provado e pago e gravado na hora");
            Assert.Contains(Feat.Trial, director.NewFeats);
            Assert.GreaterOrEqual(director.Run.Essence, essence + Trials.BonusEssence);
            StringAssert.Contains("cumprida", hud.RoomLabel);

            // Contra o relogio: o tempo estoura e a sala limpa nao paga o premio
            director.StartTrial(new MapNode { Id = IdFor(TrialKind.Clock), Type = RoomType.Trial }, 1);
            Assert.AreEqual(TrialKind.Clock, director.TrialKind);
            // tiques seguidos (poca a cada 0,4 s) sao um golpe so: a janela anda com cada dano
            for (int i = 0; i < 5; i++)
            {
                hp.TakeDamage(new DamageInfo(1f, ElementType.None, Team.Enemy));
                for (int k = 0; k < 12; k++) yield return null;
            }
            Assert.AreEqual(1, director.TrialHits, "cinco tiques seguidos contam um golpe");
            // ...mas um golpe de outra fonte no meio do fluxo conta (ficar na poca nao protege)
            var other = new GameObject("OutroAtacante");
            hp.TakeDamage(new DamageInfo(1f, ElementType.None, Team.Enemy));
            for (int k = 0; k < 24; k++) yield return null;
            hp.TakeDamage(new DamageInfo(1f, ElementType.None, Team.Enemy, other));
            Assert.AreEqual(2, director.TrialHits, "golpe de outro inimigo conta mesmo com a poca ferindo");
            Object.Destroy(other);
            Assert.AreEqual(Trials.TimeLimit(1), director.TrialLimit, 0.01f);
            for (int k = 0; k < 3000 && director.TrialElapsed <= director.TrialLimit; k++) yield return null;
            Assert.Greater(director.TrialElapsed, director.TrialLimit, "o relogio estourou");
            yield return null;
            StringAssert.Contains("perdida", hud.RoomLabel);
            talents = director.Run.Talents.Count;
            GameEvents.RaiseRoomCleared();
            for (int k = 0; k < 30; k++) yield return null;
            Assert.AreEqual(false, director.LastTrialPassed);
            Assert.AreEqual(talents, director.Run.Talents.Count, "perdida, sem talento extra");

            // Pes no Chao: um dash e ela se perde
            director.StartTrial(new MapNode { Id = IdFor(TrialKind.Grounded), Type = RoomType.Trial }, 2);
            Assert.AreEqual(TrialKind.Grounded, director.TrialKind);
            for (int k = 0; k < 5; k++) yield return null;
            StringAssert.Contains("sem dash", hud.RoomLabel);
            var heroGo = Object.FindFirstObjectByType<PlayerController>().gameObject;
            GameEvents.RaiseAttackPerformed(new AttackEvent { Kind = AttackKind.Dash, Source = heroGo, Origin = heroGo.transform.position, Direction = Vector3.forward });
            Assert.AreEqual(1, director.TrialDashes);
            for (int k = 0; k < 5; k++) yield return null;
            StringAssert.Contains("perdida", hud.RoomLabel);
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Stretch_Layouts_Are_Built_And_Walkable()
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
            var follow = cam.GetComponent<FollowCamera>();
            if (follow != null) follow.enabled = false;
            hud.HideTip();
            var path = new UnityEngine.AI.NavMeshPath();
            foreach (var (name, file, biome) in new[] { ("Forja", "forja", Biome.Ashes), ("Cisterna", "cisterna", Biome.Crypt), ("Canais", "canais", Biome.Crypt),
                ("Bosque de Pedra", "bosque", Biome.Garden), ("Pedreira", "pedreira", Biome.Garden) })
            {
                int index = director.LayoutIndex(name);
                Assert.GreaterOrEqual(index, 0, "falta o arranjo " + name);
                director.DressBiome(biome);
                director.ApplyLayout(index);
                hero.Teleport(new Vector3(0, 0.05f, -7.5f), Vector3.forward);
                for (int k = 0; k < 6; k++) yield return null;
                // da entrada da sala da para chegar ao fundo, onde ficam as portas, e aos dois cantos de cima
                foreach (var goal in new[] { new Vector3(0, 0, 8.5f), new Vector3(-12f, 0, 7.5f), new Vector3(12f, 0, 7.5f) })
                {
                    Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(goal, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas), name + ": chao perto de " + goal);
                    Assert.IsTrue(UnityEngine.AI.NavMesh.CalculatePath(hero.transform.position, hit.position, UnityEngine.AI.NavMesh.AllAreas, path), name);
                    Assert.AreEqual(UnityEngine.AI.NavMeshPathStatus.PathComplete, path.status, name + ": caminho ate " + goal);
                }
                cam.transform.position = new Vector3(0, 23f, -15f);
                cam.transform.LookAt(new Vector3(0, 0, 0.6f));
                hud.ShowBanner("", "", Color.clear, 0.01f);
                for (int k = 0; k < 3; k++) yield return null;
                Shot(cam, $"docs/img/arranjo_{file}.png");
            }
            if (follow != null) follow.enabled = true;
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Drowned_Leaves_A_Puddle_That_Soaks_And_Marks()
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
            director.DressBiome(Biome.Crypt);
            var hero = Object.FindFirstObjectByType<PlayerController>();
            var hp = hero.GetComponent<Health>();
            hp.Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, -3f), Vector3.forward);
            GameObject Load(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/" + n + ".prefab");
            var drowned = Object.Instantiate(Load("Afogado"), new Vector3(0, 0, 4f), Quaternion.Euler(0, 180, 0)).GetComponent<Health>();
            Assert.IsTrue(drowned.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Skeleton_Staff"), "segura o cajado do pack");
            // elemental de Agua: resiste a Agua, fraco a Terra
            float full = drowned.Current;
            drowned.TakeDamage(new DamageInfo(10f, ElementType.Earth, Team.Player, hero.gameObject));
            Assert.AreEqual(full - 16f, drowned.Current, 0.01f, "fraco a Terra");
            // joga o orbe e deixa a poca
            WaterPuddle puddle = null;
            for (int k = 0; k < 1200 && puddle == null; k++) { yield return null; puddle = Object.FindFirstObjectByType<WaterPuddle>(); }
            Assert.IsNotNull(puddle, "o orbe do Afogado deixa uma poca");
            Assert.AreEqual(ElementType.Water, puddle.Element);
            drowned.GetComponent<EnemyBrain>().enabled = false;
            // quem esta na poca: o heroi fica encharcado; o inimigo fica marcado com Agua
            hero.Teleport(puddle.transform.position + new Vector3(0, 0.05f, 0), Vector3.forward);
            var crawler = Object.Instantiate(Load("Rastejante"), puddle.transform.position + new Vector3(0.8f, 0, 0.4f), Quaternion.identity).GetComponent<Health>();
            crawler.GetComponent<EnemyBrain>().enabled = false;
            int pulses = puddle.Pulses;
            for (int k = 0; k < 60 && puddle != null && puddle.Pulses < pulses + 2; k++) yield return null;
            Assert.IsNotNull(puddle, "a poca dura alguns segundos");
            Assert.IsTrue(hero.GetComponent<StatusHolder>().State.Has(StatusType.Soaked), "o heroi na poca fica encharcado");
            Assert.AreEqual(ElementType.Water, crawler.GetComponent<ElementMark>().State.Element, "o inimigo na poca fica marcado com Agua");
            hero.Teleport(puddle.transform.position + new Vector3(0, 0.05f, -4.5f), Vector3.forward);
            hud.ShowBanner("", "", Color.clear, 0.01f);
            hud.HideTip();
            for (int k = 0; k < 4; k++) yield return null;
            Shot(cam, "docs/img/inimigo_afogado.png");
            {
                // de perto, para conferir o Afogado e a poca
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null) follow.enabled = false;
                drowned.transform.position = puddle.transform.position + new Vector3(-2.6f, 0, 1.2f);
                drowned.transform.rotation = Quaternion.LookRotation(Vector3.back + Vector3.right * 0.4f);
                var at = drowned.transform.position;
                float fov = cam.fieldOfView;
                cam.transform.position = at + new Vector3(2.6f, 3f, -5.4f);
                cam.transform.LookAt(at + new Vector3(1.2f, 0.8f, 0f));
                cam.fieldOfView = 40f;
                for (int k = 0; k < 3; k++) yield return null;
                Shot(cam, "docs/img/afogado_perto.png");
                cam.fieldOfView = fov;
                if (follow != null) follow.enabled = true;
            }
            // some sozinha
            for (int k = 0; k < 400 && puddle != null; k++) yield return null;
            Assert.IsTrue(puddle == null, "a poca seca");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Banner_Bearer_Rallies_Nearby_Enemies_Until_It_Falls()
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
            hero.Teleport(new Vector3(0, 0.05f, -5f), Vector3.forward);
            GameObject Load(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/" + n + ".prefab");
            var bearer = Object.Instantiate(Load("PortaEstandarte"), new Vector3(0, 0, 2f), Quaternion.Euler(0, 180, 0)).GetComponent<Health>();
            var near = Object.Instantiate(Load("Rastejante"), new Vector3(1.5f, 0, 1f), Quaternion.Euler(0, 180, 0)).GetComponent<EnemyBrain>();
            var far = Object.Instantiate(Load("Rastejante"), new Vector3(9f, 0, -4f), Quaternion.Euler(0, 180, 0)).GetComponent<EnemyBrain>();
            yield return null; yield return null; // o Start de cada um anuncia o nascimento (o circulo da aura vem dai)
            bearer.GetComponent<EnemyBrain>().enabled = false;
            near.enabled = false; far.enabled = false;
            Assert.IsTrue(bearer.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Banner"), "estandarte nas costas");
            Assert.IsNotNull(bearer.GetComponent<RallyAura>());
            for (int k = 0; k < 30; k++) yield return null;
            Assert.IsNotNull(bearer.transform.Find("AuraRing"), "o circulo da aura aparece no chao");
            Assert.IsTrue(near.Rallied, "quem esta no circulo e fortalecido");
            Assert.IsFalse(far.Rallied, "quem esta longe, nao");
            hud.ShowBanner("", "", Color.clear, 0.01f);
            hud.HideTip();
            Shot(cam, "docs/img/inimigo_estandarte.png");
            {
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null) follow.enabled = false;
                var at = bearer.transform.position;
                float fov = cam.fieldOfView;
                cam.transform.position = at + new Vector3(-3.2f, 3f, -5.2f);
                cam.transform.LookAt(at + Vector3.up * 1.4f);
                cam.fieldOfView = 40f;
                for (int k = 0; k < 3; k++) yield return null;
                Shot(cam, "docs/img/estandarte_perto.png");
                cam.fieldOfView = fov;
                if (follow != null) follow.enabled = true;
            }
            // derrubado, o bonus some logo e o circulo tambem
            bearer.TakeDamage(new DamageInfo(9999f, ElementType.None, Team.Player, hero.gameObject));
            // o abate dispara a camera lenta: em tempo de jogo o bonus ainda dura um pouco
            for (int k = 0; k < 3000 && near.Rallied; k++) yield return null;
            Assert.IsFalse(near.Rallied, "sem o estandarte, o bonus acaba");
            Assert.IsTrue(bearer == null || bearer.transform.Find("AuraRing") == null, "o circulo some");

            // Contagio (talento): o marcado que cai passa a marca para o vizinho mais perto
            hero.Modifiers.MarkSpread = 5f;
            yield return null; // o heroi publica o raio no quadro dele
            var marked = Object.Instantiate(Load("Rastejante"), near.transform.position + new Vector3(1.2f, 0, 0), Quaternion.identity).GetComponent<Health>();
            yield return null;
            marked.GetComponent<EnemyBrain>().enabled = false;
            marked.gameObject.AddComponent<ElementMark>().State.Hit(ElementType.Water, out _);
            int spreads = ElementMark.Spreads;
            marked.TakeDamage(new DamageInfo(9999f, ElementType.None, Team.Player, hero.gameObject));
            Assert.AreEqual(spreads + 1, ElementMark.Spreads, "a marca pulou");
            Assert.AreEqual(ElementType.Water, near.GetComponent<ElementMark>().State.Element, "o vizinho ficou marcado com Agua");
            hero.Modifiers.MarkSpread = 0f;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Siege_Keeps_Sending_Waves_Until_Time_Runs_Out()
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
            var spawner = Object.FindFirstObjectByType<WaveSpawner>();
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            int cleared = 0;
            GameEvents.RoomCleared += () => cleared++;
            // a sala da primeira camada vira um Cerco curto: as ondas voltam enquanto o tempo corre
            director.StartEncounter(RoomType.Combat, 1, EliteModifier.None);
            director.StartSiege(30f);
            Assert.IsTrue(director.SiegeActive);
            for (int k = 0; k < 120; k++) yield return null;
            StringAssert.Contains("Cerco", hud.RoomLabel);
            // matar quem nasce: a onda seguinte vem em vez de a sala limpar
            // (o abate dispara a camera lenta, entao conta ondas em vez de quadros)
            for (int k = 0; k < 6000 && spawner.State.Spawned < 3 && director.SiegeActive; k++)
            {
                if (k % 15 == 0)
                    foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
                        if (!e.GetComponent<Health>().IsDead) e.GetComponent<Health>().TakeDamage(new DamageInfo(99999f, ElementType.None, Team.Player, hero.gameObject));
                yield return null;
            }
            Assert.AreEqual(0, cleared, "no Cerco a sala nao limpa sozinha");
            Assert.GreaterOrEqual(spawner.State.Spawned, 3, "as ondas se repetem");
            hud.HideTip();
            Shot(cam, "docs/img/cerco.png");
            for (int k = 0; k < 3000 && director.SiegeActive; k++) yield return null;
            Assert.IsFalse(director.SiegeActive, "o tempo acaba");
            for (int k = 0; k < 60 && cleared == 0; k++) yield return null;
            Assert.AreEqual(1, cleared, "acabado o tempo, a sala limpa uma vez");
            for (int k = 0; k < 30; k++) yield return null;
            Assert.IsFalse(Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Any(e => !e.GetComponent<Health>().IsDead), "quem sobrou desmoronou");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Shade_Appears_Behind_The_Hero()
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
            var hp = hero.GetComponent<Health>();
            hp.Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, 0f), Vector3.forward);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Espectro.prefab");
            Assert.IsNotNull(prefab, "o prefab do Espectro existe");
            var shade = Object.Instantiate(prefab, new Vector3(0, 0, 7f), Quaternion.Euler(0, 180, 0)).GetComponent<EnemyBrain>();
            Assert.AreEqual(EnemyAttackKind.Ambush, shade.Definition.attack);
            int warned = 0;
            Vector3 warnedAt = default;
            GameEvents.TelegraphStarted += e => { if (e.Source == shade.gameObject) { warned++; warnedAt = e.Point; } };
            for (int k = 0; k < 1500 && shade.Ambushes == 0; k++) yield return null;
            Assert.Greater(warned, 0, "avisa antes de aparecer");
            Assert.Greater(shade.Ambushes, 0, "aparece no circulo avisado");
            var behind = warnedAt - hero.transform.position; behind.y = 0f;
            Assert.Less(behind.magnitude, 2.6f, "o circulo fica colado no heroi");
            Assert.Less(Vector3.Distance(shade.transform.position, warnedAt), 0.8f, "e ele surge ali");
            hud.ShowBanner("", "", Color.clear, 0.01f);
            hud.HideTip();
            Shot(cam, "docs/img/inimigo_espectro.png");
            GameFlow.StartElement = ElementType.Fire;
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Crypt_Elites_Bring_The_Priestess()
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
            Assert.AreEqual("Sacerdotisa", director.MiniBossFor(Biome.Crypt).name, "na Cripta, a Sacerdotisa");
            Assert.AreEqual("Carrasco", director.MiniBossFor(Biome.Ashes).name, "nos Saloes, o Carrasco");
            Assert.AreEqual("Colosso", director.MiniBossFor(Biome.Garden).name, "no Jardim, o Colosso de Pedra");
            // na run, a Elite sorteia entre os tres (com 10 camadas o mini-chefe so cairia no Jardim)
            var seenMini = new System.Collections.Generic.HashSet<string>();
            for (int node = 0; node < 30; node++) seenMini.Add(director.MiniBossForRoom(7, node).name);
            Assert.AreEqual(3, seenMini.Count, "os tres mini-chefes aparecem");
            StringAssert.Contains(director.MiniBossName(7, 0), "Carrasco Sacerdotisa Afogada Colosso de Pedra");
            var colossus = Object.Instantiate(director.MiniBossFor(Biome.Garden), new Vector3(6f, 0, 3f), Quaternion.Euler(0, 180, 0)).GetComponent<BossBrain>();
            Assert.AreEqual(ElementType.Earth, colossus.Definition.element);
            Assert.IsFalse(colossus.Definition.lastPhaseShield, "mini-chefe nao tem o escudo final");
            Assert.IsTrue(colossus.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Skeleton_Shield_Large_B"), "escudo e machado do pack");
            {
                colossus.enabled = false;
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null) follow.enabled = false;
                var at = colossus.transform.position;
                float fov = cam.fieldOfView;
                cam.transform.position = at + new Vector3(2.5f, 4f, -8.5f);
                cam.transform.LookAt(at + Vector3.up * 2f);
                cam.fieldOfView = 40f;
                hud.ShowBanner("", "", Color.clear, 0.01f);
                for (int k = 0; k < 4; k++) yield return null;
                Shot(cam, "docs/img/colosso.png");
                cam.fieldOfView = fov;
                if (follow != null) follow.enabled = true;
            }
            Object.Destroy(colossus.gameObject);
            yield return null;
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, -2f), Vector3.forward);
            director.ApplyLayout(-1);
            director.DressBiome(Biome.Crypt);
            string name = "";
            GameEvents.BossSpawned += (_, n) => name = n;
            var priestess = Object.Instantiate(director.MiniBossFor(Biome.Crypt), new Vector3(0, 0, 3f), Quaternion.Euler(0, 180, 0)).GetComponent<BossBrain>();
            for (int k = 0; k < 40; k++) yield return null;
            Assert.AreEqual("Sacerdotisa Afogada", name);
            Assert.AreEqual(ElementType.Water, priestess.Definition.element);
            for (int k = 0; k < 1500 && Object.FindFirstObjectByType<WaterPuddle>() == null; k++) yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<WaterPuddle>(), "as Erupcoes dela tambem deixam poca");
            hud.ShowBanner("", "", Color.clear, 0.01f);
            hud.HideTip();
            Shot(cam, "docs/img/sacerdotisa.png");
            var hp = priestess.GetComponent<Health>();
            hp.TakeDamage(new DamageInfo(hp.Max * 0.75f, ElementType.None, Team.Player, hero.gameObject));
            for (int k = 0; k < 30; k++) yield return null;
            Assert.AreEqual(2, priestess.Phase);
            Assert.IsFalse(priestess.Shield.Up, "mini-chefe: sem escudo na ultima fase");
            Object.Destroy(priestess.gameObject);
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Resonance_Crystal_Marks_The_Pack()
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
            director.DressBiome(Biome.Crypt);
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, -5f), Vector3.forward);
            Assert.AreEqual(2, director.PlaceCrystals(Biome.Crypt, 5, 2), "dois cristais na sala vazia");
            var crystal = Object.FindObjectsByType<ResonanceCrystal>(FindObjectsSortMode.None).First();
            Assert.AreEqual(ElementType.Water, crystal.Element, "na Cripta, cristal de Agua");
            GameObject Load(string n) => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/" + n + ".prefab");
            var a = Object.Instantiate(Load("Rastejante"), crystal.transform.position + new Vector3(1.5f, 0, 0.5f), Quaternion.identity).GetComponent<Health>();
            var b = Object.Instantiate(Load("Rastejante"), crystal.transform.position + new Vector3(-1.4f, 0, -0.8f), Quaternion.identity).GetComponent<Health>();
            yield return null; yield return null;
            a.GetComponent<EnemyBrain>().enabled = false; b.GetComponent<EnemyBrain>().enabled = false;
            for (int k = 0; k < 10; k++) yield return null;
            hud.HideTip();
            Shot(cam, "docs/img/cristal.png");
            // um golpe estoura o cristal: o bando em volta fica marcado com Agua e encharcado
            crystal.GetComponent<Health>().TakeDamage(new DamageInfo(5f, ElementType.None, Team.Player, hero.gameObject) { Direction = Vector3.forward });
            yield return null;
            Assert.AreEqual(2, crystal.Marked);
            foreach (var e in new[] { a, b })
            {
                Assert.AreEqual(ElementType.Water, e.GetComponent<ElementMark>().State.Element, "marcado com o elemento do trecho");
                Assert.IsTrue(e.GetComponent<StatusHolder>().State.Has(StatusType.Soaked), "e encharcado");
                Assert.Less(e.Current, e.Max, "o estouro fere um pouco");
            }
            for (int k = 0; k < 10; k++) yield return null;
            Assert.IsTrue(crystal == null, "o cristal some");

            // a mesma reacao em dois alvos lado a lado deixa um lamacal so (senao a skill seguinte faria duas Fusoes)
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<ReactionTable>("Assets/Game/Combat/Data/Reactions.asset");
            var c = Object.Instantiate(Load("Rastejante"), new Vector3(3f, 0, 2f), Quaternion.identity).GetComponent<Health>();
            var d = Object.Instantiate(Load("Rastejante"), new Vector3(4f, 0, 2f), Quaternion.identity).GetComponent<Health>();
            yield return null; yield return null;
            c.GetComponent<EnemyBrain>().enabled = false; d.GetComponent<EnemyBrain>().enabled = false;
            foreach (var h in new[] { c, d })
            {
                ElementCombat.Deliver(h, new DamageInfo(1f, ElementType.Water, Team.Player, hero.gameObject), table);
                ElementCombat.Deliver(h, new DamageInfo(1f, ElementType.Earth, Team.Player, hero.gameObject), table);
            }
            Assert.AreEqual(1, DamageZone.Active.Count(z => z != null && z.Fusable && ((z.PairA == ElementType.Water && z.PairB == ElementType.Earth) || (z.PairA == ElementType.Earth && z.PairB == ElementType.Water))),
                "um lamacal so para os dois");
            GameFlow.Layers = 8; GameFlow.Shrines = 3;
            Time.captureFramerate = 0;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator Perfect_Dodge_Charges_Quintessence_And_Crits()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Air;
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
            var hp = hero.GetComponent<Health>();
            hero.Teleport(new Vector3(0, 0.05f, -3f), Vector3.forward);
            var scripted = hero.gameObject.AddComponent<ScriptedInput>();
            hero.GetComponent<PlayerInputReader>().enabled = false;
            hero.Input = scripted;
            scripted.AimPoint = hero.transform.position + Vector3.forward * 3f;
            var attacker = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Rastejante.prefab");
            var foe = Object.Instantiate(attacker, new Vector3(0, 0, 2.5f), Quaternion.Euler(0, 180, 0)).GetComponent<Health>();
            yield return null; yield return null;
            foe.GetComponent<EnemyBrain>().enabled = false;
            // fora do dash o golpe entra normal
            float life = hp.Current;
            hp.TakeDamage(new DamageInfo(5f, ElementType.None, Team.Enemy, foe.gameObject));
            Assert.Less(hp.Current, life);
            Assert.AreEqual(0, hero.PerfectDodges);
            // no meio do dash: esquiva perfeita
            float meter = hero.Quintessence.Meter;
            scripted.PressDash();
            for (int k = 0; k < 30 && !hero.IsDashing; k++) yield return null;
            Assert.IsTrue(hero.IsDashing);
            life = hp.Current;
            hp.TakeDamage(new DamageInfo(12f, ElementType.None, Team.Enemy, foe.gameObject));
            hp.TakeDamage(new DamageInfo(12f, ElementType.None, Team.Enemy, foe.gameObject));
            Assert.GreaterOrEqual(hp.Current, life, "o dash atravessa o golpe (nenhum dano entra)");
            Assert.AreEqual(1, hero.PerfectDodges, "dois golpes no mesmo instante contam uma esquiva");
            Assert.AreEqual(meter + PlayerController.PerfectDodgeCharge, hero.Quintessence.Meter, 0.01f, "enche a Quintessencia");
            Assert.IsTrue(hero.PerfectCritReady);
            Assert.Greater(hp.Current, life, "Vey: a esquiva perfeita cura (Passo de Brisa)");
            for (int k = 0; k < 30 && hero.IsDashing; k++) yield return null;
            // o proximo golpe basico e critico
            bool crit = false;
            GameEvents.DamageDealt += (t, info, applied) => { if (t == foe && info.IsCrit) crit = true; };
            hero.Teleport(new Vector3(0, 0.05f, 0.8f), Vector3.forward);
            scripted.AimPoint = foe.transform.position;
            scripted.AttackHeld = true;
            for (int k = 0; k < 300 && !crit; k++) yield return null;
            scripted.AttackHeld = false;
            Assert.IsTrue(crit, "o golpe depois da esquiva perfeita e critico");
            Assert.IsFalse(hero.PerfectCritReady, "e so um");
            // perigo de chao (poca, respiradouro) atravessado no dash nao conta: nao da para encher a barra na sala vazia
            for (int k = 0; k < 40; k++) yield return null;
            int dodges = hero.PerfectDodges;
            scripted.PressDash();
            for (int k = 0; k < 30 && !hero.IsDashing; k++) yield return null;
            var hazard = new GameObject("PocaDeTeste");
            hp.TakeDamage(new DamageInfo(2f, ElementType.None, Team.Enemy, hazard) { IsReaction = true });
            Object.Destroy(hazard);
            Assert.AreEqual(dodges, hero.PerfectDodges, "perigo de chao nao e esquiva perfeita");
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
        public IEnumerator Drowned_Queen_Floods_The_Arena()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            GameFlow.StartElement = ElementType.Earth;
            GameFlow.Seed = 7; GameFlow.Layers = 5; GameFlow.Shrines = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            var hud = Object.FindFirstObjectByType<Hud>();
            cam = Camera.main;
            hud.UseCamera(cam);
            Object.FindFirstObjectByType<WaveSpawner>().enabled = false;
            for (int k = 0; k < 2000 && director.Busy; k++) yield return null;
            Assert.IsTrue(director.QueenRun, "semente 7 (resto 1 por 6): o chefe final e a Rainha");
            Assert.IsFalse(director.WeaverRun);
            var hero = Object.FindFirstObjectByType<PlayerController>();
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            hero.Teleport(new Vector3(0, 0.05f, -2f), Vector3.forward);
            director.ApplyLayout(-1);
            director.DressBiome(Biome.Crypt); // a sala final da Rainha fica com a luz da Cripta
            string name = "";
            GameEvents.BossSpawned += (_, n) => name = n;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Rainha.prefab");
            Assert.IsNotNull(prefab, "o prefab da Rainha existe");
            var queen = Object.Instantiate(prefab, new Vector3(0, 0, 3f), Quaternion.Euler(0, 180, 0)).GetComponent<BossBrain>();
            for (int k = 0; k < 40; k++) yield return null;
            Assert.AreEqual("Rainha Afogada", name);
            Assert.AreEqual(ElementType.Water, queen.Definition.element);
            Assert.IsTrue(queen.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Skeleton_Staff"), "segura o cajado do pack");
            // elemental de Agua: fraca a Terra
            var hp = queen.GetComponent<Health>();
            float full = hp.Current;
            hp.TakeDamage(new DamageInfo(10f, ElementType.Earth, Team.Player, hero.gameObject));
            Assert.AreEqual(full - 16f, hp.Current, 0.01f, "fraca a Terra");
            // a Erupcao dela deixa pocas d'agua
            for (int k = 0; k < 1500 && Object.FindFirstObjectByType<WaterPuddle>() == null; k++) yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<WaterPuddle>(), "a Erupcao da Rainha deixa poca");
            hud.ShowBanner("", "", Color.clear, 0.01f);
            hud.HideTip();
            for (int k = 0; k < 6; k++) yield return null;
            Shot(cam, "docs/img/chefe_rainha.png");
            {
                // de perto, de frente
                queen.enabled = false;
                queen.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(hero.transform.position + new Vector3(0.5f, 0, 5f));
                queen.transform.rotation = Quaternion.Euler(0, 190, 0);
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null) follow.enabled = false;
                var at = queen.transform.position;
                float fov = cam.fieldOfView;
                cam.transform.position = at + new Vector3(3f, 5.5f, -12f);
                cam.transform.LookAt(at + Vector3.up * 2.6f);
                cam.fieldOfView = 40f;
                for (int k = 0; k < 4; k++) yield return null;
                Shot(cam, "docs/img/rainha_perto.png");
                cam.fieldOfView = fov;
                if (follow != null) follow.enabled = true;
                queen.enabled = true;
            }
            hp.TakeDamage(new DamageInfo(hp.Max * 0.75f, ElementType.None, Team.Player, hero.gameObject));
            for (int k = 0; k < 30; k++) yield return null;
            Assert.AreEqual(2, queen.Phase);
            Assert.IsTrue(queen.Shield.Up, "chefe final: a ultima fase levanta o escudo");
            Object.Destroy(queen.gameObject);
            GameFlow.StartElement = ElementType.Fire;
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
            Assert.AreEqual(0, director.MasteryPicksLeft, "retomada depois da primeira sala, nenhuma escolha conta como da maestria");
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
            // e marca os inimigos dentro com o elemento do trecho: uma skill de outro elemento detona
            Assert.AreEqual(ElementType.Fire, pool.Element, "nos Saloes a poca marca com Fogo");
            Assert.IsFalse(hero.TryGetComponent(out ElementMark heroMark) && heroMark.State.HasMark, "o heroi nao e marcado pela poca");
            var meleePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Enemies/Prefabs/Rastejante.prefab");
            var soaked = Object.Instantiate(meleePrefab, new Vector3(1.2f, 0, 0.2f), Quaternion.identity).GetComponent<Health>();
            var keeps = Object.Instantiate(meleePrefab, new Vector3(-1.2f, 0, 0.2f), Quaternion.identity).GetComponent<Health>();
            soaked.GetComponent<EnemyBrain>().enabled = false;
            keeps.GetComponent<EnemyBrain>().enabled = false;
            keeps.gameObject.AddComponent<ElementMark>().State.Hit(ElementType.Earth, out _);
            hero.Teleport(new Vector3(0, 0.05f, -4f), Vector3.forward);
            int pulses = pool.Pulses;
            for (int k = 0; k < 90 && pool.Pulses < pulses + 2; k++) yield return null;
            Assert.AreEqual(ElementType.Fire, soaked.GetComponent<ElementMark>().State.Element, "inimigo na poca fica marcado com Fogo");
            Assert.AreEqual(ElementType.Earth, keeps.GetComponent<ElementMark>().State.Element, "a marca que o heroi pos nao e trocada");
            Assert.Greater(pool.Marks, 0);
            Assert.AreEqual(Tips.Text("terrain"), hud.LastTip, "a primeira marca da poca traz a dica");
            string reaction = null;
            void OnReaction(ReactionEvent e) { if (e.Target == soaked.gameObject) reaction = e.Name; }
            GameEvents.ReactionTriggered += OnReaction;
            var table = UnityEditor.AssetDatabase.LoadAssetAtPath<ReactionTable>("Assets/Game/Combat/Data/Reactions.asset");
            ElementCombat.Deliver(soaked, new DamageInfo(4f, ElementType.Water, Team.Player, hero.gameObject), table);
            GameEvents.ReactionTriggered -= OnReaction;
            Assert.IsNotNull(reaction, "Agua num inimigo marcado pela poca de Fogo vira reacao");
            Object.Destroy(soaked.gameObject);
            Object.Destroy(keeps.gameObject);
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

            // primeira sala (profundidade 0): vida da ficha, dano do aquecimento
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            var early = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            Assert.Greater(early.Length, 0, "a primeira sala deveria ter inimigos");
            foreach (var e in early)
            {
                Assert.AreEqual(e.Definition.maxHealth, e.GetComponent<Health>().Max, 0.01f, "sala 1: a vida da ficha");
                Assert.AreEqual(DifficultyCurve.EarlyDamage(0), e.Scaling.Damage, 0.001f, "sala 1: aquecimento, bate mais fraco");
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

            // Divisores: quem cai vira dois Cinzentos, que contam para a onda e nao se dividem de novo
            foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
            foreach (var b in Object.FindObjectsByType<VolatileBurst>(FindObjectsSortMode.None)) Object.Destroy(b.gameObject);
            yield return null;
            director.StartEncounter(RoomType.Elite, 4, EliteModifier.Splitting);
            for (int k = 0; k < 90 && Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length == 0; k++) yield return null;
            for (int k = 0; k < 25; k++) yield return null;
            var splitSpawner = Object.FindFirstObjectByType<WaveSpawner>();
            var splitter = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).First(e => e.Definition.attack != EnemyAttackKind.Explode && e.GetComponent<Champion>() == null && e.Definition.displayName != "Cinzento");
            Assert.IsTrue(splitter.Scaling.Splits);
            int aliveBefore = splitSpawner.State.Alive;
            splitter.GetComponent<Health>().TakeDamage(new DamageInfo(9999f, ElementType.None, Team.Player));
            yield return null;
            Assert.AreEqual(2, splitSpawner.Splits, "o Divisor vira dois");
            Assert.AreEqual(aliveBefore + 1, splitSpawner.State.Alive, "a onda conta os dois (um caiu, dois nasceram)");
            var babies = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e => e.Definition.displayName == "Cinzento" && !e.GetComponent<Health>().IsDead).ToList();
            Assert.GreaterOrEqual(babies.Count, 2);
            Assert.IsFalse(babies.Any(b => b.Scaling.Splits), "os Cinzentos nao se dividem de novo");
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
            Assert.AreEqual(Mastery.RunXp(director.Run.RoomsCleared, director.Run.Kills, false), director.MasteryGained);
            Assert.AreEqual(director.MasteryGained, MetaStore.Current.HeroXp(GameFlow.StartElement), "a derrota tambem soma maestria ao heroi");
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
