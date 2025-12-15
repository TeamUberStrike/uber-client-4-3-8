using UnityEngine;

/// <summary>
/// Unity 6 Menu System Setup - Complete testing environment for converted UI
/// This script sets up everything needed to test the modern UI conversion
/// Run this to create a complete testing scene
/// </summary>
public class Unity6CompleteSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    public bool autoSetupOnStart = true;
    public bool createFullTestEnvironment = true;
    public bool enableDebugLogging = true;
    public bool createCameraIfMissing = true;
    
    [Header("Component Configuration")]
    public bool addMenuTester = true;
    public bool addMenuIntegration = true;
    public bool addModernUIAdapter = true;
    
    private bool _setupComplete = false;
    
    void Start()
    {
        if (autoSetupOnStart && !_setupComplete)
        {
            SetupCompleteTestEnvironment();
        }
    }
    
    [ContextMenu("Setup Complete Test Environment")]
    public void SetupCompleteTestEnvironment()
    {
        Debug.Log("[Unity6CompleteSetup] 🚀 Setting up complete Unity 6 modern UI test environment...");
        
        // Step 1: Ensure basic Unity components
        EnsureBasicComponents();
        
        // Step 2: Create modern UI system
        CreateModernUISystem();
        
        // Step 3: Add testing components
        AddTestingComponents();
        
        // Step 4: Add integration components
        AddIntegrationComponents();
        
        // Step 5: Initialize everything
        InitializeSystem();
        
        _setupComplete = true;
        Debug.Log("[Unity6CompleteSetup] ✅ Complete setup finished! Press F1 for help or 1/2/3 for pages");
    }
    
    void EnsureBasicComponents()
    {
        // Ensure we have a camera
        if (createCameraIfMissing && Camera.main == null)
        {
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.backgroundColor = Color.black;
            Debug.Log("[Unity6CompleteSetup] ✅ Created Main Camera");
        }
        
        // Note: EventSystem not needed for OnGUI-based UI in Unity 6
        Debug.Log("[Unity6CompleteSetup] ℹ️ Using OnGUI - EventSystem not required");
    }
    
    void CreateModernUISystem()
    {
        // Create LegacyUIManager
        var legacyUI = FindObjectOfType<LegacyUIManager>();
        if (legacyUI == null)
        {
            var legacyGO = new GameObject("LegacyUIManager");
            legacyUI = legacyGO.AddComponent<LegacyUIManager>();
            Debug.Log("[Unity6CompleteSetup] ✅ Created LegacyUIManager");
        }
        
        // Create UIManager
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            var uiGO = new GameObject("UIManager");
            uiManager = uiGO.AddComponent<UIManager>();
            Debug.Log("[Unity6CompleteSetup] ✅ Created UIManager");
        }
    }
    
    void AddTestingComponents()
    {
        if (addMenuTester)
        {
            var tester = FindObjectOfType<Unity6MenuTester>();
            if (tester == null)
            {
                var testerGO = new GameObject("Unity6MenuTester");
                tester = testerGO.AddComponent<Unity6MenuTester>();
                Debug.Log("[Unity6CompleteSetup] ✅ Added Unity6MenuTester");
            }
        }
    }
    
    void AddIntegrationComponents()
    {
        if (addMenuIntegration)
        {
            var integration = FindObjectOfType<Unity6MenuIntegration>();
            if (integration == null)
            {
                var integrationGO = new GameObject("Unity6MenuIntegration");
                integration = integrationGO.AddComponent<Unity6MenuIntegration>();
                Debug.Log("[Unity6CompleteSetup] ✅ Added Unity6MenuIntegration");
            }
        }
        
        if (addModernUIAdapter)
        {
            var adapter = FindObjectOfType<ModernUIAdapter>();
            if (adapter == null)
            {
                var adapterGO = new GameObject("ModernUIAdapter");
                adapter = adapterGO.AddComponent<ModernUIAdapter>();
                Debug.Log("[Unity6CompleteSetup] ✅ Added ModernUIAdapter");
            }
        }
    }
    
    void InitializeSystem()
    {
        Debug.Log("[Unity6CompleteSetup] 🔧 Initializing systems...");
        
        // Force initialization of the UI system
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            Debug.Log("[Unity6CompleteSetup] 📋 UI Pages loaded:");
            var pageNames = legacyUI.GetLoadedPageNames();
            for (int i = 0; i < pageNames.Length; i++)
            {
                Debug.Log($"[Unity6CompleteSetup]   {i + 1}. {pageNames[i]}");
            }
            
            // Show the home page by default
            legacyUI.ShowPage(PageType.Home);
        }
        
        // Initialize MenuPageManager compatibility if it exists
        var menuPageManager = FindObjectOfType<MenuPageManager>();
        if (menuPageManager != null)
        {
            Debug.Log("[Unity6CompleteSetup] ✅ Found existing MenuPageManager - compatibility mode active");
        }
    }
    
    void OnGUI()
    {
        if (!_setupComplete)
        {
            // Show setup prompt
            var setupRect = new Rect(10, 10, 400, 100);
            GUI.Box(setupRect, "Unity 6 Complete Setup");
            GUI.Label(new Rect(20, 35, 380, 20), "Modern UI system not set up yet.");
            
            if (GUI.Button(new Rect(20, 60, 200, 30), "Setup Complete Test Environment"))
            {
                SetupCompleteTestEnvironment();
            }
            
            if (GUI.Button(new Rect(230, 60, 170, 30), "Auto Setup"))
            {
                autoSetupOnStart = true;
                SetupCompleteTestEnvironment();
            }
        }
        else if (enableDebugLogging)
        {
            // Show quick status
            var statusRect = new Rect(10, Screen.height - 60, 300, 50);
            GUI.Box(statusRect, "Unity 6 Setup Complete");
            GUI.Label(new Rect(20, Screen.height - 35, 280, 20), "✅ Modern UI active - Press F1 for help");
        }
    }
    
    /// <summary>
    /// Public method to test the entire system
    /// </summary>
    [ContextMenu("Run Complete System Test")]
    public void RunCompleteSystemTest()
    {
        Debug.Log("[Unity6CompleteSetup] 🧪 Running complete system test...");
        
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI == null)
        {
            Debug.LogError("[Unity6CompleteSetup] ❌ LegacyUIManager not found!");
            return;
        }
        
        // Test page transitions
        StartCoroutine(TestPageTransitions());
    }
    
    System.Collections.IEnumerator TestPageTransitions()
    {
        var legacyUI = LegacyUIManager.Instance;
        var pages = new PageType[] { PageType.Home, PageType.Shop, PageType.Training };
        
        foreach (var page in pages)
        {
            Debug.Log($"[Unity6CompleteSetup] 🔄 Testing page: {page}");
            legacyUI.ShowPage(page);
            yield return new UnityEngine.WaitForSeconds(1f);
        }
        
        Debug.Log("[Unity6CompleteSetup] ✅ System test complete!");
    }
}