using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Monta a sala de look-dev em duas variacoes (A: toon + contorno, B: URP Lit) e salva os prints em docs/img.
/// Ferramenta de editor, nao entra na build.
public static class LookDevBuilder
{
    const string KK = "Assets/ThirdParty/KayKit/";
    const string Out = "Assets/Art/LookDev/";
    const string SceneDir = "Assets/Scenes/LookDev/";

    static bool toon;
    static string matDir;
    static readonly Dictionary<string, Material> cache = new();
    static readonly List<ParticleSystem> particles = new();
    static readonly StringBuilder log = new();
    static Transform root;

    static readonly Color Fire = new(1f, 0.26f, 0.04f);
    static readonly Color Water = new(0.1f, 0.65f, 1f);
    static readonly Color Earth = new(0.6f, 0.34f, 0.1f);
    static readonly Color Air = new(0.6f, 1f, 0.85f);
    static readonly Color Ember = new(1f, 0.16f, 0.02f);
    static readonly Color Amber = new(0.85f, 0.5f, 0.16f);

    [MenuItem("Quintessence/Look-dev/Build A + B")]
    public static void BuildAll()
    {
        log.Clear();
        Build(false); // variacao B (URP Lit) foi a escolhida pelo time
        File.WriteAllText("Logs/lookdev_builder.txt", log.ToString());
    }

    /// Sala vazia (cenario, luz, pos e colisores) numa cena nova, para as cenas de teste das frentes.
    public static Transform BeginArena(string rootName)
    {
        toon = false;
        cache.Clear();
        particles.Clear();
        matDir = Out + "Materials_B_Lit/";
        Directory.CreateDirectory(matDir);
        AssetDatabase.Refresh();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject(rootName).transform;
        Environment();
        Room();
        PostVolume();
        AddColliders();
        return root;
    }

    static void AddColliders()
    {
        foreach (Transform child in root)
        {
            string n = child.name;
            if (n.StartsWith("floor") || n.StartsWith("Ember") || n.StartsWith("torch")) continue;
            foreach (var mf in child.GetComponentsInChildren<MeshFilter>())
                if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<MeshCollider>();
        }
        float w = RoomW * 4f, d = RoomD * 4f;
        Box("FloorCollider", new Vector3(0, -0.5f, 0), new Vector3(w + 4f, 1, d + 4f));
        Box("FrontLimit", new Vector3(0, 2, -(d / 2f + 0.3f)), new Vector3(w + 4f, 6, 0.6f));
    }

    static void Box(string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = center;
        go.AddComponent<BoxCollider>().size = size;
    }

    static void Build(bool useToon)
    {
        toon = useToon;
        cache.Clear();
        particles.Clear();
        string tag = toon ? "A_Toon" : "B_Lit";
        matDir = Out + "Materials_" + tag + "/";
        Directory.CreateDirectory(matDir);
        Directory.CreateDirectory(SceneDir);
        Directory.CreateDirectory("docs/img");
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("LookDev_" + tag).transform;

        Environment();
        Room();
        Actors();
        Elements();
        var cam = MakeCamera();
        PostVolume();

        foreach (var ps in particles) ps.Simulate(2.5f, true, true);
        EditorSceneManager.SaveScene(scene, SceneDir + "LookDev_" + tag + ".unity");
        AssetDatabase.SaveAssets();

        Capture(cam, "Logs/lookdev_warmup.png", 320, 180);
        Capture(cam, "docs/img/lookdev_v1.png", 1600, 900);
        // close-up
        Aim(cam, new Vector3(2.2f, 0.9f, -1.8f), 12f);
        Capture(cam, "docs/img/lookdev_v1_close.png", 1600, 900);
        Aim(cam, new Vector3(0, 0, -0.5f), 27f);
    }

    // ---------- materials ----------

