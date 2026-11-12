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
    /// Luta contra o Guardiao das Cinzas com um heroi roteirizado, do inicio ate a sala limpa.
    public class BossPlayTests
    {
        const string Scene = "Assets/Scenes/Dev/Enzo_Boss.unity";
        const string FrameDir = "Logs/frames/boss";
        PlayerController player;
        ScriptedInput input;
        Camera cam;
        RenderTexture rt;
        Texture2D tex;
        int frame, saved;
        Vector3 dodgeDir;
        float dodgeLeft;

        [UnityTest, Timeout(300000)]
        public IEnumerator Guardian_Wears_Axe_Shield_And_Amber_Crystals()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Dev/Enzo_Boss.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            BossBrain brain = null;
            for (int k = 0; k < 300 && brain == null; k++) { brain = Object.FindFirstObjectByType<BossBrain>(); yield return null; }
            Assert.IsNotNull(brain, "o Guardiao deveria nascer");
            for (int k = 0; k < 75; k++) yield return null; // termina de nascer
            brain.enabled = false;
            var hero = GameObject.FindGameObjectWithTag("Player");
            hero.GetComponent<Health>().Pool.Invulnerable = true;
            // o Guardiao tem pecas proprias, nao so o esqueleto do pack ampliado
            foreach (var n in new[] { "boss_head", "boss_shoulders", "boss_back", "boss_core", "boss_weapon" })
            {
                var part = FindDeep(brain.transform, n);
                Assert.IsNotNull(part, "falta a peca " + n);
                var r = part.GetComponentInChildren<Renderer>();
                Assert.IsNotNull(r, n + " sem malha");
                Assert.Greater(r.bounds.size.magnitude, n == "boss_core" ? 0.3f : 1f, n + " deveria ter tamanho de chefe, nao de miniatura");
            }
            Assert.IsNull(FindDeep(brain.transform, "Skeleton_Axe"), "o machado do pack saiu");
            var cam = Camera.main;
            foreach (var b in cam.GetComponents<MonoBehaviour>()) if (b.GetType().Name == "FollowCamera") b.enabled = false;
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var tex = new Texture2D(1920, 540, TextureFormat.RGB24, false);
            var focus = brain.transform.position + Vector3.up * 2.6f;
            var front = brain.transform.forward;
            for (int view = 0; view < 2; view++)
            {
                // de frente e de costas
                var dir = view == 0 ? front : -front;
                cam.transform.position = focus + dir * 17f + Vector3.up * 6f;
                cam.transform.LookAt(focus);
                yield return null;
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 960, 540), view * 960, 0); RenderTexture.active = null;
            }
            System.IO.File.WriteAllBytes("docs/img/guardiao_novo.png", tex.EncodeToPNG());
            Time.captureFramerate = 0;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator Guardian_Three_Phases_Shield_Needs_Quintessence_Then_Dies()
        {
            GameEvents.Clear();
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            if (Directory.Exists(FrameDir)) Directory.Delete(FrameDir, true);
            Directory.CreateDirectory(FrameDir);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            player = Object.FindFirstObjectByType<PlayerController>();
            var hp = player.GetComponent<Health>();
            player.GetComponent<PlayerInputReader>().enabled = false;
            input = player.gameObject.AddComponent<ScriptedInput>();
            player.Input = input;
            input.AimPoint = player.transform.position + Vector3.forward;
            // confere a LUTA, nao o balanceamento: o roteiro joga mal, entao leva 35% do dano
            hp.Pool.ExtraDamageTakenMultiplier = 0.35f;
            cam = Camera.main;
            var hud = new GameObject("Hud").AddComponent<Quintessence.UI.Hud>();
            yield return null;
            hud.UseCamera(cam);
            rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            tex = new Texture2D(960, 540, TextureFormat.RGB24, false);

            int maxPhase = 0, telegraphs = 0;
            bool shieldUp = false, shieldNeedsQuint = false, shieldBroken = false, cleared = false, quintOn = false;
            GameEvents.BossPhaseChanged += (p, _) => maxPhase = Mathf.Max(maxPhase, p);
            GameEvents.BossShieldChanged += (up, needs) => { if (up) { shieldUp = true; shieldNeedsQuint = needs; } else if (shieldUp) shieldBroken = true; };
            GameEvents.RoomCleared += () => cleared = true;
            GameEvents.QuintessenceActiveChanged += on => quintOn = on;
            GameEvents.TelegraphStarted += e => { telegraphs++; ConsiderDodge(e); };

            BossBrain boss = null;
            for (float t = 4f; t > 0f && boss == null; t -= Time.deltaTime) { boss = Object.FindFirstObjectByType<BossBrain>(); yield return Step(); }
            Assert.IsNotNull(boss, "o chefe nao nasceu");
            var bossHp = boss.GetComponent<Health>();
            Assert.IsTrue(player.Loadout.HasAll, "a cena do chefe comeca com os quatro elementos");

            // teste de fluxo, nao de balanceamento: o roteiro nao sabe ler o anel nem a investida, entao leva 40% do dano
            hp.Pool.CurseMultiplier = 0.4f;
            float limit = 360f, sinceSwitch = 0f, immuneChecked = -1f;
            bool testedImmune = false;
            while (!cleared && !hp.IsDead && limit > 0f)
            {
                float dt = Time.deltaTime;
                limit -= dt; sinceSwitch += dt;
                bool shielded = boss != null && boss.Shield.Up;

                // com o escudo de pe: confere que o chefe nao leva dano, depois enche o medidor e ativa a Quintessencia.
                // O medidor e completado por fora para a luta nao depender de quantas reacoes o roteiro consegue nos reforcos.
                if (shielded && !testedImmune)
                {
                    if (immuneChecked < 0f) immuneChecked = bossHp.Current;
                    if (limit < 360f && Vector3.Distance(player.transform.position, boss.transform.position) < 4f && player.IsAttacking)
                    {
                        yield return Step(); yield return Step(); yield return Step();
                        Assert.AreEqual(immuneChecked, bossHp.Current, 0.01f, "com o escudo o chefe nao leva dano");
                        testedImmune = true;
                        player.AddQuintessence(100f);
                        input.PressQuintessence();
                    }
                }

                Health target = bossHp != null && !bossHp.IsDead ? bossHp : null;
                input.AttackHeld = false;
                if (dodgeLeft > 0f) { dodgeLeft -= dt; input.Move = new Vector2(dodgeDir.x, dodgeDir.z); }
                else if (target != null)
                {
                    var to = target.transform.position - player.transform.position; to.y = 0;
                    float d = to.magnitude;
                    input.AimPoint = target.transform.position;
                    input.Move = d > 3.4f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                    input.AttackHeld = d < 4.6f;
                    if (d < 5f && player.AnySkillReady) input.PressSkill();
                }
                else input.Move = Vector2.zero;
                yield return Step();
            }
            input.Move = Vector2.zero; input.AttackHeld = false;

            Assert.IsFalse(hp.IsDead, "o heroi caiu");
            Assert.GreaterOrEqual(telegraphs, 3, "os ataques do chefe sao avisados");
            Assert.AreEqual(3, maxPhase, "o chefe passa pelas tres fases");
            Assert.IsTrue(shieldUp, "na ultima fase o escudo sobe");
            Assert.IsTrue(shieldNeedsQuint, "com os quatro elementos, o escudo pede Quintessencia");
            Assert.IsTrue(testedImmune, "o roteiro nao chegou a bater no escudo");
            Assert.IsTrue(shieldBroken, "um golpe durante a Quintessencia quebra o escudo");
            Assert.IsTrue(cleared, "matar o chefe limpa a sala");
            for (float t = 3f; t > 0f; t -= Time.deltaTime) yield return Step();
            Assert.IsNull(Object.FindFirstObjectByType<EnemyBrain>(), "os reforcos caem junto com o chefe");
            Assert.IsNull(Object.FindFirstObjectByType<BossBrain>());
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
        }

        void ConsiderDodge(TelegraphEvent e)
        {
            if (player == null || dodgeLeft > 0f) return;
            var p = player.transform.position;
            var side = new Vector3(-e.Direction.z, 0f, e.Direction.x);
            bool threatened;
            if (e.IsLine)
            {
                var rel = p - e.Point;
                float along = Vector3.Dot(rel, e.Direction);
                threatened = along > 0f && along < e.Length && Mathf.Abs(Vector3.Dot(rel, side)) < e.Radius + 1.2f;
                dodgeDir = Vector3.Dot(side, -p) >= 0f ? side : -side;
            }
            else
            {
                var away = p - e.Point; away.y = 0f;
                threatened = away.magnitude < e.Radius + 1.2f;
                dodgeDir = away.sqrMagnitude > 0.05f ? away.normalized : -player.transform.forward;
            }
            if (!threatened) return;
            dodgeLeft = 0.45f;
            input.PressDash();
        }

        IEnumerator Step()
        {
            yield return null;
            if (frame++ % 3 != 0 || cam == null) yield break;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes($"{FrameDir}/f{saved++:00000}.jpg", tex.EncodeToJPG(85));
        }
    }
}
#endif
