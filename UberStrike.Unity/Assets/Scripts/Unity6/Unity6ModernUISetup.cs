using UnityEngine;

/// <summary>
/// Sets up Unity 6 modern UI system during scene initialization.
/// Add this to a GameObject in your scene to enable the modern OnGUI-based UI.
/// </summary>
public class Unity6ModernUISetup : MonoBehaviour 
{
    [Header("Modern UI Configuration")]
    public bool autoEnableOnStart = true;
    public bool showDebugInfo = true;
    
    void Start()
    {
        if (autoEnableOnStart)
        {
            SetupModernUI();
        }
    }
    
    void Update()
    {
        // Debug keys for testing
        if (showDebugInfo)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var ui = LegacyUIManager.Instance;
                if (ui != null) ui.ShowPage(PageType.Home);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) 
            {
                var ui = LegacyUIManager.Instance;
                if (ui != null) ui.ShowPage(PageType.Shop);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                var ui = LegacyUIManager.Instance;
                if (ui != null) ui.ShowPage(PageType.Stats);
            }
        }
    }
    
    public void SetupModernUI()
    {
        Debug.Log("[Unity6ModernUISetup] Initializing modern UI system...");
        
        // Ensure LegacyUIManager exists
        var uiManager = LegacyUIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogError("[Unity6ModernUISetup] ❌ Failed to create LegacyUIManager");
            return;
        }
        
        Debug.Log("[Unity6ModernUISetup] ✅ LegacyUIManager ready");
        
        // Try to enable ModernUIAdapter
        var adapter = FindObjectOfType<ModernUIAdapter>();
        if (adapter != null)
        {
            adapter.enabled = true;
            Debug.Log("[Unity6ModernUISetup] ✅ ModernUIAdapter enabled");
        }
        else
        {
            Debug.LogWarning("[Unity6ModernUISetup] ⚠️ No ModernUIAdapter found - creating one");
            var go = new GameObject("ModernUIAdapter");
            go.AddComponent<ModernUIAdapter>();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("[Unity6ModernUISetup] 🎮 Press 1/2/3 keys to switch between pages");
        }
    }
    
    void OnGUI()
    {
        if (showDebugInfo)
        {
            GUI.Label(new Rect(10, 10, 300, 60), 
                "Unity6 Modern UI Active\nPress 1=Home, 2=Shop, 3=Stats");
        }
    }
}