using Cmune.Util;

class InGamePlayerPausedState : IState
{
    public InGamePlayerPausedState(StateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public void OnEnter()
    {
        HudDrawFlagGroup.Instance.AddFlag(pauseDrawFlagTuning);
        HudUtil.Instance.ShowContinueButton();
        if (!ApplicationDataManager.IsMobile)
            WeaponsHud.Instance.QuickItems.Expand();
        CmuneEventHandler.AddListener<OnPlayerUnpauseEvent>(OnPlayerUnpaused);
    }

    public void OnExit()
    {
        PlayerStateMsgHud.Instance.ButtonEnabled = false;
        HudDrawFlagGroup.Instance.RemoveFlag(pauseDrawFlagTuning);
        if (!ApplicationDataManager.IsMobile)
          WeaponsHud.Instance.QuickItems.Collapse();
        CmuneEventHandler.RemoveListener<OnPlayerUnpauseEvent>(OnPlayerUnpaused);
    }

    public void OnUpdate()
    {
    }

    public void OnGUI()
    {
    }

    private void OnPlayerUnpaused(OnPlayerUnpauseEvent ev)
    {
        _stateMachine.PopState();
    }

    private StateMachine _stateMachine;
    private const HudDrawFlags pauseDrawFlagTuning = ~(HudDrawFlags.Reticle | HudDrawFlags.Weapons);
}
