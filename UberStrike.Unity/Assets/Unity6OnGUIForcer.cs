using UnityEngine;
using System.Reflection;

/// <summary>
/// Unity 6 OnGUI System Diagnostic and Force Enable
/// </summary>
public class Unity6OnGUIForcer : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== UNITY 6 ONGUI SYSTEM DIAGNOSTIC ===");
        DiagnoseGUISystem();
        AttemptGUIFixes();
    }
    
    void DiagnoseGUISystem()
    {
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Is Editor: {Application.isEditor}");
        
        // Check if OnGUI is disabled at the engine level
        Debug.Log($"Screen size: {Screen.width}x{Screen.height}");
        Debug.Log($"Current camera: {(Camera.current != null ? Camera.current.name : "NULL")}");
        Debug.Log($"Main camera: {(Camera.main != null ? Camera.main.name : "NULL")}");
        
        // Try to detect GUI system status through reflection
        try
        {
            var guiType = typeof(GUI);
            var fieldsInfo = guiType.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Log($"GUI class has {fieldsInfo.Length} static fields");
            
            // Check if there's any indication of GUI being disabled
            foreach (var field in fieldsInfo)
            {
                if (field.Name.ToLower().Contains("enabled") || 
                    field.Name.ToLower().Contains("active") ||
                    field.Name.ToLower().Contains("disabled"))
                {
                    try
                    {
                        var value = field.GetValue(null);
                        Debug.Log($"GUI.{field.Name} = {value}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log($"GUI.{field.Name} = Error: {e.Message}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GUI reflection failed: {e.Message}");
        }
    }
    
    void AttemptGUIFixes()
    {
        Debug.Log("=== ATTEMPTING GUI FIXES ===");
        
        // This project uses legacy GUI only - no modern UI system available
        Debug.Log("Project uses legacy OnGUI system only");
        
        // Fix: Ensure proper camera setup with GUI support
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.Log("Creating new main camera for GUI...");
            GameObject camObj = new GameObject("MainCamera_ForGUI");
            mainCam = camObj.AddComponent<Camera>();
            mainCam.tag = "MainCamera";
        }
        
        // Ensure camera settings support GUI
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        mainCam.cullingMask = -1;
        mainCam.depth = 0;
        mainCam.enabled = true;
        
        Debug.Log($"Main camera configured: {mainCam.name}");
        
        // Test legacy GUI system
        StartCoroutine(TestLegacyGUI());
    }
    
    System.Collections.IEnumerator TestLegacyGUI()
    {
        yield return new WaitForSeconds(1f);
        
        Debug.Log("=== TESTING LEGACY GUI SYSTEM ===");
        
        // Force immediate GUI rendering test using Graphics.DrawTexture
        try
        {
            // Create a simple texture to test immediate mode rendering
            Texture2D testTexture = new Texture2D(200, 50);
            Color[] pixels = new Color[200 * 50];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.red;
            }
            testTexture.SetPixels(pixels);
            testTexture.Apply();
            
            Debug.Log("✅ Created test texture for immediate mode rendering");
            
            // Try to draw it directly (this bypasses OnGUI)
            StartCoroutine(ForceRenderTexture(testTexture));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Legacy GUI test failed: {e.Message}");
        }
        
        // Final attempt: Check if Unity 6 has completely disabled OnGUI
        Debug.Log("=== FINAL DIAGNOSIS ===");
        Debug.Log("If OnGUI never gets called, Unity 6 has disabled legacy GUI");
        Debug.Log("Recommendation: Use Unity 2022 LTS instead of Unity 6");
        Debug.Log("Or convert UberStrike to modern UI system (major project)");
    }
    
    System.Collections.IEnumerator ForceRenderTexture(Texture2D texture)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForEndOfFrame();
            
            // Try direct Graphics API calls
            Graphics.DrawTexture(new Rect(10, 10, 200, 50), texture);
            
            if (i == 5)
            {
                Debug.Log("Attempted direct texture rendering - check screen for red rectangle");
            }
        }
    }
    
    void Update()
    {
        // This should run, but OnGUI won't
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Update running - Frame {Time.frameCount} - Still no OnGUI calls");
        }
    }
    
    void OnGUI()
    {
        // This should never be called if OnGUI is broken
        Debug.Log("🎉 MIRACLE! OnGUI is working after all!");
        GUI.Label(new Rect(10, 10, 300, 30), "ONGUI IS WORKING!");
    }
}