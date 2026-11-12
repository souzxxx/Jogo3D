using System.Collections.Generic;
using Quintessence.Enemies;
using Quintessence.Game;
using Quintessence.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Monta as cenas do jogo de verdade: Title (menu) e Run (a run inteira acontece nela), e as poe no Build Settings.
public static class GameSceneBuilder
{
    public const string TitlePath = "Assets/Scenes/Title.unity";
    public const string RunPath = "Assets/Scenes/Run.unity";

    [MenuItem("Quintessence/Build cenas do jogo (Title + Run)")]
    public static void Build()
    {
        EnemyDevSceneBuilder.BuildBossScene();
        BuildRun();
        BuildTitle();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(TitlePath, true),
            new EditorBuildSettingsScene(RunPath, true),
        };
        AssetDatabase.SaveAssets();
    }

    /// A sala da run: 7 x 5 placas (28 x 20 m). Cenarios e lojas, desenhados para 20 x 16, se espalham nesta proporcao.
    const int RunW = 7, RunD = 5;
    static float SX = 1f, SZ = 1f;

    static void BuildRun()
    {
        LookDevBuilder.RoomW = RunW; LookDevBuilder.RoomD = RunD; LookDevBuilder.SouthApron = true;
        SX = RunW / 5f * 1f; SZ = RunD / 4f;
        try { BuildRunScene(); }
        finally { LookDevBuilder.RoomW = 5; LookDevBuilder.RoomD = 4; LookDevBuilder.SouthApron = false; SX = SZ = 1f; }
    }

    static void BuildRunScene() => CombatDevSceneBuilder.BuildPlayable("Run", RunPath, root =>
    {
        float hx = RunW * 2f, hz = RunD * 2f;
        // o bau decorativo do look-dev fica bem onde a porta do meio abre
        foreach (Transform child in root) if (child.name.StartsWith("chest")) { Object.DestroyImmediate(child.gameObject); break; }
        EnemyDevSceneBuilder.Bake(root, "Assets/Scenes/Run_NavMesh.asset");
        EnemyDevSceneBuilder.Prefabs(out var melee, out var ranged, out var brute, out var bomber);

        var sp = new GameObject("WaveSpawner");
        sp.transform.SetParent(root, false);
        sp.transform.position = new Vector3(0, 0, 1);
        var spawner = sp.AddComponent<WaveSpawner>();
        spawner.SetSpawnRadius(7f * Mathf.Min(SX, SZ) + 1f);
        spawner.SetAutoStart(false);
        EditorUtility.SetDirty(spawner);

        var flat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Feel/Materials/Flat.mat");
        var dir = new GameObject("RunDirector");
        dir.transform.SetParent(root, false);
        var director = dir.AddComponent<RunDirector>();
        director.Bind(melee, ranged, brute, bomber, flat);
        EnemyDevSceneBuilder.ExtraPrefabs(out var charger, out var lobber, out var shaman);
        director.BindExtraEnemies(charger, lobber, shaman);
        EnemyDevSceneBuilder.ElementalPrefabs(out var swarm, out var ember, out var sentinel);
        director.BindElementals(swarm, ember, sentinel);
        director.BindGuard(EnemyDevSceneBuilder.GuardPrefab());
        director.BindBanner(EnemyDevSceneBuilder.BannerPrefab());
        director.BindShade(EnemyDevSceneBuilder.ShadePrefab());
        director.BindCrystal(CrystalPrefab());
        var drowned = EnemyDevSceneBuilder.DrownedPrefab();
        director.BindDrowned(drowned, PuddlePrefab());
        const string catalogPath = "Assets/Game/Run/Data/Boons.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<Quintessence.Run.BoonCatalog>(catalogPath);
        if (catalog == null)
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Run/Data");
            AssetDatabase.Refresh();
            catalog = ScriptableObject.CreateInstance<Quintessence.Run.BoonCatalog>();
            catalog.FillDefaults();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }
        // os textos das bencaos sao do codigo: refaz a lista para o asset nao ficar com descricao antiga
        catalog.FillDefaults();
        EditorUtility.SetDirty(catalog);
        director.BindBoons(catalog);
        director.BindBoss(EnemyDevSceneBuilder.BossPrefab(melee, bomber));
        director.BindMiniBoss(EnemyDevSceneBuilder.MiniBossPrefab(melee, charger));
        director.BindWeaver(EnemyDevSceneBuilder.WeaverPrefab(ranged, melee));
        director.BindQueen(EnemyDevSceneBuilder.QueenPrefab(drowned, melee));
        director.BindCryptMiniBoss(EnemyDevSceneBuilder.PriestessPrefab(drowned, melee));
        director.BindGardenMiniBoss(EnemyDevSceneBuilder.GardenMiniBossPrefab(brute, melee));
        director.BindShop(Prop("table_small", 1f), Prop("bottle_A_labeled_green", 1.8f), Prop("bottle_B_brown", 1.8f), ShopDecor());
        // ordem do enum RoomType: Combat, Elite, Shrine, Rest, Boss, Master, Shop, Treasure, Pact
        string[] banners = { "banner_red", "banner_patternB_brown", "banner_blue", "banner_green", "banner_shield_red", "banner_patternA_yellow", "banner_patternA_green", "banner_yellow", "banner_patternC_red" };
        var bannerPrefabs = new GameObject[banners.Length];
        for (int i = 0; i < banners.Length; i++) bannerPrefabs[i] = Prop(banners[i], 1f);
        var stone = LookDevBuilder.Mat("Stone", null, new Color(0.33f, 0.34f, 0.42f), Color.black, 0f);
        director.BindLayouts(Layouts());
        director.BindRoomSize(new Vector3(0, 0.05f, -(hz - 2.5f)), hz - 0.85f, new Vector2(hx + 0.6f, hz + 0.6f), new Vector2(SX, SZ));
        director.BindTutorial(DummyPrefab());
        director.BindDecor(RoomDecors());
        director.BindMaster(MasterPrefab());
        director.BindBiomeProps(BiomePropSets());
        director.BindProps(Prop("wall_doorway", 1f), bannerPrefabs, Prop("column", 1f), Prop("chest_gold", 1f), stone);
        EditorUtility.SetDirty(director);
        var hud = new GameObject("Hud");
        hud.transform.SetParent(root, false);
        hud.AddComponent<Hud>();
    });

    /// Banca do mercador, so com pecas do pack: prateleiras, balcao, barris, ouro e o vendedor atras do balcao.
    static GameObject ShopDecor()
    {
        const string dir = "Assets/Game/Game/Prefabs/";
        System.IO.Directory.CreateDirectory(dir);
        var mat = LookDevBuilder.Mat("DungeonProp", CombatDevSceneBuilder.KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0f);
        var root = new GameObject("ShopDecor");
        GameObject P(string model, float x, float z, float rot, float scale = 1f, float y = 0f)
        {
            var go = LookDevBuilder.Spawn("Dungeon/" + model + ".fbx", new Vector3(x * SX, y, z * SZ), rot, mat, scale);
            go.name = model;
            go.transform.SetParent(root.transform, true);
            return go;
        }
        float Top(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Debug.Log($"loja {go.name}: tamanho {b.size}");
            return b.max.y;
        }
        void Solid(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var col = root.AddComponent<BoxCollider>();
            col.center = b.center; col.size = b.size;
        }

        // balcao com toalha virado para quem entra; o mercador fica atras, de frente para a camera
        var counter = P("table_long_tablecloth_decorated_A", -6.2f, -1.0f, 90);
        Solid(counter);
        float top = Top(counter);
        P("coin_stack_large", -7.55f, -0.8f, 20, 1f, top);
        P("coin_stack_medium", -7.1f, -1.3f, 0, 1f, top);
        P("coin_stack_small", -7.75f, -1.45f, 50, 1f, top);
        P("candle_triple", -4.75f, -0.75f, 0, 1f, top);
        // estantes atras dele e estoque em volta
        Solid(P("shelves", -7.3f, 2.4f, 0));
        Solid(P("shelves", -5.1f, 2.4f, 0));
        Solid(P("keg_decorated", -9.0f, 0.2f, 60));
        Solid(P("barrel_small_stack", -3.0f, 2.6f, 15));
        P("box_small_decorated", -8.9f, -1.6f, -20);
        // ouro: bau aberto ao lado do balcao com moedas pelo chao
        Solid(P("chest_gold", -3.2f, 0.2f, 165));
        P("coin_stack_large", -4.3f, -0.9f, 0);
        P("coin_stack_medium", -2.2f, -0.9f, 40);
        P("coin_stack_small", -2.5f, -1.6f, 10);
        P("coin_stack_small", -4.3f, -2.1f, 70);
        // lado leste, para a sala nao ficar torta
        Solid(P("trunk_large_A", 8.7f, 1.6f, -90));
        Solid(P("box_stacked", 8.7f, -4.3f, 10));

        // o mercador: o modelo de aventureiro do pack com outra paleta, parado atras do balcao
        const string fbx = CombatDevSceneBuilder.KK + "Adventurers/Characters/Mage.fbx";
        CombatDevSceneBuilder.Loop(fbx, "Idle");
        var ctrl = CombatDevSceneBuilder.Controller(fbx, dir + "Merchant.controller", ("Idle", "Idle", 0f),
            ("Greet", "Interact|Cheer", 1.1f), ("Thanks", "Cheer", 1.1f), ("Refuse", "Hit_A|Hit_B", 0.6f));
        var cloth = LookDevBuilder.Mat("Merchant", CombatDevSceneBuilder.KK + "Adventurers/Props/rogue_texture.png", Color.white, Color.black, 0f);
        var who = LookDevBuilder.Spawn("Adventurers/Characters/Mage.fbx", new Vector3(-6.2f * SX, 0, 0.75f * SZ), 180, cloth);
        who.name = "Merchant";
        LookDevBuilder.Hide(who, "1H_Wand", "2H_Staff", "Spellbook_open");
        who.transform.SetParent(root.transform, true);
        CombatDevSceneBuilder.Animator(who, ctrl);
        who.AddComponent<Merchant>();

        // luz propria para as estantes, que sumiam no escuro do fundo, e velas em cima dos barris
        var shelfLight = new GameObject("ShelfLight").AddComponent<Light>();
        shelfLight.transform.SetParent(root.transform, false);
        shelfLight.transform.position = new Vector3(-6.2f * SX, 2.4f, 1.5f * SZ);
        shelfLight.type = LightType.Point; shelfLight.color = new Color(1f, 0.78f, 0.5f); shelfLight.intensity = 2.6f; shelfLight.range = 5f;
        P("candle_triple", -9.0f, 0.2f, 0, 1f, 1.25f);
        P("banner_patternA_green", -6.2f, 3.05f, 0, 1f, 1.5f);

        var lamp = new GameObject("StallLight").AddComponent<Light>();
        lamp.transform.SetParent(root.transform, false);
        lamp.transform.position = new Vector3(-6.2f * SX, 3.0f, -1.6f * SZ);
        lamp.type = LightType.Point; lamp.color = new Color(1f, 0.72f, 0.42f); lamp.intensity = 2.2f; lamp.range = 7f;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, dir + "ShopDecor.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// Cenario proprio de uma sala sem luta, so com pecas do pack. pieces: (modelo, x, z, rotacao, escala, y, solido).
    static GameObject Decor(string name, Color lightColor, Vector3 lightPos, params (string model, float x, float z, float rot, float scale, float y, bool solid)[] pieces)
    {
        const string dir = "Assets/Game/Game/Prefabs/";
        var mat = LookDevBuilder.Mat("DungeonProp", CombatDevSceneBuilder.KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0f);
        var root = new GameObject(name);
        foreach (var p in pieces)
        {
            var go = LookDevBuilder.Spawn("Dungeon/" + p.model + ".fbx", new Vector3(p.x * SX, p.y, p.z * SZ), p.rot, mat, p.scale);
            go.name = p.model;
            go.transform.SetParent(root.transform, true);
            if (!p.solid) continue;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var col = root.AddComponent<BoxCollider>();
            col.center = b.center; col.size = b.size;
        }
        var lamp = new GameObject("DecorLight").AddComponent<Light>();
        lamp.transform.SetParent(root.transform, false);
        lamp.transform.position = new Vector3(lightPos.x * SX, lightPos.y, lightPos.z * SZ);
        lamp.type = LightType.Point; lamp.color = lightColor; lamp.intensity = 2.2f; lamp.range = 8f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, dir + name + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// Um cenario por tipo de sala (na ordem do enum RoomType); nulo = sala sem cenario proprio.
    static GameObject[] RoomDecors()
    {
        var warm = new Color(1f, 0.74f, 0.45f);
        var list = new GameObject[11];
        // Descanso: acampamento com camas, mesa posta e barril
        list[3] = Decor("RestDecor", warm, new Vector3(0, 2.6f, 1f),
            ("bed_decorated", -7.6f, 2.6f, 90, 1f, 0f, true), ("bed_floor", -7.6f, -0.6f, 90, 1f, 0f, false), ("bed_floor", -7.6f, -2.6f, 80, 1f, 0f, false),
            ("table_medium_decorated_A", 6.6f, 1.6f, 0, 1f, 0f, true), ("stool", 5.2f, 1.2f, 20, 1f, 0f, false), ("stool", 6.9f, 0.1f, -30, 1f, 0f, false),
            ("keg_decorated", 8.4f, -2.2f, -60, 1f, 0f, true), ("barrel_small_stack", 8.3f, 3.8f, 10, 1f, 0f, true),
            ("candle_triple", -8.6f, 0.8f, 0, 1f, 0f, false), ("candle_lit", 0.9f, 1.4f, 0, 1f, 0f, false), ("candle_lit", -0.9f, 1.4f, 0, 1f, 0f, false),
            ("box_small_decorated", -5.8f, 3.9f, 25, 1f, 0f, false), ("banner_green", -7.6f, 3.9f, 0, 1f, 1.6f, false));
        // Mestre: sala de estudo, com estantes, colunas e velas
        list[5] = Decor("MasterDecor", new Color(1f, 0.82f, 0.5f), new Vector3(0, 3f, 2.4f),
            ("shelf_large", -8.7f, 1.2f, 90, 1f, 0f, true), ("shelf_large", 8.7f, 1.2f, -90, 1f, 0f, true),
            ("column", -6.2f, 3.6f, 0, 1f, 0f, true), ("column", 6.2f, 3.6f, 0, 1f, 0f, true),
            ("candle_triple", -3f, 1.6f, 0, 1f, 0f, false), ("candle_triple", 3f, 1.6f, 0, 1f, 0f, false),
            ("table_small_decorated_A", -8.4f, -2.4f, 90, 1f, 0f, true), ("chair", -7.4f, -2.4f, 90, 1f, 0f, false),
            ("banner_patternA_yellow", -3f, 3.9f, 0, 1f, 1.6f, false), ("banner_patternA_yellow", 3f, 3.9f, 0, 1f, 1.6f, false));
        // Tesouro: ouro pelo chao, baus e trofeus
        list[7] = Decor("TreasureDecor", new Color(1f, 0.8f, 0.42f), new Vector3(0, 2.6f, -0.6f),
            ("coin_stack_large", -1.5f, 0.2f, 10, 1f, 0f, false), ("coin_stack_medium", 1.4f, 0.1f, 40, 1f, 0f, false), ("coin_stack_small", 1.9f, 1.2f, 0, 1f, 0f, false),
            ("coin_stack_small", -1.9f, 1.3f, 70, 1f, 0f, false), ("coin_stack_medium", -0.6f, -0.7f, 20, 1f, 0f, false), ("coin_stack_large", 0.9f, -0.8f, 0, 1f, 0f, false),
            ("trunk_large_A", -7.8f, 2.4f, 90, 1f, 0f, true), ("trunk_medium_B", -7.9f, 0.2f, 80, 1f, 0f, true), ("trunk_large_C", 7.8f, 2.4f, -90, 1f, 0f, true),
            ("chest", 7.8f, 0f, -80, 1f, 0f, true), ("sword_shield_gold", -5.4f, 3.95f, 0, 1f, 1.5f, false), ("sword_shield_gold", 5.4f, 3.95f, 0, 1f, 1.5f, false),
            ("barrel_large_decorated", -8.2f, -2.6f, 0, 1f, 0f, true), ("box_stacked", 8.2f, -2.8f, 15, 1f, 0f, true));
        // Pacto: sala fria, escombros, armas quebradas e um circulo de velas
        var pact = new List<(string, float, float, float, float, float, bool)>
        {
            ("rubble_half", -7.6f, 2.6f, 30, 0.55f, 0f, true), ("rubble_half", 7.4f, -2.4f, 200, 0.55f, 0f, true),
            ("sword_shield_broken", -6f, 3.95f, 0, 1f, 1.5f, false), ("sword_shield_broken", 6f, 3.95f, 0, 1f, 1.5f, false),
            ("table_long_broken", 7.6f, 2.8f, 80, 1f, 0f, true), ("banner_patternC_red", 0f, 3.95f, 0, 1f, 1.6f, false),
        };
        for (int i = 0; i < 10; i++)
        {
            float a = i * 36f * Mathf.Deg2Rad;
            pact.Add((i % 2 == 0 ? "candle_thin_lit" : "candle_melted", Mathf.Sin(a) * 8.2f * 0.82f, -1.6f + Mathf.Cos(a) * 2.2f - 1.2f, i * 40f, 1f, 0f, false));
        }
        list[8] = Decor("PactDecor", new Color(0.8f, 0.35f, 0.45f), new Vector3(0, 2.8f, -1f), pact.ToArray());
        // Provacao e Cerco tem luta e arranjo: so pecas presas a parede ou miudas junto dela, nada solido no meio
        // Provacao: estandartes brancos, prateleiras de velas e castiçais (luz fria, sala de prova)
        list[9] = Decor("TrialDecor", new Color(0.75f, 0.85f, 1f), new Vector3(0, 3f, 2f),
            ("banner_shield_white", -9.3f, 3.2f, 90, 1f, 1.5f, false), ("banner_shield_white", 9.3f, 3.2f, -90, 1f, 1.5f, false),
            ("shelf_small_candles", -9.3f, -1f, 90, 1f, 0.9f, false), ("shelf_small_candles", 9.3f, -1f, -90, 1f, 0.9f, false),
            ("candle_triple", -9f, 1.2f, 0, 1f, 0f, false), ("candle_triple", 9f, 1.2f, 0, 1f, 0f, false),
            ("candle_thin_lit", -9f, -3.6f, 0, 1f, 0f, false), ("candle_thin_lit", 9f, -3.6f, 0, 1f, 0f, false));
        // Cerco: posto de guerra, escudos e espadas nas paredes, estandartes vermelhos e tochas (luz quente)
        list[10] = Decor("SiegeDecor", new Color(1f, 0.55f, 0.3f), new Vector3(0, 3f, 1f),
            ("sword_shield", -9.3f, 2.4f, 90, 1f, 1.5f, false), ("sword_shield", 9.3f, 2.4f, -90, 1f, 1.5f, false),
            ("sword_shield_broken", -9.3f, -1.4f, 90, 1f, 1.5f, false), ("sword_shield_broken", 9.3f, -1.4f, -90, 1f, 1.5f, false),
            ("banner_thin_red", -9.3f, 0.5f, 90, 1f, 1.6f, false), ("banner_thin_red", 9.3f, 0.5f, -90, 1f, 1.6f, false),
            ("torch_lit", -9f, 4.2f, 0, 1f, 0f, false), ("torch_lit", 9f, 4.2f, 0, 1f, 0f, false),
            ("keg", -9f, -4.4f, 30, 1f, 0f, false), ("keg", 9f, -4.4f, -20, 1f, 0f, false));
        return list;
    }

    /// Adereços de cada trecho (modelados para o jogo), como obstaculos: colisor no miolo e recorte no NavMesh.
    static RunDirector.BiomeProps[] BiomePropSets()
    {
        AssetDatabase.Refresh();
        HeroBuilder.MatteParts("Assets/Art/Biomes");
        const string dir = "Assets/Game/Game/Prefabs/Biomes/";
        System.IO.Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();
        // ordem do enum Biome: Ashes, Crypt, Garden, Heart
        string[][] files =
        {
            new[] { "Ash/ash_brazier", "Ash/ash_beams", "Ash/ash_anvil", "Ash/ash_cage" },
            new[] { "Crypt/crypt_sarcophagus", "Crypt/crypt_urns", "Crypt/crypt_statue", "Crypt/crypt_pool", "Crypt/crypt_stalagmites" },
            new[] { "Garden/garden_tree", "Garden/garden_mushrooms", "Garden/garden_roots", "Garden/garden_planter", "Garden/garden_statue" },
            new[] { "Heart/heart_spire", "Heart/heart_crystals" },
        };
        var sets = new RunDirector.BiomeProps[files.Length];
        for (int b = 0; b < files.Length; b++)
        {
            var list = new List<GameObject>();
            foreach (var f in files[b])
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Biomes/" + f + ".fbx");
                if (asset == null) { Debug.LogWarning("adereco nao encontrado: " + f); continue; }
                string name = System.IO.Path.GetFileName(f);
                var root = new GameObject(name);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                model.name = "Model";
                model.transform.SetParent(root.transform, false);
                // feitos com 1 a 2,5 m; ao lado das pecas do pack pediam um pouco mais de corpo
                model.transform.localScale = Vector3.one * 1.35f;
                var rs = model.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var bounds = rs[0].bounds; foreach (var r in rs) bounds.Encapsulate(r.bounds);
                    // poca e bacia sao baixas: anda-se em volta, mas o colisor nao precisa subir
                    var size = new Vector3(bounds.size.x * 0.75f, Mathf.Max(0.4f, bounds.size.y), bounds.size.z * 0.75f);
                    var col = root.AddComponent<BoxCollider>();
                    col.center = new Vector3(bounds.center.x, size.y / 2f, bounds.center.z); col.size = size;
                    var carve = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    carve.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                    carve.center = col.center; carve.size = size + new Vector3(0.3f, 0, 0.3f);
                    carve.carving = true;
                    Debug.Log($"adereco {name}: tamanho {bounds.size}");
                }
                list.Add(PrefabUtility.SaveAsPrefabAsset(root, dir + name + ".prefab"));
                Object.DestroyImmediate(root);
            }
            sets[b] = new RunDirector.BiomeProps { props = list.ToArray() };
        }
        return sets;
    }

    /// O Mestre da sala de bencaos: o mesmo corpo do Mercador, com o cajado e outra paleta.
    static GameObject MasterPrefab()
    {
        const string dir = "Assets/Game/Game/Prefabs/";
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(dir + "Merchant.controller");
        var robe = LookDevBuilder.Mat("Master", CombatDevSceneBuilder.KK + "Adventurers/Props/knight_texture.png", new Color(0.86f, 0.82f, 0.92f), Color.black, 0f);
        var who = LookDevBuilder.Spawn("Adventurers/Characters/Mage.fbx", Vector3.zero, 0, robe);
        who.name = "Mestre";
        who.transform.SetParent(null, false);
        LookDevBuilder.Hide(who, "1H_Wand", "Spellbook_open");
        if (ctrl != null) CombatDevSceneBuilder.Animator(who, ctrl);
        var npc = who.AddComponent<Merchant>();
        npc.SetLines("Escolha uma bênção, aprendiz.", "Que ela te sirva bem.", "");
        var lamp = new GameObject("MasterLight").AddComponent<Light>();
        lamp.transform.SetParent(who.transform, false);
        lamp.transform.localPosition = new Vector3(0, 2.6f, 1.2f);
        lamp.type = LightType.Point; lamp.color = new Color(1f, 0.85f, 0.6f); lamp.intensity = 1.6f; lamp.range = 5f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(who, dir + "Master.prefab");
        Object.DestroyImmediate(who);
        return prefab;
    }

    /// Boneco de treino: apanha, nao revida e volta depois de cair.
    static GameObject DummyPrefab()
    {
        const string dir = "Assets/Game/Game/Prefabs/";
        var mat = LookDevBuilder.Mat("Skeleton", CombatDevSceneBuilder.KK + "Skeletons/Characters/skeleton_texture.png", Color.white, Color.black, 0f);
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/Combat/Animation/Dummy.controller");
        var go = new GameObject("TrainingDummy");
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 2.2f; col.radius = 0.55f; col.center = new Vector3(0, 1.1f, 0);
        go.AddComponent<Quintessence.Core.Health>().Configure(150, Quintessence.Core.Team.Enemy);
        go.AddComponent<Quintessence.Combat.DummyTarget>();
        var model = LookDevBuilder.Spawn("Skeletons/Characters/Skeleton_Minion.fbx", Vector3.zero, 0, mat);
        model.name = "Model";
        model.transform.SetParent(go.transform, false);
        if (ctrl != null) CombatDevSceneBuilder.Animator(model, ctrl);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, dir + "TrainingDummy.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// Prefab de uma peca do pack com o material do jogo.
    static GameObject Prop(string model, float scale)
    {
        const string dir = "Assets/Game/Game/Prefabs/";
        System.IO.Directory.CreateDirectory(dir);
        var mat = LookDevBuilder.Mat("DungeonProp", CombatDevSceneBuilder.KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0f);
        var go = LookDevBuilder.Spawn("Dungeon/" + model + ".fbx", Vector3.zero, 0, mat, scale);
        go.name = model;
        go.transform.SetParent(null);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, dir + model + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// Peca de cobertura: modelo do pack com colisor e recorte no NavMesh (os inimigos desviam sem refazer o mapa).
    static GameObject Obstacle(string model, float scale)
    {
        const string dir = "Assets/Game/Game/Prefabs/Obstacles/";
        System.IO.Directory.CreateDirectory(dir);
        var mat = LookDevBuilder.Mat("DungeonProp", CombatDevSceneBuilder.KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0f);
        var root = new GameObject(model);
        var visual = LookDevBuilder.Spawn("Dungeon/" + model + ".fbx", Vector3.zero, 0, mat, scale);
        visual.name = "Model";
        visual.transform.SetParent(root.transform, false);
        var rs = visual.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var x in rs) b.Encapsulate(x.bounds);
        var col = root.AddComponent<BoxCollider>();
        col.center = b.center; col.size = b.size;
        var carve = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        carve.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        carve.center = b.center; carve.size = b.size + new Vector3(0.3f, 0, 0.3f);
        carve.carving = true;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, dir + model + ".prefab");
        Object.DestroyImmediate(root);
        Debug.Log($"obstaculo {model} x{scale}: tamanho {b.size}");
        return prefab;
    }

    static GameObject Vent()
    {
        const string path = "Assets/Game/Game/Prefabs/Obstacles/EmberVent.prefab";
        var root = new GameObject("EmberVent");
        root.AddComponent<EmberVent>();
        void Disc(string name, float diameter, float y, Material m)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(d.GetComponent<Collider>());
            d.name = name;
            d.transform.SetParent(root.transform, false);
            d.transform.localPosition = new Vector3(0, y, 0);
            d.transform.localScale = new Vector3(diameter, 0.012f, diameter);
            d.GetComponent<Renderer>().sharedMaterial = m;
        }
        Disc("Scorch", 3.2f, 0.012f, LookDevBuilder.Mat("Scorch2", null, new Color(0.2f, 0.15f, 0.15f), Color.black, 0f));
        Disc("Core", 0.8f, 0.02f, LookDevBuilder.Mat("Glow_Ember", null, new Color(0.6f, 0.1f, 0.01f), new Color(1.15f, 0.18f, 0.02f), 0f));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// Barril de polvora: barril do pack em tom avermelhado, com vida curta. Qualquer golpe acende.
    /// Cristal de Ressonancia: tres lascas de cristal (a mesma malha dos cristais ambar do cenario) num material que o jogo
    /// pinta com a cor do elemento do trecho. Um golpe estoura.
    static GameObject CrystalPrefab()
    {
        const string path = "Assets/Game/Game/Prefabs/Obstacles/ResonanceCrystal.prefab";
        var mat = LookDevBuilder.Mat("ResonanceCrystal", null, new Color(0.7f, 0.7f, 0.75f), new Color(0.25f, 0.25f, 0.28f), 0f);
        var root = new GameObject("ResonanceCrystal");
        var renderers = new List<Renderer>();
        foreach (var (pos, euler, w, h) in new[] { (new Vector3(0f, 0f, 0f), new Vector3(0, 0, 0), 0.36f, 1.5f), (new Vector3(0.32f, 0f, 0.1f), new Vector3(0, 30, -22), 0.24f, 0.95f), (new Vector3(-0.28f, 0f, -0.12f), new Vector3(0, 70, 25), 0.22f, 0.8f) })
        {
            var c = new GameObject("Shard");
            c.transform.SetParent(root.transform, false);
            c.transform.localPosition = pos; c.transform.localRotation = Quaternion.Euler(euler); c.transform.localScale = new Vector3(w, h, w);
            c.AddComponent<MeshFilter>().sharedMesh = LookDevBuilder.CrystalMesh();
            var r = c.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            renderers.Add(r);
        }
        var col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.75f, 0); col.radius = 0.45f; col.height = 1.5f;
        var carve = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        carve.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule; carve.center = new Vector3(0, 0.75f, 0); carve.radius = 0.5f; carve.height = 1.5f;
        carve.carving = true;
        root.AddComponent<Quintessence.Core.Health>().Configure(1, Quintessence.Core.Team.Enemy);
        root.AddComponent<ResonanceCrystal>().Bind(renderers.ToArray());
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject ExplosivePrefab()
    {
        const string path = "Assets/Game/Game/Prefabs/Obstacles/ExplosiveBarrel.prefab";
        var mat = LookDevBuilder.Mat("PowderBarrel", CombatDevSceneBuilder.KK + "Dungeon/dungeon_texture.png", new Color(1.1f, 0.66f, 0.56f), Color.black, 0f);
        var root = new GameObject("ExplosiveBarrel");
        var visual = LookDevBuilder.Spawn("Dungeon/barrel_small.fbx", Vector3.zero, 0, mat, 1.25f);
        visual.name = "Model";
        visual.transform.SetParent(root.transform, false);
        var rs = visual.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
        var col = root.AddComponent<CapsuleCollider>();
        col.center = b.center; col.radius = Mathf.Max(b.extents.x, b.extents.z); col.height = b.size.y;
        var carve = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        carve.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        carve.center = b.center; carve.size = b.size + new Vector3(0.3f, 0, 0.3f);
        carve.carving = true;
        root.AddComponent<Quintessence.Core.Health>().Configure(8, Quintessence.Core.Team.Enemy);
        root.AddComponent<ExplosiveBarrel>();
        // faixa escura no chao: marca ate onde a explosao chega
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
        ring.name = "Scorch";
        ring.transform.SetParent(root.transform, false);
        ring.transform.localPosition = new Vector3(0, 0.012f, 0);
        ring.transform.localScale = new Vector3(2.2f, 0.01f, 2.2f);
        ring.GetComponent<Renderer>().sharedMaterial = LookDevBuilder.Mat("Scorch2", null, new Color(0.2f, 0.15f, 0.15f), Color.black, 0f);
        Debug.Log($"barril explosivo: tamanho {b.size}");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// Armadilha de espinhos: a placa do pack; os espinhos ficam recolhidos e sobem no golpe.
    static GameObject SpikesPrefab()
    {
        const string path = "Assets/Game/Game/Prefabs/Obstacles/SpikeTrap.prefab";
        var mat = LookDevBuilder.Mat("DungeonProp", CombatDevSceneBuilder.KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0f);
        var root = new GameObject("SpikeTrap");
        var visual = LookDevBuilder.Spawn("Dungeon/floor_tile_big_spikes.fbx", Vector3.zero, 0, mat, 0.7f);
        visual.name = "Model";
        visual.transform.SetParent(root.transform, false);
        var rs = visual.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
        var names = new System.Text.StringBuilder();
        Transform moving = null;
        foreach (var t in visual.GetComponentsInChildren<Transform>(true))
        {
            names.Append(t.name).Append(' ');
            if (t != visual.transform && t.name.ToLowerInvariant().Contains("spike") && t.GetComponent<Renderer>() != null && moving == null) moving = t;
        }
        Debug.Log($"espinhos: tamanho {b.size} min {b.min} filhos: {names} movel: {(moving != null ? moving.name : "modelo inteiro")}");
        // sem peca separada, o modelo inteiro sobe: fica quase todo abaixo do piso ate o golpe
        if (moving == null) moving = visual.transform;
        root.AddComponent<SpikeTrap>().Bind(moving, moving == visual.transform ? -(b.size.y - 0.06f) : -b.size.y * 0.8f / Mathf.Max(0.01f, moving.parent.lossyScale.y));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// Poca de corrupcao: manchas redondas sobrepostas, borda de cinza escura e superficie de brilho baixo
    /// (no mesmo registro dos respiradouros). width x depth em metros: a area que fere.
    /// Poca d'agua do Afogado: raio 2 (o jogo escala para o raio do golpe). Agua parada, fosca, um pouco de brilho frio.
    static GameObject PuddlePrefab()
    {
        const string path = "Assets/Game/Game/Prefabs/Obstacles/WaterPuddle.prefab";
        var root = new GameObject("WaterPuddle");
        var rim = LookDevBuilder.Mat("PuddleRim2", null, new Color(0.09f, 0.11f, 0.12f), Color.black, 0f);
        var water = LookDevBuilder.Mat("Puddle_Water", null, new Color(0.07f, 0.17f, 0.21f), new Color(0.01f, 0.04f, 0.05f), 0f);
        Disc(root.transform, "Rim", Vector3.zero, 4.4f, 0.012f, rim);
        Disc(root.transform, "Water", Vector3.zero, 4f, 0.022f, water);
        Disc(root.transform, "Ripple", new Vector3(0.5f, 0, -0.3f), 1.6f, 0.026f, rim);
        Disc(root.transform, "RippleIn", new Vector3(0.5f, 0, -0.3f), 1.3f, 0.028f, water);
        root.AddComponent<WaterPuddle>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject PoolPrefab(string name, float width, float depth, int seed)
    {
        string path = "Assets/Game/Game/Prefabs/Obstacles/" + name + ".prefab";
        var root = new GameObject(name);
        var rimMat = LookDevBuilder.Mat("Scorch2", null, new Color(0.2f, 0.15f, 0.15f), Color.black, 0f);
        var glowMat = LookDevBuilder.Mat("Glow_Pool3", null, new Color(0.13f, 0.08f, 0.08f), new Color(0.12f, 0.02f, 0.01f), 0f);
        var crackMat = LookDevBuilder.Mat("Glow_PoolCrack", null, new Color(0.6f, 0.1f, 0.01f), new Color(1f, 0.16f, 0.02f), 0f);
        var cracks = new System.Collections.Generic.List<Renderer>();
        var surfaces = new System.Collections.Generic.List<Renderer>();
        var rng = new System.Random(seed);
        // manchas ao longo do lado maior, com tamanho e deslocamento variados
        bool alongX = width >= depth;
        float length = alongX ? width : depth, thick = alongX ? depth : width;
        int blobs = Mathf.Max(2, Mathf.RoundToInt(length / (thick * 0.8f)));
        for (int b = 0; b < blobs; b++)
        {
            float t = blobs == 1 ? 0f : (b / (float)(blobs - 1) - 0.5f) * (length - thick);
            float side = ((float)rng.NextDouble() - 0.5f) * thick * 0.25f;
            float d = thick * (0.95f + (float)rng.NextDouble() * 0.35f);
            var pos = alongX ? new Vector3(t, 0, side) : new Vector3(side, 0, t);
            Disc(root.transform, "Rim", pos, d + 0.45f, 0.011f, rimMat);
            surfaces.Add(Disc(root.transform, "Surface", pos, d, 0.02f + b * 0.0005f, glowMat));
            // veios de brasa cruzando a mancha, como as rachaduras do chao da sala
            for (int c = 0; c < 3; c++)
            {
                float ang = (float)rng.NextDouble() * 180f, len = d * (0.45f + (float)rng.NextDouble() * 0.35f);
                var off = new Vector3(((float)rng.NextDouble() - 0.5f) * d * 0.4f, 0, ((float)rng.NextDouble() - 0.5f) * d * 0.4f);
                var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(crack.GetComponent<Collider>());
                crack.name = "Vein";
                crack.transform.SetParent(root.transform, false);
                crack.transform.localPosition = pos + off + Vector3.up * 0.03f;
                crack.transform.localRotation = Quaternion.Euler(0, ang, 0);
                crack.transform.localScale = new Vector3(0.07f + (float)rng.NextDouble() * 0.06f, 0.02f, len);
                var cr = crack.GetComponent<Renderer>();
                cr.sharedMaterial = crackMat;
                cracks.Add(cr);
            }
        }
        root.AddComponent<CorruptionPool>().Bind(new Vector3(width, 1.2f, depth), surfaces.ToArray(), cracks.ToArray());
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Renderer Disc(Transform parent, string name, Vector3 pos, float diameter, float y, Material m)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(d.GetComponent<Collider>());
        d.name = name;
        d.transform.SetParent(parent, false);
        d.transform.localPosition = new Vector3(pos.x, y, pos.z);
        d.transform.localScale = new Vector3(diameter, 0.008f, diameter);
        var r = d.GetComponent<Renderer>();
        r.sharedMaterial = m;
        return r;
    }

    /// Os seis arranjos iniciais. Depois de criados sao assets: da pra mover peca no Inspector sem tocar no codigo.
    static RoomLayout[] Layouts()
    {
        const string dir = "Assets/Game/Game/Layouts/";
        System.IO.Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();
        var pillar = Obstacle("pillar", 0.8f);
        var barrier = Obstacle("barrier", 1f);
        var crates = Obstacle("crates_stacked", 1f);
        var barrel = Obstacle("barrel_large", 1f);
        var box = Obstacle("box_large", 1f);
        var rubble = Obstacle("rubble_half", 0.55f);
        var column = Obstacle("column", 1f);
        var vent = Vent();

        var list = new List<RoomLayout>();
        RoomLayout Make(string file, string name, params (GameObject prefab, float x, float z, float rot)[] pieces)
        {
            string path = dir + file + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<RoomLayout>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<RoomLayout>();
                a.displayName = name;
                foreach (var p in pieces) a.pieces.Add(new LayoutPiece { prefab = p.prefab, position = new Vector3(p.x, 0, p.z), rotation = p.rot });
                AssetDatabase.CreateAsset(a, path);
            }
            list.Add(a);
            return a;
        }
        Make("1_Pilares", "Pilares", (pillar, -4.5f, 2.5f, 0), (pillar, 4.5f, 2.5f, 0), (pillar, -4.5f, -2.5f, 0), (pillar, 4.5f, -2.5f, 0));
        Make("2_Corredor", "Corredor", (barrier, -5f, 1.5f, 0), (barrier, 5f, 1.5f, 0), (barrier, 0f, -1.8f, 0), (column, -7.5f, -3.5f, 0), (column, 7.5f, -3.5f, 0));
        Make("3_Praca", "Praça", (crates, 0f, 0.6f, 15), (barrel, 1.7f, 1.3f, 0), (box, -1.6f, -0.2f, 30), (barrel, -0.9f, 2.1f, 0), (box, 5.5f, -2.5f, 10), (barrel, -5.5f, 3f, 0));
        Make("4_Ruinas", "Ruínas", (rubble, -5f, 1f, 20), (rubble, 4f, -1.5f, 110), (rubble, 1f, 3.4f, 200), (pillar, -2.5f, -1f, 0), (column, 5.5f, 3f, 0));
        Make("5_Fornalha", "Fornalha", (vent, -4.5f, 0.5f, 0), (vent, 4.5f, 0.5f, 0), (vent, 0f, 3.2f, 0), (column, -2f, -1.6f, 0), (column, 2f, -1.6f, 0));
        var powder = ExplosivePrefab();
        var spikes = SpikesPrefab();
        Make("7_Paiol", "Paiol", (powder, -3.4f, 1.2f, 0), (powder, 3.4f, 1.2f, 20), (powder, 0f, 3.4f, 40), (crates, -6.5f, -1.2f, 10), (crates, 6.5f, -1.2f, -10), (pillar, 0f, -1.4f, 0));
        Make("8_Espinhos", "Espinhos", (spikes, -3.4f, 0.6f, 0), (spikes, 0f, 0.6f, 0), (spikes, 3.4f, 0.6f, 0), (barrier, -6.4f, 2.6f, 0), (barrier, 6.4f, 2.6f, 0), (column, -6.5f, -3.2f, 0), (column, 6.5f, -3.2f, 0));
        Make("9_Arena", "Arena", (column, -5f, 3f, 0), (column, 5f, 3f, 0), (column, -5f, -2.2f, 0), (column, 5f, -2.2f, 0), (powder, 0f, 0.8f, 0), (rubble, 0f, 3.6f, 90));
        Make("10_Trincheira", "Trincheira", (barrier, -3f, 0.4f, 0), (barrier, 3f, 0.4f, 0), (barrier, -6.6f, -2f, 90), (barrier, 6.6f, -2f, 90), (spikes, 0f, 3f, 0), (powder, -7.2f, 3.4f, 0), (powder, 7.2f, 3.4f, 0));
        Make("11_Escombros", "Escombros", (rubble, -5.6f, 2.6f, 40), (rubble, 5.2f, 2.2f, 150), (rubble, -1.5f, 0.2f, 80), (pillar, 3f, -1.8f, 0), (pillar, -5f, -2.4f, 0), (vent, 1.8f, 2.8f, 0), (box, 7.2f, -3.4f, 20));
        Make("12_Camara", "Câmara", (vent, 0f, 0.4f, 0), (spikes, -4.6f, 2.6f, 0), (spikes, 4.6f, 2.6f, 0), (pillar, -5f, -2.2f, 0), (pillar, 5f, -2.2f, 0), (powder, 0f, 3.6f, 0));
        var wall = Obstacle("wall_half", 1f);
        Make("13_Ponte", "Ponte", (wall, -6f, 0.5f, 0), (wall, 6f, 0.5f, 0), (column, 0f, 2.8f, 0), (powder, -8.2f, 3.2f, 0), (powder, 8.2f, 3.2f, 0));
        Make("14_Anel", "Anel", (crates, 0f, 0.8f, 15), (barrel, 1.7f, 0.1f, 0), (box, -1.6f, 1.7f, 30), (pillar, -5.6f, 0.5f, 0), (pillar, 5.6f, 0.5f, 0), (vent, 0f, 3.4f, 0));
        Make("15_Diagonal", "Diagonal", (barrier, -4.6f, 2.2f, 45), (barrier, -1.4f, -0.6f, 45), (barrier, 3.6f, 1.6f, -45), (column, 6.6f, -2.6f, 0), (spikes, 0.6f, 3.1f, 0));
        Make("16_Colunata", "Colunata", (column, -6f, 2.6f, 0), (column, -2f, 2.6f, 0), (column, 2f, 2.6f, 0), (column, 6f, 2.6f, 0), (column, -4f, -1.6f, 0), (column, 4f, -1.6f, 0), (powder, 0f, 0.5f, 0));
        var strip = PoolPrefab("CorruptionStrip", 6f, 1.8f, 3);
        var pool = PoolPrefab("CorruptionPool", 3f, 3f, 7);
        Make("17_Travessia", "Travessia", (strip, -4.6f, 1.2f, 0), (strip, 4.6f, 1.2f, 0), (column, 0f, 3.4f, 0), (pillar, -7.2f, -3.2f, 0), (pillar, 7.2f, -3.2f, 0));
        Make("18_Charcos", "Charcos", (pool, -4.2f, 2.4f, 0), (pool, 3.8f, -0.4f, 0), (pool, -1.2f, -2.2f, 0), (pillar, 5.2f, 3.2f, 0), (box, -7.4f, -1.4f, 20));
        // arranjos com estrutura: barreiras baixas mudam a forma da sala (corredores, um fortim, uma sala em L, uma galeria)
        Make("19_Corredores", "Três Corredores", (barrier, -3.2f, -1.4f, 90), (barrier, -3.2f, 2.6f, 90), (barrier, 3.2f, -1.4f, 90), (barrier, 3.2f, 2.6f, 90),
            (column, -6.8f, -4f, 0), (column, 6.8f, -4f, 0));
        Make("20_Fortim", "Fortim", (barrier, 0f, 2.4f, 0), (barrier, -2.2f, 0.4f, 90), (barrier, 2.2f, 0.4f, 90), (powder, 0f, 0.8f, 0),
            (pillar, -6f, -2.6f, 0), (pillar, 6f, -2.6f, 0));
        Make("21_Bastiao", "Bastião em L", (barrier, 5.2f, -1f, 0), (barrier, 3.2f, -3f, 90), (rubble, 6f, -4f, 30), (crates, 6.8f, -5.6f, 10),
            (pillar, -5f, 2f, 0), (column, -6f, -2f, 0));
        Make("22_Galeria", "Galeria", (barrier, -5.5f, 2.4f, 30), (barrier, -1.8f, 0.2f, -30), (barrier, 1.8f, 2.4f, 30), (barrier, 5.5f, 0.2f, -30),
            (spikes, 3.6f, -2.6f, 0), (column, -6.8f, -3.6f, 0));
        Make("6_Cruz", "Cruz", (barrier, 0f, 2.6f, 0), (barrier, -4.2f, -0.8f, 90), (barrier, 4.2f, -0.8f, 90), (vent, -4.2f, 3.6f, 0), (vent, 4.2f, 3.6f, 0));
        // arranjos de um trecho so: aparecem nele com o triplo de chance e nunca nos outros
        void Home(RoomLayout layout, params Quintessence.Run.Biome[] biomes)
        {
            layout.biomes = new List<Quintessence.Run.Biome>(biomes);
            EditorUtility.SetDirty(layout);
        }
        Home(Make("23_Forja", "Forja", (vent, -3.6f, 2.8f, 0), (vent, 3.6f, 2.8f, 0), (vent, -3.6f, -0.8f, 0), (vent, 3.6f, -0.8f, 0),
            (barrier, 0f, 1f, 90), (powder, 0f, 3.8f, 0), (column, -7f, -3f, 0), (column, 7f, -3f, 0)), Quintessence.Run.Biome.Ashes);
        Home(Make("24_Cisterna", "Cisterna", (pool, -4.6f, 2.4f, 0), (pool, 4.6f, 2.4f, 0), (pool, -4.6f, -1.8f, 0), (pool, 4.6f, -1.8f, 0),
            (column, 0f, 0.4f, 0), (pillar, 0f, 3.8f, 0)), Quintessence.Run.Biome.Crypt);
        Home(Make("25_Bosque", "Bosque de Pedra", (pillar, -6f, 2.8f, 0), (pillar, -2.4f, 3.4f, 0), (pillar, 2.2f, 2.6f, 0), (pillar, 6.2f, 3.2f, 0),
            (pillar, -4.4f, 0f, 0), (pillar, 0.2f, 0.8f, 0), (pillar, 4.6f, -0.2f, 0), (pillar, -6.6f, -3f, 0), (pillar, 6.4f, -3.2f, 0), (rubble, -1.8f, -2.4f, 60)), Quintessence.Run.Biome.Garden);
        Home(Make("26_Pedreira", "Pedreira", (rubble, -5f, 2.4f, 30), (rubble, -1f, 3.4f, 120), (rubble, 3.6f, 2f, 200), (rubble, 6.4f, -1f, 80),
            (spikes, -3f, 0f, 0), (spikes, 2.4f, -0.8f, 0), (column, -6.8f, -3.2f, 0)), Quintessence.Run.Biome.Garden);
        Home(Make("27_Canais", "Canais", (strip, -3.4f, 1.2f, 90), (strip, 3.4f, 1.2f, 90), (pillar, 0f, 3.6f, 0), (pillar, -7f, -2.8f, 0), (pillar, 7f, -2.8f, 0)), Quintessence.Run.Biome.Crypt);
        // segunda leva (cantoneiras: 0 = quina a sudoeste, cada +90 gira no sentido horario): mobilia do proprio pack (mesas, barris, baus, cantoneiras, escombros grandes) muda a leitura da sala
        var table = Obstacle("table_long", 1f);
        var tableBroken = Obstacle("table_long_broken", 1f);
        var keg = Obstacle("keg", 1f);
        var trunk = Obstacle("trunk_large_A", 1f);
        var boxes = Obstacle("box_stacked", 1f);
        var corner = Obstacle("barrier_corner", 1f);
        var bigRubble = Obstacle("rubble_large", 0.8f);
        var shrinePillar = Obstacle("pillar_decorated", 0.8f);
        Make("28_Refeitorio", "Refeitório", (table, -4.2f, 1.4f, 0), (table, 4.2f, 1.4f, 0), (tableBroken, 0f, 3.6f, 90), (keg, -7.4f, -2.6f, 0), (keg, 7.4f, -2.6f, 0),
            (barrel, -1.2f, -1.4f, 0));
        Home(Make("29_Arsenal", "Arsenal", (boxes, -6f, 2.6f, 10), (trunk, -3.2f, 3.4f, 0), (vent, -2.8f, 0.2f, 0), (vent, 2.8f, 0.2f, 0), (trunk, 3.4f, 3.4f, 180),
            (boxes, 6f, 2.6f, -10), (powder, 0f, 2.4f, 0)), Quintessence.Run.Biome.Ashes);
        Home(Make("30_Ossario", "Ossário", (corner, -5f, 2.6f, 90), (corner, 5f, 2.6f, 180), (corner, -5f, -1.6f, 0), (corner, 5f, -1.6f, 270),
            (pool, 0f, 0.8f, 0), (shrinePillar, 0f, 3.7f, 0)), Quintessence.Run.Biome.Crypt);
        Home(Make("31_Clareira", "Clareira", (bigRubble, -4.6f, 2.6f, 20), (bigRubble, 4.6f, 2.6f, 160), (bigRubble, -5.8f, -1.8f, 70), (bigRubble, 5.8f, -1.8f, 110),
            (shrinePillar, 0f, 1f, 0), (spikes, -2.6f, 0.4f, 0), (spikes, 2.6f, 0.4f, 0)), Quintessence.Run.Biome.Garden);
        return list.ToArray();
    }

    static void BuildTitle()
    {
        var root = LookDevBuilder.BeginArena("Title");
        var cam = LookDevBuilder.MakeCamera();
        cam.fieldOfView = 38;
        cam.transform.SetPositionAndRotation(new Vector3(0, 6.6f, -11.5f), Quaternion.Euler(27, 0, 0));
        // o bau decorativo do look-dev sai: o centro do palco e do altar
        foreach (Transform child in root) if (child.name.StartsWith("chest")) { Object.DestroyImmediate(child.gameObject); break; }
        var prop = LookDevBuilder.Mat("DungeonProp", CombatDevSceneBuilder.KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0f);
        Color[] colors = { new(0.86f, 0.36f, 0.14f), new(0.22f, 0.52f, 0.86f), new(0.66f, 0.5f, 0.24f), new(0.6f, 0.86f, 0.76f) };
        GameObject Stone(string name, Vector3 pos, float size, Color c, float glow)
        {
            var st = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(st.GetComponent<Collider>());
            st.name = name;
            st.transform.SetParent(root, false);
            st.transform.position = pos;
            st.transform.localScale = Vector3.one * size;
            st.GetComponent<Renderer>().sharedMaterial = LookDevBuilder.Mat("Title_" + name, null, c * 0.85f, c * glow, 0f);
            return st;
        }
        // altar dos cinco: quatro pedestais em arco, um por elemento, e o do meio, mais alto, ainda apagado
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 2.6f, z = 3.4f - Mathf.Abs(i - 1.5f) * 0.7f;
            var col = LookDevBuilder.Spawn("Dungeon/column.fbx", new Vector3(x, 0, z), 0, prop);
            var rs = col.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Stone("ElementStone" + i, new Vector3(x, b.max.y + 0.42f, z), 0.8f, colors[i], 0.12f);
            var l = new GameObject("ElementLight" + i).AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.position = new Vector3(x, b.max.y + 1.1f, z - 0.9f);
            l.type = LightType.Point; l.color = colors[i]; l.intensity = 1.4f; l.range = 6f;
        }
        var mid = LookDevBuilder.Spawn("Dungeon/pillar_decorated.fbx", new Vector3(0, 0, 5.2f), 0, prop, 0.6f);
        var mrs = mid.GetComponentsInChildren<Renderer>();
        var mb = mrs[0].bounds; foreach (var r in mrs) mb.Encapsulate(r.bounds);
        Stone("QuintStone", new Vector3(0, mb.max.y + 0.5f, 5.2f), 0.95f, new Color(0.72f, 0.7f, 0.76f), 0.05f);
        var ql = new GameObject("QuintLight").AddComponent<Light>();
        ql.transform.SetParent(root, false);
        ql.transform.position = new Vector3(0, mb.max.y + 1.4f, 4.2f);
        ql.type = LightType.Point; ql.color = new Color(0.9f, 0.88f, 1f); ql.intensity = 0.8f; ql.range = 7f;

        // o aprendiz diante do altar, de costas para a camera
        const string heroFbx = CombatDevSceneBuilder.KK + "Adventurers/Characters/Mage.fbx";
        CombatDevSceneBuilder.Loop(heroFbx, "Idle");
        var idle = CombatDevSceneBuilder.Controller(heroFbx, "Assets/Game/Game/Prefabs/TitleHero.controller", ("Idle", "Idle", 0f), ("Cheer", "Cheer|Interact|Idle", 0f));
        // os quatro herois, cada um diante do pedestal do seu elemento, de frente para quem escolhe
        HeroBuilder.Prepare();
        for (int i = 0; i < 4; i++)
        {
            var hero = HeroBuilder.Build(i, idle);
            hero.name = "TitleHero" + i;
            hero.transform.SetParent(root, false);
            float hx = (i - 1.5f) * 2.6f, hz = 3.4f - Mathf.Abs(i - 1.5f) * 0.7f - 1.9f;
            hero.transform.SetPositionAndRotation(new Vector3(hx, 0, hz), Quaternion.Euler(0, 180 + (1.5f - i) * 9f, 0));
        }
        var go = new GameObject("TitleController");
        go.transform.SetParent(root, false);
        go.AddComponent<TitleController>();
        go.AddComponent<TitleMenu>();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), TitlePath);
    }
}
