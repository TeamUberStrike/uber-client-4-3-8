using System;
using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Types;

public class ScreenResolutionEvent { }

public class OnSetPlayerTeamEvent
{
    public TeamID TeamId { get; set; }
}

class HudUtil : Singleton<HudUtil>
{
    public void Update()
    {
        if (IsScreenResolutionChanged())
        {
            CmuneEventHandler.Route(new ScreenResolutionEvent());
        }
    }

    public void ClearAllHud()
    {
        CleanAllTemporaryHud();
        ScreenshotHud.Instance.Enable = false;
        FrameRateHud.Instance.Enable = false;
    }

    public void ClearAllFeedbackHud()
    {
        EventStreamHud.Instance.ClearAllEvents();
        EventFeedbackHud.Instance.ClearAll();
        InGameFeatHud.Instance.AnimationScheduler.ClearAll();
        DamageFeedbackHud.Instance.ClearAll();
        PlayerStateMsgHud.Instance.DisplayNone();
    }

    public void SetPlayerTeam(TeamID teamId)
    {
        CmuneEventHandler.Route(new OnSetPlayerTeamEvent() { TeamId = teamId });
    }

    public void ShowContinueButton()
    {
        PlayerStateMsgHud.Instance.ButtonEnabled = true;
        PlayerStateMsgHud.Instance.ButtonCaption = LocalizedStrings.Continue;
        PlayerStateMsgHud.Instance.OnButtonClicked = OnContinueButtonClicked;
    }

    public void ShowRespawnButton()
    {
        PlayerStateMsgHud.Instance.ButtonEnabled = true;
        PlayerStateMsgHud.Instance.TemporaryMsgEnabled = false;
        PlayerStateMsgHud.Instance.ButtonCaption = LocalizedStrings.Respawn;
        PlayerStateMsgHud.Instance.OnButtonClicked = OnRespawnButtonClicked;
    }

    public void ShowClickToRespawnText(FpsGameMode fpsGameMode)
    {
        PlayerStateMsgHud.Instance.DisplayClickToRespawnMsg();
        if (Input.GetMouseButtonDown(0))
        {
            fpsGameMode.RespawnPlayer();
        }
    }

    public void ShowRespawnFrozenTimeText(int spawnFrozenTime)
    {
        PlayerStateMsgHud.Instance.DisplayRespawnTimeMsg(spawnFrozenTime);
    }

    public void ShowTimeOutText(FpsGameMode fpsGameMode, int timeout)
    {
        PlayerStateMsgHud.Instance.DisplayDisconnectionTimeoutMsg(timeout);
    }

    public void SetTeamScore(int blueScore, int redScore)
    {
        MatchStatusHud.Instance.BlueTeamScore = blueScore;
        MatchStatusHud.Instance.RedTeamScore = redScore;
        TabScreenPanelGUI.Instance.SetTeamSplats(blueScore, redScore);
    }

    public void AddInGameEvent(string subjective, string objective, UberstrikeItemClass weaponClass,
        InGameEventFeedbackType eventType, TeamID sourceTeam, TeamID destinationTeam)
    {
        EventStreamHud.Instance.AddEventText(subjective, sourceTeam, GetEventTypeMessage(eventType), objective, destinationTeam);
    }

    public void AddInGameEvent(string sourcePlayer, string message)
    {
        AddInGameEvent(sourcePlayer, message, UberstrikeItemClass.FunctionalGeneral,
            InGameEventFeedbackType.CustomMessage, TeamID.NONE, TeamID.NONE);
    }

    #region Private methods
    private HudUtil() 
    {
    }

    private void OnRespawnButtonClicked()
    {
        GamePageManager.Instance.UnloadCurrentPage();
        GameState.LocalPlayer.UnPausePlayer();
        GameState.CurrentGame.RespawnPlayer();
    }

    private void OnContinueButtonClicked()
    {
        GamePageManager.Instance.UnloadCurrentPage();
        GameState.LocalPlayer.UnPausePlayer();
    }

    private void CleanAllTemporaryHud()
    {
        InGameChatHud.Instance.ClearAll();
        ClearAllFeedbackHud();
    }

    private string GetEventTypeMessage(InGameEventFeedbackType eventType)
    {
        switch (eventType)
        {
            case InGameEventFeedbackType.HeadShot: return "headshot";
            case InGameEventFeedbackType.Humiliation: return "smacked";
            case InGameEventFeedbackType.NutShot: return "nutshot";
            case InGameEventFeedbackType.None: return "killed";
            default: return string.Empty;
        }
    }

    private bool IsScreenResolutionChanged()
    {
        if (Screen.width != _lastScreenWidth ||
             Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            return true;
        }
        return false;
    }

    #endregion

    #region Private fields

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    #endregion
}
