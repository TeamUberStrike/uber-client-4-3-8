using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Command-line iOS export used by build-ios.sh:
///   Unity -batchmode -quit -projectPath ... -buildTarget iOS -executeMethod IosCommandLineBuilder.Build
///
/// Builds exactly the scene list from Editor Build Settings (same as the
/// Build Settings UI) and exports the Xcode project into
/// UberStrike.Unity/BuildIos. The project is regenerated fresh — signing,
/// device family and ATS all come from PlayerSettings / the
/// IosBuildPostProcessor post-process step, not from manual pbxproj edits.
/// </summary>
public static class IosCommandLineBuilder
{
    public static void Build()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName; // .../UberStrike.Unity
        string outputPath = Path.Combine(projectRoot, "BuildIos");

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("IosCommandLineBuilder failed: " + report.summary.result +
                " (" + report.summary.totalErrors + " errors)");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("IosCommandLineBuilder: Xcode project exported to " + outputPath);
    }
}
