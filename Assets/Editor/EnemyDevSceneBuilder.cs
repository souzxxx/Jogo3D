using System.IO;
using Quintessence.Core;
using Quintessence.Enemies;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// Monta a cena de teste da frente de inimigos: sala com NavMesh, heroi jogavel e duas ondas.
public static class EnemyDevSceneBuilder
{
    const string KK = CombatDevSceneBuilder.KK;
    const string Dir = "Assets/Game/Enemies/";
    const string NavPath = "Assets/Scenes/Dev/Enzo_Enemies_NavMesh.asset";
    public const string ScenePath = "Assets/Scenes/Dev/Enzo_Enemies.unity";

    [MenuItem("Quintessence/Dev/Build cena de inimigos")]
    public static void Build() => CombatDevSceneBuilder.BuildPlayable("Enzo_Enemies", ScenePath, root =>
    {
        Bake(root, NavPath);
        Prefabs(out var melee, out var ranged, out var brute, out var bomber);
        var go = new GameObject("WaveSpawner");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(1, 0, 0);
        var spawner = go.AddComponent<WaveSpawner>();
        var w1 = new Wave(); w1.entries.Add(new WaveEntry { prefab = melee, count = 3 });
        var w2 = new Wave(); w2.entries.Add(new WaveEntry { prefab = melee, count = 2 }); w2.entries.Add(new WaveEntry { prefab = ranged, count = 2 });
        var w3 = new Wave(); w3.entries.Add(new WaveEntry { prefab = brute, count = 1 }); w3.entries.Add(new WaveEntry { prefab = bomber, count = 2 }); w3.entries.Add(new WaveEntry { prefab = melee, count = 2 });
        spawner.Waves.Add(w1);
        spawner.Waves.Add(w2);
        spawner.Waves.Add(w3);
        EditorUtility.SetDirty(spawner);
    });

