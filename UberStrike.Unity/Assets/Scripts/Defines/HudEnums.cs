using Cmune.Realtime.Common;
using UberStrike.Core.Types;

[System.Flags]
public enum HudDrawFlags
{
    None = BIT_FLAGS.BIT_NONE,

    Score = BIT_FLAGS.BIT_01,
    HealthArmor = BIT_FLAGS.BIT_02,
    Ammo = BIT_FLAGS.BIT_03,
    Weapons = BIT_FLAGS.BIT_04,
    Reticle = BIT_FLAGS.BIT_05,
    RoundTime = BIT_FLAGS.BIT_06,
    XpPoints = BIT_FLAGS.BIT_07,
    InGameHelp = BIT_FLAGS.BIT_08,
    EventStream = BIT_FLAGS.BIT_09,
    RemainingKill = BIT_FLAGS.BIT_10,
    InGameChat = BIT_FLAGS.BIT_11,
    StateMsg = BIT_FLAGS.BIT_12,
}

public enum PlayerHudState
{
    None,
    Playing,
    Spectating,
    AfterRound,
}

public enum InGameEventFeedbackType
{
    None,
    Splatted,
    HeadShot,
    NutShot,
    Humiliation,
    Impressive,
    LevelUp,
    DoubleSplat,
    MultiSplat,
    UltraSplat,
    MonsterSplat,
    SplattingSpree,
    Rampage,
    Dominating,
    Unstoppable,
    DemiGod,
    GodOfSplat,
    CustomMessage
}

public enum PickUpMessageType
{
    None,
    Health5,
    Health25,
    Health50,
    Health100,
    Armor5,
    Armor50,
    Armor100,
    AmmoCannon,
    AmmoHandgun,
    AmmoLauncher,
    AmmoMachinegun,
    AmmoShotgun,
    AmmoSnipergun,
    AmmoSplattergun,
    ChangeWeapon,
    Coin,
}