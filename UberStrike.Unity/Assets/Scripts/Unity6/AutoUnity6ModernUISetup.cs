using UnityEngine;

/// <summary>
/// Auto Unity 6 Modern UI Setup - Automatically adds modern UI system to scene
/// This will run early and set up our modern UI system alongside existing systems
/// </summary>
[DefaultExecutionOrder(-50)] // Run before other scripts
public class AutoUnity6ModernUISetup : MonoBehaviour 
{
    void Awake()
    {
        Debug.Log("[AutoUnity6ModernUISetup] 🚀 Auto-setting up Unity 6 modern UI system...");
        
        // Create modern UI system if it doesn't exist
        SetupModernUISystem();
    }
    
    void SetupModernUISystem()
    {
        // Check if we already have the components
        var existingActivator = FindObjectOfType<Unity6ModernUIActivator>();
        if (existingActivator == null)
        {
            // Add the activator to this GameObject
            var activator = gameObject.AddComponent<Unity6ModernUIActivator>();
            Debug.Log("[AutoUnity6ModernUISetup] ✅ Added Unity6ModernUIActivator");
        }
        
        // Ensure we have LegacyUIManager
        var legacyUI = FindObjectOfType<LegacyUIManager>();
        if (legacyUI == null)
        {
            var uiGO = new GameObject("LegacyUIManager");
            uiGO.AddComponent<LegacyUIManager>();
            Debug.Log("[AutoUnity6ModernUISetup] ✅ Created LegacyUIManager");
        }
        
        // Ensure we have UIManager
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            var managerGO = new GameObject("UIManager");
            managerGO.AddComponent<UIManager>();
            Debug.Log("[AutoUnity6ModernUISetup] ✅ Created UIManager");
        }
        
        Debug.Log("[AutoUnity6ModernUISetup] 🎯 Modern UI system ready! The scene now has both legacy and modern UI systems.");
    }
}