    /// NavMesh da sala, sem o heroi (senao ele vira um buraco no mapa).
    public static void Bake(Transform root, string navPath)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.SetActive(false);
        var surface = root.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        AssetDatabase.DeleteAsset(navPath);
        AssetDatabase.CreateAsset(surface.navMeshData, navPath);
        if (player != null) player.SetActive(true);
    }

    /// Definicoes, animacoes e prefabs dos inimigos. Precisa de uma cena aberta por LookDevBuilder.BeginArena.
    public static void Prefabs(out GameObject melee, out GameObject ranged, out GameObject brute, out GameObject bomber)
    {
        Directory.CreateDirectory(Dir + "Data");
        Directory.CreateDirectory(Dir + "Prefabs");
        Directory.CreateDirectory(Dir + "Animation");
        AssetDatabase.Refresh();

        const string minionFbx = KK + "Skeletons/Characters/Skeleton_Minion.fbx";
        const string mageFbx = KK + "Skeletons/Characters/Skeleton_Mage.fbx";
        CombatDevSceneBuilder.Loop(minionFbx, "Idle", "Idle_Combat", "Running_A");
        CombatDevSceneBuilder.Loop(mageFbx, "Idle", "Idle_Combat", "Running_A");
        var meleeCtrl = CombatDevSceneBuilder.Controller(minionFbx, Dir + "Animation/Rastejante.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A", 0f), ("Attack", "1H_Melee_Attack_Chop", 0.8f),
            ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var rangedCtrl = CombatDevSceneBuilder.Controller(mageFbx, Dir + "Animation/Atirador.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A", 0f), ("Attack", "Spellcast_Shoot|Spellcast_Long", 0.95f),
            ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));

        var skel = LookDevBuilder.Mat("Skeleton", KK + "Skeletons/Characters/skeleton_texture.png", Color.white, Color.black, 0f);
        var shot = ProjectilePrefab();

        var meleeDef = Asset<EnemyDefinition>("Rastejante", d =>
        {
            d.displayName = "Rastejante"; d.maxHealth = 30; d.moveSpeed = 4.2f;
            d.attack = EnemyAttackKind.Melee; d.damage = 8; d.knockback = 4;
            d.attackRange = 2.2f; d.telegraphTime = 0.55f; d.recoverTime = 0.5f; d.cooldown = 1.4f;
            d.strikeOffset = 1.5f; d.strikeRadius = 1.5f;
        });
        var rangedDef = Asset<EnemyDefinition>("Atirador", d =>
        {
            d.displayName = "Atirador"; d.maxHealth = 22; d.moveSpeed = 3.4f;
            d.attack = EnemyAttackKind.Projectile; d.damage = 7; d.knockback = 2;
            d.attackRange = 11f; d.telegraphTime = 0.7f; d.recoverTime = 0.4f; d.cooldown = 2.2f;
            d.projectileSpeed = 11f; d.projectileRange = 16f; d.keepDistance = 6f;
        });
        if (rangedDef.projectilePrefab == null) { rangedDef.projectilePrefab = shot; EditorUtility.SetDirty(rangedDef); }

        melee = EnemyPrefab("Rastejante", "Skeletons/Characters/Skeleton_Minion.fbx", meleeDef, meleeCtrl, skel);
        ranged = EnemyPrefab("Atirador", "Skeletons/Characters/Skeleton_Mage.fbx", rangedDef, rangedCtrl, skel);

        const string warriorFbx = KK + "Skeletons/Characters/Skeleton_Warrior.fbx";
        const string rogueFbx = KK + "Skeletons/Characters/Skeleton_Rogue.fbx";
        CombatDevSceneBuilder.Loop(warriorFbx, "Idle", "Idle_Combat", "Walking_A", "Running_A");
        CombatDevSceneBuilder.Loop(rogueFbx, "Idle", "Idle_Combat", "Running_A", "Running_B");
        var bruteCtrl = CombatDevSceneBuilder.Controller(warriorFbx, Dir + "Animation/Bruto.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Walking_A|Running_A", 0f), ("Attack", "2H_Melee_Attack_Chop|1H_Melee_Attack_Chop", 1.25f),
            ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var bomberCtrl = CombatDevSceneBuilder.Controller(rogueFbx, Dir + "Animation/Explosivo.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_B|Running_A", 0f), ("Attack", "Cheer|Spellcast_Raise|Idle", 0.75f),
            ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var bruteDef = Asset<EnemyDefinition>("Bruto", d =>
        {
            d.displayName = "Bruto"; d.maxHealth = 70; d.moveSpeed = 2.7f;
            d.armored = true; d.armoredDamageTaken = 0.4f;
            d.attack = EnemyAttackKind.Melee; d.damage = 16; d.knockback = 9;
            d.attackRange = 3f; d.telegraphTime = 0.95f; d.recoverTime = 0.8f; d.cooldown = 2.2f;
            d.strikeOffset = 2f; d.strikeRadius = 2.4f;
        });
        var bomberDef = Asset<EnemyDefinition>("Explosivo", d =>
        {
            d.displayName = "Explosivo"; d.maxHealth = 16; d.moveSpeed = 5.6f;
            d.attack = EnemyAttackKind.Explode; d.damage = 14; d.knockback = 8; d.explosionDamageToEnemies = 12;
            d.attackRange = 1.9f; d.telegraphTime = 0.75f; d.recoverTime = 0.2f; d.cooldown = 1f;
            d.strikeOffset = 0f; d.strikeRadius = 2.7f; d.element = ElementType.Fire;
        });
        var hot = LookDevBuilder.Mat("SkeletonExplosive", KK + "Skeletons/Characters/skeleton_texture.png", new Color(1.5f, 0.75f, 0.4f), Color.black, 0f);
        brute = EnemyPrefab("Bruto", "Skeletons/Characters/Skeleton_Warrior.fbx", bruteDef, bruteCtrl, skel, 1.3f);
        bomber = EnemyPrefab("Explosivo", "Skeletons/Characters/Skeleton_Rogue.fbx", bomberDef, bomberCtrl, hot, 0.9f);
    }

    static T Asset<T>(string name, System.Action<T> init) where T : ScriptableObject
    {
        string path = Dir + "Data/" + name + ".asset";
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a; // os numeros sao de quem balanceia, nao sobrescreve
        a = ScriptableObject.CreateInstance<T>();
        init(a);
        AssetDatabase.CreateAsset(a, path);
        return a;
    }

    static GameObject ProjectilePrefab()
    {
        string path = Dir + "Prefabs/Projetil.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        var c = new Color(0.75f, 0.3f, 1f);
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = "Projetil";
        go.transform.localScale = Vector3.one * 0.5f;
        go.GetComponent<Renderer>().sharedMaterial = LookDevBuilder.Mat("Glow_Projectile", null, c * 0.6f, c * 2.2f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point; light.color = c; light.intensity = 3f; light.range = 4f;
        go.AddComponent<Projectile>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject EnemyPrefab(string name, string fbx, EnemyDefinition def, RuntimeAnimatorController ctrl, Material mat, float scale = 1f)
    {
        string path = Dir + "Prefabs/" + name + ".prefab";
        var go = new GameObject(name);
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 2.2f; col.radius = 0.55f; col.center = new Vector3(0, 1.1f, 0);
        go.AddComponent<Health>().Configure(def.maxHealth, Team.Enemy);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f; agent.height = 2.2f; agent.angularSpeed = 0f;
        var model = LookDevBuilder.Spawn(fbx, Vector3.zero, 0, mat);
        model.name = "Model";
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * scale;
        col.height *= scale; col.radius *= scale; col.center *= scale;
        var anim = CombatDevSceneBuilder.Animator(model, ctrl);
        go.AddComponent<EnemyBrain>().Bind(def, anim);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
