
public enum BoneIndex
{
    NONE,

    Rigging,
    Hips,

    LeftUpLeg,
    LeftLeg,
    LeftFood,

    RightUpLeg,
    RightLeg,
    RightFood,

    Spine,
    Neck,
    Head,
    HeadTop,

    LeftArm,
    LeftForeArm,
    LeftHand,

    RightArm,
    RightForeArm,
    RightHand
}

public enum AnimationIndex
{
    none = -1,
    idle = 0,
    run,
    jumpUp,
    jumpFall,
    jumpLand,
    shootLightGun,
    shootHeavyGun,
    lightGunBreathe,
    heavyGunBreathe,
    swimLoop,
    swimStart,
    die1,
    die2,
    die3,
    walk,
    squat,
    crouch,
    gotHit,
    lightGunUpDown,
    heavyGunUpDown,
    leanLeft,
    leanRight,
    snipeUpDown,
    meleeSwingRightToLeft,
    meleSwingLeftToRight,


    HomeNoWeaponIdle,
    HomeNoWeaponnLookAround,
    HomeNoWeaponRelaxNeck,
    HomeMeleeIdle,
    HomeMeleeLookAround,
    HomeMeleeRelaxNeck,
    HomeMeleeCheckWeapon,
    HomeSmallGunIdle,
    HomeSmallGunLookAround,
    HomeSmallGunRelaxNeck,
    HomeSmallGunCheckWeapon,
    HomeMediumGunIdle,
    HomeMediumGunLookAround,
    HomeMediumGunRelaxNeck,
    HomeMediumGunCheckWeapon,
    HomeLargeGunIdle,
    HomeLargeGunLookAround,
    HomeLargeGunRelaxNeck,
    HomeLargeGunCheckWeapon,
    HomeLargeGunShakeWeapon,

    ShopMeleeTakeOut,
    ShopMeleeAimIdle,
    ShopSmallGunTakeOut,
    ShopSmallGunAimIdle,
    ShopSmallGunShoot,
    ShopLargeGunTakeOut,
    ShopLargeGunAimIdle,
    ShopLargeGunShoot,
    ShopHideGun,
    ShopHideMelee,

    ShopNewGloves,
    ShopNewUpperBody,
    ShopNewBoots,
    ShopNewLowerBody,
    ShopNewHead,

    //what is this?
    idleWalk,

    TutorialGuideWalk,
    TutorialGuideAirlock,
    TutorialGuideTalk,
    TutorialGuideIdle,
    TutorialGuideArmory
}