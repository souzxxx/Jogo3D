#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Quintessence.Core;
using Quintessence.Run;
using Quintessence.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Game.PlayTests
{
    /// Fotografa o titulo em varias proporcoes de tela (a aba Game do editor raramente fica em 16:9).
    public class AspectShots
    {
        [TearDown]
        public void Restore()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey(MetaStore.DefaultKey);
            GameFlow.Seed = 1; GameFlow.StartElement = ElementType.Fire;
            InputMode.Gamepad = false;
            Tips.UseKey(Tips.DefaultKey);
            HealthPool.ForceEvade = null;
        }

        /// Jogando de controle, o HUD e as dicas mostram os botoes do controle no lugar das teclas.
        [UnityTest, Timeout(300000)]
        public IEnumerator Gamepad_Shows_Pad_Buttons()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            Tips.UseKey("quintessence.meta.test.tips");
            Tips.ResetAll();
            GameEvents.Clear();
            GameFlow.Seed = 7; GameFlow.StartElement = ElementType.Water;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Object.FindFirstObjectByType<Quintessence.Combat.PlayerInputReader>().enabled = false; // sem ele, ninguem desliga o modo controle
            InputMode.Gamepad = true;
            var director = Object.FindFirstObjectByType<RunDirector>();
            for (float t = 0; t < 10f && director.Busy; t += Time.unscaledDeltaTime) yield return null;
            for (int k = 0; k < 5; k++) yield return null;
            var hud = Object.FindFirstObjectByType<Hud>();
            hud.UseCamera(Camera.main);
            var labels = hud.GetComponentsInChildren<UnityEngine.UI.Text>(true).Select(t => t.text).ToList();
            Assert.Contains("A", labels, "o dash mostra o botao A");
            Assert.Contains("DIREITA", labels, "a skill de Agua mostra o direcional");
            StringAssert.Contains("direcional para a direita", hud.LastTip, "a dica inicial fala do controle");
            yield return Shoot(Camera.main, "docs/img/s6_controle.png", new Vector2Int(960, 540));
            // no controle, levar golpe vibra forte e curto
            var pc = Object.FindFirstObjectByType<Quintessence.Combat.PlayerController>();
            var rumble = pc.GetComponent<Quintessence.Combat.PadRumble>();
            Assert.IsNotNull(rumble, "o heroi tem a vibracao do controle");
            HealthPool.ForceEvade = false;
            bool shake = FxPrefs.ScreenShake;
            FxPrefs.ScreenShake = false;
            pc.GetComponent<Health>().TakeDamage(new DamageInfo(5f, ElementType.None, Team.Enemy));
            Assert.AreEqual(0f, rumble.Last.x, "com Tremor e vibracao desligado, nada vibra");
            FxPrefs.ScreenShake = true;
            pc.GetComponent<Health>().TakeDamage(new DamageInfo(5f, ElementType.None, Team.Enemy));
            FxPrefs.ScreenShake = shake;
            Assert.Greater(rumble.Last.x, 0.4f, "golpe no heroi pede vibracao forte");
            Tips.ResetAll();
        }

        public static readonly Vector2Int[] Sizes =
        {
            new(1280, 720), new(1280, 800), new(1024, 768), new(1600, 680), new(1720, 720), new(900, 700), new(1280, 1024),
        };

        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator Title_In_Many_Aspects()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Title.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var menu = Object.FindFirstObjectByType<TitleMenu>();
            Assert.IsNotNull(menu);
            menu.UseCamera(Camera.main);
            for (float t = 0; t < 2.5f; t += Time.deltaTime) yield return null; // as particulas dos pedestais precisam de um tempo para encher
            Directory.CreateDirectory("Logs/aspect");
            foreach (var s in Sizes) yield return Shoot(Camera.main, $"Logs/aspect/title_{s.x}x{s.y}.png", s);
            MetaStore.UseKey(MetaStore.DefaultKey);
        }

        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator Run_Hud_In_Many_Aspects()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            GameEvents.Clear();
            GameFlow.Seed = 7; GameFlow.StartElement = ElementType.Water;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            for (float t = 0; t < 10f && Object.FindFirstObjectByType<RunDirector>().Busy; t += Time.unscaledDeltaTime) yield return null; // cartao de capitulo: tela preta
            for (int i = 0; i < 20; i++) yield return null;
            var hud = Object.FindFirstObjectByType<Hud>();
            var director = Object.FindFirstObjectByType<RunDirector>();
            hud.UseCamera(Camera.main);
            Directory.CreateDirectory("Logs/aspect");
            foreach (var s in new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 680), new Vector2Int(1024, 768) })
            {
                yield return Shoot(Camera.main, $"Logs/aspect/run_{s.x}x{s.y}.png", s);
                hud.ShowInventory(true, director.Run);
                yield return Shoot(Camera.main, $"Logs/aspect/inv_{s.x}x{s.y}.png", s);
                hud.ShowInventory(false);
                hud.ShowMap(director.Run, true);
                yield return Shoot(Camera.main, $"Logs/aspect/map_{s.x}x{s.y}.png", s);
                hud.ShowMap(director.Run, false);
            }
            // combate parado: os inimigos chegam e avisam o golpe (aneis, avisos e rachaduras no quadro)
            var pc = Object.FindFirstObjectByType<Quintessence.Combat.PlayerController>();
            pc.GetComponent<Quintessence.Combat.PlayerInputReader>().enabled = false;
            var scripted = pc.gameObject.AddComponent<Quintessence.Combat.ScriptedInput>();
            pc.Input = scripted;
            scripted.AimPoint = pc.transform.position + new Vector3(5f, 0f, -2f); // mira de lado: seta e anel no chao
            var hp = pc.GetComponent<Health>();
            hp.Pool.ExtraDamageTakenMultiplier = 0f;
            for (int k = 0; k < 4; k++)
            {
                for (float t = 0; t < 2.2f; t += Time.deltaTime) yield return null;
                yield return Shoot(Camera.main, $"Logs/aspect/combat_{k}.png", new Vector2Int(1280, 720));
            }
            MetaStore.UseKey(MetaStore.DefaultKey);
        }

        /// Paineis com mais texto (talentos, pausa, resultado e as folhas do titulo) em tres proporcoes.
        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator Panels_In_Many_Aspects()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            GameEvents.Clear();
            Directory.CreateDirectory("Logs/aspect");
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1600, 680), new Vector2Int(1024, 768) };
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Title.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var menu = Object.FindFirstObjectByType<TitleMenu>();
            menu.UseCamera(Camera.main);
            var sheets = new (string name, System.Action open)[]
            {
                ("opcoes", menu.ShowOptions), ("grimorio", menu.ShowGrimoire), ("feitos", menu.ShowFeats), ("creditos", menu.ShowCredits),
                ("refugio", () => menu.ShowRefuge(true)),
            };
            foreach (var (name, open) in sheets)
            {
                open();
                yield return null;
                foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_{name}_{s.x}x{s.y}.png", s);
                menu.CloseOverlay();
                yield return null;
            }

            GameFlow.Seed = 7; GameFlow.StartElement = ElementType.Earth;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            for (float t = 0; t < 10f && Object.FindFirstObjectByType<RunDirector>().Busy; t += Time.unscaledDeltaTime) yield return null; // cartao de capitulo: tela preta
            for (int i = 0; i < 20; i++) yield return null;
            var hud = Object.FindFirstObjectByType<Hud>();
            var director = Object.FindFirstObjectByType<RunDirector>();
            hud.UseCamera(Camera.main);
            var all = new System.Collections.Generic.List<ElementType>(Elements.All);
            var offer = Quintessence.Run.TalentCatalog.Offer(director.Run.Talents, all, new System.Random(3), 4, ElementType.Earth);
            hud.ShowLevelUp(3, offer, _ => { }, 2, () => { });
            foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_talentos_{s.x}x{s.y}.png", s);
            hud.ShowLevelUp(0, null, null);
            hud.ShowPause(true, () => { }, () => { }, () => { });
            foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_pausa_{s.x}x{s.y}.png", s);
            yield return Shoot(Camera.main, "docs/img/s6_pausa.png", new Vector2Int(1280, 720));
            GameObject.Find("Controls").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameObject.Find("ControlsSheet"), "o botao Controles abre a tabela");
            foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_controles_{s.x}x{s.y}.png", s);
            yield return Shoot(Camera.main, "docs/img/s6_controles.png", new Vector2Int(1280, 720));
            hud.ShowPause(false);
            hud.ShowResult(false, "Brann   ·   Ascensão 2   ·   derrubado por Rainha Afogada (golpe de maré)",
                new[] { ("9/10", "salas vencidas"), ("84", "inimigos derrotados"), ("14:32", "tempo"), ("9", "nível") },
                "12 talentos, 7 bênçãos, 4 de 4 elementos, 140 de Essência"
                + "\nBuild: Sintonia de Terra II · 6 Fusões · 31 Cadências · 4 esquivas perfeitas · Pele de Pedra"
                + "\n+42 Fragmentos para o Refúgio (total: 188)   ·   Brann +60 de maestria"
                + "\nMaestria 3 de Brann: começa a run com um talento à escolha"
                + "\nFeitos: Mestre das Fusões, Intocável (+20 Fragmentos)", () => { }, () => { },
                "Tão perto do Coração. Na próxima descida, ele é seu.");
            for (int i = 0; i < 40; i++) yield return null;
            foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_resultado_{s.x}x{s.y}.png", s);
            yield return Shoot(Camera.main, "docs/img/s6_resultado.png", new Vector2Int(1280, 720));
            MetaStore.UseKey(MetaStore.DefaultKey);
        }

        /// Capa da pagina do itch.io: titulo sem run salva, desenhado em 1260 x 1000 em Logs/; docs/img/capa_itch.png e ela reduzida para 630 x 500.
        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator Itch_Cover()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            RunSaveStore.UseKey("quintessence.meta.test.run");
            RunSaveStore.Clear();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Title.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            var menu = Object.FindFirstObjectByType<TitleMenu>();
            menu.UseCamera(Camera.main);
            for (float t = 0; t < 2.5f; t += Time.deltaTime) yield return null;
            yield return Shoot(Camera.main, "Logs/capa_itch_grande.png", new Vector2Int(1260, 1000));
            RunSaveStore.UseKey(RunSaveStore.DefaultKey);
            MetaStore.UseKey(MetaStore.DefaultKey);
        }

        /// Volta pelas salas sem luta (Loja, Santuario, Mestre, Tesouro, Pacto, Descanso) e pela sala do chefe: uma foto de cada
        /// em Logs/aspect/sala_*.png, para revisar o visual de cada uma.
        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator Room_Tour()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            Tips.UseKey("quintessence.meta.test.tips");
            GameEvents.Clear();
            GameFlow.Seed = 7; GameFlow.StartElement = ElementType.Fire;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            for (float t = 0; t < 10f && director.Busy; t += Time.unscaledDeltaTime) yield return null;
            Object.FindFirstObjectByType<Quintessence.Enemies.WaveSpawner>().enabled = false;
            foreach (var e in Object.FindObjectsByType<Quintessence.Enemies.EnemyBrain>(FindObjectsSortMode.None)) Object.Destroy(e.gameObject);
            var hud = Object.FindFirstObjectByType<Hud>();
            hud.UseCamera(Camera.main);
            var pc = Object.FindFirstObjectByType<Quintessence.Combat.PlayerController>();
            pc.GetComponent<Health>().Pool.ExtraDamageTakenMultiplier = 0f;
            Directory.CreateDirectory("Logs/aspect");
            director.Run.AddEssence(60);
            foreach (var type in new[] { RoomType.Shop, RoomType.Shrine, RoomType.Master, RoomType.Treasure, RoomType.Pact, RoomType.Rest })
            {
                director.ShowRoom(type);
                pc.Teleport(director.EntryPoint + new Vector3(0f, 0f, 2.5f), Vector3.forward);
                for (float t = 0; t < 1.6f; t += Time.unscaledDeltaTime) yield return null;
                yield return Shoot(Camera.main, $"Logs/aspect/sala_{type}.png", new Vector2Int(1280, 720));
                if (type == RoomType.Treasure)
                {
                    director.Interactables.First(i => i.Prompt.StartsWith("abrir")).Use();
                    for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime) yield return null;
                    yield return Shoot(Camera.main, "Logs/aspect/sala_Treasure_aberto.png", new Vector2Int(1280, 720));
                }
            }
            MetaStore.UseKey(MetaStore.DefaultKey);
        }

        /// Prints para a pagina do itch.io: o heroi lutando com duas skills (marca e reacao) contra a primeira sala,
        /// uma foto a cada 1,2 s em Logs/itch/luta_*.png (1280 x 720); escolher as melhores a mao.
        [UnityTest, Explicit, Timeout(300000)]
        public IEnumerator Itch_Action_Shots()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            Tips.UseKey("quintessence.meta.test.tips");
            GameEvents.Clear();
            GameFlow.Seed = 11; GameFlow.StartElement = ElementType.Fire;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var director = Object.FindFirstObjectByType<RunDirector>();
            for (float t = 0; t < 10f && director.Busy; t += Time.unscaledDeltaTime) yield return null;
            var hud = Object.FindFirstObjectByType<Hud>();
            hud.UseCamera(Camera.main);
            var pc = Object.FindFirstObjectByType<Quintessence.Combat.PlayerController>();
            pc.GetComponent<Quintessence.Combat.PlayerInputReader>().enabled = false;
            var input = pc.gameObject.AddComponent<Quintessence.Combat.ScriptedInput>();
            pc.Input = input;
            pc.Awaken(ElementType.Water);
            pc.GetComponent<Health>().Pool.ExtraDamageTakenMultiplier = 0.1f;
            Directory.CreateDirectory("Logs/itch");
            int shot = 0;
            for (float t = 0; t < 26f && shot < 18; t += Time.deltaTime)
            {
                var foe = Object.FindObjectsByType<Quintessence.Enemies.EnemyBrain>(FindObjectsSortMode.None)
                    .OrderBy(e => (e.transform.position - pc.transform.position).sqrMagnitude).FirstOrDefault();
                if (foe != null)
                {
                    var to = foe.transform.position - pc.transform.position; to.y = 0f;
                    input.AimPoint = foe.transform.position;
                    input.Move = to.magnitude > 2.4f ? new Vector2(to.x, to.z).normalized : Vector2.zero;
                    input.AttackHeld = to.magnitude < 3.4f;
                    if (to.magnitude < 5f && Random.value < 0.02f) input.PressElementSkill(Random.value < 0.5f ? 1 : 2);
                }
                if (t > 2.5f + shot * 1.2f) { yield return Shoot(Camera.main, $"Logs/itch/luta_{shot:00}.png", new Vector2Int(1280, 720)); shot++; }
                yield return null;
            }
            MetaStore.UseKey(MetaStore.DefaultKey);
        }

        /// Golpe no heroi acende a borda vermelho-escura; com pouca vida ela fica pulsando. Foto para a pagina.
        [UnityTest, Timeout(300000)]
        public IEnumerator Hurt_Edge_Flashes_On_Hit_And_Pulses_On_Low_Health()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            MetaStore.UseKey("quintessence.meta.test");
            GameEvents.Clear();
            GameFlow.Seed = 7; GameFlow.StartElement = ElementType.Earth;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
            var director = Object.FindFirstObjectByType<RunDirector>();
            for (float t = 0; t < 10f && director.Busy; t += Time.unscaledDeltaTime) yield return null;
            Object.FindFirstObjectByType<Quintessence.Enemies.WaveSpawner>().enabled = false;
            var hud = Object.FindFirstObjectByType<Hud>();
            hud.UseCamera(Camera.main);
            var hp = Object.FindFirstObjectByType<Quintessence.Combat.PlayerController>().GetComponent<Health>();
            for (int k = 0; k < 5; k++) yield return null;
            Assert.Less(hud.HurtEdgeAlpha, 0.01f, "sem dano, a borda fica apagada");
            // texto que nasce no alto da tela desce para dentro dela (nao corta na borda nem cobre a barra do chefe)
            var high = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.99f, 20f));
            foreach (var word in new[] { "FASE 3", "ESCUDO!", "ALTO" }) Quintessence.Feel.DamageNumber.Spawn(high, word, Color.white);
            for (int k = 0; k < 3; k++) yield return null;
            var ys = Object.FindObjectsByType<Quintessence.Feel.DamageNumber>(FindObjectsSortMode.None)
                .Select(dn => Camera.main.WorldToViewportPoint(dn.transform.position).y).OrderBy(y => y).ToList();
            Assert.AreEqual(3, ys.Count);
            Assert.LessOrEqual(ys[2], Quintessence.Feel.DamageNumber.TopLimit + 0.01f, "texto flutuante dentro da tela");
            Assert.Greater(ys[1] - ys[0], 0.02f, "os do mesmo instante empilham para baixo em vez de se sobrepor");
            Assert.Greater(ys[2] - ys[1], 0.02f, "os do mesmo instante empilham para baixo em vez de se sobrepor");
            // no climax nunca passa de MaxLive textos no ar
            for (int k = 0; k < 20; k++) Quintessence.Feel.DamageNumber.Spawn(Camera.main.transform.position + Camera.main.transform.forward * 20f, "VAPOR!", Color.white);
            Assert.LessOrEqual(Quintessence.Feel.DamageNumber.LiveCount, Quintessence.Feel.DamageNumber.MaxLive, "textos flutuantes limitados");
            HealthPool.ForceEvade = false; // sem esquiva sorteada: o golpe tem que entrar
            float before = hp.Current;
            hp.TakeDamage(new DamageInfo(hp.Max * 0.15f, ElementType.None, Team.Enemy));
            Assert.Less(hp.Current, before, "o golpe deveria tirar vida");
            float peak = 0f;
            for (int k = 0; k < 4; k++) { yield return null; peak = Mathf.Max(peak, hud.HurtEdgeAlpha); }
            Assert.Greater(peak, 0.2f, "o golpe acende a borda");
            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime) yield return null;
            Assert.Less(hud.HurtEdgeAlpha, 0.05f, "e ela apaga logo");
            hp.TakeDamage(new DamageInfo(hp.Current - hp.Max * 0.12f, ElementType.None, Team.Enemy));
            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime) yield return null;
            Assert.Greater(hud.HurtEdgeAlpha, 0.05f, "com pouca vida a borda continua acesa");
            Assert.Greater(hud.Heartbeats, 0, "e o coracao bate");
            Directory.CreateDirectory("Logs/aspect");
            yield return Shoot(Camera.main, "docs/img/s6_vida_baixa.png", new Vector2Int(960, 540));
            HealthPool.ForceEvade = null;
            MetaStore.UseKey(MetaStore.DefaultKey);
        }

        public static IEnumerator Shoot(Camera c, string path, Vector2Int s)
        {
            var rt = new RenderTexture(s.x, s.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            c.targetTexture = rt;
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            c.Render();
            var tex = new Texture2D(s.x, s.y, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, s.x, s.y), 0, 0);
            RenderTexture.active = null;
            c.targetTexture = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex); rt.Release(); Object.Destroy(rt);
        }
    }
}
#endif
            // numero que sobe ate o teto nao fica escrito por cima da palavra parada ali (MAGMA! sobre o chefe no fundo)
            for (int k = 0; k < 90; k++) yield return null; // os de cima somem
            var wordAt = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.97f, 20f));
            Quintessence.Feel.DamageNumber.Spawn(wordAt, "MAGMA!", Color.white);
            int fades = Quintessence.Feel.DamageNumber.CeilingFades;
            var below = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.74f, 20f));
            Quintessence.Feel.DamageNumber.Spawn(below, "9", Color.white);
            for (float t = 0; t < 1.2f && Quintessence.Feel.DamageNumber.CeilingFades == fades; t += Time.unscaledDeltaTime) yield return null;
            Assert.Greater(Quintessence.Feel.DamageNumber.CeilingFades, fades, "o numero que encosta na palavra parada no teto some");
