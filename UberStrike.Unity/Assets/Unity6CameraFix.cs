using UnityEngine;

/// <summary>
/// Unity 6 Camera Fix - Ensures proper camera setup for GUI rendering
/// </summary>
[DefaultExecutionOrder(-50)]
public class Unity6CameraFix : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[Unity6CameraFix] Fixing camera setup for Unity 6...");
        FixCameraSetup();
    }
    
    void FixCameraSetup()
    {
        // Find existing cameras
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Debug.Log($"[Unity6CameraFix] Found {allCameras.Length} cameras");
        
        Camera mainCamera = null;
        Camera guiCamera = null;
        
        foreach (Camera cam in allCameras)
        {
            Debug.Log($"[Unity6CameraFix] Camera: {cam.name} - Tag: {cam.tag} - Depth: {cam.depth}");
            
            if (cam.name.ToLower().Contains("main"))
            {
                mainCamera = cam;
            }
            else if (cam.name.ToLower().Contains("gui"))
            {
                guiCamera = cam;
            }
        }
        
        // If no main camera found, convert GUICamera or create one
        if (mainCamera == null)
        {
            if (guiCamera != null)
            {
                Debug.Log("[Unity6CameraFix] Converting GUICamera to main camera...");
                
                // Set the GUI camera as main camera
                guiCamera.tag = "MainCamera";
                
                // Configure it for both 3D and GUI rendering
                guiCamera.clearFlags = CameraClearFlags.Skybox;
                guiCamera.cullingMask = -1; // Render everything
                guiCamera.depth = 0; // Main camera should have depth 0
                
                mainCamera = guiCamera;
                
                Debug.Log($"[Unity6CameraFix] ✅ Set {guiCamera.name} as MainCamera");
            }
            else
            {
                Debug.Log("[Unity6CameraFix] Creating new main camera...");
                
                // Create a new main camera
                GameObject cameraObj = new GameObject("MainCamera");
                mainCamera = cameraObj.AddComponent<Camera>();
                mainCamera.tag = "MainCamera";
                
                // Position it sensibly
                mainCamera.transform.position = new Vector3(0, 1, -3);
                mainCamera.transform.LookAt(Vector3.zero);
                
                // Configure for general rendering
                mainCamera.clearFlags = CameraClearFlags.Skybox;
                mainCamera.cullingMask = -1;
                mainCamera.depth = 0;
                
                Debug.Log("[Unity6CameraFix] ✅ Created new MainCamera");
            }
        }
        else
        {
            // Main camera exists, just ensure it's properly configured
            mainCamera.tag = "MainCamera";
            
            Debug.Log($"[Unity6CameraFix] ✅ Main camera already exists: {mainCamera.name}");
        }
        
        // Verify the fix worked
        Camera verifyMain = Camera.main;
        if (verifyMain != null)
        {
            Debug.Log($"[Unity6CameraFix] ✅ SUCCESS - Camera.main is now: {verifyMain.name}");
            Debug.Log($"[Unity6CameraFix] Position: {verifyMain.transform.position}");
            Debug.Log($"[Unity6CameraFix] Enabled: {verifyMain.enabled}");
            Debug.Log($"[Unity6CameraFix] Active: {verifyMain.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[Unity6CameraFix] ❌ FAILED - Camera.main is still NULL");
        }
    }
    
    void Start()
    {
        // Double-check after all initialization
        if (Camera.main == null)
        {
            Debug.LogError("[Unity6CameraFix] ❌ Camera.main is STILL NULL after fix attempt!");
            FixCameraSetup(); // Try again
        }
        else
        {
            Debug.Log($"[Unity6CameraFix] ✅ Camera.main confirmed working: {Camera.main.name}");
        }
    }
}