using UnityEngine;

/// <summary>
/// Bridge between legacy MenuPageManager and modern UIManager
/// Allows gradual conversion while maintaining compatibility
/// </summary>
public class ModernUIAdapter : MonoBehaviour
{
    [Header("Modern UI Integration")]
    public bool useModernUI = true;
    public bool showDebugInfo = true;
    
    private bool _isModernUIInitialized = false;
    
    void Start()
    {
        if (useModernUI)
        {
            Debug.Log("[ModernUIAdapter] Initializing modern UI system...");
            InitializeModernUI();
        }
        else
        {
            Debug.Log("[ModernUIAdapter] Using legacy GUI system");
        }
    }
    
    void InitializeModernUI()
    {
        // Ensure LegacyUIManager is created
        var uiManager = LegacyUIManager.Instance;
        if (uiManager != null)
        {
            _isModernUIInitialized = true;
            Debug.Log("[ModernUIAdapter] ✅ Legacy UI system ready");
            
            // Show home page by default
            uiManager.ShowPage(PageType.Home);
            
            // Disable legacy menu system if it exists
            DisableLegacyMenuSystem();
        }
        else
        {
            Debug.LogError("[ModernUIAdapter] ❌ Failed to initialize LegacyUIManager");
        }
    }
    
    void DisableLegacyMenuSystem()
    {
        // Find and disable legacy MenuPageManager
        var legacyMenuManager = FindObjectOfType<MenuPageManager>();
        if (legacyMenuManager != null)
        {
            Debug.Log("[ModernUIAdapter] Disabling legacy MenuPageManager");
            legacyMenuManager.enabled = false;
            
            // Hide legacy page scenes
            var pageScenes = legacyMenuManager.GetComponentsInChildren<PageScene>(true);
            foreach (var pageScene in pageScenes)
            {
                pageScene.gameObject.SetActive(false);
            }
        }
    }
    
    // Public API that mimics MenuPageManager for compatibility
    public static void LoadPage(PageType pageType, bool animated = true)
    {
        var adapter = FindObjectOfType<ModernUIAdapter>();
        if (adapter != null && adapter.useModernUI && adapter._isModernUIInitialized)
        {
            // Use modern UI system
            Debug.Log($"[ModernUIAdapter] Loading page via modern UI: {pageType}");
            UIManager.Instance.ShowPage(pageType);
        }
        else
        {
            // Fallback to legacy system
            Debug.Log($"[ModernUIAdapter] Fallback to legacy system: {pageType}");
            var legacyManager = MenuPageManager.Instance;
            if (legacyManager != null)
            {
                legacyManager.LoadPage(pageType, animated);
            }
        }
    }
    
    public static bool IsCurrentPage(PageType pageType)
    {
        var adapter = FindObjectOfType<ModernUIAdapter>();
        if (adapter != null && adapter.useModernUI && adapter._isModernUIInitialized)
        {
            return UIManager.Instance.IsPageActive(pageType);
        }
        else
        {
            return MenuPageManager.IsCurrentPage(pageType);
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        // Show modern UI status
        GUI.Box(new Rect(10, 10, 300, 120), "Modern UI Adapter Status");
        GUI.Label(new Rect(20, 35, 280, 20), $"Modern UI: {(useModernUI ? "ENABLED" : "DISABLED")}");
        GUI.Label(new Rect(20, 55, 280, 20), $"Initialized: {(_isModernUIInitialized ? "YES" : "NO")}");
        
        if (_isModernUIInitialized)
        {
            var currentPage = UIManager.Instance.GetCurrentPage();
            string currentPageName = currentPage != null ? currentPage.pageName : "None";
            GUI.Label(new Rect(20, 75, 280, 20), $"Current Page: {currentPageName}");
        }
        
        // Test buttons
        if (GUI.Button(new Rect(20, 95, 80, 25), "Home"))
        {
            LoadPage(PageType.Home);
        }
        if (GUI.Button(new Rect(110, 95, 80, 25), "Shop"))
        {
            LoadPage(PageType.Shop);
        }
        if (GUI.Button(new Rect(200, 95, 80, 25), "Stats"))
        {
            LoadPage(PageType.Stats);
        }
    }
    
    void Update()
    {
        // Handle input for testing
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadPage(PageType.Home);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LoadPage(PageType.Shop);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            LoadPage(PageType.Stats);
        }
    }
}