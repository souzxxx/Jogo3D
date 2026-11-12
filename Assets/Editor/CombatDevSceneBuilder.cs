using System.IO;
using System.Linq;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Feel;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Monta a cena de teste da frente de combate: sala, heroi jogavel e bonecos de treino.
public static class CombatDevSceneBuilder
{
    public const string KK = "Assets/ThirdParty/KayKit/";
    const string HeroFbx = KK + "Adventurers/Characters/Mage.fbx";
    const string MinionFbx = KK + "Skeletons/Characters/Skeleton_Minion.fbx";
    const string AnimDir = "Assets/Game/Combat/Animation/";
    public const string ScenePath = "Assets/Scenes/Dev/Leonardo_Combat.unity";

    [MenuItem("Quintessence/Dev/Build cena de combate")]
    public static void Build() => BuildPlayable("Leonardo_Combat", ScenePath, root =>
    {
        var skelMat = LookDevBuilder.Mat("Skeleton", KK + "Skeletons/Characters/skeleton_texture.png", Color.white, Color.black, 0f);
        Dummy(root, new Vector3(0, 0, -1), 250, skelMat, dummyCtrl);
        Dummy(root, new Vector3(4, 0, 2.5f), 200, skelMat, dummyCtrl);
        Dummy(root, new Vector3(-3, 0, 4), 160, skelMat, dummyCtrl);
        Dummy(root, new Vector3(5.5f, 0, -4), 300, skelMat, dummyCtrl);
        Dummy(root, new Vector3(7, 0, -2.5f), 270, skelMat, dummyCtrl);
    });

    static RuntimeAnimatorController dummyCtrl;

