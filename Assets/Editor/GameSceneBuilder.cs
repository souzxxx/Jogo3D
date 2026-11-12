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

    static void BuildRun() => CombatDevSceneBuilder.BuildPlayable("Run", RunPath, root =>
    {
        // o bau decorativo do look-dev fica bem onde a porta do meio abre
        foreach (Transform child in root) if (child.name.StartsWith("chest")) { Object.DestroyImmediate(child.gameObject); break; }
        EnemyDevSceneBuilder.Bake(root, "Assets/Scenes/Run_NavMesh.asset");
        EnemyDevSceneBuilder.Prefabs(out var melee, out var ranged, out var brute, out var bomber);

        var sp = new GameObject("WaveSpawner");
        sp.transform.SetParent(root, false);
        sp.transform.position = new Vector3(0, 0, 1);
        var spawner = sp.AddComponent<WaveSpawner>();
        spawner.SetAutoStart(false);
        EditorUtility.SetDirty(spawner);

        var flat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Feel/Materials/Flat.mat");
        var dir = new GameObject("RunDirector");
        dir.transform.SetParent(root, false);
        var director = dir.AddComponent<RunDirector>();
        director.Bind(melee, ranged, brute, bomber, flat);
        EnemyDevSceneBuilder.ExtraPrefabs(out var charger, out var lobber, out var shaman);
        director.BindExtraEnemies(charger, lobber, shaman);
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
        director.BindShop(Prop("table_small", 1f), Prop("bottle_A_labeled_green", 1.8f), Prop("bottle_B_brown", 1.8f), ShopDecor());
        // ordem do enum RoomType: Combat, Elite, Shrine, Rest, Boss, Master, Shop
        string[] banners = { "banner_red", "banner_patternB_brown", "banner_blue", "banner_green", "banner_shield_red", "banner_patternA_yellow", "banner_patternA_green" };
        var bannerPrefabs = new GameObject[banners.Length];
        for (int i = 0; i < banners.Length; i++) bannerPrefabs[i] = Prop(banners[i], 1f);
        var stone = LookDevBuilder.Mat("Stone", null, new Color(0.33f, 0.34f, 0.42f), Color.black, 0f);
        director.BindLayouts(Layouts());
        director.BindTutorial(DummyPrefab());
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
            var go = LookDevBuilder.Spawn("Dungeon/" + model + ".fbx", new Vector3(x, y, z), rot, mat, scale);
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
        var who = LookDevBuilder.Spawn("Adventurers/Characters/Mage.fbx", new Vector3(-6.2f, 0, 0.75f), 180, cloth);
        who.name = "Merchant";
        LookDevBuilder.Hide(who, "1H_Wand", "2H_Staff", "Spellbook_open");
        who.transform.SetParent(root.transform, true);
        CombatDevSceneBuilder.Animator(who, ctrl);
        who.AddComponent<Merchant>();

        // luz propria para as estantes, que sumiam no escuro do fundo, e velas em cima dos barris
        var shelfLight = new GameObject("ShelfLight").AddComponent<Light>();
        shelfLight.transform.SetParent(root.transform, false);
        shelfLight.transform.position = new Vector3(-6.2f, 2.4f, 1.5f);
        shelfLight.type = LightType.Point; shelfLight.color = new Color(1f, 0.78f, 0.5f); shelfLight.intensity = 2.6f; shelfLight.range = 5f;
        P("candle_triple", -9.0f, 0.2f, 0, 1f, 1.25f);
        P("banner_patternA_green", -6.2f, 3.05f, 0, 1f, 1.5f);

        var lamp = new GameObject("StallLight").AddComponent<Light>();
        lamp.transform.SetParent(root.transform, false);
        lamp.transform.position = new Vector3(-6.2f, 3.0f, -1.6f);
        lamp.type = LightType.Point; lamp.color = new Color(1f, 0.72f, 0.42f); lamp.intensity = 2.2f; lamp.range = 7f;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, dir + "ShopDecor.prefab");
        Object.DestroyImmediate(root);
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
        Make("6_Cruz", "Cruz", (barrier, 0f, 2.6f, 0), (barrier, -4.2f, -0.8f, 90), (barrier, 4.2f, -0.8f, 90), (vent, -4.2f, 3.6f, 0), (vent, 4.2f, 3.6f, 0));
        return list.ToArray();
    }

    static void BuildTitle()
    {
        var root = LookDevBuilder.BeginArena("Title");
        var cam = LookDevBuilder.MakeCamera();
        cam.fieldOfView = 38;
        cam.transform.SetPositionAndRotation(new Vector3(0, 5.6f, -11.5f), Quaternion.Euler(18, 0, 0));
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
        var heroMat = LookDevBuilder.Mat("Hero", CombatDevSceneBuilder.KK + "Adventurers/Characters/mage_texture.png", Color.white, Color.black, 0f);
        var hero = LookDevBuilder.Spawn("Adventurers/Characters/Mage.fbx", new Vector3(0, 0, 0.5f), 0, heroMat);
        hero.name = "TitleHero";
        LookDevBuilder.Hide(hero, "Spellbook", "1H_Wand");
        var idle = CombatDevSceneBuilder.Controller(heroFbx, "Assets/Game/Game/Prefabs/TitleHero.controller", ("Idle", "Idle", 0f));
        CombatDevSceneBuilder.Animator(hero, idle);
        var go = new GameObject("TitleController");
        go.transform.SetParent(root, false);
        go.AddComponent<TitleController>();
        go.AddComponent<TitleMenu>();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), TitlePath);
    }
}
