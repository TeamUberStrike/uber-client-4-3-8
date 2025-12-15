using UnityEngine;
using System.Collections;

/// <summary>
/// Unity 6 Initialization Debugger for Latest.unity Scene
/// 
/// This script helps debug why the scene appears empty when played in Unity 6.
/// It monitors the initialization sequence and displays debug information.
/// </summary>
public class Unity6InitializationDebugger : MonoBehaviour 
{
    private bool showDebugGUI = true;
    private string debugStatus = "Starting...";
    private float timer = 0f;
    
    void Start()
    {
        StartCoroutine(MonitorInitialization());
    }
    
    IEnumerator MonitorInitialization()
    {
        debugStatus = "Checking ApplicationDataManager...";
        yield return new WaitForSeconds(0.1f);
        
        // Check if ApplicationDataManager exists
        if (ApplicationDataManager.Instance == null)
        {
            debugStatus = "❌ ApplicationDataManager not found!";
            yield break;
        }
        else
        {
            debugStatus = "✅ ApplicationDataManager found";
        }
        
        yield return new WaitForSeconds(1f);
        
        // Check MenuPageManager
        debugStatus = "Checking MenuPageManager...";
        if (MenuPageManager.Instance == null)
        {
            debugStatus = "❌ MenuPageManager not found!";
        }
        else
        {
            debugStatus = "✅ MenuPageManager found";
        }
        
        yield return new WaitForSeconds(1f);
        
        // Check GamePageManager
        debugStatus = "Checking GamePageManager...";
        if (GamePageManager.Instance == null)
        {
            debugStatus = "❌ GamePageManager not found!";
        }
        else
        {
            debugStatus = "✅ GamePageManager found";
        }
        
        yield return new WaitForSeconds(1f);
        
        // Check Camera
        debugStatus = "Checking Camera...";
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            debugStatus = "❌ Main Camera not found!";
        }
        else
        {
            debugStatus = $"✅ Main Camera found: {mainCam.name}";
        }
        
        yield return new WaitForSeconds(1f);
        
        // Check for errors in ApplicationDataManager
        debugStatus = "Monitoring ApplicationDataManager initialization...";
        
        // Wait for ApplicationDataManager to initialize
        float timeout = 10f;
        while (timeout > 0)
        {
            if (ApplicationDataManager.Instance != null)
            {
                // Check if initialization is complete
                try 
                {
                    var channel = ApplicationDataManager.Channel;
                    debugStatus = $"✅ ApplicationDataManager initialized (Channel: {channel})";
                    break;
                }
                catch
                {
                    debugStatus = "⏳ ApplicationDataManager initializing...";
                }
            }
            
            timeout -= Time.deltaTime;
            yield return null;
        }
        
        if (timeout <= 0)
        {
            debugStatus = "❌ ApplicationDataManager initialization timeout!";
        }
        
        yield return new WaitForSeconds(2f);
        
        // Final check
        debugStatus = "✅ Initialization monitoring complete. Press 'H' to hide this debug info.";
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            showDebugGUI = !showDebugGUI;
        }
        
        // Check for common Unity 6 issues
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("=== Unity 6 Initialization Debug Info ===");
            Debug.Log($"Unity Version: {Application.unityVersion}");
            Debug.Log($"Platform: {Application.platform}");
            Debug.Log($"Editor: {Application.isEditor}");
            Debug.Log($"Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Debug.Log($"Camera Count: {FindObjectsOfType<Camera>().Length}");
            Debug.Log($"GameObject Count: {FindObjectsOfType<GameObject>().Length}");
            
            if (ApplicationDataManager.Instance != null)
            {
                Debug.Log("ApplicationDataManager: Found");
            }
            else
            {
                Debug.Log("ApplicationDataManager: Not found");
            }
            
            if (MenuPageManager.Instance != null)
            {
                Debug.Log("MenuPageManager: Found");
            }
            else
            {
                Debug.Log("MenuPageManager: Not found");
            }
        }
    }
    
    void OnGUI()
    {
        if (!showDebugGUI) return;
        
        // Simple debug GUI
        GUI.skin = null; // Use Unity default skin
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Unity 6 Initialization Debugger", GUI.skin.label);
        GUILayout.Space(5);
        
        GUILayout.Label($"Status: {debugStatus}");
        GUILayout.Label($"Time: {timer:F1}s");
        GUILayout.Space(5);
        
        GUILayout.Label("Controls:");
        GUILayout.Label("• H - Hide/Show this debug info");
        GUILayout.Label("• D - Log debug info to console");
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Force Initialize MenuPageManager"))
        {
            if (MenuPageManager.Instance != null)
            {
                try
                {
                    MenuPageManager.Instance.LoadPage(PageType.Home);
                    debugStatus = "✅ Forced MenuPageManager to load Home page";
                }
                catch (System.Exception e)
                {
                    debugStatus = $"❌ MenuPageManager error: {e.Message}";
                }
            }
        }
        
        if (GUILayout.Button("Check Configuration"))
        {
            string configPath = Application.dataPath + "/../EditorConfiguration.xml";
            if (System.IO.File.Exists(configPath))
            {
                debugStatus = "✅ EditorConfiguration.xml found";
            }
            else
            {
                debugStatus = "❌ EditorConfiguration.xml missing!";
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}