using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Legacy UI Manager for Unity 6 - Works without Canvas system
/// Manages OnGUI-based UI panels and navigation
/// </summary>
public class LegacyUIManager : MonoBehaviour
{
    public static LegacyUIManager Instance { get; private set; }
    
    [Header("UI Configuration")]
    public Transform menuContainer;
    public Transform gameHUDContainer;
    public Transform overlayContainer;
    
    private Dictionary<PageType, UIPage> _uiPages = new Dictionary<PageType, UIPage>();
    private UIPage _currentPage;
    private bool _isInitialized = false;
    
    void Awake()
    {
        // Initialize singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeUISystem();
    }
    
    void InitializeUISystem()
    {
        Debug.Log("[LegacyUIManager] Initializing legacy UI system for Unity 6...");
        
        // Set up UI containers
        SetupUIContainers();
        
        // Initialize UI pages
        InitializeUIPages();
        
        _isInitialized = true;
        Debug.Log("[LegacyUIManager] ✅ Legacy UI system initialized");
    }
    
    void SetupUIContainers()
    {
        Debug.Log("[LegacyUIManager] Setting up UI containers...");
        
        // Menu Container (for page navigation)
        if (menuContainer == null)
        {
            GameObject menuObj = new GameObject("MenuContainer");
            menuContainer = menuObj.transform;
            menuContainer.SetParent(transform, false);
        }
        
        // Game HUD Container (for in-game UI)
        if (gameHUDContainer == null)
        {
            GameObject hudObj = new GameObject("GameHUDContainer");
            gameHUDContainer = hudObj.transform;
            gameHUDContainer.SetParent(transform, false);
            gameHUDContainer.gameObject.SetActive(false); // Hidden by default
        }
        
        // Overlay Container (for popups, dialogs)
        if (overlayContainer == null)
        {
            GameObject overlayObj = new GameObject("OverlayContainer");
            overlayContainer = overlayObj.transform;
            overlayContainer.SetParent(transform, false);
        }
        
        Debug.Log("[LegacyUIManager] ✅ UI containers created");
    }
    
    void InitializeUIPages()
    {
        Debug.Log("[LegacyUIManager] Initializing UI pages...");
        
        // Create actual modern pages
        CreateModernPage(PageType.Home, "Home Page", typeof(ModernHomePage));
        CreateModernPage(PageType.Shop, "Shop Page", typeof(ModernShopPage));
        CreateModernPage(PageType.Stats, "Stats Page", typeof(ModernStatsPage));
        
        Debug.Log($"[LegacyUIManager] ✅ Created {_uiPages.Count} UI pages");
    }
    
    void CreateModernPage(PageType pageType, string pageName, System.Type pageComponentType)
    {
        GameObject pageObj = new GameObject($"{pageName}_UI");
        pageObj.transform.SetParent(menuContainer.transform, false);
        
        // Add the specific modern page component
        var uiPage = pageObj.AddComponent(pageComponentType) as UIPage;
        if (uiPage != null)
        {
            uiPage.pageType = pageType;
            uiPage.pageName = pageName;
        }
        
        // Hide by default
        pageObj.SetActive(false);
        
        _uiPages[pageType] = uiPage;
        
        Debug.Log($"[LegacyUIManager] ✅ Created modern page: {pageName} ({pageComponentType.Name})");
    }
    
    void CreatePlaceholderPage(PageType pageType, string pageName)
    {
        GameObject pageObj = new GameObject($"{pageName}_UI");
        pageObj.transform.SetParent(menuContainer, false);
        
        // Add UIPage component
        var uiPage = pageObj.AddComponent<UIPage>();
        uiPage.pageType = pageType;
        uiPage.pageName = pageName;
        
        // Hide by default
        pageObj.SetActive(false);
        
        _uiPages[pageType] = uiPage;
    }
    
    // Public API - replaces MenuPageManager functionality
    public void ShowPage(PageType pageType)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("[LegacyUIManager] UI system not initialized yet");
            return;
        }
        
        // Hide current page
        if (_currentPage != null)
        {
            _currentPage.gameObject.SetActive(false);
            _currentPage.OnPageHidden();
        }
        
        // Show new page
        if (_uiPages.ContainsKey(pageType))
        {
            _currentPage = _uiPages[pageType];
            _currentPage.gameObject.SetActive(true);
            
            Debug.Log($"[LegacyUIManager] ✅ Showing page: {pageType}");
            
            // Notify page it was shown
            _currentPage.OnPageShown();
        }
        else
        {
            Debug.LogError($"[LegacyUIManager] ❌ Page not found: {pageType}");
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
    
    public int GetPageCount()
    {
        return _uiPages.Count;
    }
    
    public string[] GetLoadedPageNames()
    {
        var names = new string[_uiPages.Count];
        int i = 0;
        foreach (var kvp in _uiPages)
        {
            names[i] = kvp.Value.pageName;
            i++;
        }
        return names;
    }
    
    public bool IsPageActive(PageType pageType)
    {
        return _currentPage != null && _currentPage.pageType == pageType;
    }
    
    void OnGUI()
    {
        if (!_isInitialized) return;
        
        // Render all active UI pages using OnGUI
        if (_currentPage != null && _currentPage.gameObject.activeInHierarchy)
        {
            RenderUIPage(_currentPage);
        }
    }
    
    void RenderUIPage(UIPage page)
    {
        // Basic page rendering
        GUILayout.BeginArea(new Rect(50, 50, Screen.width - 100, Screen.height - 100));
        
        GUILayout.BeginVertical("box");
        
        // Page title
        GUILayout.Label(page.pageName, GUI.skin.label);
        GUILayout.Space(20);
        
        // Page content
        GUILayout.Label($"This is the {page.pageName} converted to legacy OnGUI system.");
        GUILayout.Label("Original page content will be rebuilt here using OnGUI.");
        
        GUILayout.Space(20);
        
        // Navigation buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Home", GUILayout.Width(100)))
        {
            ShowPage(PageType.Home);
        }
        if (GUILayout.Button("Shop", GUILayout.Width(100)))
        {
            ShowPage(PageType.Shop);
        }
        if (GUILayout.Button("Stats", GUILayout.Width(100)))
        {
            ShowPage(PageType.Stats);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}