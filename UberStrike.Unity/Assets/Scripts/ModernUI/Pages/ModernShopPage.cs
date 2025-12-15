using UnityEngine;

/// <summary>
/// Unity 6 Modern Shop Page - Converted for OnGUI-based rendering
/// Provides weapon and gear purchasing interface
/// </summary>
public class ModernShopPage : UIPage
{
    [Header("Shop Configuration")]
    public bool showWeaponCategories = true;
    public bool showGearCategories = true;
    
    private int _selectedCategory = 0;
    private Vector2 _itemScrollPosition = Vector2.zero;
    private string[] _categoryNames = { "Weapons", "Gear", "Items", "Boosters" };
    
    protected override void Start()
    {
        base.Start();
        
        // Set page configuration
        pageType = PageType.Shop;
        pageName = "Guns N Stuff";
        
        InitializeShopPage();
    }
    
    void InitializeShopPage()
    {
        Debug.Log("[ModernShopPage] Initializing Unity 6 modern shop page...");
        
        // Setup avatar for shop
        SetupAvatarForShopPage();
    }
    
    void SetupAvatarForShopPage()
    {
        var menuConfig = FindObjectOfType<MenuConfiguration>();
        if (menuConfig != null && GameState.LocalDecorator != null)
        {
            Vector3 position;
            Quaternion rotation;
            if (menuConfig.GetPageAnchorPoint(PageType.Shop, out position, out rotation))
            {
                GameState.LocalDecorator.SetPosition(position, rotation);
            }
            
            GameState.LocalDecorator.HideWeapons();
        }
        
        // Reset animation state
        var animationManager = FindObjectOfType<AvatarAnimationManager>();
        if (animationManager != null)
        {
            animationManager.ResetAnimationState(PageType.Shop);
        }
        
        // Enable armor HUD
        // var armorHud = FindObjectOfType<ArmorHud>(); // ArmorHud is not a UnityEngine.Object
        // if (armorHud != null)
        // {
        //     armorHud.Enabled = true; // Uncomment when available
        // }
    }
    
    protected override void OnHideStart()
    {
        base.OnHideStart();
        
        // Disable armor HUD when leaving shop
        // var armorHud = FindObjectOfType<ArmorHud>(); // ArmorHud is not a UnityEngine.Object
        // if (armorHud != null)
        // {
        //     armorHud.Enabled = false; // Uncomment when available
        // }
    }
    
    protected override void RenderPageContent()
    {
        DrawModernShopPage();
    }
    
    void DrawModernShopPage()
    {
        // Shop header
        DrawShopHeader();
        
        // Category selection
        DrawCategoryTabs();
        
        // Item list
        DrawItemList();
        
        // Player stats/currency
        DrawPlayerStats();
        
        // Navigation buttons
        DrawNavigationButtons();
    }
    
    void DrawShopHeader()
    {
        var headerRect = new Rect(20, 20, Screen.width - 40, 60);
        GUI.BeginGroup(headerRect, "Guns N Stuff - Shop", GUI.skin.box);
        {
            GUI.Label(new Rect(10, 25, headerRect.width - 20, 30), 
                "Welcome to the UberStrike Shop! Purchase weapons, gear, and items.", 
                GUI.skin.label);
        }
        GUI.EndGroup();
    }
    
    void DrawCategoryTabs()
    {
        var tabRect = new Rect(20, 90, Screen.width - 40, 40);
        
        GUI.BeginGroup(tabRect);
        {
            _selectedCategory = GUI.SelectionGrid(new Rect(0, 10, tabRect.width, 30), 
                _selectedCategory, _categoryNames, _categoryNames.Length);
        }
        GUI.EndGroup();
    }
    
    void DrawItemList()
    {
        var listRect = new Rect(20, 140, Screen.width - 40, Screen.height - 300);
        
        GUI.BeginGroup(listRect, GUI.skin.box);
        {
            GUI.Label(new Rect(10, 10, 200, 20), $"Category: {_categoryNames[_selectedCategory]}", GUI.skin.label);
            
            // Scroll view for items
            var scrollViewRect = new Rect(10, 40, listRect.width - 20, listRect.height - 50);
            var contentRect = new Rect(0, 0, scrollViewRect.width - 20, 500); // Adjust height as needed
            
            _itemScrollPosition = GUI.BeginScrollView(scrollViewRect, _itemScrollPosition, contentRect);
            {
                DrawItemsByCategory(_selectedCategory, contentRect);
            }
            GUI.EndScrollView();
        }
        GUI.EndGroup();
    }
    
    void DrawItemsByCategory(int category, Rect contentRect)
    {
        switch (category)
        {
            case 0: // Weapons
                DrawWeaponItems(contentRect);
                break;
            case 1: // Gear
                DrawGearItems(contentRect);
                break;
            case 2: // Items
                DrawConsumableItems(contentRect);
                break;
            case 3: // Boosters
                DrawBoosterItems(contentRect);
                break;
        }
    }
    
    void DrawWeaponItems(Rect contentRect)
    {
        // Placeholder weapon items
        string[] weapons = { "Machine Gun MK1", "Assault Rifle X2", "Sniper Elite", "Shotgun Pro", "Handgun Basic" };
        int[] prices = { 1500, 2200, 3500, 1800, 800 };
        
        for (int i = 0; i < weapons.Length; i++)
        {
            var itemRect = new Rect(10, i * 80, contentRect.width - 20, 70);
            
            GUI.BeginGroup(itemRect, GUI.skin.box);
            {
                // Item name
                GUI.Label(new Rect(10, 10, 200, 20), weapons[i], GUI.skin.label);
                
                // Price
                GUI.Label(new Rect(10, 30, 150, 20), $"Price: {prices[i]} Credits", GUI.skin.label);
                
                // Buy button
                if (GUI.Button(new Rect(itemRect.width - 110, 10, 100, 30), "Buy"))
                {
                    HandleItemPurchase(weapons[i], prices[i]);
                }
                
                // Preview button
                if (GUI.Button(new Rect(itemRect.width - 110, 45, 100, 20), "Preview"))
                {
                    HandleItemPreview(weapons[i]);
                }
            }
            GUI.EndGroup();
        }
    }
    
