using UnityEngine;

/// <summary>
/// Unity 6 HUD Force Display - Ensures HUD is always visible for testing
/// Simple, always-on HUD overlay that's impossible to miss
/// </summary>
public class Unity6HUDForceDisplay : MonoBehaviour
{
    [Header("Force Display Settings")]
    public bool alwaysVisible = true;
    public bool largeText = true;
    public bool brightColors = true;
    
    private float _time = 0f;
    private bool _showStatus = true;
    
    void Start()
    {
        Debug.Log("[Unity6HUDForceDisplay] 🔥 FORCE DISPLAY HUD STARTED - You should see this!");
    }
    
    void Update()
    {
        _time += Time.deltaTime;
        
        // Toggle with any key
        if (Input.anyKeyDown)
        {
            _showStatus = !_showStatus;
            Debug.Log($"[Unity6HUDForceDisplay] Key pressed - HUD visibility: {_showStatus}");
        }
    }
    
    void OnGUI()
    {
        if (!alwaysVisible && !_showStatus) return;
        
        // Force GUI to be on top
        GUI.depth = -1000;
        
        // Large, bright, unmissable HUD
        DrawForceHUD();
    }
    
    void DrawForceHUD()
    {
        // Use very bright colors and large text
        var originalColor = GUI.color;
        var originalBackgroundColor = GUI.backgroundColor;
        
        // Bright cyan background
        GUI.backgroundColor = Color.cyan;
        GUI.color = Color.black;
        
        // Large HUD in center-top
        var hudRect = new Rect(Screen.width / 2 - 300, 50, 600, 150);
        GUI.Box(hudRect, "UNITY 6 HUD - WORKING!");
        
        // Large text
        var style = new GUIStyle(GUI.skin.label);
        if (largeText)
        {
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;
        }
        
        if (brightColors)
        {
            style.normal.textColor = Color.red;
        }
        
        float y = hudRect.y + 30;
        GUI.Label(new Rect(hudRect.x + 20, y, 560, 30), "🎮 UNITY 6 IN-GAME HUD ACTIVE", style);
        
        y += 35;
        style.fontSize = 16;
        style.normal.textColor = Color.blue;
        GUI.Label(new Rect(hudRect.x + 20, y, 560, 25), $"⏰ Time: {_time:F1}s | Camera: {Camera.main?.name ?? "None"}", style);
        
        y += 25;
        GUI.Label(new Rect(hudRect.x + 20, y, 560, 25), "🎯 TAB=Toggle | I=Debug | M=Switch | Any Key=Hide This", style);
        
        // Health/Ammo simulation in corners
        DrawCornerHUD();
        
        // Restore colors
        GUI.color = originalColor;
        GUI.backgroundColor = originalBackgroundColor;
    }
    
    void DrawCornerHUD()
    {
        var cornerStyle = new GUIStyle(GUI.skin.box);
        cornerStyle.fontSize = 16;
        cornerStyle.normal.textColor = Color.white;
        
        // Bottom left - Health
        var healthRect = new Rect(10, Screen.height - 80, 150, 70);
        GUI.backgroundColor = Color.red;
        GUI.Box(healthRect, "HEALTH");
        GUI.backgroundColor = Color.white;
        GUI.Label(new Rect(20, Screen.height - 55, 130, 20), "❤️ Health: 100");
        GUI.Label(new Rect(20, Screen.height - 35, 130, 20), "🛡️ Armor: 50");
        
        // Bottom right - Ammo  
        var ammoRect = new Rect(Screen.width - 160, Screen.height - 80, 150, 70);
        GUI.backgroundColor = Color.blue;
        GUI.Box(ammoRect, "AMMO");
        GUI.backgroundColor = Color.white;
        GUI.Label(new Rect(Screen.width - 150, Screen.height - 55, 130, 20), "🔫 Ammo: 30/120");
        GUI.Label(new Rect(Screen.width - 150, Screen.height - 35, 130, 20), "⚡ Weapon: MG");
        
        // Center crosshair
        DrawBigCrosshair();
    }
    
    void DrawBigCrosshair()
    {
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        float size = 20f;
        
        // Bright green crosshair
        var crosshairColor = Color.green;
        
        // Horizontal line
        var horizontalRect = new Rect(centerX - size, centerY - 2, size * 2, 4);
        GUI.DrawTexture(horizontalRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, crosshairColor, 0f, 0f);
        
        // Vertical line  
        var verticalRect = new Rect(centerX - 2, centerY - size, 4, size * 2);
        GUI.DrawTexture(verticalRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, crosshairColor, 0f, 0f);
        
        // Center dot
        var centerDot = new Rect(centerX - 3, centerY - 3, 6, 6);
        GUI.DrawTexture(centerDot, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.yellow, 0f, 0f);
    }
}