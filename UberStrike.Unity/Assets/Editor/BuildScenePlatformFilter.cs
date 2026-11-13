using System.Linq;
using UnityEditor;
using UnityEditor.Build;

[InitializeOnLoad]
public static class BuildScenePlatformFilter
{
    private const string MobileSplashScene = "Assets/Scenes/SplashMobile.unity";

    static BuildScenePlatformFilter()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(Build);
    }

    private static void Build(BuildPlayerOptions options)
    {
        options.scenes = FilterScenes(options.target, options.scenes);
        BuildPipeline.BuildPlayer(options);
    }

    public static string[] FilterScenes(BuildTarget target, string[] scenes)
    {
        if (target == BuildTarget.Android || target == BuildTarget.iOS)
            return scenes;

        return scenes
            .Where(path => path != MobileSplashScene)
            .ToArray();
    }
}
