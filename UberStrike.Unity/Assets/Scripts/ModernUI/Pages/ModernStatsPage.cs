using UnityEngine;

/// <summary>
/// Unity 6 Modern Stats Page - Converted for OnGUI-based rendering
/// Displays player statistics, achievements, and progress
/// </summary>
public class ModernStatsPage : UIPage
{
    [Header("Stats Configuration")]
    public bool showDetailedStats = true;
    public bool showAchievements = true;
    
    private int _selectedTab = 0;
    private string[] _tabNames = { "Overview", "Combat", "Weapons", "Achievements" };
    private Vector2 _scrollPosition = Vector2.zero;
    
    protected override void Start()
    {
        base.Start();
        
        // Set page configuration
        pageType = PageType.Stats;
        pageName = "Player Statistics";
        
        InitializeStatsPage();
    }
    
    void InitializeStatsPage()
    {
        Debug.Log("[ModernStatsPage] Initializing Unity 6 modern stats page...");
        
        SetupAvatarForStatsPage();
    }
    
    void SetupAvatarForStatsPage()
    {
        var menuConfig = FindObjectOfType<MenuConfiguration>();
        if (menuConfig != null && GameState.LocalDecorator != null)
        {
            Vector3 position;
            Quaternion rotation;
            if (menuConfig.GetPageAnchorPoint(PageType.Stats, out position, out rotation))
            {
                GameState.LocalDecorator.SetPosition(position, rotation);
            }
            
            GameState.LocalDecorator.HideWeapons();
        }
        
        // Reset animation state
        var animationManager = FindObjectOfType<AvatarAnimationManager>();
        if (animationManager != null)
        {
            animationManager.ResetAnimationState(PageType.Stats);
        }
    }
    
    protected override void RenderPageContent()
    {
        DrawModernStatsPage();
    }
    
    void DrawModernStatsPage()
    {
        // Stats header
        DrawStatsHeader();
        
        // Tab selection
        DrawStatsTabs();
        
        // Content based on selected tab
        DrawTabContent();
        
        // Navigation buttons
        DrawNavigationButtons();
    }
    
    void DrawStatsHeader()
    {
        var headerRect = new Rect(20, 20, Screen.width - 40, 80);
        GUI.BeginGroup(headerRect, "Player Statistics", GUI.skin.box);
        {
            // Player info
            GUI.Label(new Rect(10, 25, 300, 20), $"Player: {PlayerDataManager.Name}", GUI.skin.label);
            GUI.Label(new Rect(10, 45, 150, 20), $"Level: {PlayerDataManager.PlayerLevel}", GUI.skin.label);
            GUI.Label(new Rect(170, 45, 150, 20), $"XP: {PlayerDataManager.PlayerExperienceSecure}", GUI.skin.label);
            
            // Clan info if available
            if (PlayerDataManager.IsPlayerInClan)
            {
                GUI.Label(new Rect(330, 25, 200, 20), $"Clan: [{PlayerDataManager.ClanTag}]", GUI.skin.label);
            }
        }
        GUI.EndGroup();
    }
    
    void DrawStatsTabs()
    {
        var tabRect = new Rect(20, 110, Screen.width - 40, 40);
        
        GUI.BeginGroup(tabRect);
        {
            _selectedTab = GUI.SelectionGrid(new Rect(0, 10, tabRect.width, 30), 
                _selectedTab, _tabNames, _tabNames.Length);
        }
        GUI.EndGroup();
    }
    
    void DrawTabContent()
    {
        var contentRect = new Rect(20, 160, Screen.width - 40, Screen.height - 300);
        
        GUI.BeginGroup(contentRect, GUI.skin.box);
        {
            var scrollViewRect = new Rect(10, 10, contentRect.width - 20, contentRect.height - 20);
            var scrollContentRect = new Rect(0, 0, scrollViewRect.width - 20, 600);
            
            _scrollPosition = GUI.BeginScrollView(scrollViewRect, _scrollPosition, scrollContentRect);
            {
                switch (_selectedTab)
                {
                    case 0:
                        DrawOverviewStats(scrollContentRect);
                        break;
                    case 1:
                        DrawCombatStats(scrollContentRect);
                        break;
                    case 2:
                        DrawWeaponStats(scrollContentRect);
                        break;
                    case 3:
                        DrawAchievements(scrollContentRect);
                        break;
                }
            }
            GUI.EndScrollView();
        }
        GUI.EndGroup();
    }
    
