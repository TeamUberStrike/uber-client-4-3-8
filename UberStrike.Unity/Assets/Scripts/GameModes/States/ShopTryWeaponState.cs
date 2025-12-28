using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;
using Cmune.DataCenter.Common.Entities;

class ShopTryWeaponState : IState
{
    public int ItemId { get; set; }

    public void OnEnter()
    {
        CmuneEventHandler.AddListener<OnPlayerRespawnEvent>(OnPlayerRespawn);
        CmuneEventHandler.AddListener<OnMobileBackPressed>(OnMobileBackPressed);
        CmuneEventHandler.AddListener<OnPlayerPauseEvent>(OnPlayerPause);

        _shopGameMode = new ShopWeaponMode(GameConnectionManager.Rmi);
        GameState.CurrentGame = _shopGameMode;

        SpawnPointManager.Instance.ConfigureSpawnPoints(GameState.CurrentSpace.SpawnPoints.GetComponentsInChildren<SpawnPoint>(true));
        LevelCamera.Instance.SetLevelCamera(GameState.CurrentSpace.Camera,
            GameState.CurrentSpace.DefaultViewPoint.position,
            GameState.CurrentSpace.DefaultViewPoint.rotation);
        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.None);
        GameState.LocalPlayer.SetEnabled(true);

        QuickItemController.Instance.IsEnabled = true;
        QuickItemController.Instance.IsConsumptionEnabled = false;
        QuickItemController.Instance.Restriction.IsEnabled = false;
        _shopGameMode.InitializeMode();

        MenuPageManager.Instance.UnloadCurrentPage();

        XpPtsHud.Instance.OnGameStart(PlayerDataManager.PlayerLevel);
        HudUtil.Instance.SetPlayerTeam(TeamID.NONE);
        PlayerStateMsgHud.Instance.DisplayNone();
        FrameRateHud.Instance.Enable = true;
        HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlags.HealthArmor |
                HudDrawFlags.Ammo | HudDrawFlags.Weapons | HudDrawFlags.Reticle |
                HudDrawFlags.XpPoints | HudDrawFlags.StateMsg;
        ShowShopMessages();
    }

    public void OnExit()
    {
        CmuneEventHandler.RemoveListener<OnPlayerRespawnEvent>(OnPlayerRespawn);
        CmuneEventHandler.RemoveListener<OnMobileBackPressed>(OnMobileBackPressed);
        CmuneEventHandler.RemoveListener<OnPlayerPauseEvent>(OnPlayerPause);
        GameModeUtil.OnExitGameMode();
        _shopGameMode = null;
    }

    public void OnUpdate()
    {
        QuickItemController.Instance.Update();

    }

    #region Private methods

    private void Unload()
    {
        // the sequence of the code below cannot be changed, otherwise will generate exceptions
        _shopGameMode.TargetController.Disable();
        MenuPageManager.Instance.LoadPage(PageType.Shop, true);
        GameStateController.Instance.UnloadGameMode();
    }

    public void OnGUI()
    {
    }

    private void OnPlayerRespawn(OnPlayerRespawnEvent ev)
    {
        WeaponController.Instance.SetPickupWeapon(ItemId, false, true);
    }

    private void ShowShopMessages()
    {
        if (!ApplicationDataManager.IsMobile)
        {
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage, string.Empty);
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage, LocalizedStrings.ShopTutorialMsg01);
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage, LocalizedStrings.MessageQuickItemsTry);
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage, LocalizedStrings.ShopTutorialMsg02, 20.0f);
        }
    }

    private void OnMobileBackPressed(OnMobileBackPressed ev)
    {
        QuitTryWeapon();
    }

    private void OnPlayerPause(OnPlayerPauseEvent ev)
    {
        QuitTryWeapon();
    }

    private void QuitTryWeapon()
    {
        // the sequence of the code below cannot be changed, otherwise will generate exceptions
        _shopGameMode.TargetController.Disable();
        MenuPageManager.Instance.LoadPage(PageType.Shop, true);
        GameStateController.Instance.UnloadGameMode();

        if (ItemId > 0)
        {
            BuyPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.BuyItem) as BuyPanelGUI;
            if (panel)
            {
                panel.SetItem(ItemManager.Instance.GetItemInShop(ItemId), BuyingLocationType.Shop, BuyingRecommendationType.None);
            }
        }
    }

    #endregion

    #region Private fields

    private ShopWeaponMode _shopGameMode;

    #endregion
}
