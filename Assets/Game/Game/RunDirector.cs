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
        [SerializeField] GameObject swarmPrefab;
        [SerializeField] GameObject emberPrefab;
        [SerializeField] GameObject sentinelPrefab;
        [SerializeField] GameObject guardPrefab;
        [SerializeField] GameObject drownedPrefab;
        [SerializeField] GameObject puddlePrefab;
        [SerializeField] GameObject miniBossPrefab;
        [Tooltip("Segundo chefe final; a run sorteia entre ele e o Guardiao pela semente.")]
        [SerializeField] GameObject weaverPrefab;
        [SerializeField] GameObject lobberPrefab;
        [SerializeField] GameObject shamanPrefab;
        [SerializeField] Material flatMaterial;
        [SerializeField] BoonCatalog boonCatalog;
        [Header("Pecas do cenario (modelos do pack)")]
        [SerializeField] GameObject doorPrefab;
        [Tooltip("Estandarte por tipo de sala, na ordem do enum RoomType.")]
        [SerializeField] GameObject[] bannerByRoom = new GameObject[0];
        [SerializeField] GameObject shrinePrefab;
        [SerializeField] GameObject pactAltarPrefab;
        /// Altar proprio do Pacto (ArtSource/Pact); sem ele, o Pacto usa a coluna do Santuario.
        public void BindPactAltar(GameObject altar) => pactAltarPrefab = altar;
        /// Emblemas modelados de Fogo, Agua, Terra e Ar (nessa ordem) que ficam sobre os totens no lugar das esferas.
        [SerializeField] GameObject[] emblems;
        [SerializeField] GameObject chestPrefab;
        [SerializeField] GameObject tablePrefab;
        [SerializeField] GameObject bottlePrefab;
        [SerializeField] GameObject elixirPrefab;
        [SerializeField] GameObject shopDecorPrefab;
        [SerializeField] GameObject masterPrefab;
        [SerializeField] GameObject dummyPrefab;
        [Tooltip("Cenario proprio por tipo de sala, na ordem do enum RoomType (nulo = nenhum).")]
        [SerializeField] GameObject[] decorByRoom = new GameObject[0];
        [System.Serializable] public class BiomeProps { public GameObject[] props = new GameObject[0]; }
        [Tooltip("Adereços de cada trecho, na ordem do enum Biome.")]
        [SerializeField] BiomeProps[] biomeProps = new BiomeProps[0];
        [SerializeField] Material matteMaterial;
        [Tooltip("Arranjos de cobertura das salas de combate; a run sorteia um por sala.")]
        [SerializeField] RoomLayout[] layouts = new RoomLayout[0];
        [SerializeField] Vector3 entryPoint = new(0, 0.05f, -5.5f);
        [SerializeField] float doorLine = 7.15f;
        [Tooltip("Meia largura e meia profundidade da sala, com folga (limites para resgatar quem sai).")]
        [SerializeField] Vector2 arenaHalf = new(Arena.DefaultHalfX, Arena.DefaultHalfZ);
        [Tooltip("Os arranjos foram desenhados para 20 x 16: na sala maior as pecas se espalham nesta proporcao.")]
        [SerializeField] Vector2 layoutSpread = Vector2.one;

        /// Tamanho da sala da run (o builder chama ao montar a cena).
        public void BindRoomSize(Vector3 entry, float doors, Vector2 half, Vector2 spread) { entryPoint = entry; doorLine = doors; arenaHalf = half; layoutSpread = spread; }
        public Vector2 LayoutSpread => layoutSpread;
        public Vector3 EntryPoint => entryPoint;
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

        public void BindMiniBoss(GameObject prefab) => miniBossPrefab = prefab;
        [SerializeField] GameObject cryptMiniBossPrefab;
        public void BindCryptMiniBoss(GameObject prefab) => cryptMiniBossPrefab = prefab;
        [SerializeField] GameObject gardenMiniBossPrefab;
        public void BindGardenMiniBoss(GameObject prefab) => gardenMiniBossPrefab = prefab;
        /// Mini-chefe das Elites do trecho: Cripta = Sacerdotisa Afogada, Jardim = Colosso de Pedra, Saloes = Carrasco.
        public GameObject MiniBossFor(Biome biome) =>
            biome == Biome.Crypt && cryptMiniBossPrefab != null ? cryptMiniBossPrefab
            : biome == Biome.Garden && gardenMiniBossPrefab != null ? gardenMiniBossPrefab : miniBossPrefab;
        Biome encounterBiome = Biome.Ashes;
        GameObject encounterMiniBoss;

        /// Mini-chefe de uma sala de Elite: sorteado pela semente e pela sala entre os tres (com 10 camadas a Elite com
        /// mini-chefe so cai no Jardim, entao o trecho sozinho deixaria dois deles de fora).
        public GameObject MiniBossForRoom(int seed, int nodeId)
        {
            var options = new[] { miniBossPrefab, cryptMiniBossPrefab, gardenMiniBossPrefab };
            int pick = (int)((((long)seed * 7919 + nodeId * 104729) % 3 + 3) % 3);
            for (int i = 0; i < 3; i++) { var o = options[(pick + i) % 3]; if (o != null) return o; }
            return null;
        }

        /// Nome do mini-chefe para a porta da Elite.
        public string MiniBossName(int seed, int nodeId)
        {
            var p = MiniBossForRoom(seed, nodeId);
            return p != null && p.TryGetComponent(out BossBrain b) && b.Definition != null ? b.Definition.displayName : "o mini-chefe";
        }
        public void BindElementals(GameObject swarm, GameObject ember, GameObject sentinel) { swarmPrefab = swarm; emberPrefab = ember; sentinelPrefab = sentinel; }
        public void BindGuard(GameObject guard) => guardPrefab = guard;
        [SerializeField] GameObject bannerPrefab;
        public void BindBanner(GameObject banner) => bannerPrefab = banner;
        [SerializeField] GameObject shadePrefab;
        public void BindShade(GameObject shade) => shadePrefab = shade;
        [SerializeField] GameObject crystalPrefab;
        public void BindCrystal(GameObject crystal) => crystalPrefab = crystal;

        // pontos possiveis do Cristal de Ressonancia (fracoes da sala): longe da entrada e das portas
        static readonly Vector3[] CrystalSpots =
        {
            new(-0.42f, 0, 0.18f), new(0.42f, 0, 0.18f), new(0f, 0, 0.42f), new(-0.62f, 0, 0.5f), new(0.62f, 0, 0.5f), new(0f, 0, -0.05f),
        };

        /// Poe os Cristais de Ressonancia da sala: um da segunda sala em diante, dois do meio da run. Publico para os testes.
        [SerializeField] GameObject[] breakablePrefabs = new GameObject[0];
        public void BindBreakables(GameObject[] list) => breakablePrefabs = list ?? new GameObject[0];
        public int BreakablesPlaced { get; private set; }

        /// Caixotes pequenos que quebram (Breakable), encostados nas paredes laterais e na de baixo, longe da entrada,
        /// so onde nao ha peca do arranjo nem adereco e o chao e navegavel. Sem colisor: nao mexem em nada da fisica.
        public int PlaceBreakables(int seed, int count)
        {
            BreakablesPlaced = 0;
            if (breakablePrefabs.Length == 0 || count <= 0) return 0;
            if (layoutRoot == null) layoutRoot = new GameObject("Layout").transform;
            var rng = new System.Random(seed);
            for (int tries = 0; tries < 60 && BreakablesPlaced < count; tries++)
            {
                Vector3 pos;
                if (rng.Next(3) > 0)
                    pos = new Vector3((rng.Next(2) == 0 ? -1f : 1f) * (Arena.HalfX - 2.2f), 0f, Mathf.Lerp(-0.6f, 0.75f, (float)rng.NextDouble()) * Arena.HalfZ);
                else
                {
                    pos = new Vector3(Mathf.Lerp(-0.8f, 0.8f, (float)rng.NextDouble()) * Arena.HalfX, 0f, -(Arena.HalfZ - 2.2f));
                    if (Mathf.Abs(pos.x - entryPoint.x) < 3.5f) continue;
                }
                // esfera acima do piso (o colisor do chao nao conta), larga o bastante para nao encostar em peca nem parede
                if (Physics.CheckSphere(pos + Vector3.up * 0.95f, 0.65f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (!UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 0.8f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                bool crowded = false;
                foreach (var b in Breakable.Active) if (b != null && (b.transform.position - hit.position).sqrMagnitude < 1.4f) crowded = true;
                if (crowded) continue;
                var prefab = breakablePrefabs[rng.Next(breakablePrefabs.Length)];
                var go = Instantiate(prefab, new Vector3(hit.position.x, 0f, hit.position.z), Quaternion.Euler(0, rng.Next(360), 0), layoutRoot);
                go.name = prefab.name;
                BreakablesPlaced++;
            }
            return BreakablesPlaced;
        }

        public int PlaceCrystals(Biome biome, int seed, int count)
        {
            if (crystalPrefab == null || biome == Biome.Heart || count <= 0) return 0;
            if (layoutRoot == null) layoutRoot = new GameObject("Layout").transform;
            int placed = 0, start = Mathf.Abs(seed) % CrystalSpots.Length;
            for (int i = 0; i < CrystalSpots.Length && placed < count; i++)
            {
                var f = CrystalSpots[(start + i) % CrystalSpots.Length];
                var pos = new Vector3(f.x * Arena.HalfX, 0f, f.z * Arena.HalfZ);
                // nao em cima de peca do arranjo (barreira, pilar, barril) nem fora do chao navegavel; perigos de chao abaixo
                // (a esfera fica acima do piso: o colisor do chao nao conta)
                if (Physics.CheckSphere(pos + Vector3.up * 1.2f, 0.9f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (!UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 0.6f, UnityEngine.AI.NavMesh.AllAreas)) continue;
                var go = Instantiate(crystalPrefab, hit.position, Quaternion.Euler(0, (seed * 53 + i * 71) % 360, 0), layoutRoot);
                go.name = crystalPrefab.name;
                go.GetComponent<ResonanceCrystal>().SetElement(ResonanceCrystal.ElementFor(biome));
                placed++;
            }
            if (placed > 0 && !tipThisRoom) Tip("crystal");
            return placed;
        }
        public void BindDrowned(GameObject drowned, GameObject puddle) { drownedPrefab = drowned; puddlePrefab = puddle; }
        public void BindWeaver(GameObject prefab) => weaverPrefab = prefab;

        /// O chefe final desta run: semente par traz a Tecela das Cinzas, impar o Guardiao.
        public bool WeaverRun => weaverPrefab != null && (GameFlow.Seed & 1) == 0;
        /// Rainha Afogada: um terco das sementes impares (resto 1 por 6). As outras impares sao do Guardiao.
        public bool QueenRun => queenPrefab != null && !WeaverRun && (((GameFlow.Seed % 6) + 6) % 6) == 1;
        [SerializeField] GameObject queenPrefab;
        public void BindQueen(GameObject prefab) => queenPrefab = prefab;

        public void BindExtraEnemies(GameObject charger, GameObject lobber, GameObject shaman)
        {
            chargerPrefab = charger; lobberPrefab = lobber; shamanPrefab = shaman;
        }

        public void BindBoons(BoonCatalog catalog) => boonCatalog = catalog;
        public void BindEmblems(GameObject[] list) => emblems = list;

        /// Emblema do elemento sobre um totem; sem os modelos (cena antiga), a pedra fosca de antes.
        GameObject Emblem(Transform parent, ElementType e, Vector3 at, float scale = 1f)
        {
            int i = (int)e - 1;
            if (emblems != null && i >= 0 && i < emblems.Length && emblems[i] != null)
            {
                var go = Instantiate(emblems[i], parent);
                go.transform.localPosition = at;
                go.transform.localScale = Vector3.one * scale;
                return go;
            }
            return Matte(GameObject.CreatePrimitive(PrimitiveType.Sphere), parent, at + Vector3.up * 0.35f, Vector3.one * 0.7f, ElementColors.Of(e)).gameObject;
        }
        public void BindBoss(GameObject boss) => bossPrefab = boss;

        public void BindProps(GameObject door, GameObject[] banners, GameObject shrine, GameObject chest, Material matte)
        {
            doorPrefab = door; bannerByRoom = banners; shrinePrefab = shrine; chestPrefab = chest; matteMaterial = matte;
        }

        static readonly Color Cream = new(0.93f, 0.88f, 0.78f);

        public void BindTutorial(GameObject dummy) => dummyPrefab = dummy;
        public void BindDecor(GameObject[] decors) => decorByRoom = decors;
        public void BindBiomeProps(BiomeProps[] sets) => biomeProps = sets;

        /// Onde os adereços do trecho entram: cantos da frente e as duas laterais, longe da entrada, das portas e dos layouts.
        /// Em fracao da meia sala (10 x 8 na sala padrao): cantos da frente e as laterais.
        static readonly (Vector3 pos, float rot)[] PropSpots =
        {
            (new Vector3(-0.66f, 0, -0.89f), 20f), (new Vector3(0.66f, 0, -0.89f), -25f),
            (new Vector3(-0.86f, 0, 0.01f), 90f), (new Vector3(0.85f, 0, -0.34f), -90f),
        };
        public int BiomePropsPlaced { get; private set; }

        /// Espalha os adereços do trecho nas bordas da sala. Somem junto com o layout. Publico para os testes e as capturas.
        public void PlaceBiomeProps(Biome biome, int seed)
        {
            BiomePropsPlaced = 0;
            int b = (int)biome;
            if (b >= biomeProps.Length || biomeProps[b] == null || biomeProps[b].props.Length == 0) return;
            if (layoutRoot == null) layoutRoot = new GameObject("Layout").transform;
            var set = biomeProps[b].props;
            int start = Mathf.Abs(seed) % set.Length;
            for (int i = 0; i < PropSpots.Length; i++)
            {
                var prefab = set[(start + i) % set.Length];
                if (prefab == null) continue;
                var (frac, rot) = PropSpots[i];
                var pos = new Vector3(frac.x * (Arena.HalfX - 0.6f), 0, frac.z * (Arena.HalfZ - 0.6f));
                var go = Instantiate(prefab, pos, Quaternion.Euler(0, rot + ((seed + i * 37) % 3 - 1) * 12f, 0), layoutRoot);
                go.name = prefab.name;
                BiomePropsPlaced++;
            }
        }

        /// Poe o cenario proprio do tipo de sala (camas no Descanso, ouro no Tesouro...). Some junto com o layout.
        public void Dress(RoomType type)
        {
            int t = (int)type;
            if (t >= decorByRoom.Length || decorByRoom[t] == null) return;
            if (layoutRoot == null) layoutRoot = new GameObject("Layout").transform;
            Instantiate(decorByRoom[t], layoutRoot);
            CurrentDecor = decorByRoom[t].name;
        }
        public string CurrentDecor { get; private set; } = "";
        public void BindLayouts(RoomLayout[] list) => layouts = list;
        public void BindMaster(GameObject prefab) => masterPrefab = prefab;

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

        /// Monta uma sala sem luta (Loja, Santuario, Mestre, Tesouro, Pacto, Descanso) com o cenario dela, sem passar pelo mapa.
        /// Para as capturas da volta pelas salas.
        public void ShowRoom(RoomType type)
        {
            Cleanup();
            ApplyLayout(-1);
            Dress(type);
            roomBase = $"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · {RoomInfo.Name(type)}";
            roomKind = type;
            hud.SetRoom(roomBase, type);
            switch (type)
            {
                case RoomType.Shop: SetupShop(); break;
                case RoomType.Shrine: SetupShrine(); break;
                case RoomType.Master: SetupBoons(true); break;
                case RoomType.Treasure: SetupTreasure(); break;
                case RoomType.Pact: SetupPact(false); break;
                case RoomType.Rest: SetupRest(); break;
            }
        }

        /// Layout da sala: a primeira sala da run so sorteia entre os sem perigo (so cobertura e barris).
        /// Arranjo da sala: os do trecho pesam o triplo, os de outro trecho ficam de fora; a primeira sala nao tem perigo.
        int PickLayout(MapNode node)
        {
            var biome = Biomes.For(node.Layer, Run.Map.LayerCount);
            var weights = new int[layouts.Length];
            for (int i = 0; i < layouts.Length; i++)
                weights[i] = layouts[i] == null || (node.Layer == 0 && layouts[i].HasHazard) ? 0 : layouts[i].WeightFor(biome);
            int pick = LayoutPicker.PickWeighted(GameFlow.Seed, node.Id, weights);
            return pick >= 0 ? pick : LayoutPicker.Pick(GameFlow.Seed, node.Id, layouts.Length);
        }
        /// Indice do arranjo com este nome (-1 se nao houver). Para os testes e as capturas.
        public int LayoutIndex(string displayName) => System.Array.FindIndex(layouts, l => l != null && l.displayName == displayName);
        Transform layoutRoot;

        /// Troca o arranjo da sala. index < 0 = sala vazia. Publico para a cena de teste e as capturas.
        public void ApplyLayout(int index)
        {
            // a sala anterior sai da fisica ja (o Destroy so vale no fim do quadro)
            if (layoutRoot != null) { layoutRoot.gameObject.SetActive(false); Destroy(layoutRoot.gameObject); }
            layoutRoot = new GameObject("Layout").transform;
            CurrentLayout = "";
            CurrentDecor = "";
            if (index < 0 || index >= layouts.Length || layouts[index] == null) return;
            CurrentLayout = layouts[index].displayName;
            foreach (var p in layouts[index].pieces)
                if (p.prefab != null) Instantiate(p.prefab, new Vector3(p.position.x * layoutSpread.x, p.position.y, p.position.z * layoutSpread.y), Quaternion.Euler(0, p.rotation, 0), layoutRoot);
        }

        void Awake()
        {
            Arena.Configure(arenaHalf.x, arenaHalf.y);
            GameClock.SetPaused(false);
            hud = FindFirstObjectByType<Hud>();
            if (hud == null) hud = new GameObject("Hud").AddComponent<Hud>();
            dresser = gameObject.AddComponent<BiomeDresser>();
        }

        void OnDestroy() { Arena.ResetSize(); GameClock.Holding = false; }

        void OnEnable()
        {
            GameEvents.RoomCleared += OnRoomCleared;
            GameEvents.Died += OnDied;
            GameEvents.BossSpawned += OnBossSpawned;
            GameEvents.ElementAwakened += OnAwakened;
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.DamageDealt += OnDamageDealt;
            CorruptionPool.Marked += OnPoolMarked;
            GameEvents.PuddleLanded += OnPuddleLanded;
            GameEvents.QuintessenceActiveChanged += OnQuintessence;
            GameEvents.ReactionTriggered += OnReactionForFeats;
            GameEvents.BossShieldChanged += OnBossShield;
            GameEvents.AttackPerformed += OnAttackForTrial;
            GameEvents.PerfectDodge += OnPerfectDodge;
            GameEvents.Thorns += OnThorns;
            GameEvents.TelegraphStarted += OnMeleeWindup;
        }

        // corpo a corpo nao tem circulo no chao: a primeira arma erguida ensina a ler a animacao
        void OnMeleeWindup(TelegraphEvent e) { if (e.Melee) Tip("melee"); }

        // Pes no Chao: cada dash do heroi conta
        void OnAttackForTrial(AttackEvent e)
        {
            if (!TrialActive || Busy || e.Kind != AttackKind.Dash || player == null || e.Source != player.gameObject) return;
            TrialDashes++;
            UpdateTrialLabel();
        }

        // ---------- contagem para os Feitos ----------
        int quintessences, fusions, trialsPassed, shieldBreaks;
        bool shieldUp, shieldNeedsQuintessence;
        /// Feitos que esta run cumpriu (para a tela de resultado e os testes).
        public List<Feat> NewFeats { get; private set; } = new();
        void OnQuintessence(bool on) { if (on && !Finished) { quintessences++; GrantNow(); } }
        // a Fusao chega sem alvo (e uma area, nao um inimigo)
        void OnReactionForFeats(ReactionEvent e) { if (!Finished && e.Target == null && !string.IsNullOrEmpty(e.Name)) { fusions++; GrantNow(); } }
        void OnBossShield(bool up, bool needsQuintessence)
        {
            if (up) { shieldUp = true; shieldNeedsQuintessence = needsQuintessence; return; }
            // so conta quebra de verdade: o escudo pedia Quintessencia e ela estava ativa (nao vale o escudo que so expirou)
            if (shieldUp && shieldNeedsQuintessence && player != null && player.QuintessenceActive && !Finished) { shieldBreaks++; GrantNow(); }
            shieldUp = false;
        }

        /// Feitos que acontecem no meio da run sao pagos e gravados na hora: sair e continuar nao os perde.
        void GrantNow()
        {
            if (Run == null || GameFlow.Tutorial) return;
            var got = MetaStore.Current.GrantFeats(new RunRecord
            {
                Quintessences = quintessences, Fusions = fusions, TrialsPassed = trialsPassed, ShieldBreaks = shieldBreaks,
            });
            if (got.Count == 0) return;
            NewFeats.AddRange(got);
            MetaStore.Save();
            foreach (var f in got)
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 4.4f, "FEITO: " + Feats.Name(f).ToUpperInvariant(), new Color(0.9f, 0.76f, 0.42f), 1f, 2f);
            Sfx.Play("feat");
        }

        void OnPuddleLanded(Vector3 point, float radius, ElementType element, float seconds) { if (seconds > 0f) WaterPuddle.Spawn(puddlePrefab, point, radius, element, seconds); }

        void OnPoolMarked() => Tip("terrain");

        // ---------- Cerco ----------

        public const float SiegeSeconds = 40f;
        public bool SiegeActive { get; private set; }
        public float SiegeLeft { get; private set; }
        string siegeLabel = "";

        /// Comeca o Cerco: as ondas da profundidade se repetem ate o tempo acabar. Publico para os testes.
        public void StartSiege(float seconds = SiegeSeconds)
        {
            spawner.SetRepeat(true);
            SiegeLeft = seconds;
            SiegeActive = true;
            hud.ShowBanner("CERCO", $"aguente {seconds:0} segundos: as ondas não param", RoomInfo.Color(RoomType.Siege) * 1.25f, 4f);
            UpdateSiegeLabel();
        }

        void TickSiege()
        {
            if (!SiegeActive || Busy || Finished) return;
            SiegeLeft -= Time.deltaTime;
            if (SiegeLeft > 0f) { UpdateSiegeLabel(); return; }
            // tempo esgotado: quem sobrou desmorona e a sala limpa (a bencao vem no RoomCleared)
            SiegeActive = false;
            SiegeLeft = 0f;
            hud.SetRoom($"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · Cerco · resistiu", RoomType.Siege);
            Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.9f, "CERCO VENCIDO", new Color(0.9f, 0.76f, 0.42f), 1.1f, 2f);
            Sfx.Play("level_up");
            spawner.EndSiege();
        }

        void UpdateSiegeLabel()
        {
            string label = $"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · Cerco · {Mathf.CeilToInt(Mathf.Max(0f, SiegeLeft))} s";
            if (label == siegeLabel) return;
            siegeLabel = label;
            hud.SetRoom(label, RoomType.Siege);
        }

        // ---------- Provacao ----------

        public bool TrialActive { get; private set; }
        public TrialKind TrialKind { get; private set; }
        public float TrialLimit { get; private set; }
        public float TrialElapsed { get; private set; }
        public int TrialHits { get; private set; }
        public int TrialDashes { get; private set; }
        /// Resultado da ultima Provacao (null = nenhuma terminou nesta run).
        public bool? LastTrialPassed { get; private set; }
        float lastTrialHit = -99f, lastTrialCounted = -99f;
        GameObject lastTrialSource;
        /// Sala cuja Provacao ja estava perdida no save (sair e continuar nao da outra chance).
        int restoredTrialLost = -1;
        bool trialForfeit;
        public int MasteryPicksLeft => masteryPicks;
        bool TrialHolding => !trialForfeit && Trials.Holding(TrialKind, TrialElapsed, TrialHits, TrialLimit, TrialDashes);
        string trialStatus = "";
        bool trialLostShown;

        /// Comeca a Provacao da sala: luta comum da profundidade com o desafio sorteado. Publico para os testes.
        public void StartTrial(MapNode node, int waves)
        {
            TrialKind = Trials.Pick(GameFlow.Seed, node.Id);
            TrialLimit = Trials.TimeLimit(waves);
            TrialElapsed = 0f;
            TrialHits = 0;
            TrialDashes = 0;
            lastTrialHit = lastTrialCounted = -99f;
            lastTrialSource = null;
            trialLostShown = false;
            trialForfeit = node.Id == restoredTrialLost;
            TrialActive = true;
            hud.ShowBanner("PROVAÇÃO", $"{Trials.Name(TrialKind)}: {Trials.Goal(TrialKind, TrialLimit)}", RoomInfo.Color(RoomType.Trial) * 1.25f, 4f);
            UpdateTrialLabel();
        }

        void OnDamageDealt(Health target, DamageInfo info, float applied)
        {
            if (!TrialActive || target != playerHealth || applied <= 0f || Busy) return;
            // tiques seguidos da mesma fonte (poca, rastro) sao um golpe so: a janela anda so para ela.
            // Golpes de fontes diferentes contam, a nao ser que cheguem juntos com o ultimo golpe contado
            float now = Time.time;
            bool sameStream = info.Source == lastTrialSource && now - lastTrialHit < Trials.HitWindow;
            bool together = now - lastTrialCounted < Trials.HitWindow;
            lastTrialHit = now;
            lastTrialSource = info.Source;
            if (sameStream || together) return;
            lastTrialCounted = now;
            TrialHits++;
            UpdateTrialLabel();
        }

        void TickTrial()
        {
            if (!TrialActive || Busy || Finished) return;
            TrialElapsed += Time.deltaTime;
            // o letreiro so muda quando o texto muda (um segundo a menos, um golpe a mais)
            if ((TrialHolding ? Trials.Status(TrialKind, TrialElapsed, TrialHits, TrialLimit, TrialDashes) : "perdida") != trialStatus) UpdateTrialLabel();
            if (!trialLostShown && !TrialHolding)
            {
                trialLostShown = true;
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, "PROVAÇÃO PERDIDA", new Color(0.66f, 0.68f, 0.74f), 1f, 1.8f);
                // o save da sala guarda que esta Provacao ja foi perdida
                restoredTrialLost = Run.Current.Id;
                if (currentSave != null && currentSave.current == Run.Current.Id) { currentSave.trialLost = Run.Current.Id; RunSaveStore.Save(currentSave); }
                Sfx.Play("block");
            }
        }

        void UpdateTrialLabel()
        {
            if (!TrialActive || Run == null) return;
            trialStatus = (TrialHolding ? Trials.Status(TrialKind, TrialElapsed, TrialHits, TrialLimit, TrialDashes) : "perdida");
            hud.SetRoom($"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · Provação · {trialStatus}", RoomType.Trial);
        }

        /// Fecha a Provacao com a sala limpa: cumprida, da um talento extra e Essencia.
        void FinishTrial()
        {
            if (!TrialActive) return;
            TrialActive = false;
            bool passed = TrialHolding;
            LastTrialPassed = passed;
            if (passed)
            {
                trialsPassed++;
                GrantNow();
                Run.Progress.GrantPick();
                Run.AddEssence(Trials.BonusEssence);
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.9f, "PROVAÇÃO CUMPRIDA: TALENTO EXTRA", new Color(0.9f, 0.76f, 0.42f), 1.1f, 2.2f);
                Sfx.Play("level_up");
            }
            else if (!trialLostShown)
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.9f, "PROVAÇÃO PERDIDA", new Color(0.66f, 0.68f, 0.74f), 1f, 1.8f);
            hud.SetRoom($"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · Provação · {(passed ? "cumprida" : "perdida")}", RoomType.Trial);
        }

        void OnEnemySpawned(Health enemy)
        {
            if (enemy == null) return;
            if (enemy.TryGetComponent(out ElementalAffinity _) && enemy.GetComponent<BossBrain>() == null) Tip("affinity");
            if (enemy.TryGetComponent(out FrontGuard _)) Tip("guard");
            if (enemy.TryGetComponent(out AuraSource _)) Tip("banner");
            if (enemy.TryGetComponent(out EnemyBrain brain) && brain.Definition != null && brain.Definition.attack == EnemyAttackKind.Ambush) Tip("shade");
        }

        void OnPerfectDodge(Vector3 at) => Tip("perfect");
        void OnThorns(Vector3 at) => Tip("thorns");

        void OnAwakened(ElementType e)
        {
            if (player == null || Run == null || GameFlow.Tutorial) return;
            int n = player.Loadout.Awakened.Count;
            if (n == 2) Tip("react");
            else if (n == 3) Tip("fusion");
            else if (n >= 4) Tip("quint");
        }

        /// Mostra a dica uma vez so por jogador.
        /// A sala ja mostrou uma dica (o rodape so tem uma: a segunda apagaria a primeira).
        bool tipThisRoom;

        // dicas esperam a vez: uma so na tela e so marcada como vista quando aparece (antes a da arma erguida
        // apagava a de controles em segundos e as duas ja contavam como vistas)
        readonly Queue<string> tipQueue = new();
        /// Dicas esperando a vez (para os testes).
        public int TipsQueued => tipQueue.Count;
        /// A dica id esta esperando a vez (para os testes).
        public bool TipQueued(string id) => tipQueue.Contains(id);

        void Tip(string id)
        {
            if (GameFlow.Tutorial || Tips.WasSeen(id) || tipQueue.Contains(id)) return;
            if (hud.TipVisible) { tipQueue.Enqueue(id); return; }
            ShowTipNow(id);
        }

        string roomBase = "";
        RoomType roomKind;

        /// Objetivo fixo da sala de luta no chip do alto: onda e quantos inimigos faltam (antes so um "ONDA 1/2" flutuante).
        void TickObjective()
        {
            if (string.IsNullOrEmpty(roomBase) || spawner == null || spawner.State == null || Busy || TrialActive || SiegeActive) return;
            if (roomKind != RoomType.Combat && roomKind != RoomType.Elite && roomKind != RoomType.Boss) return;
            var st = spawner.State;
            string label = roomBase;
            if (st.Started && !st.Cleared && st.CurrentWave >= 0)
                label += $" · onda {st.CurrentWave + 1}/{st.TotalWaves} · {st.Alive} {(st.Alive == 1 ? "inimigo" : "inimigos")}";
            // so mexe no rotulo que e dela (Provacao e Cerco escrevem o proprio)
            if (label != hud.RoomLabel && hud.RoomLabel.StartsWith(roomBase)) hud.SetRoom(label);
        }

        void TickTips()
        {
            if (tipQueue.Count > 0 && !hud.TipVisible && !Busy && !GameClock.Paused && !Finished) ShowTipNow(tipQueue.Dequeue());
        }

        void ShowTipNow(string id)
        {
            if (!Tips.Consume(id)) return;
            tipThisRoom = true;
            if (id == "start")
            {
                // a skill inicial fica na tecla do elemento do heroi (Liora no 2, Brann no 3, Vey no 4)
                string[] skill = { "", "Labareda", "Maré", "Pilar de Pedra", "Vendaval" };
                int k = (int)GameFlow.StartElement;
                string[] pad = { "", "para cima", "para a direita", "para baixo", "para a esquerda" };
                hud.ShowTip(InputMode.Gamepad
                    ? $"RT ataca. Sua skill, {skill[k]}, sai no direcional {pad[k]} (ou LT). A desvia."
                    : $"Clique esquerdo ataca. Sua skill, {skill[k]}, está na tecla {k} (ou clique direito). Espaço desvia.", 9f);
                return;
            }
            hud.ShowTip(Tips.Text(id, InputMode.Gamepad));
        }

        /// O chefe nao passa pela escala das salas: a Ascensao entra aqui.
        void OnBossSpawned(Health boss, string name)
        {
            if (Run != null && !GameFlow.Tutorial)
            {
                bool final = Run.Current.Type == RoomType.Boss;
                hud.ShowBanner(name.ToUpperInvariant(), final ? "Chefe final" : "Mini-chefe", final ? new Color(0.95f, 0.62f, 0.32f) : new Color(0.8f, 0.78f, 0.9f), 3f);
                // chefe final: a camera vai ate ele enquanto ele sai do chao
                if (final && boss != null && Camera.main != null && Camera.main.TryGetComponent(out FollowCamera follow)) follow.Peek(boss.transform, 2.2f);
            }
            if (GameFlow.Ascension <= 0 || boss == null) return;
            boss.Pool.SetMax(boss.Max * Ascension.HealthMult(GameFlow.Ascension));
        }

        void OnDisable()
        {
            GameEvents.RoomCleared -= OnRoomCleared;
            GameEvents.Died -= OnDied;
            GameEvents.BossSpawned -= OnBossSpawned;
            GameEvents.ElementAwakened -= OnAwakened;
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.DamageDealt -= OnDamageDealt;
            CorruptionPool.Marked -= OnPoolMarked;
            GameEvents.PuddleLanded -= OnPuddleLanded;
            GameEvents.QuintessenceActiveChanged -= OnQuintessence;
            GameEvents.ReactionTriggered -= OnReactionForFeats;
            GameEvents.BossShieldChanged -= OnBossShield;
            GameEvents.AttackPerformed -= OnAttackForTrial;
            GameEvents.PerfectDodge -= OnPerfectDodge;
            GameEvents.Thorns -= OnThorns;
            GameEvents.TelegraphStarted -= OnMeleeWindup;
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
            hud.SetRanks(Run.SkillRank);
            talentRng = new System.Random(GameFlow.Seed ^ 0x5f3759df);
            startTime = Time.time;
            // melhorias permanentes do Refugio entram antes da primeira sala
            HeroPassives.Apply(GameFlow.StartElement, player.Modifiers);
            MetaStore.Current.ApplyTo(player.Modifiers);
            int mastery = MetaStore.Current.MasteryLevel(GameFlow.StartElement);
            Mastery.ApplyTo(mastery, player.Modifiers);
            ApplyMaxHealth();
            Run.AddEssence(MetaStore.Current.StartEssence + Mastery.StartEssence(mastery));
            Run.Rerolls = MetaStore.Current.Rerolls + Mastery.ExtraRerolls(mastery);
            Run.TalentChoices = MetaStore.Current.TalentChoices;
            for (int i = 0; i < Mastery.StartTalents(mastery); i++) { Run.Progress.GrantPick(); masteryPicks++; }
            hud.SetEssence(Run.Essence);
            if (GameFlow.Resume != null)
            {
                var save = GameFlow.Resume;
                GameFlow.Resume = null;
                try { RestoreFrom(save); }
                catch (System.Exception e)
                {
                    // save que nao serve mais (adulterado ou de outra versao): descarta e comeca uma run nova
                    Debug.LogWarning("save da run descartado: " + e.Message);
                    RunSaveStore.Clear();
                    GameFlow.StartRun(GameFlow.StartElement);
                    return;
                }
            }
            StartCoroutine(EnterRoom(Run.Current, true));
        }

        // ---------- save da run ----------

        /// Esta run veio de um save (Continuar).
        public bool Resumed { get; private set; }

        /// Foto da run agora. Chamado ao entrar em cada sala; publico para os testes.
        public void SaveRun()
        {
            if (GameFlow.Tutorial || Run == null || Finished) return;
            var s = new RunSave
            {
                hero = (int)GameFlow.StartElement, seed = GameFlow.Seed, layers = GameFlow.Layers, shrines = GameFlow.Shrines, ascension = GameFlow.Ascension,
                current = Run.Current.Id, depth = Run.Depth, visited = new List<int>(Run.Visited).ToArray(),
                essence = Run.Essence, roomsCleared = Run.RoomsCleared, kills = Run.Kills, rerolls = Run.Rerolls, pacts = PactsTaken, talentChoices = Run.TalentChoices,
                ranks = (int[])Run.SkillRank.Clone(),
                level = Run.Progress.Level, xp = Run.Progress.Xp, pending = Run.Progress.PendingPicks, totalXp = Run.Progress.TotalXp,
                health = playerHealth.Current, maxHealth = playerHealth.Max, baseMaxHealth = baseMaxHealth, curse = playerHealth.Pool.CurseMultiplier,
                elapsed = Time.time - startTime, modifiers = player.Modifiers.ToJson(),
                trialLost = restoredTrialLost == Run.Current.Id ? restoredTrialLost : -1,
            };
            var talents = new List<string>();
            foreach (var t in Run.Talents.Taken) talents.Add(t.Id);
            s.talents = talents.ToArray();
            var boons = new List<SavedBoon>();
            foreach (var b in Run.Boons)
                boons.Add(new SavedBoon { id = b.id, name = b.name, description = b.description, patron = (int)b.patron, stat = (int)b.stat, value = b.value });
            s.boons = boons.ToArray();
            var awakened = new List<int>();
            foreach (var e in player.Loadout.Awakened) awakened.Add((int)e);
            s.awakened = awakened.ToArray();
            RunSaveStore.Save(s);
            currentSave = s;
        }

        RunSave currentSave;
        float saveHealthTimer;

        /// O save acompanha o dano levado na sala (so para baixo): sair e continuar nao devolve a vida. Publico para os testes.
        public void SyncSavedHealth()
        {
            if (currentSave == null || Finished || playerHealth == null || playerHealth.IsDead) return;
            if (playerHealth.Current >= currentSave.health - 0.5f) return;
            currentSave.health = playerHealth.Current;
            RunSaveStore.Save(currentSave);
        }

        void RestoreFrom(RunSave s)
        {
            if (s.current < 0 || s.current >= Run.Map.Nodes.Count) throw new System.InvalidOperationException("a sala do save nao existe neste mapa");
            foreach (int e in s.awakened) if (e < 1 || e > 4) throw new System.InvalidOperationException("elemento invalido no save");
            Run.Restore(s.current, s.visited, s.essence, s.roomsCleared);
            Run.Kills = s.kills;
            Run.Rerolls = s.rerolls;
            if (s.talentChoices >= 3) Run.TalentChoices = Mathf.Min(s.talentChoices, 4); // save antigo (0): fica o valor do Refugio
            for (int i = 0; i < Run.SkillRank.Length && i < s.ranks.Length; i++) Run.SkillRank[i] = Mathf.Clamp(s.ranks[i], 1, RunState.MaxSkillRank);
            Run.Progress.Restore(s.level, s.xp, s.pending, s.totalXp);
            // a escolha da maestria so continua pendente se o save e da primeira sala e ela ainda nao foi usada
            masteryPicks = Run.Depth == 0 ? Mathf.Min(masteryPicks, s.pending) : 0;
            restoredTrialLost = s.trialLost;
            foreach (var id in s.talents) Run.Talents.Restore(TalentCatalog.Find(id));
            foreach (var b in s.boons)
                Run.AddBoon(new Boon { id = b.id, name = b.name, description = b.description, patron = (ElementType)b.patron, stat = (BoonStat)b.stat, value = b.value });
            foreach (int e in s.awakened) player.Awaken((ElementType)e);
            // a foto dos modificadores ja traz passiva, Refugio, bencaos, talentos, pactos e graus: substitui o que o Start aplicou
            player.Modifiers.FromJson(s.modifiers);
            baseMaxHealth = s.baseMaxHealth;
            playerHealth.Pool.SetMax(s.maxHealth);
            playerHealth.Pool.SetCurrent(s.health);
            playerHealth.Pool.CurseMultiplier = s.curse > 0f ? s.curse : 1f;
            PactsTaken = s.pacts;
            startTime = Time.time - s.elapsed;
            hud.SetBoons(Run.Boons);
            hud.SetEssence(Run.Essence);
            Resumed = true;
        }

        void Update()
        {
            if (Finished) return;
            // a dificuldade pode mudar na pausa: vale na hora
            if (playerHealth != null) playerHealth.Pool.AssistMultiplier = GameFlow.Tutorial ? 1f : GamePrefs.HeroDamageTaken;
            TickTrial();
            TickSiege();
            TickTips();
            TickObjective();
            if ((saveHealthTimer -= Time.unscaledDeltaTime) <= 0f) { saveHealthTimer = 2f; SyncSavedHealth(); }
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
            if (paused && Run != null)
                hud.PauseSummary = $"{HeroPassives.HeroName(GameFlow.StartElement)}  ·  nível {Run.Progress.Level}  ·  sala {Run.Depth + 1}/{Run.Map.LayerCount}  ·  {Biomes.Name(Biomes.For(Run.Depth, Run.Map.LayerCount))}"
                    + $"  ·  {Plural(Run.Talents.Count, "talento", "talentos")}" + (GameFlow.Ascension > 0 ? $"  ·  Ascensão {GameFlow.Ascension}" : "");
            hud.ShowPause(paused, () => SetPaused(false), () => { SyncSavedHealth(); GameFlow.ToTitle(); }, GameFlow.RestartRun);
            if (paused) hud.ShowMap(Run, false);
        }

        // ---------- salas ----------

        // nome do trecho novo guardado durante a troca de sala: aparece na tela escura, antes da sala, e some com ela
        bool entering;
        (string title, string sub, Color color)? chapter;

        IEnumerator EnterRoom(MapNode node, bool first = false)
        {
            Busy = true;
            entering = true;
            GameClock.Holding = true;
            hud.HidePrompt();
            if (!first)
            {
                yield return Fade(0f, 1f, 0.3f);
                Run.Advance(node.Id);
            }
            Cleanup();
            tipThisRoom = false;
            SaveRun();
            ApplyLayout(LayoutPicker.UsesLayout(node.Type) ? PickLayout(node) : -1);
            Dress(node.Type);
            TrialActive = false;
            SiegeActive = false;
            if (node.Type == RoomType.Combat || node.Type == RoomType.Siege || node.Type == RoomType.Elite || node.Type == RoomType.Boss || node.Type == RoomType.Trial)
                // a sala da Rainha Afogada e um salao alagado: adereços da Cripta em vez dos do Coracao
                PlaceBiomeProps(node.Type == RoomType.Boss && QueenRun ? Biome.Crypt : Biomes.For(Run.Depth, Run.Map.LayerCount), GameFlow.Seed + node.Id * 7);
            DressBiome(Biomes.For(Run.Depth, Run.Map.LayerCount), true);
            // a Rainha Afogada alaga o Coracao: a luz e a cor da sala final viram as da Cripta (o nome continua o do Coracao)
            if (node.Type == RoomType.Boss && QueenRun && dresser != null) dresser.Apply(Biome.Crypt);
            player.Teleport(entryPoint, Vector3.forward);
            roomBase = $"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · {RoomInfo.Name(node.Type)}";
            roomKind = node.Type;
            hud.SetRoom(roomBase, node.Type);
            yield return null;
            entering = false;
            if (chapter.HasValue)
            {
                // cartao de capitulo: o nome do trecho sobre o preto, antes de a sala comecar (ondas, chefe e letreiros
                // de objetivo vem depois dele, entao nada age nem se sobrepoe com a tela escura)
                var c = chapter.Value; chapter = null;
                hud.SetFade(1f);
                hud.ShowChapter(c.title, c.sub, c.color, ChapterHold + 0.35f);
                for (float t = 0f; t < ChapterHold; t += Time.captureFramerate > 0 ? 1f / Time.captureFramerate : Time.unscaledDeltaTime) yield return null;
            }
            // morreu na tela escura (queimadura que veio da sala anterior): a sala nao abre por tras do resultado
            if (Finished) { GameClock.Holding = false; Busy = false; yield break; }

            if (first && Run.Depth == 0) Tip("start");
            if (node.Type == RoomType.Shrine) Tip("shrine");
            else if (node.Type == RoomType.Elite) Tip("elite");
            else if (node.Type == RoomType.Shop) Tip("shop");
            else if (node.Type == RoomType.Pact) Tip("pact");
            else if (node.Type == RoomType.Trial) Tip("trial");
            else if (node.Type == RoomType.Siege) Tip("siege");
            // Cristais de Ressonancia nas salas de luta, da segunda em diante (o chefe luta sem eles). Depois do quadro de espera:
            // a sala anterior ja saiu da fisica e da malha de navegacao
            if (node.Type != RoomType.Boss && LayoutPicker.UsesLayout(node.Type) && Run.Depth >= 1)
                PlaceCrystals(Biomes.For(Run.Depth, Run.Map.LayerCount), GameFlow.Seed + node.Id * 13, Run.Depth >= 4 ? 2 : 1);
            // caixotes que quebram nas salas de luta (o chefe fica sem: a sala dele e o palco)
            if (node.Type != RoomType.Boss && LayoutPicker.UsesLayout(node.Type))
                PlaceBreakables(GameFlow.Seed + node.Id * 17, 4);
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
                    if (!SetupBoons(true)) { Run.RoomCleared(); OpenDoors(); }
                    break;
                case RoomType.Treasure:
                    SetupTreasure();
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                case RoomType.Pact:
                    SetupPact(true);
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                case RoomType.Rest:
                    SetupRest();
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                case RoomType.Siege:
                    // as ondas de uma sala comum, repetidas ate o tempo acabar
                    StartEncounter(RoomType.Combat, Run.Depth, EliteModifier.None);
                    StartSiege();
                    break;
                case RoomType.Trial:
                    // a luta e a de uma sala comum da mesma profundidade; o desafio vem por cima
                    StartEncounter(RoomType.Combat, Run.Depth, EliteModifier.None);
                    StartTrial(node, EncounterPlanner.Plan(RoomType.Combat, Run.Depth, Biomes.For(Run.Depth, Run.Map.LayerCount)).Count);
                    break;
                default:
                    StartEncounter(node.Type, Run.Depth, node.Type == RoomType.Elite ? DifficultyCurve.PickElite(GameFlow.Seed, node.Id) : EliteModifier.None);
                    break;
            }
            GameClock.Holding = false; // o heroi volta a andar enquanto a sala clareia
            yield return Fade(1f, 0f, 0.35f);
            Busy = false;
        }

        const float ChapterHold = 1.3f;

        /// Comeca as ondas de uma sala com a dificuldade da profundidade e, em Elite, o traco. Publico para os testes e as capturas.
        public const float FirstRoomGrace = 1.5f;

        public void StartEncounter(RoomType type, int depth, EliteModifier trait)
        {
            var diff = DifficultyCurve.For(type, depth, trait);
            diff.Health *= Ascension.HealthMult(GameFlow.Ascension);
            diff.Damage *= Ascension.DamageMult(GameFlow.Ascension);
            spawner.Scaling = new EnemyScaling { Health = diff.Health, Damage = diff.Damage, Speed = diff.Speed, Cooldown = diff.Cooldown, Size = diff.Size, Volatile = diff.Volatile, Wards = diff.Wards, Splits = diff.Splits, Thorns = diff.Thorns };
            spawner.SplitPrefab = swarmPrefab;
            CurrentTrait = trait;
            if (trait != EliteModifier.None)
            {
                hud.SetRoom($"Sala {depth + 1}/{Run.Map.LayerCount} · Elite: {DifficultyCurve.Name(trait)}", type);
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f,
                    $"ELITE {DifficultyCurve.Name(trait).ToUpperInvariant()}: {DifficultyCurve.Describe(trait)}", new Color(0.86f, 0.62f, 0.34f), 1f, 2.6f);
            }
            spawner.Champion = type == RoomType.Elite && depth < EncounterPlanner.MiniBossDepth;
            encounterBiome = Run != null ? Biomes.For(depth, Run.Map.LayerCount) : Biome.Ashes;
            encounterMiniBoss = Run != null ? MiniBossForRoom(GameFlow.Seed, Run.Current.Id) : MiniBossFor(encounterBiome);
            // primeira sala: o primeiro esqueleto chegava em ~3 s, antes de dar para ler a dica de controles
            spawner.Run(BuildWaves(EncounterPlanner.Plan(type, depth, encounterBiome)), depth == 0 && Run != null && Run.Depth == 0 ? FirstRoomGrace : 0f);
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
                if (p.Swarm > 0 && swarmPrefab != null) w.entries.Add(new WaveEntry { prefab = swarmPrefab, count = p.Swarm });
                if (p.Ember > 0 && emberPrefab != null) w.entries.Add(new WaveEntry { prefab = emberPrefab, count = p.Ember });
                if (p.Sentinel > 0 && sentinelPrefab != null) w.entries.Add(new WaveEntry { prefab = sentinelPrefab, count = p.Sentinel });
                if (p.Guard > 0) w.entries.Add(new WaveEntry { prefab = guardPrefab != null ? guardPrefab : meleePrefab, count = p.Guard });
                if (p.Banner > 0) w.entries.Add(new WaveEntry { prefab = bannerPrefab != null ? bannerPrefab : rangedPrefab, count = p.Banner });
                if (p.Shade > 0) w.entries.Add(new WaveEntry { prefab = shadePrefab != null ? shadePrefab : chargerPrefab != null ? chargerPrefab : meleePrefab, count = p.Shade });
                if (p.Drowned > 0) w.entries.Add(new WaveEntry { prefab = drownedPrefab != null ? drownedPrefab : lobberPrefab != null ? lobberPrefab : rangedPrefab, count = p.Drowned });
                var mini = encounterMiniBoss != null ? encounterMiniBoss : MiniBossFor(encounterBiome);
                if (p.MiniBoss > 0 && mini != null) w.entries.Add(new WaveEntry { prefab = mini, count = p.MiniBoss });
                if (p.Boss > 0) w.entries.Add(new WaveEntry { prefab = WeaverRun ? weaverPrefab : QueenRun ? queenPrefab : bossPrefab != null ? bossPrefab : brutePrefab, count = p.Boss });
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
            foreach (var w in FindObjectsByType<WaterPuddle>(FindObjectsSortMode.None)) Destroy(w.gameObject);
            foreach (var v in FindObjectsByType<VolatileBurst>(FindObjectsSortMode.None)) Destroy(v.gameObject);
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
            FinishTrial();
            Run.AddEssence(Mathf.RoundToInt(essence * player.Modifiers.EssenceMult));
            hud.SetEssence(Run.Essence);
            if (node.Type == RoomType.Boss)
            {
                // o heroi para e comemora enquanto o resultado nao aparece: o que sobrou do chefe no chao (areas, tiros,
                // pocas, estilhacos) some e ele nao leva mais dano, senao apanharia parado debaixo do VITORIA
                GameClock.Holding = true;
                playerHealth.Pool.Invulnerable = true;
                foreach (var z in FindObjectsByType<DamageZone>(FindObjectsSortMode.None)) Destroy(z.gameObject);
                foreach (var p in FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
                foreach (var w in FindObjectsByType<WaterPuddle>(FindObjectsSortMode.None)) Destroy(w.gameObject);
                foreach (var v in FindObjectsByType<VolatileBurst>(FindObjectsSortMode.None)) Destroy(v.gameObject);
                var anim = player.GetComponentInChildren<PlayerAnimator>();
                if (anim != null) anim.Celebrate();
                hud.ShowFlash("VITÓRIA!", new Color(0.97f, 0.88f, 0.58f), 1.5f, player.transform);
                StartCoroutine(Finish(true));
                return;
            }
            hud.ShowFlash("SALA LIMPA!", new Color(0.97f, 0.88f, 0.58f), 1.4f, player != null ? player.transform : null);
            if (node.Reward == RewardType.Boon && SetupBoons()) return; // as portas abrem depois de escolher
            OpenDoors();
        }

        void OnDied(Health target, DamageInfo info)
        {
            // quem desmorona no fim do Cerco (fonte = spawner) nao conta como abate do heroi
            bool collapsed = info.NoCredit;
            if (target != playerHealth && target.Team == Team.Enemy && Run != null && !Finished && !collapsed
                && (target.GetComponent<EnemyBrain>() != null || target.GetComponent<BossBrain>() != null)) GrantKillXp(target);
            if (target == playerHealth && !Finished) { KilledBy = KillerName(info.Source); StartCoroutine(Finish(false)); }
        }

        /// Quem derrubou o heroi (para a tela de derrota e os testes).
        public string KilledBy { get; private set; } = "";
        /// Maestria que esta run deu ao heroi e quantos niveis ele subiu.
        public int MasteryGained { get; private set; }
        public int MasteryLevelsUp { get; private set; }
        /// Nivel de Ascensao que esta vitoria liberou (0 = nenhum).
        public int NewAscension { get; private set; }

        static string Plural(int n, string one, string many) => n == 1 ? $"1 {one}" : $"{n} {many}";

        /// Resumo do build para o fim da run: a maior Sintonia, Fusoes e esquivas perfeitas (so o que aconteceu).
        public string BuildLine()
        {
            var parts = new List<string>();
            ElementType best = ElementType.None;
            int bestTier = 0;
            foreach (var e in Elements.All)
            {
                int t = Synergy.Tier(Run.Talents, e);
                if (t > bestTier) { bestTier = t; best = e; }
            }
            if (bestTier > 0) parts.Add($"Sintonia de {Synergy.ElementName(best)} {(bestTier == 2 ? "II" : "I")}");
            if (player.Fusions > 0) parts.Add(Plural(player.Fusions, "Fusão", "Fusões"));
            if (player.Cadences > 0) parts.Add(Plural(player.Cadences, "Cadência", "Cadências"));
            if (player.PerfectDodges > 0) parts.Add(Plural(player.PerfectDodges, "esquiva perfeita", "esquivas perfeitas"));
            foreach (var t in Run.Talents.Taken) if (t.Hero != ElementType.None) { parts.Add(t.Name); break; }
            return parts.Count > 0 ? "\nBuild: " + string.Join("   ·   ", parts) : "";
        }

        public static string KillerName(GameObject source)
        {
            if (source == null) return "";
            if (source.TryGetComponent(out EnemyBrain e) && e.Definition != null) return e.Definition.displayName;
            if (source.TryGetComponent(out BossBrain b) && b.Definition != null) return b.Definition.displayName;
            if (source.GetComponent<ExplosiveBarrel>() != null) return "Barril de pólvora";
            if (source.GetComponent<SpikeTrap>() != null) return "Armadilha de espinhos";
            if (source.GetComponent<EmberVent>() != null) return "Respiradouro de brasa";
            if (source.GetComponent<VolatileBurst>() != null) return "Explosão de um Volátil";
            if (source.GetComponent<CorruptionPool>() != null) return "Poça de corrupção";
            return "";
        }

        IEnumerator Finish(bool victory)
        {
            Finished = true;
            Victory = victory;
            RunSaveStore.Clear(); // run acabou: nada para continuar
            hud.HidePrompt();
            hud.ShowMap(Run, false);
            int seconds = Mathf.RoundToInt(Time.time - startTime);
            int unlockedBefore = Ascension.Unlocked(MetaStore.Current.Wins);
            int shards = MetaStore.Current.EndRun(Run.RoomsCleared, victory, Ascension.ShardMult(GameFlow.Ascension), GameFlow.StartElement);
            int unlockedNow = Ascension.Unlocked(MetaStore.Current.Wins);
            MasteryGained = Mastery.RunXp(Run.RoomsCleared, Run.Kills, victory);
            MasteryLevelsUp = MetaStore.Current.AddHeroXp(GameFlow.StartElement, MasteryGained);
            var endFeats = MetaStore.Current.GrantFeats(new RunRecord
            {
                Final = true, Victory = victory, Ascension = GameFlow.Ascension, Kills = Run.Kills, Talents = Run.Talents.Count,
                Quintessences = quintessences, Fusions = fusions, TrialsPassed = trialsPassed, ShieldBreaks = shieldBreaks,
            });
            NewFeats.AddRange(endFeats);
            NewAscension = unlockedNow > unlockedBefore ? unlockedNow : 0;
            MetaStore.Save();
            ShardsEarned = shards;
            // o resultado ja esta gravado: fechar a aba durante a pausa abaixo nao perde a run
            yield return new WaitForSecondsRealtime(victory ? 1.6f : 1.2f);
            string hero = HeroPassives.HeroName(GameFlow.StartElement);
            string header = hero + (GameFlow.Ascension > 0 ? $"   ·   Ascensão {GameFlow.Ascension}" : "") + (GamePrefs.Relaxed ? "   ·   dificuldade Tranquila" : "")
                + (!victory && KilledBy.Length > 0 ? $"   ·   derrubado por {KilledBy}" : "");
            var tiles = new[]
            {
                ($"{Run.RoomsCleared}/{Run.Map.LayerCount}", "salas vencidas"),
                (Run.Kills.ToString(), Run.Kills == 1 ? "inimigo derrotado" : "inimigos derrotados"),
                ($"{seconds / 60}:{seconds % 60:00}", "tempo"),
                (Run.Progress.Level.ToString(), "nível"),
            };
            string details = $"{Plural(Run.Talents.Count, "talento", "talentos")}, {Plural(Run.Boons.Count, "bênção", "bênçãos")}, {player.Loadout.Awakened.Count} de 4 elementos, {Run.Essence} de Essência"
                + BuildLine()
                + $"\n+{shards} Fragmentos para o Refúgio (total: {MetaStore.Current.Shards})   ·   {hero} +{MasteryGained} de maestria"
                + (MasteryLevelsUp > 0 ? $"\nMaestria {MetaStore.Current.MasteryLevel(GameFlow.StartElement)} de {hero}: {Mastery.Perk(MetaStore.Current.MasteryLevel(GameFlow.StartElement))}" : "")
                + (NewFeats.Count > 0 ? $"\nFeito{(NewFeats.Count > 1 ? "s" : "")}: {string.Join(", ", NewFeats.ConvertAll(Feats.Name))} (+{NewFeats.Count * Feats.Reward} Fragmentos)" : "")
                + (NewAscension > 0 ? $"\nAscensão {NewAscension} liberada: escolha no título para enfrentar inimigos mais fortes" : "");
            Sfx.Play(victory ? "victory" : "defeat");
            hud.ShowResult(victory, header, tiles, details, GameFlow.RestartRun, GameFlow.ToTitle, Epilogue(victory));
            // feito novo: um sino depois do som do resultado
            // os do meio da run ja tocaram na hora
            if (endFeats.Count > 0) { yield return new WaitForSecondsRealtime(0.9f); Sfx.Play("feat"); }
        }

        /// Frase de fechamento do resultado: na vitoria, o chefe final que caiu; na derrota, o trecho onde a run terminou.
        public string Epilogue(bool victory)
        {
            if (victory)
                return WeaverRun ? "Os fios da Tecelã se desfazem em cinza. Os quatro elementos se calam, e o quinto responde."
                    : QueenRun ? "A Rainha Afogada volta às águas paradas, e o Coração das Cinzas bate de novo."
                    : "O Guardião tomba sobre as próprias cinzas. A Quintessência desperta em você.";
            return lastBiome switch
            {
                Biome.Crypt => "A Cripta Afogada fecha as águas sobre mais um herói.",
                Biome.Garden => "O Jardim de Pedra ganha mais uma estátua.",
                Biome.Heart => "Tão perto do Coração. Na próxima descida, ele é seu.",
                _ => "Os Salões de Cinza guardam mais um nome. O Refúgio guarda o que você aprendeu.",
            };
        }

        // ---------- trechos da run ----------

        /// Troca o clima da sala para o trecho. announce: mostra o letreiro se o trecho mudou.
        public void DressBiome(Biome biome, bool announce = false)
        {
            if (dresser != null) dresser.Apply(biome);
            // musica do trecho: Cripta e Jardim tem faixa propria; o Coracao fica com a de luta do chefe
            GameEvents.RaiseAmbient(biome == Biome.Crypt ? 1 : biome == Biome.Garden ? 2 : 0);
            if (biome == lastBiome) return;
            lastBiome = biome;
            if (!announce) return;
            string sub = biome == Biome.Heart ? "O fim da descida" : "Ato " + Biomes.Roman(Biomes.Act(biome));
            var color = Color.Lerp(dresser != null ? dresser.CrackColor : Cream, Cream, 0.55f);
            if (entering) chapter = (Biomes.Name(biome).ToUpperInvariant(), sub, color);
            else hud.ShowBanner(Biomes.Name(biome).ToUpperInvariant(), sub, color);
        }

        // ---------- tesouro e pacto ----------

        /// Monta a sala de Tesouro sem passar pelo mapa. Para os testes e as capturas.
        public void ShowTreasure() { Cleanup(); ApplyLayout(-1); Dress(RoomType.Treasure); SetupTreasure(); }
        /// Monta a sala de Pacto sem passar pelo mapa. Para os testes e as capturas.
        public void ShowPact() { Cleanup(); ApplyLayout(-1); Dress(RoomType.Pact); SetupPact(false); }
        /// Monta a sala de Descanso (so o cenario). Para as capturas.
        public void ShowRest() { Cleanup(); ApplyLayout(-1); Dress(RoomType.Rest); SetupRest(); }

        /// Onde fica a fogueira do Descanso (o GameSceneBuilder poe a fogueira aqui; as duas escolhas ficam dos lados dela).
        public static readonly Vector3 CampfireAt = new(0f, 0f, 0.4f);
        public const float RestHeal = 0.35f, RestSharpen = 0.1f;
        /// O que o heroi escolheu no ultimo Descanso ("" = ainda nada). Para os testes.
        public string RestChoice { get; private set; } = "";

        /// Descanso: dormir junto ao fogo (cura) ou afiar a arma (golpe basico mais forte pelo resto da run). Um dos dois.
        void SetupRest()
        {
            RestChoice = "";
            var warm = new Color(0.95f, 0.66f, 0.38f);
            var picks = new List<Interactable>();
            var options = new (string name, string text, string icon, Color color, float x, System.Action take)[]
            {
                ("Dormir", $"Cura {Mathf.RoundToInt(RestHeal * 100)}% da vida", "boon_rest", new Color(0.5f, 0.8f, 0.55f), -2.9f, () =>
                {
                    playerHealth.Heal(playerHealth.Max * RestHeal);
                    Sfx.Play("heal");
                    Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.5f, $"+{Mathf.RoundToInt(RestHeal * 100)}% DE VIDA", new Color(0.5f, 1f, 0.55f), 1.1f, 1.8f);
                    RestChoice = "Dormir";
                }),
                ("Afiar a arma", $"+{Mathf.RoundToInt(RestSharpen * 100)}% de dano no golpe básico", "boon_damage", warm, 2.9f, () =>
                {
                    player.Modifiers.BasicDamageMult += RestSharpen;
                    Sfx.Play("pickup");
                    Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.5f, "ARMA AFIADA", warm, 1.1f, 1.8f);
                    RestChoice = "Afiar a arma";
                }),
            };
            foreach (var o in options)
            {
                var root = new GameObject("Rest_" + o.name);
                root.transform.position = CampfireAt + new Vector3(o.x, 0f, -1.3f);
                Medal(root.transform, new Vector3(0, 1.1f, 0), o.icon, o.color, 1.05f);
                Stack(root.transform, new Vector3(0, 1.75f, 0), (o.name, Cream, 0.8f), (Wrap(o.text, 18), new Color(0.8f, 0.76f, 0.7f), 0.55f));
                Glow(root.transform, new Vector3(0, 1.2f, -0.6f), o.color, 1.2f);
                Interactable entry = null;
                var take = o.take;
                entry = new Interactable
                {
                    Object = root, Prompt = (o.name == "Dormir" ? "dormir junto ao fogo" : "afiar a arma"),
                    Use = () =>
                    {
                        take();
                        // um ou outro: as duas escolhas somem
                        foreach (var p in picks) { interactables.Remove(p); if (p.Object != null) Destroy(p.Object); }
                    },
                };
                picks.Add(entry);
                interactables.Add(entry);
            }
        }
        /// Monta a sala do Mestre sem passar pelo mapa. Para as capturas.
        public void ShowMaster() { Cleanup(); ApplyLayout(-1); Dress(RoomType.Master); SetupBoons(true); }

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
        /// shuffle: a sala de verdade sorteia tres dos cinco; a de teste mostra os tres primeiros, sempre iguais.
        void SetupPact(bool shuffle)
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
                ("Pacto do Eco", "Reações causam +30%", "−15% de vida máxima", "boon_reaction", () =>
                {
                    baseMaxHealth *= 0.85f;
                    player.Modifiers.ReactionPower += 0.3f;
                    ApplyMaxHealth();
                    return true;
                }),
                ("Pacto da Pressa", "+15% de velocidade e dash 30% mais rápido", "Você recebe +15% de dano", "boon_speed", () =>
                {
                    player.Modifiers.MoveSpeedMult += 0.15f;
                    player.Modifiers.DashCooldownMult *= 0.7f;
                    playerHealth.Pool.CurseMultiplier *= 1.15f;
                    return true;
                }),
            };
            var pactRng = RoomRng(3);
            if (shuffle)
                for (int k = offers.Length - 1; k > 0; k--)
                {
                    int j = pactRng.Next(k + 1);
                    (offers[k], offers[j]) = (offers[j], offers[k]);
                }
            System.Array.Resize(ref offers, 3);
            for (int i = 0; i < offers.Length; i++)
            {
                var o = offers[i];
                var root = new GameObject("Pact_" + i);
                root.transform.position = new Vector3((i - (offers.Length - 1) / 2f) * 5.4f, 0, 0.2f);
                var altar = pactAltarPrefab != null ? pactAltarPrefab : shrinePrefab;
                if (altar != null) Instantiate(altar, root.transform).transform.localPosition = Vector3.zero;
                Medal(root.transform, new Vector3(0, 2.25f, 0), o.icon, wine, 1.1f);
                Stack(root.transform, new Vector3(0, 2.85f, 0), (o.name, Cream, 0.8f),
                    (Wrap(o.gain, 22), new Color(0.62f, 0.82f, 0.6f), 0.55f), (Wrap(o.cost, 24), new Color(0.85f, 0.45f, 0.4f), 0.55f));
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

        // ---------- sorteios ----------

        /// Sorteio fixo por sala: sair e continuar mostra as mesmas ofertas (loja, bencaos, pacto).
        System.Random RoomRng(int salt) => new(unchecked(GameFlow.Seed * 31 + Run.Current.Id * 7919 + salt * 104729));

        /// Sorteio de talentos pelo estado da run: a mesma escolha volta igual depois de continuar; trocar muda.
        System.Random TalentRng() => new(unchecked(GameFlow.Seed * 17 + Run.Talents.Count * 101 + Run.Rerolls * 7 + Run.Progress.Level * 13));

        // ---------- nivel e talentos ----------

        System.Random talentRng;
        bool levelUpOpen;
        List<Talent> talentOffer;
        /// Escolhas que vieram da maestria (o painel diz isso em vez de "Nivel").
        int masteryPicks;
        public bool LevelUpOpen => levelUpOpen;
        public IReadOnlyList<Talent> TalentOffer => talentOffer;

        /// Sem inimigo vivo na sala: a escolha de talento nunca abre com inimigo em campo (entre ondas pode abrir; o spawner pausa junto).
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
            Sfx.Play("level_up");
            Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.9f, "NÍVEL " + Run.Progress.Level + "!", new Color(0.9f, 0.76f, 0.42f), 1.3f, 1.8f);
        }

        void OpenLevelUp()
        {
            talentRng = TalentRng();
            talentOffer = TalentCatalog.Offer(Run.Talents, player.Loadout.Awakened, talentRng, Run.TalentChoices, GameFlow.StartElement);
            if (talentOffer.Count == 0) { while (Run.Progress.ConsumePick()) { } masteryPicks = 0; return; }
            if (GameFlow.AutoPickTalents) { PickTalent(0); return; }
            levelUpOpen = true;
            levelUpOpenedAt = Time.unscaledTime;
            GameClock.SetPaused(true);
            hud.ShowMap(Run, false);
            ShowLevelUpPanel();
            if (masteryPicks == 0) Tip("level"); // depois do painel: a dica fica por cima dele
        }

        /// Troca as tres opcoes por outras (Intuicao, do Refugio). Publico para os testes.
        public void RerollTalents()
        {
            if (!levelUpOpen || Run.Rerolls <= 0) return;
            Run.Rerolls--;
            var fresh = TalentCatalog.Offer(Run.Talents, player.Loadout.Awakened, TalentRng(), Run.TalentChoices, GameFlow.StartElement);
            if (fresh.Count == 0) { Run.Rerolls++; return; }
            talentOffer = fresh;
            Sfx.Play("ui_click");
            ShowLevelUpPanel();
        }

        void ShowLevelUpPanel()
        {
            if (masteryPicks > 0)
            {
                int m = MetaStore.Current.MasteryLevel(GameFlow.StartElement);
                hud.ShowLevelUp(0, talentOffer, PickTalent, Run.Rerolls, RerollTalents, $"MAESTRIA {m}", $"{HeroPassives.HeroName(GameFlow.StartElement)} começa a run com um talento à escolha");
            }
            else hud.ShowLevelUp(Run.Progress.Level - Run.Progress.PendingPicks + 1, talentOffer, PickTalent, Run.Rerolls, RerollTalents);
        }

        float levelUpOpenedAt;

        void LevelUpKeys()
        {
            if (talentOffer == null) return;
            // no controle: o direcional anda entre as cartas e A escolhe (pelo foco da interface); Y troca as opcoes
            var pad = Gamepad.current;
            // (meio segundo de carencia: o Y da Quintessencia apertado na hora em que o painel abre nao gasta a troca)
            if (pad != null && pad.buttonNorth.wasPressedThisFrame && Time.unscaledTime - levelUpOpenedAt > 0.5f) { RerollTalents(); return; }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) PickTalent(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) PickTalent(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) PickTalent(2);
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) PickTalent(3);
            else if (kb.tKey.wasPressedThisFrame) RerollTalents();
        }

        /// Pega o talento de indice index da oferta atual. Publico para os testes.
        public void PickTalent(int index)
        {
            if (talentOffer == null || index < 0 || index >= talentOffer.Count) return;
            var t = talentOffer[index];
            talentOffer = null;
            Run.Progress.ConsumePick();
            if (masteryPicks > 0) masteryPicks--;
            Run.Talents.Take(t, player.Modifiers);
            ApplyMaxHealth();
            int line = 0;
            foreach (var (se, tier) in Run.Talents.LastSynergies)
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * (4.4f + 0.7f * line++), $"SINTONIA DE {Synergy.ElementName(se).ToUpperInvariant()}: {Synergy.Describe(tier)}",
                    Color.Lerp(ElementColors.Of(se), Color.white, 0.3f), 1.05f, 2.4f);
            if (Run.Talents.LastSynergies.Count > 0) Sfx.Play("feat");
            Sfx.Play("pickup");
            if (levelUpOpen)
            {
                levelUpOpen = false;
                hud.ShowLevelUp(0, null, null);
                GameClock.SetPaused(false);
                // a tecla que escolheu o talento nao pode virar skill no quadro seguinte
                // a tecla que escolheu nao vira skill: o heroi ignora comandos no quadro em que a pausa sai (GameClock.ResumedFrame)
            }
            // com outra escolha na fila o painel reabre ja: o nome flutuando passaria por cima do titulo dele
            if (Run.Progress.PendingPicks > 0) return;
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
                if (node.Type == RoomType.Elite) kind += ": " + DifficultyCurve.Name(DifficultyCurve.PickElite(GameFlow.Seed, node.Id))
                    + (node.Layer >= EncounterPlanner.MiniBossDepth ? "\ne " + MiniBossName(GameFlow.Seed, node.Id) : "");
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
            // medalhao do tipo da sala no lintel (o estandarte la em cima fica fora do quadro da camera)
            Medal(root.transform, new Vector3(0, 2.75f, -0.7f), RoomInfo.Icon(type), RoomInfo.Color(type), 1.05f);
            Label(root.transform, new Vector3(0, 1.45f, -1.5f), label, Cream, 0.62f);
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
            string[] skills = { "", "Labareda", "Maré", "Pilar de Pedra", "Vendaval" };
            // a outra escolha do Santuario: em vez de despertar um elemento novo, aprofundar uma skill que ja tem
            var deepen = new List<ElementType>();
            foreach (var a in player.Loadout.Awakened) if (Run.SkillRank[(int)a] < RunState.MaxSkillRank) deepen.Add(a);
            int total = missing.Count + deepen.Count;
            // despertar vem primeiro na lista; aprofundar entra depois (mesma posicao no chao)
            var later = new List<Interactable>();
            for (int d = 0; d < deepen.Count; d++)
            {
                var e = deepen[d];
                var c = ElementColors.Of(e);
                int next = Run.SkillRank[(int)e] + 1;
                var root = new GameObject("Deepen_" + e);
                root.transform.position = new Vector3((missing.Count + d - (total - 1) / 2f) * 4.3f, 0, 0f);
                float top = 1.25f;
                if (shrinePrefab != null) Instantiate(shrinePrefab, root.transform).transform.localPosition = Vector3.zero;
                Emblem(root.transform, e, new Vector3(0, top + 0.12f, 0), 1.3f);
                Stack(root.transform, new Vector3(0, top + 1.46f, 0), (skills[(int)e] + " " + RunState.Roman(next), Cream, 0.72f),
                    (next == RunState.MaxSkillRank ? Wrap("+30% de dano e " + RunState.Signature(e), 22) : "+30% de dano\nrecarga 10% menor", new Color(0.8f, 0.76f, 0.7f), 0.5f));
                Glow(root.transform, new Vector3(0, top + 0.7f, -0.7f), c, 1.4f);
                later.Add(new Interactable
                {
                    Object = root, Prompt = "aprofundar " + skills[(int)e],
                    Use = () =>
                    {
                        if (!Run.Deepen(e, player.Modifiers)) return;
                        Sfx.Play("pickup");
                        Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, (skills[(int)e] + " " + RunState.Roman(next)).ToUpperInvariant() + "!", c, 1.3f, 1.8f);
                        foreach (var it in interactables) if (it.Object != null) Destroy(it.Object);
                        interactables.Clear();
                        Run.RoomCleared();
                        OpenDoors();
                    },
                });
            }
            for (int i = 0; i < missing.Count; i++)
            {
                var e = missing[i];
                var c = ElementColors.Of(e);
                var root = new GameObject("Shrine_" + e);
                root.transform.position = new Vector3((i - (total - 1) / 2f) * 4.3f, 0, 0f);
                float top = 1.25f;
                if (shrinePrefab != null) Instantiate(shrinePrefab, root.transform).transform.localPosition = Vector3.zero;
                else Matte(GameObject.CreatePrimitive(PrimitiveType.Cube), root.transform, new Vector3(0, 0.6f, 0), new Vector3(1f, 1.2f, 1f), new Color(0.4f, 0.4f, 0.45f));
                // o elemento aparece como uma pedra fosca sobre o pedestal e na luz que ela joga em volta
                Emblem(root.transform, e, new Vector3(0, top + 0.12f, 0), 1.4f);
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
            interactables.AddRange(later);
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
            var stock = ShopPlanner.Stock(catalog, Run.BoonIds, player.Loadout.Awakened, RoomRng(1), Run.Depth);
            // banca do mercador: prateleiras, balcao, ouro e o proprio (tudo do pack), some junto com o layout da sala
            if (shopDecorPrefab != null)
            {
                if (layoutRoot == null) layoutRoot = new GameObject("Layout").transform;
                var decor = Instantiate(shopDecorPrefab, layoutRoot);
                var stall = decor.transform.Find("Merchant");
                if (stall != null)
                {
                    Label(stall, new Vector3(0, 2.95f, 0), "MERCADOR", new Color(0.88f, 0.74f, 0.42f), 0.7f);
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
                if (item.Kind == ShopItemKind.Talent)
                    Medal(root.transform, new Vector3(0, top + 0.75f, 0), "boon_skill", new Color(0.9f, 0.76f, 0.42f), 1.25f);
                else if (item.Kind == ShopItemKind.Boon)
                    Medal(root.transform, new Vector3(0, top + 0.75f, 0), BoonEffects.Icon(item.Boon.stat), c, 1.25f);
                else
                {
                    var flask = item.Kind == ShopItemKind.MaxHealth && elixirPrefab != null ? elixirPrefab : bottlePrefab;
                    if (flask != null) Instantiate(flask, root.transform).transform.localPosition = new Vector3(0, top, 0);
                }
                var tags = Stack(root.transform, new Vector3(0, top + 1.42f, 0),
                    (Wrap(item.Name, 13), Cream, 0.72f), (Wrap(item.Description, 16), new Color(0.8f, 0.76f, 0.7f), 0.5f), (item.Price + " de Essência", CanPay, 0.62f));
                priceTags.Add((tags[2], item.Price));
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
                            case ShopItemKind.Talent: Run.Progress.GrantPick(); break;
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
        Merchant masterNpc;
        /// O Mestre da sala atual, se houver (para os testes).
        public Merchant MasterNpc => masterNpc;

        /// withMaster: sala do Mestre, com ele atras dos baus. Recompensa de Elite vem so com os baus.
        bool SetupBoons(bool withMaster = false)
        {
            masterNpc = null;
            if (boonCatalog == null) return false;
            var offer = BoonPicker.Offer(boonCatalog.boons, Run.BoonIds, player.Loadout.Awakened, RoomRng(2));
            if (offer.Count == 0) return false;
            if (withMaster && masterPrefab != null)
            {
                if (layoutRoot == null) layoutRoot = new GameObject("Layout").transform;
                var m = Instantiate(masterPrefab, new Vector3(-5.4f * layoutSpread.x, 0, -3.6f * layoutSpread.y), Quaternion.Euler(0, 120, 0), layoutRoot);
                m.name = "Mestre";
                Label(m.transform, new Vector3(0, 3.2f, 0), "MESTRE", new Color(0.88f, 0.74f, 0.42f), 0.7f);
                masterNpc = m.GetComponent<Merchant>();
                if (masterNpc != null) masterNpc.Bind(player.transform);
            }
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
                Stack(root.transform, new Vector3(0, 2.55f, 0), (boon.name, Cream, 0.9f), (Wrap(boon.description, 22), new Color(0.8f, 0.76f, 0.7f), 0.6f));
                Glow(root.transform, new Vector3(0, 1.9f, -0.7f), c, 1.8f);
                interactables.Add(new Interactable
                {
                    Object = root, Prompt = "aceitar " + boon.name,
                    Use = () =>
                    {
                        Sfx.Play("pickup");
                        if (masterNpc != null) masterNpc.Thank();
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
        Feel.CombatFeedback feedback;

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
            // medalhao de verdade: disco de pedra com espessura, aro de gomos aceso na cor da sala e o icone na face
            // (antes eram tres figuras chapadas viradas para a camera, que liam como adesivo de interface)
            feedback ??= FindFirstObjectByType<Feel.CombatFeedback>();
            if (feedback != null && Feel.CombatFeedback.RuneRingMesh != null)
            {
                var disc = Matte(GameObject.CreatePrimitive(PrimitiveType.Cylinder), root, new Vector3(0, 0, 0.06f), new Vector3(size, 0.05f, size), new Color(0.15f, 0.13f, 0.17f));
                disc.localRotation = Quaternion.Euler(90f, 0f, 0f);
                OwnedMaterials.Add(root.gameObject, disc.GetComponent<Renderer>().sharedMaterial);
                var rim = new GameObject("Rim").transform;
                rim.SetParent(root, false);
                rim.localPosition = new Vector3(0, 0, -0.01f);
                rim.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                rim.localScale = Vector3.one * (size * 0.52f);
                rim.gameObject.AddComponent<MeshFilter>().sharedMesh = Feel.CombatFeedback.RuneRingMesh;
                var rimMat = feedback.RingMaterial(color);
                rim.gameObject.AddComponent<MeshRenderer>().sharedMaterial = rimMat;
                OwnedMaterials.Add(root.gameObject, rimMat);
                var ic = new GameObject("Icon").AddComponent<SpriteRenderer>();
                ic.transform.SetParent(root, false);
                ic.transform.localPosition = new Vector3(0, 0, -0.05f);
                var sprite = UI.UiKit.Icon(icon);
                ic.sprite = sprite; ic.color = Color.Lerp(color, Cream, 0.55f);
                ic.transform.localScale = Vector3.one * (size * 0.62f / sprite.bounds.size.x);
                return;
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

        /// Altura de uma linha de Label por unidade de escala (fonte de titulo, tamanho 80, characterSize 0,075).
        const float LabelLine = 0.58f;

        /// Placa de textos empilhados de baixo para cima a partir de baseLocal (a borda de baixo da ultima linha).
        /// Cada texto ocupa a altura das linhas que tem: descricao que quebra em duas empurra o nome para cima
        /// em vez de cair por cima do preco ou do icone. Devolve os textos na ordem dada (de cima para baixo).
        TextMesh[] Stack(Transform parent, Vector3 baseLocal, params (string text, Color color, float scale)[] rows)
        {
            var made = new TextMesh[rows.Length];
            float y = baseLocal.y;
            for (int i = rows.Length - 1; i >= 0; i--)
            {
                var r = rows[i];
                int lines = 1;
                foreach (char ch in r.text) if (ch == '\n') lines++;
                float h = lines * LabelLine * r.scale;
                made[i] = Label(parent, new Vector3(baseLocal.x, y + h * 0.5f, baseLocal.z), r.text, r.color, r.scale);
                y += h + 0.08f;
            }
            return made;
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
