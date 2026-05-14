using System;
using UnityEditor;
using UnityEngine;

// Batch-mode entry point for the Android (IL2CPP) build.
// Reads SDK/NDK/JDK locations from environment variables so it carries no
// machine-specific paths. Invoked via:
//   Unity.exe -batchmode -quit -projectPath <proj> -buildTarget Android
//             -executeMethod LocalAndroidBuild.Run -logFile <log>
public static class LocalAndroidBuild
{
    public static void Run()
    {
        string sdk = Environment.GetEnvironmentVariable("UBERSTRIKE_ANDROID_SDK");
        string ndk = Environment.GetEnvironmentVariable("UBERSTRIKE_ANDROID_NDK");
        string jdk = Environment.GetEnvironmentVariable("UBERSTRIKE_ANDROID_JDK");

        if (!string.IsNullOrEmpty(sdk))
            UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdk;
        if (!string.IsNullOrEmpty(ndk))
            UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = ndk;
        if (!string.IsNullOrEmpty(jdk))
            UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = jdk;

        UnityEngine.Debug.LogFormat("[LocalAndroidBuild] SDK={0}", sdk);
        UnityEngine.Debug.LogFormat("[LocalAndroidBuild] NDK={0}", ndk);
        UnityEngine.Debug.LogFormat("[LocalAndroidBuild] JDK={0}", jdk);
        UnityEngine.Debug.LogFormat("[LocalAndroidBuild] applicationIdentifier={0}",
            PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
        UnityEngine.Debug.LogFormat("[LocalAndroidBuild] scriptingBackend={0} architectures={1}",
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android),
            PlayerSettings.Android.targetArchitectures);

        SceneExporter.BuildAndroidDev();

        UnityEngine.Debug.Log("[LocalAndroidBuild] BuildAndroidDev returned.");
    }
}
