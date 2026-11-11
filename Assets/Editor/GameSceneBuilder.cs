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
        director.BindBoons(catalog);
        director.BindBoss(EnemyDevSceneBuilder.BossPrefab(melee, bomber));
        // ordem do enum RoomType: Combat, Elite, Shrine, Rest, Boss, Master
        string[] banners = { "banner_red", "banner_patternB_brown", "banner_blue", "banner_green", "banner_shield_red", "banner_patternA_yellow" };
        var bannerPrefabs = new GameObject[banners.Length];
        for (int i = 0; i < banners.Length; i++) bannerPrefabs[i] = Prop(banners[i], 1f);
        var stone = LookDevBuilder.Mat("Stone", null, new Color(0.33f, 0.34f, 0.42f), Color.black, 0f);
        director.BindLayouts(Layouts());
        director.BindProps(Prop("wall_doorway", 1f), bannerPrefabs, Prop("column", 1f), Prop("chest_gold", 1f), stone);
        EditorUtility.SetDirty(director);
        var hud = new GameObject("Hud");
        hud.transform.SetParent(root, false);
        hud.AddComponent<Hud>();
    });

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
        cam.transform.SetPositionAndRotation(new Vector3(0, 6.5f, -12.5f), Quaternion.Euler(24, 0, 0));
        Color[] colors = { new(1f, 0.38f, 0.08f), new(0.15f, 0.6f, 1f), new(0.72f, 0.5f, 0.2f), new(0.65f, 1f, 0.85f) };
        for (int i = 0; i < 4; i++)
        {
            var l = new GameObject("ElementLight" + i).AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.position = new Vector3((i - 1.5f) * 4.5f, 2f, 3f);
            l.type = LightType.Point; l.color = colors[i]; l.intensity = 5f; l.range = 8f;
        }
        var go = new GameObject("TitleController");
        go.transform.SetParent(root, false);
        go.AddComponent<TitleController>();
        go.AddComponent<TitleMenu>();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), TitlePath);
    }
}
