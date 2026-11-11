using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Build de entrega. WebGL para o itch.io: `unity build --target WebGL --execute-method BuildTools.BuildWebGL -o Builds/WebGL`.
public static class BuildTools
{
    const string Output = "Builds/WebGL";

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
