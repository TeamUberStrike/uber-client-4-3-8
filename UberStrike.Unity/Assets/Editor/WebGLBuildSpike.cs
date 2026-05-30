using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Phase 0 WebGL build spike (2026-05-30). Minimal scene set to surface
// IL2CPP/plugin link errors and prove boot-to-menu. Not for production.
// Invoke headless: Unity.exe -batchmode -quit -projectPath <proj>
//   -buildTarget WebGL -executeMethod WebGLBuildSpike.Build -logFile <log>
public static class WebGLBuildSpike
{
    [MenuItem("File/Build UberStrike/Internal Dev/WebGL (Spike)")]
    public static void Build()
    {
        // Fewest scenes that still reach the lobby menu (mirrors the Android
        // build's default: Latest host scene + Spaceship lobby, no maps).
        string[] scenes =
        {
            "Assets/Scenes/Latest.unity",
            "Assets/Scenes/LevelSpaceship.unity",
        };

        // Phase 0 player settings.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.dataCaching = true;
        // Bake a JS decompressor into the loader so .gz artifacts load from any
        // static host (e.g. `python -m http.server`) that doesn't send
        // Content-Encoding: gzip. Costs a little startup time; fine for the spike.
        PlayerSettings.WebGL.decompressionFallback = true;

        const string outDir = "Build/WebGL-Spike";
        System.IO.Directory.CreateDirectory(outDir);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Debug.LogFormat("[WebGLSpike] result={0} errors={1} warnings={2} totalSize={3} time={4}",
            s.result, s.totalErrors, s.totalWarnings, s.totalSize, s.totalTime);

        if (s.result != BuildResult.Succeeded)
        {
            Debug.LogErrorFormat("[WebGLSpike] BUILD FAILED: {0}", s.result);
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage m in step.messages)
                {
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        Debug.LogErrorFormat("[WebGLSpike] {0}: {1}", step.name, m.content);
                }
            }
        }
    }
}
