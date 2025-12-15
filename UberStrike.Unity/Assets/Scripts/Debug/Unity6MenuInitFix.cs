using UnityEngine;
using System.Collections;

/// <summary>
/// Unity 6 Menu Initialization Fix
/// This script forces the proper initialization sequence for UberStrike in Unity 6
/// Attach this to a GameObject in Latest.unity and set it to run before other scripts
/// </summary>
[DefaultExecutionOrder(-100)] // Run before other scripts
public class Unity6MenuInitFix : MonoBehaviour
{
    [Header("Force Initialization Settings")]
    public bool forceMenuInitialization = true;
    public bool debugLogging = true;
    public float initializationDelay = 0.5f;
    
    [Header("Required Components Check")]
    public bool ensureRequiredComponents = true;
    
    private bool hasInitialized = false;

    void Awake()
    {
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] Starting Unity 6 menu initialization fix...");
        
        // Ensure this runs early in the initialization process
        StartCoroutine(ForceInitialization());
    }

    IEnumerator ForceInitialization()
    {
        // Wait a moment for Unity to finish its own initialization
        yield return new WaitForSeconds(initializationDelay);
        
        if (hasInitialized) yield break;
        hasInitialized = true;
        
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] Beginning forced initialization sequence...");
        
        // Step 1: Ensure ApplicationDataManager is working
        yield return ForceApplicationDataManagerInit();
        
        // Step 2: Ensure GameState is properly initialized
        yield return ForceGameStateInit();
        
        // Step 3: Force MenuPageManager initialization
        yield return ForceMenuPageManagerInit();
        
        // Step 4: Ensure proper camera setup
        yield return ForceCameraSetup();
        
        // Step 5: Activate the first page
        yield return ActivateHomePage();
        
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] ✅ Forced initialization complete!");
    }

    IEnumerator ForceApplicationDataManagerInit()
    {
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] Checking ApplicationDataManager...");
        
        var appManager = FindObjectOfType<ApplicationDataManager>();
        if (appManager == null)
        {
            Debug.LogError("[Unity6MenuInitFix] ❌ ApplicationDataManager not found!");
            yield break;
        }
        
        // Force enable if disabled
        if (!appManager.enabled)
        {
            if (debugLogging)
                Debug.Log("[Unity6MenuInitFix] Enabling ApplicationDataManager...");
            appManager.enabled = true;
        }
        
        if (!appManager.gameObject.activeInHierarchy)
        {
            if (debugLogging)
                Debug.Log("[Unity6MenuInitFix] Activating ApplicationDataManager GameObject...");
            appManager.gameObject.SetActive(true);
        }
        
        yield return new WaitForEndOfFrame();
    }

    IEnumerator ForceGameStateInit()
    {
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] Initializing GameState...");
        
        // Ensure GameState singleton is initialized
        var gameState = GameState.Instance;
        if (gameState == null)
        {
            Debug.LogError("[Unity6MenuInitFix] ❌ Could not initialize GameState!");
            yield break;
        }
        
        // Check if LocalPlayer exists
        if (GameState.LocalPlayer == null)
        {
            if (debugLogging)
                Debug.Log("[Unity6MenuInitFix] Creating LocalPlayer...");
            
            // Try to find LocalPlayer in scene
            var localPlayer = FindObjectOfType<LocalPlayer>();
            if (localPlayer != null)
            {
                if (!localPlayer.enabled)
                    localPlayer.enabled = true;
                if (!localPlayer.gameObject.activeInHierarchy)
                    localPlayer.gameObject.SetActive(true);
            }
        }
        
        yield return new WaitForEndOfFrame();
    }

    IEnumerator ForceMenuPageManagerInit()
    {
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] Forcing MenuPageManager initialization...");
        
        var menuManager = FindObjectOfType<MenuPageManager>();
        if (menuManager == null)
        {
            Debug.LogError("[Unity6MenuInitFix] ❌ MenuPageManager not found!");
            yield break;
        }
        
        // Force enable
        if (!menuManager.enabled)
        {
            if (debugLogging)
                Debug.Log("[Unity6MenuInitFix] Enabling MenuPageManager...");
            menuManager.enabled = true;
        }
        
        if (!menuManager.gameObject.activeInHierarchy)
        {
            if (debugLogging)
                Debug.Log("[Unity6MenuInitFix] Activating MenuPageManager GameObject...");
            menuManager.gameObject.SetActive(true);
        }
        
        // Force re-initialization by disabling and re-enabling
        menuManager.enabled = false;
        yield return new WaitForEndOfFrame();
        menuManager.enabled = true;
        
        // Give it time to collect PageScene components
        yield return new WaitForSeconds(0.2f);
        
        // Check if it found PageScene components
        var pageScenes = menuManager.GetComponentsInChildren<PageScene>(true);
        if (debugLogging)
            Debug.Log($"[Unity6MenuInitFix] Found {pageScenes.Length} PageScene components");
        
        // Activate all PageScene components
        foreach (var pageScene in pageScenes)
        {
            if (!pageScene.enabled)
            {
                if (debugLogging)
                    Debug.Log($"[Unity6MenuInitFix] Enabling PageScene: {pageScene.name}");
                pageScene.enabled = true;
            }
        }
        
        yield return new WaitForEndOfFrame();
    }

    IEnumerator ForceCameraSetup()
    {
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] Setting up cameras...");
        
        // Find main camera
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // Try to find any camera
            var cameras = FindObjectsOfType<Camera>();
            if (cameras.Length > 0)
            {
                mainCamera = cameras[0];
                if (debugLogging)
                    Debug.Log($"[Unity6MenuInitFix] Using camera: {mainCamera.name}");
            }
        }
        
        if (mainCamera != null)
        {
            // Ensure camera is active and enabled
            if (!mainCamera.enabled)
                mainCamera.enabled = true;
            if (!mainCamera.gameObject.activeInHierarchy)
                mainCamera.gameObject.SetActive(true);
            
            // Set up basic camera properties for menu viewing
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.cullingMask = -1; // Everything
            
            if (debugLogging)
                Debug.Log($"[Unity6MenuInitFix] Main camera configured: {mainCamera.name}");
        }
        
        // Check for GUI Camera specifically
        var guiCamera = GameObject.Find("GUICamera");
        if (guiCamera != null)
        {
            var guiCam = guiCamera.GetComponent<Camera>();
            if (guiCam != null)
            {
                if (!guiCam.enabled)
                    guiCam.enabled = true;
                if (!guiCamera.activeInHierarchy)
                    guiCamera.SetActive(true);
                
                if (debugLogging)
                    Debug.Log("[Unity6MenuInitFix] GUI Camera activated");
            }
        }
        
        yield return new WaitForEndOfFrame();
    }

    IEnumerator ActivateHomePage()
    {
        if (debugLogging)
            Debug.Log("[Unity6MenuInitFix] Attempting to activate home page...");
        
        // Wait a moment for everything to initialize
        yield return new WaitForSeconds(0.5f);
        
        try
        {
            // Try to activate the home page
            var menuManager = MenuPageManager.Instance;
            if (menuManager != null)
            {
                // Force load home page
                menuManager.LoadPage(PageType.Home, true);
                
                if (debugLogging)
                    Debug.Log("[Unity6MenuInitFix] ✅ Home page activation attempted");
            }
            else
            {
                Debug.LogWarning("[Unity6MenuInitFix] ⚠️ MenuPageManager instance not available");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Unity6MenuInitFix] ❌ Error activating home page: {e.Message}");
        }
        
        yield return new WaitForEndOfFrame();
    }

    // Manual trigger for testing
    [ContextMenu("Force Reinitialize Menu")]
    public void ForceReinitializeMenu()
    {
        hasInitialized = false;
        StartCoroutine(ForceInitialization());
    }

    void OnGUI()
    {
        if (debugLogging && !hasInitialized)
        {
            GUI.Label(new Rect(10, 10, 300, 20), "Unity 6 Menu Init Fix: Initializing...");
        }
        else if (debugLogging)
        {
            GUI.Label(new Rect(10, 10, 300, 20), "Unity 6 Menu Init Fix: ✅ Complete");
        }
    }
}