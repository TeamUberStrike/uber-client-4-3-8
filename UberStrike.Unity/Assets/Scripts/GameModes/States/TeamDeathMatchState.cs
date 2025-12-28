using System;
using Cmune.Util;
using UberStrike.Realtime.Common;

class TeamDeathMatchState : IState
{
    public GameMetaData GameMetaData { get; set; }

    public TeamDeathMatchState()
    {
        _ingameCountdown = new InGameCountdown();
        _stateMachine = new StateMachine();
        _stateMachine.RegisterState((int)GameStateId.PregameLoadout, new InGamePregameLoadoutState());
        _stateMachine.RegisterState((int)GameStateId.Playing, new InGamePlayingState(_stateMachine, _hudDrawFlag));
        _stateMachine.RegisterState((int)GameStateId.EndOfMatch, new InGameEndOfMatchState());
        _stateMachine.RegisterState((int)GameStateId.Killed, new InGamePlayerKilledState(_stateMachine, _hudDrawFlag, true));
        _stateMachine.RegisterState((int)GameStateId.Paused, new InGamePlayerPausedState(_stateMachine));
        _stateMachine.RegisterState((int)GameStateId.Spectating, new InGameSpectatingState(_stateMachine, _hudDrawFlag));
    }

    public void OnEnter()
    {
        if (GameMetaData == null) throw new NullReferenceException("Load team death match with invalid GameMetaData");
        _teamDeathMatchGameMode = new TeamDeathMatchGameMode(GameMetaData);
        GameModeUtil.OnEnterGameMode(_teamDeathMatchGameMode);

        TabScreenPanelGUI.Instance.SortPlayersByRank = TabScreenPlayerSorter.SortTeamMatchPlayers;
        QuickItemController.Instance.IsConsumptionEnabled = true;
        QuickItemController.Instance.Restriction.IsEnabled = true;
        QuickItemController.Instance.Restriction.RenewGameUses();

        CmuneEventHandler.AddListener<OnModeInitializedEvent>(OnModeStart);
        CmuneEventHandler.AddListener<OnMatchStartEvent>(OnMatchStart);
        CmuneEventHandler.AddListener<OnMatchEndEvent>(OnMatchEnd);
        CmuneEventHandler.AddListener<OnUpdateTeamScoreEvent>(OnUpdateTeamScore);
        CmuneEventHandler.AddListener<OnChangeTeamSuccessEvent>(TeamGameModeUtil.OnChangeTeamSuccess);
        CmuneEventHandler.AddListener<OnChangeTeamFailEvent>(TeamGameModeUtil.OnChangeTeamFail);
        CmuneEventHandler.AddListener<OnPlayerChangeTeamEvent>(OnPlayerChangeTeam);

        _stateMachine.SetState((int)GameStateId.PregameLoadout);
    }

    public void OnExit()
    {
        _stateMachine.PopAllStates();

        CmuneEventHandler.RemoveListener<OnModeInitializedEvent>(OnModeStart);
        CmuneEventHandler.RemoveListener<OnMatchStartEvent>(OnMatchStart);
        CmuneEventHandler.RemoveListener<OnMatchEndEvent>(OnMatchEnd);
        CmuneEventHandler.RemoveListener<OnUpdateTeamScoreEvent>(OnUpdateTeamScore);
        CmuneEventHandler.RemoveListener<OnChangeTeamSuccessEvent>(TeamGameModeUtil.OnChangeTeamSuccess);
        CmuneEventHandler.RemoveListener<OnChangeTeamFailEvent>(TeamGameModeUtil.OnChangeTeamFail);
        CmuneEventHandler.RemoveListener<OnPlayerChangeTeamEvent>(OnPlayerChangeTeam);

        GameModeUtil.OnExitGameMode();
        _teamDeathMatchGameMode = null;
    }

