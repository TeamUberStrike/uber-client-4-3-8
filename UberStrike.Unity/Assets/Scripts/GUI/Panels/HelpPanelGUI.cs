using System;
using UnityEngine;

public class HelpPanelGUI : PanelGuiBase
{
    private void Start()
    {
        _helpTabs = new GUIContent[] { new GUIContent(LocalizedStrings.General), 
            new GUIContent(LocalizedStrings.Gameplay), 
            new GUIContent(LocalizedStrings.Items), 
            //new GUIContent("Rules"),
            new GUIContent("About") };
    }

    private void OnGUI()
    {
        int height = Mathf.RoundToInt((Screen.height - 56) * 0.75f);
        _rect = new Rect((Screen.width - 630) * 0.5f, GlobalUIRibbon.Instance.GetHeight(), 630, height);

        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            DrawHelpPanel();
        }
        GUI.EndGroup();
    }

    private void DrawHelpPanel()
    {
        GUI.depth = (int)GuiDepth.Panel;

        GUI.Label(new Rect(0, 0, _rect.width, 56), LocalizedStrings.HelpCaps, BlueStonez.tab_strip);

        //Draw Stats Tabs
        _selectedHelpTab = GUI.SelectionGrid(new Rect(2, 31, 360, 22), _selectedHelpTab, _helpTabs, _helpTabs.Length, BlueStonez.tab_medium);

        GUI.BeginGroup(new Rect(16, 55, _rect.width - 32, _rect.height - 56 - 44), string.Empty, BlueStonez.window_standard_grey38);
        //Draw Panel content
        switch (_selectedHelpTab)
        {
            case 0:
                DrawGeneralGroup();
                break;
            case 1:
                DrawGameplayGroup();
                break;
            case 2:
                DrawItemsGroup();
                break;
            //case 3:
            //    DrawRuleGroup();
            //    break;
            case 3:
                DrawCreditsGroup();
                break;
        }
        GUI.EndGroup();

        if (GUI.Button(new Rect(_rect.width - 136, _rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button))
        {
            PanelManager.Instance.ClosePanel(PanelType.Help);
        }
    }

    private void DrawRuleGroup()
    {
        GUI.skin = BlueStonez.Skin;
        int height = 550;
        _scrollItems = GUITools.BeginScrollView(new Rect(1, 2, _rect.width - 33, _rect.height - 54 - 50), _scrollItems, new Rect(0, 0, WIDTH + 60, height));
        {
            //System
            Rect systemGroupRect = new Rect(14, 16, WIDTH + 30, height - 30);
            DrawGroupControl(systemGroupRect, "In-game Rules", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(systemGroupRect);
            {
                float y = 10;
                y = DrawGroupLabel(y, "Introduction", "Before we let you loose into the wild world of UberStrike, " +
                "we've written a few simple guidelines that are in place to make your gaming experience as fun and fair as possible. " +
                "Having a good time in a multiplayer game is a team effort! So do your part to help our community enjoy themselves;)\n\n" +
                "We hope that you have a pleasant stay in Uberstrike!");
                y = DrawGroupLabel(y, "Chatting", "1: No swearing or inappropriate content. Every time an inappropriate word is typed, three puppies and a kitten get caught in a revolving door.\n" +
                                                  "2: No \"Caps lock\" (using it for emphasis is okay). Please only emphazise with discretion and tact.\n" +
                                                  "3: No spamming. This includes baloney, rubbish, prattle, balderdash, hogwash, fatuity, drivel, mumbo jumbo, and canned precooked meat products. \n" +
                                                  "4: Do not personally attack any person(s). If you happen to be a hata, don't be hatin,' becasue the mods gonna be moderatin.' \n" +
                                                  "5: No backseat moderating. Believe it or not, we didn't add the convenient little 'Report Button' just because it looks pretty up there in the corner of the screen, although it does go nicely with that cute little gear symbol.\n" +
                                                  "6: Do not discuss topics that involve race, color, creed, religion, sex, or politics. It's not like we play games to get extra exposure to the many issues we face constantly in our daily lives.");

                y = DrawGroupLabel(y, "General", "1: Alternate or \"Second\" Accounts in-game ARE allowed, although we all love you just the way you are.\n" +
                                                 "2: No account sharing! Your account is yours, and if another player is caught using it, all parties will get banned. Sharing definitely isn't caring round these parts.\n" +
                                                 "3: Exploiting of glitches will not be tolerated. Cheating of any kind will result in a permanent ban, which may or may not include eternal banishment to the land of angry ankle-biting woodchucks.\n" +
                                                 "4: Be respectful to the Administrators/Moderators/QAs. These people work hard for you, so please show them respect. If you do, you might even get a cookie!\n" +
                                                 "5: Advertising of any content unrelated to UberStrike is not permitted.\n" +
                                                 "6: Please do not try to cleverly circumvent the rules listed here. Although some of these rules are flexible, they are here for a reason, and will be enforced.\n" +
                                                 "7: Join a server in your area. You will not get banned for lagging, although you may get kicked from the current game.\n" +
                                                 "8: Above all, use common sense. Studies have shown it works 87% better than no sense at all!\n" +
                                                 "9: Have fun!");
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
    }

    private void DrawGeneralGroup()
    {
        GUI.skin = BlueStonez.Skin;
        int height = 490;
        _scrollBasics = GUITools.BeginScrollView(new Rect(1, 2, _rect.width - 33, _rect.height - 54 - 50), _scrollBasics, new Rect(0, 0, WIDTH + 60, height));
        {
            //System
            Rect systemGroupRect = new Rect(14, 16, WIDTH + 30, height - 30); // 508
            DrawGroupControl(systemGroupRect, LocalizedStrings.WelcomeToUS, BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(systemGroupRect);
            {
                float y = 10;
                y = DrawGroupLabel(y, LocalizedStrings.Introduction, LocalizedStrings.IntroHelpDesc);
                y = DrawGroupLabel(y, LocalizedStrings.Home, LocalizedStrings.HomeHelpDesc);
                y = DrawGroupLabel(y, LocalizedStrings.Play, LocalizedStrings.PlayHelpDesc);
                y = DrawGroupLabel(y, LocalizedStrings.Profile, LocalizedStrings.ProfileHelpDesc);
                y = DrawGroupLabel(y, LocalizedStrings.Shop, LocalizedStrings.ShopHelpDesc);
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
    }

    private void DrawGameplayGroup()
    {
        GUI.skin = BlueStonez.Skin;
        int height = 950;
        _scrollGameplay = GUITools.BeginScrollView(new Rect(1, 2, _rect.width - 33, _rect.height - 54 - 50), _scrollGameplay, new Rect(0, 0, WIDTH + 60, height));
        {
            Rect systemGroupRect = new Rect(14, 16, WIDTH + 30, height - 30);
            DrawGroupControl(systemGroupRect, LocalizedStrings.Gameplay, BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(systemGroupRect);
            {
                float y = 10;
                y = DrawGroupLabel(y, "Character Level", "Your character level in UberStrike determines what items you have access to in the Shop. The higher your level, the more items you are able to get. Your character levels up by earning XP in the game.");
                y = DrawGroupLabel(y, "Earning XP", "There are five XP granting events in UberStrike. XP events stack, so if for example you get a headshot splat with a melee weapon you get 3 XP, one for the splat, one for the headshot, and one for the melee.");
                y = DrawGroupLabel(y, "Splatting an Enemy", "When you deal the final below to an enemy you get 1 XP.");
                y = DrawGroupLabel(y, "Headshot Splats", "When you splat an enemy with a headshot you get 1 XP.");
                y = DrawGroupLabel(y, "Nutshot Splats", "When you splat an enemy with a nutshot you get 1 XP.");
                y = DrawGroupLabel(y, "Melee Splats", "When you splat an enemy with a melee weapon you get 1 XP.");
                y = DrawGroupLabel(y, "Deal 100 Damage", "For every 100 damage you deal you get 1 XP.");
                y = DrawGroupLabel(y, "Health", "Health is what you need to survive. You start every life with 100 health, and if it reaches zero, you are splatted and have to respawn. If you take damage, you can replenish your health by picking up health packs in game.");
                y = DrawGroupLabel(y, "Armor Points", "Armor Points are picked up in the game. They absorb a percentage of the damage you receive. The percentage depends on the gear you have equipped.");
                y = DrawGroupLabel(y, "Looking Around", "UberStrike is a 3D environment, which means you need to be able to look around. To make your character do this you need to move the mouse.");
                y = DrawGroupLabel(y, "Moving Around", "In UberStrike you use the WASD keys to control the movement of your character. This means that pressing the W key on your keyboard will cause your character to walk forwards. With just the W key and the mouse you can navigate your character to almost every location in the game environment. Pressing the S key will cause you to walk backwards, and pressing the A and D keys will cause you to move left and right (called 'strafing').The final key you抣l need to know to get around in UberStrike is the spacebar. Pressing this key will cause your character to jump, which is essential for quickly getting around certain obstacles in the game. If you can get the hang of using the WASD keys to move, the spacebar to jump over obstacles, and the mouse to look around all at the same time, then you have mastered the basics of navigating a first person 3D environment. The use of these keys is common throughout many first person games, so practice them in UberStrike and you抣l be a pro in no time.");
                y = DrawGroupLabel(y, "Selecting Different Weapons", "By scrolling the mouse wheel you can cycle through all of your available weapons. You can also choose specific weapons by pressing the number keys 1 through 5.");
                y = DrawGroupLabel(y, "Combat", "In UberStrike your character carries weapons that you can use to splat other players. You use your weapons by clicking the mouse buttons. Pressing the left mouse button will cause the weapon to shoot, called 'Primary Fire' and pressing the right mouse button will use the weapon抯 special functions, called 'Alternate Fire' Be aware that not all weapons have an Alternate Fire function, and for those that do, it is often a different function for each weapon. An example of an Alternate Fire function would be the zoom, which is the Alternate Fire for Sniper Rifle class weapons.");
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
    }

    private void DrawItemsGroup()
    {
        GUI.skin = BlueStonez.Skin;
        int height = 690;
        _scrollItems = GUITools.BeginScrollView(new Rect(1, 2, _rect.width - 33, _rect.height - 54 - 50), _scrollItems, new Rect(0, 0, WIDTH + 60, height));
        {
            //System
            Rect systemGroupRect = new Rect(14, 16, WIDTH + 30, height - 30);
            DrawGroupControl(systemGroupRect, LocalizedStrings.Items, BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(systemGroupRect);
            {
                float y = 10;
                y = DrawGroupLabel(y, "Weapons", "Your character gets access to weapons after you buy them. You can also pick them up within the game, but you do not get to keep them when you are splatted or when you leave the game. Weapons are divided into eight classes: Melee, Handguns, Machine Guns, Shotguns, Sniper Rifles, Splatter Guns, Cannons, and Launchers. Each weapon class functions differently in game and is applicable in different combat contexts. For example, shotgun class weapons are generally better for close range battles, while the sniper rifle class weapons are better from a distance. Weapons have an expiry date of up to 90 days, depending on how long you buy them for. After the expiry date has passed, the weapon will disappear from your inventory.");
                y = DrawGroupLabel(y, "Gear", "Gear items are used to customize your character and increase your in-game protection. They have an effect on your speed, the amount of AP you can carry without depletion, and the amount of damage each AP absorbs.");
                y = DrawGroupLabel(y, "Loadout", "The Loadout is a list of all items that you own that your character currently has equipped. Your loadout dictates your character抯 appearance in the game.");
                y = DrawGroupLabel(y, "Inventory", "The Inventory is a list of all the items that you own that your character does NOT have equipped. All items here have an expiry time, after which they will disappear from your inventory.");
                y = DrawGroupLabel(y, "Shop", "The Shop is a place where you can buy items for standardized prices. It has the widest variety of items in UberStrike. Purchasing items in the shop is restricted according to your character level. If an item has a level that is above your character level, you cannot purchase it. You can increase your character level by playing the game and earning XP (see gameplay).");
                y = DrawGroupLabel(y, "Underground", "The Underground is a special shop that sells rare and unique items that cannot be found elsewhere. Items in the Underground have no level restrictions.");
                y = DrawGroupLabel(y, "Points", "Points are used to purchase items from the Shop. You get at least 500 points every day you log into UberStrike.");
                y = DrawGroupLabel(y, "Credits", "Credits are used to purchase powerful items from the Shop. You can obtain credits by clicking on the 'Get Credits' button in the bottom right hand corner of the shop, or on the Taskbar at the top of the screen. Credits are the only currency that can be used to purchase rare items in the Underground.");
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
    }

    private void DrawCreditsGroup()
    {
        GUI.skin = BlueStonez.Skin;
        int height = 360;
        _scrollItems = GUITools.BeginScrollView(new Rect(1, 2, _rect.width - 33, _rect.height - 54 - 50), _scrollItems, new Rect(0, 0, WIDTH + 60, height));
        {
            //System
            Rect systemGroupRect = new Rect(14, 16, WIDTH + 30, height - 30);
            DrawGroupControl(systemGroupRect, "About Uberstrike", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(systemGroupRect);
            {
                float y = 10;
                y = DrawGroupLabel(y, "The Team", "Pierre Andre, Ludovic Bodin, Benny Chen, Nad Chishtie, Jonny Farrell, Tommy Franken, Lanmay Jung, Jamin Lee, Kate Li, Monika Michalak, Lucas Ren, Shaun Lelacheur Sales, Dagmara Sitek, Paolo Stanner, Lee Turner, Alex Wang, Graham Vanderplank, Gabriel Weyer, Scarlett Yin, Brian Zhang, Alice Zhao");
                y = DrawGroupLabel(y, "The Mods", "Akalron, Army of One, avanos, ~H3ADSH0T~, Gray Mouser, GUY82, king_john, niashy, Simon1700, Snake Doctor, P_U_M_B_A, The Alpha Male, THE ENDER, timewarp01, Tweex, W00t");
                y = DrawGroupLabel(y, "The QA Testers", "ATOMjkee, Butcherr, Buford T Justice, Carlos Spicy Weine, Dark Drone, Equi|ibrium, hendronimus, KXI_SYSTEM, Neofighter, -ORTHRUS-, -Shruikan-, Simon1700, tayw97, +TrIgGeR_sPaZuM+");
                y = DrawGroupLabel(y, "The Legends", "Chingachgook, Ehnonimus, Enzo., karanraj, Leeness, Lev175, neel4d, Stylezxy, Ultimus Maximus");
                if (PlayerDataManager.IsPlayerLoggedIn)
                    DrawGroupLabel(y, "The Gamers", "YOU '" + PlayerDataManager.Name + "' and " + CurrentPlayers() + " other awesome players!");
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
    }

    private float DrawGroupLabel(float yOffset, string header, string text, bool center = false)
    {
        Rect position = new Rect(16, yOffset + 25, 490, 0);

        if (!String.IsNullOrEmpty(header))
        {
            GUI.color = new Color(0.87f, 0.64f, 0.035f, 1);
            GUI.Label(new Rect(position.x, position.y, position.width, 16), header + ":", BlueStonez.label_interparkbold_13pt_left);
            GUI.color = new Color(1, 1, 1, 0.8f);
        }

        float height = 0;
        if (center)
        {
            height = BlueStonez.label_interparkbold_11pt.CalcHeight(new GUIContent(text), position.width);
            GUI.Label(new Rect(position.x, position.y + 16, position.width, height), text, BlueStonez.label_interparkbold_11pt);
        }
        else
        {
            height = BlueStonez.label_interparkbold_11pt_left_wrap.CalcHeight(new GUIContent(text), position.width);
            GUI.Label(new Rect(position.x, position.y + 16, position.width, height), text, BlueStonez.label_interparkbold_11pt_left_wrap);
        }
        //GUI.Label(new Rect(position.x, position.y + 16, position.width, position.height - 16), text, center ? BlueStonez.label_interparkbold_11pt : BlueStonez.label_interparkbold_11pt_left_wrap);
        GUI.color = Color.white;

        return yOffset + 20 + height + 16;
    }

    private void DrawGroupControl(Rect rect, string title, GUIStyle style)
    {
        GUI.BeginGroup(rect, string.Empty, BlueStonez.group_grey81);
        GUI.EndGroup();
        GUI.Label(new Rect(rect.x + 18, rect.y - 8, style.CalcSize(new GUIContent(title)).x + 10, 16), title, style);
    }

    int CurrentPlayers()
    {
        return currentPlayers + Mathf.RoundToInt((float)DateTime.Now.Subtract(baseTime).TotalSeconds * newPlayersPerSecond);
    }

    #region Fields
    private Rect _rect;
    private GUIContent[] _helpTabs;
    private int _selectedHelpTab = 0;
    private Vector2 _scrollBasics;
    private Vector2 _scrollGameplay;
    private Vector2 _scrollItems;
    private const int WIDTH = 500;

    // counting player effect
    private DateTime baseTime = new DateTime(2011, 12, 11);
    private float newPlayersPerSecond = 0.14f;
    private int currentPlayers = 4079820;

    #endregion
}
