using System.Collections.Generic;
using Cmune.Realtime.Common;
using Cmune.Util;
using UberStrike.Realtime.Common;

[NetworkClass((short)GameMode.DeathMatch)]
public class DeathMatchGameMode : FpsGameMode
{
    public DeathMatchGameMode(GameMetaData gameData)
        : base(GameConnectionManager.Rmi, gameData)
    {
        PlayerLeadStatus.Instance.ResetPlayerLead();
    }

    [NetworkMethod(FpsGameRPC.UpdateSplatCount)]
    protected void OnUpdateSplatCount(short myKills, short otherKills, bool isLeading)
    {
        GameState.LocalCharacter.Kills = myKills;
        CmuneEventHandler.Route(new OnUpdateDeathMatchScoreEvent() 
            { MyScore = myKills, OtherPlayerScore = otherKills, IsLeading = isLeading });
    }

    protected override void OnEndOfMatch()
    {
        IsWaitingForSpawn = false;
        IsMatchRunning = false;

        _stateInterpolator.Pause();

        GameState.LocalPlayer.Pause();
        GameState.IsReadyForNextGame = false;

        HideRemotePlayerHudFeedback();
    }
}
