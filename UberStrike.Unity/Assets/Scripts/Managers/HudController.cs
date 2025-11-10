using Cmune.Util;
using UnityEngine;

public class HudController : MonoSingleton<HudController>
{
    public HudDrawFlags DrawFlags
    {
        get
        {
            return _finalDrawFlag;
        }
        set
        {
            _finalDrawFlag = value;
            UpdateHudVisibilityByFlag();
        }
    }

    public string DrawFlagString
    {
        get
        {
            return CmunePrint.Flag<HudDrawFlags>((ushort)DrawFlags);
        }
    }

    #region Private
    private void OnEnable()
    {
        UpdateHudVisibilityByFlag();
    }

    private void OnDisable()
    {
        //don't do anything when unity is shutting down
        if (!GameState.IsShuttingDown)
        {
            UpdateHudVisibilityByFlag();
        }
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Hud;

        if (IsDrawFlagEnabled(HudDrawFlags.InGameChat))
        {
            InGameChatHud.Instance.Draw();
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            XpPtsHud.Instance.PopupTemporarily();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.HealthArmor))
        {
            HpApHud.Instance.Draw();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.Ammo))
        {
            AmmoHud.Instance.Draw();
            TemporaryWeaponHud.Instance.Draw();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.XpPoints))
        {
            XpPtsHud.Instance.Draw();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.Weapons))
        {
            WeaponsHud.Instance.Draw();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.EventStream))
        {
            EventStreamHud.Instance.Draw();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.Score |
            HudDrawFlags.RoundTime | HudDrawFlags.RemainingKill))
        {
            MatchStatusHud.Instance.Draw();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.Reticle))
        {
            ReticleHud.Instance.Draw();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.InGameHelp))
        {
            InGameHelpHud.Instance.Draw();
        }

        // all the temporary messages
        EventFeedbackHud.Instance.Draw();
        LevelUpHud.Instance.Draw();
        PopupHud.Instance.Draw();
        GameModeObjectiveHud.Instance.Draw();
        TeamChangeWarningHud.Instance.Draw();
        DamageFeedbackHud.Instance.Draw();
        PlayerStateMsgHud.Instance.Draw();

        // screen info
        ScreenshotHud.Instance.Draw();
        FrameRateHud.Instance.Draw();
    }

    private void Update()
    {
        HpApHud.Instance.Update();
        XpPtsHud.Instance.Update();
        PickupNameHud.Instance.Update();
        ReticleHud.Instance.Update();
        LocalShotFeedbackHud.Instance.Update();
        DamageFeedbackHud.Instance.Update();
        InGameFeatHud.Instance.Update();
        WeaponsHud.Instance.Update();
        EventStreamHud.Instance.Update();
        GameModeObjectiveHud.Instance.Update();

        if (IsDrawFlagEnabled(HudDrawFlags.InGameHelp))
        {
            InGameHelpHud.Instance.Update();
        }
        if (IsDrawFlagEnabled(HudDrawFlags.InGameChat))
        {
            InGameChatHud.Instance.Update();
        }

        HudUtil.Instance.Update();
    }

    private void UpdateHudVisibilityByFlag()
    {
        HpApHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.HealthArmor);
        AmmoHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.Ammo);
        XpPtsHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.XpPoints);
        WeaponsHud.Instance.SetEnabled(enabled && IsDrawFlagEnabled(HudDrawFlags.Weapons));
        EventStreamHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.EventStream);
        MatchStatusHud.Instance.IsScoreEnabled = enabled && IsDrawFlagEnabled(HudDrawFlags.Score);
        MatchStatusHud.Instance.IsClockEnabled = enabled && IsDrawFlagEnabled(HudDrawFlags.RoundTime);
        MatchStatusHud.Instance.IsRemainingKillEnabled = enabled && IsDrawFlagEnabled(HudDrawFlags.RemainingKill);
        InGameHelpHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.InGameHelp);
        ReticleHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.Reticle);
        InGameChatHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.InGameChat);

        EventFeedbackHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.StateMsg);
        GameModeObjectiveHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.StateMsg);
        PickupNameHud.Instance.Enabled = enabled && IsDrawFlagEnabled(HudDrawFlags.StateMsg);
        PlayerStateMsgHud.Instance.TemporaryMsgEnabled = enabled && IsDrawFlagEnabled(HudDrawFlags.StateMsg);
        PlayerStateMsgHud.Instance.PermanentMsgEnabled = enabled && IsDrawFlagEnabled(HudDrawFlags.StateMsg);
    }

    private bool IsDrawFlagEnabled(HudDrawFlags drawFlag)
    {
        return (DrawFlags & drawFlag) != 0;
    }

    private HudDrawFlags _finalDrawFlag;
    #endregion
}