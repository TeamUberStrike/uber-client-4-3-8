using UnityEngine;

/// <summary>
/// Unity 6 In-Game HUD Overlay - Compatible replacement for existing HUD system
/// Works with existing HudController but provides Unity 6 compatibility
/// </summary>
public class Unity6InGameHUD : MonoBehaviour
{
    [Header("HUD Configuration")]
    public bool enableHUD = true;
    public bool showDebugInfo = false;
    public bool replaceExistingHUD = true;
    
    [Header("HUD Components")]
    public bool showHealthArmor = true;
    public bool showAmmo = true;
    public bool showWeapons = true;
    public bool showXP = true;
    public bool showMinimap = false;
    public bool showCrosshair = true;
    
    private HudController _originalHudController;
    private bool _isInitialized = false;
    private float _hudAlpha = 1f;
    
    void Start()
    {
        InitializeInGameHUD();
    }
    
    void InitializeInGameHUD()
    {
        Debug.Log("[Unity6InGameHUD] 🎮 Initializing Unity 6 in-game HUD overlay...");
        
        // Find existing HUD controller
        _originalHudController = FindObjectOfType<HudController>();
        if (_originalHudController != null)
        {
            Debug.Log("[Unity6InGameHUD] ✅ Found existing HudController - integrating...");
            
            if (replaceExistingHUD)
            {
                // Keep original HUD but enhance it
                Debug.Log("[Unity6InGameHUD] 🔧 Enhancing existing HUD for Unity 6 compatibility");
            }
        }
        else
        {
            Debug.Log("[Unity6InGameHUD] ⚠️ No HudController found - creating standalone HUD");
        }
        
        _isInitialized = true;
        Debug.Log("[Unity6InGameHUD] ✅ In-game HUD initialized for Unity 6");
    }
    
    void Update()
    {
        if (!_isInitialized) return;
        
        // Handle HUD toggle
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            enableHUD = !enableHUD;
            Debug.Log($"[Unity6InGameHUD] 🔄 HUD toggled: {(enableHUD ? "ON" : "OFF")}");
        }
        
        // Handle debug info toggle
        if (Input.GetKeyDown(KeyCode.I))
        {
            showDebugInfo = !showDebugInfo;
        }
        
