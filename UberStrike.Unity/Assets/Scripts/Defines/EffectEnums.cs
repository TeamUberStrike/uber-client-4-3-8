
public enum ParticleConfigurationType
{
    None = 0,

    MeleeDefault,
    HandgunDefault,
    MachinegunDefault,
    ShotgunDefault,
    CannonDefault,
    LauncherDefault,
    SplattergunDefault,
    SniperRifleDefault,

    UberMachineGun,
    EnigmaCannon,
    ParticleLance,
    ParticleLanceDE,
    FusionLance,
    SnapShot,
    MagmaRifle,

    HaloCannon
}

/// <summary>
/// If add more melee weapons add them to IsMelee property of WeaponDecorator
/// After you add more elements to this Enum, don't forget to add them to ShowEffect and ShowExplosionEffect functions
/// and MenuAvatarAnimationController functions UpdateAnimationsForShop and UpdateAnimationsForHome
/// </summary>
public enum ImpactEffectType
{
    None = 0,

    MeleeDefault = 1,
    HGDefault = 2,
    HGExecutioner = 3,
    HGJudge = 4,
    HGJury = 5,
    MGDefault = 6,
    MGBattleSnake = 7,
    MGShadowGun = 8,
    SGDefault = 9,
    SGPaintHammer = 10,
    SGPaintShotty = 11,
    SGThunderbuss = 12,
    CNDefault = 13,
    CNForceCannon = 14,
    CNPaintzerfaust = 15,
    LRDefault = 16,
    LREnamelator = 17,
    LRMortalExporter = 18,
    SPDefault = 19,
    SPMadSplatter = 20,
    SPMagmaRifle = 21,
    SPVandalizer = 22,
    SRDefault = 23,
    SRDeliverator = 24,
    SROrdinatorRifle = 25,
    SRParticleLance = 26,
    MGUMG = 27,
    CNEnigmaCannon = 28,
    LRTheFinalWord = 29,
    SRVanquisher = 30,
    HGCupidsNoobhunter = 31,
    SRNefariousNeedler = 32,
    HGSpitefulSkewer = 33,
    HGGodfathersHandCannon = 34,
    SGSnotGun = 35,
    MeleeKatana = 36,
    MeleeSickle = 37,
    MeleeDundeeKnife = 38,
    MeleeRamboKnife = 39,
    MeleePirateKnife = 40,
    MeleeUberHammer = 41,
    MeleeSantaAxe = 42,
    MeleeSantaScythe = 43,
    SRFusionLance = 44,
    SRDarkVanquisher = 45,
    SPArcticRifle = 46,
    CNForceCannonPlus = 47,
    SGFirewave = 48,
    MGSabertooth = 49,
}

/// <summary>
/// list of possible surfaces
/// </summary>
public enum SurfaceEffectType
{
    None = 0,
    Default = 1,
    WoodEffect = 2,
    WaterEffect = 3,
    StoneEffect = 4,
    MetalEffect = 5,
    GrassEffect = 6,
    SandEffect = 7,
    Splat = 8,
}