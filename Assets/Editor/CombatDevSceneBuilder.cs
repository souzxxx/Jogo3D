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
    const string KK = "Assets/ThirdParty/KayKit/";
    const string HeroFbx = KK + "Adventurers/Characters/Mage.fbx";
    const string MinionFbx = KK + "Skeletons/Characters/Skeleton_Minion.fbx";
    const string AnimDir = "Assets/Game/Combat/Animation/";
    public const string ScenePath = "Assets/Scenes/Dev/Leonardo_Combat.unity";

    [MenuItem("Quintessence/Dev/Build cena de combate")]
    public static void Build()
    {
        Directory.CreateDirectory(AnimDir);
        Directory.CreateDirectory("Assets/Scenes/Dev");
        AssetDatabase.Refresh();

        Loop(HeroFbx, "Idle", "Running_A");
        Loop(MinionFbx, "Idle", "Idle_Combat");
        var heroCtrl = Controller(HeroFbx, AnimDir + "Hero.controller",
            ("Idle", "Idle", 0f), ("Run", "Running_A", 0f), ("Dash", "Dodge_Forward", 0.3f),
            ("Attack0", "Spellcast_Shoot", 0.34f), ("Attack1", "Spellcast_Shoot", 0.34f),
            ("Attack2", "Spellcast_Long", 0.5f), ("Skill", "Spellcast_Raise", 0.4f));
        var dummyCtrl = Controller(MinionFbx, AnimDir + "Dummy.controller", ("Idle", "Idle_Combat", 0f));

        var root = LookDevBuilder.BeginArena("Leonardo_Combat");
        // depois de abrir a cena nova: criar antes deixa as referencias nulas
        var defs = new[] { FireDef(), WaterDef() };
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
        also.arraySize = 1;
        also.GetArrayElementAtIndex(0).intValue = (int)ElementType.Water;
        pso.ApplyModifiedPropertiesWithoutUndo();
        var heroMat = LookDevBuilder.Mat("Hero", KK + "Adventurers/Characters/mage_texture.png", Color.white, Color.black, 0f);
        var model = LookDevBuilder.Spawn("Adventurers/Characters/Mage.fbx", Vector3.zero, 0, heroMat);
        model.name = "Model";
        model.transform.SetParent(player.transform, false);
        LookDevBuilder.Hide(model, "Spellbook", "1H_Wand");
        var anim = Animator(model, heroCtrl);
        player.AddComponent<PlayerAnimator>().Bind(pc, anim);
        var light = new GameObject("PresenceLight").AddComponent<Light>();
        light.transform.SetParent(player.transform, false);
        light.transform.localPosition = new Vector3(0, 2.4f, 0);
        light.type = LightType.Point; light.color = new Color(1f, 0.8f, 0.6f); light.intensity = 1.6f; light.range = 6f;

        // bonecos de treino
        var skelMat = LookDevBuilder.Mat("Skeleton", KK + "Skeletons/Characters/skeleton_texture.png", Color.white, Color.black, 0f);
        Dummy(root, new Vector3(0, 0, -1), 250, skelMat, dummyCtrl);
        Dummy(root, new Vector3(4, 0, 2.5f), 200, skelMat, dummyCtrl);
        Dummy(root, new Vector3(-3, 0, 4), 160, skelMat, dummyCtrl);

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

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
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

    static ReactionTable Reactions() => Asset<ReactionTable>("Reactions", t =>
    {
        void Add(string n, ElementType a, ElementType b, float dmg, float rad, float kb, StatusType? st, float dur) =>
            t.reactions.Add(new Reaction { name = n, a = a, b = b, damage = dmg, radius = rad, knockback = kb,
                appliesStatus = st.HasValue, status = st ?? StatusType.Burn, statusDuration = dur });
        Add("Tempestade de Fogo", ElementType.Fire, ElementType.Air, 20, 2.5f, 6, StatusType.Burn, 3);
        Add("Vapor", ElementType.Fire, ElementType.Water, 8, 3f, 2, StatusType.Blinded, 4);
        Add("Magma", ElementType.Fire, ElementType.Earth, 10, 2f, 2, StatusType.Burn, 4);
        Add("Lama", ElementType.Water, ElementType.Earth, 8, 2f, 0, StatusType.Rooted, 2);
        Add("Nevasca", ElementType.Water, ElementType.Air, 10, 2f, 0, StatusType.Frozen, 2);
        Add("Estilhacos", ElementType.Earth, ElementType.Air, 14, 3f, 8, null, 0);
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

    static Animator Animator(GameObject model, RuntimeAnimatorController ctrl)
    {
        var a = model.GetComponent<Animator>();
        if (a == null) a = model.AddComponent<Animator>();
        a.runtimeAnimatorController = ctrl;
        a.applyRootMotion = false;
        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return a;
    }

    static void Loop(string fbx, params string[] names)
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
    static RuntimeAnimatorController Controller(string fbx, string path, params (string state, string clip, float fit)[] states)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var (state, clip, fit) in states)
        {
            var st = sm.AddState(state);
            st.motion = clips[clip];
            st.speed = fit > 0f ? Mathf.Clamp(clips[clip].length / fit, 0.5f, 3f) : 1f;
        }
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }
}
