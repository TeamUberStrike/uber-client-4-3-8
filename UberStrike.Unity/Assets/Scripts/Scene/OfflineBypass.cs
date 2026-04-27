using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.WebService.Unity;
using UnityEngine;

/// <summary>
/// Bypasses the dead UberStrike backend servers and bootstraps
/// the game directly to the home menu with fake offline data.
/// All web service calls (auth, login, shop, inventory, etc.) are skipped.
/// </summary>
public static class OfflineBypass
{
    public static void Bootstrap()
    {
        Debug.Log("[OfflineBypass] Starting offline bootstrap...");

        // 1. Set dummy encryption keys (prevents NullReferenceException in crypto code paths)
        Configuration.EncryptionInitVector = "offline0123456789";
        Configuration.EncryptionPassPhrase = "offline0123456789";

        // 2. Mark as online (various systems check this flag)
        ApplicationDataManager.IsOnline = true;

        // 3. Populate XP-by-level table (must come before SetPlayerStatisticsView)
        ApplicationDataManager.Instance.SetLevelCapsView(CreateLevelCaps());

        // 4. Set fake player identity
        PlayerDataManager.Instance.ServerLocalPlayerMemberView = CreateMemberView();

        // 5. Set fake player statistics (level calculated from XP via level caps)
        PlayerDataManager.Instance.SetPlayerStatisticsView(CreatePlayerStatistics());

        // 6. Register all bundled maps
        LevelManager.Instance.InitializeMapsToLoad(CreateMapList());

        // 7. Create the local player avatar (uses default gear from LoadoutManager defaults;
        //    gear prefabs are already in ItemManager._unityItems from UnityItemConfiguration.Awake)
        GameState.LocalDecorator = AvatarBuilder.Instance.CreateLocalAvatar();

        // 8. Navigate to the home menu
        MenuPageManager.Instance.LoadPage(PageType.Home);
        GlobalUIRibbon.IsVisible = true;
        GlobalUIRibbon.Instance.Show();

        Debug.Log("[OfflineBypass] Offline bootstrap complete. Welcome to UberStrike offline!");
    }

    private static MemberView CreateMemberView()
    {
        return new MemberView
        {
            PublicProfile = new PublicProfileView
            {
                Cmid = 1,
                Name = "Player",
                AccessLevel = MemberAccessLevel.Admin,
                EmailAddressStatus = EmailAddressStatus.Verified,
                GroupTag = string.Empty,
                LastLoginDate = DateTime.UtcNow,
                IsChatDisabled = false
            },
            MemberWallet = new MemberWalletView
            {
                Cmid = 1,
                Points = 10000,
                Credits = 10000,
                PointsExpiration = DateTime.MaxValue,
                CreditsExpiration = DateTime.MaxValue
            },
            MemberItems = new List<int>()
        };
    }

    private static PlayerStatisticsView CreatePlayerStatistics()
    {
        return new PlayerStatisticsView
        {
            Cmid = 1,
            Xp = 100000,
            Level = 10,
            PersonalRecord = new PlayerPersonalRecordStatisticsView(),
            WeaponStatistics = new PlayerWeaponStatisticsView()
        };
    }

    // Adds maps that exist locally as built scenes but aren't served by the
    // dev web service (ws-dev.uberforever.eu). Called by AuthenticationManager
    // after GetMaps completes so they appear alongside the server roster.
    public static void InjectLocalMaps(List<MapView> maps)
    {
        if (maps == null) return;
        // MapId MUST match MapConfiguration._mapId serialized in the scene,
        // otherwise LevelManager.AddLoadedMap can't pair the loaded scene with
        // the clicked map entry and GameLoader hangs on load.
        TryInject(maps, 22,  "AqualabResearchHub", "LevelAqualabResearchHub");
        TryInject(maps, 100, "SuperPRISMReactor",  "LevelSuperPRISMReactor");
    }

    private static void TryInject(List<MapView> maps, int mapId, string displayName, string sceneName)
    {
        if (maps.Exists(m => m.SceneName == sceneName)) return;
        if (maps.Exists(m => m.MapId == mapId)) return;
        maps.Add(BuildMap(mapId, displayName, sceneName));
    }

