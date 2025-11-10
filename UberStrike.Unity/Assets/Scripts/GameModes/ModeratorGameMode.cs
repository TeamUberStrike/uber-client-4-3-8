using UberStrike.Realtime.Common;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;

[NetworkClass((short)GameMode.Moderation)]
public class ModeratorGameMode : ClientNetworkClass
{
    public static void ModerateGameMode(FpsGameMode mode)
    {
        if (_moderator != null)
        {
            _moderator.Dispose();
        }

        //create a new instance
        _moderator = new ModeratorGameMode(mode.GameData);

        //join the GameMode purly as a Spectator
        mode.InitializeMode(TeamID.NONE, true);

        //move like a spectator
        PlayerSpectatorControl.Instance.IsEnabled = true;
    }

    private ModeratorGameMode(GameMetaData data)
        : base(GameConnectionManager.Rmi)
    {
    }

    protected override void Dispose(bool dispose)
    {
        PlayerSpectatorControl.Instance.IsEnabled = false;

        base.Dispose(dispose);

        _moderator = null;
    }

    private static ModeratorGameMode _moderator;
}