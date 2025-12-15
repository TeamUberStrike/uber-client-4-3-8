using UnityEngine;

/// <summary>
/// Auto Setup - Automatically adds basic systems to any scene
/// Just put this on any GameObject and it will add the essential components
/// </summary>
public class AutoSetup : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔧 AUTO SETUP STARTING...");
        
        SetupBasicSystems();
    }
    
    void SetupBasicSystems()
    {
        // Add basic Unity test if not present
        if (FindObjectOfType<BasicUnityTest>() == null)
        {
            var testGO = new GameObject("BasicUnityTest");
            testGO.AddComponent<BasicUnityTest>();
            Debug.Log("✅ Added BasicUnityTest");
        }
        
        // Add simple menu navigation if not present
        if (FindObjectOfType<UltraSimpleMenuNav>() == null)
        {
            var navGO = new GameObject("UltraSimpleMenuNav");
            navGO.AddComponent<UltraSimpleMenuNav>();
            Debug.Log("✅ Added UltraSimpleMenuNav");
        }
        
        Debug.Log("🎉 AUTO SETUP COMPLETE!");
        Debug.Log("👀 Look for white text on screen");
        Debug.Log("🎮 Press ESC to try going to menu");
        Debug.Log("🧪 Press SPACEBAR to test input");
    }
}