using System.IO;
using System.Linq;
using Quintessence.Combat;
using Quintessence.Core;
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
            ("Attack0", "1H_Melee_Attack_Slice_Diagonal", 0.42f), ("Attack1", "1H_Melee_Attack_Slice_Horizontal", 0.42f),
            ("Attack2", "2H_Melee_Attack_Chop", 0.6f));
        var dummyCtrl = Controller(MinionFbx, AnimDir + "Dummy.controller", ("Idle", "Idle_Combat", 0f));

        var root = LookDevBuilder.BeginArena("Leonardo_Combat");

        // heroi
        var player = new GameObject("Player");
        player.transform.SetParent(root, false);
        player.transform.position = new Vector3(-5, 0.05f, -3);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2.2f; cc.radius = 0.5f; cc.center = new Vector3(0, 1.1f, 0);
        player.AddComponent<Health>().Configure(100, Team.Player);
        var pc = player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInputReader>();
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

        var cam = LookDevBuilder.MakeCamera();
        cam.gameObject.AddComponent<FollowCamera>().SetTarget(player.transform);
        var so = new SerializedObject(cam.GetComponent<FollowCamera>());
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
    }

    static void Dummy(Transform root, Vector3 pos, float yRot, Material mat, RuntimeAnimatorController ctrl)
    {
        var go = new GameObject("Dummy");
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 2.2f; col.radius = 0.55f; col.center = new Vector3(0, 1.1f, 0);
        go.AddComponent<Health>().Configure(40, Team.Enemy);
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
