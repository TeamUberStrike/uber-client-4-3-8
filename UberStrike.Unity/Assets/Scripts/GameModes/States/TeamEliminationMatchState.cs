using UberStrike.Realtime.Common;
using Cmune.Util;
using System;
using UnityEngine;

class TeamEliminationMatchState : IState
{
    public GameMetaData GameMetaData { get; set; }

    public TeamEliminationMatchState()
    {
        _ingameCountdown = new InGameCountdown();
        _stateMachine = new StateMachine();
        _stateMachine.RegisterState((int)GameStateId.PregameLoadout, new InGamePregameLoadoutState());
        _stateMachine.RegisterState((int)GameStateId.GraceCountdown, new InGameGraceCountdownState());
        _stateMachine.RegisterState((int)GameStateId.Playing, new InGamePlayingState(_stateMachine, _hudDrawFlag));
        _stateMachine.RegisterState((int)GameStateId.EndOfMatch, new InGameEndOfMatchState());
        _stateMachine.RegisterState((int)GameStateId.Killed, new InGamePlayerKilledState(_stateMachine, _hudDrawFlag, true));
        _stateMachine.RegisterState((int)GameStateId.Paused, new InGamePlayerPausedState(_stateMachine));
        _stateMachine.RegisterState((int)GameStateId.Spectating, new InGameSpectatingState(_stateMachine, _hudDrawFlag));
    }

    public void OnEnter()
    {
        if (GameMetaData == null) throw new NullReferenceException("Load team elimination match with invalid GameMetaData");
        _teamEliminationMatchGameMode = new TeamEliminationGameMode(GameMetaData);
        GameModeUtil.OnEnterGameMode(_teamEliminationMatchGameMode);

        TabScreenPanelGUI.Instance.SortPlayersByRank = TabScreenPlayerSorter.SortTeamMatchPlayers;
        QuickItemController.Instance.IsConsumptionEnabled = true;
        QuickItemController.Instance.Restriction.IsEnabled = true;
        QuickItemController.Instance.Restriction.RenewGameUses();

        CmuneEventHandler.AddListener<OnModeInitializedEvent>(OnModeStart);
        CmuneEventHandler.AddListener<OnMatchEndEvent>(OnMatchEnd);
        CmuneEventHandler.AddListener<OnUpdateRoundStatsEvent>(OnUpdateRoundStats);
        CmuneEventHandler.AddListener<OnGraceTimeCountdownEvent>(OnGraceTimeCountdown);
        CmuneEventHandler.AddListener<OnTeamEliminationRoundStartEvent>(OnRoundStart);
        CmuneEventHandler.AddListener<OnTeamEliminationRoundEndEvent>(OnRoundEnd);
        CmuneEventHandler.AddListener<OnTeamEliminationSyncRoundTimeEvent>(OnSyncRoundTime);
        CmuneEventHandler.AddListener<OnSetWaitingForPlayersEvent>(OnSetWatingForPlayers);
        CmuneEventHandler.AddListener<OnChangeTeamSuccessEvent>(TeamGameModeUtil.OnChangeTeamSuccess);
        CmuneEventHandler.AddListener<OnChangeTeamFailEvent>(TeamGameModeUtil.OnChangeTeamFail);
        CmuneEventHandler.AddListener<OnPlayerChangeTeamEvent>(OnPlayerChangeTeam);

        _stateMachine.SetState((int)GameStateId.PregameLoadout);
    }