    void DrawOverviewStats(Rect contentRect)
    {
        int yPos = 10;
        int lineHeight = 25;
        int sectionSpacing = 40;
        
        // General Stats
        GUI.Label(new Rect(10, yPos, 200, 20), "General Statistics", GUI.skin.label);
        yPos += 30;
        
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Total Play Time:", "45h 32m");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Games Played:", "127");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Games Won:", "89");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Win Ratio:", "70.1%");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Credits Earned:", "24,567");
        
        yPos += sectionSpacing;
        
        // Combat Overview
        GUI.Label(new Rect(10, yPos, 200, 20), "Combat Overview", GUI.skin.label);
        yPos += 30;
        
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Total Kills:", "1,234");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Total Deaths:", "567");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "K/D Ratio:", "2.18");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Accuracy:", "78.5%");
    }
    
    void DrawCombatStats(Rect contentRect)
    {
        int yPos = 10;
        int lineHeight = 25;
        int sectionSpacing = 40;
        
        // Detailed Combat Stats
        GUI.Label(new Rect(10, yPos, 200, 20), "Detailed Combat Statistics", GUI.skin.label);
        yPos += 30;
        
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Headshots:", "234 (18.9%)");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Multi Kills:", "89");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Longest Streak:", "12");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Total Damage:", "156,789");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Shots Fired:", "12,456");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Shots Hit:", "9,789");
        
        yPos += sectionSpacing;
        
        // Game Modes
        GUI.Label(new Rect(10, yPos, 200, 20), "Game Mode Statistics", GUI.skin.label);
        yPos += 30;
        
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Deathmatch Wins:", "45");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Team Deathmatch Wins:", "32");
        yPos += lineHeight;
        DrawStatLine(new Rect(20, yPos, contentRect.width - 40, lineHeight), "Elimination Wins:", "12");
    }
    
    void DrawWeaponStats(Rect contentRect)
    {
        int yPos = 10;
        int lineHeight = 30;
        
        GUI.Label(new Rect(10, yPos, 200, 20), "Weapon Statistics", GUI.skin.label);
        yPos += 30;
        
        // Weapon categories
        string[] weapons = { "Machine Gun", "Assault Rifle", "Sniper Rifle", "Shotgun", "Handgun", "Melee" };
        int[] kills = { 456, 321, 189, 145, 98, 25 };
        float[] accuracy = { 72.3f, 78.9f, 85.1f, 65.4f, 69.8f, 100.0f };
        
        for (int i = 0; i < weapons.Length; i++)
        {
            var weaponRect = new Rect(10, yPos, contentRect.width - 20, lineHeight);
            
            GUI.BeginGroup(weaponRect, GUI.skin.box);
            {
                GUI.Label(new Rect(10, 5, 150, 20), weapons[i], GUI.skin.label);
                GUI.Label(new Rect(170, 5, 100, 20), $"Kills: {kills[i]}", GUI.skin.label);
                GUI.Label(new Rect(280, 5, 120, 20), $"Accuracy: {accuracy[i]}%", GUI.skin.label);
            }
            GUI.EndGroup();
            
            yPos += lineHeight + 5;
        }
    }
    
    void DrawAchievements(Rect contentRect)
    {
        int yPos = 10;
        int lineHeight = 40;
        
        GUI.Label(new Rect(10, yPos, 200, 20), "Achievements", GUI.skin.label);
        yPos += 30;
        
        // Sample achievements
        string[] achievements = {
            "First Blood - Get your first kill",
            "Marksman - Achieve 80% accuracy",
            "Veteran - Play 100 games",
            "Survivor - Win without dying",
            "Collector - Own 10 weapons"
        };
        
        bool[] unlocked = { true, true, true, false, false };
        
        for (int i = 0; i < achievements.Length; i++)
        {
            var achievementRect = new Rect(10, yPos, contentRect.width - 20, lineHeight);
            
            GUI.color = unlocked[i] ? Color.white : Color.gray;
            GUI.BeginGroup(achievementRect, GUI.skin.box);
            {
                string status = unlocked[i] ? "✅" : "🔒";
                GUI.Label(new Rect(10, 5, 30, 20), status, GUI.skin.label);
                GUI.Label(new Rect(50, 5, achievementRect.width - 60, 30), achievements[i], GUI.skin.label);
            }
            GUI.EndGroup();
            GUI.color = Color.white;
            
            yPos += lineHeight + 5;
        }
    }
    
    void DrawStatLine(Rect rect, string label, string value)
    {
        GUI.Label(new Rect(rect.x, rect.y, 200, rect.height), label, GUI.skin.label);
        GUI.Label(new Rect(rect.x + 220, rect.y, rect.width - 220, rect.height), value, GUI.skin.label);
    }
    
    void DrawNavigationButtons()
    {
        var navRect = new Rect(20, Screen.height - 70, Screen.width - 40, 50);
        
        GUI.BeginGroup(navRect);
        {
            if (GUI.Button(new Rect(10, 10, 100, 30), "Home"))
            {
                NavigateToPage(PageType.Home);
            }
            
            if (GUI.Button(new Rect(120, 10, 100, 30), "Shop"))
            {
                NavigateToPage(PageType.Shop);
            }
            
            if (GUI.Button(new Rect(navRect.width - 110, 10, 100, 30), "Close"))
            {
                NavigateToPage(PageType.Home);
            }
        }
        GUI.EndGroup();
    }
}