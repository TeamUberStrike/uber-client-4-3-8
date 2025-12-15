using UnityEngine;

/// <summary>
/// Simple scene navigation script to go from LevelSpaceShip to Menu
/// Press ESC to go to main menu
/// </summary>
public class SceneNavigator : MonoBehaviour
{
    [Header("Navigation Settings")]
    public bool enableKeyboardNavigation = true;
    public KeyCode menuKey = KeyCode.Escape;
    public PageType targetPage = PageType.Home;
    
    private bool _hasNavigated = false;
    
    void Start()
    {
        Debug.Log("[SceneNavigator] 🎮 Scene Navigator Ready!");
        Debug.Log($"[SceneNavigator] Press {menuKey} to go to {targetPage} page");
        
        // Auto-check what scene we're in
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneNavigator] Current scene: {sceneName}");
        
        // Check if we have required components
        CheckRequiredComponents();
    }
    
    void Update()
    {
        if (enableKeyboardNavigation && !_hasNavigated)
        {
            // ESC key to go to menu
            if (Input.GetKeyDown(menuKey))
            {
                NavigateToMenu();
            }
            
            // Alternative keys
            if (Input.GetKeyDown(KeyCode.M))
            {
                NavigateToMenu();
            }
            
            // Number keys for different pages
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                NavigateToPage(PageType.Home);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                NavigateToPage(PageType.Shop);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                NavigateToPage(PageType.Stats);
            }
        }
    }
    
    void NavigateToMenu()
    {
        NavigateToPage(targetPage);
    }
    
    void NavigateToPage(PageType page)
    {
        if (_hasNavigated)
        {
            Debug.LogWarning("[SceneNavigator] Already navigated, ignoring request");
            return;
        }
        
        _hasNavigated = true;
        
        Debug.Log($"[SceneNavigator] 🚀 Navigating to {page} page...");
        
        try
        {
            // Check if GameStateController exists
            var gameStateController = GameStateController.Instance;
            if (gameStateController != null)
            {
                Debug.Log("[SceneNavigator] ✅ Using GameStateController.UnloadLevelAndLoadPage");
                gameStateController.UnloadLevelAndLoadPage(page);
            }
            else
            {
                Debug.LogWarning("[SceneNavigator] ⚠️ GameStateController not found, trying MenuPageManager directly");
                
                // Fallback: Try MenuPageManager directly
                var menuManager = MenuPageManager.Instance;
                if (menuManager != null)
                {
                    Debug.Log("[SceneNavigator] ✅ Using MenuPageManager.LoadPage");
                    menuManager.LoadPage(page, true);
                }
                else
                {
                    Debug.LogError("[SceneNavigator] ❌ Neither GameStateController nor MenuPageManager available!");
                    _hasNavigated = false; // Allow retry
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneNavigator] ❌ Navigation failed: {ex.Message}");
            _hasNavigated = false; // Allow retry
        }
    }
    
    void CheckRequiredComponents()
    {
        // Check GameStateController
        var gameStateController = GameStateController.Instance;
        if (gameStateController != null)
        {
            Debug.Log("[SceneNavigator] ✅ GameStateController found");
        }
        else
        {
            Debug.LogWarning("[SceneNavigator] ⚠️ GameStateController not found");
        }
        
        // Check MenuPageManager
        var menuManager = MenuPageManager.Instance;
        if (menuManager != null)
        {
            Debug.Log("[SceneNavigator] ✅ MenuPageManager found");
        }
        else
        {
            Debug.LogWarning("[SceneNavigator] ⚠️ MenuPageManager not found");
        }
        
        // Check BackgroundMusicPlayer
        var musicPlayer = BackgroundMusicPlayer.Instance;
        if (musicPlayer != null)
        {
            Debug.Log("[SceneNavigator] ✅ BackgroundMusicPlayer found");
        }
        else
        {
            Debug.LogWarning("[SceneNavigator] ⚠️ BackgroundMusicPlayer not found");
        }
    }
    
    void OnGUI()
    {
        if (!_hasNavigated)
        {
            // Show navigation instructions
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            
            GUI.Label(new Rect(10, 10, 400, 30), $"Press {menuKey} or M to go to Menu", style);
            GUI.Label(new Rect(10, 40, 400, 30), "Press 1=Home, 2=Shop, 3=Stats", style);
            
            // Manual buttons
            if (GUI.Button(new Rect(10, 80, 120, 40), "Go to Menu"))
            {
                NavigateToMenu();
            }
            
            if (GUI.Button(new Rect(140, 80, 80, 40), "Home"))
            {
                NavigateToPage(PageType.Home);
            }
            
            if (GUI.Button(new Rect(230, 80, 80, 40), "Shop"))
            {
                NavigateToPage(PageType.Shop);
            }
            
            if (GUI.Button(new Rect(320, 80, 80, 40), "Stats"))
            {
                NavigateToPage(PageType.Stats);
            }
        }
    }
}