    /// Sala + heroi jogavel + feedback + camera. extra monta o resto da cena (bonecos, inimigos) antes de salvar.
    public static void BuildPlayable(string rootName, string scenePath, System.Action<Transform> extra)
    {
        Directory.CreateDirectory(AnimDir);
        Directory.CreateDirectory("Assets/Scenes/Dev");
        AssetDatabase.Refresh();

        Loop(HeroFbx, "Idle", "Running_A");
        Loop(MinionFbx, "Idle", "Idle_Combat");
        var heroCtrl = Controller(HeroFbx, AnimDir + "Hero.controller",
            ("Idle", "Idle", 0f), ("Run", "Running_A", 0f),
            // dash: a esquiva certa para o lado em que o heroi vai, em relacao a onde ele olha
            ("DashF", "Dodge_Forward", 0.3f), ("DashB", "Dodge_Backward", 0.3f), ("DashL", "Dodge_Left", 0.3f), ("DashR", "Dodge_Right", 0.3f),
            // combo: dois cortes de cajado e um golpe de cima para fechar
            ("Attack0", "1H_Melee_Attack_Slice_Diagonal", 0.34f), ("Attack1", "1H_Melee_Attack_Slice_Horizontal", 0.34f),
            ("Attack2", "2H_Melee_Attack_Chop", 0.5f),
            // uma skill por elemento: disparo, varrida, erguer do chao e giro
            ("SkillFire", "Spellcast_Shoot", 0.4f), ("SkillWater", "2H_Melee_Attack_Slice", 0.45f),
            ("SkillEarth", "Spellcast_Raise", 0.6f), ("SkillAir", "2H_Melee_Attack_Spinning|2H_Melee_Attack_Spin", 0.45f),
            ("Hit", "Hit_A", 0.3f), ("Death", "Death_A", 0f), ("Quint", "Cheer", 0.7f));
        // cada heroi golpeia com o que carrega: garras, orbe, martelo ou duas adagas (o resto das animacoes e igual)
        var heroCtrls = new RuntimeAnimatorController[4];
        (string a, string b, string c)[] combos =
        {
            ("Unarmed_Melee_Attack_Punch_A|1H_Melee_Attack_Slice_Diagonal", "Unarmed_Melee_Attack_Punch_B|1H_Melee_Attack_Slice_Horizontal", "Unarmed_Melee_Attack_Kick|2H_Melee_Attack_Chop"),
            ("1H_Melee_Attack_Slice_Diagonal", "1H_Melee_Attack_Slice_Horizontal", "1H_Melee_Attack_Chop|2H_Melee_Attack_Chop"),
            ("2H_Melee_Attack_Slice|1H_Melee_Attack_Slice_Diagonal", "2H_Melee_Attack_Stab|1H_Melee_Attack_Slice_Horizontal", "2H_Melee_Attack_Chop"),
            ("Dualwield_Melee_Attack_Slice|1H_Melee_Attack_Slice_Diagonal", "Dualwield_Melee_Attack_Stab|1H_Melee_Attack_Slice_Horizontal", "Dualwield_Melee_Attack_Chop|2H_Melee_Attack_Chop"),
        };
        for (int h = 0; h < 4; h++)
            heroCtrls[h] = Controller(HeroFbx, AnimDir + "Hero_" + HeroBuilder.Specs[h].Name + ".controller",
                ("Idle", "Idle", 0f), ("Run", "Running_A", 0f),
                ("DashF", "Dodge_Forward", 0.3f), ("DashB", "Dodge_Backward", 0.3f), ("DashL", "Dodge_Left", 0.3f), ("DashR", "Dodge_Right", 0.3f),
                ("Attack0", combos[h].a, 0.34f), ("Attack1", combos[h].b, 0.34f), ("Attack2", combos[h].c, 0.5f),
                ("SkillFire", "Spellcast_Shoot", 0.4f), ("SkillWater", "2H_Melee_Attack_Slice", 0.45f),
                ("SkillEarth", "Spellcast_Raise", 0.6f), ("SkillAir", "2H_Melee_Attack_Spinning|2H_Melee_Attack_Spin", 0.45f),
                ("Hit", "Hit_A", 0.3f), ("Death", "Death_A", 0f), ("Quint", "Cheer", 0.7f));
        dummyCtrl = Controller(MinionFbx, AnimDir + "Dummy.controller", ("Idle", "Idle_Combat", 0f));

        var root = LookDevBuilder.BeginArena(rootName);
        // depois de abrir a cena nova: criar antes deixa as referencias nulas
        var defs = new[] { FireDef(), WaterDef(), EarthDef(PillarPrefab()), AirDef() };
        var table = Reactions();
        var flat = FlatMaterial();
        AssetDatabase.SaveAssets();
        RenderSettings.ambientSkyColor *= 1.5f;
        RenderSettings.ambientEquatorColor *= 1.5f;
        RenderSettings.ambientGroundColor *= 1.5f;

        // heroi
        var player = new GameObject("Player") { tag = "Player" };
        player.transform.SetParent(root, false);
        player.transform.position = new Vector3(-5, 0.05f, -3);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2.2f; cc.radius = 0.5f; cc.center = new Vector3(0, 1.1f, 0);
        player.AddComponent<Health>().Configure(100, Team.Player);
        var pc = player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInputReader>();
        var pso = new SerializedObject(pc);
        var arr = pso.FindProperty("elements");
        arr.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
        pso.FindProperty("reactions").objectReferenceValue = table;
        var also = pso.FindProperty("alsoAwakened");
        also.arraySize = 3;
        also.GetArrayElementAtIndex(0).intValue = (int)ElementType.Water;
        also.GetArrayElementAtIndex(1).intValue = (int)ElementType.Earth;
        also.GetArrayElementAtIndex(2).intValue = (int)ElementType.Air;
        pso.ApplyModifiedPropertiesWithoutUndo();
        // os quatro herois moram no mesmo prefab; o HeroSkin mostra o do elemento inicial
        HeroBuilder.Prepare();
        var heroModels = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            heroModels[i] = HeroBuilder.Build(i, heroCtrls[i]);
            heroModels[i].transform.SetParent(player.transform, false);
            heroModels[i].SetActive(i == 0);
        }
        var heroAnim = player.AddComponent<PlayerAnimator>();
        heroAnim.Bind(pc, heroModels[0].GetComponent<Animator>());
        player.AddComponent<HeroSkin>().Bind(heroModels, heroAnim, pc);
        var light = new GameObject("PresenceLight").AddComponent<Light>();
        light.transform.SetParent(player.transform, false);
        light.transform.localPosition = new Vector3(0, 2.4f, 0);
        light.type = LightType.Point; light.color = new Color(1f, 0.8f, 0.6f); light.intensity = 1.6f; light.range = 6f;

        var fb = new GameObject("CombatFeedback");
        fb.transform.SetParent(root, false);
        fb.AddComponent<CombatFeedback>().Configure(LookDevBuilder.Additive(), flat);
        var fso = new SerializedObject(fb.GetComponent<CombatFeedback>());
        fso.FindProperty("particleMaterial").objectReferenceValue = LookDevBuilder.Additive();
        fso.FindProperty("flatMaterial").objectReferenceValue = flat;
        fso.ApplyModifiedPropertiesWithoutUndo();