    public void OnExit()
    {
        _stateMachine.PopAllStates();

        CmuneEventHandler.RemoveListener<OnModeInitializedEvent>(OnModeStart);
        CmuneEventHandler.RemoveListener<OnMatchEndEvent>(OnMatchEnd);
        CmuneEventHandler.RemoveListener<OnUpdateRoundStatsEvent>(OnUpdateRoundStats);
        CmuneEventHandler.RemoveListener<OnGraceTimeCountdownEvent>(OnGraceTimeCountdown);
        CmuneEventHandler.RemoveListener<OnTeamEliminationRoundStartEvent>(OnRoundStart);
        CmuneEventHandler.RemoveListener<OnTeamEliminationRoundEndEvent>(OnRoundEnd);
        CmuneEventHandler.RemoveListener<OnTeamEliminationSyncRoundTimeEvent>(OnSyncRoundTime);
        CmuneEventHandler.RemoveListener<OnSetWaitingForPlayersEvent>(OnSetWatingForPlayers);
        CmuneEventHandler.RemoveListener<OnChangeTeamSuccessEvent>(TeamGameModeUtil.OnChangeTeamSuccess);
        CmuneEventHandler.RemoveListener<OnChangeTeamFailEvent>(TeamGameModeUtil.OnChangeTeamFail);
        CmuneEventHandler.RemoveListener<OnPlayerChangeTeamEvent>(OnPlayerChangeTeam);

        GameModeUtil.OnExitGameMode();
        _teamEliminationMatchGameMode = null;
    }

    public void OnUpdate()
    {
        QuickItemController.Instance.Update();
        if (_teamEliminationMatchGameMode.IsMatchRunning)
        {
            _ingameCountdown.Update();
            TeamGameModeUtil.DetectTeamChange(_teamEliminationMatchGameMode);
            GameModeUtil.UpdatePlayerStateMsg(_teamEliminationMatchGameMode, true);
            UpdateSpectatorStateMsg();
        }
        else
        {
            _ingameCountdown.Stop();
        }
        _stateMachine.Update();
    }

    public void OnGUI()
    {
    }

    #region Private methods

    private void OnModeStart(OnModeInitializedEvent ev)
    {
        _stateMachine.SetState((int)GameStateId.Playing);

        HudUtil.Instance.SetPlayerTeam(GameState.LocalCharacter.TeamID);
        GameModeObjectiveHud.Instance.DisplayGameMode(GameMode.TeamElimination);
        XpPtsHud.Instance.OnGameStart(PlayerDataManager.PlayerLevel);
        MatchStatusHud.Instance.RemainingSeconds = 0;
        InGameHelpHud.Instance.EnableChangeTeamHelp = true;
        HudUtil.Instance.SetTeamScore(0, 0);
        PlayerLeadStatus.Instance.ResetPlayerLead();
        MatchStatusHud.Instance.RemainingRoundsText = "";
        GlobalUIRibbon.Instance.Hide();
        GameState.LocalPlayer.UnPausePlayer();
    }

    private void OnSetWatingForPlayers(OnSetWaitingForPlayersEvent ev)
    {
        PlayerStateMsgHud.Instance.DisplayWaitingForOtherPlayerMsg();
        _stateMachine.SetState((int)GameStateId.Playing);
    }

    private void OnMatchEnd(OnMatchEndEvent ev)
    {
        _stateMachine.SetState((int)GameStateId.EndOfMatch);

        int redTeamSplats = _teamEliminationMatchGameMode.RedTeamSplat;
        int blueTeamSplats = _teamEliminationMatchGameMode.BlueTeamSplat;
        if (redTeamSplats > blueTeamSplats)
        {
            PopupHud.Instance.PopupWinTeam(TeamID.RED);
        }
        else if (redTeamSplats < blueTeamSplats)
        {
            PopupHud.Instance.PopupWinTeam(TeamID.BLUE);
        }
        else
        {
            PopupHud.Instance.PopupWinTeam(TeamID.NONE);
        }
    }

    private void OnUpdateRoundStats(OnUpdateRoundStatsEvent ev)
    {
        Color c;
        MatchStatusHud.Instance.RemainingRoundsText = GetRoundStatus(out c); //TODO: the color is currently not used.
        HudUtil.Instance.SetTeamScore(ev.BlueWinRoundCount, ev.RedWinRoundCount);
    }

    private void OnGraceTimeCountdown(OnGraceTimeCountdownEvent ev)
    {
        _stateMachine.SetState((int)GameStateId.GraceCountdown);
    }

