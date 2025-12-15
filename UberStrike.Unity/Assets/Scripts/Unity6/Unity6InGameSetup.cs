using UnityEngine;
using System.Collections;

/// <summary>
/// Unity 6 In-Game Setup - Automatically sets up Unity 6 compatible HUD in 3D scenes
/// Detects when player is in-game and activates appropriate HUD system
/// </summary>
public class Unity6InGameSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool autoDetectInGame = true;
    public bool createHUDOnStart = true;
    public bool showSetupLogs = true;
    
    [Header("HUD Components")]
    public bool enableInGameHUD = true;
    public bool enableHUDBridge = true;
    
    private bool _isInGame = false;
    private bool _setupComplete = false;
    
    void Start()
    {
        if (autoDetectInGame)
        {
            StartCoroutine(DetectGameState());
        }
        else if (createHUDOnStart)
        {
            SetupInGameHUD();
        }
    }
    
    IEnumerator DetectGameState()
    {
        // Wait a moment for game systems to initialize
        yield return new WaitForSeconds(1f);
        
        if (showSetupLogs)
        {
            Debug.Log("[Unity6InGameSetup] 🔍 Detecting game state...");
        }
        
        // Check if we're in a 3D game scene
        _isInGame = DetectInGameState();
        
        if (_isInGame)
        {
            if (showSetupLogs)
            {
                Debug.Log("[Unity6InGameSetup] 🎮 In-game detected - setting up Unity 6 HUD...");
            }
            SetupInGameHUD();
        }
        else
        {
            if (showSetupLogs)
            {
                Debug.Log("[Unity6InGameSetup] 📋 Menu state detected - no in-game HUD needed");
            }
        }
    }
    
    bool DetectInGameState()
    {
        // Multiple ways to detect if we're in-game
        
        // 1. Check for HudController (indicates in-game)
        var hudController = FindObjectOfType<HudController>();
        if (hudController != null)
        {
            if (showSetupLogs)
            {
                Debug.Log("[Unity6InGameSetup] ✅ Found HudController - in 3D game mode");
            }
            return true;
        }
        
        // 2. Check for FPS camera or player controller
        var fpsCamera = Camera.main;
        if (fpsCamera != null && fpsCamera.transform.position.y > 0.5f) // Likely player camera height
        {
            if (showSetupLogs)
            {
                Debug.Log("[Unity6InGameSetup] ✅ Found elevated camera - likely in-game");
            }
            return true;
        }
        
        // 3. Check for game objects that indicate 3D gameplay
        var gameObjects = FindObjectsOfType<GameObject>();
        foreach (var go in gameObjects)
        {
            if (go.name.Contains("Player") || go.name.Contains("Character") || 
                go.name.Contains("Gun") || go.name.Contains("Weapon"))
            {
                if (showSetupLogs)
                {
                    Debug.Log($"[Unity6InGameSetup] ✅ Found game object: {go.name} - likely in-game");
                }
                return true;
            }
        }
        
        // 4. Check for specific UberStrike components (singleton instances)
        // Note: These are singleton classes, not UnityEngine.Object components
        try
        {
            if (WeaponsHud.Instance != null || 
                AmmoHud.Instance != null ||
                HpApHud.Instance != null)
            {
                if (showSetupLogs)
                {
                    Debug.Log("[Unity6InGameSetup] ✅ Found UberStrike HUD singletons - in-game");
                }
                return true;
            }
        }
        catch
        {
            // Singletons might not be initialized yet, that's okay
        }
        
        return false;
    }
    
    void SetupInGameHUD()
    {
        if (_setupComplete) return;
        
        if (showSetupLogs)
        {
            Debug.Log("[Unity6InGameSetup] 🔧 Setting up Unity 6 in-game HUD components...");
        }
        
        // Create HUD Bridge
        if (enableHUDBridge)
        {
            var hudBridge = FindObjectOfType<Unity6HUDBridge>();
            if (hudBridge == null)
            {
                GameObject bridgeGO = new GameObject("Unity6HUDBridge");
                bridgeGO.AddComponent<Unity6HUDBridge>();
                
                if (showSetupLogs)
                {
                    Debug.Log("[Unity6InGameSetup] ✅ Created Unity6HUDBridge");
                }
            }
        }
        
        // Create In-Game HUD overlay
        if (enableInGameHUD)
        {
            var inGameHUD = FindObjectOfType<Unity6InGameHUD>();
            if (inGameHUD == null)
            {
                GameObject hudGO = new GameObject("Unity6InGameHUD");
                hudGO.AddComponent<Unity6InGameHUD>();
                
                if (showSetupLogs)
                {
                    Debug.Log("[Unity6InGameSetup] ✅ Created Unity6InGameHUD");
                }
            }
        }
        
        // Create Unity6CanvasHUD for proper Unity 6 rendering (OnGUI is deprecated)
        var canvasHUD = FindObjectOfType<Unity6CanvasHUD>();
        if (canvasHUD == null)
        {
            GameObject canvasGO = new GameObject("Unity6CanvasHUDManager");
            canvasGO.AddComponent<Unity6CanvasHUD>();
            DontDestroyOnLoad(canvasGO);
            
            if (showSetupLogs)
            {
                Debug.Log("[Unity6InGameSetup] 🎮 Created Unity6CanvasHUD - Proper Unity 6 UI!");
            }
        }
        
        // Create SceneNavigator for easy navigation back to menu
        var sceneNavigator = FindObjectOfType<SceneNavigator>();
        if (sceneNavigator == null)
        {
            GameObject navGO = new GameObject("Unity6SceneNavigator");
            navGO.AddComponent<SceneNavigator>();
            DontDestroyOnLoad(navGO);
            
            if (showSetupLogs)
            {
                Debug.Log("[Unity6InGameSetup] 🧭 Created SceneNavigator - Press ESC to go to Menu!");
            }
        }
        
        _setupComplete = true;
        
        if (showSetupLogs)
        {
            Debug.Log("[Unity6InGameSetup] 🎉 Unity 6 Canvas-based HUD setup complete!");
            Debug.Log("[Unity6InGameSetup] Controls: TAB=Toggle HUD, I=Debug, C=Crosshair, ESC=Menu");
        }
    }
    
    void Update()
    {
        // Allow manual setup trigger
        if (Input.GetKeyDown(KeyCode.H) && !_setupComplete)
        {
            if (showSetupLogs)
            {
                Debug.Log("[Unity6InGameSetup] 🔄 Manual HUD setup triggered");
            }
            SetupInGameHUD();
        }
    }
    
    void OnGUI()
    {
        if (!_setupComplete && !autoDetectInGame)
        {
            // Show manual setup option
            var setupRect = new Rect(10, 10, 300, 80);
            GUI.Box(setupRect, "Unity 6 In-Game Setup");
            
            GUI.Label(new Rect(20, 35, 280, 20), "Press F9 to setup Unity 6 HUD");
            GUI.Label(new Rect(20, 55, 280, 20), "Or enable Auto Detect In Game");
        }
        else if (_setupComplete)
        {
            // Show completion status briefly
            if (Time.time < 5f) // Show for first 5 seconds
            {
                var statusRect = new Rect(10, 10, 250, 60);
                GUI.color = new Color(0f, 1f, 0f, 0.8f);
                GUI.Box(statusRect, "Unity 6 HUD Ready!");
                
                GUI.color = Color.white;
                GUI.Label(new Rect(20, 35, 230, 20), "F2/F3/F4 for HUD controls");
            }
        }
    }
}