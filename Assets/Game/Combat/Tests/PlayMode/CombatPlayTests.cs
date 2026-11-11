#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Quintessence.Core;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Quintessence.Combat.PlayTests
{
    /// Joga a cena de teste com entrada roteirizada, confere as regras e grava os quadros em Logs/frames/combat.
    public class CombatPlayTests
    {
        const string Scene = "Assets/Scenes/Dev/Leonardo_Combat.unity";
        string frameDir;

        PlayerController player;
        ScriptedInput input;
        Camera cam;
        RenderTexture rt;
        Texture2D tex;
        int frame, saved;

        IEnumerator Setup(string frames)
        {
            frameDir = "Logs/frames/" + frames;
            frame = saved = 0;
            Time.timeScale = 1f;
            Time.captureFramerate = 30;
            if (Directory.Exists(frameDir)) Directory.Delete(frameDir, true);
            Directory.CreateDirectory(frameDir);

            GameEvents.Clear(); // assinantes de testes anteriores apontam para objetos ja destruidos
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
        }

        IEnumerator SwitchTo(ElementType e)
        {
            for (int i = 0; i < 4 && player.Element != e; i++)
            {
                input.PressSwitch(1);
                yield return Play(0.45f);
            }
            Assert.AreEqual(e, player.Element, "nao conseguiu trocar para " + e);
        }

        [UnityTest]
        public IEnumerator Hero_Uses_Earth_Air_And_Quintessence()
        {
            yield return Setup("s3");
            var reactions = new List<string>();
            var statuses = new List<StatusType>();
            int shattered = 0, blocked = 0;
            bool quintOn = false;
            GameEvents.ReactionTriggered += e => reactions.Add(e.Name);
            GameEvents.StatusApplied += (_, t, _) => statuses.Add(t);
            GameEvents.Shattered += _ => shattered++;
            GameEvents.HitBlocked += (_, _) => blocked++;
            GameEvents.QuintessenceActiveChanged += on => quintOn = on;

            var dummies = Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None);
            System.Array.Sort(dummies, (a, b) => Dist(a.transform).CompareTo(Dist(b.transform)));
            var a1 = dummies[0].GetComponent<Health>();
            var a2 = dummies[1].GetComponent<Health>();
            var hp = player.GetComponent<Health>();
            Assert.IsTrue(player.Loadout.HasAll, "a cena de teste comeca com os quatro elementos");

            // 1) Terra: Pilar de Pedra invoca o pilar, da dano em volta e atordoa
            yield return SwitchTo(ElementType.Earth);
            yield return RunTo(a1.transform, 3.6f, 3f);
            float before = a1.Current;
            input.AimPoint = a1.transform.position;
            input.PressSkill();
            yield return Play(0.6f, () => input.AimPoint = a1.transform.position);
            Assert.Less(a1.Current, before, "o Pilar de Pedra nao causou dano");
            Assert.Contains(StatusType.Stun, statuses, "o Pilar de Pedra atordoa");
            Assert.IsNotNull(Object.FindFirstObjectByType<TimedLife>(), "o pilar deveria ter sido invocado");

            // 2) dash de Terra: curta e bloqueia um golpe depois dela
            var p0 = player.transform.position;
            input.Move = new Vector2(-1, 0);
            input.PressDash();
            yield return Play(0.04f);
            input.Move = Vector2.zero; // solta o direcional para medir so a dash, sem a corrida depois
            yield return Play(0.3f);
            float dashed = Vector3.Distance(p0, player.transform.position);
            Assert.Less(dashed, 3.4f, "a dash de Terra e mais curta");
            Assert.AreEqual(0f, hp.TakeDamage(new DamageInfo(20, ElementType.None, Team.Enemy)), "a armadura bloqueia um golpe");
            Assert.AreEqual(1, blocked);
            Assert.Greater(hp.TakeDamage(new DamageInfo(1, ElementType.None, Team.Enemy)), 0f, "so um golpe e bloqueado");
            yield return Play(0.3f);

            // 3) combo de Terra marca; troca para Ar e bate: Estilhacos
            yield return RunTo(a1.transform, 2.4f, 3f);
            yield return Attack(a1, 0.5f);
            yield return SwitchTo(ElementType.Air);
            yield return Attack(a1, 0.5f);
            Assert.Contains("Estilhacos", reactions, "Ar em alvo marcado com Terra");
            yield return Play(0.4f);

            // 4) Vendaval: explosao em volta do heroi que empurra
            yield return RunTo(a1.transform, 2.2f, 3f);
            var far = a1.transform.position;
            input.PressSkill();
            yield return Play(0.6f);
            Assert.Greater(Vector3.Distance(far, a1.transform.position), 0.8f, "o Vendaval deveria empurrar");

            // 5) dash de Ar tem duas cargas: a segunda sai logo depois da primeira
            var center = new Vector3(0, 0, -1) - player.transform.position;
            center.y = 0; center.Normalize();
            input.Move = new Vector2(center.x, center.z);
            input.PressDash();
            yield return Play(0.25f);
            Assert.IsFalse(player.IsDashing);
            input.PressDash();
            yield return Play(0.07f);
            Assert.IsTrue(player.IsDashing, "a segunda carga da dash de Ar nao saiu");
            input.Move = Vector2.zero;
            yield return Play(0.5f);

            // 6) Agua marca, Ar detona Nevasca; o golpe seguinte estilhaca o congelado.
            // Um golpe so de cada vez: um golpe ja iniciado sai com o elemento que estiver ativo na hora do impacto.
            yield return SwitchTo(ElementType.Water);
            yield return RunTo(a2.transform, 2.4f, 4f);
            yield return Attack(a2, 0.1f);
            yield return Play(0.4f);
            yield return SwitchTo(ElementType.Air);
            yield return Attack(a2, 0.1f);
            yield return Play(0.4f);
            Assert.Contains("Nevasca", reactions, "Ar em alvo marcado com Agua");
            Assert.Contains(StatusType.Frozen, statuses);
            yield return Attack(a2, 0.1f);
            yield return Play(0.4f);
            Assert.Greater(shattered, 0, "golpe em alvo Congelado deveria estilhacar");
            yield return Play(0.4f);

            // 7) Quintessencia. O medidor enche com 10 reacoes; aqui completamos o que falta por fora para o video nao ficar longo.
            Assert.Greater(player.Quintessence.Meter, 0f, "as reacoes do heroi deveriam encher o medidor");
            input.PressQuintessence();
            yield return Play(0.1f);
            Assert.IsFalse(quintOn, "medidor incompleto nao ativa");
            player.AddQuintessence(100f);
            input.PressQuintessence();
            yield return Play(0.3f);
            Assert.IsTrue(quintOn, "medidor cheio + quatro elementos deveria ativar");
            float full = hp.TakeDamage(new DamageInfo(10, ElementType.None, Team.Enemy));
            Assert.AreEqual(5f, full, 0.01f, "na Quintessencia o heroi leva metade do dano");
            int beforeQ = reactions.Count;
            yield return RunTo(a2.transform, 2.4f, 3f);
            yield return Attack(a2, 2.6f);
            Assert.GreaterOrEqual(reactions.Count - beforeQ, 3, "na Quintessencia os golpes disparam reacao sem precisar de marca");
            yield return Play(5.4f);
            Assert.IsFalse(quintOn, "a Quintessencia dura 8 s");
            yield return Play(0.6f);

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Time.captureFramerate = 0;
        }

        [UnityTest]
        public IEnumerator Hero_Fights_With_Fire_And_Water()
        {
            yield return Setup("combat");

            var reactions = new List<string>();
            var statuses = new List<StatusType>();
            int marks = 0, zones = 0, strikes = 0;
            GameEvents.ReactionTriggered += e => reactions.Add(e.Name);
            GameEvents.StatusApplied += (_, t, _) => statuses.Add(t);
            GameEvents.MarkApplied += (_, _) => marks++;
            GameEvents.ZoneSpawned += _ => zones++;
            GameEvents.AttackPerformed += e => { if (e.SwitchStrike) strikes++; };

            var dummies = Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(dummies.Length, 3);
            System.Array.Sort(dummies, (a, b) => Dist(a.transform).CompareTo(Dist(b.transform)));
            var a1 = dummies[0].GetComponent<Health>();
            var a2 = dummies[1].GetComponent<Health>();
            var a3 = dummies[2].GetComponent<Health>();

            // 1) parado, mirando
            Assert.AreEqual(ElementType.Fire, player.Element);
            input.AimPoint = a1.transform.position;
            yield return Play(0.5f);

            // 2) corre e solta o combo de Fogo: marca e, no finalizador, Queimadura
            var start = player.transform.position;
            yield return RunTo(a1.transform, 2.4f, 3f);
            Assert.Greater(Vector3.Distance(start, player.transform.position), 2f, "o heroi nao andou");
            float before = a1.Current;
            yield return Attack(a1, 1.02f);
            Assert.Less(a1.Current, before, "o combo nao causou dano");
            Assert.Greater(marks, 0, "golpe de Fogo deveria marcar");
            Assert.Contains(StatusType.Burn, statuses, "finalizador de Fogo aplica Queimadura");

            // 3) troca para Agua logo depois de acertar: Golpe de Troca + reacao Vapor
            input.PressSwitch(1);
            yield return Play(0.2f);
            Assert.AreEqual(ElementType.Water, player.Element);
            yield return Attack(a1, 0.5f);
            Assert.Contains("Vapor", reactions, "Agua em alvo marcado com Fogo deveria dar Vapor");
            Assert.Greater(strikes, 0, "trocar logo apos acertar deveria dar Golpe de Troca");
            yield return Play(0.5f);

            // 4) skill de Agua: empurra e Encharca
            var far = a1.transform.position;
            input.AimPoint = far;
            input.PressSkill();
            yield return Play(0.6f, () => input.AimPoint = a1.transform.position);
            Assert.Contains(StatusType.Soaked, statuses, "Mare aplica Encharcado");
            Assert.Greater(Vector3.Distance(far, a1.transform.position), 0.5f, "Mare deveria empurrar");
            yield return Play(0.4f);

            // 5) volta para Fogo, vai ao segundo boneco e solta a Labareda
            input.PressSwitch(-1);
            yield return Play(0.3f);
            Assert.AreEqual(ElementType.Fire, player.Element);
            yield return RunTo(a2.transform, 3.2f, 4f);
            float hp2 = a2.Current;
            input.PressSkill();
            yield return Play(0.6f, () => input.AimPoint = a2.transform.position);
            Assert.AreEqual(18f, hp2 - a2.Current, 3.1f, "Labareda causa 18 (mais um tique de Queimadura no maximo)");

            // 6) dash de Fogo atravessando: invulneravel e deixa rastro
            var hp = player.GetComponent<Health>();
            var to = a2.transform.position - player.transform.position;
            to.y = 0; to.Normalize();
            var side = new Vector3(-to.z, 0, to.x);
            var d = (to + side * 0.9f).normalized;
            input.Move = new Vector2(d.x, d.z);
            var p0 = player.transform.position;
            input.PressDash();
            yield return Play(0.07f);
            Assert.IsTrue(player.IsDashing, "nao entrou em dash");
            Assert.AreEqual(0f, hp.TakeDamage(new DamageInfo(50, ElementType.Fire, Team.Enemy)), "dash deve dar i-frames");
            yield return Play(0.25f);
            Assert.Greater(Vector3.Distance(p0, player.transform.position), 3f, "dash curta demais");
            Assert.Greater(zones, 0, "dash de Fogo deixa rastro");
            input.Move = Vector2.zero;
            yield return Play(0.5f);

            // 7) alterna elementos no terceiro boneco: reacoes em sequencia
            yield return RunTo(a3.transform, 2.4f, 5f);
            int beforeReactions = reactions.Count;
            for (int i = 0; i < 4; i++)
            {
                yield return Attack(a3, 0.75f);
                input.PressSwitch(1);
                yield return Play(0.25f);
            }
            Assert.GreaterOrEqual(reactions.Count - beforeReactions, 2, "alternar elementos deveria encadear reacoes");
            yield return Play(1.0f);

            Assert.Greater(saved, 100, "poucos quadros gravados");
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "a pausa de impacto deve devolver o tempo ao normal");
            Time.captureFramerate = 0;
        }

        float Dist(Transform t) => Vector3.Distance(t.position, player.transform.position);

        /// Ataca avancando: o golpe empurra o alvo e o heroi anda a 60% durante o golpe.
        IEnumerator Attack(Health target, float seconds)
        {
            input.AttackHeld = true;
            while (seconds > 0f)
            {
                var to = target.transform.position - player.transform.position;
                to.y = 0;
                input.AimPoint = target.transform.position;
                input.Move = to.magnitude > 2.2f ? new Vector2(to.normalized.x, to.normalized.z) : Vector2.zero;
                seconds -= Time.deltaTime;
                yield return Step();
            }
            input.AttackHeld = false;
            input.Move = Vector2.zero;
        }

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
            File.WriteAllBytes($"{frameDir}/f{saved++:0000}.png", tex.EncodeToPNG());
        }
    }
}
#endif
