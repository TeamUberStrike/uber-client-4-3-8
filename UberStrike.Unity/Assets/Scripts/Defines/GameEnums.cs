
using UberStrike.Realtime.Common;
using Cmune.Realtime.Common;
using UberStrike.Core.Types;

public enum GameMode
{
    None = 0,
    Moderation = GameModeID.ModerationMode,
    DeathMatch = GameModeID.DeathMatch,
    TeamDeathMatch = GameModeID.TeamDeathMatch,
    TeamElimination = GameModeID.EliminationMode,
    Tutorial = 108,
    Training = 109,
    TryWeapon = 110
}

public enum GameStateId
{
    None = 0,

    Login,

    Menu,
    HomePage,
    PlayPage,
    ShopPage,
    ClanPage,
    InboxPage,
    ProfilePage,
    ChatPage,

    DeathMatch,
    TeamDeathMatch,
    TeamElimination,
    Tutorial,
    Training,
    TryWeapon,

    Playing,
    PregameLoadout,
    WaitingForPlayers,
    EndOfMatch,
    Spectating,
    Killed,
    InGameShop,
    GraceCountdown,
    Paused,
}

public enum UberstrikeLayer
{
    Default = 0,
    TransparentFX = 1,
    IgnoreRaycast = 2,
    Water = 4,
    UI = 5,
    GloballyLit = 8,
    GloballyLit_Reflect = 9,
    GloballyLit_Refract = 10,
    GloballyLit_ReflectRefract = 11,
    GloballyLit_DynamicReflectRefract = 12,
    LocallyLit = 13,
    LocallyLit_Reflect = 14,
    LocallyLit_Refract = 15,
    LocallyLit_ReflectRefract = 16,
    LocallyLit_DynamicReflectRefract = 17,
    LocalPlayer = 18,
    Weapons = 19,
    RemotePlayer = 20,
    Props = 21,
    Trigger = 22,
    Teleporter = 23,
    RemoteProjectile = 24,
    Controller = 25,
    LocalProjectile = 26,
    MeshGUI = 27,
    Raidbot = 28,
    Ragdoll = 29,
    //ItemPreview = 30,
    //PackPreview = 31,
}

public enum UberstrikeTag
{
    //unity internals
    Untagged,
    Respawn,
    Finish,
    EditorOnly,
    MainCamera,
    Player,
    GameController,

    //custom
    Avatar,
    HitArea,
    Robot,
    Prop,
    Wood,
    Metal,
    Water,
    Sand,
    Stone,
    SolidWood,
    Weapon,
    MovableObject,
    Grass,
    Sticky,
    DynamicProp,
    Cement,
}

[System.Flags]
public enum CombatRangeCategory
{
    Close = BIT_FLAGS.BIT_01,
    Medium = BIT_FLAGS.BIT_02,
    Far = BIT_FLAGS.BIT_03,

    CloseMedium = Close | Medium,
    MediumFar = Medium | Far,
    CloseMediumFar = Close | Medium | Far,
    CloseFar = Close | Far,
}