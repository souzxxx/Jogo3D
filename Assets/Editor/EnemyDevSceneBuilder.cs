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
        ExtraPrefabs(out _, out _, out _);
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

    public const string BossScenePath = "Assets/Scenes/Dev/Enzo_Boss.unity";

    [MenuItem("Quintessence/Dev/Build cena do chefe")]
    public static void BuildBossScene() => CombatDevSceneBuilder.BuildPlayable("Enzo_Boss", BossScenePath, root =>
    {
        Bake(root, "Assets/Scenes/Dev/Enzo_Boss_NavMesh.asset");
        Prefabs(out var melee, out _, out _, out var bomber);
        var boss = BossPrefab(melee, bomber);
        var go = new GameObject("WaveSpawner");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(0, 0, 2);
        var spawner = go.AddComponent<WaveSpawner>();
        var w = new Wave(); w.entries.Add(new WaveEntry { prefab = boss, count = 1 });
        spawner.Waves.Add(w);
        EditorUtility.SetDirty(spawner);
    });

    /// Guardiao das Cinzas. Precisa de uma cena aberta por LookDevBuilder.BeginArena.
    public static GameObject BossPrefab(GameObject add1, GameObject add2)
    {
        const string fbx = KK + "Skeletons/Characters/Skeleton_Warrior.fbx";
        CombatDevSceneBuilder.Loop(fbx, "Idle", "Idle_Combat", "Walking_A", "Running_A");
        var ctrl = CombatDevSceneBuilder.Controller(fbx, Dir + "Animation/Guardiao.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Walking_A|Running_A", 0f), ("Attack", "1H_Melee_Attack_Jump_Chop|2H_Melee_Attack_Chop|1H_Melee_Attack_Chop", 1.3f),
            ("Cast", "Throw|Spellcast_Shoot|Cheer|Idle", 1.1f), ("Summon", "Spellcast_Summon|Spellcast_Raise|Cheer|Idle", 1.5f),
            ("Roar", "Taunt|Cheer|Idle", 0.9f), ("Hit", "Hit_B|Hit_A", 0.35f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 1.2f));
        var def = Asset<BossDefinition>("Guardiao", d => { });
        def.projectilePrefab = ProjectilePrefab();
        def.addPrefabs = new[] { add1, add2 };
        EditorUtility.SetDirty(def);

        AssetDatabase.Refresh();
        HeroBuilder.MatteParts("Assets/Art/Boss");
        var ember = LookDevBuilder.Mat("GuardianBody", "Assets/Art/Boss/boss_body_texture.png", Color.white, Color.black, 0f);
        var go = new GameObject("Guardiao");
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 5f; col.radius = 1.3f; col.center = new Vector3(0, 2.5f, 0);
        go.AddComponent<Health>().Configure(def.maxHealth, Team.Enemy);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 1.3f; agent.height = 5f; agent.angularSpeed = 0f;
        var model = LookDevBuilder.Spawn("Skeletons/Characters/Skeleton_Warrior.fbx", Vector3.zero, 0, ember);
        model.name = "Model";
        // as pecas proprias entram com o modelo na origem e sem escala (foram modeladas no espaco do corpo); depois ele e ampliado
        model.transform.SetParent(null, false);
        model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        model.transform.localScale = Vector3.one;
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            if (r.name == "Skeleton_Warrior_Helmet" || r.name == "Skeleton_Warrior_Cloak") r.gameObject.SetActive(false);
        foreach (var (file, bone) in new[] { ("boss_head", "head"), ("boss_shoulder_l", "upperarm.l"), ("boss_shoulder_r", "upperarm.r"), ("boss_back", "chest"), ("boss_core", "chest"), ("boss_weapon", "handslot.r") })
            HeroBuilder.Attach(model, "Assets/Art/Boss/" + file + ".fbx", bone);
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * 2.3f;
        var anim = CombatDevSceneBuilder.Animator(model, ctrl);
        go.AddComponent<BossBrain>().Bind(def, anim);
        var light = new GameObject("EmberLight").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0, 3.2f, 0.8f);
        light.type = LightType.Point; light.color = new Color(1f, 0.5f, 0.25f); light.intensity = 2.6f; light.range = 7f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Dir + "Prefabs/Guardiao.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// Tecela das Cinzas: segundo chefe final. A esqueleto conjuradora do pack ampliada, com pecas proprias
    /// (Assets/Art/Boss2) quando existirem; sem elas, cajado e cristais do pack.
    public static GameObject WeaverPrefab(GameObject add1, GameObject add2)
    {
        const string fbx = KK + "Skeletons/Characters/Skeleton_Mage.fbx";
        CombatDevSceneBuilder.Loop(fbx, "Idle", "Idle_Combat", "Running_A", "Walking_A");
        var ctrl = CombatDevSceneBuilder.Controller(fbx, Dir + "Animation/Tecela.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Walking_A|Running_A", 0f), ("Attack", "Spellcast_Shoot|Spellcast_Long", 1f),
            ("Cast", "Spellcast_Shoot|Spellcast_Long", 0.9f), ("Summon", "Spellcast_Summon|Spellcast_Raise", 1.3f),
            ("Roar", "Spellcast_Raise|Taunt|Cheer", 0.9f), ("Hit", "Hit_B|Hit_A", 0.3f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 1.2f));
        var def = Asset<BossDefinition>("Tecela", d =>
        {
            d.displayName = "Tecelã das Cinzas"; d.pattern = 1; d.maxHealth = 520f; d.moveSpeed = 2.6f;
            d.cooldownByPhase = new[] { 1.7f, 1.4f, 1.05f };
            d.fanDamage = 7f; d.fanCount = 4; d.fanCountLastPhase = 6; d.fanSpread = 18f;
            d.eruptionDamage = 9f; d.eruptionCount = 4;
            d.ringDamage = 11f; d.ringCount = 8;
            d.blinkWindup = 0.7f; d.blinkDistance = 7.5f; d.blinkDamage = 10f; d.blinkRadius = 2.2f;
            d.maxAdds = 3; d.addsPerPhaseChange = 2; d.addInterval = 9f;
        });
        def.projectilePrefab = ProjectilePrefab();
        def.addPrefabs = new[] { add1, add2 };
        EditorUtility.SetDirty(def);

        AssetDatabase.Refresh();
        bool art = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Boss2/weaver_head.fbx") != null;
        if (art) HeroBuilder.MatteParts("Assets/Art/Boss2");
        var skin = art ? LookDevBuilder.Mat("WeaverBody", "Assets/Art/Boss2/weaver_body_texture.png", Color.white, Color.black, 0f)
                       : LookDevBuilder.Mat("WeaverAsh", KK + "Skeletons/Characters/skeleton_texture.png", new Color(0.6f, 0.48f, 0.62f), Color.black, 0f);
        var go = new GameObject("Tecela");
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 4.8f; col.radius = 1.1f; col.center = new Vector3(0, 2.4f, 0);
        go.AddComponent<Health>().Configure(def.maxHealth, Team.Enemy);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 1.1f; agent.height = 4.8f; agent.angularSpeed = 0f;
        var model = LookDevBuilder.Spawn("Skeletons/Characters/Skeleton_Mage.fbx", Vector3.zero, 0, skin);
        model.name = "Model";
        model.transform.SetParent(null, false);
        model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        model.transform.localScale = Vector3.one;
        var hidden = new System.Collections.Generic.List<string>();
        if (art)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("Hat") || r.name.Contains("Staff")) { r.gameObject.SetActive(false); hidden.Add(r.name); }
            foreach (var (file, bone) in new[] { ("weaver_head", "head"), ("weaver_mantle", "chest"), ("weaver_shoulder_l", "upperarm.l"), ("weaver_shoulder_r", "upperarm.r"),
                ("weaver_core", "chest"), ("weaver_staff", "handslot.r"), ("weaver_shards", "chest") })
                HeroBuilder.Attach(model, "Assets/Art/Boss2/" + file + ".fbx", bone);
        }
        Debug.Log("Tecela: arte propria " + art + ", escondidas: " + string.Join(", ", hidden));
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * 2.2f;
        if (!art)
        {
            // sem a arte propria: cajado do pack e cristais ambar em volta dos ombros
            Transform hand = null, chest = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) { if (t.name == "handslot.r") hand = t; if (t.name == "chest") chest = t; }
            if (hand != null && hand.childCount == 0)
            {
                var gear = LookDevBuilder.Mat("SkeletonGear", KK + "Skeletons/Props/skeleton_texture.png", new Color(0.7f, 0.66f, 0.68f), Color.black, 0f);
                var staff = LookDevBuilder.Spawn("Skeletons/Props/Skeleton_Staff.fbx", Vector3.zero, 0, gear);
                staff.transform.SetParent(hand, false);
                staff.transform.localPosition = Vector3.zero; staff.transform.localRotation = Quaternion.identity;
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(KK + "Skeletons/Props/Skeleton_Staff.fbx");
                staff.transform.localScale = Vector3.one * ((src != null ? src.transform.localScale.x : 1f) * model.transform.lossyScale.x / Mathf.Max(0.0001f, hand.lossyScale.x));
            }
            if (chest != null)
            {
                var amber = LookDevBuilder.Mat("Glow_Amber", null, new Color(0.85f, 0.5f, 0.16f) * 0.6f, new Color(0.85f, 0.5f, 0.16f) * 0.55f, 0f);
                foreach (var (pos, euler, h) in new[] { (new Vector3(-0.32f, 0.2f, -0.1f), new Vector3(0, 0, 30), 0.42f), (new Vector3(0.32f, 0.2f, -0.1f), new Vector3(0, 0, -30), 0.42f), (new Vector3(0f, 0.3f, -0.25f), new Vector3(-25, 0, 0), 0.55f) })
                {
                    var c = new GameObject("AmberCrystal");
                    c.transform.SetParent(chest, false);
                    c.transform.localPosition = pos; c.transform.localRotation = Quaternion.Euler(euler); c.transform.localScale = new Vector3(0.14f, h, 0.14f);
                    c.AddComponent<MeshFilter>().sharedMesh = LookDevBuilder.CrystalMesh();
                    c.AddComponent<MeshRenderer>().sharedMaterial = amber;
                }
            }
        }
        var anim = CombatDevSceneBuilder.Animator(model, ctrl);
        go.AddComponent<BossBrain>().Bind(def, anim);
        var light = new GameObject("EmberLight").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0, 3f, 0.8f);
        light.type = LightType.Point; light.color = new Color(1f, 0.55f, 0.3f); light.intensity = 2.2f; light.range = 7f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Dir + "Prefabs/Tecela.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// Rainha Afogada: terceiro chefe final. O esqueleto mago do pack, ampliado, com a textura recolorida do Afogado e o cajado;
    /// golpes de Agua, Erupcoes que deixam pocas, teleporte da segunda fase. Afogados e Rastejantes como reforco.
    public static GameObject QueenPrefab(GameObject add1, GameObject add2)
    {
        const string fbx = KK + "Skeletons/Characters/Skeleton_Mage.fbx";
        var ctrl = CombatDevSceneBuilder.Controller(fbx, Dir + "Animation/Rainha.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Walking_A|Running_A", 0f), ("Attack", "Spellcast_Long|Spellcast_Shoot", 1f),
            ("Cast", "Spellcast_Shoot|Spellcast_Long", 0.9f), ("Summon", "Spellcast_Summon|Spellcast_Raise", 1.3f),
            ("Roar", "Spellcast_Raise|Taunt|Cheer", 0.9f), ("Hit", "Hit_B|Hit_A", 0.3f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 1.2f));
        var def = Asset<BossDefinition>("Rainha", d =>
        {
            d.displayName = "Rainha Afogada"; d.pattern = 2; d.element = ElementType.Water; d.maxHealth = 560f; d.moveSpeed = 2.2f;
            d.cooldownByPhase = new[] { 1.8f, 1.45f, 1.1f };
            d.slamDamage = 16f; d.slamRadius = 3f;
            d.fanDamage = 8f; d.fanCount = 3; d.fanCountLastPhase = 5; d.fanSpread = 20f;
            d.eruptionDamage = 10f; d.eruptionCount = 3; d.eruptionRadius = 2.2f; d.eruptionPuddleSeconds = 6f;
            d.ringDamage = 10f; d.ringCount = 8;
            d.blinkWindup = 0.8f; d.blinkDistance = 6f; d.blinkDamage = 10f; d.blinkRadius = 2.2f;
            d.maxAdds = 3; d.addsPerPhaseChange = 2; d.addInterval = 10f;
        });
        return DrownedBoss("Rainha", def, add1, add2, 2.3f, ctrl);
    }

    /// Sacerdotisa Afogada: mini-chefe das Elites da Cripta. A Rainha em menor, sem o escudo da ultima fase.
    public static GameObject PriestessPrefab(GameObject add1, GameObject add2)
    {
        const string fbx = KK + "Skeletons/Characters/Skeleton_Mage.fbx";
        var ctrl = CombatDevSceneBuilder.Controller(fbx, Dir + "Animation/Sacerdotisa.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Walking_A|Running_A", 0f), ("Attack", "Spellcast_Long|Spellcast_Shoot", 1f),
            ("Cast", "Spellcast_Shoot|Spellcast_Long", 0.9f), ("Summon", "Spellcast_Summon|Spellcast_Raise", 1.3f),
            ("Roar", "Spellcast_Raise|Taunt|Cheer", 0.9f), ("Hit", "Hit_B|Hit_A", 0.3f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 1f));
        var def = Asset<BossDefinition>("Sacerdotisa", d =>
        {
            d.displayName = "Sacerdotisa Afogada"; d.pattern = 2; d.element = ElementType.Water; d.maxHealth = 210f; d.moveSpeed = 2.6f;
            d.cooldownByPhase = new[] { 1.6f, 1.3f, 1.05f };
            d.slamDamage = 12f; d.slamRadius = 2.6f; d.slamRange = 4.2f;
            d.fanDamage = 7f; d.fanCount = 3; d.fanCountLastPhase = 4; d.fanSpread = 20f;
            d.eruptionDamage = 9f; d.eruptionCount = 3; d.eruptionRadius = 2f; d.eruptionPuddleSeconds = 5f;
            d.ringDamage = 9f; d.ringCount = 6; d.ringRadius = 4.6f; d.ringCircleRadius = 1.8f;
            d.blinkWindup = 0.8f; d.blinkDistance = 5.5f; d.blinkDamage = 9f; d.blinkRadius = 2f;
            d.lastPhaseShield = false; d.maxAdds = 1; d.addsPerPhaseChange = 1; d.addInterval = 12f;
        });
        return DrownedBoss("Sacerdotisa", def, add1, add2, 1.55f, ctrl);
    }

    /// Corpo dos chefes de agua: o mago esqueleto do pack com a textura do Afogado, cajado, afinidade de Agua e luz fria.
    static GameObject DrownedBoss(string file, BossDefinition def, GameObject add1, GameObject add2, float scale, RuntimeAnimatorController ctrl)
    {
        def.projectilePrefab = ProjectilePrefab();
        def.addPrefabs = new[] { add1, add2 };
        EditorUtility.SetDirty(def);

        string tex = Recolor(KK + "Skeletons/Characters/skeleton_texture.png", "Assets/Art/Enemies/skeleton_texture_drowned.png", 0.56f, 0.22f);
        var skin = LookDevBuilder.Mat("QueenDrowned", tex, new Color(0.8f, 0.92f, 0.95f), Color.black, 0f);
        var go = new GameObject(file);
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 5f * scale / 2.3f; col.radius = 1.15f * scale / 2.3f; col.center = new Vector3(0, 2.5f * scale / 2.3f, 0);
        go.AddComponent<Health>().Configure(def.maxHealth, Team.Enemy);
        go.AddComponent<ElementalAffinity>().Set(ElementType.Water);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 1.15f * scale / 2.3f; agent.height = 5f * scale / 2.3f; agent.angularSpeed = 0f;
        var model = LookDevBuilder.Spawn("Skeletons/Characters/Skeleton_Mage.fbx", Vector3.zero, 0, skin);
        model.name = "Model";
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * scale;
        Transform hand = null;
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "handslot.r") hand = t;
        if (hand != null)
        {
            var gear = LookDevBuilder.Mat("SkeletonGear", KK + "Skeletons/Props/skeleton_texture.png", new Color(0.7f, 0.66f, 0.68f), Color.black, 0f);
            var staff = LookDevBuilder.Spawn("Skeletons/Props/Skeleton_Staff.fbx", Vector3.zero, 0, gear);
            staff.name = "Skeleton_Staff";
            staff.transform.SetParent(hand, false);
            staff.transform.localPosition = Vector3.zero; staff.transform.localRotation = Quaternion.identity;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(KK + "Skeletons/Props/Skeleton_Staff.fbx");
            staff.transform.localScale = Vector3.one * ((src != null ? src.transform.localScale.x : 1f) * model.transform.lossyScale.x / Mathf.Max(0.0001f, hand.lossyScale.x));
        }
        var anim = CombatDevSceneBuilder.Animator(model, ctrl);
        go.AddComponent<BossBrain>().Bind(def, anim);
        // luz fria e baixa: agua parada, sem neon
        var light = new GameObject("TideLight").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0, 3f, 0.8f);
        light.type = LightType.Point; light.color = new Color(0.45f, 0.75f, 0.85f); light.intensity = 1.6f; light.range = 7f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Dir + "Prefabs/" + file + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// Carrasco: mini-chefe do meio da run. O esqueleto guerreiro do pack com o capuz e a foice do Ceifador,
    /// ampliado; usa o cerebro do chefe com numeros menores e sem o escudo da ultima fase.
    public static GameObject MiniBossPrefab(GameObject add1, GameObject add2)
    {
        const string fbx = KK + "Skeletons/Characters/Skeleton_Warrior.fbx";
        CombatDevSceneBuilder.Loop(fbx, "Idle", "Idle_Combat", "Walking_A", "Running_A");
        var ctrl = CombatDevSceneBuilder.Controller(fbx, Dir + "Animation/Carrasco.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A|Walking_A", 0f), ("Attack", "2H_Melee_Attack_Chop|1H_Melee_Attack_Jump_Chop", 1.1f),
            ("Cast", "Throw|Spellcast_Shoot|Idle", 1f), ("Summon", "Spellcast_Raise|Cheer|Idle", 1.3f),
            ("Roar", "Taunt|Cheer|Idle", 0.8f), ("Hit", "Hit_B|Hit_A", 0.3f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 1f));
        var def = Asset<BossDefinition>("Carrasco", d =>
        {
            d.displayName = "Carrasco"; d.maxHealth = 200f; d.moveSpeed = 3.3f;
            d.cooldownByPhase = new[] { 1.6f, 1.3f, 1.05f };
            d.slamDamage = 13f; d.slamRadius = 2.6f; d.slamOffset = 2.1f; d.slamWindup = 0.8f; d.slamRange = 4.2f;
            d.fanDamage = 7f; d.fanCount = 3; d.fanCountLastPhase = 4;
            d.eruptionDamage = 9f; d.eruptionCount = 3; d.eruptionRadius = 1.8f;
            d.chargeDamage = 12f; d.chargeWidth = 1.3f; d.chargeLength = 11f; d.chargeWindup = 0.95f; d.chargeSpeed = 24f;
            d.ringDamage = 10f; d.ringCount = 6; d.ringRadius = 4.6f; d.ringCircleRadius = 1.8f;
            d.lastPhaseShield = false; d.maxAdds = 1; d.addsPerPhaseChange = 1; d.addInterval = 12f;
        });
        def.projectilePrefab = ProjectilePrefab();
        def.addPrefabs = new[] { add1, add2 };
        EditorUtility.SetDirty(def);

        AssetDatabase.Refresh();
        HeroBuilder.MatteParts("Assets/Art/Enemies");
        var skin = LookDevBuilder.Mat("SkeletonExecutioner", KK + "Skeletons/Characters/skeleton_texture.png", new Color(0.74f, 0.6f, 0.58f), Color.black, 0f);
        var go = new GameObject("Carrasco");
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 3.8f; col.radius = 0.95f; col.center = new Vector3(0, 1.9f, 0);
        go.AddComponent<Health>().Configure(def.maxHealth, Team.Enemy);
        var agent = go.AddComponent<NavMeshAgent>();
        agent.radius = 0.95f; agent.height = 3.8f; agent.angularSpeed = 0f;
        var model = LookDevBuilder.Spawn("Skeletons/Characters/Skeleton_Warrior.fbx", Vector3.zero, 0, skin);
        model.name = "Model";
        model.transform.SetParent(null, false);
        model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        model.transform.localScale = Vector3.one;
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            if (r.name == "Skeleton_Warrior_Helmet" || r.name == "Skeleton_Warrior_Cloak") r.gameObject.SetActive(false);
        foreach (var (file, bone) in new[] { ("ceifador_head", "head"), ("ceifador_weapon", "handslot.r"), ("ceifador_back", "chest") })
            HeroBuilder.Attach(model, "Assets/Art/Enemies/" + file + ".fbx", bone);
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * 1.7f;
        var anim = CombatDevSceneBuilder.Animator(model, ctrl);
        go.AddComponent<BossBrain>().Bind(def, anim);
        var light = new GameObject("EmberLight").AddComponent<Light>();
        light.transform.SetParent(go.transform, false);
        light.transform.localPosition = new Vector3(0, 2.6f, 0.6f);
        light.type = LightType.Point; light.color = new Color(0.7f, 0.75f, 1f); light.intensity = 1.6f; light.range = 5f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, Dir + "Prefabs/Carrasco.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// O que faz do esqueleto do pack o Guardiao das Cinzas: machado e escudo do pack nas maos e os cristais ambar
    /// da corrupcao crescendo nas costas e nos ombros.
    static void DressGuardian(Transform model)
    {
        Transform Bone(string name)
        {
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            Debug.LogWarning("Guardiao: osso nao encontrado: " + name);
            return model;
        }
        var gear = LookDevBuilder.Mat("SkeletonGear", KK + "Skeletons/Props/skeleton_texture.png", new Color(0.7f, 0.66f, 0.68f), Color.black, 0f);
        void Hold(string prop, string bone, Vector3 euler, float scale)
        {
            var go = LookDevBuilder.Spawn("Skeletons/Props/" + prop + ".fbx", Vector3.zero, 0, gear);
            go.name = prop;
            go.transform.SetParent(Bone(bone), false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(euler);
            // os ossos da mao vem do FBX com escala propria: acerta para a peca ficar do tamanho do modelo
            float boneScale = Mathf.Max(0.0001f, go.transform.parent.lossyScale.x);
            // e a peca do pack tambem tem escala de importacao, que o Spawn zera
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(KK + "Skeletons/Props/" + prop + ".fbx");
            float native = source != null ? source.transform.localScale.x : 1f;
            go.transform.localScale = Vector3.one * (scale * native * model.lossyScale.x / boneScale);
            Debug.Log($"Guardiao: {prop} escala nativa {native}, osso {boneScale}, modelo {model.lossyScale.x}");
        }
        Hold("Skeleton_Axe", "handslot.r", Vector3.zero, 1.05f);
        Hold("Skeleton_Shield_Large_A", "handslot.l", new Vector3(0, 180, 0), 1f);

        var amber = LookDevBuilder.Mat("Glow_Amber", null, new Color(0.85f, 0.5f, 0.16f) * 0.6f, new Color(0.85f, 0.5f, 0.16f) * 0.55f, 0f);
        void Crystal(string bone, Vector3 pos, Vector3 euler, float width, float height)
        {
            var go = new GameObject("AmberCrystal");
            go.transform.SetParent(Bone(bone), false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = new Vector3(width, height, width);
            go.AddComponent<MeshFilter>().sharedMesh = LookDevBuilder.CrystalMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = amber;
        }
        // costas: um leque de cristais saindo por tras
        Crystal("chest", new Vector3(0f, 0.05f, -0.22f), new Vector3(-35, 0, 0), 0.2f, 0.62f);
        Crystal("chest", new Vector3(-0.2f, 0.02f, -0.2f), new Vector3(-30, 0, 28), 0.16f, 0.48f);
        Crystal("chest", new Vector3(0.2f, 0.02f, -0.2f), new Vector3(-30, 0, -28), 0.16f, 0.48f);
        // ombros
        Crystal("chest", new Vector3(-0.36f, 0.16f, 0f), new Vector3(0, 0, 38), 0.15f, 0.4f);
        Crystal("chest", new Vector3(0.36f, 0.16f, 0f), new Vector3(0, 0, -38), 0.15f, 0.4f);
        // mais dois, grandes, abrindo o leque
        Crystal("chest", new Vector3(-0.3f, -0.08f, -0.16f), new Vector3(-20, 0, 50), 0.14f, 0.4f);
        Crystal("chest", new Vector3(0.3f, -0.08f, -0.16f), new Vector3(-20, 0, -50), 0.14f, 0.4f);
    }

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
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var rangedCtrl = CombatDevSceneBuilder.Controller(mageFbx, Dir + "Animation/Atirador.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A", 0f), ("Attack", "Spellcast_Shoot|Spellcast_Long", 0.95f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));

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
            ("Hit", "Hit_B|Hit_A", 0.35f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var bomberCtrl = CombatDevSceneBuilder.Controller(rogueFbx, Dir + "Animation/Explosivo.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_B|Running_A", 0f), ("Attack", "Taunt|Cheer|Spellcast_Raise|Idle", 0.75f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var bruteDef = Asset<EnemyDefinition>("Bruto", d =>
        {
            d.displayName = "Bruto"; d.maxHealth = 70; d.moveSpeed = 2.7f;
            d.armored = true; d.armoredDamageTaken = 0.5f;
            d.attack = EnemyAttackKind.Melee; d.damage = 13; d.knockback = 9;
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
        var hot = LookDevBuilder.Mat("SkeletonExplosive2", KK + "Skeletons/Characters/skeleton_texture.png", new Color(1.1f, 0.72f, 0.5f), Color.black, 0f);
        brute = EnemyPrefab("Bruto", "Skeletons/Characters/Skeleton_Warrior.fbx", bruteDef, bruteCtrl, skel, 1.3f);
        bomber = EnemyPrefab("Explosivo", "Skeletons/Characters/Skeleton_Rogue.fbx", bomberDef, bomberCtrl, hot, 0.9f);
    }

    /// Ceifador (investida), Arremessador (bomba em area) e Xama (cura). Chamar depois de Prefabs.
    public static void ExtraPrefabs(out GameObject charger, out GameObject lobber, out GameObject shaman)
    {
        const string rogueFbx = KK + "Skeletons/Characters/Skeleton_Rogue.fbx";
        const string mageFbx = KK + "Skeletons/Characters/Skeleton_Mage.fbx";
        const string minionFbx = KK + "Skeletons/Characters/Skeleton_Minion.fbx";
        var chargerCtrl = CombatDevSceneBuilder.Controller(rogueFbx, Dir + "Animation/Ceifador.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A|Running_B", 0f), ("Attack", "1H_Melee_Attack_Stab|Dodge_Forward|1H_Melee_Attack_Chop", 0.9f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var lobberCtrl = CombatDevSceneBuilder.Controller(minionFbx, Dir + "Animation/Arremessador.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A", 0f), ("Attack", "Throw|Spellcast_Long|1H_Melee_Attack_Chop", 1f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var shamanCtrl = CombatDevSceneBuilder.Controller(mageFbx, Dir + "Animation/Xama.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A", 0f), ("Attack", "Spellcast_Raise|Spellcast_Summon|Spellcast_Long", 1.1f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));

        var chargerDef = Asset<EnemyDefinition>("Ceifador", d =>
        {
            d.displayName = "Ceifador"; d.maxHealth = 38; d.moveSpeed = 4.6f;
            d.attack = EnemyAttackKind.Charge; d.damage = 13; d.knockback = 9;
            d.attackRange = 7f; d.telegraphTime = 0.8f; d.recoverTime = 1.15f; d.cooldown = 3f;
            d.chargeDistance = 8.5f; d.chargeSpeed = 22f; d.chargeWidth = 0.9f;
        });
        var lobberDef = Asset<EnemyDefinition>("Arremessador", d =>
        {
            d.displayName = "Arremessador"; d.maxHealth = 26; d.moveSpeed = 3.2f;
            d.attack = EnemyAttackKind.Lob; d.damage = 10; d.knockback = 5;
            d.attackRange = 10f; d.telegraphTime = 1.05f; d.recoverTime = 0.5f; d.cooldown = 2.8f;
            d.strikeRadius = 2.3f; d.keepDistance = 5f;
        });
        var shamanDef = Asset<EnemyDefinition>("Xama", d =>
        {
            d.displayName = "Xamã"; d.maxHealth = 34; d.moveSpeed = 3.2f;
            d.attack = EnemyAttackKind.Heal; d.damage = 6; d.knockback = 2;
            d.attackRange = 10f; d.telegraphTime = 1.1f; d.recoverTime = 0.5f; d.cooldown = 3.6f;
            d.healAmount = 14f; d.healRadius = 6f; d.keepDistance = 6.5f;
            d.projectileSpeed = 10f; d.projectileRange = 14f;
        });
        if (shamanDef.projectilePrefab == null) { shamanDef.projectilePrefab = ProjectilePrefab(); EditorUtility.SetDirty(shamanDef); }

        // cada tipo tem um tom proprio do osso e uma peca do pack na mao: da para reconhecer de cima
        const string tex = KK + "Skeletons/Characters/skeleton_texture.png";
        var steel = LookDevBuilder.Mat("SkeletonCharger", tex, new Color(0.62f, 0.68f, 0.82f), Color.black, 0f);
        var moss = LookDevBuilder.Mat("SkeletonLobber", tex, new Color(0.78f, 0.8f, 0.56f), Color.black, 0f);
        var jade = LookDevBuilder.Mat("SkeletonShaman", tex, new Color(0.56f, 0.82f, 0.7f), Color.black, 0f);
        AssetDatabase.Refresh();
        HeroBuilder.MatteParts("Assets/Art/Enemies");
        HeroBuilder.MatteParts("Assets/Resources/Models");
        dress = (new[] { ("ceifador_head", "head"), ("ceifador_weapon", "handslot.r"), ("ceifador_back", "chest") }, new[] { "Skeleton_Rogue_Hood", "Skeleton_Rogue_Cape" });
        charger = EnemyPrefab("Ceifador", "Skeletons/Characters/Skeleton_Rogue.fbx", chargerDef, chargerCtrl, steel, 1.12f);
        dress = (new[] { ("arremessador_head", "head"), ("arremessador_back", "chest"), ("arremessador_weapon", "handslot.r") }, new string[0]);
        lobber = EnemyPrefab("Arremessador", "Skeletons/Characters/Skeleton_Minion.fbx", lobberDef, lobberCtrl, moss, 1.05f);
        dress = (new[] { ("xama_head", "head"), ("xama_weapon", "handslot.r"), ("xama_back", "chest") }, new[] { "Skeleton_Mage_Hat" });
        shaman = EnemyPrefab("Xama", "Skeletons/Characters/Skeleton_Mage.fbx", shamanDef, shamanCtrl, jade, 1.05f);
        dress = default;
    }

    /// Cinzento (enxame), Brasa Viva (elemental de Fogo) e Sentinela de Pedra (torre de Terra). Chamar depois de Prefabs.
    /// Pecas que o proximo EnemyPrefab segura alem da principal: (peca do pack, osso, rotacao).
    static (string prop, string bone, Vector3 euler)[] holds;

    /// Escudeiro: esqueleto guerreiro com escudo grande e lamina, tudo do pack KayKit. Escudo a frente, gira devagar.
    public static GameObject GuardPrefab()
    {
        const string warriorFbx = KK + "Skeletons/Characters/Skeleton_Warrior.fbx";
        CombatDevSceneBuilder.Loop(warriorFbx, "Blocking", "Walking_B");
        var ctrl = CombatDevSceneBuilder.Controller(warriorFbx, Dir + "Animation/Escudeiro.controller",
            ("Idle", "Blocking|Idle_Combat", 0f), ("Run", "Walking_B|Walking_A", 0f), ("Attack", "Block_Attack|1H_Melee_Attack_Stab", 0.9f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A|Death_B", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var def = Asset<EnemyDefinition>("Escudeiro", d =>
        {
            d.displayName = "Escudeiro"; d.maxHealth = 50; d.moveSpeed = 3.1f;
            d.attack = EnemyAttackKind.Melee; d.damage = 10; d.knockback = 7;
            d.attackRange = 2.3f; d.telegraphTime = 0.7f; d.recoverTime = 0.6f; d.cooldown = 1.8f;
            d.strikeOffset = 1.5f; d.strikeRadius = 1.6f;
            d.guardArc = 130f; d.guardDamageTaken = 0.15f; d.turnSpeed = 100f;
        });
        // aco frio, como o resto da tropa: sem brilho
        var steel = LookDevBuilder.Mat("SkeletonGuard", KK + "Skeletons/Characters/skeleton_texture.png", new Color(0.66f, 0.7f, 0.78f), Color.black, 0f);
        // na pose de guarda (Blocking) o emblema fica para a frente com o escudo deitado -90 no X
        holds = new[] { ("Skeleton_Shield_Large_B", "handslot.l", new Vector3(-90, 0, 0)) };
        var prefab = EnemyPrefab("Escudeiro", "Skeletons/Characters/Skeleton_Warrior.fbx", def, ctrl, steel, 1.12f, "Skeleton_Blade", "handslot.r");
        holds = null;
        return prefab;
    }

    /// Espectro: o esqueleto de capuz do pack, escurecido, com a lamina; some e reaparece nas costas do heroi.
    public static GameObject ShadePrefab()
    {
        const string rogueFbx = KK + "Skeletons/Characters/Skeleton_Rogue.fbx";
        var ctrl = CombatDevSceneBuilder.Controller(rogueFbx, Dir + "Animation/Espectro.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_B|Running_A", 0f), ("Attack", "1H_Melee_Attack_Stab|1H_Melee_Attack_Chop", 0.9f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A|Death_B", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var def = Asset<EnemyDefinition>("Espectro", d =>
        {
            d.displayName = "Espectro"; d.maxHealth = 30; d.moveSpeed = 3.4f;
            d.attack = EnemyAttackKind.Ambush; d.damage = 11; d.knockback = 6;
            d.attackRange = 10f; d.telegraphTime = 0.95f; d.recoverTime = 0.7f; d.cooldown = 3.2f;
            d.strikeRadius = 1.7f; d.keepDistance = 6f;
        });
        // capuz e osso escurecidos, um pouco frios: sombra, sem brilho
        var shade = LookDevBuilder.Mat("SkeletonShade", KK + "Skeletons/Characters/skeleton_texture.png", new Color(0.42f, 0.4f, 0.52f), Color.black, 0f);
        return EnemyPrefab("Espectro", "Skeletons/Characters/Skeleton_Rogue.fbx", def, ctrl, shade, 1f, "Skeleton_Blade", "handslot.r");
    }

    /// Porta-Estandarte: esqueleto guerreiro do pack com o cajado na mao e um estandarte do pack nas costas.
    /// Fica atras do bando e atira de longe; enquanto vive, quem esta no circulo dele bate mais forte e anda mais rapido.
    public static GameObject BannerPrefab()
    {
        const string warriorFbx = KK + "Skeletons/Characters/Skeleton_Warrior.fbx";
        var ctrl = CombatDevSceneBuilder.Controller(warriorFbx, Dir + "Animation/PortaEstandarte.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Walking_A|Running_A", 0f), ("Attack", "1H_Melee_Attack_Chop|2H_Melee_Attack_Chop", 0.9f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A|Death_B", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var def = Asset<EnemyDefinition>("PortaEstandarte", d =>
        {
            d.displayName = "Porta-Estandarte"; d.maxHealth = 46; d.moveSpeed = 2.8f;
            d.attack = EnemyAttackKind.Projectile; d.damage = 6; d.knockback = 2;
            d.attackRange = 9f; d.telegraphTime = 0.75f; d.recoverTime = 0.4f; d.cooldown = 2.3f;
            d.projectileSpeed = 10f; d.projectileRange = 14f; d.keepDistance = 6.5f;
            d.auraRadius = 6f; d.auraDamageMult = 1.25f; d.auraSpeedMult = 1.15f;
        });
        if (def.projectilePrefab == null) { def.projectilePrefab = ProjectilePrefab(); EditorUtility.SetDirty(def); }
        var bone = LookDevBuilder.Mat("SkeletonBanner", KK + "Skeletons/Characters/skeleton_texture.png", new Color(0.86f, 0.74f, 0.66f), Color.black, 0f);
        holds = new[] { ("Skeleton_Staff", "handslot.l", Vector3.zero) };
        var prefab = EnemyPrefab("PortaEstandarte", "Skeletons/Characters/Skeleton_Warrior.fbx", def, ctrl, bone, 1.05f);
        holds = null;
        // o estandarte nas costas: a peca de parede do pack, fina e vermelha, presa ao peito e erguida acima da cabeca
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        Transform chest = null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == "chest") chest = t;
        if (chest != null)
        {
            var flagMat = LookDevBuilder.Mat("DungeonProp", KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0.018f);
            var flag = LookDevBuilder.Spawn("Dungeon/banner_thin_red.fbx", Vector3.zero, 0, flagMat);
            flag.name = "Banner";
            flag.transform.SetParent(chest, false);
            float s = BannerScale / Mathf.Max(0.0001f, chest.lossyScale.x);
            flag.transform.localScale = Vector3.one * s;
            flag.transform.localPosition = BannerOffset / Mathf.Max(0.0001f, chest.lossyScale.x);
            flag.transform.localRotation = Quaternion.Euler(BannerEuler);
        }
        PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(prefab));
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "Prefabs/PortaEstandarte.prefab");
    }

    // posicao do estandarte nas costas, em metros no mundo (ajustada pela captura do teste)
    static readonly float BannerScale = 0.36f;
    static readonly Vector3 BannerOffset = new(0f, 0.8f, -0.3f);
    static readonly Vector3 BannerEuler = new(0f, 0f, 0f);

    /// Afogado: elemental de Agua da Cripta. Esqueleto mago palido e esverdeado com o cajado do pack; joga um orbe
    /// que deixa uma poca d'agua (encharca o heroi, marca os inimigos). Resiste a Agua, fraco a Terra.
    public static GameObject DrownedPrefab()
    {
        const string mageFbx = KK + "Skeletons/Characters/Skeleton_Mage.fbx";
        var ctrl = CombatDevSceneBuilder.Controller(mageFbx, Dir + "Animation/Afogado.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Walking_A|Running_A", 0f), ("Attack", "Spellcast_Long|Spellcast_Shoot|Spellcast_Raise", 1f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var def = Asset<EnemyDefinition>("Afogado", d =>
        {
            d.displayName = "Afogado"; d.maxHealth = 40; d.moveSpeed = 3f;
            d.attack = EnemyAttackKind.Lob; d.damage = 8; d.knockback = 3; d.element = ElementType.Water;
            d.attackRange = 9.5f; d.telegraphTime = 1f; d.recoverTime = 0.5f; d.cooldown = 2.6f;
            d.strikeRadius = 2.2f; d.keepDistance = 5.5f;
            d.affinity = ElementType.Water; d.puddleSeconds = 5f;
        });
        // osso e trapo encharcados: a mesma textura do pack com a tunica e o chapeu puxados para verde-agua, fosco
        string tex = Recolor(KK + "Skeletons/Characters/skeleton_texture.png", "Assets/Art/Enemies/skeleton_texture_drowned.png", 0.56f, 0.22f);
        var drowned = LookDevBuilder.Mat("SkeletonDrowned2", tex, new Color(0.86f, 0.94f, 0.94f), Color.black, 0f);
        return EnemyPrefab("Afogado", "Skeletons/Characters/Skeleton_Mage.fbx", def, ctrl, drowned, 1.05f, "Skeleton_Staff", "handslot.r");
    }

    /// Copia uma textura do pack girando a matiz so das cores saturadas (pano, chapeu); osso e metal, quase sem cor, ficam.
    /// Devolve o caminho da copia. hueShift em voltas (0,5 = cor oposta).
    static string Recolor(string source, string target, float hueShift, float minSaturation)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(System.IO.File.ReadAllBytes(source));
        var px = tex.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            Color.RGBToHSV(px[i], out float h, out float s, out float v);
            if (s < minSaturation) continue;
            var c = Color.HSVToRGB(Mathf.Repeat(h + hueShift, 1f), s * 0.62f, v * 0.88f);
            c.a = px[i].a;
            px[i] = c;
        }
        tex.SetPixels(px);
        System.IO.File.WriteAllBytes(target, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(target);
        var src = (TextureImporter)AssetImporter.GetAtPath(source);
        var dst = (TextureImporter)AssetImporter.GetAtPath(target);
        if (src != null && dst != null)
        {
            dst.filterMode = src.filterMode; dst.mipmapEnabled = src.mipmapEnabled; dst.textureCompression = src.textureCompression;
            dst.sRGBTexture = src.sRGBTexture; dst.maxTextureSize = src.maxTextureSize;
            dst.SaveAndReimport();
        }
        return target;
    }

    /// Usa as pecas de Assets/Art/Enemies2 quando existirem; sem elas, so a tinta e a escala.
    public static void ElementalPrefabs(out GameObject swarm, out GameObject ember, out GameObject sentinel)
    {
        const string minionFbx = KK + "Skeletons/Characters/Skeleton_Minion.fbx";
        const string warriorFbx = KK + "Skeletons/Characters/Skeleton_Warrior.fbx";
        const string mageFbx = KK + "Skeletons/Characters/Skeleton_Mage.fbx";
        var swarmCtrl = CombatDevSceneBuilder.Controller(minionFbx, Dir + "Animation/Cinzento.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A", 0f), ("Attack", "1H_Melee_Attack_Stab|1H_Melee_Attack_Chop", 0.6f),
            ("Hit", "Hit_A|Hit_B", 0.25f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.6f));
        var emberCtrl = CombatDevSceneBuilder.Controller(warriorFbx, Dir + "Animation/BrasaViva.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Running_A|Walking_A", 0f), ("Attack", "1H_Melee_Attack_Chop|2H_Melee_Attack_Chop", 0.8f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_B|Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));
        var sentinelCtrl = CombatDevSceneBuilder.Controller(mageFbx, Dir + "Animation/Sentinela.controller",
            ("Idle", "Idle_Combat|Idle", 0f), ("Run", "Idle_Combat|Idle", 0f), ("Attack", "Spellcast_Shoot|Spellcast_Long", 0.75f),
            ("Hit", "Hit_A|Hit_B", 0.3f), ("Death", "Death_A", 0f), ("Spawn", "Spawn_Ground|Skeletons_Awaken_Standing|Idle", 0.7f));

        var swarmDef = Asset<EnemyDefinition>("Cinzento", d =>
        {
            d.displayName = "Cinzento"; d.maxHealth = 10; d.moveSpeed = 5.6f;
            d.attack = EnemyAttackKind.Melee; d.damage = 4; d.knockback = 1.5f;
            d.attackRange = 1.6f; d.telegraphTime = 0.45f; d.recoverTime = 0.4f; d.cooldown = 1.1f;
            d.strikeOffset = 1f; d.strikeRadius = 1f;
        });
        var emberDef = Asset<EnemyDefinition>("BrasaViva", d =>
        {
            d.displayName = "Brasa Viva"; d.maxHealth = 44; d.moveSpeed = 3.8f;
            d.attack = EnemyAttackKind.Melee; d.damage = 11; d.knockback = 5; d.element = ElementType.Fire;
            d.attackRange = 2.4f; d.telegraphTime = 0.65f; d.recoverTime = 0.55f; d.cooldown = 1.6f;
            d.strikeOffset = 1.6f; d.strikeRadius = 1.7f;
            d.affinity = ElementType.Fire; d.burstOnDeath = true;
        });
        var sentinelDef = Asset<EnemyDefinition>("Sentinela", d =>
        {
            d.displayName = "Sentinela de Pedra"; d.maxHealth = 46; d.moveSpeed = 0f;
            d.attack = EnemyAttackKind.Projectile; d.damage = 8; d.knockback = 2; d.element = ElementType.Earth;
            d.attackRange = 13f; d.telegraphTime = 0.7f; d.recoverTime = 0.3f; d.cooldown = 1.7f;
            d.projectileSpeed = 12f; d.projectileRange = 16f; d.keepDistance = 0f;
            d.affinity = ElementType.Earth; d.immovable = true;
        });
        if (sentinelDef.projectilePrefab == null) { sentinelDef.projectilePrefab = ProjectilePrefab(); EditorUtility.SetDirty(sentinelDef); }

        AssetDatabase.Refresh();
        bool art = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Enemies2/brasa_head.fbx") != null;
        if (art) HeroBuilder.MatteParts("Assets/Art/Enemies2");
        const string tex = KK + "Skeletons/Characters/skeleton_texture.png";
        var ash = LookDevBuilder.Mat("SkeletonSwarm", tex, new Color(0.62f, 0.6f, 0.62f), Color.black, 0f);
        var coal = LookDevBuilder.Mat("SkeletonEmber", tex, new Color(1.05f, 0.58f, 0.42f), Color.black, 0f);
        var moss = LookDevBuilder.Mat("SkeletonSentinel2", tex, new Color(0.5f, 0.68f, 0.52f), Color.black, 0f);
        dress = art ? (new[] { ("cinzento_head", "head"), ("cinzento_back", "chest"), ("cinzento_weapon", "handslot.r") }, new string[0]) : default;
        swarm = EnemyPrefab("Cinzento", "Skeletons/Characters/Skeleton_Minion.fbx", swarmDef, swarmCtrl, ash, 0.65f);
        dress = art ? (new[] { ("brasa_head", "head"), ("brasa_body", "chest"), ("brasa_shoulder_l", "upperarm.l"), ("brasa_shoulder_r", "upperarm.r"), ("brasa_weapon", "handslot.r") },
            new[] { "Skeleton_Warrior_Helmet", "Skeleton_Warrior_Cloak" }) : default;
        ember = EnemyPrefab("BrasaViva", "Skeletons/Characters/Skeleton_Warrior.fbx", emberDef, emberCtrl, coal, 1.1f);
        dress = art ? (new[] { ("sentinela_head", "head"), ("sentinela_back", "chest"), ("sentinela_base", "root"), ("sentinela_weapon", "handslot.r") }, new[] { "Skeleton_Mage_Hat" }) : default;
        sentinel = EnemyPrefab("Sentinela", "Skeletons/Characters/Skeleton_Mage.fbx", sentinelDef, sentinelCtrl, moss, 1.25f);
        dress = default;
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
        var c = new Color(0.58f, 0.34f, 0.7f);
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = "Projetil";
        go.transform.localScale = Vector3.one * 0.5f;
        go.GetComponent<Renderer>().sharedMaterial = LookDevBuilder.Mat("Glow_Projectile", null, c * 0.6f, c * 0.9f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point; light.color = c; light.intensity = 1.6f; light.range = 3.5f;
        go.AddComponent<Projectile>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// Pecas proprias (arquivo, osso) e malhas do pack a esconder para o proximo EnemyPrefab.
    static ((string file, string bone)[] parts, string[] hide) dress;

    static GameObject EnemyPrefab(string name, string fbx, EnemyDefinition def, RuntimeAnimatorController ctrl, Material mat, float scale = 1f,
        string prop = null, string bone = null)
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
        if (dress.parts != null)
        {
            // as pecas entram com o modelo na origem e sem escala, como no chefe
            model.transform.SetParent(null, false);
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                if (System.Array.IndexOf(dress.hide, r.name) >= 0) r.gameObject.SetActive(false);
            foreach (var (partFile, partBone) in dress.parts)
            {
                string partPath = "Assets/Art/Enemies/" + partFile + ".fbx";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(partPath) == null) partPath = "Assets/Art/Enemies2/" + partFile + ".fbx";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(partPath) != null) HeroBuilder.Attach(model, partPath, partBone);
            }
        }
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * scale;
        col.height *= scale; col.radius *= scale; col.center *= scale;
        void HoldProp(string propName, string boneName, Vector3 euler)
        {
            Transform hand = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == boneName) hand = t;
            if (hand == null) return;
            var gear = LookDevBuilder.Mat("SkeletonGear", KK + "Skeletons/Props/skeleton_texture.png", new Color(0.7f, 0.66f, 0.68f), Color.black, 0f);
            var held = LookDevBuilder.Spawn("Skeletons/Props/" + propName + ".fbx", Vector3.zero, 0, gear);
            held.name = propName;
            held.transform.SetParent(hand, false);
            held.transform.localPosition = Vector3.zero;
            held.transform.localRotation = Quaternion.Euler(euler);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(KK + "Skeletons/Props/" + propName + ".fbx");
            float native = source != null ? source.transform.localScale.x : 1f;
            held.transform.localScale = Vector3.one * (native * model.transform.lossyScale.x / Mathf.Max(0.0001f, hand.lossyScale.x));
        }
        if (prop != null) HoldProp(prop, bone, Vector3.zero);
        if (holds != null) foreach (var (p, b, e) in holds) HoldProp(p, b, e);
        var anim = CombatDevSceneBuilder.Animator(model, ctrl);
        go.AddComponent<EnemyBrain>().Bind(def, anim);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
