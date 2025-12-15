using UnityEngine;

/// <summary>
/// Simple Modern UI Test - Quick verification that our OnGUI system works
/// Add this to any GameObject to test Unity 6 modern UI
/// </summary>
public class SimpleModernUITest : MonoBehaviour 
{
    private bool showTestUI = true;
    private int currentPage = 1;
    
    void Start()
    {
        Debug.Log("[SimpleModernUITest] 🧪 Testing Unity 6 OnGUI system...");
        
        // Test if our components exist
        TestSystemComponents();
    }
    
    void TestSystemComponents()
    {
        Debug.Log("[SimpleModernUITest] 🔍 Checking system components...");
        
        // Check for existing MenuPageManager
        var menuManager = FindObjectOfType<MenuPageManager>();
        if (menuManager != null)
        {
            Debug.Log("[SimpleModernUITest] ✅ Found existing MenuPageManager: " + menuManager.name);
            Debug.Log("[SimpleModernUITest] MenuPageManager.enabled: " + menuManager.enabled);
        }
        else
        {
            Debug.Log("[SimpleModernUITest] ⚠️ No MenuPageManager found");
        }
        
        // Test our modern UI components
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            Debug.Log("[SimpleModernUITest] ✅ LegacyUIManager working");
        }
        else
        {
            Debug.Log("[SimpleModernUITest] 📝 Creating LegacyUIManager...");
            var go = new GameObject("LegacyUIManager");
            go.AddComponent<LegacyUIManager>();
        }
        
        Debug.Log("[SimpleModernUITest] 🎮 Press TAB to toggle test UI, 1/2/3 for pages");
    }
    
    void Update()
    {
        // Toggle test UI
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showTestUI = !showTestUI;
            Debug.Log("[SimpleModernUITest] Test UI: " + (showTestUI ? "ON" : "OFF"));
        }
        
        // Page navigation
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentPage = 1;
            Debug.Log("[SimpleModernUITest] Switched to page 1 (Home)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentPage = 2; 
            Debug.Log("[SimpleModernUITest] Switched to page 2 (Shop)");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentPage = 3;
            Debug.Log("[SimpleModernUITest] Switched to page 3 (Stats)");
        }
    }
    
    void OnGUI()
    {
        if (!showTestUI) return;
        
        // Simple test GUI to prove OnGUI works in Unity 6
        GUI.Box(new Rect(10, 10, 400, 120), "Unity 6 Modern UI Test");
        
        GUI.Label(new Rect(20, 35, 380, 20), "✅ OnGUI working in Unity 6!");
        GUI.Label(new Rect(20, 55, 380, 20), "Current Page: " + GetPageName(currentPage));
        GUI.Label(new Rect(20, 75, 380, 20), "Press TAB=Toggle, 1/2/3=Pages");
        
        // Test buttons
        if (GUI.Button(new Rect(20, 95, 80, 25), "Home"))
        {
            currentPage = 1;
            Debug.Log("[SimpleModernUITest] Button clicked - Home page");
        }
        
        if (GUI.Button(new Rect(110, 95, 80, 25), "Shop"))
        {
            currentPage = 2;
            Debug.Log("[SimpleModernUITest] Button clicked - Shop page");
        }
        
        if (GUI.Button(new Rect(200, 95, 80, 25), "Stats"))
        {
            currentPage = 3;
            Debug.Log("[SimpleModernUITest] Button clicked - Stats page");
        }
        
        if (GUI.Button(new Rect(290, 95, 100, 25), "Test Modern UI"))
        {
            TestLegacyUIManager();
        }
    }
    
    void TestLegacyUIManager()
    {
        var legacyUI = LegacyUIManager.Instance;
        if (legacyUI != null)
        {
            Debug.Log("[SimpleModernUITest] 🚀 Activating LegacyUIManager...");
            legacyUI.ShowPage(PageType.Home);
        }
        else
        {
            Debug.Log("[SimpleModernUITest] ⚠️ LegacyUIManager not found");
        }
    }
    
    string GetPageName(int page)
    {
        switch (page)
        {
            case 1: return "Home Page";
            case 2: return "Shop Page"; 
            case 3: return "Stats Page";
            default: return "Unknown";
        }
    }
}