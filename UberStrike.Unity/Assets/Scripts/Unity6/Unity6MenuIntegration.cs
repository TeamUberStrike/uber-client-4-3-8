using UnityEngine;

/// <summary>
/// Unity 6 Menu Integration - Bridges modern UI system with existing MenuPageManager
/// This component allows the modern UI to work alongside existing UberStrike systems
/// </summary>
[DefaultExecutionOrder(-20)]  // Run early to set up integration
public class Unity6MenuIntegration : MonoBehaviour
{
    [Header("Integration Settings")]
    public bool useModernUISystem = true;
    public bool fallbackToLegacyMenu = true;
    public bool showDebugInfo = true;
    
    private bool _integrationComplete = false;
    
    void Awake()
    {
        Debug.Log("[Unity6MenuIntegration] 🔧 Setting up Unity 6 menu integration...");
        
        if (useModernUISystem)
        {
            SetupModernUIIntegration();
        }
    }
    
    void SetupModernUIIntegration()
    {
        // Ensure we have the modern UI managers
        EnsureModernUIManagers();
        
        // Set up integration with existing MenuPageManager
        IntegrateWithMenuPageManager();
        
        _integrationComplete = true;
        Debug.Log("[Unity6MenuIntegration] ✅ Integration setup complete");
    }
    
    void EnsureModernUIManagers()
    {
        // Ensure LegacyUIManager exists
        var legacyUI = FindObjectOfType<LegacyUIManager>();
        if (legacyUI == null)
        {
            var legacyGO = new GameObject("LegacyUIManager");
            legacyGO.AddComponent<LegacyUIManager>();
            Debug.Log("[Unity6MenuIntegration] ✅ Created LegacyUIManager");
        }
        
        // Ensure UIManager exists  
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            var uiGO = new GameObject("UIManager");
            uiGO.AddComponent<UIManager>();
            Debug.Log("[Unity6MenuIntegration] ✅ Created UIManager");
        }
        
        // Add ModernUIAdapter if needed
        var adapter = FindObjectOfType<ModernUIAdapter>();
        if (adapter == null)
        {
            var adapterGO = new GameObject("ModernUIAdapter");
            adapterGO.AddComponent<ModernUIAdapter>();
            Debug.Log("[Unity6MenuIntegration] ✅ Created ModernUIAdapter");
        }
    }
    
    void IntegrateWithMenuPageManager()
    {
        // Find existing MenuPageManager
        var menuPageManager = FindObjectOfType<MenuPageManager>();
        if (menuPageManager != null)
        {
            Debug.Log("[Unity6MenuIntegration] 🔗 Found existing MenuPageManager, setting up integration...");
            
            // Create a bridge component
            var bridge = menuPageManager.gameObject.GetComponent<MenuPageManagerBridge>();
            if (bridge == null)
            {
                bridge = menuPageManager.gameObject.AddComponent<MenuPageManagerBridge>();
                Debug.Log("[Unity6MenuIntegration] ✅ Added MenuPageManagerBridge");
            }
        }
        else if (fallbackToLegacyMenu)
        {
            Debug.LogWarning("[Unity6MenuIntegration] ⚠️ MenuPageManager not found, using modern UI only");
        }
    }
    
    void OnGUI()
    {
        if (showDebugInfo && _integrationComplete)
        {
            // Show integration status
            var statusRect = new Rect(10, Screen.height - 120, 300, 100);
            GUI.Box(statusRect, "Unity 6 Menu Integration");
            
            GUI.Label(new Rect(15, Screen.height - 95, 290, 20), "✅ Modern UI System Active");
            
            var legacyUI = LegacyUIManager.Instance;
            var currentPage = legacyUI?.GetCurrentPage();
            string pageInfo = currentPage != null ? $"Current: {currentPage.pageName}" : "No page active";
            
            GUI.Label(new Rect(15, Screen.height - 75, 290, 20), pageInfo);
            GUI.Label(new Rect(15, Screen.height - 55, 290, 20), "Press 1/2/3 for page navigation");
            GUI.Label(new Rect(15, Screen.height - 35, 290, 20), "Press H to hide this info");
        }
        
        // Hide debug info with H key
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.H)
        {
            showDebugInfo = !showDebugInfo;
        }
    }
}

/// <summary>
/// Bridge between old MenuPageManager and new modern UI system
/// Intercepts LoadPage calls and redirects them to the modern system
/// </summary>
public class MenuPageManagerBridge : MonoBehaviour
{
    private MenuPageManager _originalManager;
    
    void Awake()
    {
        _originalManager = GetComponent<MenuPageManager>();
        Debug.Log("[MenuPageManagerBridge] 🌉 Bridge established with MenuPageManager");
    }
    
    public void LoadPageModern(PageType pageType, bool animated = true)
    {
        Debug.Log($"[MenuPageManagerBridge] 🔄 Redirecting page load to modern UI: {pageType}");
        
        // Use modern UI system
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            legacyUI.ShowPage(pageType);
        }
        else
        {
            Debug.LogWarning("[MenuPageManagerBridge] ⚠️ LegacyUIManager not available, falling back to original");
            if (_originalManager != null)
            {
                _originalManager.LoadPage(pageType, animated);
            }
        }
    }
    
    /// <summary>
    /// Static method that can replace MenuPageManager.Instance.LoadPage calls
    /// </summary>
    public static void LoadPage(PageType pageType, bool animated = true)
    {
        var bridge = FindObjectOfType<MenuPageManagerBridge>();
        if (bridge != null)
        {
            bridge.LoadPageModern(pageType, animated);
        }
        else
        {
            // Fallback to modern UI directly
            var legacyUI = LegacyUIManager.Instance;
            if (legacyUI != null)
            {
                legacyUI.ShowPage(pageType);
            }
            else
            {
                Debug.LogError("[MenuPageManagerBridge] ❌ No UI system available!");
            }
        }
    }
}