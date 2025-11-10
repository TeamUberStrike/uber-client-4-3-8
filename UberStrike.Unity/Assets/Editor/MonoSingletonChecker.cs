using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;

public static class MonoSingletonChecker
{
    [MenuItem("Cmune Tools/Post Build Events/Check All MonoSingletons")]
    public static void CheckAllMonoSingletons()
    {
        try
        {
            if (EditorApplication.currentScene.EndsWith("Latest.unity"))
            {
                // Get all implementations of MonoSingeton by reflection
                Assembly assembly = Assembly.LoadFile(Application.dataPath + "/../Library/ScriptAssemblies/Assembly-CSharp.dll");
                Type[] monoSingletons = Array.FindAll<Type>(assembly.GetTypes(), t => IsSubclassOfRawGeneric(typeof(MonoSingleton<>), t.BaseType));

                // Check if all of them are attached somewhere in the project hierarchy (only once)
                foreach (var a in monoSingletons)
                {
                    UnityEngine.Object[] components = UnityEngine.Object.FindObjectsOfTypeIncludingAssets(a);
                    if (components.Length == 0)
                    {
                        if (!a.IsAbstract)
                        {
                            Debug.LogWarning("The MonoSingleton<" + a + "> is NOT attached to any GameObject in the scene!");
                        }
                    }
                    else if (components.Length > 1 && Array.FindAll(components, c => c.hideFlags == 0).Length != 1)
                    {
                        Debug.LogError("The MonoSingleton<" + a + "> is attached to multiple GameObjects in the scene!");
                        foreach (MonoBehaviour b in components)
                        {
                            Debug.Log(b.hideFlags + " " + b.GetInstanceID() + " " + b.gameObject);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("CheckAllMonoSingletons skipped because not in Latest scene: " + EditorApplication.currentScene);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("The MonoSingletonChecker failed with a " + e.GetType() + ":" + e.Message);
        }
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type type)
    {
        if (type != null)
        {
            var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (generic == cur)
            {
                return true;
            }
        }
        return false;
    }
}
