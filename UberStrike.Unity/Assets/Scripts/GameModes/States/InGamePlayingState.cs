using Cmune.Util;

class InGamePlayingState : IState
{
    public InGamePlayingState(StateMachine stateMachine, HudDrawFlags hudDrawFlag)
    {
        _stateMachine = stateMachine;
        _killComboCounter = new KillComboCounter();
        _hudDrawFlag = hudDrawFlag;
    }

    public void OnEnter()
    {
        PlayerLeadStatus.Instance.ResetPlayerLead();
        HudDrawFlagGroup.Instance.BaseDrawFlag = _hudDrawFlag;
        _killComboCounter.ResetCounter();

        QuickItemController.Instance.IsEnabled = true;

        CmuneEventHandler.AddListener<OnPlayerKillEnemyEvent>(OnPlayerKillEnemy);
        CmuneEventHandler.AddListener<OnPlayerSuicideEvent>(GameModeUtil.OnPlayerSuicide);
        CmuneEventHandler.AddListener<OnPlayerKilledEvent>(GameModeUtil.OnPlayerKilled);
        CmuneEventHandler.AddListener<OnPlayerDamageEvent>(GameModeUtil.OnPlayerDamage);

        CmuneEventHandler.AddListener<OnPlayerDeadEvent>(OnPlayerDead);
        CmuneEventHandler.AddListener<OnPlayerPauseEvent>(OnPlayerPaused);
        CmuneEventHandler.AddListener<OnPlayerSpectatingEvent>(OnPlayerSpectating);
        CmuneEventHandler.AddListener<OnCameraZoomInEvent>(OnCameraZoomIn);
        CmuneEventHandler.AddListener<OnCameraZoomOutEvent>(OnCameraZoomOut);

        if (GameState.LocalPlayer.IsGamePaused)
        {
            _stateMachine.PushState((int)GameStateId.Paused);
        }

        HudDrawFlagGroup.Instance.RemoveFlag(cameraZoomedDrawFlagTuning);
    }

    public void OnExit()
    {
        QuickItemController.Instance.IsEnabled = false;

        CmuneEventHandler.RemoveListener<OnPlayerKillEnemyEvent>(OnPlayerKillEnemy);
        CmuneEventHandler.RemoveListener<OnPlayerSuicideEvent>(GameModeUtil.OnPlayerSuicide);
        CmuneEventHandler.RemoveListener<OnPlayerKilledEvent>(GameModeUtil.OnPlayerKilled);
        CmuneEventHandler.RemoveListener<OnPlayerDamageEvent>(GameModeUtil.OnPlayerDamage);
        CmuneEventHandler.RemoveListener<OnPlayerDeadEvent>(OnPlayerDead);
        CmuneEventHandler.RemoveListener<OnPlayerPauseEvent>(OnPlayerPaused);
        CmuneEventHandler.RemoveListener<OnPlayerSpectatingEvent>(OnPlayerSpectating);
        CmuneEventHandler.RemoveListener<OnCameraZoomInEvent>(OnCameraZoomIn);
        CmuneEventHandler.RemoveListener<OnCameraZoomOutEvent>(OnCameraZoomOut);
    }

    public void OnUpdate()
    {
    }

    public void OnGUI() { }

    private void OnPlayerKillEnemy(OnPlayerKillEnemyEvent ev)
    {
        _killComboCounter.OnKillEnemy();
        GameModeUtil.OnPlayerKillEnemy(ev);
    }

    private void OnPlayerPaused(OnPlayerPauseEvent ev)
    {
        _stateMachine.PushState((int)GameStateId.Paused);
    }

    private void OnPlayerSpectating(OnPlayerSpectatingEvent ev)
    {
        _stateMachine.SetState((int)GameStateId.Spectating);
    }

    private void OnPlayerDead(OnPlayerDeadEvent ev)
    {
        _stateMachine.PushState((int)GameStateId.Killed);
    }

    private void OnCameraZoomIn(OnCameraZoomInEvent ev)
    {
        HudDrawFlagGroup.Instance.AddFlag(cameraZoomedDrawFlagTuning);
    }

    private void OnCameraZoomOut(OnCameraZoomOutEvent ev)
    {
        HudDrawFlagGroup.Instance.RemoveFlag(cameraZoomedDrawFlagTuning);
    }

    private StateMachine _stateMachine;
    private KillComboCounter _killComboCounter;
    private HudDrawFlags _hudDrawFlag;

    private const HudDrawFlags cameraZoomedDrawFlagTuning = ~HudDrawFlags.Weapons;
}
