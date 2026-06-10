using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// WebGL build for the Photon-over-WebSocket path (uberstrike-webgl-photon RUNBOOK Stage 3).
//
// Two entry points:
//   * Build()           — production-lean profile (gzip, explicit-throw exceptions).
//   * BuildDiagnostic() — names the IL2CPP "RuntimeError: null function" match-entry crash
//                         (FullWithStacktrace + Low stripping). Use this first; once a clean
//                         match runs, switch back to Build().
//
// Headless:
//   Unity.exe -batchmode -quit -projectPath <proj> -buildTarget WebGL
//     -executeMethod WebGLBuildSpike.Build          -logFile <log>
//     -executeMethod WebGLBuildSpike.BuildDiagnostic -logFile <log>
//
// IL2CPP stripping is held back by Assets/link.xml + Assets/Scripts/AOT/PhotonAotHints.cs
// (committed on this branch). Those preserve Photon/SDK reflection + the runtime
// Dictionary<K,V> generic instantiations the deserializer builds — the same protection the
// iOS build needed (exp/mobile-shader-merge commit 6168640d). They apply to WebGL because
// the linker rules are platform-independent; no per-target change needed.
public static class WebGLBuildSpike
{
    // Latest = the persistent host scene (build scene 0); Spaceship = lobby.
    // Then the 6 mobile-supported maps: LevelManager._mobileSupportedMaps = {3,4,5,7,8,10}.
    // WebGL reports the IPad channel, so the map loader takes the additive
    // LoadLevelAdditiveAsync(sceneName) path — it loads scenes FROM THE BUILD, not from an
    // AssetBundle host — so every offered map must be baked in or the player gets the
    // "dark map" (mobile-stripped/empty) fallback. Ids/names mirror SceneExporter.MapsToExport.
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Latest.unity",
        "Assets/Scenes/LevelSpaceship.unity",
        "Assets/Scenes/LevelTheWarehouse.unity",       // 3
        "Assets/Scenes/LevelTempleOfTheRaven.unity",   // 4
        "Assets/Scenes/LevelFortWinter.unity",         // 5
        "Assets/Scenes/LevelSkyGarden.unity",          // 7
        "Assets/Scenes/LevelCuberStrike.unity",        // 8
        "Assets/Scenes/LevelSpaceportAlpha.unity",     // 10
    };

    [MenuItem("File/Build UberStrike/Internal Dev/WebGL (Production-lean)")]
    public static void Build() => BuildCore(diagnostic: false);

    [MenuItem("File/Build UberStrike/Internal Dev/WebGL (Diagnostic — name the IL2CPP crash)")]
    public static void BuildDiagnostic() => BuildCore(diagnostic: true);

    private static void BuildCore(bool diagnostic)
    {
        // exceptionSupport: the diagnostic profile keeps full managed stack traces so the
        // match-entry "null function" surfaces with a NAME (feed it into link.xml). The lean
        // profile keeps only explicit throws to stay small/fast.
        PlayerSettings.WebGL.exceptionSupport = diagnostic
            ? WebGLExceptionSupport.FullWithStacktrace
            : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        // Managed stripping: drop to Low for diagnostics so a stripped symbol can't masquerade
        // as a different fault. Restored afterward so we don't churn ProjectSettings.
        ManagedStrippingLevel prevStrip = PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.WebGL);
        if (diagnostic)
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Low);

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.dataCaching = true;
        // Bake a JS decompressor into the loader so .gz artifacts load from any static host
        // (e.g. `python -m http.server`, Netlify) that doesn't send Content-Encoding: gzip.
        PlayerSettings.WebGL.decompressionFallback = true;

        string outDir = diagnostic ? "Build/WebGL-Diagnostic" : "Build/WebGL";
        System.IO.Directory.CreateDirectory(outDir);

        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };

        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary s = report.summary;
            Debug.LogFormat("[WebGL] profile={0} result={1} errors={2} warnings={3} maps={4} totalSize={5} time={6}",
                diagnostic ? "diagnostic" : "lean", s.result, s.totalErrors, s.totalWarnings,
                Scenes.Length - 2, s.totalSize, s.totalTime);

            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogErrorFormat("[WebGL] BUILD FAILED: {0}", s.result);
                foreach (BuildStep step in report.steps)
                    foreach (BuildStepMessage m in step.messages)
                        if (m.type == LogType.Error || m.type == LogType.Exception)
                            Debug.LogErrorFormat("[WebGL] {0}: {1}", step.name, m.content);
            }
        }
        finally
        {
            if (diagnostic)
                PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, prevStrip);
        }
    }
}
