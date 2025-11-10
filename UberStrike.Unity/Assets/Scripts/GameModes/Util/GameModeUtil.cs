using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Core.Types;

static class GameModeUtil
{
    public static void OnEnterGameMode(FpsGameMode gameMode)
    {
        GameState.CurrentGame = gameMode;
        TabScreenPanelGUI.Instance.SetGameName(gameMode.GameData.RoomName);
        TabScreenPanelGUI.Instance.SetServerName(GameServerManager.Instance.GetServerName(gameMode.GameData.ServerConnection));

        MenuPageManager.Instance.UnloadCurrentPage();
        MenuPageManager.Instance.enabled = false;

        LevelCamera.Instance.SetLevelCamera(GameState.CurrentSpace.Camera,
            GameState.CurrentSpace.DefaultViewPoint.position,
            GameState.CurrentSpace.DefaultViewPoint.rotation);
        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.None);
        GameState.LocalPlayer.SetEnabled(true);
    }

    public static void OnExitGameMode()
    {
        GameConnectionManager.Stop();

        LevelCamera.Instance.ReleaseCamera();
        WeaponController.Instance.StopInputHandler();
        ProjectileManager.Instance.ClearAll();

        GamePageManager.Instance.UnloadCurrentPage();
        MenuPageManager.Instance.enabled = true;

        GameState.CurrentGame.UnloadAllPlayers();
        GameState.CurrentGame = null;
        GameState.LocalPlayer.SetEnabled(false);

        HudUtil.Instance.ClearAllHud();

        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.None);

        /* Enabled the avatar if in ragdoll mode */
        if (GameState.LocalDecorator)
        {
            if (!GameState.LocalDecorator.gameObject.active)
            {
                GameState.LocalDecorator.DisableRagdoll();
            }
            GameState.LocalDecorator.MeshRenderer.enabled = true;
            GameState.LocalDecorator.HudInformation.enabled = true;
        }
        QuickItemController.Instance.Clear();
    }

    public static void OnPlayerDamage(OnPlayerDamageEvent ev)
    {
        DamageFeedbackHud.Instance.AddDamageMark(Mathf.Clamp01(ev.DamageValue / 50f), ev.Angle);

        if (GameState.LocalCharacter.Armor.ArmorPoints > 0)
        {
            SfxManager.Play2dAudioClip(SoundEffectType.PcLocalPlayerHitArmorRemaining);
        }
        else
        {
            SfxManager.Play2dAudioClip(SoundEffectType.PcLocalPlayerHitNoArmor);
        }
    }

    public static void OnPlayerKillEnemy(OnPlayerKillEnemyEvent ev)
    {
        InGameEventFeedbackType type = InGameEventFeedbackType.None;
        if (ev.WeaponCategory == UberstrikeItemClass.WeaponMelee)
        {
            type = InGameEventFeedbackType.Humiliation;
            LocalShotFeedbackHud.Instance.DisplayLocalShotFeedback(type);
            SfxManager.Play2dAudioClip(SoundEffectType.PcKilledBySplatbat);
        }
        else if (ev.BodyHitPart == BodyPart.Head)
        {
            type = InGameEventFeedbackType.HeadShot;
            LocalShotFeedbackHud.Instance.DisplayLocalShotFeedback(type);
            SfxManager.Play2dAudioClip(SoundEffectType.PcGotHeadshotKill);
        }
        else if (ev.BodyHitPart == BodyPart.Nuts)
        {
            type = InGameEventFeedbackType.NutShot;
            LocalShotFeedbackHud.Instance.DisplayLocalShotFeedback(type);
            SfxManager.Play2dAudioClip(SoundEffectType.PcGotNutshotKill);
        }
        else
        {
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage, 
                string.Format(LocalizedStrings.YouKilledN, ev.EmemyInfo.PlayerName));
        }

        HudUtil.Instance.AddInGameEvent(GameState.LocalCharacter.PlayerName, 
            ev.EmemyInfo.PlayerName, ev.WeaponCategory, type, GameState.LocalCharacter.TeamID, ev.EmemyInfo.TeamID);
    }

    public static void OnPlayerKilled(OnPlayerKilledEvent ev)
    {
        InGameEventFeedbackType type = InGameEventFeedbackType.None;
        if (ev.WeaponCategory == UberstrikeItemClass.WeaponMelee)
        {
            type = InGameEventFeedbackType.Humiliation;
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage, 
                string.Format(LocalizedStrings.SmackdownFromN, ev.ShooterInfo.PlayerName));

            // TODO: weird code in wrong place, later double check this code should be really here?!
            if (LevelCamera.Instance.IsZoomedIn)
                LevelCamera.Instance.DoZoomOut(60, 10);
        }
        else if (ev.BodyHitPart == BodyPart.Head)
        {
            type = InGameEventFeedbackType.HeadShot;
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage,
                string.Format(LocalizedStrings.HeadshotFromN, ev.ShooterInfo.PlayerName), 6);
        }
        else if (ev.BodyHitPart == BodyPart.Nuts)
        {
            type = InGameEventFeedbackType.NutShot;
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage,
                string.Format(LocalizedStrings.NutshotFromN, ev.ShooterInfo.PlayerName), 6);
        }
        else
        {
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage,
                string.Format(LocalizedStrings.KilledByN, ev.ShooterInfo.PlayerName), 6);
        }
        HudUtil.Instance.AddInGameEvent(ev.ShooterInfo.PlayerName,
            GameState.LocalCharacter.PlayerName, ev.WeaponCategory, type, ev.ShooterInfo.TeamID, GameState.LocalCharacter.TeamID);
    }

    public static void OnPlayerSuicide(OnPlayerSuicideEvent ev)
    {
        if (ev.PlayerInfo.ActorId == GameState.CurrentPlayerID)
        {
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage,
                LocalizedStrings.CongratulationsYouKilledYourself);
        }
        HudUtil.Instance.AddInGameEvent(ev.PlayerInfo.PlayerName, LocalizedStrings.NKilledThemself,
            UberstrikeItemClass.FunctionalGeneral, InGameEventFeedbackType.CustomMessage,
            ev.PlayerInfo.TeamID, TeamID.NONE);
    }

    public static void UpdatePlayerStateMsg(FpsGameMode gameMode, bool checkTimeout)
    {
        if (gameMode.IsWaitingForSpawn)
        {
            UpdateWaitingForSpawnMsg(gameMode, checkTimeout);
        }
        else
        {
            //TODO: ugly way to enable/disable QuickItemController, fix it with game states after this release
            if (gameMode.IsWaitingForPlayers)
            {
                PlayerStateMsgHud.Instance.DisplayWaitingForOtherPlayerMsg();
            }
            else
            {
                PlayerStateMsgHud.Instance.DisplayNone();
            }
        }
    }

    public static void UpdateWaitingForSpawnMsg(FpsGameMode gameMode, bool checkTimeout)
    {
        int spawnFrozenTime = Mathf.CeilToInt(gameMode.NextSpawnTime - Time.time);
        if (spawnFrozenTime > 0)
        {
            HudUtil.Instance.ShowRespawnFrozenTimeText(spawnFrozenTime);
        }
        else
        {
            int timeout = Mathf.CeilToInt(gameMode.NextSpawnTime - Time.time + (PlayerDataManager.AccessLevel == 0 ?
                                    DisconnectionTimeout : DisconnectionTimeoutAdmin));
            if (checkTimeout == false || timeout > 0)
            {
                if (!GameState.LocalPlayer.IsGamePaused)
                {
                    HudUtil.Instance.ShowClickToRespawnText(gameMode);
                }
                else
                {
                    HudUtil.Instance.ShowRespawnButton();
                }

                if (checkTimeout == true && timeout < 10)
                {
                    HudUtil.Instance.ShowTimeOutText(gameMode, timeout);
                }
            }
            else if (checkTimeout == true && !gameMode.IsGameClosed)
            {
                //Kicked out because of inactivity
                gameMode.IsGameClosed = true;
                if (GameState.LocalDecorator)
                    GameState.LocalDecorator.DisableRagdoll();
                GameStateController.Instance.UnloadLevelAndLoadPage(PageType.Home);
                PopupSystem.ShowMessage("Wake up!", "It looks like you were asleep." +
                        "The server disconnected you because you were idle for more than one minute while waiting to respawn.");
            }
        }
    }

    #region Private

    private const int DisconnectionTimeout = 120;
    private const int DisconnectionTimeoutAdmin = 1200;

    #endregion
}
