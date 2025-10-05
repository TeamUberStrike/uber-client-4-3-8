class InGamePregameLoadoutState : IState
{
    public void OnEnter()
    {
        GamePageManager.Instance.LoadPage(PageType.PreGame);
        HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlags.XpPoints | HudDrawFlags.InGameChat;
        XpPtsHud.Instance.ResetXp(PlayerDataManager.PlayerLevel);
        XpPtsHud.Instance.IsXpPtsTextVisible = false;
        XpPtsHud.Instance.DisplayPermanently();
    }

    public void OnExit()
    {
        GamePageManager.Instance.UnloadCurrentPage();
    }

    public void OnUpdate() { }

    public void OnGUI() { }
}
