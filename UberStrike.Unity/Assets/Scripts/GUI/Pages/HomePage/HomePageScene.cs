using UnityEngine;

public class HomePageScene : PageScene
{
    private bool _lastIsPlayerInClan;
    private string _lastClanTag;

    public override PageType PageType
    {
        get { return PageType.Home; }
    }

    protected override void OnLoad()
    {
        Vector3 position;
        Quaternion rotation;
        if (MenuConfiguration.Instance.GetPageAnchorPoint(PageType, out position, out rotation))
        {
            GameState.LocalDecorator.SetPosition(position, rotation);
        }

        if (GameState.LocalDecorator != null)
            GameState.LocalDecorator.HideWeapons();

        AvatarAnimationManager.Instance.ResetAnimationState(PageType);

        EventPopupManager.Instance.ShowNextPopup(1);
    }

    //TODO: needs to be fixed
    private void Update()
    {
        if (_lastIsPlayerInClan != PlayerDataManager.IsPlayerInClan || _lastClanTag != PlayerDataManager.ClanTag)
        {
            GameState.LocalDecorator.HudInformation.SetAvatarLabel(PlayerDataManager.IsPlayerInClan ?
                string.Format("[{0}] {1}", PlayerDataManager.ClanTag, PlayerDataManager.Name) : PlayerDataManager.Name);
            _lastIsPlayerInClan = PlayerDataManager.IsPlayerInClan;
            _lastClanTag = PlayerDataManager.ClanTag;
        }
    }
}