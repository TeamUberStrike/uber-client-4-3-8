using UnityEngine;

/// <summary>
/// Ultra Simple Menu Navigator - No external dependencies
/// Just basic Unity scene loading functionality
/// </summary>
public class UltraSimpleMenuNav : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🎮 ULTRA SIMPLE MENU NAV READY - Press ESC to go to menu!");
        DontDestroyOnLoad(gameObject);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
        {
            GoToMenu();
        }
    }
    
    void GoToMenu()
    {
        Debug.Log("🚀 Attempting to load Latest scene...");
        
        // Try the most basic scene loading
        try
        {
            Application.LoadLevel("Latest");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load Latest scene: {ex.Message}");
            
            // Try alternative scene names
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Latest");
            }
            catch (System.Exception ex2)
            {
                Debug.LogError($"SceneManager also failed: {ex2.Message}");
            }
        }
    }
    
    void OnGUI()
    {
        // Simple GUI that definitely works
        if (GUI.Button(new Rect(10, 10, 200, 60), "🏠 GO TO MENU"))
        {
            GoToMenu();
        }
        
        GUI.Label(new Rect(10, 80, 300, 30), "Press ESC or M key to go to menu");
    }
}