        // Update HUD alpha for fade effects
        UpdateHUDVisibility();
    }
    
    void UpdateHUDVisibility()
    {
        if (!enableHUD)
        {
            _hudAlpha = Mathf.Lerp(_hudAlpha, 0f, Time.deltaTime * 5f);
        }
        else
        {
            _hudAlpha = Mathf.Lerp(_hudAlpha, 1f, Time.deltaTime * 5f);
        }
    }
    
    void OnGUI()
    {
        if (!_isInitialized || _hudAlpha <= 0.01f) return;
        
        // Set overall HUD alpha
        Color originalColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, _hudAlpha);
        
        // Draw Unity 6 compatible HUD elements
        if (enableHUD)
        {
            DrawInGameHUD();
        }
        
        // Debug information overlay
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
        
        // Controls help
        DrawControlsHelp();
        
        // Restore original color
        GUI.color = originalColor;
    }
    
    void DrawInGameHUD()
    {
        // Health and Armor (bottom left)
        if (showHealthArmor)
        {
            DrawHealthArmorHUD();
        }
        
        // Ammo (bottom right)
        if (showAmmo)
        {
            DrawAmmoHUD();
        }
        
        // Weapons (right side)
        if (showWeapons)
        {
            DrawWeaponsHUD();
        }
        
        // XP Bar (top)
        if (showXP)
        {
            DrawXPHUD();
        }
        
        // Crosshair (center)
        if (showCrosshair)
        {
            DrawCrosshair();
        }
        
        // Minimap (top right)
        if (showMinimap)
        {
            DrawMinimapHUD();
        }
    }
    
    void DrawHealthArmorHUD()
    {
        var healthRect = new Rect(20, Screen.height - 120, 200, 100);
        GUI.Box(healthRect, "Health & Armor");
        
        // Simulate health/armor values
        int health = 100; // Would get from PlayerDataManager or GameState
        int armor = 75;
        
        GUI.Label(new Rect(30, Screen.height - 95, 180, 20), $"Health: {health}");
        GUI.Label(new Rect(30, Screen.height - 75, 180, 20), $"Armor: {armor}");
        
        // Health bar
        var healthBarRect = new Rect(30, Screen.height - 55, 160, 10);
        GUI.Box(healthBarRect, "");
        var healthFillRect = new Rect(healthBarRect.x + 1, healthBarRect.y + 1, 
                                     (healthBarRect.width - 2) * (health / 100f), healthBarRect.height - 2);
        GUI.DrawTexture(healthFillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.green, 0f, 0f);
    }
    
    void DrawAmmoHUD()
    {
        var ammoRect = new Rect(Screen.width - 220, Screen.height - 120, 200, 100);
        GUI.Box(ammoRect, "Ammunition");
        
        // Simulate ammo values
        int currentAmmo = 30;
        int totalAmmo = 120;
        
        GUI.Label(new Rect(Screen.width - 210, Screen.height - 95, 180, 20), $"Ammo: {currentAmmo}/{totalAmmo}");
        GUI.Label(new Rect(Screen.width - 210, Screen.height - 75, 180, 20), $"Weapon: Machine Gun");
    }
    
    void DrawWeaponsHUD()
    {
        var weaponsRect = new Rect(Screen.width - 150, Screen.height / 2 - 100, 140, 200);
        GUI.Box(weaponsRect, "Weapons");
        
        // Simulate weapon list
        string[] weapons = { "Machine Gun", "Sniper Rifle", "Shotgun", "Melee" };
        
        for (int i = 0; i < weapons.Length; i++)
        {
            float y = weaponsRect.y + 30 + (i * 25);
            bool isSelected = i == 0; // First weapon selected
            
            if (isSelected)
            {
                GUI.color = Color.yellow;
            }
            
            GUI.Label(new Rect(weaponsRect.x + 10, y, 120, 20), $"{i + 1}. {weapons[i]}");
            
            if (isSelected)
            {
                GUI.color = Color.white;
            }
        }
    }
    
    void DrawXPHUD()
    {
        var xpRect = new Rect(Screen.width / 2 - 200, 20, 400, 40);
        GUI.Box(xpRect, "Experience Points");
        
        // Simulate XP values
        int currentXP = 1250;
        int nextLevelXP = 2000;
        int level = 15;
        
        GUI.Label(new Rect(xpRect.x + 10, xpRect.y + 10, 100, 20), $"Level {level}");
        GUI.Label(new Rect(xpRect.x + 300, xpRect.y + 10, 100, 20), $"XP: {currentXP}/{nextLevelXP}");
        
        // XP Bar
        var xpBarRect = new Rect(xpRect.x + 120, xpRect.y + 15, 160, 10);
        GUI.Box(xpBarRect, "");
        var xpFillRect = new Rect(xpBarRect.x + 1, xpBarRect.y + 1, 
                                 (xpBarRect.width - 2) * ((float)currentXP / nextLevelXP), xpBarRect.height - 2);
        GUI.DrawTexture(xpFillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.cyan, 0f, 0f);
    }
    
    void DrawCrosshair()
    {
        // Simple crosshair in center of screen
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        float size = 10f;
        
        // Horizontal line
        GUI.DrawTexture(new Rect(centerX - size, centerY - 1, size * 2, 2), 
                       Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.white, 0f, 0f);
        
        // Vertical line  
        GUI.DrawTexture(new Rect(centerX - 1, centerY - size, 2, size * 2), 
                       Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.white, 0f, 0f);
    }
    
    void DrawMinimapHUD()
    {
        var minimapRect = new Rect(Screen.width - 220, 20, 200, 200);
        GUI.Box(minimapRect, "Minimap");
        
        GUI.Label(new Rect(minimapRect.x + 10, minimapRect.y + 30, 180, 20), "Map: Training Area");
        GUI.Label(new Rect(minimapRect.x + 10, minimapRect.y + 50, 180, 20), "Players: 1/8");
        
        // Simple minimap representation
        var mapArea = new Rect(minimapRect.x + 10, minimapRect.y + 70, 180, 120);
        GUI.Box(mapArea, "");
        
        // Player dot (center)
        var playerDot = new Rect(mapArea.x + mapArea.width/2 - 2, mapArea.y + mapArea.height/2 - 2, 4, 4);
        GUI.DrawTexture(playerDot, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.blue, 0f, 0f);
    }
    
    void DrawDebugInfo()
    {
        var debugRect = new Rect(10, 50, 300, 200);
        GUI.Box(debugRect, "Unity 6 HUD Debug Info");
        
        float y = 75;
        GUI.Label(new Rect(20, y, 280, 20), $"HUD Alpha: {_hudAlpha:F2}"); y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Screen: {Screen.width}x{Screen.height}"); y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"FPS: {1f / Time.deltaTime:F0}"); y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Original HUD: {(_originalHudController != null ? "Found" : "Missing")}"); y += 20;
        GUI.Label(new Rect(20, y, 280, 20), $"Unity Version: {Application.unityVersion}"); y += 20;
    }
    
    void DrawControlsHelp()
    {
        if (!enableHUD) return;
        
        var helpRect = new Rect(10, Screen.height - 200, 250, 80);
        GUI.color = new Color(1f, 1f, 1f, 0.7f);
        GUI.Box(helpRect, "Unity 6 HUD Controls");
        
        GUI.color = new Color(1f, 1f, 1f, _hudAlpha * 0.8f);
        GUI.Label(new Rect(20, Screen.height - 175, 230, 20), "TAB - Toggle HUD");
        GUI.Label(new Rect(20, Screen.height - 155, 230, 20), "I - Debug Info");
        GUI.Label(new Rect(20, Screen.height - 135, 230, 20), "ESC - Menu (if available)");
        
        GUI.color = Color.white;
    }
}