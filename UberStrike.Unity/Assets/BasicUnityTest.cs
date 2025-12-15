using UnityEngine;

/// <summary>
/// Simple test to verify basic Unity systems are working
/// Shows visible text and responds to input
/// </summary>
public class BasicUnityTest : MonoBehaviour
{
    private float time = 0f;
    private int frameCount = 0;
    private bool keyPressed = false;
    
    void Start()
    {
        Debug.Log("🧪 BASIC UNITY TEST STARTED!");
        Debug.Log("You should see white text on screen if Unity is working");
        
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        time += Time.deltaTime;
        frameCount++;
        
        // Test input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            keyPressed = true;
            Debug.Log("✅ SPACEBAR DETECTED - Input system working!");
        }
    }
    
    void OnGUI()
    {
        // Test if GUI rendering works at all
        GUI.Label(new Rect(200, 100, 400, 30), "🧪 BASIC UNITY TEST - Can you see this text?");
        GUI.Label(new Rect(200, 130, 400, 30), $"Time: {time:F1}s | Frames: {frameCount}");
        GUI.Label(new Rect(200, 160, 400, 30), keyPressed ? "✅ Input working!" : "Press SPACEBAR to test input");
        
        // Big test button
        if (GUI.Button(new Rect(200, 200, 200, 50), "CLICK ME TO TEST"))
        {
            Debug.Log("✅ BUTTON CLICKED - GUI system working!");
        }
    }
}