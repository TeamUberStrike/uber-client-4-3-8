using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Unity 6 GUI Rendering Fix - Forces OnGUI system to work
/// </summary>
[DefaultExecutionOrder(-500)]
public class Unity6GUIFix : MonoBehaviour
{
    private bool guiTestActive = true;
    private Camera renderCamera;
    
    void Awake()
    {
        Debug.Log("[Unity6GUIFix] Attempting to fix GUI rendering in Unity 6...");
        
        // Ensure we have a proper render camera
        SetupRenderCamera();
        
        // Force enable GUI rendering
        EnableGUIRendering();
    }
    
    void SetupRenderCamera()
    {
        // Find or create a camera for GUI rendering
        renderCamera = Camera.main;
        
        if (renderCamera == null)
        {
            renderCamera = FindObjectOfType<Camera>();
        }
        
        if (renderCamera != null)
        {
            Debug.Log($"[Unity6GUIFix] Using camera for GUI: {renderCamera.name}");
            
            // Ensure camera can render GUI
            renderCamera.clearFlags = CameraClearFlags.Skybox;
            renderCamera.cullingMask = -1;
            renderCamera.depth = 0;
            renderCamera.enabled = true;
            
            // Force Unity to recognize this as the main camera
            if (renderCamera.tag != "MainCamera")
            {
                renderCamera.tag = "MainCamera";
                Debug.Log("[Unity6GUIFix] Set camera tag to MainCamera");
            }
        }
        else
        {
            Debug.LogError("[Unity6GUIFix] ❌ No camera found for GUI rendering!");
        }
    }
    
    void EnableGUIRendering()
    {
        // Force Unity to enable GUI rendering
        // In Unity 6, GUI might be disabled by default for performance
        
        Debug.Log("[Unity6GUIFix] Forcing GUI system activation...");
        
        // Ensure GUI is enabled in the graphics settings
        QualitySettings.realtimeReflectionProbes = true;
        
        // Force immediate GUI update
        Canvas.ForceUpdateCanvases();
    }
    
    void Start()
    {
        Debug.Log("[Unity6GUIFix] GUI Fix initialization complete");
        
        // Verify camera setup
        Camera main = Camera.main;
        Debug.Log($"[Unity6GUIFix] Camera.main verification: {(main != null ? main.name : "NULL")}");
        
        if (main != null)
        {
            Debug.Log($"[Unity6GUIFix] Camera position: {main.transform.position}");
            Debug.Log($"[Unity6GUIFix] Camera enabled: {main.enabled}");
            Debug.Log($"[Unity6GUIFix] Camera active: {main.gameObject.activeInHierarchy}");
        }
    }
    
    void OnGUI()
    {
        if (!guiTestActive) return;
        
        // Force GUI to render with immediate mode
        GUI.depth = 0;
        
        // Test if GUI is working at all
        GUI.Label(new Rect(10, 10, 300, 30), "=== UNITY 6 GUI TEST ===");
        GUI.Label(new Rect(10, 40, 300, 30), $"Time: {Time.time:F1}");
        GUI.Label(new Rect(10, 70, 300, 30), $"Camera: {(Camera.main != null ? Camera.main.name : "NULL")}");
        
        // Large, obvious button
        if (GUI.Button(new Rect(10, 100, 200, 50), "GUI IS WORKING!"))
        {
            Debug.Log("✅ GUI button clicked - OnGUI system is functional!");
            guiTestActive = false; // Turn off test once confirmed working
        }
        
        // Instructions
        GUI.Label(new Rect(10, 160, 400, 60), 
                  "If you can see this text, OnGUI is working.\n" +
                  "If UberStrike menu still doesn't show,\n" +
                  "the issue is with UberStrike's specific GUI code.");
        
        // UberStrike menu status
        var menuManager = FindObjectOfType<MenuPageManager>();
        string menuStatus = menuManager != null ? "FOUND" : "NOT FOUND";
        GUI.Label(new Rect(10, 230, 300, 30), $"MenuPageManager: {menuStatus}");
        
        if (menuManager != null)
        {
            var pages = menuManager.GetComponentsInChildren<PageScene>(true);
            GUI.Label(new Rect(10, 260, 300, 30), $"PageScene components: {pages.Length}");
            
            // Manual menu activation
            if (GUI.Button(new Rect(10, 290, 180, 40), "FORCE MENU LOAD"))
            {
                Debug.Log("[Unity6GUIFix] Manual menu activation attempted");
                try
                {
                    var instance = MenuPageManager.Instance;
                    if (instance != null)
                    {
                        instance.LoadPage(PageType.Home, true);
                        Debug.Log("✅ Manual menu load attempted");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Manual menu load failed: {e.Message}");
                }
            }
        }
    }
    
    void Update()
    {
        // Log periodically to confirm Update is working
        if (Time.frameCount % 120 == 0) // Every ~2 seconds at 60fps
        {
            Debug.Log($"[Unity6GUIFix] Update working - Frame: {Time.frameCount}, Camera.main: {(Camera.main != null ? Camera.main.name : "NULL")}");
        }
    }
}