    void DrawGearItems(Rect contentRect)
    {
        // Placeholder gear items
        string[] gearItems = { "Combat Helmet", "Body Armor", "Combat Boots", "Tactical Gloves", "Face Mask" };
        int[] prices = { 800, 1200, 600, 400, 500 };
        
        for (int i = 0; i < gearItems.Length; i++)
        {
            var itemRect = new Rect(10, i * 80, contentRect.width - 20, 70);
            
            GUI.BeginGroup(itemRect, GUI.skin.box);
            {
                GUI.Label(new Rect(10, 10, 200, 20), gearItems[i], GUI.skin.label);
                GUI.Label(new Rect(10, 30, 150, 20), $"Price: {prices[i]} Credits", GUI.skin.label);
                
                if (GUI.Button(new Rect(itemRect.width - 110, 10, 100, 30), "Buy"))
                {
                    HandleItemPurchase(gearItems[i], prices[i]);
                }
                
                if (GUI.Button(new Rect(itemRect.width - 110, 45, 100, 20), "Preview"))
                {
                    HandleItemPreview(gearItems[i]);
                }
            }
            GUI.EndGroup();
        }
    }
    
    void DrawConsumableItems(Rect contentRect)
    {
        string[] items = { "Health Pack", "Ammo Refill", "Speed Boost", "Armor Repair", "Shield Generator" };
        int[] prices = { 50, 30, 75, 60, 100 };
        
        for (int i = 0; i < items.Length; i++)
        {
            var itemRect = new Rect(10, i * 60, contentRect.width - 20, 50);
            
            GUI.BeginGroup(itemRect, GUI.skin.box);
            {
                GUI.Label(new Rect(10, 10, 200, 20), items[i], GUI.skin.label);
                GUI.Label(new Rect(10, 25, 150, 20), $"Price: {prices[i]} Credits", GUI.skin.label);
                
                if (GUI.Button(new Rect(itemRect.width - 80, 15, 70, 25), "Buy"))
                {
                    HandleItemPurchase(items[i], prices[i]);
                }
            }
            GUI.EndGroup();
        }
    }
    
    void DrawBoosterItems(Rect contentRect)
    {
        string[] boosters = { "XP Booster 2x", "Credit Booster 1.5x", "Damage Boost", "Health Boost", "Speed Boost" };
        int[] prices = { 200, 150, 300, 250, 200 };
        
        for (int i = 0; i < boosters.Length; i++)
        {
            var itemRect = new Rect(10, i * 60, contentRect.width - 20, 50);
            
            GUI.BeginGroup(itemRect, GUI.skin.box);
            {
                GUI.Label(new Rect(10, 10, 200, 20), boosters[i], GUI.skin.label);
                GUI.Label(new Rect(10, 25, 150, 20), $"Price: {prices[i]} Credits", GUI.skin.label);
                
                if (GUI.Button(new Rect(itemRect.width - 80, 15, 70, 25), "Buy"))
                {
                    HandleItemPurchase(boosters[i], prices[i]);
                }
            }
            GUI.EndGroup();
        }
    }
    
    void DrawPlayerStats()
    {
        var statsRect = new Rect(20, Screen.height - 140, Screen.width - 40, 60);
        
        GUI.BeginGroup(statsRect, "Player Stats", GUI.skin.box);
        {
            // Display player credits and stats
            var credits = PlayerDataManager.IsPlayerLoggedIn ? PlayerDataManager.Credits : 0;
            
            GUI.Label(new Rect(10, 25, 150, 20), $"Credits: {credits}", GUI.skin.label);
            GUI.Label(new Rect(170, 25, 150, 20), $"Level: {PlayerDataManager.PlayerLevel}", GUI.skin.label);
            GUI.Label(new Rect(330, 25, 200, 20), $"Player: {PlayerDataManager.Name}", GUI.skin.label);
        }
        GUI.EndGroup();
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
            
            if (GUI.Button(new Rect(120, 10, 100, 30), "Stats"))
            {
                NavigateToPage(PageType.Stats);
            }
            
            if (GUI.Button(new Rect(navRect.width - 110, 10, 100, 30), "Close Shop"))
            {
                NavigateToPage(PageType.Home);
            }
        }
        GUI.EndGroup();
    }
    
    void HandleItemPurchase(string itemName, int price)
    {
        Debug.Log($"[ModernShopPage] Attempting to purchase {itemName} for {price} credits");
        
        if (PlayerDataManager.Credits >= price)
        {
            Debug.Log($"[ModernShopPage] Purchase successful: {itemName}");
            // In full implementation:
            // - Deduct credits
            // - Add item to inventory
            // - Update UI
            // - Show confirmation
        }
        else
        {
            Debug.Log($"[ModernShopPage] Insufficient credits for {itemName}");
            // Show insufficient credits message
        }
    }
    
    void HandleItemPreview(string itemName)
    {
        Debug.Log($"[ModernShopPage] Previewing item: {itemName}");
        
        // In full implementation:
        // - Show item on avatar
        // - Display item stats
        // - Allow rotation/inspection
    }
}