class InGameGraceCountdownState : IState
{
    public void OnEnter()
    {
        HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlags.HealthArmor |
                    HudDrawFlags.Ammo | HudDrawFlags.Weapons | HudDrawFlags.EventStream |
                    HudDrawFlags.XpPoints | HudDrawFlags.StateMsg |
                    HudDrawFlags.RoundTime | HudDrawFlags.RemainingKill | HudDrawFlags.Score |
                    HudDrawFlags.InGameChat;
        HudUtil.Instance.ClearAllFeedbackHud();
        PlayerStateMsgHud.Instance.DisplayNone();
        PopupHud.Instance.PopupRoundStart();
    }

    public void OnExit() { }

    public void OnUpdate() { }

    public void OnGUI() { }
}