using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Modern UI Manager for Unity 6 - Uses OnGUI-based rendering system
/// Manages page navigation and UI state for legacy compatibility
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI Configuration")]
    public bool useOnGUIFallback = true;
    public GameObject uiContainer;
    public GameObject menuContainer;
    public GameObject gameHUDContainer;
    
    [Header("UI Prefabs")]
    public GameObject homePagePrefab;
    public GameObject shopPagePrefab;
    public GameObject statsPagePrefab;
    
    private Dictionary<PageType, UIPage> _uiPages = new Dictionary<PageType, UIPage>();
    private UIPage _currentPage;
    private bool _isInitialized = false;
    
    void Awake()
    {
        // Instance is automatically managed by MonoSingleton base class
        InitializeUISystem();
    }
    
    void InitializeUISystem()
    {
        Debug.Log("[UIManager] Initializing modern UI system for Unity 6...");
        
        // Create main UI container (no Canvas needed for OnGUI)
        if (uiContainer == null)
        {
            CreateMainUIContainer();
        }
        
        // Set up UI containers
        SetupUIContainers();
        
        // Initialize UI pages
        InitializeUIPages();
        
        _isInitialized = true;
        Debug.Log("[UIManager] ✅ Modern UI system initialized");
    }
    
    void CreateMainUIContainer()
    {
        Debug.Log("[UIManager] Creating main UI container for OnGUI system...");
        
        GameObject containerObj = new GameObject("MainUIContainer");
        containerObj.transform.SetParent(transform);
        uiContainer = containerObj;
        
        Debug.Log("[UIManager] ✅ Main UI container created for OnGUI rendering");
    }
    
    void SetupUIContainers()
    {
        Debug.Log("[UIManager] Setting up UI containers...");
        
        // Menu Container (for page navigation)
        if (menuContainer == null)
        {
            GameObject menuObj = new GameObject("MenuContainer");
            menuObj.transform.SetParent(uiContainer.transform, false);
            menuContainer = menuObj;
        }
        
        // Game HUD Container (for in-game UI)
        if (gameHUDContainer == null)
        {
            GameObject hudObj = new GameObject("GameHUDContainer");
            hudObj.transform.SetParent(uiContainer.transform, false);
            gameHUDContainer = hudObj;
            gameHUDContainer.SetActive(false); // Hidden by default
        }
        
        Debug.Log("[UIManager] ✅ UI containers created");
    }
    
    void InitializeUIPages()
    {
        Debug.Log("[UIManager] Initializing UI pages...");
        
        // Create actual modern pages instead of placeholders
        CreateModernPage(PageType.Home, "Home Page", typeof(ModernHomePage));
        CreateModernPage(PageType.Shop, "Shop Page", typeof(ModernShopPage));  
        CreateModernPage(PageType.Stats, "Stats Page", typeof(ModernStatsPage));
        
        Debug.Log($"[UIManager] ✅ Created {_uiPages.Count} modern UI pages");
    }
    
    void CreateModernPage(PageType pageType, string pageName, System.Type pageComponentType)
    {
        GameObject pageObj = new GameObject($"{pageName}_UI");
        pageObj.transform.SetParent(menuContainer.transform, false);
        
        // Add the specific page component type
        var uiPage = pageObj.AddComponent(pageComponentType) as UIPage;
        if (uiPage != null)
        {
            uiPage.pageType = pageType;
            uiPage.pageName = pageName;
        }
        
        // Hide by default
        pageObj.SetActive(false);
        
        _uiPages[pageType] = uiPage;
        
        Debug.Log($"[UIManager] ✅ Created modern page: {pageName} ({pageComponentType.Name})");
    }
    
    void CreatePlaceholderPage(PageType pageType, string pageName)
    {
        GameObject pageObj = new GameObject($"{pageName}_UI");
        pageObj.transform.SetParent(menuContainer.transform, false);
        
        // Add UIPage component
        var uiPage = pageObj.AddComponent<UIPage>();
        uiPage.pageType = pageType;
        uiPage.pageName = pageName;
        
        // Add placeholder content using Legacy UI components
        CreatePlaceholderContent(pageObj.transform, pageName);
        
        // Hide by default
        pageObj.SetActive(false);
        
        _uiPages[pageType] = uiPage;
    }
    
    void CreatePlaceholderContent(Transform parent, string pageName)
    {
        // Create title using LegacyTextData
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        titleObj.transform.localPosition = new Vector3(0, 100, 0);
        
        var titleText = titleObj.AddComponent<LegacyTextData>();
        titleText.text = pageName;
        titleText.fontSize = 32;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        // Create info text
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(parent, false);
        infoObj.transform.localPosition = new Vector3(0, 0, 0);
        
        var infoText = infoObj.AddComponent<LegacyTextData>();
        infoText.text = $"This is the converted {pageName} using Unity 6's OnGUI system.\n\nOriginal content will be rebuilt here.";
        infoText.fontSize = 18;
        infoText.color = Color.gray;
        infoText.alignment = TextAnchor.MiddleCenter;
    }
    
    // Public API - replaces MenuPageManager functionality
    public void ShowPage(PageType pageType)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[UIManager] UI system not initialized yet");
            return;
        }
        
        // Hide current page
        if (_currentPage != null)
        {
            _currentPage.gameObject.SetActive(false);
        }
        
        // Show new page
        if (_uiPages.ContainsKey(pageType))
        {
            _currentPage = _uiPages[pageType];
            _currentPage.gameObject.SetActive(true);
            
            Debug.Log($"[UIManager] ✅ Showing page: {pageType}");
            
            // Notify page it was shown
            _currentPage.OnPageShown();
        }
        else
        {
            Debug.LogError($"[UIManager] ❌ Page not found: {pageType}");
        }
    }
    
    public void ShowGameHUD(bool show)
    {
        if (gameHUDContainer != null)
        {
            gameHUDContainer.gameObject.SetActive(show);
            menuContainer.gameObject.SetActive(!show);
        }
    }
    
    public UIPage GetCurrentPage()
    {
        return _currentPage;
    }
    
    public bool IsPageActive(PageType pageType)
    {
        return _currentPage != null && _currentPage.pageType == pageType;
    }
}