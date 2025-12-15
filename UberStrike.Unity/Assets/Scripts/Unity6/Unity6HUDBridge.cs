using UnityEngine;

/// <summary>
/// Unity 6 HUD Bridge - Connects Unity 6 HUD with existing UberStrike HUD system
/// Provides compatibility layer for in-game HUD components
/// </summary>
public class Unity6HUDBridge : MonoBehaviour
{
    [Header("Integration Settings")]
    public bool enableUnity6HUD = true;
    public bool keepOriginalHUD = true;
    public bool enhanceExistingHUD = true;
    
    private HudController _hudController;
    private Unity6InGameHUD _unity6HUD;
    private bool _isInitialized = false;
    
    void Start()
    {
        InitializeHUDBridge();
    }
    
    void InitializeHUDBridge()
    {
        Debug.Log("[Unity6HUDBridge] 🔗 Initializing HUD bridge for Unity 6...");
        
        // Find existing HUD controller
        _hudController = FindObjectOfType<HudController>();
        if (_hudController != null)
        {
            Debug.Log("[Unity6HUDBridge] ✅ Found HudController - setting up integration");
            
            if (enhanceExistingHUD)
            {
                EnhanceExistingHUD();
            }
        }
        
        // Set up Unity 6 HUD overlay
        if (enableUnity6HUD)
        {
            SetupUnity6HUD();
        }
        
        _isInitialized = true;
        Debug.Log("[Unity6HUDBridge] ✅ HUD bridge initialized");
    }
    
    void EnhanceExistingHUD()
    {
        Debug.Log("[Unity6HUDBridge] 🔧 Enhancing existing HUD for Unity 6 compatibility...");
        
        // Ensure HUD controller is enabled
        if (_hudController != null)
        {
            _hudController.enabled = keepOriginalHUD;
            Debug.Log($"[Unity6HUDBridge] Original HUD Controller: {(keepOriginalHUD ? "Enabled" : "Disabled")}");
        }
        
        // Check for existing HUD components and ensure they work in Unity 6
        CheckHUDComponents();
    }
    
    void CheckHUDComponents()
    {
        // Check if key HUD singletons are available
        var components = new System.Collections.Generic.Dictionary<string, bool>
        {
            {"HpApHud", HpApHud.Instance != null},
            {"AmmoHud", AmmoHud.Instance != null}, 
            {"WeaponsHud", WeaponsHud.Instance != null},
            {"XpPtsHud", XpPtsHud.Instance != null},
            {"MatchStatusHud", MatchStatusHud.Instance != null},
            {"ReticleHud", ReticleHud.Instance != null}
        };
        
        foreach (var component in components)
        {
            string status = component.Value ? "✅" : "❌";
            Debug.Log($"[Unity6HUDBridge] {status} {component.Key}: {(component.Value ? "Available" : "Missing")}");
        }
    }
    
    void SetupUnity6HUD()
    {
        // Create Unity 6 HUD overlay if it doesn't exist
        _unity6HUD = FindObjectOfType<Unity6InGameHUD>();
        if (_unity6HUD == null)
        {
            GameObject hudGO = new GameObject("Unity6InGameHUD");
            _unity6HUD = hudGO.AddComponent<Unity6InGameHUD>();
            Debug.Log("[Unity6HUDBridge] ✅ Created Unity 6 HUD overlay");
        }
        else
        {
            Debug.Log("[Unity6HUDBridge] ✅ Found existing Unity 6 HUD overlay");
        }
    }
    
    void Update()
    {
        if (!_isInitialized) return;
        
        // Handle HUD switching
        if (Input.GetKeyDown(KeyCode.M))
        {
            SwitchHUDMode();
        }
        
        // Monitor HUD health
        MonitorHUDHealth();
    }
    
    void SwitchHUDMode()
    {
        keepOriginalHUD = !keepOriginalHUD;
        
        if (_hudController != null)
        {
            _hudController.enabled = keepOriginalHUD;
        }
        
        if (_unity6HUD != null)
        {
            _unity6HUD.enableHUD = !keepOriginalHUD || enhanceExistingHUD;
        }
        
        Debug.Log($"[Unity6HUDBridge] 🔄 Switched HUD mode - Original: {keepOriginalHUD}, Unity6: {_unity6HUD?.enableHUD}");
    }
    
    void MonitorHUDHealth()
    {
        // Check if original HUD components are still working
        if (_hudController != null && keepOriginalHUD)
        {
            if (!_hudController.enabled)
            {
                Debug.LogWarning("[Unity6HUDBridge] ⚠️ Original HUD Controller disabled unexpectedly");
            }
        }
    }
    
    void OnGUI()
    {
        if (!_isInitialized) return;
        
        // Show HUD status in corner
        DrawHUDStatus();
    }
    
    void DrawHUDStatus()
    {
        var statusRect = new Rect(Screen.width - 200, Screen.height - 100, 190, 90);
        GUI.color = new Color(1f, 1f, 1f, 0.7f);
        GUI.Box(statusRect, "Unity 6 HUD Status");
        
        GUI.color = Color.white;
        float y = statusRect.y + 20;
        
        string originalStatus = keepOriginalHUD ? "✅ Active" : "❌ Disabled";
        GUI.Label(new Rect(statusRect.x + 10, y, 170, 20), $"Original: {originalStatus}");
        y += 18;
        
        string unity6Status = (_unity6HUD?.enableHUD == true) ? "✅ Active" : "❌ Disabled";
        GUI.Label(new Rect(statusRect.x + 10, y, 170, 20), $"Unity 6: {unity6Status}");
        y += 18;
        
        GUI.Label(new Rect(statusRect.x + 10, y, 170, 20), "M - Switch Mode");
    }
    
    /// <summary>
    /// Public method to get HUD data for integration
    /// </summary>
    public HUDData GetCurrentHUDData()
    {
        var data = new HUDData();
        
        // Try to get data from original HUD components
        if (HpApHud.Instance != null)
        {
            // data.health = HpApHud.Instance.CurrentHealth; // When available
            data.health = 100; // Default for now
        }
        
        if (AmmoHud.Instance != null)
        {
            // data.ammo = AmmoHud.Instance.CurrentAmmo; // When available  
            data.ammo = 30; // Default for now
        }
        
        return data;
    }
}

/// <summary>
/// Data structure for HUD information sharing
/// </summary>
[System.Serializable]
public class HUDData
{
    public int health = 100;
    public int armor = 50;
    public int ammo = 30;
    public int totalAmmo = 120;
    public int level = 1;
    public int experience = 0;
    public string weaponName = "Machine Gun";
    public bool isAlive = true;
}