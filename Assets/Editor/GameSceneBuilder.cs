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
        EditorUtility.SetDirty(director);
        var hud = new GameObject("Hud");
        hud.transform.SetParent(root, false);
        hud.AddComponent<Hud>();
    });

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
