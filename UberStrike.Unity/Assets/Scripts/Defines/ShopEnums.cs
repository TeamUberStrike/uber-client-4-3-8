
public enum LoadoutSlotType
{
    GearHead,
    GearFace,
    GearGloves,
    GearUpperBody,
    GearLowerBody,
    GearBoots,
    GearHolo,

    WeaponMelee,
    WeaponPrimary,
    WeaponSecondary,
    WeaponTertiary,
    WeaponPickup,

    QuickUseItem1,
    QuickUseItem2,
    QuickUseItem3,

    FunctionalItem1,
    FunctionalItem2,
    FunctionalItem3,

    Inventory,
    Shop,
    Underground,
    None,
}

public enum ShopSortedColumns
{
    None = 0,
    PriceShop,
    Level,
    Duration,
    Name,
}

public enum ShopArea
{
    Inventory = 0,
    Shop = 1,
    MysteryBox = 2,
    Packs = 3,
    Credits = 4,
}

public enum LoadoutArea
{
    Weapons = 0,
    Gear = 1,
    QuickItems = 2,
}

public enum DefaultWeaponId
{
    WeaponMelee = 1000,
    WeaponHandgun = 1001,
    WeaponMachinegun = 1002,
    WeaponShotgun = 1003,
    WeaponSniperRifle = 1004,
    WeaponSplattergun = 1006,
    WeaponLauncher = 1007,
    WeaponCannon = 1005,
}

public enum RecommendType
{
    MostEfficient = 0,
    RecommendedWeapon,
    RecommendedArmor,
    StaffPick,
}