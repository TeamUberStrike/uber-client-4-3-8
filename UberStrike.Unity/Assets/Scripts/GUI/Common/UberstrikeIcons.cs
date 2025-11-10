using Cmune.DataCenter.Common.Entities;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Core.Types;

public class UberstrikeIcons : MonoSingleton<UberstrikeIcons>
{
    [SerializeField]
    private Texture2D _xpIcon20x20;
    public static Texture2D XPIcon20x20 { get { return (Instance) ? Instance._xpIcon20x20 : null; } }

    private static Texture2D _white;
    public static Texture2D White
    {
        get
        {
            if (_white == null)
            {
                _white = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var colors = new Color[16 * 16];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = Color.white;
                }
                _white.SetPixels(colors);
                _white.Apply();
            }
            return _white;
        }
    }

    [SerializeField]
    private Texture2D _mainMenuPlay64x64;
    public static Texture2D MainMenuPlay64x64 { get { return (Instance) ? Instance._mainMenuPlay64x64 : null; } }

    [SerializeField]
    private Texture2D _mainMenuShop64x64;
    public static Texture2D MainMenuShop64x64 { get { return (Instance) ? Instance._mainMenuShop64x64 : null; } }

    [SerializeField]
    private Texture2D _mainMenuTrain64x64;
    public static Texture2D MainMenuTrain64x64 { get { return (Instance) ? Instance._mainMenuTrain64x64 : null; } }

    [SerializeField]
    private Texture2D _mainMenuQuit64x64;
    public static Texture2D MainMenuQuit64x64 { get { return (Instance) ? Instance._mainMenuQuit64x64 : null; } }

    [SerializeField]
    private Texture2D[] _weaponClasses;
    public static Texture2D WeaponMelee { get { return (Instance._weaponClasses.Length > 0) ? Instance._weaponClasses[0] : null; } }
    public static Texture2D WeaponHandGun { get { return (Instance._weaponClasses.Length > 1) ? Instance._weaponClasses[1] : null; } }
    public static Texture2D WeaponMachineGun { get { return (Instance._weaponClasses.Length > 2) ? Instance._weaponClasses[2] : null; } }
    public static Texture2D WeaponShotGun { get { return (Instance._weaponClasses.Length > 3) ? Instance._weaponClasses[3] : null; } }
    public static Texture2D WeaponSniperRifle { get { return (Instance._weaponClasses.Length > 4) ? Instance._weaponClasses[4] : null; } }
    public static Texture2D WeaponCannon { get { return (Instance._weaponClasses.Length > 5) ? Instance._weaponClasses[5] : null; } }
    public static Texture2D WeaponSplatterGun { get { return (Instance._weaponClasses.Length > 6) ? Instance._weaponClasses[6] : null; } }
    public static Texture2D WeaponLauncher { get { return (Instance._weaponClasses.Length > 7) ? Instance._weaponClasses[7] : null; } }

    [SerializeField]
    private Texture2D[] _gearClasses;
    public static Texture2D GearBoots { get { return (Instance._gearClasses.Length > 0) ? Instance._gearClasses[0] : null; } }
    public static Texture2D GearHead { get { return (Instance._gearClasses.Length > 1) ? Instance._gearClasses[1] : null; } }
    public static Texture2D GearFace { get { return (Instance._gearClasses.Length > 2) ? Instance._gearClasses[2] : null; } }
    public static Texture2D GearUpperBody { get { return (Instance._gearClasses.Length > 3) ? Instance._gearClasses[3] : null; } }
    public static Texture2D GearLowerBody { get { return (Instance._gearClasses.Length > 4) ? Instance._gearClasses[4] : null; } }
    public static Texture2D GearGloves { get { return (Instance._gearClasses.Length > 5) ? Instance._gearClasses[5] : null; } }
    public static Texture2D GearHolo { get { return (Instance._gearClasses.Length > 6) ? Instance._gearClasses[6] : null; } }

    [SerializeField]
    private Texture2D[] _itemCategories;
    public static Texture2D ItemRecent { get { return (Instance._itemCategories.Length > 0) ? Instance._itemCategories[0] : null; } }
    public static Texture2D ItemSpecial { get { return (Instance._itemCategories.Length > 1) ? Instance._itemCategories[1] : null; } }
    public static Texture2D ItemWeapons { get { return (Instance._itemCategories.Length > 2) ? Instance._itemCategories[2] : null; } }
    public static Texture2D ItemGear { get { return (Instance._itemCategories.Length > 3) ? Instance._itemCategories[3] : null; } }
    public static Texture2D ItemQuick { get { return (Instance._itemCategories.Length > 4) ? Instance._itemCategories[4] : null; } }
    public static Texture2D ItemFunctional { get { return (Instance._itemCategories.Length > 5) ? Instance._itemCategories[5] : null; } }
    public static Texture2D ItemNewHotSale { get { return (Instance._itemCategories.Length > 6) ? Instance._itemCategories[6] : null; } }

    public static Texture2D GetIconForItemClass(UberstrikeItemClass itemClass)
    {
        switch (itemClass)
        {
            case UberstrikeItemClass.GearBoots: return GearBoots;
            case UberstrikeItemClass.GearHead: return GearHead;
            case UberstrikeItemClass.GearFace: return GearFace;
            case UberstrikeItemClass.GearUpperBody: return GearUpperBody;
            case UberstrikeItemClass.GearLowerBody: return GearLowerBody;
            case UberstrikeItemClass.GearGloves: return GearGloves;
            case UberstrikeItemClass.GearHolo: return GearHolo;

            case UberstrikeItemClass.WeaponMelee: return WeaponMelee;
            case UberstrikeItemClass.WeaponHandgun: return WeaponHandGun;
            case UberstrikeItemClass.WeaponMachinegun: return WeaponMachineGun;
            case UberstrikeItemClass.WeaponShotgun: return WeaponShotGun;
            case UberstrikeItemClass.WeaponSniperRifle: return WeaponSniperRifle;
            case UberstrikeItemClass.WeaponCannon: return WeaponCannon;
            case UberstrikeItemClass.WeaponSplattergun: return WeaponSplatterGun;
            case UberstrikeItemClass.WeaponLauncher: return WeaponLauncher;

            case UberstrikeItemClass.QuickUseGeneral: return ItemQuick;
            case UberstrikeItemClass.QuickUseGrenade: return ItemQuick;
            case UberstrikeItemClass.QuickUseMine: return ItemQuick;
            case UberstrikeItemClass.FunctionalGeneral: return ItemFunctional;
            case UberstrikeItemClass.SpecialGeneral: return ItemSpecial;

            default: return null;
        }
    }

    [SerializeField]
    private Texture2D[] _channels;

    public static Texture2D ChannelCmune { get { return (Instance._channels.Length > 0 && Instance._channels[0] != null) ? Instance._channels[0] : White; } }
    public static Texture2D ChannelFacebook { get { return (Instance._channels.Length > 1 && Instance._channels[1] != null) ? Instance._channels[1] : White; } }
    public static Texture2D ChannelWindows { get { return (Instance._channels.Length > 2 && Instance._channels[2] != null) ? Instance._channels[2] : White; } }
    public static Texture2D ChannelApple { get { return (Instance._channels.Length > 3 && Instance._channels[3] != null) ? Instance._channels[3] : White; } }
    public static Texture2D ChannelKongregate { get { return (Instance._channels.Length > 4 && Instance._channels[4] != null) ? Instance._channels[4] : White; } }
    public static Texture2D ChannelAndroid { get { return (Instance._channels.Length > 5 && Instance._channels[5] != null) ? Instance._channels[5] : White; } }
    public static Texture2D ChanneliOs { get { return (Instance._channels.Length > 6 && Instance._channels[6] != null) ? Instance._channels[6] : White; } }

    public static Texture2D GetIconForChannel(ChannelType channel)
    {
        switch (channel)
        {
            case ChannelType.WebPortal: return ChannelCmune;
            case ChannelType.WebFacebook: return ChannelFacebook;
            case ChannelType.WindowsStandalone: return ChannelWindows;
            case ChannelType.MacAppStore: return ChannelApple;
            case ChannelType.OSXStandalone: return ChannelApple;
            case ChannelType.Kongregate: return ChannelKongregate;
            case ChannelType.Android: return ChannelAndroid;
            case ChannelType.IPad:
            case ChannelType.IPhone: return ChanneliOs;

            default: return White;
        }
    }

    [SerializeField]
    private Texture2D[] _presence;
    public static Texture2D PresenceOffline { get { return (Instance._presence.Length > 0 && Instance._presence[0] != null) ? Instance._presence[0] : White; } }
    public static Texture2D PresenceOnline { get { return (Instance._presence.Length > 1 && Instance._presence[1] != null) ? Instance._presence[1] : White; } }
    public static Texture2D PresenceInGame { get { return (Instance._presence.Length > 2 && Instance._presence[2] != null) ? Instance._presence[2] : White; } }

    [SerializeField]
    private Texture2D _cheatIconBlue;
    public static Texture2D CheatIconBlue { get { return (Instance) ? Instance._cheatIconBlue != null ? Instance._cheatIconBlue : White : null; } }

    [SerializeField]
    private Texture2D[] _loadoutCategories;
    public static Texture2D LoadoutWeapon { get { return (Instance._loadoutCategories.Length > 0 && Instance._loadoutCategories[0] != null) ? Instance._loadoutCategories[0] : White; } }
    public static Texture2D LoadoutGear { get { return (Instance._loadoutCategories.Length > 1 && Instance._loadoutCategories[1] != null) ? Instance._loadoutCategories[1] : White; } }
    public static Texture2D LoadoutQuick { get { return (Instance._loadoutCategories.Length > 2 && Instance._loadoutCategories[2] != null) ? Instance._loadoutCategories[2] : White; } }

    [SerializeField]
    private Texture2D[] _labSections;
    public static Texture2D LabInventory { get { return (Instance._labSections.Length > 0 && Instance._labSections[0] != null) ? Instance._labSections[0] : White; } }
    public static Texture2D LabShop { get { return (Instance._labSections.Length > 1 && Instance._labSections[1] != null) ? Instance._labSections[1] : White; } }
    public static Texture2D LabUnderground { get { return (Instance._labSections.Length > 2 && Instance._labSections[2] != null) ? Instance._labSections[2] : White; } }
    public static Texture2D LabPacks { get { return (Instance._labSections.Length > 3 && Instance._labSections[3] != null) ? Instance._labSections[3] : White; } }
    public static Texture2D LabCredits { get { return (Instance._labSections.Length > 4 && Instance._labSections[4] != null) ? Instance._labSections[4] : White; } }

    [SerializeField]
    private Texture2D _widgetScalerIcon;
    public static Texture2D WidgetScalerIcon
    {
        get { return Instance._widgetScalerIcon; }
    }

    [SerializeField]
    private Texture2D _blueBox;
    public static Texture2D BlueBox
    {
        get { return Instance._blueBox; }
    }

    [SerializeField]
    private Texture2D _checkMark;
    public static Texture2D CheckMark
    {
        get { return Instance._checkMark; }
    }

    [SerializeField]
    private Texture2D _newMessageIcon;
    public static Texture2D NewMessageIcon { get { return Exists ? Instance._newMessageIcon : White; } }

    [SerializeField]
    private Texture2D[] _shopItemRentDurations;
    public static Texture2D Duration1Day { get { return (Instance._shopItemRentDurations.Length > 0 && Instance._shopItemRentDurations[0] != null) ? Instance._shopItemRentDurations[0] : White; } }
    public static Texture2D Duration7Days { get { return (Instance._shopItemRentDurations.Length > 1 && Instance._shopItemRentDurations[1] != null) ? Instance._shopItemRentDurations[1] : White; } }
    public static Texture2D Duration30Days { get { return (Instance._shopItemRentDurations.Length > 2 && Instance._shopItemRentDurations[2] != null) ? Instance._shopItemRentDurations[2] : White; } }
    public static Texture2D Duration90Days { get { return (Instance._shopItemRentDurations.Length > 3 && Instance._shopItemRentDurations[3] != null) ? Instance._shopItemRentDurations[3] : White; } }
    public static Texture2D DurationPermanent { get { return (Instance._shopItemRentDurations.Length > 4 && Instance._shopItemRentDurations[4] != null) ? Instance._shopItemRentDurations[4] : White; } }

    [SerializeField]
    private Texture2D _itemExpiration;
    public static Texture2D ItemExpiration { get { return Exists ? Instance._itemExpiration : White; } }

    [SerializeField]
    public Texture2D _itemArmorPoints;
    public static Texture2D ItemArmorPoints { get { return Exists ? Instance._itemArmorPoints : White; } }

    [SerializeField]
    private Texture2D _sortUpArrow;
    public static Texture2D SortUpArrow { get { return Exists ? Instance._sortUpArrow : White; } }

    [SerializeField]
    private Texture2D _sortDownArrow;
    public static Texture2D SortDownArrow { get { return Exists ? Instance._sortDownArrow : White; } }

    [SerializeField]
    private Texture2D _expandBigArrow;
    public static Texture2D ExpandBigArrow { get { return Exists ? Instance._expandBigArrow : White; } }

    [SerializeField]
    private Texture2D[] _promotionTags;
    public static Texture2D PromotionNew { get { return (Instance._promotionTags.Length > 0 && Instance._promotionTags[0] != null) ? Instance._promotionTags[0] : White; } }
    public static Texture2D PromotionHot { get { return (Instance._promotionTags.Length > 1 && Instance._promotionTags[1] != null) ? Instance._promotionTags[1] : White; } }
    public static Texture2D PromotionSale { get { return (Instance._promotionTags.Length > 2 && Instance._promotionTags[2] != null) ? Instance._promotionTags[2] : White; } }

    [SerializeField]
    private Texture2D _levelLock;
    public static Texture2D LevelLock { get { return Exists ? Instance._levelLock : White; } }

    [SerializeField]
    private Texture2D _levelUp;
    public static Texture2D LevelUp { get { return Exists ? Instance._levelUp : White; } }

    [SerializeField]
    private Texture2D _facebook;
    public static Texture2D Facebook { get { return Exists ? Instance._facebook : White; } }

    [SerializeField]
    private Texture2D[] _achievements;
    public static Texture2D AcvMostValuable { get { return (Instance._achievements.Length > 0 && Instance._achievements[0] != null) ? Instance._achievements[0] : White; } }
    public static Texture2D AcvMostAgressive { get { return (Instance._achievements.Length > 1 && Instance._achievements[1] != null) ? Instance._achievements[1] : White; } }
    public static Texture2D AcvSharpestShooter { get { return (Instance._achievements.Length > 2 && Instance._achievements[2] != null) ? Instance._achievements[2] : White; } }
    public static Texture2D AcvTriggerHppy { get { return (Instance._achievements.Length > 3 && Instance._achievements[3] != null) ? Instance._achievements[3] : White; } }
    public static Texture2D AcvHardestHitter { get { return (Instance._achievements.Length > 4 && Instance._achievements[4] != null) ? Instance._achievements[4] : White; } }
    public static Texture2D AcvCostEffective { get { return (Instance._achievements.Length > 5 && Instance._achievements[5] != null) ? Instance._achievements[5] : White; } }

    public static Texture2D GetAchievement(AchievementType achievement)
    {
        Texture2D tex = White;

        switch (achievement)
        {
            case AchievementType.MostValuable:
                tex = AcvMostValuable;
                break;
            case AchievementType.MostAggressive:
                tex = AcvMostAgressive;
                break;
            case AchievementType.SharpestShooter:
                tex = AcvSharpestShooter;
                break;
            case AchievementType.TriggerHappy:
                tex = AcvTriggerHppy;
                break;
            case AchievementType.HardestHitter:
                tex = AcvHardestHitter;
                break;
            case AchievementType.CostEffective:
                tex = AcvCostEffective;
                break;
        }

        return tex;
    }

    [SerializeField]
    private MovieTexture[] _achievementBadges;
    public static MovieTexture GetAchievementBadge(AchievementType achievementType)
    {
        if (!Exists)
        {
            return null;
        }
        switch (achievementType)
        {
            case AchievementType.MostValuable:
                return Instance._achievementBadges[0];
            case AchievementType.MostAggressive:
                return Instance._achievementBadges[1];
            case AchievementType.SharpestShooter:
                return Instance._achievementBadges[2];
            case AchievementType.TriggerHappy:
                return Instance._achievementBadges[3];
            case AchievementType.HardestHitter:
                return Instance._achievementBadges[4];
            case AchievementType.CostEffective:
                return Instance._achievementBadges[5];
            case AchievementType.None:
                return Instance._achievementBadges[6];
            default:
                return null;
        }
    }

    public static string GetAchievementTitle(AchievementType type)
    {
        switch (type)
        {
            case AchievementType.MostValuable:
                return "MOST VALUABLE";
            case AchievementType.MostAggressive:
                return "MOST AGGRESSIVE";
            case AchievementType.TriggerHappy:
                return "TRIGGER HAPPY";
            case AchievementType.SharpestShooter:
                return "SHARPEST SHOOTER";
            case AchievementType.CostEffective:
                return "COST EFFECTIVE";
            case AchievementType.HardestHitter:
                return "HARDEST HITTER";
            case AchievementType.None:
                return "UBERSTRIKE PLAYER";
            default:
                return string.Empty;
        }
    }

    [SerializeField]
    private MovieTexture[] _recommendationBadges;
    public static MovieTexture GetRecommendBadge(RecommendType recomType)
    {
        if (!Exists)
        {
            return null;
        }
        switch (recomType)
        {
            case RecommendType.MostEfficient:
                return Instance._recommendationBadges[0];
            case RecommendType.RecommendedWeapon:
                return Instance._recommendationBadges[1];
            case RecommendType.StaffPick:
                return Instance._recommendationBadges[2];
            case RecommendType.RecommendedArmor:
                return Instance._recommendationBadges[3];
            default:
                return null;
        }
    }

    [SerializeField]
    private Texture2D[] _stats;
    public static Texture2D StatsKills { get { return (Instance._stats.Length > 0) ? Instance._stats[0] : null; } }
    public static Texture2D StatsSmackDown { get { return (Instance._stats.Length > 1) ? Instance._stats[1] : null; } }
    public static Texture2D StatsHeadshots { get { return (Instance._stats.Length > 2) ? Instance._stats[2] : null; } }
    public static Texture2D StatsNutshots { get { return (Instance._stats.Length > 3) ? Instance._stats[3] : null; } }
    public static Texture2D StatsDamage { get { return (Instance._stats.Length > 4) ? Instance._stats[4] : null; } }
    public static Texture2D StatsDeaths { get { return (Instance._stats.Length > 5) ? Instance._stats[5] : null; } }
    public static Texture2D StatsKDR { get { return (Instance._stats.Length > 6) ? Instance._stats[6] : null; } }
    public static Texture2D StatsSuicides { get { return (Instance._stats.Length > 7) ? Instance._stats[7] : null; } }

    [SerializeField]
    private Texture2D _checkmarkForEquipedItem;
    public static Texture2D CheckMarkForEquippedItem { get { return Instance._checkmarkForEquipedItem ? Instance._checkmarkForEquipedItem : White; } }

    [SerializeField]
    private Texture2D _lotteryIcon;
    public static Texture2D LotteryIcon { get { return Instance._lotteryIcon ? Instance._lotteryIcon : White; } }
}