    private static MapView BuildMap(int mapId, string displayName, string sceneName)
    {
        var defaults = new MapSettings
        {
            KillsMin = 10, KillsMax = 200, KillsCurrent = 50,
            PlayersMin = 2, PlayersMax = 16, PlayersCurrent = 8,
            TimeMin = 5, TimeMax = 30, TimeCurrent = 10,
        };
        return new MapView
        {
            MapId = mapId,
            DisplayName = displayName,
            Description = displayName,
            SceneName = sceneName,
            FileName = string.Format("Map-{0:00}.unity3d", mapId),
            IsBlueBox = false,
            SupportedGameModes = 7,
            MaxPlayers = 16,
            Settings = new Dictionary<GameModeType, MapSettings>
            {
                { GameModeType.DeathMatch, defaults },
                { GameModeType.TeamDeathMatch, defaults },
                { GameModeType.EliminationMode, defaults },
            },
        };
    }

    private static List<MapView> CreateMapList()
    {
        // All 10 bundled maps (standalone build loads from Map-XX.unity3d asset bundles)
        return new List<MapView>
        {
            CreateMap(1,  "LostParadise2",       "LevelLostParadise2"),
            CreateMap(2,  "SpaceportAlpha",       "LevelSpaceportAlpha"),
            CreateMap(3,  "AqualabResearchHub",   "LevelAqualabResearchHub"),
            CreateMap(4,  "CatalystLab",          "LevelCatalystLab"),
            CreateMap(5,  "FortWinter",           "LevelFortWinter"),
            CreateMap(6,  "Gideon'sTower",        "LevelGideon'sTower"),
            CreateMap(7,  "SkyGarden",            "LevelSkyGarden"),
            CreateMap(8,  "SuperPRISMReactor",    "LevelSuperPRISMReactor"),
            CreateMap(9,  "TempleOfTheRaven",     "LevelTempleOfTheRaven"),
            CreateMap(10, "CuberSpace",           "LevelCuberSpace"),
            CreateMap(11, "MonkeyIsland",         "LevelMonkeyIsland"),
            CreateMap(12, "CuberStrike",          "LevelCuberStrike"),
        };
    }

    private static MapView CreateMap(int mapId, string displayName, string sceneName)
    {
        var defaultSettings = new MapSettings
        {
            KillsMin = 10,
            KillsMax = 200,
            KillsCurrent = 50,
            PlayersMin = 2,
            PlayersMax = 16,
            PlayersCurrent = 8,
            TimeMin = 5,
            TimeMax = 30,
            TimeCurrent = 10
        };

        return new MapView
        {
            MapId = mapId,
            DisplayName = displayName,
            Description = displayName,
            SceneName = sceneName,
            FileName = string.Format("Map-{0:00}.unity3d", mapId),
            IsBlueBox = false,
            SupportedGameModes = 7,  // DeathMatch + TeamDeathMatch + EliminationMode
            MaxPlayers = 16,
            Settings = new Dictionary<GameModeType, MapSettings>
            {
                { GameModeType.DeathMatch, defaultSettings },
                { GameModeType.TeamDeathMatch, defaultSettings },
                { GameModeType.EliminationMode, defaultSettings }
            }
        };
    }

    private static List<PlayerLevelCapView> CreateLevelCaps()
    {
        // Standard UberStrike XP progression curve (80 levels)
        var caps = new List<PlayerLevelCapView>();
        for (int level = 1; level <= 80; level++)
        {
            caps.Add(new PlayerLevelCapView
            {
                Level = level,
                XPRequired = GetXpForLevel(level)
            });
        }
        return caps;
    }

    private static int GetXpForLevel(int level)
    {
        // Approximation of UberStrike's XP curve: quadratic growth
        // Level 1 = 0, Level 10 ~ 9000, Level 20 ~ 38000, Level 80 ~ 616000
        if (level <= 1) return 0;
        return (level - 1) * (level - 1) * 100 + (level - 1) * 100;
    }
}
