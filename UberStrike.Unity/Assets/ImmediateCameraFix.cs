using UnityEngine;

/// <summary>
/// Immediate Camera Fix - Runs immediately to fix camera before anything else
/// </summary>
[DefaultExecutionOrder(-1000)] // Run VERY early
public class ImmediateCameraFix : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[ImmediateCameraFix] URGENT CAMERA FIX STARTING...");
        
        // Find the GUICamera and immediately make it the main camera
        Camera[] cameras = FindObjectsOfType<Camera>();
        Debug.Log($"[ImmediateCameraFix] Found {cameras.Length} cameras in Awake");
        
        Camera guiCamera = null;
        foreach (Camera cam in cameras)
        {
            Debug.Log($"[ImmediateCameraFix] Camera: {cam.name} - Tag: {cam.tag}");
            
            if (cam.name == "GUICamera")
            {
                guiCamera = cam;
                break;
            }
        }
        
        if (guiCamera != null)
        {
            Debug.Log("[ImmediateCameraFix] Converting GUICamera to MainCamera immediately...");
            
            // FORCE it to be the main camera
            guiCamera.tag = "MainCamera";
            
            // Make sure it can render GUI
            guiCamera.clearFlags = CameraClearFlags.SolidColor;
            guiCamera.backgroundColor = Color.black;
            guiCamera.cullingMask = -1;
            guiCamera.depth = 0;
            
            // Force enable it
            guiCamera.enabled = true;
            guiCamera.gameObject.SetActive(true);
            
            Debug.Log("[ImmediateCameraFix] ✅ FORCED GUICamera to be MainCamera");
            
            // Verify immediately
            Camera mainCheck = Camera.main;
            Debug.Log($"[ImmediateCameraFix] Verification - Camera.main is now: {(mainCheck != null ? mainCheck.name : "STILL NULL")}");
        }
        else
        {
            Debug.LogError("[ImmediateCameraFix] ❌ GUICamera not found!");
        }
    }
    
    void Start()
    {
        // Final verification
        Camera main = Camera.main;
        if (main != null)
        {
            Debug.Log($"[ImmediateCameraFix] ✅ SUCCESS - Camera.main working: {main.name}");
        }
        else
        {
            Debug.LogError("[ImmediateCameraFix] ❌ FAILED - Camera.main still NULL in Start()");
        }
    }
    
    void OnGUI()
    {
        // Force display something to test if OnGUI works now
        GUI.Label(new Rect(10, 10, 200, 30), "CAMERA FIX TEST");
        GUI.Label(new Rect(10, 40, 300, 30), $"Camera.main: {(Camera.main != null ? Camera.main.name : "NULL")}");
        
        if (GUI.Button(new Rect(10, 70, 150, 30), "OnGUI Works!"))
        {
            Debug.Log("✅ OnGUI button clicked - GUI WORKING!");
        }
    }
}