using UnityEngine;

/// <summary>
/// Unity 6 Modern UI Activator - Tests the new OnGUI-based UI system
/// Add this to test the modern UI system we built for Unity 6
/// </summary>
public class Unity6ModernUIActivator : MonoBehaviour 
{
    [Header("Modern UI Test")]
    public bool activateOnStart = true;
    public bool showDebugUI = true;
    
    void Start()
    {
        if (activateOnStart)
        {
            Debug.Log("[Unity6ModernUIActivator] 🚀 Testing modern UI system...");
            TestModernUISystem();
        }
    }
    
    void TestModernUISystem()
    {
        // Test LegacyUIManager
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            Debug.Log("[Unity6ModernUIActivator] ✅ LegacyUIManager working - showing Home page");
            legacyUI.ShowPage(PageType.Home);
        }
        else
        {
            Debug.LogWarning("[Unity6ModernUIActivator] ⚠️ LegacyUIManager not found - creating one");
            var uiGO = new GameObject("LegacyUIManager");
            uiGO.AddComponent<LegacyUIManager>();
        }
        
        // Test UIManager  
        var uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            Debug.Log("[Unity6ModernUIActivator] ✅ UIManager working");
        }
        else
        {
            Debug.LogWarning("[Unity6ModernUIActivator] ⚠️ UIManager not found - creating one");
            var managerGO = new GameObject("UIManager");
            managerGO.AddComponent<UIManager>();
        }
        
        // Add Unity6ModernUISetup for keyboard controls
        var setupComponent = FindObjectOfType<Unity6ModernUISetup>();
        if (setupComponent == null)
        {
            Debug.Log("[Unity6ModernUIActivator] 📝 Adding Unity6ModernUISetup component");
            gameObject.AddComponent<Unity6ModernUISetup>();
        }
        
        Debug.Log("[Unity6ModernUIActivator] 🎮 Modern UI system active! Press 1/2/3 for page navigation");
    }
    
    void OnGUI()
    {
        if (showDebugUI)
        {
            // Show test status
            GUI.Label(new Rect(10, 70, 400, 80), 
                "Unity 6 Modern UI Test Active\\n" +
                "LegacyUIManager: " + (LegacyUIManager.Instance != null ? "✅" : "❌") + "\\n" +
                "UIManager: " + (UIManager.Instance != null ? "✅" : "❌") + "\\n" +
                "Press 1/2/3 for page navigation");
        }
    }
    
    void Update()
    {
        // Test keyboard input
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[Unity6ModernUIActivator] 🔄 Reactivating modern UI system...");
            TestModernUISystem();
        }
    }
}