    public void OnUpdate()
    {
        QuickItemController.Instance.Update();
        if (_teamDeathMatchGameMode.IsMatchRunning)
        {
            _ingameCountdown.Update();
            TeamGameModeUtil.DetectTeamChange(_teamDeathMatchGameMode);
            GameModeUtil.UpdatePlayerStateMsg(_teamDeathMatchGameMode, true);
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
        HudUtil.Instance.SetPlayerTeam(GameState.LocalCharacter.TeamID);
        GameModeObjectiveHud.Instance.DisplayGameMode(GameMode.TeamDeathMatch);
        XpPtsHud.Instance.OnGameStart(PlayerDataManager.PlayerLevel);
        MatchStatusHud.Instance.RemainingSeconds = 0;
        InGameHelpHud.Instance.EnableChangeTeamHelp = true;
        FrameRateHud.Instance.Enable = true;
    }

    private void OnMatchStart(OnMatchStartEvent ev)
    {
        _ingameCountdown.EndTime = ev.MatchEndServerTicks;
        _stateMachine.SetState((int)GameStateId.Playing);

        HudUtil.Instance.ClearAllFeedbackHud();
        XpPtsHud.Instance.OnGameStart(PlayerDataManager.PlayerLevel);
        MatchStatusHud.Instance.ResetKillsLeftAudio();
        MatchStatusHud.Instance.RemainingKills = _teamDeathMatchGameMode.GameData.SplatLimit;
        PlayerLeadStatus.Instance.ResetPlayerLead();
        HudUtil.Instance.SetTeamScore(0, 0);
    }

    private void OnMatchEnd(OnMatchEndEvent ev)
    {
        _stateMachine.SetState((int)GameStateId.EndOfMatch);

        int redTeamSplats = _teamDeathMatchGameMode.RedTeamSplat;
        int blueTeamSplats = _teamDeathMatchGameMode.BlueTeamSplat;
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

    private void OnUpdateTeamScore(OnUpdateTeamScoreEvent ev)
    {
        int redScore = ev.RedScore;
        int blueScore = ev.BlueScore;
        bool isLeading = ev.IsLeading;

        int myKills = (GameState.LocalCharacter.TeamID == TeamID.RED ? redScore : blueScore);
        MatchStatusHud.Instance.RemainingKills = GameMetaData.SplatLimit - myKills;

        bool playAudio =
            !_teamDeathMatchGameMode.IsGameAboutToEnd &&
            !(blueScore == GameMetaData.SplatLimit || redScore == GameMetaData.SplatLimit) && // we didn't reach the kill limit
            _teamDeathMatchGameMode.RedTeamPlayerCount > 0 &&
            _teamDeathMatchGameMode.BlueTeamPlayerCount > 0; //we have more than one player

        if (playAudio)
        {
            MatchStatusHud.Instance.PlayKillsLeftAudio(GameMetaData.SplatLimit - Math.Max(redScore, blueScore));
        }

        switch (GameState.LocalCharacter.TeamID)
        {
            case TeamID.RED:
                PlayerLeadStatus.Instance.PlayLeadAudio(redScore, blueScore, isLeading, playAudio);
                break;
            case TeamID.BLUE:
                PlayerLeadStatus.Instance.PlayLeadAudio(blueScore, redScore, isLeading, playAudio);
                break;
        }

        HudUtil.Instance.SetTeamScore(blueScore, redScore);
    }

    private void OnPlayerChangeTeam(OnPlayerChangeTeamEvent ev)
    {
        TeamGameModeUtil.OnPlayerChangeTeam(_teamDeathMatchGameMode,
            ev.PlayerID, ev.PlayerInfo, ev.TargetTeamID);
    }

    #endregion

    #region Private fields

    private TeamDeathMatchGameMode _teamDeathMatchGameMode;
    private InGameCountdown _ingameCountdown;
    private StateMachine _stateMachine;
    private const HudDrawFlags _hudDrawFlag = HudDrawFlags.HealthArmor |
                        HudDrawFlags.Ammo | HudDrawFlags.Weapons | HudDrawFlags.EventStream |
                        HudDrawFlags.Reticle | HudDrawFlags.XpPoints | HudDrawFlags.StateMsg |
                        HudDrawFlags.RoundTime | HudDrawFlags.RemainingKill | HudDrawFlags.Score |
                        HudDrawFlags.InGameChat;

    #endregion
}