    public static Material Mat(string key, string texPath, Color baseColor, Color emission, float outline)
    {
        if (cache.TryGetValue(key, out var m)) return m;
        m = AssetDatabase.LoadAssetAtPath<Material>(matDir + key + ".mat");
        if (m != null) { cache[key] = m; return m; }
        if (toon)
        {
            m = new Material(Shader.Find("Quintessence/Toon"));
            m.SetColor("_OutlineColor", new Color(0.07f, 0.06f, 0.12f));
            m.SetFloat("_OutlineWidth", outline);
        }
        else
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetFloat("_Smoothness", 0.12f);
            if (emission.maxColorComponent > 0)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
        m.SetTexture("_BaseMap", string.IsNullOrEmpty(texPath) ? Texture2D.whiteTexture : AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        m.SetColor("_BaseColor", baseColor);
        m.SetColor("_EmissionColor", emission);
        AssetDatabase.CreateAsset(m, matDir + key + ".mat");
        cache[key] = m;
        return m;
    }

    static Material Dungeon => Mat("Dungeon", KK + "Dungeon/dungeon_texture.png", new Color(0.5f, 0.52f, 0.68f), Color.black, 0f);
    static Material Prop => Mat("DungeonProp", KK + "Dungeon/dungeon_texture.png", Color.white, Color.black, 0.018f);
    static Material Glow(string key, Color c, float intensity) => Mat("Glow_" + key, null, c * 0.6f, c * intensity, 0f);

    static Material additive;
    public static Material Additive()
    {
        if (additive != null) return additive;
        const string path = Out + "ParticleAdditive.mat";
        additive = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (additive != null) return additive;
        additive = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        additive.SetFloat("_Surface", 1);
        additive.SetFloat("_Blend", 2);
        additive.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        additive.SetInt("_DstBlend", (int)BlendMode.One);
        additive.SetInt("_ZWrite", 0);
        additive.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        additive.renderQueue = 3000;
        additive.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
        AssetDatabase.CreateAsset(additive, path);
        return additive;
    }

    // ---------- helpers ----------

    public static GameObject Spawn(string path, Vector3 pos, float yRot, Material mat, float scale = 1f)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(KK + path);
        if (asset == null) { log.AppendLine("MISSING " + path); return new GameObject("missing_" + path); }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yRot, 0));
        go.transform.localScale = Vector3.one * scale;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        return go;
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(t);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Light Point(string name, Vector3 pos, Color c, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }

    static ParticleSystem Fx(string name, Vector3 pos, Vector3 euler, Color c, float rate, float life, float speed,
        float size, ParticleSystemShapeType shape, float radius, float angle, float gravity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(c * 0.8f, Color.Lerp(c, Color.white, 0.15f));
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.4f, size);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = shape; sh.radius = radius; sh.angle = angle;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0, 1) });
        col.color = g;
        var sol = ps.sizeOverLifetime; sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Additive();
        particles.Add(ps);
        return ps;
    }

    static void Pose(GameObject go, string fbx, float t, params string[] clipNames)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(KK + fbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToList();
        log.AppendLine(fbx + " clips: " + string.Join(", ", clips.Select(c => c.name)));
        log.AppendLine(fbx + " renderers: " + string.Join(", ", go.GetComponentsInChildren<Renderer>(true).Select(r => r.name)));
        log.AppendLine(fbx + " bounds: " + BoundsOf(go).size);
        foreach (var n in clipNames)
        {
            var clip = clips.FirstOrDefault(c => c.name.ToLower().Contains(n.ToLower()));
            if (clip == null) continue;
            clip.SampleAnimation(go, clip.length * t);
            log.AppendLine("  posed with " + clip.name);
            return;
        }
    }

    public static void Hide(GameObject go, params string[] names)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            if (names.Any(n => r.name.ToLower().Contains(n.ToLower()))) r.gameObject.SetActive(false);
    }

    // ---------- scene ----------

    static void Environment()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.2f, 0.36f);
        RenderSettings.ambientEquatorColor = new Color(0.17f, 0.15f, 0.26f);
        RenderSettings.ambientGroundColor = new Color(0.09f, 0.07f, 0.11f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.09f);
        RenderSettings.fogStartDistance = 26f;
        RenderSettings.fogEndDistance = 60f;

        var go = new GameObject("Sun_Dusk");
        go.transform.SetParent(root, false);
        go.transform.rotation = Quaternion.Euler(40, -38, 0);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.66f, 0.46f);
        l.intensity = 0.75f;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.85f;
    }

    /// Tamanho da sala em placas de piso (4 m cada). O titulo e as cenas de teste usam 5 x 4; a run usa uma maior.
    public static int RoomW = 5, RoomD = 4;

    static void Room()
    {
        var probe = Spawn("Dungeon/floor_tile_large.fbx", Vector3.zero, 0, Dungeon);
        float T = Mathf.Round(BoundsOf(probe).size.x);
        float floorTop = BoundsOf(probe).max.y;
        log.AppendLine("floorTop " + floorTop);
        log.AppendLine("floor_tile_large bounds " + BoundsOf(probe).size + " -> T=" + T);
        Object.DestroyImmediate(probe);
        if (T < 1) T = 4;

        int W = RoomW, D = RoomD;
        var rng = new System.Random(7);
        for (int i = 0; i < W; i++)
        for (int j = 0; j < D; j++)
        {
            string tile = "Dungeon/floor_tile_large.fbx";
            Spawn(tile, new Vector3((i - (W - 1) / 2f) * T, -floorTop, (j - (D - 1) / 2f) * T), 0, Dungeon);
        }

        float hx = W * T / 2f, hz = D * T / 2f;
        string[] back = { "wall.fbx", "wall_cracked.fbx", "wall_arched.fbx", "wall.fbx", "wall_broken.fbx" };
        for (int i = 0; i < W; i++)
            Spawn("Dungeon/" + back[i % back.Length], new Vector3((i - (W - 1) / 2f) * T, 0, hz), 0, Dungeon);
        for (int j = 0; j < D; j++)
        {
            float z = (j - (D - 1) / 2f) * T;
            Spawn("Dungeon/wall.fbx", new Vector3(-hx, 0, z), 90, Dungeon);
            Spawn("Dungeon/" + (j == 1 ? "wall_doorway.fbx" : "wall.fbx"), new Vector3(hx, 0, z), 90, Dungeon);
        }
        Spawn("Dungeon/wall_corner.fbx", new Vector3(-hx, 0, hz), 0, Dungeon);
        Spawn("Dungeon/wall_corner.fbx", new Vector3(hx, 0, hz), 270, Dungeon);

        // posicoes pensadas para 20 x 16 e esticadas para o tamanho da sala
        float sx = hx / 10f, sz = hz / 8f;
        Spawn("Dungeon/pillar_decorated.fbx", new Vector3(-(hx - 1.4f), 0, 2f * sz), 0, Prop, 0.8f);
        Spawn("Dungeon/pillar_decorated.fbx", new Vector3(hx - 1.4f, 0, 2f * sz), 0, Prop, 0.8f);
        Spawn("Dungeon/barrel_large.fbx", new Vector3(-(hx - 1.4f), 0, hz - 1.6f), 20, Prop);
        Spawn("Dungeon/barrel_small_stack.fbx", new Vector3(-(hx - 2.8f), 0, hz - 1.4f), 0, Prop);
        Spawn("Dungeon/crates_stacked.fbx", new Vector3(hx - 1.7f, 0, hz - 1.7f), -15, Prop);
        Spawn("Dungeon/chest_gold.fbx", new Vector3(0, 0, hz - 1.8f), 180, Prop);
        Spawn("Dungeon/rubble_half.fbx", new Vector3(-(hx - 1.7f), 0, -2f * sz), 100, Prop, 0.5f);

        foreach (float x in new[] { -0.8f * hx, -0.4f * hx, 0.4f * hx, 0.8f * hx })
        {
            Spawn("Dungeon/torch_mounted.fbx", new Vector3(x, 2.2f, hz - 0.55f), 180, Prop);
            Point("TorchLight", new Vector3(x, 2.9f, hz - 1.1f), new Color(1f, 0.55f, 0.2f), 2.2f, 7f);
            Fx("TorchFlame", new Vector3(x, 2.95f, hz - 0.85f), new Vector3(-90, 0, 0), new Color(1f, 0.45f, 0.08f), 30, 0.6f, 0.9f, 0.4f,
                ParticleSystemShapeType.Cone, 0.08f, 8, -0.1f);
        }

        // corrupcao: rachaduras de brasa no chao + cristais ambar
        Crack(new Vector3(-7.5f * sx, 0, -6.5f * sz), 35, 9, 11);
        Crack(new Vector3(6.8f * sx, 0, -7.4f * sz), 5, 7, 23);
        Crack(new Vector3(-2f * sx, 0, 4.5f * sz), 80, 6, 5);
        Crystals(new Vector3(-(hx - 1.6f), 0, -5.6f * sz), 31);
        Crystals(new Vector3(hx - 1.8f, 0, -6.2f * sz), 47);
        Crystals(new Vector3(hx - 1.4f, 0, 2.6f * sz), 59);
        if (W > 5)
        {
            // sala grande: mais uma rachadura e mais cristais para o meio nao ficar vazio
            Crack(new Vector3(4f * sx, 0, 1.5f * sz), 200, 7, 41);
            Crystals(new Vector3(-(hx - 1.6f), 0, 3.4f * sz), 73);
        }
    }

    static void Crack(Vector3 start, float heading, int segments, int seed)
    {
        var rng = new System.Random(seed);
        var mat = Glow("Ember", Ember, 1.15f);
        var p = start;
        for (int i = 0; i < segments; i++)
        {
            heading += (float)(rng.NextDouble() - 0.5) * 80f;
            float len = 0.6f + (float)rng.NextDouble() * 0.9f;
            var dir = Quaternion.Euler(0, heading, 0) * Vector3.forward;
            float w = 0.07f + (float)rng.NextDouble() * 0.08f;
            Prim(PrimitiveType.Cube, "EmberCrack", p + dir * len / 2 + Vector3.up * 0.03f, new Vector3(0, heading, 0),
                new Vector3(w, 0.03f, len + 0.12f), mat);
            p += dir * len;
            if (i % 3 == 1) Point("EmberLight", p + Vector3.up * 0.4f, Ember, 0.7f, 3f);
        }
        Fx("EmberSparks", (start + p) / 2 + Vector3.up * 0.1f, new Vector3(-90, 0, 0), Ember, 10, 2.2f, 0.7f, 0.14f,
            ParticleSystemShapeType.Circle, 2.5f, 0, -0.05f);
    }

    static void Crystals(Vector3 center, int seed)
    {
        var rng = new System.Random(seed);
        var mat = Glow("Amber", Amber, 0.55f);
        for (int i = 0; i < 5; i++)
        {
            float h = 0.5f + (float)rng.NextDouble() * 1.1f;
            var off = new Vector3((float)rng.NextDouble() - 0.5f, 0, (float)rng.NextDouble() - 0.5f) * 1.1f;
            var tilt = new Vector3((float)(rng.NextDouble() - 0.5) * 40, rng.Next(360), (float)(rng.NextDouble() - 0.5) * 40);
            var c = Prim(PrimitiveType.Cube, "AmberCrystal", center + off, tilt, new Vector3(0.34f, h * 1.3f, 0.34f), mat);
            c.GetComponent<MeshFilter>().sharedMesh = CrystalMesh();
        }
        Point("AmberLight", center + Vector3.up * 0.9f, Amber, 1.4f, 4.5f);
    }

    static Mesh crystal;
    /// Prisma hexagonal com ponta, base em y=0 e altura 1, normais chapadas.
    public static Mesh CrystalMesh()
    {
        if (crystal != null) return crystal;
        const string path = Out + "Crystal.asset";
        crystal = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (crystal != null) return crystal;
        var v = new List<Vector3>();
        Vector3 P(int i, float y, float rad) { float a = i * Mathf.PI / 3f; return new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad); }
        var tip = new Vector3(0, 1, 0);
        for (int i = 0; i < 6; i++)
        {
            Vector3 b0 = P(i, 0, 0.38f), b1 = P(i + 1, 0, 0.38f), m0 = P(i, 0.68f, 0.5f), m1 = P(i + 1, 0.68f, 0.5f);
            v.AddRange(new[] { b0, m0, m1, b0, m1, b1, m0, tip, m1 });
        }
        crystal = new Mesh { name = "Crystal" };
        crystal.SetVertices(v);
        crystal.SetTriangles(Enumerable.Range(0, v.Count).ToArray(), 0);
        crystal.RecalculateNormals();
        crystal.RecalculateBounds();
        AssetDatabase.CreateAsset(crystal, path);
        return crystal;
    }

    static void Actors()
    {
        var heroMat = Mat("Hero", KK + "Adventurers/Characters/mage_texture.png", Color.white, Color.black, 0.022f);
        var skelMat = Mat("Skeleton", KK + "Skeletons/Characters/skeleton_texture.png", Color.white, Color.black, 0.022f);

        var hero = Spawn("Adventurers/Characters/Mage.fbx", new Vector3(0, 0, -1), 110, heroMat);
        Pose(hero, "Adventurers/Characters/Mage.fbx", 0.45f, "Spellcast_Shoot", "Spellcast", "Idle");
        Hide(hero, "Spellbook", "1H_Wand");

        var w = Spawn("Skeletons/Characters/Skeleton_Warrior.fbx", new Vector3(4.7f, 0, -2.7f), -70, skelMat);
        Pose(w, "Skeletons/Characters/Skeleton_Warrior.fbx", 0.35f, "2H_Melee_Attack_Chop", "1H_Melee_Attack_Chop", "Attack", "Idle");
        var m1 = Spawn("Skeletons/Characters/Skeleton_Minion.fbx", new Vector3(2.6f, 0, 2.2f), -130, skelMat);
        Pose(m1, "Skeletons/Characters/Skeleton_Minion.fbx", 0.3f, "Running_A", "Run", "Idle");
        var m2 = Spawn("Skeletons/Characters/Skeleton_Minion.fbx", new Vector3(-4.2f, 0, -3.2f), 60, skelMat);
        Pose(m2, "Skeletons/Characters/Skeleton_Minion.fbx", 0.7f, "Running_A", "Run", "Idle");
        var mg = Spawn("Skeletons/Characters/Skeleton_Mage.fbx", new Vector3(-5.2f, 0, 2.6f), 125, skelMat);
        Pose(mg, "Skeletons/Characters/Skeleton_Mage.fbx", 0.5f, "Spellcast_Raise", "Spellcast", "Idle");
    }

    static void Elements()
    {
        // heroi lancando Fogo no guerreiro (direcao do olhar: yaw 110)
        var dir = Quaternion.Euler(0, 110, 0) * Vector3.forward;
        var hand = new Vector3(0, 1.25f, -1) + dir * 1.1f;
        Fx("FireCast", hand, new Vector3(0, 110, 0), new Color(1f, 0.3f, 0.05f), 260, 0.5f, 8f, 1.7f,
            ParticleSystemShapeType.Cone, 0.12f, 13, -0.2f);
        Fx("FireCastCore", hand, new Vector3(0, 110, 0), new Color(1f, 0.6f, 0.15f), 120, 0.32f, 8f, 0.8f,
            ParticleSystemShapeType.Cone, 0.05f, 6, 0f);
        Point("FireCastLight", hand + dir * 1.8f, Fire, 4.5f, 6.5f);
        Prim(PrimitiveType.Sphere, "FireCore", hand, Vector3.zero, Vector3.one * 0.32f, Glow("Fire", Fire, 1.7f));

        // telegrafia do ataque do guerreiro (disco no chao)
        Prim(PrimitiveType.Cylinder, "Telegraph", new Vector3(0, 0.02f, -1) + dir * 3.4f, Vector3.zero,
            new Vector3(2.4f, 0.01f, 2.4f), Glow("Telegraph", new Color(0.9f, 0.08f, 0.08f), 0.7f));

        // marca de Agua sobre um inimigo (leitura no mundo, sem HUD)
        var mk = new Vector3(2.6f, 2.65f, 2.2f);
        Prim(PrimitiveType.Sphere, "Mark_Water", mk, Vector3.zero, Vector3.one * 0.3f, Glow("Water", Water, 1.5f));
        Point("MarkLight", mk + Vector3.down * 0.3f, Water, 1.8f, 3.5f);
        Fx("MarkDrip", mk, new Vector3(90, 0, 0), Water, 16, 0.7f, 1.2f, 0.18f,
            ParticleSystemShapeType.Sphere, 0.15f, 0, 0.6f);

        // os quatro santuarios, um por elemento
        Shrine("Fire", Fire, new Vector3(-6, 0, 5.4f), -0.2f);
        Shrine("Water", Water, new Vector3(-2, 0, 5.8f), 0.5f);
        Shrine("Earth", Earth, new Vector3(2, 0, 5.8f), 0.9f);
        Shrine("Air", Air, new Vector3(6, 0, 5.4f), -0.4f);
    }

    static void Shrine(string name, Color c, Vector3 pos, float gravity)
    {
        Prim(PrimitiveType.Cylinder, "Shrine_" + name, pos + Vector3.up * 0.5f, Vector3.zero, new Vector3(0.8f, 0.5f, 0.8f),
            Mat("Stone", null, new Color(0.33f, 0.34f, 0.42f), Color.black, 0.015f));
        Prim(PrimitiveType.Sphere, "Orb_" + name, pos + Vector3.up * 1.6f, Vector3.zero, Vector3.one * 0.5f, Glow(name, c, 1.5f));
        Point("ShrineLight_" + name, pos + Vector3.up * 1.7f + Vector3.back * 0.6f, c, 3.2f, 5.5f);
        Fx("ShrineFx_" + name, pos + Vector3.up * 1.6f, new Vector3(-90, 0, 0), c, 26, 1.3f, 0.9f, 0.26f,
            ParticleSystemShapeType.Sphere, 0.3f, 0, gravity * 0.3f);
    }

    public static Camera MakeCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.SetParent(root, false);
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 30;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.025f, 0.06f);
        cam.allowHDR = true;
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        Aim(cam, new Vector3(0, 0, -0.5f), 27f);
        return cam;
    }

    static void Aim(Camera cam, Vector3 target, float distance)
    {
        var rot = Quaternion.Euler(41, 0, 0);
        cam.transform.SetPositionAndRotation(target - rot * Vector3.forward * distance, rot);
    }

    static void PostVolume()
    {
        const string path = Out + "LookDevProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var bloom = Add<Bloom>(profile);
            bloom.intensity.Override(1.0f); bloom.threshold.Override(1.0f); bloom.scatter.Override(0.7f);
            var vig = Add<Vignette>(profile);
            vig.intensity.Override(0.3f); vig.smoothness.Override(0.5f);
            var ca = Add<ColorAdjustments>(profile);
            ca.postExposure.Override(0f); ca.contrast.Override(10f); ca.saturation.Override(10f);
            var tm = Add<Tonemapping>(profile);
            tm.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);
        }
        var go = new GameObject("PostFX");
        go.transform.SetParent(root, false);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.sharedProfile = profile;
    }

    static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var c = profile.Add<T>(true);
        AssetDatabase.AddObjectToAsset(c, profile);
        return c;
    }

    static void Capture(Camera cam, string path, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
    }
}
