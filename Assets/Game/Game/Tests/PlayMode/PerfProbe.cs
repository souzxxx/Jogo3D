#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Enemies;
using Quintessence.Run;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Game.PlayTests
{
    /// Rubrica 2 (desempenho): tempo de quadro nas salas mais pesadas (Cerco fundo, Elite com mini-chefe, chefe final),
    /// com o heroi soltando skills no meio do bando. Roda no editor, entao e o pior caso (o player e mais leve). Grava Logs/perf.txt.
    public class PerfProbe
    {
        [UnityTest, Explicit, Timeout(900000)]
        public IEnumerator Heaviest_Rooms_Frame_Times()
        {
            PlayerPrefs.DeleteKey("quintessence.meta.perf");
            MetaStore.UseKey("quintessence.meta.perf");
            RunSaveStore.UseKey("quintessence.meta.perf.run");
            GameFlow.AutoPickTalents = true;
            var log = new List<string> { $"# editor {Application.unityVersion}, {SystemInfo.graphicsDeviceName}, tela {Screen.width}x{Screen.height}" };
            foreach (var (label, type, depth, trait) in new[]
            {
                ("Cerco na camada 8", RoomType.Siege, 7, EliteModifier.None),
                ("Elite Divisores na camada 7", RoomType.Elite, 6, EliteModifier.Splitting),
                ("Chefe final", RoomType.Boss, 9, EliteModifier.None),
            })
            {
                GameEvents.Clear();
                Time.captureFramerate = 0;
                GameFlow.StartElement = ElementType.Fire; GameFlow.Seed = 11; GameFlow.Layers = 10; GameFlow.Shrines = 3;
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                var director = Object.FindFirstObjectByType<RunDirector>();
                for (float t = 0; t < 10f && director.Busy; t += Time.unscaledDeltaTime) yield return null;
                var hero = Object.FindFirstObjectByType<PlayerController>();
                var hp = hero.GetComponent<Health>();
                hp.Pool.Invulnerable = true;
                foreach (var e in Elements.All) hero.Awaken(e);
                hero.GetComponent<PlayerInputReader>().enabled = false;
                var input = hero.gameObject.AddComponent<ScriptedInput>();
                hero.Input = input;
                if (type == RoomType.Siege) director.StartSiege();
                else director.StartEncounter(type, depth, trait);
                for (float t = 0; t < 3f; t += Time.unscaledDeltaTime) yield return null; // ondas chegando

                // em modo de lote nada e desenhado: a camera desenha num alvo de 1280x720 a cada quadro, para o custo de GPU entrar na conta
                var cam = Camera.main;
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                var frames = new List<float>();
                var sw = new System.Diagnostics.Stopwatch();
                int peak = 0, slot = 0;
                for (float t = 0; t < 15f; t += Time.deltaTime)
                {
                    var alive = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(b => !b.GetComponent<Health>().IsDead).ToArray();
                    peak = Mathf.Max(peak, alive.Length + Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).Length);
                    var target = alive.OrderBy(b => (b.transform.position - hero.transform.position).sqrMagnitude).FirstOrDefault();
                    if (target != null) input.AimPoint = target.transform.position;
                    input.AttackHeld = false; // so skills: os inimigos se acumulam e os efeitos aparecem
                    if (Time.frameCount % 20 == 0) input.PressElementSkill(1 + (slot++ % 4));
                    if (Time.frameCount % 90 == 0) input.PressDash();
                    sw.Restart();
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                    var fence = new Texture2D(1, 1); RenderTexture.active = rt; fence.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); RenderTexture.active = null; Object.Destroy(fence);
                    float render = (float)sw.Elapsed.TotalMilliseconds;
                    yield return null;
                    frames.Add(Time.unscaledDeltaTime * 1000f + render);
                }
                rt.Release();
                input.AttackHeld = false;
                frames.Sort();
                float avg = frames.Average(), p99 = frames[(int)(frames.Count * 0.99f)], worst = frames[frames.Count - 1];
                log.Add($"{label}: {1000f / avg:0} fps em media, 99% dos quadros em ate {p99:0.0} ms, pior {worst:0.0} ms, ate {peak} inimigos vivos, {frames.Count} quadros");
            }
            File.WriteAllText("Logs/perf.txt", string.Join("\n", log));
            Debug.Log("PERF\n" + string.Join("\n", log));
            MetaStore.UseKey(MetaStore.DefaultKey);
            RunSaveStore.UseKey(RunSaveStore.DefaultKey);
            GameFlow.AutoPickTalents = false;
            GameFlow.Seed = 1;
        }
    }
}
#endif
