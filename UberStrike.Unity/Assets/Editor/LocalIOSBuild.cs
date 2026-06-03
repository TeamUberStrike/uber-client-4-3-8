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
//
// IMPORTANT (platform-dependent, one codebase):
//   The iOS-specific PlayerSettings (IL2CPP backend, bundle id, target device,
//   min OS version, HTTP policy) are applied HERE at build time against the iOS
//   build-target group only. This keeps ProjectSettings.asset identical to main's
//   desktop config — the iOS settings never touch the Standalone/PC values, so the
//   merge introduces zero PC regression. Adjust the constants below if HaZard wants
//   different iOS identity; signing itself is configured on the Mac in Xcode.
public static class LocalIOSBuild
{
    // iOS bundle id used by the proven mobile-il2cpp build pipeline (v3 Xcode export).
    const string IOSBundleIdentifier = "com.yoxa.uberstrike";
    // Minimum iOS version to target. Conservative default; raise if Unity 2022 requires it.
    const string IOSMinimumOSVersion = "12.0";

    public static void Run()
    {
        ConfigureIOSPlayerSettings();

        Debug.LogFormat("[LocalIOSBuild] applicationIdentifier={0}",
            PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS));
        Debug.LogFormat("[LocalIOSBuild] scriptingBackend={0}",
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS));
        Debug.LogFormat("[LocalIOSBuild] targetDevice={0}", PlayerSettings.iOS.targetDevice);
        Debug.LogFormat("[LocalIOSBuild] targetOSVersion={0}",
            PlayerSettings.iOS.targetOSVersionString);

        SceneExporter.BuildiOS();

        Debug.Log("[LocalIOSBuild] BuildiOS returned — Xcode project written to iOS/");
    }

    // Applies only the iOS-build-target-group settings required for a valid IL2CPP
    // Xcode export. Standalone/PC PlayerSettings are never read or written here.
    static void ConfigureIOSPlayerSettings()
    {
        // iOS must be IL2CPP — Mono is not a supported scripting backend on the platform.
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.iOS, Il2CppCompilerConfiguration.Release);

        // Per-platform bundle id (does not change the desktop applicationIdentifier).
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, IOSBundleIdentifier);

        // Build for both iPhone and iPad (Universal) and target the device SDK (not simulator).
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetOSVersionString = IOSMinimumOSVersion;

        // The UberForever content CDN and some internal calls are plain HTTP; allow it
        // (IOSPostBuildProcessor also sets NSAllowsArbitraryLoads in Info.plist).
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

        Debug.Log("[LocalIOSBuild] iOS PlayerSettings configured (IL2CPP, Universal, "
            + IOSBundleIdentifier + ", min iOS " + IOSMinimumOSVersion + ").");
    }
}
