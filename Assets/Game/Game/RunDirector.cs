using System.Collections;
using System.Collections.Generic;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Enemies;
using Quintessence.Run;
using Quintessence.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quintessence.Game
{
    /// Conduz a run dentro da cena: monta cada sala do mapa, abre as portas quando ela e resolvida,
    /// troca de sala, e encerra em vitoria (chefe) ou derrota (heroi caiu).
    public class RunDirector : MonoBehaviour
    {
        [SerializeField] GameObject meleePrefab;
        [SerializeField] GameObject rangedPrefab;
        [SerializeField] GameObject brutePrefab;
        [SerializeField] GameObject bomberPrefab;
        [SerializeField] GameObject bossPrefab;
        [SerializeField] GameObject chargerPrefab;
        [SerializeField] GameObject lobberPrefab;
        [SerializeField] GameObject shamanPrefab;
        [SerializeField] Material flatMaterial;
        [SerializeField] BoonCatalog boonCatalog;
        [Header("Pecas do cenario (modelos do pack)")]
        [SerializeField] GameObject doorPrefab;
        [Tooltip("Estandarte por tipo de sala, na ordem do enum RoomType.")]
        [SerializeField] GameObject[] bannerByRoom = new GameObject[0];
        [SerializeField] GameObject shrinePrefab;
        [SerializeField] GameObject chestPrefab;
        [SerializeField] GameObject tablePrefab;
        [SerializeField] GameObject bottlePrefab;
        [SerializeField] GameObject elixirPrefab;
        [SerializeField] GameObject shopDecorPrefab;
        [SerializeField] GameObject dummyPrefab;
        [SerializeField] Material matteMaterial;
        [Tooltip("Arranjos de cobertura das salas de combate; a run sorteia um por sala.")]
        [SerializeField] RoomLayout[] layouts = new RoomLayout[0];
        [SerializeField] Vector3 entryPoint = new(0, 0.05f, -5.5f);
        [SerializeField] float doorLine = 7.15f;
        [SerializeField] float interactRange = 2.4f;

        public RunState Run { get; private set; }
        public bool Finished { get; private set; }
        public bool Victory { get; private set; }
        public bool Busy { get; private set; }
        public IReadOnlyList<Interactable> Interactables => interactables;

        PlayerController player;
        Health playerHealth;
        WaveSpawner spawner;
        Hud hud;
        BiomeDresser dresser;
        Biome lastBiome = (Biome)(-1);
        public Biome CurrentBiome => lastBiome;
        readonly List<Interactable> interactables = new();
        float startTime, baseMaxHealth;
        bool mapByTab;
        System.Random rng;

        public class Interactable
        {
            public GameObject Object;
            public string Prompt;
            public System.Action Use;
            public Vector3 Position => Object.transform.position;
        }

        public void Bind(GameObject melee, GameObject ranged, GameObject brute, GameObject bomber, Material flat)
        {
            meleePrefab = melee; rangedPrefab = ranged; brutePrefab = brute; bomberPrefab = bomber; flatMaterial = flat;
        }

        public void BindExtraEnemies(GameObject charger, GameObject lobber, GameObject shaman)
        {
            chargerPrefab = charger; lobberPrefab = lobber; shamanPrefab = shaman;
        }

        public void BindBoons(BoonCatalog catalog) => boonCatalog = catalog;
        public void BindBoss(GameObject boss) => bossPrefab = boss;

        public void BindProps(GameObject door, GameObject[] banners, GameObject shrine, GameObject chest, Material matte)
        {
            doorPrefab = door; bannerByRoom = banners; shrinePrefab = shrine; chestPrefab = chest; matteMaterial = matte;
        }

        static readonly Color Cream = new(0.93f, 0.88f, 0.78f);

        public void BindTutorial(GameObject dummy) => dummyPrefab = dummy;
        public void BindLayouts(RoomLayout[] list) => layouts = list;
        public void BindShop(GameObject table, GameObject bottle, GameObject elixir, GameObject decor)
        {
            tablePrefab = table; bottlePrefab = bottle; elixirPrefab = elixir; shopDecorPrefab = decor;
        }

        /// Monta a loja na sala atual sem passar pelo mapa. Para a cena de teste e as capturas.
        public void ShowShop()
        {
            Cleanup();
            ApplyLayout(-1);
            SetupShop();
        }
        public int LayoutCount => layouts.Length;
        public int ShardsEarned { get; private set; }
        public EliteModifier CurrentTrait { get; private set; }
        public int HeroRescues { get; private set; }

        /// Perdeu o foco (trocou de aba ou de janela) no meio da sala: pausa em vez de deixar o heroi apanhar sozinho.
        void OnApplicationFocus(bool focused)
        {
            if (!focused && enabled && !Finished && !Busy && !GameClock.Paused && Run != null) SetPaused(true);
        }

        /// Monta um Santuario na sala atual sem passar pelo mapa. Para os testes e as capturas.
        public void ShowShrine()
        {
            Cleanup();
            ApplyLayout(-1);
            SetupShrine();
        }
        public string CurrentLayout { get; private set; } = "";
        Transform layoutRoot;

        /// Troca o arranjo da sala. index < 0 = sala vazia. Publico para a cena de teste e as capturas.
        public void ApplyLayout(int index)
        {
            if (layoutRoot != null) Destroy(layoutRoot.gameObject);
            layoutRoot = new GameObject("Layout").transform;
            CurrentLayout = "";
            if (index < 0 || index >= layouts.Length || layouts[index] == null) return;
            CurrentLayout = layouts[index].displayName;
            foreach (var p in layouts[index].pieces)
                if (p.prefab != null) Instantiate(p.prefab, p.position, Quaternion.Euler(0, p.rotation, 0), layoutRoot);
        }

        void Awake()
        {
            GameClock.SetPaused(false);
            hud = FindFirstObjectByType<Hud>();
            if (hud == null) hud = new GameObject("Hud").AddComponent<Hud>();
            dresser = gameObject.AddComponent<BiomeDresser>();
        }

        void OnEnable()
        {
            GameEvents.RoomCleared += OnRoomCleared;
            GameEvents.Died += OnDied;
        }

        void OnDisable()
        {
            GameEvents.RoomCleared -= OnRoomCleared;
            GameEvents.Died -= OnDied;
        }

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            playerHealth = player.GetComponent<Health>();
            spawner = FindFirstObjectByType<WaveSpawner>();
            if (GameFlow.Tutorial)
            {
                // treino: sem mapa nem ondas; outro diretor cuida da sala
                Run = new RunState(RunMapGenerator.Generate(1, 3, 0));
                hud.SetBoons(null);
                hud.SetFade(0f);
                gameObject.AddComponent<TutorialDirector>().Begin(player, hud, dummyPrefab);
                enabled = false;
                return;
            }
            Run = new RunState(RunMapGenerator.Generate(GameFlow.Seed, GameFlow.Layers, GameFlow.Shrines));
            player.ResetLoadout(GameFlow.StartElement);
            baseMaxHealth = playerHealth.Max;
            rng = new System.Random(GameFlow.Seed);
            hud.SetBoons(Run.Boons);
            hud.SetProgress(Run.Progress, Run.Talents);
            talentRng = new System.Random(GameFlow.Seed ^ 0x5f3759df);
            startTime = Time.time;
            // melhorias permanentes do Refugio entram antes da primeira sala
            HeroPassives.Apply(GameFlow.StartElement, player.Modifiers);
            MetaStore.Current.ApplyTo(player.Modifiers);
            ApplyMaxHealth();
            Run.AddEssence(MetaStore.Current.StartEssence);
            hud.SetEssence(Run.Essence);
            StartCoroutine(EnterRoom(Run.Current, true));
        }

        void Update()
        {
            if (Finished) return;
            if (levelUpOpen) { }
            else if (MenuInput.InventoryPressed && !Busy && (!GameClock.Paused || hud.InventoryVisible)) SetInventory(!hud.InventoryVisible);
            else if (MenuInput.PausePressed)
            {
                // Esc fecha o inventario se ele estiver aberto; senao pausa
                if (hud.InventoryVisible) SetInventory(false);
                else SetPaused(!GameClock.Paused);
            }
            // mapa enquanto segura; so fecha o que o proprio atalho abriu
            if (!GameClock.Paused && MenuInput.MapHeld != mapByTab)
            {
                mapByTab = MenuInput.MapHeld;
                hud.ShowMap(Run, mapByTab);
            }
            if (levelUpOpen) { LevelUpKeys(); hud.HidePrompt(); return; }
            if (GameClock.Paused || Busy) { hud.HidePrompt(); return; }
            if (Run.Progress.PendingPicks > 0 && RoomQuiet) { OpenLevelUp(); return; }
            // heroi fora da sala (atravessou parede, caiu): volta para a entrada
            if (!Arena.Contains(player.transform.position)) { player.Teleport(entryPoint, Vector3.forward); HeroRescues++; }

            Interactable near = null;
            float best = interactRange;
            foreach (var i in interactables)
            {
                float d = Vector3.Distance(i.Position, player.transform.position);
                if (d < best) { best = d; near = i; }
            }
            bool pressed = player.Input != null && player.Input.ConsumeInteract();
            if (near == null) { hud.HidePrompt(); return; }
            hud.ShowPrompt("E — " + near.Prompt);
            if (pressed) near.Use();
        }

        /// Abre ou fecha o inventario. O jogo para enquanto ele esta aberto.
        public void SetInventory(bool open)
        {
            GameClock.SetPaused(open);
            hud.ShowInventory(open, Run);
            if (open) hud.ShowMap(Run, false);
        }

        public void SetPaused(bool paused)
        {
            GameClock.SetPaused(paused);
            hud.ShowPause(paused, () => SetPaused(false), GameFlow.ToTitle);
            if (paused) hud.ShowMap(Run, false);
        }

        // ---------- salas ----------

        IEnumerator EnterRoom(MapNode node, bool first = false)
        {
            Busy = true;
            hud.HidePrompt();
            if (!first)
            {
                yield return Fade(0f, 1f, 0.3f);
                Run.Advance(node.Id);
            }
            Cleanup();
            ApplyLayout(LayoutPicker.UsesLayout(node.Type) ? LayoutPicker.Pick(GameFlow.Seed, node.Id, layouts.Length) : -1);
            DressBiome(Biomes.For(Run.Depth, Run.Map.LayerCount), true);
            player.Teleport(entryPoint, Vector3.forward);
            hud.SetRoom($"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · {RoomInfo.Name(node.Type)}", node.Type);
            yield return null;

            switch (node.Type)
            {
                case RoomType.Shrine:
                    SetupShrine();
                    break;
                case RoomType.Shop:
                    SetupShop();
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                case RoomType.Master:
                    if (!SetupBoons()) { Run.RoomCleared(); OpenDoors(); }
                    break;
                case RoomType.Treasure:
                    SetupTreasure();
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                case RoomType.Pact:
                    SetupPact();
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                case RoomType.Rest:
                    playerHealth.Heal(playerHealth.Max * 0.3f);
                    Sfx.Play("heal");
                    Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.5f, "DESCANSO: +30% DE VIDA", new Color(0.5f, 1f, 0.55f), 1.1f, 1.8f);
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                default:
                    StartEncounter(node.Type, Run.Depth, node.Type == RoomType.Elite ? DifficultyCurve.PickElite(GameFlow.Seed, node.Id) : EliteModifier.None);
                    break;
            }
            yield return Fade(1f, 0f, 0.35f);
            Busy = false;
        }

        /// Comeca as ondas de uma sala com a dificuldade da profundidade e, em Elite, o traco. Publico para os testes e as capturas.
        public void StartEncounter(RoomType type, int depth, EliteModifier trait)
        {
            var diff = DifficultyCurve.For(type, depth, trait);
            spawner.Scaling = new EnemyScaling { Health = diff.Health, Damage = diff.Damage, Speed = diff.Speed, Cooldown = diff.Cooldown, Size = diff.Size };
            CurrentTrait = trait;
            if (trait != EliteModifier.None)
            {
                hud.SetRoom($"Sala {depth + 1}/{Run.Map.LayerCount} · Elite: {DifficultyCurve.Name(trait)}", type);
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f,
                    $"ELITE {DifficultyCurve.Name(trait).ToUpperInvariant()}: {DifficultyCurve.Describe(trait)}", new Color(0.86f, 0.62f, 0.34f), 1f, 2.6f);
            }
            spawner.Champion = type == RoomType.Elite;
            spawner.Run(BuildWaves(EncounterPlanner.Plan(type, depth, Run != null ? Biomes.For(depth, Run.Map.LayerCount) : Biome.Ashes)));
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            // gravando quadro a quadro (testes), o relogio real nao acompanha o do jogo: o escurecimento segue o passo da captura
            for (float t = 0f; t < seconds; t += Time.captureFramerate > 0 ? 1f / Time.captureFramerate : Time.unscaledDeltaTime)
            {
                hud.SetFade(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            hud.SetFade(to);
        }

        List<Wave> BuildWaves(List<EncounterPlanner.WavePlan> plan)
        {
            var waves = new List<Wave>();
            foreach (var p in plan)
            {
                var w = new Wave();
                if (p.Melee > 0) w.entries.Add(new WaveEntry { prefab = meleePrefab, count = p.Melee });
                if (p.Ranged > 0) w.entries.Add(new WaveEntry { prefab = rangedPrefab, count = p.Ranged });
                if (p.Brute > 0 && brutePrefab != null) w.entries.Add(new WaveEntry { prefab = brutePrefab, count = p.Brute });
                if (p.Bomber > 0 && bomberPrefab != null) w.entries.Add(new WaveEntry { prefab = bomberPrefab, count = p.Bomber });
                if (p.Charger > 0 && chargerPrefab != null) w.entries.Add(new WaveEntry { prefab = chargerPrefab, count = p.Charger });
                if (p.Lobber > 0 && lobberPrefab != null) w.entries.Add(new WaveEntry { prefab = lobberPrefab, count = p.Lobber });
                if (p.Shaman > 0 && shamanPrefab != null) w.entries.Add(new WaveEntry { prefab = shamanPrefab, count = p.Shaman });
                if (p.Boss > 0) w.entries.Add(new WaveEntry { prefab = bossPrefab != null ? bossPrefab : brutePrefab, count = p.Boss });
                waves.Add(w);
            }
            return waves;
        }

        void Cleanup()
        {
            foreach (var i in interactables) if (i.Object != null) Destroy(i.Object);
            interactables.Clear();
            foreach (var z in FindObjectsByType<DamageZone>(FindObjectsSortMode.None)) Destroy(z.gameObject);
            foreach (var p in FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
            foreach (var t in FindObjectsByType<TimedLife>(FindObjectsSortMode.None)) Destroy(t.gameObject);
            foreach (var e in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) Destroy(e.gameObject);
            foreach (var b in FindObjectsByType<BossBrain>(FindObjectsSortMode.None)) Destroy(b.gameObject);
        }

        void OnRoomCleared()
        {
            if (Finished) return;
            var node = Run.Current;
            Run.RoomCleared();
            int essence = EncounterPlanner.EssenceReward(node.Type, Run.Depth);
            if (node.Reward == RewardType.Health)
            {
                playerHealth.Heal(playerHealth.Max * 0.25f);
                Sfx.Play("heal");
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.2f, "+25% DE VIDA", new Color(0.5f, 1f, 0.55f), 1f, 1.4f);
                essence /= 2;
            }
            if (player.Modifiers.HealOnRoomClear > 0f) playerHealth.Heal(playerHealth.Max * player.Modifiers.HealOnRoomClear);
            Run.AddEssence(Mathf.RoundToInt(essence * player.Modifiers.EssenceMult));
            hud.SetEssence(Run.Essence);
            if (node.Type == RoomType.Boss) { StartCoroutine(Finish(true)); return; }
            if (node.Reward == RewardType.Boon && SetupBoons()) return; // as portas abrem depois de escolher
            OpenDoors();
        }

        void OnDied(Health target, DamageInfo info)
        {
            if (target != playerHealth && target.Team == Team.Enemy && Run != null && !Finished
                && (target.GetComponent<EnemyBrain>() != null || target.GetComponent<BossBrain>() != null)) GrantKillXp(target);
            if (target == playerHealth && !Finished) StartCoroutine(Finish(false));
        }

        IEnumerator Finish(bool victory)
        {
            Finished = true;
            Victory = victory;
            hud.HidePrompt();
            hud.ShowMap(Run, false);
            yield return new WaitForSecondsRealtime(victory ? 1.6f : 1.2f);
            int seconds = Mathf.RoundToInt(Time.time - startTime);
            int shards = MetaStore.Current.EndRun(Run.RoomsCleared, victory);
            MetaStore.Save();
            ShardsEarned = shards;
            string stats = $"{HeroPassives.HeroName(GameFlow.StartElement)}   ·   Nível {Run.Progress.Level}   ·   {Run.Kills} inimigos derrotados   ·   {Run.Talents.Count} talentos e {Run.Boons.Count} bênçãos"
                + $"\nSalas vencidas: {Run.RoomsCleared}   ·   Essência: {Run.Essence}   ·   Elementos: {player.Loadout.Awakened.Count}/4   ·   Tempo: {seconds / 60}:{seconds % 60:00}"
                + $"\n+{shards} Fragmentos para o Refúgio (total: {MetaStore.Current.Shards})";
            Sfx.Play(victory ? "victory" : "defeat");
            hud.ShowResult(victory, stats, GameFlow.RestartRun, GameFlow.ToTitle);
        }

        // ---------- trechos da run ----------

        /// Troca o clima da sala para o trecho. announce: mostra o letreiro se o trecho mudou.
        public void DressBiome(Biome biome, bool announce = false)
        {
            if (dresser != null) dresser.Apply(biome);
            if (biome == lastBiome) return;
            lastBiome = biome;
            if (!announce) return;
            string sub = biome == Biome.Heart ? "O fim da descida" : "Ato " + Biomes.Roman(Biomes.Act(biome));
            hud.ShowBanner(Biomes.Name(biome).ToUpperInvariant(), sub, Color.Lerp(dresser != null ? dresser.CrackColor : Cream, Cream, 0.55f));
        }

        // ---------- tesouro e pacto ----------

        /// Monta a sala de Tesouro sem passar pelo mapa. Para os testes e as capturas.
        public void ShowTreasure() { Cleanup(); ApplyLayout(-1); SetupTreasure(); }
        /// Monta a sala de Pacto sem passar pelo mapa. Para os testes e as capturas.
        public void ShowPact() { Cleanup(); ApplyLayout(-1); SetupPact(); }

        void SetupTreasure()
        {
            int amount = Mathf.RoundToInt((30 + 6 * Run.Depth) * player.Modifiers.EssenceMult);
            var gold = new Color(0.88f, 0.74f, 0.42f);
            var root = new GameObject("Treasure");
            root.transform.position = new Vector3(0, 0, 0.6f);
            if (chestPrefab != null)
            {
                var chest = Instantiate(chestPrefab, root.transform).transform;
                chest.localPosition = Vector3.zero;
                chest.localRotation = Quaternion.Euler(0, 180, 0);
                chest.localScale *= 1.25f;
            }
            Medal(root.transform, new Vector3(0, 2.1f, 0), "essence", gold, 1.2f);
            Label(root.transform, new Vector3(0, 3.6f, 0), "Tesouro", Cream, 0.9f);
            Label(root.transform, new Vector3(0, 2.95f, 0), $"{amount} de Essência e um gole de vida", new Color(0.8f, 0.76f, 0.7f), 0.55f);
            Glow(root.transform, new Vector3(0, 1.8f, -0.8f), gold, 2.2f);
            Interactable entry = null;
            entry = new Interactable
            {
                Object = root, Prompt = "abrir o baú",
                Use = () =>
                {
                    Sfx.Play("pickup");
                    Run.AddEssence(amount);
                    hud.SetEssence(Run.Essence);
                    playerHealth.Heal(playerHealth.Max * 0.15f);
                    Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, $"+{amount} DE ESSÊNCIA", gold, 1.2f, 1.6f);
                    interactables.Remove(entry);
                    Destroy(root);
                },
            };
            interactables.Add(entry);
        }

        public int PactsTaken { get; private set; }

        /// Pacto: um ganho forte em troca de um custo permanente. Da para recusar: as portas ja estao abertas.
        void SetupPact()
        {
            var wine = new Color(0.72f, 0.3f, 0.42f);
            var offers = new (string name, string gain, string cost, string icon, System.Func<bool> take)[]
            {
                ("Pacto de Sangue", "+25% de dano em tudo", "−20% de vida máxima", "boon_damage", () =>
                {
                    baseMaxHealth *= 0.8f;
                    player.Modifiers.DamageAll += 0.25f;
                    ApplyMaxHealth();
                    return true;
                }),
                ("Pacto de Vidro", "Skills recarregam 25% mais rápido", "Você recebe +25% de dano", "boon_skill", () =>
                {
                    player.Modifiers.SkillCooldownMult *= 0.75f;
                    playerHealth.Pool.CurseMultiplier *= 1.25f;
                    return true;
                }),
                ("Pacto da Ganância", "Um talento agora", "Entrega toda a Essência (mínimo 30)", "essence", () =>
                {
                    if (Run.Essence < 30) return false;
                    Run.TrySpend(Run.Essence);
                    hud.SetEssence(Run.Essence);
                    Run.Progress.GrantPick();
                    return true;
                }),
            };
            for (int i = 0; i < offers.Length; i++)
            {
                var o = offers[i];
                var root = new GameObject("Pact_" + i);
                root.transform.position = new Vector3((i - (offers.Length - 1) / 2f) * 5.4f, 0, 0.2f);
                if (shrinePrefab != null) Instantiate(shrinePrefab, root.transform).transform.localPosition = Vector3.zero;
                Medal(root.transform, new Vector3(0, 2.25f, 0), o.icon, wine, 1.1f);
                Label(root.transform, new Vector3(0, 4.35f, 0), o.name, Cream, 0.8f);
                Label(root.transform, new Vector3(0, 3.7f, 0), Wrap(o.gain, 22), new Color(0.62f, 0.82f, 0.6f), 0.55f);
                Label(root.transform, new Vector3(0, 3.15f, 0), Wrap(o.cost, 24), new Color(0.85f, 0.45f, 0.4f), 0.55f);
                Glow(root.transform, new Vector3(0, 2f, -0.8f), wine, 1.8f);
                interactables.Add(new Interactable
                {
                    Object = root, Prompt = "selar o " + o.name,
                    Use = () =>
                    {
                        if (!o.take())
                        {
                            Sfx.Play("block");
                            Feel.DamageNumber.Spawn(root.transform.position + Vector3.up * 4.8f, "ESSÊNCIA INSUFICIENTE", new Color(0.8f, 0.4f, 0.35f), 0.8f, 1f);
                            return;
                        }
                        PactsTaken++;
                        Sfx.Play("pickup");
                        Run.AddBoon(new Boon { id = "pact_" + PactsTaken + "_" + o.icon, name = o.name, description = o.gain + " · " + o.cost, patron = ElementType.None, stat = BoonStat.SwitchStrike });
                        hud.SetBoons(Run.Boons);
                        Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, o.name.ToUpperInvariant() + " SELADO", wine, 1.2f, 1.8f);
                        // um pacto por sala: os outros altares somem, as portas ficam
                        for (int k = interactables.Count - 1; k >= 0; k--)
                        {
                            if (interactables[k].Object == null || !interactables[k].Object.name.StartsWith("Pact_")) continue;
                            Destroy(interactables[k].Object);
                            interactables.RemoveAt(k);
                        }
                    },
                });
            }
        }

        // ---------- nivel e talentos ----------

        System.Random talentRng;
        bool levelUpOpen;
        List<Talent> talentOffer;
        public bool LevelUpOpen => levelUpOpen;
        public IReadOnlyList<Talent> TalentOffer => talentOffer;

        /// Sem inimigo vivo na sala: a escolha de talento nunca interrompe a luta.
        bool RoomQuiet => spawner == null || spawner.State == null || spawner.State.Alive <= 0;

        void GrantKillXp(Health enemy)
        {
            var brain = enemy.GetComponent<EnemyBrain>();
            bool boss = enemy.GetComponent<BossBrain>() != null;
            float baseHealth = brain != null && brain.Definition != null ? brain.Definition.maxHealth : enemy.Max;
            Run.Kills++;
            bool champion = enemy.GetComponent<Champion>() != null;
            if (champion) { Run.AddEssence(Mathf.RoundToInt(15 * player.Modifiers.EssenceMult)); hud.SetEssence(Run.Essence); }
            int levels = Run.Progress.AddXp(Mathf.RoundToInt(Progression.XpForEnemy(baseHealth, boss) * (champion ? 3 : 1) * player.Modifiers.XpMult));
            if (levels <= 0) return;
            Sfx.Play("pickup");
            Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.9f, "NÍVEL " + Run.Progress.Level + "!", new Color(0.9f, 0.76f, 0.42f), 1.3f, 1.8f);
        }

        void OpenLevelUp()
        {
            if (talentRng == null) talentRng = new System.Random(GameFlow.Seed);
            talentOffer = TalentCatalog.Offer(Run.Talents, player.Loadout.Awakened, talentRng);
            if (talentOffer.Count == 0) { while (Run.Progress.ConsumePick()) { } return; }
            if (GameFlow.AutoPickTalents) { PickTalent(0); return; }
            levelUpOpen = true;
            GameClock.SetPaused(true);
            hud.ShowMap(Run, false);
            hud.ShowLevelUp(Run.Progress.Level - Run.Progress.PendingPicks + 1, talentOffer, PickTalent);
        }

        void LevelUpKeys()
        {
            var kb = Keyboard.current;
            if (kb == null || talentOffer == null) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) PickTalent(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) PickTalent(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) PickTalent(2);
        }

        /// Pega o talento de indice index da oferta atual. Publico para os testes.
        public void PickTalent(int index)
        {
            if (talentOffer == null || index < 0 || index >= talentOffer.Count) return;
            var t = talentOffer[index];
            talentOffer = null;
            Run.Progress.ConsumePick();
            Run.Talents.Take(t, player.Modifiers);
            ApplyMaxHealth();
            Sfx.Play("pickup");
            if (levelUpOpen)
            {
                levelUpOpen = false;
                hud.ShowLevelUp(0, null, null);
                GameClock.SetPaused(false);
                // a tecla que escolheu o talento nao pode virar skill no quadro seguinte
                if (player.Input != null) { player.Input.ConsumeElementSkill(); player.Input.ConsumeSkill(); }
            }
            var c = t.Element == ElementType.None ? new Color(0.9f, 0.76f, 0.42f) : ElementColors.Of(t.Element);
            Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, t.Name.ToUpperInvariant() + "!", c, 1.1f, 1.5f);
        }

        // ---------- portas e santuario ----------

        void OpenDoors()
        {
            var choices = Run.Choices;
            for (int i = 0; i < choices.Count; i++)
            {
                var node = choices[i];
                float x = (i - (choices.Count - 1) / 2f) * 6f;
                string reward = RoomInfo.Name(node.Reward);
                string kind = RoomInfo.Name(node.Type);
                if (node.Type == RoomType.Elite) kind += ": " + DifficultyCurve.Name(DifficultyCurve.PickElite(GameFlow.Seed, node.Id));
                string label = kind + (reward.Length > 0 ? "\n" + reward : "");
                var go = BuildDoor(new Vector3(x, 0, doorLine), node.Type, label);
                interactables.Add(new Interactable
                {
                    Object = go, Prompt = "entrar: " + RoomInfo.Name(node.Type),
                    Use = () => { if (!Busy) { Sfx.Play("door"); StartCoroutine(EnterRoom(node)); } },
                });
            }
        }

        /// Porta: vao de pedra do pack encostado na parede do fundo, com o estandarte do tipo da sala em cima.
        GameObject BuildDoor(Vector3 pos, RoomType type, string label)
        {
            var root = new GameObject("Door");
            root.transform.position = pos;
            if (doorPrefab != null) Instantiate(doorPrefab, root.transform).transform.localPosition = Vector3.zero;
            // fundo escuro do vao
            Matte(GameObject.CreatePrimitive(PrimitiveType.Cube), root.transform, new Vector3(0, 1.45f, 0.35f), new Vector3(1.9f, 2.9f, 0.1f), new Color(0.03f, 0.03f, 0.05f));
            int t = (int)type;
            if (t < bannerByRoom.Length && bannerByRoom[t] != null)
            {
                var banner = Instantiate(bannerByRoom[t], root.transform);
                banner.transform.localPosition = new Vector3(0, 3.55f, -0.42f);
            }
            Label(root.transform, new Vector3(0, 1.9f, -1.5f), label, Cream, 0.62f);
            Glow(root.transform, new Vector3(0, 2.2f, -1.4f), new Color(1f, 0.82f, 0.6f), 1.5f);
            return root;
        }

        void SetupShrine()
        {
            var missing = new List<ElementType>();
            foreach (var e in Elements.All)
            {
                bool has = false;
                foreach (var a in player.Loadout.Awakened) if (a == e) has = true;
                if (!has) missing.Add(e);
            }
            if (missing.Count == 0)
            {
                Run.AddEssence(20);
                hud.SetEssence(Run.Essence);
                Run.RoomCleared();
                OpenDoors();
                return;
            }
            string[] names = { "", "Fogo", "Água", "Terra", "Ar" };
            for (int i = 0; i < missing.Count; i++)
            {
                var e = missing[i];
                var c = ElementColors.Of(e);
                var root = new GameObject("Shrine_" + e);
                root.transform.position = new Vector3((i - (missing.Count - 1) / 2f) * 4.5f, 0, 0f);
                float top = 1.25f;
                if (shrinePrefab != null) Instantiate(shrinePrefab, root.transform).transform.localPosition = Vector3.zero;
                else Matte(GameObject.CreatePrimitive(PrimitiveType.Cube), root.transform, new Vector3(0, 0.6f, 0), new Vector3(1f, 1.2f, 1f), new Color(0.4f, 0.4f, 0.45f));
                // o elemento aparece como uma pedra fosca sobre o pedestal e na luz que ela joga em volta
                Matte(GameObject.CreatePrimitive(PrimitiveType.Sphere), root.transform, new Vector3(0, top + 0.45f, 0), Vector3.one * 0.7f, c);
                Label(root.transform, new Vector3(0, top + 1.7f, 0), names[(int)e], Cream, 0.8f);
                Glow(root.transform, new Vector3(0, top + 0.7f, -0.7f), c, 2.2f);
                interactables.Add(new Interactable
                {
                    Object = root, Prompt = "despertar " + names[(int)e],
                    Use = () =>
                    {
                        if (!player.Awaken(e)) return;
                        Sfx.Play("pickup");
                        Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, names[(int)e].ToUpperInvariant() + " DESPERTO!", c, 1.3f, 1.8f);
                        foreach (var it in interactables) if (it.Object != null) Destroy(it.Object);
                        interactables.Clear();
                        Run.RoomCleared();
                        OpenDoors();
                    },
                });
            }
        }

        /// Loja: cada item numa mesa do pack, com nome, efeito e preco. Da para sair sem comprar: as portas ja estao abertas.
        Merchant merchant;
        readonly List<(TextMesh label, int price)> priceTags = new();
        static readonly Color CanPay = new(0.88f, 0.74f, 0.42f), CannotPay = new(0.78f, 0.36f, 0.32f);

        /// Preco em dourado se a Essencia paga, em vermelho se nao. Refaz depois de cada compra.
        public void RefreshPrices()
        {
            foreach (var (label, price) in priceTags)
                if (label != null) label.color = Run.Essence >= price ? CanPay : CannotPay;
        }

        void SetupShop()
        {
            merchant = null;
            priceTags.Clear();
            var catalog = boonCatalog != null ? boonCatalog.boons : new List<Boon>();
            var stock = ShopPlanner.Stock(catalog, Run.BoonIds, player.Loadout.Awakened, rng, Run.Depth);
            // banca do mercador: prateleiras, balcao, ouro e o proprio (tudo do pack), some junto com o layout da sala
            if (shopDecorPrefab != null)
            {
                if (layoutRoot == null) layoutRoot = new GameObject("Layout").transform;
                var decor = Instantiate(shopDecorPrefab, layoutRoot);
                var stall = decor.transform.Find("Merchant");
                if (stall != null)
                {
                    Label(stall, new Vector3(0, 3.1f, 0), "MERCADOR", new Color(0.88f, 0.74f, 0.42f), 0.7f);
                    merchant = stall.GetComponent<Merchant>();
                    if (merchant != null) merchant.Bind(player.transform);
                }
            }
            for (int i = 0; i < stock.Count; i++)
            {
                var item = stock[i];
                var c = item.Kind == ShopItemKind.Boon && item.Boon.patron != ElementType.None ? ElementColors.Of(item.Boon.patron)
                    : item.Kind == ShopItemKind.Boon ? new Color(0.85f, 0.72f, 0.4f) : new Color(0.45f, 0.7f, 0.45f);
                var root = new GameObject("Shop_" + item.Name);
                root.transform.position = new Vector3(2.7f + (i - (stock.Count - 1) / 2f) * 3.1f, 0, -1.4f);
                float top = 0.9f;
                if (tablePrefab != null)
                {
                    var table = Instantiate(tablePrefab, root.transform);
                    table.transform.localPosition = Vector3.zero;
                    var rs = table.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0) { var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds); top = b.max.y; }
                }
                if (item.Kind == ShopItemKind.Boon)
                    Medal(root.transform, new Vector3(0, top + 0.75f, 0), BoonEffects.Icon(item.Boon.stat), c, 1.25f);
                else
                {
                    var flask = item.Kind == ShopItemKind.MaxHealth && elixirPrefab != null ? elixirPrefab : bottlePrefab;
                    if (flask != null) Instantiate(flask, root.transform).transform.localPosition = new Vector3(0, top, 0);
                }
                Label(root.transform, new Vector3(0, top + 2.8f, 0), Wrap(item.Name, 13), Cream, 0.72f);
                Label(root.transform, new Vector3(0, top + 2.1f, 0), Wrap(item.Description, 16), new Color(0.8f, 0.76f, 0.7f), 0.5f);
                priceTags.Add((Label(root.transform, new Vector3(0, top + 1.6f, 0), item.Price + " de Essência", CanPay, 0.62f), item.Price));
                Glow(root.transform, new Vector3(0, top + 0.8f, -0.7f), c, 1.1f);
                Interactable entry = null;
                entry = new Interactable
                {
                    Object = root, Prompt = $"comprar {item.Name} ({item.Price})",
                    Use = () =>
                    {
                        if (!Run.TrySpend(item.Price))
                        {
                            Sfx.Play("block");
                            Feel.DamageNumber.Spawn(root.transform.position + Vector3.up * 3.6f, "ESSÊNCIA INSUFICIENTE", new Color(0.8f, 0.4f, 0.35f), 0.8f, 1f);
                            if (merchant != null) merchant.Refuse();
                            return;
                        }
                        Sfx.Play("pickup");
                        hud.SetEssence(Run.Essence);
                        switch (item.Kind)
                        {
                            case ShopItemKind.Heal: playerHealth.Heal(playerHealth.Max * ShopPlanner.HealFraction); break;
                            case ShopItemKind.MaxHealth: player.Modifiers.MaxHealthAdd += ShopPlanner.MaxHealthGain; ApplyMaxHealth(); break;
                            case ShopItemKind.Boon: GrantBoon(item.Boon); break;
                        }
                        Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, item.Name.ToUpperInvariant() + "!", c, 1.1f, 1.4f);
                        interactables.Remove(entry);
                        Destroy(root);
                        if (merchant != null) merchant.Thank();
                        RefreshPrices();
                    },
                };
                interactables.Add(entry);
            }
            RefreshPrices();
        }

        /// O mercador da loja atual, se houver (para os testes).
        public Merchant ShopMerchant => merchant;

        void ApplyMaxHealth()
        {
            float max = Mathf.Max(20f, baseMaxHealth + player.Modifiers.MaxHealthAdd);
            if (max < playerHealth.Max - 0.01f) { playerHealth.Pool.SetMax(max); return; }
            if (max <= playerHealth.Max) return;
            float gain = max - playerHealth.Max;
            playerHealth.Pool.SetMax(max);
            playerHealth.Heal(gain);
        }

        void GrantBoon(Boon boon)
        {
            BoonEffects.Apply(player.Modifiers, boon);
            Run.AddBoon(boon);
            ApplyMaxHealth();
            hud.SetBoons(Run.Boons);
        }

        /// Tres bencaos para escolher uma. Devolve false se nao sobrou nenhuma para oferecer.
        bool SetupBoons()
        {
            if (boonCatalog == null) return false;
            var offer = BoonPicker.Offer(boonCatalog.boons, Run.BoonIds, player.Loadout.Awakened, rng);
            if (offer.Count == 0) return false;
            for (int i = 0; i < offer.Count; i++)
            {
                var boon = offer[i];
                var c = boon.patron == ElementType.None ? new Color(1f, 0.85f, 0.4f) : ElementColors.Of(boon.patron);
                var root = new GameObject("Boon_" + boon.id);
                root.transform.position = new Vector3((i - (offer.Count - 1) / 2f) * 5.6f, 0, -0.5f);
                // bau do pack com uma pedra fosca na cor do mestre que oferece
                if (chestPrefab != null)
                {
                    // a frente do modelo aponta para o fundo da sala: vira para a camera
                    var chest = Instantiate(chestPrefab, root.transform).transform;
                    chest.localPosition = Vector3.zero;
                    chest.localRotation = Quaternion.Euler(0, 180, 0);
                }
                Medal(root.transform, new Vector3(0, 1.8f, 0), BoonEffects.Icon(boon.stat), c, 1.15f);
                Label(root.transform, new Vector3(0, 3.5f, 0), boon.name, Cream, 0.9f);
                Label(root.transform, new Vector3(0, 2.7f, 0), Wrap(boon.description, 20), new Color(0.8f, 0.76f, 0.7f), 0.6f);
                Glow(root.transform, new Vector3(0, 1.9f, -0.7f), c, 1.8f);
                interactables.Add(new Interactable
                {
                    Object = root, Prompt = "aceitar " + boon.name,
                    Use = () =>
                    {
                        Sfx.Play("pickup");
                        GrantBoon(boon);
                        Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, boon.name.ToUpperInvariant() + "!", c, 1.2f, 1.6f);
                        foreach (var it in interactables) if (it.Object != null) Destroy(it.Object);
                        interactables.Clear();
                        if (Run.Current.Type == RoomType.Master) Run.RoomCleared();
                        OpenDoors();
                    },
                });
            }
            return true;
        }

        static string Wrap(string text, int width)
        {
            var sb = new System.Text.StringBuilder();
            int line = 0;
            foreach (var word in text.Split(' '))
            {
                if (line > 0 && line + word.Length > width) { sb.Append('\n'); line = 0; }
                else if (line > 0) { sb.Append(' '); line++; }
                sb.Append(word);
                line += word.Length;
            }
            return sb.ToString();
        }

        Material Tinted(Color c) => new(flatMaterial) { color = c };

        /// Peca de cor chapada mas iluminada pela cena (fosca), como os modelos do pack.
        Transform Matte(GameObject go, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var m = new Material(matteMaterial != null ? matteMaterial : flatMaterial);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            go.GetComponent<Renderer>().material = m;
            return go.transform;
        }

        /// Medalhao com o icone do efeito, virado para a camera: disco escuro, aro e desenho na cor do mestre.
        void Medal(Transform parent, Vector3 localPos, string icon, Color color, float size = 0.95f)
        {
            var root = new GameObject("Medal").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            root.gameObject.AddComponent<FaceCamera>();
            void Layer(Sprite sprite, Color tint, float width, int order)
            {
                var sr = new GameObject("Layer" + order).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root, false);
                sr.sprite = sprite; sr.color = tint; sr.sortingOrder = order;
                sr.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
            }
            Layer(UI.UiKit.Circle, new Color(0.1f, 0.09f, 0.13f), size, 0);
            Layer(UI.UiKit.Ring, Color.Lerp(color, Cream, 0.2f), size, 1);
            Layer(UI.UiKit.Icon(icon), Color.Lerp(color, Cream, 0.55f), size * 0.66f, 2);
        }

        Transform Block(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = Tinted(color);
            return go.transform;
        }

        void Glow(Transform parent, Vector3 localPos, Color color, float intensity = 3f)
        {
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = localPos;
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = 5.5f;
        }

        TextMesh Label(Transform parent, Vector3 localPos, string text, Color color, float scale = 1f)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<TextMesh>();
            t.font = GameFonts.Display;
            t.text = text;
            t.fontSize = 80;
            t.characterSize = 0.075f * scale;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = GameFonts.Display.material;
            go.AddComponent<FaceCamera>();
            return t;
        }
    }
}
