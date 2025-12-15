using UnityEngine;

/// <summary>
/// Ultra-simple OnGUI test - just draws a red box
/// If this doesn't show up, OnGUI is completely broken
/// </summary>
public class SuperSimpleOnGUITest : MonoBehaviour
{
    private int frameCount = 0;
    
    void Start()
    {
        Debug.Log("[SuperSimpleOnGUITest] 🚀 Ultra simple OnGUI test started!");
        
        // Ensure this GameObject stays alive
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        frameCount++;
    }
    
    void OnGUI()
    {
        // Log periodically to confirm OnGUI is being called
        if (frameCount % 120 == 0) // Every 2 seconds at 60 FPS
        {
            Debug.Log($"[SuperSimpleOnGUITest] OnGUI called at frame {frameCount}");
        }
        
        // Draw the simplest possible GUI element
        GUI.Box(new Rect(10, 10, 200, 100), "RED BOX TEST");
        
        // Draw a bright red rectangle using DrawTexture
        var redRect = new Rect(50, 200, 300, 100);
        GUI.DrawTexture(redRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.red, 0f, 0f);
        
        // Simple label
        GUI.Label(new Rect(60, 230, 280, 40), "THIS IS A SIMPLE GUI TEST");
    }
}