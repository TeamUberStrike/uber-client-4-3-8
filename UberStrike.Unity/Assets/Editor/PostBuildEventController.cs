using System;
using System.Reflection;
using UnityEditor;

class PostBuildEventController : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        CreateVisualStudioProject.UpdateProject(false);
    }

    public static void ClearConsole()
    {
        // Use the modern way to access LogEntries
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
}