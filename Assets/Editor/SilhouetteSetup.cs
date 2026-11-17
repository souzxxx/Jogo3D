using System.Linq;
using Quintessence.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Silhueta do heroi e dos inimigos atras do cenario: cria as camadas Hero e Enemy, os dois materiais
/// e as quatro passadas RenderObjects nos renderers (PC e Mobile). Pode rodar de novo: troca as passadas antigas.
/// `unity run . -- -executeMethod SilhouetteSetup.Run` (tambem roda no SceneRebuild.All)
public static class SilhouetteSetup
{
    public const int HeroLayer = 6, EnemyLayer = 7;
    const string ShaderPath = "Assets/Game/Feel/Shaders/Silhouette.shader";
    const string Prefix = "Silhueta";
    static readonly string[] Renderers = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };

    [MenuItem("Quintessence/Silhueta atras do cenario")]
    public static void Run()
    {
        EnsureLayer(HeroLayer, Silhouette.HeroLayer);
        EnsureLayer(EnemyLayer, Silhouette.EnemyLayer);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        // heroi: pergaminho claro, o mesmo tom dos textos do HUD. Bit 2 do stencil; pinta onde o heroi nao esta a vista.
        var hero = Material("Assets/Game/Feel/Materials/SilhouetteHero.mat", shader,
            new Color(0.80f, 0.72f, 0.56f, 0.3f), new Color(0.98f, 0.92f, 0.78f, 0.75f), 2, 2, 2, CompareFunction.NotEqual);
        // inimigos: tijolo apagado. Bit 1; so pinta onde nada esta marcado (nunca por cima do heroi ou da silhueta dele).
        var enemy = Material("Assets/Game/Feel/Materials/SilhouetteEnemy.mat", shader,
            new Color(0.55f, 0.24f, 0.2f, 0.3f), new Color(0.82f, 0.42f, 0.34f, 0.7f), 1, 3, 1, CompareFunction.Greater);
        foreach (var path in Renderers)
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (data == null) { Debug.LogWarning("sem renderer em " + path); continue; }
            foreach (var old in data.rendererFeatures.Where(f => f == null || f.name.StartsWith(Prefix)).ToList())
            {
                data.rendererFeatures.Remove(old);
                if (old != null) Object.DestroyImmediate(old, true);
            }
            // ordem importa: as duas mascaras antes das duas silhuetas, e a do heroi antes da dos inimigos
            Add(data, "Silhueta: mascara do heroi", HeroLayer, hero, 0);
            Add(data, "Silhueta: mascara dos inimigos", EnemyLayer, enemy, 0);
            Add(data, "Silhueta: heroi", HeroLayer, hero, 1);
            Add(data, "Silhueta: inimigos", EnemyLayer, enemy, 1);
            typeof(ScriptableRendererData).GetMethod("ValidateRendererFeatures", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(data, null);
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Silhueta: camadas, materiais e passadas prontos");
    }

    static void Add(ScriptableRendererData data, string name, int layer, Material m, int pass)
    {
        var f = ScriptableObject.CreateInstance<RenderObjects>();
        f.name = name;
        f.settings.passTag = name;
        f.settings.Event = RenderPassEvent.AfterRenderingOpaques;
        f.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
        f.settings.filterSettings.LayerMask = 1 << layer;
        f.settings.overrideMode = RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Material;
        f.settings.overrideMaterial = m;
        f.settings.overrideMaterialPassIndex = pass;
        f.settings.overrideDepthState = false; // o ZTest vem do shader (LEqual na mascara, Greater na silhueta)
        f.SetActive(true);
        AssetDatabase.AddObjectToAsset(f, data);
        data.rendererFeatures.Add(f);
    }

    static Material Material(string path, Shader shader, Color fill, Color rim, int reference, int read, int write, CompareFunction comp)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        m.SetColor("_Fill", fill);
        m.SetColor("_Rim", rim);
        m.SetFloat("_RimPower", 3f);
        m.SetFloat("_Ref", reference);
        m.SetFloat("_ReadMask", read);
        m.SetFloat("_WriteMask", write);
        m.SetFloat("_Comp", (float)comp);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureLayer(int index, string name)
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        var slot = layers.GetArrayElementAtIndex(index);
        if (slot.stringValue == name) return;
        if (!string.IsNullOrEmpty(slot.stringValue)) Debug.LogWarning($"camada {index} era '{slot.stringValue}', agora '{name}'");
        slot.stringValue = name;
        tags.ApplyModifiedProperties();
    }
}
