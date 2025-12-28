using Cmune.Util;
using UnityEngine;

class InGameSpectatingState : IState
{
    public InGameSpectatingState(StateMachine stateMachine, HudDrawFlags gameModeFlag)
    {
        _gameModeFlag = gameModeFlag;
        _stateMachine = stateMachine;
    }

    public void OnEnter()
    {
        HudDrawFlagGroup.Instance.BaseDrawFlag &= ~(HudDrawFlags.Weapons | HudDrawFlags.Reticle | 
            HudDrawFlags.HealthArmor | HudDrawFlags.Ammo);
        HudDrawFlagGroup.Instance.BaseDrawFlag |= (HudDrawFlags.InGameHelp);
        CmuneEventHandler.AddListener<OnPlayerPauseEvent>(OnPlayerPaused);

        QuickItemController.Instance.IsEnabled = false;

        if (GameState.LocalPlayer.IsGamePaused)
        {
            _stateMachine.PushState((int)GameStateId.Paused);
        }
    }

    public void OnExit()
    {
        GamePageManager.Instance.UnloadCurrentPage();
        CmuneEventHandler.RemoveListener<OnPlayerPauseEvent>(OnPlayerPaused);
        HudDrawFlagGroup.Instance.BaseDrawFlag = _gameModeFlag;
    }

    public void OnUpdate()
    {
    }

    public void OnGUI() { }

    private void OnPlayerPaused(OnPlayerPauseEvent ev)
    {
        _stateMachine.PushState((int)GameStateId.Paused);
    }

    private HudDrawFlags _gameModeFlag;
    private StateMachine _stateMachine;
}
