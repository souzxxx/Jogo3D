#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using Quintessence.Core;
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
            for (int i = 0; i < 90; i++) yield return null;
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
            for (int i = 0; i < 30; i++) yield return null;
            var hud = Object.FindFirstObjectByType<Hud>();
            var director = Object.FindFirstObjectByType<RunDirector>();
            hud.UseCamera(Camera.main);
            var all = new System.Collections.Generic.List<ElementType>(Elements.All);
            var offer = Quintessence.Run.TalentCatalog.Offer(director.Run.Talents, all, new System.Random(3), 4, ElementType.Earth);
            hud.ShowLevelUp(3, offer, _ => { }, 2, () => { });
            foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_talentos_{s.x}x{s.y}.png", s);
            hud.ShowLevelUp(0, null, null);
            hud.ShowPause(true, () => { }, () => { });
            foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_pausa_{s.x}x{s.y}.png", s);
            hud.ShowPause(false);
            string stats = "Ascensão 2   ·   Brann   ·   Nível 9   ·   84 inimigos derrotados   ·   12 talentos e 7 bênçãos\nDerrubado por: Rainha Afogada (golpe de maré)"
                + "\nSalas vencidas: 9   ·   Essência: 140   ·   Elementos: 4/4   ·   Tempo: 14:32"
                + "\nBuild: Sintonia de Terra 2 · Fusões 6 · Cadências 31 · esquivas perfeitas 4 · Pele de Pedra"
                + "\n+42 Fragmentos para o Refúgio (total: 188)   ·   Brann +60 de maestria"
                + "\nMaestria 3 de Brann: começa a run com um talento à escolha"
                + "\nFeitos: Mestre das Fusões, Intocável (+20 Fragmentos)";
            hud.ShowResult(false, stats, () => { }, () => { });
            for (int i = 0; i < 40; i++) yield return null;
            foreach (var s in sizes) yield return Shoot(Camera.main, $"Logs/aspect/p_resultado_{s.x}x{s.y}.png", s);
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
