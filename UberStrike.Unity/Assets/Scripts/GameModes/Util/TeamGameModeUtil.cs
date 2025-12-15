using UberStrike.Realtime.Common;
using UnityEngine;

static class TeamGameModeUtil
{
    public static void OnChangeTeamSuccess(OnChangeTeamSuccessEvent ev)
    {
        switch (ev.CurrentTeamID)
        {
            case TeamID.BLUE:
                TeamChangeWarningHud.Instance.DisplayWarningMsg(LocalizedStrings.ChangingToRedTeam, ColorScheme.HudTeamRed);
                HudUtil.Instance.SetPlayerTeam(TeamID.RED);
                break;

            case TeamID.RED:
                TeamChangeWarningHud.Instance.DisplayWarningMsg(LocalizedStrings.ChangingToBlueTeam, ColorScheme.HudTeamBlue);
                HudUtil.Instance.SetPlayerTeam(TeamID.BLUE);
                break;
        }
    }

    public static void OnChangeTeamFail(OnChangeTeamFailEvent ev)
    {
        switch (ev.Reason)
        {
            case OnChangeTeamFailEvent.FailReason.CannotChangeToATeamWithEqual:
                TeamChangeWarningHud.Instance.DisplayWarningMsg(LocalizedStrings.YouCannotChangeToATeamWithEqual, Color.white);
                break;

            case OnChangeTeamFailEvent.FailReason.OnlyOneTeamChangePerLife:
                TeamChangeWarningHud.Instance.DisplayWarningMsg(LocalizedStrings.OnlyOneTeamChangePerLife, Color.white);
                break;
        }
    }

    public static void OnPlayerChangeTeam(TeamDeathMatchGameMode gameMode, int playerId,
        UberStrike.Realtime.Common.CharacterInfo playerInfo, TeamID targetTeamID)
    {
        string teamName = targetTeamID == TeamID.BLUE ? LocalizedStrings.Blue : LocalizedStrings.Red;
        EventStreamHud.Instance.AddEventText(playerInfo.PlayerName, playerInfo.TeamID, string.Format(LocalizedStrings.ChangingToTeamN, teamName));

        if (playerInfo.ActorId == GameState.CurrentPlayerID)
        {
            ReticleHud.Instance.FocusCharacter(TeamID.NONE);
            gameMode.ChangeAllPlayerOutline(GameState.LocalCharacter.TeamID);
        }
        else
        {
            gameMode.ChangePlayerOutlineById(playerId);
        }
    }

    public static void DetectTeamChange(TeamDeathMatchGameMode gameMode)
    {
        if (!(KeyInput.AltPressed && KeyInput.CtrlPressed) && gameMode.IsGameStarted
            && !InGameChatHud.Instance.CanInput && !ChatPageGUI.IsChatActive && !PanelManager.Instance.IsPanelOpen(PanelType.Options)
            && InputManager.Instance.RawValue(GameInputKey.ChangeTeam) > 0 && GUITools.SaveClickIn(1))
        {
            GUITools.Clicked();
            gameMode.ChangeTeam();
        }
    }
}