        var cam = LookDevBuilder.MakeCamera();
        cam.gameObject.AddComponent<CameraShake>();
        cam.gameObject.AddComponent<FollowCamera>().SetTarget(player.transform);
        var so = new SerializedObject(cam.GetComponent<FollowCamera>());
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        extra?.Invoke(root);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
        AssetDatabase.SaveAssets();
    }

    const string DataDir = "Assets/Game/Combat/Data/";

    static T Asset<T>(string name, System.Action<T> init) where T : ScriptableObject
    {
        Directory.CreateDirectory(DataDir);
        string path = DataDir + name + ".asset";
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a; // ja existe: os numeros sao de quem balanceia, nao sobrescreve
        a = ScriptableObject.CreateInstance<T>();
        init(a);
        AssetDatabase.CreateAsset(a, path);
        return a;
    }

    static ElementDefinition FireDef() => Asset<ElementDefinition>("Fire", d =>
    {
        d.element = ElementType.Fire;
        d.finisherAppliesStatus = true; d.finisherStatus = StatusType.Burn; d.finisherStatusDuration = 3f;
        d.skillName = "Labareda"; d.skillDamage = 18f; d.skillCooldown = 4f; d.skillRange = 5.5f; d.skillArc = 70f; d.skillKnockback = 3f;
        d.skillAppliesStatus = true; d.skillStatus = StatusType.Burn; d.skillStatusDuration = 3f;
        d.dash = DashVariant.FireTrail;
    });

    static ElementDefinition WaterDef() => Asset<ElementDefinition>("Water", d =>
    {
        d.element = ElementType.Water;
        d.knockbackMultiplier = 1.8f;
        d.finisherAppliesStatus = true; d.finisherStatus = StatusType.Soaked; d.finisherStatusDuration = 3f;
        d.skillName = "Mare"; d.skillDamage = 12f; d.skillCooldown = 5f; d.skillRange = 7f; d.skillArc = 50f; d.skillKnockback = 14f;
        d.skillAppliesStatus = true; d.skillStatus = StatusType.Soaked; d.skillStatusDuration = 3f;
        d.dash = DashVariant.Cleanse;
    });

    static ElementDefinition EarthDef(GameObject pillar) => Asset<ElementDefinition>("Earth", d =>
    {
        d.element = ElementType.Earth;
        d.damageMultiplier = 1.15f;
        d.finisherAppliesStatus = true; d.finisherStatus = StatusType.ArmorBroken; d.finisherStatusDuration = 4f;
        d.skillName = "Pilar de Pedra"; d.skillShape = SkillShape.Summon; d.skillSummonPrefab = pillar; d.skillSummonDistance = 3.5f;
        d.skillDamage = 16f; d.skillCooldown = 6f; d.skillRange = 3.4f; d.skillKnockback = 6f;
        d.skillAppliesStatus = true; d.skillStatus = StatusType.Stun; d.skillStatusDuration = 0.8f;
        d.dash = DashVariant.Armor; d.dashDistanceMultiplier = 0.67f;
    });

    static ElementDefinition AirDef() => Asset<ElementDefinition>("Air", d =>
    {
        d.element = ElementType.Air;
        d.critChance = 0.2f;
        d.skillName = "Vendaval"; d.skillShape = SkillShape.Radial;
        d.skillDamage = 10f; d.skillCooldown = 4f; d.skillRange = 4f; d.skillKnockback = 16f;
        d.dash = DashVariant.Plain; d.dashCharges = 2;
    });

    static GameObject PillarPrefab()
    {
        const string path = "Assets/Game/Combat/Prefabs/StonePillar.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        Directory.CreateDirectory("Assets/Game/Combat/Prefabs");
        AssetDatabase.Refresh();
        var go = new GameObject("StonePillar");
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 2.6f; col.radius = 0.75f; col.center = new Vector3(0, 1.3f, 0);
        go.AddComponent<Health>().Configure(40, Team.Player);
        go.AddComponent<TimedLife>();
        var stone = LookDevBuilder.Mat("EarthStone", KK + "Dungeon/dungeon_texture.png", new Color(1.1f, 0.85f, 0.6f), Color.black, 0f);
        var model = LookDevBuilder.Spawn("Dungeon/pillar.fbx", Vector3.zero, 0, stone, 0.6f);
        model.name = "Model";
        model.transform.SetParent(go.transform, false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static ReactionTable Reactions() => Asset<ReactionTable>("Reactions", t =>
    {
        Reaction Add(string n, ElementType a, ElementType b, float dmg, float rad, float kb)
        {
            var r = new Reaction { name = n, a = a, b = b, damage = dmg, radius = rad, knockback = kb };
            t.reactions.Add(r);
            return r;
        }
        var storm = Add("Tempestade de Fogo", ElementType.Fire, ElementType.Air, 20, 2.5f, 6);
        storm.appliesStatus = true; storm.status = StatusType.Burn; storm.statusDuration = 3;
        // nuvem que fica 4 s: quem esta dentro erra metade dos ataques
        var steam = Add("Vapor", ElementType.Fire, ElementType.Water, 8, 3f, 2);
        steam.zoneDuration = 4; steam.zoneInterval = 0.5f; steam.zoneAppliesStatus = true; steam.zoneStatus = StatusType.Blinded; steam.zoneStatusDuration = 0.8f;
        // poca de lava: 6 a cada 0,5 s por 4 s
        var magma = Add("Magma", ElementType.Fire, ElementType.Earth, 10, 2f, 2);
        magma.zoneDuration = 4; magma.zoneInterval = 0.5f; magma.zoneDamage = 6;
        // enraiza 2 s e deixa Encharcado (dura 3 s alem da raiz)
        var mud = Add("Lama", ElementType.Water, ElementType.Earth, 8, 2f, 0);
        mud.appliesStatus = true; mud.status = StatusType.Rooted; mud.statusDuration = 2;
        mud.appliesSecondStatus = true; mud.secondStatus = StatusType.Soaked; mud.secondStatusDuration = 5;
        var blizzard = Add("Nevasca", ElementType.Water, ElementType.Air, 10, 2f, 0);
        blizzard.appliesStatus = true; blizzard.status = StatusType.Frozen; blizzard.statusDuration = 2;
        // simplificado: explosao de lascas em area maior, em vez de 6 projeteis
        Add("Estilhacos", ElementType.Earth, ElementType.Air, 14, 3.5f, 9);
    });

    static Material FlatMaterial()
    {
        const string path = "Assets/Game/Feel/Materials/Flat.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        Directory.CreateDirectory("Assets/Game/Feel/Materials");
        AssetDatabase.Refresh();
        m = new Material(Shader.Find("Sprites/Default"));
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void Dummy(Transform root, Vector3 pos, float yRot, Material mat, RuntimeAnimatorController ctrl)
    {
        var go = new GameObject("Dummy");
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 2.2f; col.radius = 0.55f; col.center = new Vector3(0, 1.1f, 0);
        go.AddComponent<Health>().Configure(150, Team.Enemy);
        go.AddComponent<DummyTarget>();
        var model = LookDevBuilder.Spawn("Skeletons/Characters/Skeleton_Minion.fbx", Vector3.zero, 0, mat);
        model.name = "Model";
        model.transform.SetParent(go.transform, false);
        Animator(model, ctrl);
    }

    public static Animator Animator(GameObject model, RuntimeAnimatorController ctrl)
    {
        var a = model.GetComponent<Animator>();
        if (a == null) a = model.AddComponent<Animator>();
        a.runtimeAnimatorController = ctrl;
        a.applyRootMotion = false;
        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return a;
    }

    public static void Loop(string fbx, params string[] names)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(fbx);
        var clips = mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations;
        bool changed = false;
        foreach (var c in clips)
            if (names.Contains(c.name) && !c.loopTime) { c.loopTime = true; changed = true; }
        if (!changed) return;
        mi.clipAnimations = clips;
        mi.SaveAndReimport();
    }

    /// states: (nome do estado, nome do clip, duracao desejada em segundos; 0 = velocidade normal)
    public static RuntimeAnimatorController Controller(string fbx, string path, params (string state, string clip, float fit)[] states)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var (state, clip, fit) in states)
        {
            var st = sm.AddState(state);
            // "A|B|C": usa o primeiro clip que existir no modelo
            var found = clip.Split('|').Select(n => clips.TryGetValue(n, out var c) ? c : null).FirstOrDefault(c => c != null);
            if (found == null) { Debug.LogWarning($"{fbx}: nenhum clip de '{clip}' para o estado {state}"); continue; }
            st.motion = found;
            st.speed = fit > 0f ? Mathf.Clamp(found.length / fit, 0.5f, 3f) : 1f;
        }
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }
}
