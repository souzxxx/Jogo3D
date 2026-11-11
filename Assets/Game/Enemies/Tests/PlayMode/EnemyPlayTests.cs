#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using Quintessence.Combat;
using Quintessence.Core;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Enemies.PlayTests
{
    /// Joga a sala de inimigos com um heroi roteirizado: apanha parado, depois desvia dos avisos e limpa as duas ondas.
    public class EnemyPlayTests
    {
        const string Scene = "Assets/Scenes/Dev/Enzo_Enemies.unity";
        const string FrameDir = "Logs/frames/enemies";

        PlayerController player;
        ScriptedInput input;
        Health hp;
        Camera cam;
        RenderTexture rt;
        Texture2D tex;
        int frame, saved;
        Vector3 dodgeDir;
        float dodgeLeft;
        bool dodging;

        [UnityTest]
        public IEnumerator Waves_Telegraph_Attack_And_Room_Clears()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            if (Directory.Exists(FrameDir)) Directory.Delete(FrameDir, true);
            Directory.CreateDirectory(FrameDir);

            GameEvents.Clear(); // assinantes de testes anteriores apontam para objetos ja destruidos
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player, "cena sem heroi");
            Assert.IsNotNull(Object.FindFirstObjectByType<WaveSpawner>(), "cena sem ondas");
            hp = player.GetComponent<Health>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            input = player.gameObject.AddComponent<ScriptedInput>();
            player.Input = input;
            input.AimPoint = player.transform.position + Vector3.forward;
            cam = Camera.main;
            rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            tex = new Texture2D(960, 540, TextureFormat.RGB24, false);

            int telegraphs = 0, telegraphsBeforeFirstHit = -1, waves = 0, spawned = 0;
            bool cleared = false, started = false, sawProjectile = false;
            // o tiro em si pode nao sair se o atirador morrer no meio do aviso; o aviso em linha sempre vem antes
            GameEvents.TelegraphStarted += e => { telegraphs++; if (e.IsLine) sawProjectile = true; if (dodging) ConsiderDodge(e); };
            GameEvents.DamageDealt += (target, _, _) => { if (target == hp && telegraphsBeforeFirstHit < 0) telegraphsBeforeFirstHit = telegraphs; };
            GameEvents.WaveStarted += (_, _) => waves++;
            GameEvents.EnemySpawned += _ => spawned++;
            GameEvents.RoomStarted += () => started = true;
            GameEvents.RoomCleared += () => cleared = true;
            bool sawArmor = false, sawExplosion = false;
            GameEvents.ArmorChanged += (_, on) => { if (on) sawArmor = true; };
            GameEvents.Exploded += (_, _) => sawExplosion = true;

            // 1) parado: os inimigos chegam, avisam e acertam
            float wait = 9f;
            while (hp.Current >= hp.Max && wait > 0f) { wait -= Time.deltaTime; yield return Step(); }
            Assert.IsTrue(started, "a sala deveria ter comecado");
            Assert.AreEqual(3, spawned, "a primeira onda tem 3 inimigos");
            Assert.Less(hp.Current, hp.Max, "heroi parado deveria apanhar");
            Assert.Greater(telegraphsBeforeFirstHit, 0, "todo golpe tem aviso antes");

            // 2) agora joga: desvia dos avisos, bate, troca de elemento e usa skill ate limpar a sala
            dodging = true;
            float limit = 150f, sinceSwitch = 0f;
            while (!cleared && !hp.IsDead && limit > 0f)
            {
                float dt = Time.deltaTime;
                limit -= dt;
                sinceSwitch += dt;
                if (Object.FindFirstObjectByType<Projectile>() != null) sawProjectile = true;

                var target = Nearest();
                if (dodgeLeft > 0f)
                {
                    dodgeLeft -= dt;
                    input.Move = new Vector2(dodgeDir.x, dodgeDir.z);
                    input.AttackHeld = false;
                }
                else if (target == null)
                {
                    input.Move = Vector2.zero;
                    input.AttackHeld = false;
                }
                else
                {
                    var to = target.transform.position - player.transform.position;
                    to.y = 0;
                    float d = to.magnitude;
                    input.AimPoint = target.transform.position;
                    input.Move = d > 2.6f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    input.AttackHeld = d < 3.6f;
                    if (d < 4.5f && player.SkillCooldown01(player.Element) <= 0f) input.PressSkill();
                    if (sinceSwitch > 1.3f) { sinceSwitch = 0f; input.PressSwitch(1); }
                }
                yield return Step();
            }
            input.Move = Vector2.zero;
            input.AttackHeld = false;

            Assert.IsFalse(hp.IsDead, "o heroi morreu antes de limpar a sala");
            Assert.IsTrue(cleared, "a sala deveria ficar limpa quando a ultima onda morre");
            Assert.AreEqual(3, waves, "tres ondas");
            Assert.AreEqual(12, spawned, "3 + 4 + 5 (Bruto, 2 Explosivos, 2 Rastejantes)");
            Assert.IsTrue(sawArmor, "o Bruto nasce com armadura");
            Assert.IsTrue(sawExplosion, "Explosivo deveria explodir (ou ser morto antes: entao este teste precisa de outro roteiro)");
            Assert.IsTrue(sawProjectile, "o atirador deveria ter avisado ou disparado um tiro");
            for (float t = 1.8f; t > 0f; t -= Time.deltaTime) yield return Step();
            Assert.IsNull(Object.FindFirstObjectByType<EnemyBrain>(), "inimigos mortos somem da cena");

            Time.captureFramerate = 0;
            Time.timeScale = 1f;
        }

        void ConsiderDodge(TelegraphEvent e)
        {
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
            // sai de lado em relacao ao golpe, para o lado que aponta para o centro da sala
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
                // o roteiro nao ataca o Explosivo: deixa chegar, desvia do aviso e confere a explosao
                if (e.Definition.attack == EnemyAttackKind.Explode) continue;
                float d = Vector3.Distance(e.transform.position, player.transform.position);
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
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
