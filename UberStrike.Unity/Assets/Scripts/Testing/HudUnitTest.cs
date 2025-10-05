using UnityEngine;
using System.Collections;
using UberStrike.Realtime.Common;
using System.Collections.Generic;

public class HudUnitTest : MonoBehaviour
{
    private void Start()
    {
        HudStyleUtility.Instance.OnTeamChange(new OnSetPlayerTeamEvent());

        LocalizedStrings.KillsRemain = "Kills Remaining";
        LocalizedStrings.WaitingForOtherPlayers = "Waiting For Other Players";
        LocalizedStrings.SpectatorMode = "Spectator Mode";
        lastTeamId = TeamID.NONE;
        teamID = TeamID.BLUE;

        MatchStatusHud.Instance.RemainingSeconds = 31;
        MatchStatusHud.Instance.RemainingKills = 6;

        InGameHelpHud.Instance.EnableChangeTeamHelp = true;

        TemporaryWeaponHud.Instance.StartCounting(30);

        weaponHud = new MeshGUIList();
        weaponHud.Enabled = true;
        weaponHud.AddItem("Melee");
        weaponHud.AddItem("Machine Gun");
        weaponHud.AddItem("Sniper");

        _quickItemsHud = new Dictionary<LoadoutSlotType, QuickItemHud>();
        armorQuickItem = new QuickItemHud("ArmorQuickItem");
        healthQuickItem = new QuickItemHud("HealthQuickItem");
        springQuickItem = new QuickItemHud("SpringQuickItem");
        _quickItemsGroup = new Animatable2DGroup();
        _quickItemsHud.Add(LoadoutSlotType.QuickUseItem1, springQuickItem);
        _quickItemsHud.Add(LoadoutSlotType.QuickUseItem2, healthQuickItem);
        _quickItemsHud.Add(LoadoutSlotType.QuickUseItem3, armorQuickItem);
        foreach (QuickItemHud quickItemHud in _quickItemsHud.Values)
        {
            _quickItemsGroup.Group.Add(quickItemHud.Group);
        }

        springQuickItem.Amount = 3;
        ResetQuickItemsTransform();
    }

    private void Update()
    {
        HpApHud.Instance.Enabled = enableHpAp;
        AmmoHud.Instance.Enabled = enableAmmo;
        XpPtsHud.Instance.Enabled = enableXpPtsBar;
        EventStreamHud.Instance.Enabled = enableEventStream;
        MatchStatusHud.Instance.Enabled = enableTeamScoreAndClock;
        InGameHelpHud.Instance.Enabled = enableHelpInfo;
        PlayerStateMsgHud.Instance.PermanentMsgEnabled = enablePermanentStateMsg;
        TemporaryWeaponHud.Instance.Enabled = enableTemporaryWeapon;

        HpApHud.Instance.Update();
        XpPtsHud.Instance.Update();
        LocalShotFeedbackHud.Instance.Update();
        PickupNameHud.Instance.Update();
        InGameFeatHud.Instance.Update();
        EventStreamHud.Instance.Update();
        GameModeObjectiveHud.Instance.Update();

        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            weaponHud.AnimDownward();
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            weaponHud.AnimUpward();
        }

        if (teamID != lastTeamId)
        {
            HudUtil.Instance.SetPlayerTeam(teamID);
            lastTeamId = teamID;
        }

