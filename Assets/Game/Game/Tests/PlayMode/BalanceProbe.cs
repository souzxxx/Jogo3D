#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
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
    /// Sonda de balanceamento: um roteiro simples joga runs inteiras e anota onde cai e com quanta vida sai de cada sala.
    /// Nao e teste de aprovacao (por isso Explicit): serve para comparar numeros antes e depois de um ajuste.
    public class BalanceProbe
    {
        PlayerController player;
        ScriptedInput input;
        Health hp;
        Vector3 dodgeDir, lastPos;
        float dodgeLeft, stuck, sidestep;
        Vector2 sideDir;

        [UnityTest, Explicit, Timeout(3000000)]
        public IEnumerator Probe_Full_Runs()
        {
            var log = new System.Text.StringBuilder();
            float taken = 0.5f; // o roteiro joga pior que uma pessoa: leva metade do dano
            GameFlow.AutoPickTalents = true;
            MetaStore.UseKey("quintessence.meta.test");
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            foreach (var hero in Elements.All)
                for (int seed = 1; seed <= 1; seed++)
                {
                    Time.timeScale = 1f;
                    Time.captureFramerate = 30;
                    GameEvents.Clear();
                    GameFlow.StartElement = hero; GameFlow.Seed = seed * 7 + (int)hero; GameFlow.Layers = 10; GameFlow.Shrines = 3; GameFlow.Tutorial = false;
                    yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Run.unity", new LoadSceneParameters(LoadSceneMode.Single));
                    yield return null; yield return null;
                    var director = Object.FindFirstObjectByType<RunDirector>();
                    player = Object.FindFirstObjectByType<PlayerController>();
                    hp = player.GetComponent<Health>();
                    player.GetComponent<PlayerInputReader>().enabled = false;
                    input = player.gameObject.AddComponent<ScriptedInput>();
                    player.Input = input;
                    hp.Pool.ExtraDamageTakenMultiplier = taken;
                    GameEvents.TelegraphStarted += ConsiderDodge;
                    GameEvents.QuintessenceActiveChanged += on => { if (!on) hp.Pool.ExtraDamageTakenMultiplier = taken; };
                    float limit = 2500f, sinceInteract = 0f, roomTime = 0f, clock = 0f;
                    int lastDepth = -1, tried = 0;
                    string rooms = "";
                    while (!director.Finished && limit > 0f)
                    {
                        float dt = Time.deltaTime;
                        limit -= dt; sinceInteract += dt; roomTime += dt; clock += dt;
                        if (!director.Busy && director.Run.Depth != lastDepth)
                        {
                            if (lastDepth >= 0) rooms += $" hp {hp.Current / hp.Max * 100f:0}% {roomTime:0}s |";
                            lastDepth = director.Run.Depth; roomTime = 0f; tried = 0;
                            rooms += $" {lastDepth + 1}:{director.Run.Current.Type}";
                        }
                        if (hp.Pool.ExtraDamageTakenMultiplier == 1f) hp.Pool.ExtraDamageTakenMultiplier = taken;
                        var target = Nearest();
                        input.AttackHeld = false;
                        // o escurecimento da troca de sala corre em tempo real e a sonda em tempo de jogo: o roteiro luta mesmo durante ele
                        if (dodgeLeft > 0f) { dodgeLeft -= dt; input.Move = new Vector2(dodgeDir.x, dodgeDir.z); }
                        else if (target != null)
                        {
                            var to = target.transform.position - player.transform.position; to.y = 0;
                            float d = to.magnitude;
                            input.AimPoint = target.transform.position;
                            float reach = target.GetComponent<BossBrain>() != null ? 1f : 0f;
                            input.Move = d > 2.6f + reach ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                            input.AttackHeld = d < 3.6f + reach;
                            if (d < 4.5f + reach && player.AnySkillReady) input.PressSkill();
                            if (player.QuintessenceReady) input.PressQuintessence();
                        }
                        else if (director.Busy) input.Move = Vector2.zero;
                        else if (director.Interactables.Count > 0)
                        {
                            // primeiro o que a sala oferece (bau, bencao, santuario, um item); depois a primeira porta
                            var pick = director.Interactables.FirstOrDefault(i => !i.Prompt.StartsWith("entrar"));
                            if (pick == null || tried >= 2) pick = director.Interactables.FirstOrDefault(i => i.Prompt.StartsWith("entrar")) ?? pick;
                            var goal = pick.Position;
                            var to = goal - player.transform.position; to.y = 0;
                            float d = to.magnitude;
                            input.AimPoint = goal;
                            input.Move = d > 1.4f ? new Vector2(to.x / d, to.z / d) : Vector2.zero;
                            if (d < 2.2f && sinceInteract > 0.5f) { sinceInteract = 0f; if (!pick.Prompt.StartsWith("entrar")) tried++; pick.Use(); }
                        }
                        else input.Move = Vector2.zero;
                        Unstick(dt);
                        yield return null;
                    }
                    GameEvents.TelegraphStarted -= ConsiderDodge;
                    string line = $"{HeroSkin.NameOf(hero)} seed {GameFlow.Seed}: {(director.Victory ? "VITORIA" : director.Finished ? "caiu na sala " + (director.Run.Depth + 1) + " (" + director.Run.Current.Type + ")" : "TEMPO ESGOTADO na sala " + (director.Run.Depth + 1))}"
                        + $" | nivel {director.Run.Progress.Level} | abates {director.Run.Kills} | {clock:0}s | talentos: {string.Join(",", director.Run.Talents.Taken.Select(t => t.Id))}\n   {rooms}";
                    log.AppendLine(line);
                    Debug.Log("SONDA " + line);
                    File.WriteAllText("Logs/balance.txt", log.ToString());
                }
            GameFlow.AutoPickTalents = false;
            MetaStore.UseKey(MetaStore.DefaultKey);
            GameFlow.Layers = 10; GameFlow.StartElement = ElementType.Fire;
            Time.captureFramerate = 0;
        }

        void ConsiderDodge(TelegraphEvent e)
        {
            if (player == null || e.Friendly) return;
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
            var boss = Object.FindFirstObjectByType<BossBrain>();
            if (boss != null && !boss.GetComponent<Health>().IsDead) return boss.GetComponent<Health>();
            foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                var h = e.GetComponent<Health>();
                if (h.IsDead) continue;
                float d = Vector3.Distance(e.transform.position, player.transform.position);
                // o Xama cura os outros: e o primeiro alvo
                if (e.Definition.attack == EnemyAttackKind.Heal) d *= 0.4f;
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }

        void Unstick(float dt)
        {
            var pos = player.transform.position;
            bool trying = input.Move.sqrMagnitude > 0.2f;
            if (sidestep > 0f) { sidestep -= dt; input.Move = sideDir; }
            else if (trying && (pos - lastPos).magnitude < 0.03f)
            {
                stuck += dt;
                if (stuck > 0.35f) { stuck = 0f; sidestep = 0.55f; sideDir = (Random.value < 0.5f ? 1f : -1f) * new Vector2(-input.Move.y, input.Move.x); }
            }
            else stuck = 0f;
            lastPos = pos;
        }
    }
}
#endif
