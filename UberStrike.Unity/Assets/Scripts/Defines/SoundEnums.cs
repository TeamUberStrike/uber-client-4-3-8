
public enum ImpactSoundType
{
    None = 0,
    Cement,
    Glass,
    Grass,
    Metal,
    Sand,
    Stone,
    Water,
    Wood,
    Sword
}

public enum SoundEffectType
{
    None = 0,

    EnvMediumSplash,
    EnvBigSplash,

    EnvImpactCement1,
    EnvImpactCement2,
    EnvImpactCement3,
    EnvImpactCement4,

    EnvImpactGlass1,
    EnvImpactGlass2,
    EnvImpactGlass3,
    EnvImpactGlass4,
    EnvImpactGlass5,

    EnvImpactGrass1,
    EnvImpactGrass2,
    EnvImpactGrass3,
    EnvImpactGrass4,

    EnvImpactMetal1,
    EnvImpactMetal2,
    EnvImpactMetal3,
    EnvImpactMetal4,
    EnvImpactMetal5,

    EnvSwordImpactMetal1,
    EnvSwordImpactMetal2,
    EnvSwordImpactMetal3,

    EnvImpactSand1,
    EnvImpactSand2,
    EnvImpactSand3,
    EnvImpactSand4,
    EnvImpactSand5,

    EnvImpactStone1,
    EnvImpactStone2,
    EnvImpactStone3,
    EnvImpactStone4,
    EnvImpactStone5,

    EnvImpactWater1,
    EnvImpactWater2,
    EnvImpactWater3,
    EnvImpactWater4,
    EnvImpactWater5,

    EnvImpactWood1,
    EnvImpactWood2,
    EnvImpactWood3,
    EnvImpactWood4,
    EnvImpactWood5,

    GameGetCredits,
    GameGetPoints,
    GameGetXP,
    GameLevelUp,

    GameMatchEndingCountdown1,
    GameMatchEndingCountdown2,
    GameMatchEndingCountdown3,
    GameMatchEndingCountdown4,
    GameMatchEndingCountdown5,
    GameTakenLead,
    GameTiedLead,
    GameLostLead,
    GameBlueWins,
    GameRedWins,
    GameDraw,
    GameGameOver,
    GameYouWin,
    GameCountdownTonal1,
    GameCountdownTonal2,
    GameFight,
    GameFocusEnemy,

    #region FootSteps

    PcFootStepGrass1,
    PcFootStepGrass2,
    PcFootStepGrass3,
    PcFootStepGrass4,

    PcFootStepDirt1,
    PcFootStepDirt2,
    PcFootStepDirt3,
    PcFootStepDirt4,

    PcFootStepRock1,
    PcFootStepRock2,
    PcFootStepRock3,
    PcFootStepRock4,

    PcFootStepSand1,
    PcFootStepSand2,
    PcFootStepSand3,
    PcFootStepSand4,

    PcFootStepWood1,
    PcFootStepWood2,
    PcFootStepWood3,
    PcFootStepWood4,

    PcFootStepMetal1,
    PcFootStepMetal2,
    PcFootStepMetal3,
    PcFootStepMetal4,

    PcFootStepHeavyMetal1,
    PcFootStepHeavyMetal2,
    PcFootStepHeavyMetal3,
    PcFootStepHeavyMetal4,

    PcFootStepWater1,
    PcFootStepWater2,
    PcFootStepWater3,

    PcFootStepSnow1,
    PcFootStepSnow2,
    PcFootStepSnow3,
    PcFootStepSnow4,

    PcSwimAboveWater1,
    PcSwimAboveWater2,
    PcSwimAboveWater3,
    PcSwimAboveWater4,

    PcFootStepGlass1,
    PcFootStepGlass2,
    PcFootStepGlass3,
    PcFootStepGlass4,

    #endregion

    PcGotHeadshotKill,
    PcGotNutshotKill,
    PcKilledBySplatbat,

    PcLocalPlayerHitArmorRemaining,
    PcLocalPlayerHitNoArmor,
    PcLocalPlayerHitNoArmorLowHealth,

    PcNormalKill1,
    PcNormalKill2,
    PcNormalKill3,

    PcSwimUnderWater,
    PcQuickItemRecharge,
    PcLandingGrunt,

    PropsSmallHealth,
    PropsMediumHealth,
    PropsBigHealth,
    PropsMegaHealth,
    PropsAmmoPickup,
    PropsArmorShard,
    PropsGoldArmor,
    PropsJumpPad,
    PropsSilverArmor,
    PropsWeaponPickup,
    PropsJumpPad2D,

    WeaponOutOfAmmoClick,
    WeaponSniperZoomIn,
    WeaponSniperZoomOut,
    WeaponUnderwaterExplosion1,
    WeaponUnderwaterExplosion2,
    WeaponWeaponSwitch,
    WeaponSniperScopeIn,
    WeaponSniperScopeOut,
    WeaponLauncherBounce1,
    WeaponLauncherBounce2,

    HUDAmmoPickup,
    HUDArmorShard,
    HUDBigHealth,
    HUDGoldArmor,
    HUDMediumHealth,
    HUDMegaHealth,
    HUDSilverArmor,
    HUDSmallHealth,
    HUDWeaponPickup,

    UIClickReady,
    UIClickUnready,
    UIClosePanel,
    UICloseLoadout,
    UIEndOfRound,
    UIEquipGear,
    UIEquipItem,
    UIEquipWeapon,
    UIButtonClick,

    UIRibbonClick,
    UIJoinGame,
    UIJoinServer,
    UIOpenLoadout,
    UIOpenPanel,
    UIGetCredits,

    UINewMessage,
    UINewRequest,
    UILeaveServer,
    UICreateGame,
    UIObjective,
    UISubObjective,
    UIObjectiveTick,
    UIWasdPressed,

    PropsTargetPopup,
    PropsTargetDamage,

    UIKillLeft1,
    UIKillLeft2,
    UIKillLeft3,
    UIKillLeft4,
    UIKillLeft5,

    UIDoubleKill,
    UITripleKill,
    UIQuadKill,
    UIMegaKill,
    UIUberKill,
    UIHeadShot,
    UINutShot,
    UISmackdown,

    UIMysteryBoxMusic,
    UIMysteryBoxWin,

    BGMSeletronRadio
}

/// <summary>
/// Careful, don't change the order, as the enum is used in inspectors.
/// Only append values, don't inject.
/// </summary>
public enum FootStepSoundType
{
    Dirt,
    Grass,
    Metal,
    Rock,
    Sand,
    Water,
    Wood,
    Swim,
    Dive,
    Snow,
    HeavyMetal,
    Glass,
}