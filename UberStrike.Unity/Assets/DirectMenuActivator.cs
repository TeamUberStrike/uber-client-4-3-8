using UnityEngine;
using System.Collections;

/// <summary>
/// Direct Menu Activation Script
/// This bypasses complex initialization and directly activates the menu system
/// </summary>
[DefaultExecutionOrder(100)] // Run after other initialization scripts
public class DirectMenuActivator : MonoBehaviour 
{
    [Header("Direct Menu Activation")]
    public bool forceActivateMenu = true;
    public float activationDelay = 2f;
    
    void Start()
    {
        if (forceActivateMenu)
        {
            Debug.Log("[DirectMenuActivator] Starting direct menu activation...");
            StartCoroutine(ActivateMenuDirect());
        }
    }
    
    IEnumerator ActivateMenuDirect()
    {
        // Wait for ApplicationDataManager to finish loading
        yield return new WaitForSeconds(activationDelay);
        
        Debug.Log("[DirectMenuActivator] Attempting direct menu activation...");
        
        // Step 1: Find and activate all key GameObjects
        ActivateKeyGameObjects();
        
        // Step 2: Ensure cameras are working
        SetupCameras();
        
        // Step 3: Force MenuPageManager to show something
        yield return ForceMenuDisplay();
        
        Debug.Log("[DirectMenuActivator] ✅ Direct activation complete!");
    }
    
    void ActivateKeyGameObjects()
    {
        string[] keyObjects = { 
            "MenuPageManager", "ApplicationDataManager", "MenuConfiguration",
            "GUICamera", "LevelManager", "GamePageManager" 
        };
        
        foreach (string objName in keyObjects)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                if (!obj.activeInHierarchy)
                {
                    obj.SetActive(true);
                    Debug.Log($"[DirectMenuActivator] Activated: {objName}");
                }
                
                // Enable all components on the object
                MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null && !comp.enabled)
                    {
                        comp.enabled = true;
                        Debug.Log($"[DirectMenuActivator] Enabled component: {comp.GetType().Name} on {objName}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[DirectMenuActivator] ⚠️ {objName} not found in scene");
            }
        }
    }
    
    void SetupCameras()
    {
        Debug.Log("[DirectMenuActivator] Setting up cameras...");
        
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Debug.Log($"[DirectMenuActivator] Found {allCameras.Length} cameras");
        
        foreach (Camera cam in allCameras)
        {
            if (!cam.enabled)
            {
                cam.enabled = true;
                Debug.Log($"[DirectMenuActivator] Enabled camera: {cam.name}");
            }
            
            if (!cam.gameObject.activeInHierarchy)
            {
                cam.gameObject.SetActive(true);
                Debug.Log($"[DirectMenuActivator] Activated camera GameObject: {cam.name}");
            }
        }
        
        // Ensure main camera is set up properly
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.Skybox;
            mainCam.cullingMask = -1;
            Debug.Log($"[DirectMenuActivator] Main camera configured: {mainCam.name}");
        }
    }
    
    IEnumerator ForceMenuDisplay()
    {
        Debug.Log("[DirectMenuActivator] Forcing menu display...");
        
        // Try multiple approaches to get the menu to show
        
        // Approach 1: Direct MenuPageManager activation
        MenuPageManager menuManager = FindObjectOfType<MenuPageManager>();
        if (menuManager != null)
        {
            Debug.Log("[DirectMenuActivator] Found MenuPageManager, attempting activation...");
            
            // Force restart the component
            menuManager.enabled = false;
            yield return new WaitForEndOfFrame();
            menuManager.enabled = true;
            yield return new WaitForEndOfFrame();
            
            // Check for PageScene components and activate them
            PageScene[] pageScenes = menuManager.GetComponentsInChildren<PageScene>(true);
            Debug.Log($"[DirectMenuActivator] Found {pageScenes.Length} PageScene components");
            
            foreach (PageScene page in pageScenes)
            {
                if (page != null)
                {
                    page.gameObject.SetActive(true);
                    page.enabled = true;
                    Debug.Log($"[DirectMenuActivator] Activated PageScene: {page.name} (Type: {page.PageType})");
                }
            }
            
            // Try to load the home page
            yield return new WaitForSeconds(0.5f);
            try
            {
                MenuPageManager instance = MenuPageManager.Instance;
                if (instance != null)
                {
                    instance.LoadPage(PageType.Home, true);
                    Debug.Log("[DirectMenuActivator] ✅ Home page load attempted via instance");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DirectMenuActivator] ❌ Failed to load home page: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("[DirectMenuActivator] ❌ MenuPageManager not found!");
        }
        
        // Approach 2: Activate all PageScene components in scene
        PageScene[] allPageScenes = FindObjectsOfType<PageScene>(true);
        Debug.Log($"[DirectMenuActivator] Found {allPageScenes.Length} total PageScene components in scene");
        
        foreach (PageScene page in allPageScenes)
        {
            if (page != null && !page.gameObject.activeInHierarchy)
            {
                page.gameObject.SetActive(true);
                Debug.Log($"[DirectMenuActivator] Force activated PageScene: {page.name}");
            }
        }
        
        yield return new WaitForEndOfFrame();
    }
    
    void OnGUI()
    {
        // Simple status display
        GUI.Label(new Rect(10, Screen.height - 30, 400, 20), 
                  "[DirectMenuActivator] Menu activation running...");
    }
}