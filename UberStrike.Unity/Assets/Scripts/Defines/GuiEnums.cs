
public enum PageType
{
    None = 0,
    Home,
    Play,
    Stats,
    Shop,
    Inbox,
    Clans,
    Training,
    Login,
    Chat,
    EndOfMatch,
    PreGame
}

public enum PanelType
{
    None = 0,

    Login,
    Signup,
    CompleteAccount,

    Options,
    Help,

    ReportPlayer,
    CreateGame,
    Moderation,
    BuyItem,

    ClanRequest,
    FriendRequest,
    SendMessage,

    NameChange,
}

public enum Popup
{
    None = 0,
    Message = 1,
    GameLoading = 2,
    VersionConflict = 3,
    PublishOnFacebook = 4,
}

/// Higher values are lower in the GUI
public enum GuiDepth
{
    Hud = 100,

    Page = 11,
    Chat = 9,
    GlobalRibbon = 7,
    Event = 5,
    Panel = 3,
    Popup = 1,
    Window = 0,
    Sfx = -1,
}