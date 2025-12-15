using System;
using System.Collections.Generic;
using Cmune.Realtime.Common;
using Cmune.Realtime.Common.Utils;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

[NetworkClass((short)GameMode.TeamDeathMatch)]
public class TeamDeathMatchGameMode : FpsGameMode
{
    public TeamDeathMatchGameMode(GameMetaData gameData)
        : base(GameConnectionManager.Rmi, gameData)
    { }

    [NetworkMethod(FpsGameRPC.UpdateSplatCount)]
    protected virtual void OnUpdateSplatCount(int blueScore, int redScore, bool isLeading)
    {
        if (_blueTeamSplats != blueScore || _redTeamSplats != redScore)
        {
            CmuneEventHandler.Route(new OnUpdateTeamScoreEvent() 
                { BlueScore = blueScore, RedScore = redScore, IsLeading = isLeading });
            _blueTeamSplats = blueScore;
            _redTeamSplats = redScore;
        }
    }

    [NetworkMethod(FpsGameRPC.TeamBalanceUpdate)]
    protected void OnTeamBalanceUpdate(int blueCount, int redCount)
    {
        //CmuneDebug.LogErrorFormat("TeamBalanceUpdate {0} blue - {1}, red - {2}",GameData.RoomID, blueCount, redCount);
        _blueTeamPlayerCount = blueCount;
        _redTeamPlayerCount = redCount;
    }

    public override void RespawnPlayer()
    {
        try
        {
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);

            IsWaitingForSpawn = false;
            _canChangeTeamInThisLife = true;

            Vector3 pos;
            Quaternion rot;
            SpawnPointManager.Instance.GetSpawnPointAt(_nextSpawnPoint, (GameMode)GameData.GameMode, GameState.LocalCharacter.TeamID, out pos, out rot);

            SpawnPlayerAt(pos, rot);
        }
        catch (Exception e)
        {
            throw CmuneDebug.Exception(e.InnerException, "RespawnPlayer with GameState.LocalCharacter {0}", CmunePrint.Properties(GameState.LocalCharacter));
        }
    }

    protected override void OnEndOfMatch()
    {
        IsWaitingForSpawn = false;
        IsMatchRunning = false;

        _stateInterpolator.Pause();

        GameState.LocalPlayer.Pause(true);
        GameState.IsReadyForNextGame = false;

        HideRemotePlayerHudFeedback();
        CmuneEventHandler.Route(new TeamGameEndEvent() { RedTeamSplats = _redTeamSplats, BlueTeamSplats = _blueTeamSplats });
    }

    protected override void UpdatePlayerCounters()
    {
        _redTeamPlayerCount = 0;
        _blueTeamPlayerCount = 0;

        foreach (UberStrike.Realtime.Common.CharacterInfo v in Players.Values)
        {
            if (v.TeamID == TeamID.RED)
            {
                _redTeamPlayerCount++;
            }
            else if (v.TeamID == TeamID.BLUE)
            {
                _blueTeamPlayerCount++;
            }
        }

        //Debug.LogError("RED: " + _redTeamPlayerCount + " BLUE: " + _blueTeamPlayerCount);
    }

    [NetworkMethod(FpsGameRPC.PlayerTeamChange)]
    protected void OnPlayerTeamChange(int playerID, byte teamId)
    {
        if (GameState.HasCurrentGame)
        {
            UberStrike.Realtime.Common.CharacterInfo i = GameState.CurrentGame.GetPlayerWithID(playerID);
            if (i != null)
            {
                i.TeamID = (TeamID)teamId;
                UpdatePlayerCounters();
                CmuneEventHandler.Route(new OnPlayerChangeTeamEvent() { PlayerID = playerID, TargetTeamID = i.TeamID });
            }
        }
    }

    protected override void OnSplatGameEvent(int shooter, int target, byte weaponClass, byte bodyPart)
    {
        base.OnSplatGameEvent(shooter, target, weaponClass, bodyPart);

        if (target == MyActorId) _canChangeTeamInThisLife = false;
    }

    public virtual void ChangeTeam()
    {
        //change team outside of rounds
        if (PlayerSpectatorControl.Instance.IsEnabled && HasMyTeamMorePlayers)
        {
            CmuneEventHandler.Route(new OnChangeTeamSuccessEvent() 
                { CurrentTeamID = GameState.LocalCharacter.TeamID });
            SendPlayerTeamChange();
        }
        else if (!HasMyTeamMorePlayers)
        {
            CmuneEventHandler.Route(new OnChangeTeamFailEvent() 
                { Reason = OnChangeTeamFailEvent.FailReason.CannotChangeToATeamWithEqual });
        }
        else if (!_canChangeTeamInThisLife)
        {
            CmuneEventHandler.Route(new OnChangeTeamFailEvent() 
                { Reason = OnChangeTeamFailEvent.FailReason.OnlyOneTeamChangePerLife });
        }
        else
        {
            CmuneEventHandler.Route(new OnChangeTeamSuccessEvent() 
                { CurrentTeamID = GameState.LocalCharacter.TeamID });
            SendPlayerTeamChange();
            if (_isLocalAvatarLoaded && GameState.LocalCharacter.IsAlive)
            {
                //only change team 1x per life
                _canChangeTeamInThisLife = false;
            }
        }
    }

    #region Properties
    protected bool HasMyTeamMorePlayers
    {
        get
        {
            return (GameState.LocalCharacter.TeamID == TeamID.RED && _redTeamPlayerCount > _blueTeamPlayerCount) || 
                   (GameState.LocalCharacter.TeamID == TeamID.BLUE && _blueTeamPlayerCount > _redTeamPlayerCount);
        }
    }

    public int BlueTeamPlayerCount
    {
        get { return _blueTeamPlayerCount; }
    }

    public int RedTeamPlayerCount
    {
        get { return _redTeamPlayerCount; }
    }

    public int RedTeamSplat
    {
        get { return _redTeamSplats; }
    }

    public int BlueTeamSplat
    {
        get { return _blueTeamSplats; }
    }

    public bool CanJoinBlueTeam
    {
        get { return _redTeamPlayerCount >= _blueTeamPlayerCount; }
    }

    public bool CanJoinRedTeam
    {
        get { return _redTeamPlayerCount <= _blueTeamPlayerCount; }
    }
    #endregion

    #region Fields
    protected int _redTeamPlayerCount;
    protected int _blueTeamPlayerCount;

    protected int _redTeamSplats;
    protected int _blueTeamSplats;

    protected bool _canChangeTeamInThisLife;
    #endregion
}
