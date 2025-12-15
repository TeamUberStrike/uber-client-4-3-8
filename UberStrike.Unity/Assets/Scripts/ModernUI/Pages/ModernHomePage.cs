using UnityEngine;
using System.Collections;

/// <summary>
/// Unity 6 Modern HomePage - Converted from HomePageGUI
/// Uses OnGUI-based rendering through our LegacyUIManager system
/// </summary>
public class ModernHomePage : UIPage
{
    [Header("Home Page Configuration")]
    public bool enableMainMenu = true;
    public float mainMenuAnimationTime = 0.25f;
    
    private float _mainMenuX = -250f;
    private bool _hasCheckedPerformance = false;
    private bool _isAnimating = false;
    
    // Constants from original
    private const int PromotionWidth = 320;
    private const float PromoTextureAspect = 323 / 430f;
    
    protected override void Start()
    {
        base.Start();
        
        // Set page configuration
        pageType = PageType.Home;
        pageName = "Home Page";
        
        // Initialize home page
        InitializeHomePage();
    }
    
    void InitializeHomePage()
    {
        Debug.Log("[ModernHomePage] Initializing Unity 6 modern home page...");
        
        // Performance check (from original)
        if (!_hasCheckedPerformance)
        {
            var performanceTest = FindObjectOfType<PerformanceTest>();
            if (performanceTest != null)
            {
                performanceTest.enabled = true;
            }
            _hasCheckedPerformance = true;
        }
        
        // Start background music
        // var backgroundMusic = FindObjectOfType<BackgroundMusicPlayer>(); // Not a UnityEngine.Object
        // if (backgroundMusic != null)
        // {
        //     backgroundMusic.Play(); // Uncomment when BackgroundMusicPlayer is available
        // }
        
        // Update avatar
        // var avatarBuilder = FindObjectOfType<AvatarBuilder>(); // Not a UnityEngine.Object
        // if (avatarBuilder != null)
        // {
        //     avatarBuilder.UpdateLocalAvatar(); // Uncomment when AvatarBuilder is available
        // }
    }
    
    protected override void OnShowStart()
    {
        base.OnShowStart();
        
        // Start main menu animation
        if (enableMainMenu && !_isAnimating)
        {
            StartCoroutine(AnimateMainMenu());
        }
        
        // Set avatar position and rotation
        SetupAvatarForHomePage();
    }
    
    void SetupAvatarForHomePage()
    {
        // Avatar setup from original HomePageScene
        var menuConfig = FindObjectOfType<MenuConfiguration>();
        if (menuConfig != null && GameState.LocalDecorator != null)
        {
            Vector3 position;
            Quaternion rotation;
            if (menuConfig.GetPageAnchorPoint(PageType.Home, out position, out rotation))
            {
                GameState.LocalDecorator.SetPosition(position, rotation);
            }
            
            GameState.LocalDecorator.HideWeapons();
        }
        
        // Reset animation state
        var animationManager = FindObjectOfType<AvatarAnimationManager>();
        if (animationManager != null)
        {
            animationManager.ResetAnimationState(PageType.Home);
        }
    }
    
    IEnumerator AnimateMainMenu()
    {
        _isAnimating = true;
        float t = 0f;
        float startX = -250f;
        float endX = 0f;
        
        while (t < mainMenuAnimationTime)
        {
            t += Time.deltaTime;
            float progress = (t / mainMenuAnimationTime);
            _mainMenuX = Mathf.Lerp(startX, endX, progress * progress); // Ease-in effect
            yield return null;
        }
        
        _mainMenuX = endX;
        _isAnimating = false;
    }
    
    protected override void RenderPageContent()
    {
        // Use OnGUI for rendering (called by LegacyUIManager)
        DrawModernHomePage();
    }
    
    void DrawModernHomePage()
    {
        // Draw main menu if enabled
        if (enableMainMenu)
        {
            GUI.enabled = true; // Enable for now, was: !PanelManager.IsAnyPanelOpen
            
            DrawMainMenu();
            DrawWeeklySpecial();
            
            GUI.enabled = true;
        }
    }
    