    private void OnRoundStart(OnTeamEliminationRoundStartEvent ev)
    {
        QuickItemController.Instance.Restriction.RenewRoundUses();
        HudUtil.Instance.ClearAllFeedbackHud();

        _ingameCountdown.EndTime = ev.RoundEndServerTicks;
        if (PlayerSpectatorControl.Instance.IsEnabled)
        {
            _stateMachine.SetState((int)GameStateId.Spectating);
        }
        else
        {
            _stateMachine.SetState((int)GameStateId.Playing);
        }
    }

    private void OnRoundEnd(OnTeamEliminationRoundEndEvent ev)
    {
        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.AfterRound);
        QuickItemController.Instance.IsEnabled = false;
        PopupHud.Instance.PopupWinTeam(ev.WinTeamID);
    }

    private void OnSyncRoundTime(OnTeamEliminationSyncRoundTimeEvent ev)
    {
        _ingameCountdown.EndTime = ev.RoundEndServerTicks;
    }

    private void OnPlayerChangeTeam(OnPlayerChangeTeamEvent ev)
    {
        TeamGameModeUtil.OnPlayerChangeTeam(_teamEliminationMatchGameMode,
            ev.PlayerID, ev.PlayerInfo, ev.TargetTeamID);
    }

    private string GetRoundStatus(out Color color)
    {
        color = Color.white;
        int redTeamSplat = _teamEliminationMatchGameMode.RedTeamSplat;
        int blueTeamSplat = _teamEliminationMatchGameMode.BlueTeamSplat;
        bool redMatchRound = redTeamSplat == (GameMetaData.SplatLimit - 1);
        bool blueMatchRound = blueTeamSplat == (GameMetaData.SplatLimit - 1);

        if (blueMatchRound && redMatchRound)
        {
            return LocalizedStrings.FinalRoundCaps;
        }
        else if (blueMatchRound)
        {
            color = ColorScheme.HudTeamBlue;
            return string.Format(LocalizedStrings.FinalRoundX, LocalizedStrings.BlueCaps);
        }
        else if (redMatchRound)
        {
            color = ColorScheme.HudTeamRed;
            return string.Format(LocalizedStrings.FinalRoundX, LocalizedStrings.RedCaps);
        }
        else
        {
            return string.Format(LocalizedStrings.NRoundsLeft, GameMetaData.SplatLimit - Mathf.Max(blueTeamSplat, redTeamSplat));
        }
    }

    private void UpdateSpectatorStateMsg()
    {
        if (GlobalUIRibbon.Instance.IsEnabled && Screen.lockCursor)
            Screen.lockCursor = false;

        if (_teamEliminationMatchGameMode.IsWaitingForPlayers)
        {
            PlayerStateMsgHud.Instance.DisplayWaitingForOtherPlayerMsg();
        }
        else if (PlayerSpectatorControl.Instance.IsEnabled)
        {
            if (PlayerSpectatorControl.Instance.IsFollowingPlayer)
            {
                UberStrike.Realtime.Common.CharacterInfo i = _teamEliminationMatchGameMode.GetPlayerWithID(PlayerSpectatorControl.Instance.CurrentActorId);
                PlayerStateMsgHud.Instance.DisplaySpectatorFollowingMsg(i);
            }
            else
            {
                PlayerStateMsgHud.Instance.DisplaySpectatorModeMsg();
            }

            if (GameState.LocalPlayer.IsGamePaused)
            {
                HudUtil.Instance.ShowContinueButton();
            }
        }
        else
        {
            PlayerStateMsgHud.Instance.DisplayNone();
        }
    }

    #endregion

    #region Private fields

    private TeamEliminationGameMode _teamEliminationMatchGameMode;
    private InGameCountdown _ingameCountdown;
    private StateMachine _stateMachine;
    private const HudDrawFlags _hudDrawFlag = HudDrawFlags.HealthArmor |
                        HudDrawFlags.Ammo | HudDrawFlags.Weapons | HudDrawFlags.EventStream |
                        HudDrawFlags.Reticle | HudDrawFlags.XpPoints | HudDrawFlags.StateMsg |
                        HudDrawFlags.RoundTime | HudDrawFlags.RemainingKill | HudDrawFlags.Score |
                        HudDrawFlags.InGameChat;
    #endregion
}
