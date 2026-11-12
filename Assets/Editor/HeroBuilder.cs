using System.Linq;
using UnityEditor;
using UnityEngine;

/// Monta os quatro herois do Quintessence sobre os corpos do pack: cabeca, cauda e armas proprias presas aos ossos,
/// textura do corpo recolorida. As animacoes sao as do pack (mesmo esqueleto nos quatro).
public static class HeroBuilder
{
    const string Art = "Assets/Art/Heroes/";

    public struct Spec
    {
        public string Name, Body;
        public (string file, string bone)[] Parts;
        public string[] Hide;
    }

    public static readonly Spec[] Specs =
    {
        new()
        {
            Name = "Rax", Body = "Rogue",
            Parts = new[] { ("rax_head", "head"), ("rax_tail", "hips"), ("rax_weapon_l", "handslot.l"), ("rax_weapon_r", "handslot.r") },
            Hide = new[] { "Rogue_Head", "Rogue_Cape", "Knife", "Knife_Offhand", "1H_Crossbow", "2H_Crossbow", "Throwable" },
        },
        new()
        {
            Name = "Liora", Body = "Mage",
            Parts = new[] { ("liora_head", "head"), ("liora_tail", "hips"), ("liora_weapon", "handslot.r") },
            Hide = new[] { "Mage_Head", "Mage_Hat", "Mage_Cape", "1H_Wand", "2H_Staff", "Spellbook", "Spellbook_open" },
        },
        new()
        {
            Name = "Brann", Body = "Barbarian",
            Parts = new[] { ("brann_head", "head"), ("brann_tail", "hips"), ("brann_weapon", "handslot.r") },
            Hide = new[] { "Barbarian_Head", "Barbarian_Hat", "Barbarian_Cape", "1H_Axe", "1H_Axe_Offhand", "2H_Axe", "Barbarian_Round_Shield", "Mug" },
        },
        new()
        {
            Name = "Vey", Body = "RogueHooded",
            Parts = new[] { ("vey_head", "head"), ("vey_tail", "hips"), ("vey_weapon_l", "handslot.l"), ("vey_weapon_r", "handslot.r") },
            Hide = new[] { "Rogue_LegLeft", "Rogue_LegRight", "Knife", "Knife_Offhand", "1H_Crossbow", "2H_Crossbow", "Throwable" },
        },
    };

    /// As pecas saem do Blender com material proprio; aqui viram foscas como o resto do jogo.
    static void MatteParts()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Heroes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                // o pelo escuro do Rax sumia na sala escura (olhos pretos sobre pelo quase preto): clareia para um azul-ardosia
                if (path.Contains("/Rax/") && m.name == "Fur" && m.HasProperty("_BaseColor"))
                    m.SetColor("_BaseColor", new Color(0.3f, 0.33f, 0.46f));
            }
        }
    }

    /// Modelo do heroi index (0 Rax, 1 Liora, 2 Brann, 3 Vey), em pe na origem, sem pai.
    public static GameObject Build(int index, RuntimeAnimatorController ctrl)
    {
        var s = Specs[index];
        string tex = Art + s.Name + "/" + s.Name.ToLowerInvariant() + "_body_texture.png";
        var mat = LookDevBuilder.Mat("Hero_" + s.Name, tex, Color.white, Color.black, 0f);
        var model = LookDevBuilder.Spawn("Adventurers/Characters/" + s.Body + ".fbx", Vector3.zero, 0, mat);
        model.name = "Model_" + s.Name;
        model.transform.SetParent(null, false);
        model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var found = new System.Text.StringBuilder();
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            found.Append(r.name).Append(' ');
            if (s.Hide.Contains(r.name)) r.gameObject.SetActive(false);
        }
        foreach (var (file, bone) in s.Parts)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Art + s.Name + "/" + file + ".fbx");
            if (asset == null) { Debug.LogWarning("heroi " + s.Name + ": falta a peca " + file); continue; }
            Transform target = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == bone) target = t;
            if (target == null) { Debug.LogWarning("heroi " + s.Name + ": osso nao encontrado: " + bone); continue; }
            var part = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            part.name = file;
            // a peca foi modelada no espaco do corpo em repouso: entra na raiz e so depois vai para o osso
            part.transform.SetParent(model.transform, false);
            part.transform.SetParent(target, true);
            var rs = part.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                found.Append($"| {file}: centro {b.center} tamanho {b.size} ");
            }
        }
        Debug.Log($"heroi {s.Name} ({s.Body}): {found}");
        if (ctrl != null) CombatDevSceneBuilder.Animator(model, ctrl);
        return model;
    }

    public static void Prepare()
    {
        AssetDatabase.Refresh();
        MatteParts();
    }
}
