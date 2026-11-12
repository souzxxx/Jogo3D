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
            // o roteiro desvia de todo aviso com reflexo perfeito, mas joga sem estrategia: dano cheio por padrao
            float taken = ProbeTaken;
            GameFlow.AutoPickTalents = true;
            MetaStore.UseKey("quintessence.meta.test");
            RunSaveStore.UseKey("quintessence.run.probe");
            Tips.UseKey("quintessence.tips.probe");
            PlayerPrefs.DeleteKey("quintessence.meta.test");
            foreach (var hero in Elements.All)
                for (int seed = 1; seed <= ProbeSeeds; seed++)
                {
                    if (OnlyHero != ElementType.None && (hero != OnlyHero || seed != OnlySeed)) continue;
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
                    var damageBy = new System.Collections.Generic.Dictionary<string, float>();
                    GameEvents.DamageDealt += (t, info, applied) =>
                    {
                        if (t != hp) return;
                        string who = RunDirector.KillerName(info.Source);
                        if (who.Length == 0) who = info.Source != null ? info.Source.name.Replace("(Clone)", "") : "status";
                        damageBy[who] = (damageBy.TryGetValue(who, out float v) ? v : 0f) + applied;
                    };
                    GameEvents.BossShieldChanged += (up, _) => { if (!up && lastBoss != null && !lastBoss.GetComponent<Health>().IsDead) shieldBreaks++; };
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
                        // sala que nao termina: anota quem ficou vivo e onde, e desiste da run
                        if (roomTime > 240f && stuckReport.Length == 0)
                        {
                            var alive = Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e => !e.GetComponent<Health>().IsDead)
                                .Select(e => { var ag = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); return $"{e.Definition.displayName}@{e.transform.position.x:0.0},{e.transform.position.z:0.0} navmesh={ag.isOnNavMesh} fase={e.Phase} vida={e.GetComponent<Health>().Current:0}"; });
                            var sp = Object.FindFirstObjectByType<WaveSpawner>();
                            stuckReport = $"PRESO sala {director.Run.Depth + 1} layout '{director.CurrentLayout}' heroi@{player.transform.position.x:0.0},{player.transform.position.z:0.0} vivos(state)={sp?.State?.Alive} resgates={sp?.StallRescues}: " + string.Join(" | ", alive);
                            Debug.Log("SONDA " + stuckReport);
                            break;
                        }
                        var target = Nearest();
                        input.AttackHeld = false;
                        // o escurecimento da troca de sala corre em tempo real e a sonda em tempo de jogo: o roteiro luta mesmo durante ele
                        var inPool = Object.FindObjectsByType<CorruptionPool>(FindObjectsSortMode.None).FirstOrDefault(pl => pl.Contains(player.transform.position));
                        if (dodgeLeft > 0f) { dodgeLeft -= dt; input.Move = new Vector2(dodgeDir.x, dodgeDir.z); }
                        else if (inPool != null)
                        {
                            // como uma pessoa: sai da poca pelo lado mais perto, de costas para a parede de tras
                            var dir = inPool.ExitDirection(player.transform.position);
                            input.Move = new Vector2(dir.x, dir.z);
                        }
                        else if (target != null)
                        {
                            Fight(target);
                            // nao entra numa poca para perseguir: espera na borda e usa as skills de longe
                            var pools = Object.FindObjectsByType<CorruptionPool>(FindObjectsSortMode.None);
                            var next = player.transform.position + new Vector3(input.Move.x, 0, input.Move.y) * 0.8f;
                            if (pools.Any(pl => pl.Contains(next))) input.Move = Vector2.zero;
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
                            input.Move = d > 1.4f ? SteerTo(goal) : Vector2.zero;
                            if (d < 2.2f && sinceInteract > 0.5f) { sinceInteract = 0f; if (!pick.Prompt.StartsWith("entrar")) tried++; pick.Use(); }
                        }
                        else input.Move = Vector2.zero;
                        Unstick(dt);
                        yield return null;
                    }
                    GameEvents.TelegraphStarted -= ConsiderDodge;
                    string bossInfo = lastBoss == null ? "" : $" | chefe {lastBossName} fase {lastBossPhase + 1} vida {lastBossHp * 100f:0}% quebras {shieldBreaks}";
                    lastBoss = null; shieldBreaks = 0;
                    if (stuckReport.Length > 0) { log.AppendLine(stuckReport); stuckReport = ""; }
                    string line = $"{HeroSkin.NameOf(hero)} seed {GameFlow.Seed}: {(director.Victory ? "VITORIA" : director.Finished ? "caiu na sala " + (director.Run.Depth + 1) + " (" + director.Run.Current.Type + ")" : "TEMPO ESGOTADO na sala " + (director.Run.Depth + 1))}"
                        + bossInfo + $" | dano: {string.Join(", ", damageBy.OrderByDescending(kv => kv.Value).Take(5).Select(kv => $"{kv.Key} {kv.Value:0}"))}" + $" | nivel {director.Run.Progress.Level} | abates {director.Run.Kills} | {clock:0}s | talentos: {string.Join(",", director.Run.Talents.Taken.Select(t => t.Id))}\n   {rooms}";
                    log.AppendLine(line);
                    Debug.Log("SONDA " + line);
                    File.WriteAllText("Logs/balance.txt", log.ToString());
                }
            GameFlow.AutoPickTalents = false;
            MetaStore.UseKey(MetaStore.DefaultKey);
            RunSaveStore.Clear();
            RunSaveStore.UseKey(RunSaveStore.DefaultKey);
            Tips.UseKey(Tips.DefaultKey);
            GameFlow.Layers = 10; GameFlow.StartElement = ElementType.Fire;
            Time.captureFramerate = 0;
        }

        /// Quanto do dano o roteiro leva e quantas sementes por heroi. Mudar aqui para comparar.
        public static float ProbeTaken = 1f;
        public static int ProbeSeeds = 3;
        /// Para investigar uma run so (None = todas).
        public static ElementType OnlyHero = ElementType.None;
        public static int OnlySeed = 1;
        string stuckReport = "";

        BossBrain lastBoss;
        string lastBossName = "";
        int lastBossPhase, shieldBreaks;
        float lastBossHp;

        /// Luta como um jogador razoavel: marca com uma skill, detona com a de outro elemento,
        /// enche a Quintessencia nos reforcos quando o chefe esta de escudo e solta o R para quebra-lo.
        void Fight(Health target)
        {
            var boss = target.GetComponent<BossBrain>();
            if (boss != null)
            {
                lastBoss = boss; lastBossName = boss.Definition.displayName; lastBossPhase = boss.Phase;
                lastBossHp = target.Current / target.Max;
            }
            var to = target.transform.position - player.transform.position; to.y = 0;
            float d = to.magnitude;
            input.AimPoint = target.transform.position;
            float reach = boss != null ? 1f : 0f;
            input.Move = d > 2.6f + reach ? SteerTo(target.transform.position) : Vector2.zero;
            input.AttackHeld = d < 3.6f + reach;
            if (player.QuintessenceReady) input.PressQuintessence();
            if (d > 4.5f + reach) return;
            // skill: se o alvo ja tem marca, detona com outro elemento; senao marca com qualquer pronta
            var mark = target.TryGetComponent(out ElementMark m) && m.State.HasMark ? m.State.Element : ElementType.None;
            ElementType pick = ElementType.None;
            foreach (var e in player.Loadout.Awakened)
            {
                if (player.SkillCooldown01(e) > 0f) continue;
                if (mark != ElementType.None && e == mark) continue;
                pick = e;
                if (mark != ElementType.None) break; // detonar vem primeiro
            }
            if (pick != ElementType.None) input.PressElementSkill((int)pick);
        }

        static readonly UnityEngine.AI.NavMeshPath navPath = new();

        /// Anda ate goal contornando barreiras pelo NavMesh (como uma pessoa daria a volta).
        Vector2 SteerTo(Vector3 goal)
        {
            var from = player.transform.position;
            var straight = goal - from; straight.y = 0f;
            var fallback = straight.sqrMagnitude > 0.01f ? new Vector2(straight.x, straight.z).normalized : Vector2.zero;
            if (!UnityEngine.AI.NavMesh.SamplePosition(from, out var a, 2f, UnityEngine.AI.NavMesh.AllAreas)) return fallback;
            if (!UnityEngine.AI.NavMesh.SamplePosition(goal, out var b, 3f, UnityEngine.AI.NavMesh.AllAreas)) return fallback;
            if (!UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, UnityEngine.AI.NavMesh.AllAreas, navPath) || navPath.corners.Length < 2) return fallback;
            for (int i = 1; i < navPath.corners.Length; i++)
            {
                var d = navPath.corners[i] - from; d.y = 0f;
                if (d.sqrMagnitude > 0.09f) return new Vector2(d.x, d.z).normalized;
            }
            return fallback;
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
            if (boss != null && !boss.GetComponent<Health>().IsDead)
            {
                // escudo que pede Quintessencia: enquanto ela nao esta ativa, reacoes nos reforcos enchem a barra
                bool waitShield = boss.Shield.Up && boss.Shield.NeedsQuintessence && !player.QuintessenceActive;
                if (!waitShield) return boss.GetComponent<Health>();
            }
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
