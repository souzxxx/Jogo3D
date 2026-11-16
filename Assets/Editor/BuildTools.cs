using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Builds de entrega. WebGL para o itch.io: `unity build --target WebGL --execute-method BuildTools.BuildWebGL -o Builds/WebGL`.
public static class BuildTools
{
    const string Output = "Builds/WebGL";

    /// Build de Windows (obrigatoria na entrega): `unity build --target StandaloneWindows64 --execute-method BuildTools.BuildWindows`.
    /// Sai em Builds/Windows/Quintessence.exe; para o itch.io, zipar a pasta inteira (o .exe sozinho nao roda).
    [MenuItem("Quintessence/Build Windows")]
    public static void BuildWindows()
    {
        PlayerSettings.productName = "Quintessence";
        PlayerSettings.companyName = "Insper";
        PlayerSettings.SplashScreen.show = false;
        // abre em tela cheia na resolucao do monitor; Alt+Enter e a opcao de tela cheia do jogo alternam para janela
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Quintessence.exe");
    }

    static void Build(BuildTarget target, string output)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) { Debug.LogError("BUILD: nenhuma cena no Build Settings"); EditorApplication.Exit(1); return; }
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output, target = target, options = BuildOptions.None });
        var s = report.summary;
        Debug.Log($"BUILD: {target} {s.result} · {s.totalSize / (1024f * 1024f):0.0} MB · {s.totalTime.TotalMinutes:0.0} min · erros {s.totalErrors} · avisos {s.totalWarnings}");
        if (s.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
    }

    [MenuItem("Quintessence/Build WebGL (itch.io)")]
    public static void BuildWebGL()
    {
        PlayerSettings.productName = "Quintessence";
        PlayerSettings.companyName = "Insper";
        // Gzip com fallback: funciona no itch.io sem depender de cabecalhos do servidor
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        // pagina de carregamento propria (fundo do titulo, barra nas cores do jogo, sem a marca da Unity)
        PlayerSettings.WebGL.template = "PROJECT:Quintessence";
        // sem a tela "Made with Unity" na abertura (opcional no Unity 6): o jogo abre direto no titulo
        PlayerSettings.SplashScreen.show = false;

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) { Debug.LogError("BUILD: nenhuma cena no Build Settings"); EditorApplication.Exit(1); return; }

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes, locationPathName = Output, target = BuildTarget.WebGL, options = BuildOptions.None,
        });
        var s = report.summary;
        Debug.Log($"BUILD: {s.result} · {s.totalSize / (1024f * 1024f):0.0} MB · {s.totalTime.TotalMinutes:0.0} min · erros {s.totalErrors} · avisos {s.totalWarnings} · cenas {string.Join(", ", scenes)}");
        if (s.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
    }
}
