using UnityEngine;

public class BasicRenderTest : MonoBehaviour
{
    public Material testMaterial;
    
    void Start()
    {
        Debug.Log("=== BASIC RENDER TEST ===");
        Debug.Log("Camera.main: " + (Camera.main != null ? Camera.main.name : "NULL"));
        Debug.Log("Camera.main.enabled: " + (Camera.main != null ? Camera.main.enabled.ToString() : "N/A"));
        
        Camera[] allCameras = FindObjectsOfType<Camera>();
        Debug.Log($"Total cameras: {allCameras.Length}");
        
        foreach (Camera cam in allCameras)
        {
            Debug.Log($"Camera: {cam.name} - Enabled: {cam.enabled} - Active: {cam.gameObject.activeInHierarchy} - Depth: {cam.depth}");
        }
        
        // Create a test cube to see if 3D rendering works
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "RenderTestCube";
        testCube.transform.position = new Vector3(0, 0, 5);
        Debug.Log("Created test cube at (0,0,5)");
        
        // Make it red for visibility
        Renderer renderer = testCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
            Debug.Log("Set test cube color to red");
        }
    }
    
    void OnGUI()
    {
        // This should appear if OnGUI works at all
        Debug.Log("OnGUI called at " + Time.time);
        
        // Force draw something very basic
        GUI.Box(new Rect(0, 0, 100, 50), "BASIC TEST");
        GUI.Label(new Rect(10, 10, 80, 30), "GUI WORKS");
    }
    
    void Update()
    {
        // Log every few seconds to confirm Update is working
        if (Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"Update working - Time: {Time.time:F1}");
            
            // Check if we can see anything in the scene
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Debug.Log($"Main camera position: {mainCam.transform.position}");
                Debug.Log($"Main camera looking at: {mainCam.transform.forward}");
            }
        }
    }
}