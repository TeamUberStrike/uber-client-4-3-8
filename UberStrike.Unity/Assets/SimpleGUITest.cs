using UnityEngine;

/// <summary>
/// Simple GUI Test - Shows if OnGUI rendering is working
/// </summary>
public class SimpleGUITest : MonoBehaviour
{
    void OnGUI()
    {
        // Draw a simple test GUI to verify OnGUI is working
        GUI.Label(new Rect(10, 50, 200, 30), "GUI RENDERING TEST");
        GUI.Label(new Rect(10, 80, 300, 30), "If you can see this, OnGUI is working!");
        
        if (GUI.Button(new Rect(10, 110, 150, 30), "Test Button"))
        {
            Debug.Log("✅ GUI Button clicked - OnGUI is working!");
        }
        
        // Display current time to show it's updating
        GUI.Label(new Rect(10, 150, 300, 30), $"Time: {Time.time:F1}");
        
        // Show some system info
        GUI.Label(new Rect(10, 180, 400, 30), $"Screen: {Screen.width}x{Screen.height}");
        GUI.Label(new Rect(10, 210, 400, 30), $"FPS: {(1f/Time.deltaTime):F0}");
        
        // Check for MenuPageManager status
        var menuManager = FindObjectOfType<MenuPageManager>();
        string menuStatus = menuManager != null ? "Found" : "NOT FOUND";
        GUI.Label(new Rect(10, 240, 400, 30), $"MenuPageManager: {menuStatus}");
        
        if (menuManager != null)
        {
            var pages = menuManager.GetComponentsInChildren<PageScene>(true);
            GUI.Label(new Rect(10, 270, 400, 30), $"PageScene count: {pages.Length}");
            
            if (pages.Length > 0)
            {
                for (int i = 0; i < Mathf.Min(pages.Length, 3); i++)
                {
                    string pageInfo = pages[i] != null ? 
                        $"{pages[i].name} ({pages[i].PageType}) - Active: {pages[i].gameObject.activeInHierarchy}" : 
                        "null";
                    GUI.Label(new Rect(10, 300 + i * 30, 500, 30), $"Page {i}: {pageInfo}");
                }
            }
        }
        
        // Manual menu activation button
        if (GUI.Button(new Rect(10, 400, 200, 40), "Force Load Home Page"))
        {
            try
            {
                var instance = MenuPageManager.Instance;
                if (instance != null)
                {
                    instance.LoadPage(PageType.Home, true);
                    Debug.Log("✅ Manual home page load attempted");
                }
                else
                {
                    Debug.LogError("❌ MenuPageManager.Instance is null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Manual load failed: {e.Message}");
            }
        }
    }
}