        HudUtil.Instance.Update();
    }

    private void OnGUI()
    {
        if (enableMultiKill)
        {
            if (GUI.Button(new Rect(0.0f, 0.0f, 100.0f, 30.0f), "Popup Double"))
            {
                PopupHud.Instance.PopupMultiKill(2);
            }
            if (GUI.Button(new Rect(0.0f, 40.0f, 100.0f, 30.0f), "Popup Triple"))
            {
                PopupHud.Instance.PopupMultiKill(3);
            }
            if (GUI.Button(new Rect(0.0f, 80.0f, 100.0f, 30.0f), "Popup Quad"))
            {
                PopupHud.Instance.PopupMultiKill(4);
            }
            if (GUI.Button(new Rect(0.0f, 120.0f, 100.0f, 30.0f), "Popup Mega"))
            {
                PopupHud.Instance.PopupMultiKill(5);
            }
            if (GUI.Button(new Rect(0.0f, 160.0f, 100.0f, 30.0f), "Popup Uber"))
            {
                PopupHud.Instance.PopupMultiKill(6);
            }
            if (GUI.Button(new Rect(0.0f, 200.0f, 100.0f, 30.0f), "Round Start"))
            {
                PopupHud.Instance.PopupRoundStart();
            }
            PopupHud.Instance.Draw();
        }

        if (enableHpAp)
        {
            if (GUI.Button(new Rect(Screen.width - 100.0f, 200.0f, 100.0f, 30.0f), "Increase HP"))
            {
                HpApHud.Instance.HP = (HpApHud.Instance.HP + 1);
            }
            if (GUI.Button(new Rect(Screen.width - 100.0f, 240.0f, 100.0f, 30.0f), "Decrease HP"))
            {
                HpApHud.Instance.HP = (HpApHud.Instance.HP - 1);
            }
            if (GUI.Button(new Rect(Screen.width - 150.0f, 280.0f, 150.0f, 30.0f), "Decrease Armor"))
            {
                HpApHud.Instance.AP = (HpApHud.Instance.AP - 1);
            }
            HpApHud.Instance.Draw();
        }

        if (enableXpPtsBar)
        {
            if (GUI.Button(new Rect(Screen.width - 100.0f, 320.0f, 100.0f, 30.0f), "Generate XP"))
            {
                XpPtsHud.Instance.GainXp(3);
            }
            if (GUI.Button(new Rect(Screen.width - 100.0f, 360.0f, 100.0f, 30.0f), "Generate Pts"))
            {
                XpPtsHud.Instance.GainPoints(3);
            }
            if (GUI.Button(new Rect(Screen.width - 100.0f, 120.0f, 100.0f, 30.0f), "Popup Xp bar"))
            {
                XpPtsHud.Instance.PopupTemporarily();
            }
            XpPtsHud.Instance.Draw();
        }

        if (enableAmmo)
        {
            if (GUI.Button(new Rect(Screen.width - 150.0f, 400.0f, 150.0f, 30.0f), "Increase Ammo"))
            {
                AmmoHud.Instance.Ammo += 1;
            }
            AmmoHud.Instance.Draw();
        }

        if (enableEventStream)
        {
            if (GUI.Button(new Rect(Screen.width - 100.0f, 440.0f, 100.0f, 30.0f), "Add Event"))
            {
                EventStreamHud.Instance.AddEventText("UberPlayerUberPlayerUberPlayer", TeamID.BLUE, "smackdown", "Bulletsponge", TeamID.RED);
            }
            EventStreamHud.Instance.Draw();
        }

        if (GUI.Button(new Rect(Screen.width - 200.0f, 480.0f, 200.0f, 30.0f), "Set Deathmatch Mode"))
        {
            GameModeObjectiveHud.Instance.DisplayGameMode(GameMode.DeathMatch);
        }
        GameModeObjectiveHud.Instance.Draw();

        if (enableTeamScoreAndClock)
        {
            if (GUI.Button(new Rect(0.0f, 520.0f, 150.0f, 30.0f), "Decrement Clock"))
            {
                --MatchStatusHud.Instance.RemainingSeconds;
            }
            if (GUI.Button(new Rect(0.0f, 560.0f, 200.0f, 30.0f), "Decrement Remaining Kill"))
            {
                //--MatchStatusHud.Instance.RemainingKills;
                MatchStatusHud.Instance.RemainingRoundsText = "Final Round RED";
            }
            MatchStatusHud.Instance.Draw();
        }

        if (enableHelpInfo)
        {
            InGameHelpHud.Instance.Draw();
        }

        if (GUI.Button(new Rect(0.0f, 480.0f, 150.0f, 30.0f), "Add Event Feedback"))
        {
            EventFeedbackHud.Instance.EnqueueFeedback(InGameEventFeedbackType.CustomMessage, "Headshot from Benny");
        }
        EventFeedbackHud.Instance.Draw();

        PickupNameHud.Instance.Draw();
        if (GUI.Button(new Rect(0.0f, 220.0f, 150.0f, 30.0f), "Anim Pickup Item"))
        {
            PickupNameHud.Instance.DisplayPickupName("Ammo Small", PickUpMessageType.Armor5);
        }

        if (GUI.Button(new Rect(0.0f, 360.0f, 150.0f, 30.0f), "Nut Shot"))
        {
            LocalShotFeedbackHud.Instance.DisplayLocalShotFeedback(InGameEventFeedbackType.NutShot);
        }
        if (GUI.Button(new Rect(0.0f, 400.0f, 150.0f, 30.0f), "Head Shot"))
        {
            LocalShotFeedbackHud.Instance.DisplayLocalShotFeedback(InGameEventFeedbackType.HeadShot);
        }
        if (GUI.Button(new Rect(0.0f, 440.0f, 150.0f, 30.0f), "Smackdown"))
        {
            LocalShotFeedbackHud.Instance.DisplayLocalShotFeedback(InGameEventFeedbackType.Humiliation);
        }

        if (GUI.Button(new Rect(0.0f, 260.0f, 150.0f, 30.0f), "Decrement Spring"))
        {
            springQuickItem.Amount -= 1;
        }
        weaponHud.Draw();
        _quickItemsGroup.Draw();

        PlayerStateMsgHud.Instance.Draw();
        if (enablePermanentStateMsg)
        {
            if (GUI.Button(new Rect(Screen.width - 200.0f, 0.0f, 200.0f, 30.0f), "WaitingForPlayer"))
            {
                PlayerStateMsgHud.Instance.DisplayWaitingForOtherPlayerMsg();
            }
            if (GUI.Button(new Rect(Screen.width - 100.0f, 40.0f, 100.0f, 30.0f), "SpectatorMsg"))
            {
                PlayerStateMsgHud.Instance.DisplaySpectatorModeMsg();
            }
        }

        if (GUI.Button(new Rect(300.0f, 0.0f, 150.0f, 30.0f), "Set temporary remaining"))
        {
            TemporaryWeaponHud.Instance.RemainingSeconds -= 1;
        }
        TemporaryWeaponHud.Instance.Draw();
    }


    private void ResetQuickItemsTransform()
    {
        const float adjustFactor = 0.75f;
        foreach (var quickItemHud in _quickItemsHud.Values)
        {
            quickItemHud.ResetHud();
        }

        var slot1 = _quickItemsHud[LoadoutSlotType.QuickUseItem1].Group;
        var slot2 = _quickItemsHud[LoadoutSlotType.QuickUseItem2].Group;
        var slot3 = _quickItemsHud[LoadoutSlotType.QuickUseItem3].Group;

        slot3.Position = Vector2.zero;
        slot2.Position = slot3.Position - new Vector2(0.0f, slot3.Rect.height * adjustFactor);
        slot1.Position = slot2.Position - new Vector2(0.0f, slot2.Rect.height * adjustFactor);

        _quickItemsGroup.Position = new Vector2(Screen.width - 50.0f, Screen.height - _quickItemsGroup.Rect.height / 2);
    }

    public GUISkin skin;
    public TeamID teamID;
    public bool enableMultiKill;
    public bool enableHpAp;
    public bool enableAmmo;
    public bool enableXpPtsBar;
    public bool enableEventStream;
    public bool enableTeamScoreAndClock;
    public bool enableHelpInfo;
    public bool enablePermanentStateMsg;
    public bool enableTemporaryWeapon;

    private TeamID lastTeamId;
    private MeshGUIList weaponHud;

    QuickItemHud armorQuickItem = new QuickItemHud("ArmorQuickItem");
    QuickItemHud healthQuickItem = new QuickItemHud("HealthQuickItem");
    QuickItemHud springQuickItem = new QuickItemHud("SpringQuickItem");
    private Dictionary<LoadoutSlotType, QuickItemHud> _quickItemsHud;
    private Animatable2DGroup _quickItemsGroup;
}
