using UnityEngine;
using System.Collections;

/// <summary>
/// Unity 6 Initialization Diagnostic Tool
/// Attach this to a GameObject in Latest.unity to debug why the menu isn't showing
/// </summary>
public class Unity6InitDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    public bool enableVerboseLogging = true;
    public bool checkEveryFrame = false;
    
    private bool hasRun = false;

    void Start()
    {
        Debug.Log("=== Unity 6 Init Diagnostic Started ===");
        StartCoroutine(RunDiagnostics());
    }

    void Update()
    {
        if (checkEveryFrame && !hasRun)
        {
            StartCoroutine(RunDiagnostics());
        }
    }

    IEnumerator RunDiagnostics()
    {
        if (hasRun) yield break;
        hasRun = true;

        yield return new WaitForEndOfFrame();
        
        Debug.Log("=== SCENE DIAGNOSTIC REPORT ===");
        
        // Check ApplicationDataManager
        CheckApplicationDataManager();
        
        // Check MenuPageManager
        CheckMenuPageManager();
        
        // Check Scene Objects
        CheckSceneObjects();
        
        // Check Cameras
        CheckCameras();
        
        // Check Canvas and UI
        CheckCanvasAndUI();
        
        Debug.Log("=== END DIAGNOSTIC REPORT ===");
    }

    void CheckApplicationDataManager()
    {
        Debug.Log("--- ApplicationDataManager Check ---");
        
        var appDataManager = FindObjectOfType<ApplicationDataManager>();
        if (appDataManager == null)
        {
            Debug.LogError("❌ ApplicationDataManager NOT FOUND in scene!");
            return;
        }
        
        Debug.Log("✅ ApplicationDataManager found: " + appDataManager.name);
        Debug.Log("- GameObject active: " + appDataManager.gameObject.activeInHierarchy);
        Debug.Log("- Component enabled: " + appDataManager.enabled);
        
        // Check if it has been initialized
        var initField = typeof(ApplicationDataManager).GetField("IsLoaded", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (initField != null)
        {
            bool isLoaded = (bool)initField.GetValue(null);
            Debug.Log("- ApplicationData IsLoaded: " + isLoaded);
        }
    }

    void CheckMenuPageManager()
    {
        Debug.Log("--- MenuPageManager Check ---");
        
        var menuManager = FindObjectOfType<MenuPageManager>();
        if (menuManager == null)
        {
            Debug.LogError("❌ MenuPageManager NOT FOUND in scene!");
            return;
        }
        
        Debug.Log("✅ MenuPageManager found: " + menuManager.name);
        Debug.Log("- GameObject active: " + menuManager.gameObject.activeInHierarchy);
        Debug.Log("- Component enabled: " + menuManager.enabled);
        
        // Check children for PageScene components
        var pageScenes = menuManager.GetComponentsInChildren<PageScene>();
        Debug.Log("- PageScene components found: " + pageScenes.Length);
        
        foreach (var page in pageScenes)
        {
            Debug.Log("  * " + page.name + " - Active: " + page.gameObject.activeInHierarchy);
        }
    }

    void CheckSceneObjects()
    {
        Debug.Log("--- Scene Objects Check ---");
        
        var allObjects = FindObjectsOfType<GameObject>();
        Debug.Log("Total GameObjects in scene: " + allObjects.Length);
        
        int activeObjects = 0;
        int inactiveObjects = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj.activeInHierarchy)
                activeObjects++;
            else
                inactiveObjects++;
        }
        
        Debug.Log("- Active objects: " + activeObjects);
        Debug.Log("- Inactive objects: " + inactiveObjects);
        
        // Check for key GameObjects
        string[] keyObjects = { "MenuPageManager", "ApplicationDataManager", "LevelManager", 
                               "GamePageManager", "ParticleEffectController", "GUICamera" };
        
        foreach (var objName in keyObjects)
        {
            var obj = GameObject.Find(objName);
            if (obj != null)
            {
                Debug.Log("✅ " + objName + " found - Active: " + obj.activeInHierarchy);
            }
            else
            {
                Debug.LogWarning("⚠️ " + objName + " not found");
            }
        }
    }

    void CheckCameras()
    {
        Debug.Log("--- Camera Check ---");
        
        var cameras = FindObjectsOfType<Camera>();
        Debug.Log("Cameras found: " + cameras.Length);
        
        foreach (var cam in cameras)
        {
            Debug.Log("- Camera: " + cam.name);
            Debug.Log("  * Active: " + cam.gameObject.activeInHierarchy);
            Debug.Log("  * Enabled: " + cam.enabled);
            Debug.Log("  * Depth: " + cam.depth);
            Debug.Log("  * Clear Flags: " + cam.clearFlags);
            Debug.Log("  * Culling Mask: " + cam.cullingMask);
        }
        
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log("✅ Main Camera: " + mainCamera.name);
        }
        else
        {
            Debug.LogError("❌ No Main Camera found!");
        }
    }

    void CheckCanvasAndUI()
    {
        Debug.Log("--- Canvas and UI Check ---");
        
        var canvases = FindObjectsOfType<Canvas>();
        Debug.Log("Canvas components found: " + canvases.Length);
        
        foreach (var canvas in canvases)
        {
            Debug.Log("- Canvas: " + canvas.name);
            Debug.Log("  * Active: " + canvas.gameObject.activeInHierarchy);
            Debug.Log("  * Enabled: " + canvas.enabled);
            Debug.Log("  * Render Mode: " + canvas.renderMode);
            Debug.Log("  * Sort Order: " + canvas.sortingOrder);
        }
        
        // Check for Canvas components (Unity's new UI system)
        var canvasComponents = FindObjectsOfType<Canvas>();
        Debug.Log("Canvas components: " + canvasComponents.Length);
        
        // Note: Legacy GUITexture and GUIText have been removed in Unity 6
        // This project appears to use legacy GUI system
    }

    // Method to force reinitialize managers
    [ContextMenu("Force Reinitialize")]
    public void ForceReinitialize()
    {
        Debug.Log("=== FORCING REINITIALIZE ===");
        
        var appDataManager = FindObjectOfType<ApplicationDataManager>();
        if (appDataManager != null)
        {
            // Try to restart ApplicationDataManager
            appDataManager.enabled = false;
            appDataManager.enabled = true;
        }
        
        var menuManager = FindObjectOfType<MenuPageManager>();
        if (menuManager != null)
        {
            // Try to restart MenuPageManager
            menuManager.enabled = false;
            menuManager.enabled = true;
        }
        
        Debug.Log("Reinitialize attempted");
    }
}