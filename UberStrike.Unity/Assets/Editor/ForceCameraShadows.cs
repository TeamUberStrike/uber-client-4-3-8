using UnityEngine;
using UnityEditor;

public class ForceCameraShadows : MonoBehaviour
{
    [MenuItem("UberStrike/Force Camera Shadows")]
    public static void ForceShadows()
    {
        Debug.Log("=== FORCING CAMERA SHADOWS ===");
        
        // Find all cameras
        Camera[] cameras = FindObjectsOfType<Camera>();
        
        foreach (var cam in cameras)
        {
            Debug.Log($"[CAMERA] {cam.name}");
            
            // Force rendering path to Forward (which supports shadows)
            cam.renderingPath = RenderingPath.Forward;
            Debug.Log($"  Set rendering path: Forward");
            
            // Ensure camera renders shadows
            cam.clearFlags = CameraClearFlags.Skybox; // Or Solid Color
            
            // Check for post-processing that might disable shadows
            var postProcessing = cam.GetComponent("PostProcessingBehaviour");
            if (postProcessing != null)
            {
                Debug.LogWarning($"  Found PostProcessingBehaviour - this might interfere with shadows");
            }
            
            EditorUtility.SetDirty(cam);
        }
        
        Debug.Log("=== CAMERA SHADOWS FORCED ===");
        Debug.Log("Save scene and test!");
    }
}
