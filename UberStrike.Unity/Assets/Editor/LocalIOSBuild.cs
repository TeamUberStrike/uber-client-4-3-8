using UnityEditor;
using UnityEngine;

// Batch-mode entry point for the iOS (IL2CPP) build.
// Produces an Xcode project under UberStrike.Unity/iOS/ which must be
// compiled on macOS with Xcode to produce the final .ipa.
//
// Usage:
//   Unity.exe -batchmode -quit -nographics \
//     -projectPath "C:\path\to\UberStrike.Unity" \
//     -buildTarget iOS \
//     -executeMethod LocalIOSBuild.Run \
//     -logFile ios_build.log
public static class LocalIOSBuild
{
    public static void Run()
    {
        Debug.LogFormat("[LocalIOSBuild] applicationIdentifier={0}",
            PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS));
        Debug.LogFormat("[LocalIOSBuild] scriptingBackend={0}",
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS));
        Debug.LogFormat("[LocalIOSBuild] targetOSVersion={0}",
            PlayerSettings.iOS.targetOSVersionString);

        SceneExporter.BuildiOS();

        Debug.Log("[LocalIOSBuild] BuildiOS returned — Xcode project written to iOS/");
    }
}
