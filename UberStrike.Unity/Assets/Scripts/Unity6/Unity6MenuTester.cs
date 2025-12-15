using UnityEngine;

/// <summary>
/// Unity 6 Menu Testing System - Comprehensive testing interface for modern UI
/// Provides keyboard shortcuts and testing functionality for the converted UI system
/// </summary>
public class Unity6MenuTester : MonoBehaviour
{
    [Header("Testing Configuration")]
    public bool enableKeyboardShortcuts = true;
    public bool showHelpOverlay = true;
    public bool enablePageCycling = true;
    public bool logPageTransitions = true;
    
    [Header("Keyboard Shortcuts")]
    public KeyCode homePageKey = KeyCode.Alpha1;
    public KeyCode shopPageKey = KeyCode.Alpha2;
    public KeyCode statsPageKey = KeyCode.Alpha3;
    public KeyCode cycleForwardKey = KeyCode.Tab;
    public KeyCode cycleBackwardKey = KeyCode.BackQuote; // Tilde key
    public KeyCode toggleHelpKey = KeyCode.F1;
    public KeyCode testAnimationsKey = KeyCode.Alpha4;
    
    private PageType[] _pageSequence = { PageType.Home, PageType.Shop, PageType.Training, PageType.None };
    private int _currentPageIndex = 0;
    private float _lastKeyPressTime = 0f;
    private const float KEY_REPEAT_DELAY = 0.2f;
    
    void Start()
    {
        Debug.Log("[Unity6MenuTester] 🧪 Menu testing system initialized");
        Debug.Log("[Unity6MenuTester] Press F1 for help, 1/2/3 for pages, Tab to cycle");
    }
    
    void Update()
    {
        if (!enableKeyboardShortcuts) return;
        
        HandleKeyboardInput();
    }
    
    void HandleKeyboardInput()
    {
        // Prevent key repeat spam
        if (Time.time - _lastKeyPressTime < KEY_REPEAT_DELAY) return;
        
        // Direct page navigation
        if (Input.GetKeyDown(homePageKey))
        {
            NavigateToPage(PageType.Home, "Home");
        }
        else if (Input.GetKeyDown(shopPageKey))
        {
            NavigateToPage(PageType.Shop, "Shop");
        }
        else if (Input.GetKeyDown(statsPageKey))
        {
            NavigateToPage(PageType.Training, "Stats/Training");
        }
        else if (Input.GetKeyDown(testAnimationsKey))
        {
            TestAnimations();
        }
        
        // Page cycling
        if (enablePageCycling)
        {
            if (Input.GetKeyDown(cycleForwardKey))
            {
                CyclePage(1);
            }
            else if (Input.GetKeyDown(cycleBackwardKey))
            {
                CyclePage(-1);
            }
        }
        
        // Toggle help
        if (Input.GetKeyDown(toggleHelpKey))
        {
            showHelpOverlay = !showHelpOverlay;
            _lastKeyPressTime = Time.time;
        }
    }
    
    void NavigateToPage(PageType pageType, string pageName)
    {
        _lastKeyPressTime = Time.time;
        
        if (logPageTransitions)
        {
            Debug.Log($"[Unity6MenuTester] 🧭 Navigating to {pageName} page");
        }
        
        // Use the modern UI system
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            legacyUI.ShowPage(pageType);
        }
        else
        {
            Debug.LogWarning("[Unity6MenuTester] ⚠️ LegacyUIManager not found!");
        }
        
