using UnityEngine;

/// <summary>
/// Unity 6 Always On HUD - Simple, always visible HUD that requires no input
/// This will definitely be visible if OnGUI is working at all
/// </summary>
public class Unity6AlwaysOnHUD : MonoBehaviour
{
    private float _time = 0f;
    private int _frameCount = 0;
    
    void Start()
    {
        Debug.Log("[Unity6AlwaysOnHUD] 🔥 ALWAYS ON HUD STARTED - This should be visible!");
    }
    
    void Update()
    {
        _time += Time.deltaTime;
        _frameCount++;
    }
    
    void OnGUI()
    {
        // Debug: Verify OnGUI is being called
        if (_frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[Unity6AlwaysOnHUD] OnGUI called - Frame {_frameCount}, Screen {Screen.width}x{Screen.height}");
        }
        
        // Force to be on absolute top
        GUI.depth = -10000;
        
        // Bright, unmissable display
        DrawAlwaysOnHUD();
    }
    
    void DrawAlwaysOnHUD()
    {
        // Very bright background
        GUI.backgroundColor = Color.magenta;
        GUI.color = Color.white;
        
        // Large box across top of screen
        var topRect = new Rect(0, 0, Screen.width, 80);
        GUI.Box(topRect, "");
        
        // Large white text on bright background
        var titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 32;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        
        GUI.Label(topRect, "🎮 UNITY 6 HUD IS WORKING! 🎮", titleStyle);
        
        // Info bar
        var infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize = 16;
        infoStyle.normal.textColor = Color.yellow;
        infoStyle.alignment = TextAnchor.MiddleCenter;
        
        var infoRect = new Rect(0, 40, Screen.width, 30);
        GUI.Label(infoRect, $"Time: {_time:F1}s | Frames: {_frameCount} | Screen: {Screen.width}x{Screen.height}", infoStyle);
        
        // Corner indicators
        DrawCornerIndicators();
        
        // Center crosshair
        DrawCenterCrosshair();
        
        // Reset colors
        GUI.backgroundColor = Color.white;
        GUI.color = Color.white;
    }
    
    void DrawCornerIndicators()
    {
        var cornerStyle = new GUIStyle(GUI.skin.box);
        cornerStyle.normal.textColor = Color.black;
        cornerStyle.fontSize = 14;
        cornerStyle.fontStyle = FontStyle.Bold;
        
        // Top-left corner
        GUI.backgroundColor = Color.red;
        var tlRect = new Rect(10, 90, 100, 50);
        GUI.Box(tlRect, "TOP LEFT\nHP: 100");
        
        // Top-right corner
        GUI.backgroundColor = Color.blue;
        var trRect = new Rect(Screen.width - 110, 90, 100, 50);
        GUI.Box(trRect, "TOP RIGHT\nAmmo: 30");
        
        // Bottom-left corner
        GUI.backgroundColor = Color.green;
        var blRect = new Rect(10, Screen.height - 60, 100, 50);
        GUI.Box(blRect, "BOT LEFT\nArmor: 50");
        
        // Bottom-right corner
        GUI.backgroundColor = Color.yellow;
        var brRect = new Rect(Screen.width - 110, Screen.height - 60, 100, 50);
        GUI.Box(brRect, "BOT RIGHT\nXP: 1250");
        
        GUI.backgroundColor = Color.white;
    }
    
    void DrawCenterCrosshair()
    {
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        float size = 25f;
        
        // Very bright crosshair
        var crossColor = Color.red;
        
        // Thick horizontal line
        var hRect = new Rect(centerX - size, centerY - 3, size * 2, 6);
        GUI.DrawTexture(hRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, crossColor, 0f, 0f);
        
        // Thick vertical line
        var vRect = new Rect(centerX - 3, centerY - size, 6, size * 2);
        GUI.DrawTexture(vRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, crossColor, 0f, 0f);
        
        // Center circle
        var circleRect = new Rect(centerX - 8, centerY - 8, 16, 16);
        GUI.DrawTexture(circleRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.white, 0f, 0f);
        
        // Center dot
        var dotRect = new Rect(centerX - 2, centerY - 2, 4, 4);
        GUI.DrawTexture(dotRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.black, 0f, 0f);
    }
}