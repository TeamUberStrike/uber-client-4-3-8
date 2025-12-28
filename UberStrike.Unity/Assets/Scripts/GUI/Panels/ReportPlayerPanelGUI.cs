using System;
using System.Collections.Generic;
using System.Text;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client.Events;
using Cmune.Util;
using UnityEngine;

public class ReportPlayerPanelGUI : PanelGuiBase
{
    private void Awake()
    {
        CmuneEventHandler.AddListener<MemberListUpdatedEvent>(OnMemberListUpdatedEvent);
    }

    private void Start()
    {
        _isDropdownActive = false;
        _abuse = LocalizedStrings.SelectType;

        Array allAbusions = System.Enum.GetValues(typeof(MemberReportType));
        _reportTypeTexts = new string[allAbusions.Length];
        int i = 0;
        foreach (MemberReportType t in allAbusions)
        {
            _reportTypeTexts[i++] = Enum.GetName(typeof(MemberReportType), t);
        }

        _playerNameTexts = new string[0];
    }

    private void OnEnable()
    {
        GUI.FocusControl("ReportDetail");
    }

    private void OnGUI()
    {
        _rect = new Rect((Screen.width - 570) * 0.5f, (Screen.height - 345) * 0.5f, 570, 345);

        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.window_standard_grey38);
        DrawReportPanel();
        GUI.EndGroup();
    }

    public override void Show()
    {
        base.Show();

        _commUsersCount = 0;
        _commUsers = ChatManager.Instance.GetCommUsersToReport();
    }

    public override void Hide()
    {
        base.Hide();

        _commUsers = null;
        _selectedAbusion = -1;
        _abuse = LocalizedStrings.SelectType;
    }

    public static void ReportInboxPlayer(PrivateMessageView msg, string messageSender)
    {
        int reportedCmid = msg.FromCmid;
        string reason = msg.ContentText;

        if (CommConnectionManager.IsConnected)
        {
            PopupSystem.ShowMessage(LocalizedStrings.ReportPlayerCaps, string.Format(LocalizedStrings.ReportPlayerWarningMsg, messageSender),
                PopupSystem.AlertType.OKCancel,
                () =>
                {
                    CommConnectionManager.CommCenter.SendPlayerReport(new int[] { reportedCmid }, MemberReportType.OffensiveChat, reason);
                }, "Report", null, "Cancel",
                PopupSystem.ActionType.Negative);
        }
        else
        {
            PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ReportPlayerErrorMsg, PopupSystem.AlertType.OK, null);
        }
    }

    //private void DrawReportPanel()
    //{
    //    GUI.depth = (int)GuiDepth.Panel;

    //    GUI.skin = BlueStonez.Skin;
    //    GUI.Label(new Rect(0, 0, _rect.width, 56), LocalizedStrings.ReportPlayerCaps, BlueStonez.tab_strip);

    //    GUI.color = Color.red;
    //    GUI.Label(new Rect(16, _rect.height - 40, 300, 30), LocalizedStrings.ReportPlayerInfoMsg, BlueStonez.label_interparkbold_11pt_left_wrap);
    //    GUI.color = Color.white;

    //    Rect rect = new Rect(17, 55, _rect.width - 34, _rect.height - 100);
    //    GUI.BeginGroup(rect, string.Empty, BlueStonez.window_standard_grey38);
    //    {
    //        GUI.Label(new Rect(16, 20, 100, 18), LocalizedStrings.ReportType, BlueStonez.label_interparkbold_18pt_left);
    //        GUI.Label(new Rect(16, 50, 100, 18), LocalizedStrings.PlayerNames, BlueStonez.label_interparkbold_18pt_left);
    //        GUI.Label(new Rect(16, 80, 100, 18), LocalizedStrings.Details, BlueStonez.label_interparkbold_18pt_left);

    //        GUI.enabled = !_isDropdownActive;
    //        //DETAILS
    //        GUI.SetNextControlName("ReportDetail");
    //        _reason = GUI.TextArea(new Rect(16, 110, 290, 120), _reason);

    //        //NAMES
    //        GUI.Label(new Rect(125, 50, 180, 22), _selectedPlayers, BlueStonez.textField);
    //        if (string.IsNullOrEmpty(_selectedPlayers))
    //        {
    //            GUI.color = Color.gray;
    //            GUI.Label(new Rect(130, 52, 180, 22), "(" + LocalizedStrings.NoPlayerSelected + ")");
    //            GUI.color = Color.white;
    //        }

    //        GUI.enabled = true;

    //        int index = DoDropDownList(new Rect(125, 20, 183, 22), new Rect(135, 50, 194, 84), _reportTypeTexts, ref _abuse, false);
    //        if (index != -1)
    //        {
    //            _selectedAbusion = index;
    //            _abuse = _reportTypeTexts[index];
    //        }

    //        GUI.SetNextControlName("SearchUser");
    //        _searchPattern = GUI.TextField(new Rect(325, 20, 196, 22), _searchPattern);
    //        if (string.IsNullOrEmpty(_searchPattern) && GUI.GetNameOfFocusedControl() != "SearchUser")
    //        {
    //            GUI.color = Color.gray;
    //            GUI.Label(new Rect(333, 22, 196, 22), LocalizedStrings.SelectAPlayer);
    //            GUI.color = Color.white;
    //        }

    //        int i = 0;
    //        GUI.Label(new Rect(325, 50, 175, 178), GUIContent.none, BlueStonez.box_grey50);
    //        _scrollUsers = GUITools.BeginScrollView(new Rect(325, 50, 195, 178), _scrollUsers, new Rect(0, 0, 150, Mathf.Max(PlayerChatLog.ParticipantsCount * 20, 178)), false, true);
    //        {
    //            StringBuilder b = new StringBuilder();
    //            string pattern = _searchPattern.ToLowerInvariant();
    //            foreach (PlayerChatLog.ChatParticipant p in PlayerChatLog.AllParticipants)
    //            {
    //                bool isSelected = _reportedCmids.Contains(p.Cmid);
    //                if (isSelected) b.Append(p.Name).Append(", ");
    //                if (p.Name.ToLowerInvariant().Contains(pattern))
    //                {
    //                    bool toggle = GUI.Toggle(new Rect(2, 2 + i * 20, 171, 20), isSelected, p.Name, BlueStonez.dropdown_listItem);
    //                    if (toggle != isSelected)
    //                    {
    //                        _reportedCmids.Clear();
    //                        if (!isSelected)
    //                            _reportedCmids.Add(p.Cmid);
    //                    }
    //                    i++;
    //                }
    //            }

    //            _selectedPlayers = b.ToString();
    //        }
    //        GUITools.EndScrollView();

    //        if (PlayerChatLog.ParticipantsCount == 0)
    //        {
    //            GUI.Label(new Rect(325, 50, 175, 178), LocalizedStrings.NoPlayersToReport, BlueStonez.label_interparkmed_11pt);
    //        }
    //        else if (i == 0)
    //        {
    //            GUI.Label(new Rect(325, 50, 175, 178), LocalizedStrings.NoMatchFound, BlueStonez.label_interparkmed_11pt);
    //        }

    //    }
    //    GUI.EndGroup();

    //    if (GUI.Button(new Rect(_rect.width - 125, _rect.height - 40, 120, 32), LocalizedStrings.CancelCaps, BlueStonez.button))
    //    {
    //        GUIManager.Instance.ClosePanel(Panel.ReportPlayer);

    //        _reportedCmids.Clear();
    //        _selectedPlayers = string.Empty;
    //        _reason = string.Empty;
    //        _selectedAbusion = -1;
    //    }

    //    GUI.enabled = _selectedAbusion >= 0 && !string.IsNullOrEmpty(_selectedPlayers) && !string.IsNullOrEmpty(_reason);
    //    if (GUI.Button(new Rect(_rect.width - 125 - 125, _rect.height - 40, 120, 32), LocalizedStrings.SendCaps, BlueStonez.button_red))
    //    {
    //        if (CommConnectionManager.IsInitialized && CommConnectionManager.IsConnected)
    //        {
    //            PopupSystem.ShowMessage(LocalizedStrings.ReportPlayerCaps, string.Format(LocalizedStrings.ReportPlayerWarningMsg, _selectedPlayers), PopupSystem.AlertType.OKCancel, ConfirmAbuseReport, "Report", null, "Cancel", PopupSystem.ActionType.Negative);
    //        }
    //        else
    //        {
    //            PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ReportPlayerErrorMsg, PopupSystem.AlertType.OK, null);
    //        }
    //    }
    //}

    private void DrawReportPanel()
    {
        GUI.depth = (int)GuiDepth.Panel;

        GUI.skin = BlueStonez.Skin;
        GUI.Label(new Rect(0, 0, _rect.width, 56), LocalizedStrings.ReportPlayerCaps, BlueStonez.tab_strip);

        GUI.color = Color.red;
        GUI.Label(new Rect(16, _rect.height - 40, 300, 30), LocalizedStrings.ReportPlayerInfoMsg, BlueStonez.label_interparkbold_11pt_left_wrap);
        GUI.color = Color.white;

        Rect rect = new Rect(17, 55, _rect.width - 34, _rect.height - 100);
        GUI.BeginGroup(rect, string.Empty, BlueStonez.window_standard_grey38);
        {
            GUI.Label(new Rect(16, 20, 100, 18), LocalizedStrings.ReportType, BlueStonez.label_interparkbold_18pt_left);
            GUI.Label(new Rect(16, 50, 100, 18), LocalizedStrings.PlayerNames, BlueStonez.label_interparkbold_18pt_left);
            GUI.Label(new Rect(16, 80, 100, 18), LocalizedStrings.Details, BlueStonez.label_interparkbold_18pt_left);

            GUI.enabled = !_isDropdownActive;
            //DETAILS
            GUI.SetNextControlName("ReportDetail");
            _reason = GUI.TextArea(new Rect(16, 110, 290, 120), _reason);

            //NAMES
            GUI.Label(new Rect(125, 50, 180, 22), _selectedPlayers, BlueStonez.textField);
            if (string.IsNullOrEmpty(_selectedPlayers))
            {
                GUI.color = Color.gray;
                GUI.Label(new Rect(130, 52, 180, 22), "(" + LocalizedStrings.NoPlayerSelected + ")");
                GUI.color = Color.white;
            }

            GUI.enabled = true;

            int index = DoDropDownList(new Rect(125, 20, 183, 22), new Rect(135, 50, 194, 84), _reportTypeTexts, ref _abuse, false);
            if (index != -1)
            {
                _selectedAbusion = index;
                _abuse = _reportTypeTexts[index];
            }

            GUI.SetNextControlName("SearchUser");
            _searchPattern = GUI.TextField(new Rect(325, 20, 196, 22), _searchPattern);
            if (string.IsNullOrEmpty(_searchPattern) && GUI.GetNameOfFocusedControl() != "SearchUser")
            {
                GUI.color = Color.gray;
                GUI.Label(new Rect(333, 22, 196, 22), LocalizedStrings.SelectAPlayer);
                GUI.color = Color.white;
            }

            int i = 0;
            GUI.Label(new Rect(325, 50, 175, 178), GUIContent.none, BlueStonez.box_grey50);
            _scrollUsers = GUITools.BeginScrollView(new Rect(325, 50, 195, 178), _scrollUsers, new Rect(0, 0, 150, Mathf.Max(_commUsersCount * 20, 178)), false, true);
            if (_commUsers != null)
            {
                StringBuilder b = new StringBuilder();
                string pattern = _searchPattern.ToLowerInvariant();

                foreach (CommUser p in _commUsers)
                {
                    bool isSelected = _reportedCmids.Contains(p.Cmid);
                    if (isSelected) b.Append(p.Name).Append(", ");
                    if (p.Name.ToLowerInvariant().Contains(pattern))
                    {
                        bool toggle = GUI.Toggle(new Rect(2, 2 + i * 20, 171, 20), isSelected, p.Name, BlueStonez.dropdown_listItem);
                        if (toggle != isSelected)
                        {
                            _reportedCmids.Clear();
                            if (!isSelected)
                                _reportedCmids.Add(p.Cmid);
                        }
                        i++;
                    }
                }

                _commUsersCount = i;
                _selectedPlayers = b.ToString();
            }
            GUITools.EndScrollView();

            if (_commUsersCount == 0)
            {
                GUI.Label(new Rect(325, 50, 175, 178), LocalizedStrings.NoPlayersToReport, BlueStonez.label_interparkmed_11pt);
            }
            else if (i == 0)
            {
                GUI.Label(new Rect(325, 50, 175, 178), LocalizedStrings.NoMatchFound, BlueStonez.label_interparkmed_11pt);
            }

        }
        GUI.EndGroup();

        if (GUITools.Button(new Rect(_rect.width - 125, _rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
        {
            PanelManager.Instance.ClosePanel(PanelType.ReportPlayer);

            _commUsers = null;
            _reportedCmids.Clear();
            _selectedPlayers = string.Empty;
            _reason = string.Empty;
            _selectedAbusion = -1;
        }

        GUI.enabled = _selectedAbusion >= 0 && !string.IsNullOrEmpty(_selectedPlayers) && !string.IsNullOrEmpty(_reason);
        if (GUITools.Button(new Rect(_rect.width - 125 - 125, _rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.SendCaps), BlueStonez.button_red))
        {
            if (CommConnectionManager.IsConnected)
            {
                PopupSystem.ShowMessage(LocalizedStrings.ReportPlayerCaps, string.Format(LocalizedStrings.ReportPlayerWarningMsg, _selectedPlayers),
                    PopupSystem.AlertType.OKCancel,
                    ConfirmAbuseReport, "Report",
                    null, "Cancel", PopupSystem.ActionType.Negative);
            }
            else
            {
                PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ReportPlayerErrorMsg, PopupSystem.AlertType.OK, null);
            }
        }
    }

    private void ConfirmAbuseReport()
    {
        CommConnectionManager.CommCenter.SendPlayerReport(_reportedCmids.ToArray(), MemberReportType.OffensiveChat, _reason);

        PanelManager.Instance.ClosePanel(PanelType.ReportPlayer);
        PopupSystem.ShowMessage(LocalizedStrings.ReportPlayerCaps, LocalizedStrings.ReportPlayerSuccessMsg, PopupSystem.AlertType.OK, null);

        _reportedCmids.Clear();
        _selectedPlayers = string.Empty;
        _reason = string.Empty;
        _selectedAbusion = -1;
    }

    private void DrawGroupControl(Rect rect, string title, GUIStyle style)
    {
        GUI.BeginGroup(rect, string.Empty, BlueStonez.group_grey81);
        GUI.EndGroup();
        GUI.Label(new Rect(rect.x + 18, rect.y - 8, style.CalcSize(new GUIContent(title)).x + 10, 16), title, style);
    }

    private int DoDropDownList(Rect position, Rect size, string[] items, ref string defaultText, bool canEdit)
    {
        int index = -1;
        Rect rectLabel = new Rect(position.x, position.y, position.width - position.height, position.height);
        Rect rectButton = new Rect(position.x + position.width - position.height - 2, position.y - 1, position.height, position.height);

        bool temp = GUI.enabled;
        GUI.enabled = !(_isDropdownActive && _currentActiveItems != items);
        if (canEdit)
        {
            defaultText = GUI.TextField(new Rect(position.x, position.y, position.width - position.height, position.height - 1), defaultText, BlueStonez.textArea);
        }
        else
        {
            GUI.Label(rectLabel, defaultText, BlueStonez.label_dropdown);
        }

        if (GUI.Button(rectButton, GUIContent.none, BlueStonez.dropdown_button))
        {
            _isDropdownActive = !_isDropdownActive;
            _currentActiveItems = items;
        }

        if (_isDropdownActive && _currentActiveItems == items)
        {
            Rect rect = new Rect(position.x, position.y + position.height - 1, size.width - 16, size.height);
            GUI.Box(rect, string.Empty, BlueStonez.window_standard_grey38);
            _listScroll = GUITools.BeginScrollView(rect, _listScroll, new Rect(0, 0, rect.width - 20, items.Length * 20));
            for (int i = 0; i < items.Length; i++)
            {

                if (GUI.Button(new Rect(2, i * 20 + 2, rect.width - 4, 20), items[i], BlueStonez.dropdown_listItem))
                {
                    _isDropdownActive = false;
                    index = i;
                }
            }
            GUITools.EndScrollView();
        }
        GUI.enabled = temp;

        return index;
    }

    private void OnMemberListUpdatedEvent(MemberListUpdatedEvent ev)
    {
        _playerNameTexts = new string[ev.Players.Length];

        int i = 0;
        foreach (ActorInfo player in ev.Players)
        {
            //when the list (length/order) is changing we must ensure that the index of the selected item is updated
            _playerNameTexts[i++] = player.PlayerName;
        }
    }

    #region FIELDS

    private Rect _rect;
    private bool _isDropdownActive;
    private Vector2 _listScroll;

    private string[] _reportTypeTexts;
    private string[] _playerNameTexts;

    private int _selectedAbusion = -1;
    private string _reason = string.Empty;
    private string _abuse = string.Empty;

    private string _selectedPlayers = string.Empty;
    private string _searchPattern = string.Empty;
    private Vector2 _scrollUsers;
    private List<int> _reportedCmids = new List<int>();

    private string[] _currentActiveItems;

    private int _commUsersCount = 0;
    private List<CommUser> _commUsers = null;

    #endregion
}
