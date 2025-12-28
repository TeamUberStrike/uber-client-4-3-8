using System;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

class DeathMatchState : IState
{
    public GameMetaData GameMetaData { get; set; }

    public DeathMatchState()
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
        if (GameMetaData == null) throw new NullReferenceException("Load death match with invalid GameMetaData");
        _deathMatchGameMode = new DeathMatchGameMode(GameMetaData);
        GameModeUtil.OnEnterGameMode(_deathMatchGameMode);

        TabScreenPanelGUI.Instance.SortPlayersByRank = TabScreenPlayerSorter.SortDeathMatchPlayers;
        QuickItemController.Instance.IsConsumptionEnabled = true;
        QuickItemController.Instance.Restriction.IsEnabled = true;
        QuickItemController.Instance.Restriction.RenewGameUses();

        CmuneEventHandler.AddListener<OnModeInitializedEvent>(OnModeStart);
        CmuneEventHandler.AddListener<OnMatchStartEvent>(OnMatchStart);
        CmuneEventHandler.AddListener<OnMatchEndEvent>(OnMatchEnd);
        CmuneEventHandler.AddListener<OnUpdateDeathMatchScoreEvent>(OnUpdateScore);

        _stateMachine.SetState((int)GameStateId.PregameLoadout);
    }

    public void OnExit()
    {
        _stateMachine.PopAllStates();

        CmuneEventHandler.RemoveListener<OnModeInitializedEvent>(OnModeStart);
        CmuneEventHandler.RemoveListener<OnMatchStartEvent>(OnMatchStart);
        CmuneEventHandler.RemoveListener<OnMatchEndEvent>(OnMatchEnd);
        CmuneEventHandler.RemoveListener<OnUpdateDeathMatchScoreEvent>(OnUpdateScore);

        GameModeUtil.OnExitGameMode();
        _deathMatchGameMode = null;
    }

    public void OnUpdate()
    {
        QuickItemController.Instance.Update();
        if (_deathMatchGameMode.IsMatchRunning)
        {
            _ingameCountdown.Update();
            GameModeUtil.UpdatePlayerStateMsg(_deathMatchGameMode, true);
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

        HudUtil.Instance.SetPlayerTeam(TeamID.NONE);
        XpPtsHud.Instance.OnGameStart(PlayerDataManager.PlayerLevel);
        MatchStatusHud.Instance.RemainingSeconds = 0;
        GameModeObjectiveHud.Instance.DisplayGameMode(GameMode.DeathMatch);
        InGameHelpHud.Instance.EnableChangeTeamHelp = false;
        FrameRateHud.Instance.Enable = true;
    }

    private void OnMatchStart(OnMatchStartEvent ev)
    {
        _ingameCountdown.EndTime = ev.MatchEndServerTicks;
        _stateMachine.SetState((int)GameStateId.Playing);

        MatchStatusHud.Instance.RemainingKills = GameState.CurrentGame.GameData.SplatLimit;
        HudUtil.Instance.ClearAllFeedbackHud();
        XpPtsHud.Instance.OnGameStart(PlayerDataManager.PlayerLevel);
        MatchStatusHud.Instance.ResetKillsLeftAudio();
    }

    private void OnMatchEnd(OnMatchEndEvent ev)
    {
        PlayerLeadStatus.Instance.OnDeathMatchOver();
        _stateMachine.SetState((int)GameStateId.EndOfMatch);
    }

    private void OnUpdateScore(OnUpdateDeathMatchScoreEvent ev)
    {
        int myKills = ev.MyScore;
        int otherKills = ev.OtherPlayerScore;
        bool isLeading = ev.IsLeading;

        int remainingKills = GameMetaData.SplatLimit - Mathf.Max(myKills, otherKills);
        bool playAudio =
             !_deathMatchGameMode.IsGameAboutToEnd
             && !(myKills == GameMetaData.SplatLimit || otherKills == GameMetaData.SplatLimit) //we didn't reach the kill limit
             && _deathMatchGameMode.PlayerCount > 1;
        if (remainingKills != MatchStatusHud.Instance.RemainingKills)
        {
            MatchStatusHud.Instance.RemainingKills = remainingKills;
            if (playAudio)
            {
                MatchStatusHud.Instance.PlayKillsLeftAudio(GameMetaData.SplatLimit - Mathf.Max(otherKills, myKills));
            }
        }
        PlayerLeadStatus.Instance.PlayLeadAudio(myKills, otherKills, isLeading, playAudio);
    }

    #endregion

    #region Private fields

    private DeathMatchGameMode _deathMatchGameMode;
    private InGameCountdown _ingameCountdown;
    private StateMachine _stateMachine;
    private const HudDrawFlags _hudDrawFlag = HudDrawFlags.HealthArmor |
                    HudDrawFlags.Ammo | HudDrawFlags.Weapons | HudDrawFlags.EventStream |
                    HudDrawFlags.Reticle | HudDrawFlags.XpPoints | HudDrawFlags.StateMsg |
                    HudDrawFlags.RoundTime | HudDrawFlags.RemainingKill | HudDrawFlags.InGameChat;

    #endregion
}
