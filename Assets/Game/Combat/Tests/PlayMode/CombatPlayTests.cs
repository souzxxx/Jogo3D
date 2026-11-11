#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using Quintessence.Core;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Combat.PlayTests
{
    /// Joga a cena de teste com entrada roteirizada, confere as regras e grava os quadros em Logs/frames/s1.
    public class CombatPlayTests
    {
        const string Scene = "Assets/Scenes/Dev/Leonardo_Combat.unity";
        const string FrameDir = "Logs/frames/s1";

        PlayerController player;
        ScriptedInput input;
        Camera cam;
        RenderTexture rt;
        Texture2D tex;
        int frame, saved;

        [UnityTest]
        public IEnumerator Hero_Runs_Combos_And_Dashes()
        {
            Time.captureFramerate = 30;
            if (Directory.Exists(FrameDir)) Directory.Delete(FrameDir, true);
            Directory.CreateDirectory(FrameDir);

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "cena sem heroi");
            player.GetComponent<PlayerInputReader>().enabled = false;
            input = player.gameObject.AddComponent<ScriptedInput>();
            player.Input = input;
            cam = Camera.main;
            rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            tex = new Texture2D(960, 540, TextureFormat.RGB24, false);

            var dummies = Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(dummies.Length, 2);
            System.Array.Sort(dummies, (a, b) => Dist(a.transform).CompareTo(Dist(b.transform)));
            var first = dummies[0].GetComponent<Health>();
            var second = dummies[1].GetComponent<Health>();

            // 1) parado, mirando
            input.AimPoint = first.transform.position;
            yield return Play(0.5f);

            // 2) corre ate o primeiro boneco
            var start = player.transform.position;
            yield return RunTo(first.transform, 2.0f, 3f);
            Assert.Greater(Vector3.Distance(start, player.transform.position), 2f, "o heroi nao andou");

            // 3) combo completo
            float before = first.Current;
            input.AttackHeld = true;
            yield return Play(1.1f, () => input.AimPoint = first.transform.position);
            input.AttackHeld = false;
            Assert.Less(first.Current, before, "o combo nao causou dano");
            Assert.AreEqual(22f, before - first.Current, 0.01f, "combo de 3 golpes deve somar 6+6+10");
            yield return Play(0.3f);

            // 4) dash com invulnerabilidade
            var hp = player.GetComponent<Health>();
            var p0 = player.transform.position;
            input.Move = new Vector2(-0.7f, 0.7f);
            input.PressDash();
            yield return Play(0.07f);
            Assert.IsTrue(player.IsDashing, "nao entrou em dash");
            Assert.AreEqual(0f, hp.TakeDamage(new DamageInfo(50, ElementType.Fire, Team.Enemy)), "dash deve dar i-frames");
            yield return Play(0.25f);
            Assert.IsFalse(player.IsDashing);
            Assert.Greater(Vector3.Distance(p0, player.transform.position), 3f, "dash curta demais");
            Assert.Greater(hp.TakeDamage(new DamageInfo(1, ElementType.Fire, Team.Enemy)), 0f, "i-frames nao acabaram");
            input.Move = Vector2.zero;
            yield return Play(0.25f);

            // 5) vai ao segundo boneco e bate ate ele cair
            yield return RunTo(second.transform, 2.0f, 4f);
            input.AttackHeld = true;
            float limit = 4f;
            while (!second.IsDead && limit > 0f)
            {
                input.AimPoint = second.transform.position;
                // o golpe empurra o alvo: o heroi avanca atacando (anda a 60% durante o golpe)
                var to = second.transform.position - player.transform.position;
                to.y = 0;
                input.Move = to.magnitude > 1.8f ? new Vector2(to.normalized.x, to.normalized.z) : Vector2.zero;
                limit -= Time.deltaTime;
                yield return Step();
            }
            input.AttackHeld = false;
            input.Move = Vector2.zero;
            Assert.IsTrue(second.IsDead, "o segundo boneco deveria ter morrido");
            yield return Play(0.9f);

            Assert.Greater(saved, 60, "poucos quadros gravados");
            Time.captureFramerate = 0;
        }

        float Dist(Transform t) => Vector3.Distance(t.position, player.transform.position);

        IEnumerator RunTo(Transform target, float stopAt, float timeout)
        {
            while (Dist(target) > stopAt && timeout > 0f)
            {
                var to = target.position - player.transform.position;
                to.y = 0;
                to.Normalize();
                input.Move = new Vector2(to.x, to.z);
                input.AimPoint = target.position;
                timeout -= Time.deltaTime;
                yield return Step();
            }
            input.Move = Vector2.zero;
        }

        IEnumerator Play(float seconds, System.Action each = null)
        {
            while (seconds > 0f)
            {
                each?.Invoke();
                seconds -= Time.deltaTime;
                yield return Step();
            }
        }

        IEnumerator Step()
        {
            yield return null;
            if (frame++ % 2 != 0 || cam == null) yield break;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes($"{FrameDir}/f{saved++:0000}.png", tex.EncodeToPNG());
        }
    }
}
#endif
