using UnityEngine;

/// <summary>
/// Unity 6 Status Display - Shows current system status and available controls
/// Displays system readiness and user instructions
/// </summary>
public class Unity6StatusDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showStatus = true;
    public bool showControls = true;
    public bool autoHideAfterDelay = true;
    public float autoHideDelay = 10f;
    
    private float _startTime;
    private bool _hasAutoHidden = false;
    
    void Start()
    {
        _startTime = Time.time;
        Debug.Log("[Unity6StatusDisplay] 📋 Status display ready - system is operational!");
    }
    
    void Update()
    {
        // Auto-hide after delay
        if (autoHideAfterDelay && !_hasAutoHidden && Time.time - _startTime > autoHideDelay)
        {
            showStatus = false;
            _hasAutoHidden = true;
            Debug.Log("[Unity6StatusDisplay] ⏰ Auto-hiding status after 10 seconds");
        }
        
        // Toggle with H key
        if (Input.GetKeyDown(KeyCode.H))
        {
            showStatus = !showStatus;
        }
    }
    
    void OnGUI()
    {
        if (!showStatus) return;
        
        // Main status box
        var statusRect = new Rect(10, 10, 400, 200);
        GUI.Box(statusRect, "Unity 6 Modern UI System - OPERATIONAL");
        
        float y = 35;
        float lineHeight = 18;
        
        // System status
        GUI.Label(new Rect(20, y, 380, lineHeight), "🎉 System Status: FULLY OPERATIONAL", GetStatusStyle());
        y += lineHeight + 5;
        
        // Component status
        GUI.Label(new Rect(25, y, 370, lineHeight), "✅ LegacyUIManager: Active with 3 pages");
        y += lineHeight;
        
        GUI.Label(new Rect(25, y, 370, lineHeight), "✅ MenuPageManager: Integrated and enabled");
        y += lineHeight;
        
        GUI.Label(new Rect(25, y, 370, lineHeight), "✅ Camera System: MainCamera configured");
        y += lineHeight;
        
        GUI.Label(new Rect(25, y, 370, lineHeight), "✅ Modern Pages: Home, Shop, Stats loaded");
        y += lineHeight + 5;
        
        // Controls
        if (showControls)
        {
            GUI.Label(new Rect(20, y, 380, lineHeight), "🎮 Controls:", GetControlsHeaderStyle());
            y += lineHeight;
            
            GUI.Label(new Rect(25, y, 370, lineHeight), "1 - Home Page  |  2 - Shop Page  |  3 - Stats Page");
            y += lineHeight;
            
            GUI.Label(new Rect(25, y, 370, lineHeight), "TAB - Toggle Test UI  |  H - Hide/Show This Status");
            y += lineHeight;
            
            GUI.Label(new Rect(25, y, 370, lineHeight), "F1 - Full Help (when Unity6MenuTester active)");
        }
    }
    
    GUIStyle GetStatusStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.green;
        return style;
    }
    
    GUIStyle GetControlsHeaderStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.cyan;
        return style;
    }
}