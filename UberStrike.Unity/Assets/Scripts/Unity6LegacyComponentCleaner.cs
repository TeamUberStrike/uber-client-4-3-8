using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
#endif

/// <summary>
/// Unity 6 Legacy Component Cleaner
/// Removes deprecated particle system and GUI components from scenes
/// </summary>
public class Unity6LegacyComponentCleaner : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Unity6/Clean Legacy Components from Current Scene")]
    public static void CleanCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        CleanLegacyComponentsFromScene(currentScene);
    }
    
    [MenuItem("Tools/Unity6/Clean Legacy Components from Latest.unity")]
    public static void CleanLatestScene()
    {
        string scenePath = "Assets/Scenes/Latest.unity";
        
        // Open the scene
        Scene originalScene = SceneManager.GetActiveScene();
        Scene latestScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        if (latestScene.IsValid())
        {
            CleanLegacyComponentsFromScene(latestScene);
            
            // Save the scene
            EditorSceneManager.SaveScene(latestScene);
            Debug.Log("Latest.unity scene cleaned and saved!");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Could not open Latest.unity scene", "OK");
        }
    }
    
    private static void CleanLegacyComponentsFromScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            Debug.LogError("Invalid scene provided for cleaning");
            return;
        }
        
        List<string> removedComponents = new List<string>();
        int totalRemoved = 0;
        
        // Get all GameObjects in the scene
        GameObject[] rootObjects = scene.GetRootGameObjects();
        
        foreach (GameObject rootObj in rootObjects)
        {
            totalRemoved += CleanLegacyComponentsFromGameObject(rootObj, removedComponents);
        }
        
        // Log results
        Debug.Log($"Unity 6 Legacy Component Cleanup Complete!");
        Debug.Log($"Total components removed: {totalRemoved}");
        Debug.Log($"Scene: {scene.name}");
        
        if (removedComponents.Count > 0)
        {
            Debug.Log("Removed component types:");
            HashSet<string> uniqueTypes = new HashSet<string>(removedComponents);
            foreach (string componentType in uniqueTypes)
            {
                int count = 0;
                foreach (string removed in removedComponents)
                {
                    if (removed == componentType) count++;
                }
                Debug.Log($"  - {componentType}: {count} instances");
            }
        }
        
        EditorUtility.DisplayDialog("Unity 6 Legacy Cleanup", 
            $"Successfully removed {totalRemoved} legacy components from {scene.name}!\n\nCheck Console for details.", 
            "OK");
    }
    
    private static int CleanLegacyComponentsFromGameObject(GameObject obj, List<string> removedComponents)
    {
        int removedCount = 0;
        
        // Legacy Particle System Components (Unity 3.x/4.x era)
        // Since these types no longer exist in Unity 6, we use string-based approach
        removedCount += RemoveLegacyComponentByName(obj, "ParticleAnimator", removedComponents);
        removedCount += RemoveLegacyComponentByName(obj, "ParticleRenderer", removedComponents);
        removedCount += RemoveLegacyComponentByName(obj, "EllipsoidParticleEmitter", removedComponents);
        
        // Legacy GUI Components
        removedCount += RemoveGUILayerComponent(obj, removedComponents);
        
        // Check children recursively
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            removedCount += CleanLegacyComponentsFromGameObject(obj.transform.GetChild(i).gameObject, removedComponents);
        }
        
        return removedCount;
    }
    
    private static int RemoveComponentIfExists<T>(GameObject obj, List<string> removedComponents) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component != null)
        {
            Debug.Log("Removing " + typeof(T).Name + " from " + obj.name);
            DestroyImmediate(component);
            removedComponents.Add(typeof(T).Name);
            return 1;
        }
        return 0;
    }
    
    private static int RemoveLegacyComponentByName(GameObject obj, string componentName, List<string> removedComponents)
    {
        Component[] components = obj.GetComponents<Component>();
        int removed = 0;
        
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (components[i] == null) continue;
            
            string typeName = components[i].GetType().Name;
            if (typeName == componentName)
            {
                Debug.Log("Removing " + componentName + " from " + obj.name);
                DestroyImmediate(components[i]);
                removedComponents.Add(componentName);
                removed++;
            }
        }
        
        return removed;
    }
    
    private static int RemoveGUILayerComponent(GameObject obj, List<string> removedComponents)
    {
        // GUILayer was removed in Unity 2019.3+, but we can check for it
        Component[] components = obj.GetComponents<Component>();
        int removed = 0;
        
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (components[i] == null) continue;
            
            string typeName = components[i].GetType().Name;
            if (typeName == "GUILayer")
            {
                Debug.Log("Removing GUILayer from " + obj.name);
                DestroyImmediate(components[i]);
                removedComponents.Add("GUILayer");
                removed++;
            }
        }
        
        return removed;
    }
    
    [MenuItem("Tools/Unity6/Remove Legacy Particle Emitters")]
    public static void RemoveLegacyParticleEmitters()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int removed = 0;
        List<string> cleanedObjects = new List<string>();
        
        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();
            
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] == null) continue;
                
                string typeName = components[i].GetType().Name;
                
                // Remove legacy particle components
                if (typeName.Contains("ParticleEmitter") || 
                    typeName == "EllipsoidParticleEmitter" || 
                    typeName == "MeshParticleEmitter")
                {
                    Debug.Log($"Removing {typeName} from {obj.name}");
                    DestroyImmediate(components[i]);
                    if (!cleanedObjects.Contains(obj.name))
                    {
                        cleanedObjects.Add(obj.name);
                    }
                    removed++;
                }
            }
        }
        
        Debug.Log($"Removed {removed} legacy particle emitter components from {cleanedObjects.Count} objects");
        EditorUtility.DisplayDialog("Legacy Cleanup", 
            $"Removed {removed} legacy particle emitters from {cleanedObjects.Count} objects", 
            "OK");
    }
    
    [MenuItem("Tools/Unity6/Fix Missing Script References")]
    public static void FixMissingScriptReferences()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int fixedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();
            
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] == null)
                {
                    Debug.Log("Removing missing script reference from " + obj.name);
                    // Remove null/missing components
                    SerializedObject serializedObject = new SerializedObject(obj);
                    SerializedProperty componentArray = serializedObject.FindProperty("m_Component");
                    
                    for (int j = componentArray.arraySize - 1; j >= 0; j--)
                    {
                        SerializedProperty componentProperty = componentArray.GetArrayElementAtIndex(j);
                        SerializedProperty componentRef = componentProperty.FindPropertyRelative("component");
                        
                        if (componentRef.objectReferenceValue == null)
                        {
                            componentArray.DeleteArrayElementAtIndex(j);
                            fixedCount++;
                        }
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        
        Debug.Log("Fixed " + fixedCount + " missing script references");
        EditorUtility.DisplayDialog("Missing Scripts", 
            "Fixed " + fixedCount + " missing script references", 
            "OK");
    }
    
    [MenuItem("Tools/Unity6/Full Scene Cleanup (Latest.unity)")]
    public static void FullSceneCleanup()
    {
        if (EditorUtility.DisplayDialog("Full Scene Cleanup", 
            "This will:\n1. Clean legacy components\n2. Remove particle emitters\n3. Fix missing scripts\n4. Save the scene\n\nContinue?", 
            "Yes", "Cancel"))
        {
            CleanLatestScene();
            RemoveLegacyParticleEmitters();
            FixMissingScriptReferences();
            
            // Save scene
            EditorSceneManager.SaveOpenScenes();
            
            Debug.Log("Full Unity 6 scene cleanup completed!");
            EditorUtility.DisplayDialog("Cleanup Complete", 
                "Latest.unity has been fully cleaned for Unity 6 compatibility!", 
                "OK");
        }
    }
#endif
}