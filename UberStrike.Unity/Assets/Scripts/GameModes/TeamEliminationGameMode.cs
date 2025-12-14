using System.Collections.Generic;
using Cmune.Realtime.Common;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

[NetworkClass((short)GameMode.TeamElimination)]
public class TeamEliminationGameMode : TeamDeathMatchGameMode
{
    public TeamEliminationGameMode(GameMetaData gameData)
        : base(gameData)
    {
        _pendingAvatarLoadingJobs = new Dictionary<int, CharacterInfo>();
    }

    protected override void OnModeInitialized()
    {
        IsMatchRunning = true;
    }

    public override void RespawnPlayer()
    {
        PlayerSpectatorControl.Instance.IsEnabled = false;

        //run the interpolator anyway, makes sure that we see our teammates running around even if the round is not started
        //turn off to disable this behaviour
        _stateInterpolator.Run();

        base.RespawnPlayer();

        //if the respawn is followed by a grace time period - we set input to disabled
        if (Players.Count > 1)
        {
            InputManager.Instance.IsInputEnabled = false;
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.AfterRound);
        }
    }

    protected override void OnSetNextSpawnPoint(int index, int coolDownTime)
    {
        LoadAllPendingAvatars();

        base.OnSetNextSpawnPoint(index, coolDownTime);
    }

    [NetworkMethod(FpsGameRPC.TeamEliminationRoundEnd)]
    protected void OnTeamEliminationRoundEnd(int teamId)
    {
        //Debug.LogWarning("OnSetWinTeam " + teamId);

        //Here we set the round running flag to FALSE, because this event is called as soon as:
        //a) one team was eliminated
        //b) when the grace count down timer is started
        IsMatchRunning = false;

        if (BlueTeamPlayerCount > 0 && RedTeamPlayerCount > 0)
        {
            CmuneEventHandler.Route(new OnTeamEliminationRoundEndEvent() { WinTeamID = (TeamID)teamId });
        }
    }

    [NetworkMethod(FpsGameRPC.SetWaitingForPlayers)]
    protected void OnSetWaitingForPlayers()
    {
        //Debug.Log("OnSetWaitingForPlayers");

        _stateInterpolator.Run();

        //enable controls again if disabled during grace time 
        if (!GameState.LocalPlayer.IsGamePaused)
        {
            InputManager.Instance.IsInputEnabled = true;
        }

        if (_isLocalAvatarLoaded)
        {
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);
        }
        else
        {
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Spectating);
        }

        CmuneEventHandler.Route(new OnSetWaitingForPlayersEvent());
    }

    [NetworkMethod(FpsGameRPC.SyncRoundTime)]
    protected virtual void OnSyncRoundTime(int roundEndServerTicks)
    {
        _roundStartTime = roundEndServerTicks - (GameData.RoundTime * 1000);
        CmuneEventHandler.Route(new OnTeamEliminationSyncRoundTimeEvent()
        {
            RoundEndServerTicks = roundEndServerTicks
        });
    }

    //TODO: OnMatchStart event is abused by TeamEliminationGameMode for round start, fix this in the future.
    protected override void OnMatchStart(int roundCount, int roundEndServerTicks)
    {
        IsMatchRunning = true;
        _roundStartTime = roundEndServerTicks - (GameData.RoundTime * 1000);
        _stateInterpolator.Run();

        //enable controls again if disabled during grace time 
        if (!GameState.LocalPlayer.IsGamePaused)
            InputManager.Instance.IsInputEnabled = true;

        if (_isLocalAvatarLoaded)
        {
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);
        }
        else // I'm a spectator
        {
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Spectating);
            GameState.LocalDecorator.HudInformation.Hide();
        }

        if (GameState.LocalCharacter.IsSpectator)
        {
            LoadAllPendingAvatars();
        }

        foreach (CharacterConfig cfg in _characterByActorId.Values)
        {
            cfg.IsAnimationEnabled = true;
        }

        CmuneEventHandler.Route(new OnTeamEliminationRoundStartEvent()
        {
            RoundCount = roundCount,
            RoundEndServerTicks = roundEndServerTicks
        });
    }

    [NetworkMethod(FpsGameRPC.UpdateRoundStats)]
    protected void OnUpdateRoundStats(int round, int blueScore, int redScore)
    {
        _blueTeamSplats = blueScore;
        _redTeamSplats = redScore;

        CmuneEventHandler.Route(new OnUpdateRoundStatsEvent() { BlueWinRoundCount = blueScore, RedWinRoundCount = redScore, Round = round });
    }

    [NetworkMethod(FpsGameRPC.PlayerSpectator)]
    protected void OnPlayerSpectator(int actorId)
    {
        if (actorId == MyActorId)
        {
            PlayerSpectatorControl.Instance.IsEnabled = true;
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Spectating);
        }
        else if (PlayerSpectatorControl.Instance.IsEnabled &&
                 PlayerSpectatorControl.Instance.CurrentActorId == actorId)
        {
            PlayerSpectatorControl.Instance.EnterFreeMoveMode();
        }
    }

    [NetworkMethod(FpsGameRPC.LoadPendingAvatarOfPlayers)]
    protected void OnLoadPendingAvatarOfPlayers(List<int> actorIDs)
    {
        foreach (int i in actorIDs)
        {
            if (_pendingAvatarLoadingJobs.ContainsKey(i))
            {
                InstantiateCharacter(_pendingAvatarLoadingJobs[i]);

                _pendingAvatarLoadingJobs.Remove(i);
                //Debug.LogError("_pendingAvatarLoadingJobs R " + i);
            }
        }
    }

    private void LoadAllPendingAvatars()
    {
        //if I am getting respawned - instantiate all waiting players (including me)
        if (_pendingAvatarLoadingJobs.Count > 0)
        {
            foreach (CharacterInfo c in _pendingAvatarLoadingJobs.Values)
            {
                InstantiateCharacter(c);
                //Debug.LogError("_pendingAvatarLoadingJobs R " + c.ActorId);
            }

            _pendingAvatarLoadingJobs.Clear();
        }
    }

    [NetworkMethod(FpsGameRPC.GraceTimeCountDown)]
    protected void OnGraceTimeCountDown(int round, int duration)
    {
        CmuneEventHandler.Route(new OnGraceTimeCountdownEvent());
        EnableAllAvatarHudInfo(false);
    }

    protected override void OnNormalJoin(UberStrike.Realtime.Common.CharacterInfo player)
    {
        _pendingAvatarLoadingJobs[player.ActorId] = player;

        if (player.ActorId != MyActorId)
        {
            //insert the CharacterInfo in the synchronization / interpolation system
            _stateInterpolator.AddCharacterInfo(player);
        }
        else
        {
            SendMethodToServer(FpsGameRPC.SetPowerUpCount, MyActorId, PickupItem.GetRespawnDurations());
        }
    }

    protected override void OnPlayerLeft(int actorId)
    {
        // cancel the pending avatar loading task
        if (_pendingAvatarLoadingJobs.ContainsKey(actorId))
        {
            _pendingAvatarLoadingJobs.Remove(actorId);
            //Debug.LogError("_pendingAvatarLoadingJobs R " + actorId);
        }

        // auto follow next player
        if (PlayerSpectatorControl.Instance.CurrentActorId == actorId)
        {
            if (PlayerSpectatorControl.Instance.IsEnabled)
                PlayerSpectatorControl.Instance.FollowNextPlayer();
        }

        base.OnPlayerLeft(actorId);
    }

    private void EnableAllAvatarHudInfo(bool enabled)
    {
        foreach (CharacterConfig character in _characterByActorId.Values)
        {
            if (character.Decorator && character.Decorator.HudInformation)
                character.Decorator.HudInformation.ForceShowInformation = enabled;
        }
    }

    #region Properties

    public override bool CanShowTabscreen
    {
        get { return Players.Count > 0; }
    }

    public override bool IsWaitingForPlayers
    {
        get
        {
            return IsGameStarted && (BlueTeamPlayerCount == 0 || RedTeamPlayerCount == 0);
        }
    }

    #endregion

    #region Fields
    /// <summary>
    /// list of players who join the game when round runing
    ///  they are not spawned until the round ends
    /// </summary>
    private Dictionary<int, UberStrike.Realtime.Common.CharacterInfo> _pendingAvatarLoadingJobs;
    #endregion
}
