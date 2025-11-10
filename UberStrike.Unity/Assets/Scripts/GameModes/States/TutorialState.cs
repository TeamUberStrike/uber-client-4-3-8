using UberStrike.Realtime.Common;
using Cmune.Util;
using UnityEngine;

class TutorialState : IState
{
    public TutorialState()
    {
        _stateMachine = new StateMachine();
        _stateMachine.RegisterState((int)GameStateId.Paused, new InGamePlayerPausedState(_stateMachine));
    }

    public void OnEnter()
    {
        CmuneEventHandler.AddListener<OnMatchStartEvent>(OnMatchStart);
        CmuneEventHandler.AddListener<OnPlayerRespawnEvent>(OnPlayerRespawn);
        CmuneEventHandler.AddListener<OnPlayerPauseEvent>(OnPlayerPause);

        _tutorialGameMode = new TutorialGameMode(GameConnectionManager.Rmi);
        GameState.CurrentGame = _tutorialGameMode;

        TabScreenPanelGUI.Instance.SetGameName("Tutorial");
        TabScreenPanelGUI.Instance.SetServerName(string.Empty);

        MenuPageManager.Instance.UnloadCurrentPage();
        MenuPageManager.Instance.enabled = false;

        LevelCamera.Instance.SetLevelCamera(GameState.CurrentSpace.Camera,
            GameState.CurrentSpace.DefaultViewPoint.position,
            GameState.CurrentSpace.DefaultViewPoint.rotation);
        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.None);
        GameState.LocalPlayer.SetEnabled(true);

        FrameRateHud.Instance.Enable = true;
        HudUtil.Instance.SetPlayerTeam(TeamID.NONE);
        PlayerStateMsgHud.Instance.DisplayNone();
    }

    public void OnExit()
    {
        CmuneEventHandler.RemoveListener<OnMatchStartEvent>(OnMatchStart);
        CmuneEventHandler.RemoveListener<OnPlayerRespawnEvent>(OnPlayerRespawn);
        CmuneEventHandler.RemoveListener<OnPlayerPauseEvent>(OnPlayerPause);
        GameModeUtil.OnExitGameMode();
        _tutorialGameMode = null;
    }

    public void OnUpdate()
    {
        if (_tutorialGameMode.IsMatchRunning)
        {
            GameModeUtil.UpdatePlayerStateMsg(_tutorialGameMode, false);
        }
    }

    public void OnGUI() 
    {
        _tutorialGameMode.DrawGui();
    }

    #region Private methods

    private void OnMatchStart(OnMatchStartEvent ev)
    {
        HudUtil.Instance.ClearAllFeedbackHud();
    }

    private void OnPlayerRespawn(OnPlayerRespawnEvent ev)
    {
        GamePageManager.Instance.UnloadCurrentPage();
        HudUtil.Instance.ClearAllFeedbackHud();
        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.None);
        if (_tutorialGameMode.Sequence.State == TutorialCinematicSequence.TutorialState.AirlockMouseLookSubtitle)
        {
            GameState.LocalPlayer.IsWalkingEnabled = false;
        }
        HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlags.XpPoints;
    }

    private void OnPlayerPause(OnPlayerPauseEvent ev)
    {
        _stateMachine.PushState((int)GameStateId.Paused);
    }

    #endregion

    #region Private fields

    private TutorialGameMode _tutorialGameMode;
    private StateMachine _stateMachine;

    #endregion
}
