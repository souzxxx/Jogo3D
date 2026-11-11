#if UNITY_EDITOR
using System.Collections;
using System.IO;
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

            bool sawDoors = false, sawShrine = false;
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
                    input.Move = d > 2.6f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    input.AttackHeld = d < 3.6f;
                    if (d < 4.5f && player.SkillCooldown01(player.Element) <= 0f) input.PressSkill();
                    if (sinceSwitch > 1.3f) { sinceSwitch = 0f; input.PressSwitch(1); }
                }
                else if (director.Interactables.Count > 0)
                {
                    sawDoors = true;
                    var goal = director.Interactables[0].Position;
                    var to = goal - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.AimPoint = goal;
                    input.Move = d > 1.4f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    if (d < 2.2f && sinceInteract > 0.4f) { sinceInteract = 0f; input.PressInteract(); }
                }
                else input.Move = Vector2.zero;
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
