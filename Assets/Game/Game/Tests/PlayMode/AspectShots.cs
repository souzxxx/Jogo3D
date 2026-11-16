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
            var hp = Object.FindFirstObjectByType<Quintessence.Combat.PlayerController>().GetComponent<Health>();
            hp.Pool.ExtraDamageTakenMultiplier = 0f;
            for (int k = 0; k < 4; k++)
            {
                for (float t = 0; t < 2.2f; t += Time.deltaTime) yield return null;
                yield return Shoot(Camera.main, $"Logs/aspect/combat_{k}.png", new Vector2Int(1280, 720));
            }
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
