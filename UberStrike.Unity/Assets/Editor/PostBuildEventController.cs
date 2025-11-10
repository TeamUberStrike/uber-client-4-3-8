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
        Assembly assembly = Assembly.GetAssembly(typeof(UnityEditorInternal.Macros));
        Type type = assembly.GetType("UnityEditorInternal.LogEntries");
        MethodInfo method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
}