        // Update cycle index
        for (int i = 0; i < _pageSequence.Length; i++)
        {
            if (_pageSequence[i] == pageType)
            {
                _currentPageIndex = i;
                break;
            }
        }
    }
    
    void CyclePage(int direction)
    {
        _lastKeyPressTime = Time.time;
        
        _currentPageIndex = (_currentPageIndex + direction) % _pageSequence.Length;
        if (_currentPageIndex < 0) _currentPageIndex = _pageSequence.Length - 1;
        
        var targetPage = _pageSequence[_currentPageIndex];
        string pageName = GetPageDisplayName(targetPage);
        
        if (logPageTransitions)
        {
            Debug.Log($"[Unity6MenuTester] 🔄 Cycling to {pageName} (index {_currentPageIndex})");
        }
        
        NavigateToPage(targetPage, pageName);
    }
    
    void TestAnimations()
    {
        _lastKeyPressTime = Time.time;
        
        Debug.Log("[Unity6MenuTester] 🎬 Testing page animations...");
        
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            var currentPage = legacyUI.GetCurrentPage();
            if (currentPage != null)
            {
                // Test fade animation
                currentPage.FadeOut(() => {
                    Debug.Log("[Unity6MenuTester] ✨ Fade out complete, fading back in...");
                    currentPage.FadeIn();
                });
            }
        }
    }
    
    string GetPageDisplayName(PageType pageType)
    {
        switch (pageType)
        {
            case PageType.Home: return "Home";
            case PageType.Shop: return "Shop";
            case PageType.Training: return "Training/Stats";
            case PageType.None: return "No Page";
            default: return pageType.ToString();
        }
    }
    
    void OnGUI()
    {
        if (!showHelpOverlay) return;
        
        DrawHelpOverlay();
        DrawSystemStatus();
    }
    
    void DrawHelpOverlay()
    {
        var helpRect = new Rect(Screen.width - 350, 10, 340, 220);
        GUI.Box(helpRect, "Unity 6 Menu Tester - Help");
        
        float y = 35;
        float lineHeight = 18;
        
        GUI.Label(new Rect(helpRect.x + 10, y, 320, lineHeight), "🧪 Testing Controls:");
        y += lineHeight + 5;
        
        GUI.Label(new Rect(helpRect.x + 15, y, 320, lineHeight), $"{homePageKey} - Navigate to Home Page");
        y += lineHeight;
        
        GUI.Label(new Rect(helpRect.x + 15, y, 320, lineHeight), $"{shopPageKey} - Navigate to Shop Page");
        y += lineHeight;
        
        GUI.Label(new Rect(helpRect.x + 15, y, 320, lineHeight), $"{statsPageKey} - Navigate to Stats Page");
        y += lineHeight;
        
        GUI.Label(new Rect(helpRect.x + 15, y, 320, lineHeight), $"{testAnimationsKey} - Test Page Animations");
        y += lineHeight + 5;
        
        GUI.Label(new Rect(helpRect.x + 10, y, 320, lineHeight), "🔄 Navigation:");
        y += lineHeight;
        
        GUI.Label(new Rect(helpRect.x + 15, y, 320, lineHeight), $"{cycleForwardKey} - Cycle Forward");
        y += lineHeight;
        
        GUI.Label(new Rect(helpRect.x + 15, y, 320, lineHeight), $"{cycleBackwardKey} - Cycle Backward");
        y += lineHeight + 5;
        
        GUI.Label(new Rect(helpRect.x + 10, y, 320, lineHeight), "ℹ️ System:");
        y += lineHeight;
        
        GUI.Label(new Rect(helpRect.x + 15, y, 320, lineHeight), $"{toggleHelpKey} - Toggle This Help");
    }
    
    void DrawSystemStatus()
    {
        var statusRect = new Rect(Screen.width - 350, 240, 340, 120);
        GUI.Box(statusRect, "System Status");
        
        float y = statusRect.y + 25;
        float lineHeight = 16;
        
        // Current page
        var legacyUI = LegacyUIManager.Instance;
        var currentPage = legacyUI?.GetCurrentPage();
        string pageStatus = currentPage != null ? $"✅ {currentPage.pageName}" : "❌ No Page";
        
        GUI.Label(new Rect(statusRect.x + 10, y, 320, lineHeight), $"Current Page: {pageStatus}");
        y += lineHeight;
        
        // System status
        bool hasLegacyUI = legacyUI != null;
        bool hasUIManager = UIManager.Instance != null;
        
        GUI.Label(new Rect(statusRect.x + 10, y, 320, lineHeight), $"LegacyUIManager: {(hasLegacyUI ? "✅" : "❌")}");
        y += lineHeight;
        
        GUI.Label(new Rect(statusRect.x + 10, y, 320, lineHeight), $"UIManager: {(hasUIManager ? "✅" : "❌")}");
        y += lineHeight;
        
        // Page count
        if (hasLegacyUI && legacyUI.GetPageCount() > 0)
        {
            GUI.Label(new Rect(statusRect.x + 10, y, 320, lineHeight), $"Loaded Pages: {legacyUI.GetPageCount()}");
        }
        else
        {
            GUI.Label(new Rect(statusRect.x + 10, y, 320, lineHeight), "Pages: Not loaded");
        }
    }
}