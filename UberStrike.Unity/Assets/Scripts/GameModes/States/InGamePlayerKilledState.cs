using Cmune.Util;
using UnityEngine;

class InGamePlayerKilledState : IState
{
    public InGamePlayerKilledState(StateMachine stateMachine, HudDrawFlags gameModeFlag, bool showInGameHelp)
    {
        _gameModeFlag = gameModeFlag;
        _showInGameHelp = showInGameHelp;
        _stateMachine = stateMachine;
    }

    public void OnEnter()
    {
        GameModeObjectiveHud.Instance.Clear();
        InGameFeatHud.Instance.AnimationScheduler.ClearAll();
        HudDrawFlagGroup.Instance.BaseDrawFlag &= ~(HudDrawFlags.Weapons | HudDrawFlags.Reticle | 
            HudDrawFlags.HealthArmor | HudDrawFlags.Ammo | HudDrawFlags.EventStream);
        if (_showInGameHelp)
        {
            HudDrawFlagGroup.Instance.BaseDrawFlag |= HudDrawFlags.InGameHelp;
        }
        Screen.lockCursor = false;

        QuickItemController.Instance.IsEnabled = false;

        CmuneEventHandler.AddListener<OnPlayerRespawnEvent>(OnPlayerRespawn);
    }

    public void OnExit()
    {
        HudDrawFlagGroup.Instance.BaseDrawFlag = _gameModeFlag;
        GamePageManager.Instance.UnloadCurrentPage();
        HudUtil.Instance.ClearAllFeedbackHud();
        PlayerStateMsgHud.Instance.ButtonEnabled = false;

        QuickItemController.Instance.IsEnabled = true;

        CmuneEventHandler.RemoveListener<OnPlayerRespawnEvent>(OnPlayerRespawn);
    }

    public void OnUpdate()
    {
    }

    public void OnGUI() { }

    private void OnPlayerRespawn(OnPlayerRespawnEvent ev)
    {
        _stateMachine.PopState();
    }

    private HudDrawFlags _gameModeFlag;
    private bool _showInGameHelp;
    private StateMachine _stateMachine;
}