    void DrawMainMenu()
    {
        int buttonCount = 4; // Play, Shop, Training, Quit
        int buttonSpacing = 59 + 8;
        int topOffset = 14;
        int height = topOffset + (buttonSpacing * buttonCount);
        
        // Calculate center position
        int top = Mathf.RoundToInt((((Screen.height - 60) * 0.5f) + 60) - (height * 0.5f));
        
        GUI.BeginGroup(new Rect(_mainMenuX, top, 310, height));
        {
            GUI.color = Color.white;
            
            // Play Button
            if (DrawMainMenuButton(new Vector2(0, topOffset), "PLAY", "Join the battle!", new Vector2(6, -14)))
            {
                Debug.Log("[ModernHomePage] Play button clicked");
                HandlePlayButtonClick();
            }
            
            // Shop Button  
            if (DrawMainMenuButton(new Vector2(0, topOffset + buttonSpacing), "GUNS N STUFF", "Visit the shop", new Vector2(6, -5)))
            {
                Debug.Log("[ModernHomePage] Shop button clicked");
                NavigateToPage(PageType.Shop);
            }
            
            // Training Button
            if (DrawMainMenuButton(new Vector2(0, topOffset + (buttonSpacing * 2)), "TRAINING", "Practice mode", new Vector2(6, -5)))
            {
                Debug.Log("[ModernHomePage] Training button clicked");
                NavigateToPage(PageType.Training);
            }
            
            // Quit Button
            if (DrawMainMenuButton(new Vector2(0, topOffset + (buttonSpacing * 3)), "QUIT", "Exit the game", new Vector2(6, -4)))
            {
                Debug.Log("[ModernHomePage] Quit button clicked");
                HandleQuitButtonClick();
            }
            
            GUI.color = Color.white;
        }
        GUI.EndGroup();
    }
    
    bool DrawMainMenuButton(Vector2 position, string text, string tooltip, Vector2 iconPosition)
    {
        // Draw main menu button using OnGUI
        var rect = new Rect(position.x, position.y, 310, 59);
        
        // Custom button style for main menu
        var originalSkin = GUI.skin;
        var buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;
        
        bool clicked = GUI.Button(rect, text, buttonStyle);
        
        // Show tooltip on hover
        if (rect.Contains(Event.current.mousePosition))
        {
            var tooltipRect = new Rect(Event.current.mousePosition.x + 10, Event.current.mousePosition.y - 20, 200, 25);
            GUI.Label(tooltipRect, tooltip, GUI.skin.box);
        }
        
        return clicked;
    }
    
    void DrawWeeklySpecial()
    {
        // Simplified weekly special for now
        float textureHeight = PromoTextureAspect * PromotionWidth;
        float height = 28 + textureHeight + 58;
        Rect rect = new Rect(Screen.width - PromotionWidth - 20, 100, PromotionWidth, height);
        
        GUI.BeginGroup(rect, "Weekly Special", GUI.skin.box);
        {
            // Title
            GUI.Label(new Rect(10, 25, rect.width - 20, 20), "Featured Item", GUI.skin.label);
            
            // Placeholder for promotion content
            GUI.Label(new Rect(10, 50, rect.width - 20, 60), 
                "Weekly special items will be displayed here.\\n" +
                "This is converted from the original OnGUI system.", 
                GUI.skin.textArea);
        }
        GUI.EndGroup();
    }
    
    void HandlePlayButtonClick()
    {
        // Handle play button logic
        if (PlayerDataManager.IsPlayerLoggedIn)
        {
            // Join fastest server
            // var gameServerController = FindObjectOfType<GameServerController>(); // Not a UnityEngine.Object
            // if (gameServerController != null)
            // {
            //     gameServerController.JoinFastestServer(); // Uncomment when available
                Debug.Log("[ModernHomePage] Would join fastest server");
            // }
        }
        else
        {
            // Go to training
            NavigateToPage(PageType.Training);
        }
    }
    
    void HandleQuitButtonClick()
    {
        // Show quit confirmation
        Debug.Log("[ModernHomePage] Quit confirmation would be shown");
        
        // For now, just log the action
        // In full implementation, show PopupSystem.ShowMessage
        
        #if !UNITY_EDITOR
        Application.Quit();
        #else
        Debug.Log("[ModernHomePage] Application.Quit() called (editor mode)");
        #endif
    }
    
    void Update()
    {
        // Handle clan tag updates (from original Update method)
        UpdateClanTagDisplay();
    }
    
    void UpdateClanTagDisplay()
    {
        // This would update the clan tag display
        // Implementation depends on GameState.LocalDecorator being available
        if (GameState.LocalDecorator?.HudInformation != null && PlayerDataManager.IsPlayerInClan)
        {
            var displayName = PlayerDataManager.IsPlayerInClan ? 
                $"[{PlayerDataManager.ClanTag}] {PlayerDataManager.Name}" : 
                PlayerDataManager.Name;
            
            // GameState.LocalDecorator.HudInformation.SetAvatarLabel(displayName);
        }
    }
}