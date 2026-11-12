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

        /// Espera a skill do elemento ficar pronta e lanca (teclas 1 a 4).
        IEnumerator Cast(ElementType e, Transform at, float after = 0.6f)
        {
            for (float t = 8f; t > 0f && player.SkillCooldown01(e) > 0f; t -= Time.deltaTime) yield return Step();
            Assert.AreEqual(0f, player.SkillCooldown01(e), "a skill de " + e + " nao recarregou");
            if (at != null) input.AimPoint = at.position;
            input.PressElementSkill((int)e);
            yield return Play(after, () => { if (at != null) input.AimPoint = at.position; });
        }

        [UnityTest]
        public IEnumerator Skills_Protect_Escape_Freeze_And_Quintessence()
        {
            yield return Setup("s3");
            var reactions = new List<string>();
            var statuses = new List<StatusType>();
            int shattered = 0, blocked = 0, marks = 0;
            bool quintOn = false;
            GameEvents.ReactionTriggered += e => reactions.Add(e.Name);
            GameEvents.StatusApplied += (_, t, _) => statuses.Add(t);
            GameEvents.Shattered += _ => shattered++;
            GameEvents.HitBlocked += (_, _) => blocked++;
            GameEvents.MarkApplied += (_, _) => marks++;
            GameEvents.QuintessenceActiveChanged += on => quintOn = on;

            var dummies = Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None);
            System.Array.Sort(dummies, (a, b) => Dist(a.transform).CompareTo(Dist(b.transform)));
            var a1 = dummies[0].GetComponent<Health>();
            var a2 = dummies[1].GetComponent<Health>();
            var hp = player.GetComponent<Health>();
            Assert.IsTrue(player.Loadout.HasAll, "a cena de teste comeca com os quatro elementos");

            // 1) Terra (tecla 3): o Pilar de Pedra da dano em volta, atordoa, quebra armadura e protege o heroi
            yield return RunTo(a1.transform, 3.6f, 3f);
            float before = a1.Current;
            yield return Cast(ElementType.Earth, a1.transform);
            Assert.Less(a1.Current, before, "o Pilar de Pedra nao causou dano");
            Assert.Contains(StatusType.Stun, statuses, "o Pilar de Pedra atordoa");
            Assert.Contains(StatusType.ArmorBroken, statuses, "a skill de Terra quebra armadura");
            Assert.IsNotNull(Object.FindFirstObjectByType<TimedLife>(), "o pilar deveria ter sido invocado");
            Assert.AreEqual(1, marks, "a skill marca o alvo com o elemento dela");
            Assert.AreEqual(0f, hp.TakeDamage(new DamageInfo(20, ElementType.None, Team.Enemy)), "a protecao da Terra bloqueia o proximo golpe");
            Assert.AreEqual(1, blocked);
            Assert.Greater(hp.TakeDamage(new DamageInfo(1, ElementType.None, Team.Enemy)), 0f, "so um golpe e bloqueado");
            yield return Play(0.3f);

            // 2) golpe basico: da dano, mas nao tem elemento. Nao marca nem detona a marca de Terra
            yield return RunTo(a1.transform, 2.4f, 3f);
            before = a1.Current;
            yield return Attack(a1, 1.0f);
            Assert.Less(a1.Current, before, "o combo nao causou dano");
            Assert.AreEqual(1, marks, "golpe basico nao marca");
            Assert.AreEqual(0, reactions.Count, "golpe basico nao detona reacao");

            // 3) Ar (tecla 4) no alvo marcado com Terra: Estilhacos. O Vendaval empurra e devolve o dash
            input.Move = new Vector2(-1, 0);
            input.PressDash();
            yield return Play(0.3f);
            input.Move = Vector2.zero;
            Assert.AreEqual(0, player.DashCharges, "o dash foi gasto");
            yield return RunTo(a1.transform, 2.2f, 3f);
            yield return Cast(ElementType.Earth, a1.transform); // marca de novo, caso a primeira tenha expirado
            var far = a1.transform.position;
            if (player.DashCharges > 0) { input.PressDash(); yield return Play(0.25f); yield return RunTo(a1.transform, 2.2f, 2f); far = a1.transform.position; }
            yield return Cast(ElementType.Air, null);
            Assert.Contains("Estilhacos", reactions, "skill de Ar em alvo marcado com Terra");
            Assert.Greater(Vector3.Distance(far, a1.transform.position), 0.8f, "o Vendaval deveria empurrar");
            Assert.AreEqual(player.DashMaxCharges, player.DashCharges, "o Vendaval e a fuga: devolve o dash");
            yield return Play(0.4f);

            // 4) Agua (tecla 2): a Mare cura o heroi. Depois Ar no mesmo alvo: Nevasca congela, e o golpe seguinte estilhaca
            hp.TakeDamage(new DamageInfo(30, ElementType.None, Team.Enemy));
            float hurt = hp.Current;
            yield return RunTo(a2.transform, 3.2f, 4f);
            yield return Cast(ElementType.Water, a2.transform);
            Assert.Greater(hp.Current, hurt, "a Mare cura quem lanca");
            Assert.Contains(StatusType.Soaked, statuses, "a Mare encharca");
            yield return RunTo(a2.transform, 2.2f, 4f);
            yield return Cast(ElementType.Air, null);
            Assert.Contains("Nevasca", reactions, "skill de Ar em alvo marcado com Agua");
            Assert.Contains(StatusType.Frozen, statuses);
            yield return RunTo(a2.transform, 2.4f, 3f);
            yield return Attack(a2, 0.2f);
            yield return Play(0.4f);
            Assert.Greater(shattered, 0, "golpe em alvo Congelado deveria estilhacar");
            yield return Play(0.4f);

            // 5) Quintessencia. O medidor enche com as reacoes; aqui completamos o que falta por fora para o video nao ficar longo.
            Assert.Greater(player.Quintessence.Meter, 0f, "as reacoes do heroi deveriam encher o medidor");
            input.PressQuintessence();
            yield return Play(0.1f);
            Assert.IsFalse(quintOn, "medidor incompleto nao ativa");
            player.AddQuintessence(100f);
            input.PressQuintessence();
            yield return Play(0.3f);
            Assert.IsTrue(quintOn, "medidor cheio + quatro elementos deveria ativar");
            Assert.AreEqual(1, player.Bursts, "o despertar solta a explosao do heroi");
            float full = hp.TakeDamage(new DamageInfo(10, ElementType.None, Team.Enemy));
            Assert.AreEqual(5f, full, 0.01f, "na Quintessencia o heroi leva metade do dano");
            int beforeQ = reactions.Count;
            yield return RunTo(a2.transform, 2.4f, 3f);
            yield return Attack(a2, 2.6f);
            Assert.GreaterOrEqual(reactions.Count - beforeQ, 2, "na Quintessencia ate o golpe basico dispara reacao (o despertar ja empurrou e queimou o alvo)");
            yield return Play(5.4f);
            Assert.IsFalse(quintOn, "a Quintessencia dura 8 s");
            yield return Play(0.6f);

            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Time.captureFramerate = 0;
        }

        [UnityTest]
        public IEnumerator Each_Element_Has_Its_Own_Particles()
        {
            yield return Setup("fx");
            var dummies = Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None);
            System.Array.Sort(dummies, (x, y) => Dist(x.transform).CompareTo(Dist(y.transform)));
            var names = new Dictionary<ElementType, string>
            {
                { ElementType.Fire, "FxEmbers" }, { ElementType.Water, "FxDroplets" }, { ElementType.Earth, "FxDebris" }, { ElementType.Air, "FxWisps" },
            };
            foreach (var pair in names)
            {
                player.ResetLoadout(pair.Key);
                input.AimPoint = dummies[0].transform.position;
                yield return Play(0.5f);
                Assert.AreEqual(0f, player.SkillCooldown01(pair.Key));
                var ps = GameObject.Find(pair.Value).GetComponent<ParticleSystem>();
                int before = ps.particleCount;
                input.PressElementSkill((int)pair.Key);
                yield return Play(0.08f);
                Assert.AreEqual("Skill" + pair.Key, player.GetComponentInChildren<PlayerAnimator>().Current, "cada elemento tem a sua animacao de skill");
                int most = 0;
                // foto no meio do efeito
                yield return Play(0.4f, () => most = Mathf.Max(most, ps.particleCount));
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); RenderTexture.active = null;
                File.WriteAllBytes($"docs/img/fx_{pair.Key.ToString().ToLowerInvariant()}.png", tex.EncodeToPNG());
                yield return Play(0.25f, () => most = Mathf.Max(most, ps.particleCount));
                Assert.Greater(most, before, $"a skill de {pair.Key} deveria soltar as particulas proprias do elemento");
                yield return Play(1.6f); // deixa tudo sumir antes do proximo elemento
            }
            Time.captureFramerate = 0;
        }

        [UnityTest]
        public IEnumerator Fire_And_Water_Skills_Combine_Into_Steam()
        {
            yield return Setup("combat");

            var reactions = new List<string>();
            var statuses = new List<StatusType>();
            var marked = new List<ElementType>();
            GameEvents.ReactionTriggered += e => reactions.Add(e.Name);
            GameEvents.StatusApplied += (_, t, _) => statuses.Add(t);
            GameEvents.MarkApplied += (e, _) => marked.Add(e);

            var dummies = Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(dummies.Length, 3);
            System.Array.Sort(dummies, (a, b) => Dist(a.transform).CompareTo(Dist(b.transform)));
            var a1 = dummies[0].GetComponent<Health>();
            var a2 = dummies[1].GetComponent<Health>();
            var a3 = dummies[2].GetComponent<Health>();

            // 1) parado, mirando
            input.AimPoint = a1.transform.position;
            yield return Play(0.5f);

            // 2) corre e solta o combo: dano, sem marca e sem status
            var start = player.transform.position;
            yield return RunTo(a1.transform, 2.4f, 3f);
            Assert.Greater(Vector3.Distance(start, player.transform.position), 2f, "o heroi nao andou");
            float before = a1.Current;
            yield return Attack(a1, 1.02f);
            Assert.Less(a1.Current, before, "o combo nao causou dano");
            Assert.AreEqual(29f, before - a1.Current, 6.6f, "combo completo em alvo limpo: 8 + 8 + 13 (um critico no maximo)");
            Assert.AreEqual(0, marked.Count, "golpe basico nao marca");
            Assert.AreEqual(0, statuses.Count, "golpe basico nao aplica status");

            // 3) Labareda (tecla 1): 18 de dano, marca de Fogo e Queimadura
            yield return RunTo(a1.transform, 3.2f, 3f);
            float hp1 = a1.Current;
            yield return Cast(ElementType.Fire, a1.transform);
            Assert.AreEqual(18f, hp1 - a1.Current, 3.1f, "Labareda causa 18 (mais um tique de Queimadura no maximo)");
            Assert.Contains(ElementType.Fire, marked, "a skill de Fogo marca o alvo");
            Assert.Contains(StatusType.Burn, statuses, "Labareda aplica Queimadura");
            Assert.AreEqual(ElementType.Fire, player.Element);

            // 4) golpe basico em alvo marcado bate mais forte que em alvo limpo (15%)
            yield return RunTo(a1.transform, 2.4f, 3f);
            float markedHit = 0f, cleanHit = 0f;
            System.Action<Health, DamageInfo, float> watch = (h, info, applied) =>
            {
                if (info.Element != ElementType.None || info.IsReaction || info.IsCrit) return;
                if (h == a1 && markedHit == 0f) markedHit = applied;
                if (h == a3 && cleanHit == 0f) cleanHit = applied;
            };
            GameEvents.DamageDealt += watch;
            for (int k = 0; k < 6 && markedHit == 0f; k++) yield return Attack(a1, 0.35f);

            // 5) Mare (tecla 2) no mesmo alvo: Vapor, empurra e Encharca
            yield return RunTo(a1.transform, 3.2f, 3f);
            var far = a1.transform.position;
            yield return Cast(ElementType.Water, a1.transform);
            Assert.Contains("Vapor", reactions, "skill de Agua em alvo marcado com Fogo deveria dar Vapor");
            Assert.Contains(StatusType.Soaked, statuses, "Mare aplica Encharcado");
            Assert.Greater(Vector3.Distance(far, a1.transform.position), 0.5f, "Mare deveria empurrar");
            Assert.AreEqual(ElementType.Water, player.Element, "a cor do heroi segue a ultima skill usada");
            yield return Play(0.4f);

            // 6) skill em recarga nao sai de novo
            float cd = player.SkillCooldown01(ElementType.Water);
            Assert.Greater(cd, 0f);
            int reactionsNow = reactions.Count;
            input.PressElementSkill(2);
            yield return Play(0.3f);
            Assert.AreEqual(reactionsNow, reactions.Count);

            // 7) dash atravessando: invulneravel
            var hp = player.GetComponent<Health>();
            yield return RunTo(a2.transform, 3.2f, 4f);
            var to = a2.transform.position - player.transform.position;
            to.y = 0; to.Normalize();
            input.Move = new Vector2(to.x, to.z);
            input.PressDash();
            yield return Play(0.05f);
            Assert.IsTrue(player.IsDashing);
            Assert.AreEqual(0f, hp.TakeDamage(new DamageInfo(50, ElementType.None, Team.Enemy)), "invulneravel durante o dash");
            input.Move = Vector2.zero;
            yield return Play(0.5f);

            // 8) terceiro boneco, limpo: mede o golpe basico sem marca e depois encadeia Fogo -> Agua de novo
            yield return RunTo(a3.transform, 2.4f, 5f);
            for (int k = 0; k < 6 && cleanHit == 0f; k++) yield return Attack(a3, 0.35f);
            GameEvents.DamageDealt -= watch;
            // o combo bate 8, 8 e 13 em alvo limpo; em alvo marcado, 15% a mais
            Assert.That(markedHit, Is.EqualTo(9.2f).Within(0.05f).Or.EqualTo(14.95f).Within(0.05f), "golpe basico em alvo marcado deveria bater 15% mais forte");
            int beforeReactions = reactions.Count;
            yield return RunTo(a3.transform, 3.2f, 3f);
            yield return Cast(ElementType.Fire, a3.transform);
            yield return RunTo(a3.transform, 3.2f, 3f);
            yield return Cast(ElementType.Water, a3.transform);
            Assert.GreaterOrEqual(reactions.Count - beforeReactions, 1, "Fogo e depois Agua no mesmo alvo reagem");
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
