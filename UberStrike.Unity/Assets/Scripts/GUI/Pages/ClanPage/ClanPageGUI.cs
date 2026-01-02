using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UnityEngine;

public class ClanPageGUI : MonoBehaviour
{
    public class ClanCreationEvent { }

    private void Awake()
    {
        CmuneEventHandler.AddListener<ClanCreationEvent>(OnClanCreated);
    }

    private void OnClanCreated(ClanCreationEvent ev)
    {
        createAClan = false;
        _newClanMotto = string.Empty;
        _newClanName = string.Empty;
        _newClanTag = string.Empty;
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;
        GUI.skin = BlueStonez.Skin;

        Rect rect = new Rect(0, GlobalUIRibbon.Instance.GetHeight(), GUITools.ScreenWidth, GUITools.ScreenHeight - GlobalUIRibbon.Instance.GetHeight());
        GUI.BeginGroup(rect, BlueStonez.box_grey31);
        {
            GUI.enabled = PlayerDataManager.IsPlayerLoggedIn && IsNoPopupOpen() && !ClanDataManager.Instance.IsProcessingWebservice;

            if (PlayerDataManager.IsPlayerInClan)
            {
                float headerHeight = 73;
                float footerHeight = 40;
                float mainHeight = rect.height - headerHeight - footerHeight;

                DrawClanRosterHeader(new Rect(0, 0, rect.width, headerHeight));
                DrawMembersView(new Rect(0, headerHeight, rect.width, mainHeight));
                DrawClanRosterFooter(new Rect(0, headerHeight + mainHeight, rect.width, footerHeight));
            }
            else
            {
                GUI.Box(rect, GUIContent.none, BlueStonez.box_grey38);

                if (createAClan)
                {
                    DrawCreateClanMessage(rect);
                }
                else
                {
                    DrawNoClanMessage(rect);
                }
            }
            GuiManager.DrawTooltip();

            GUI.enabled = true;
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Drawing header bar with clan info
    /// </summary>
    /// <param name="rect"></param>
    private void DrawClanRosterHeader(Rect rect)
    {
        int leftOffset = (int)rect.width;
        GUI.BeginGroup(rect, BlueStonez.box_grey31);
        {
            GUI.Label(new Rect(10, 5, rect.width - 20, 18), string.Format("Your Clan: {0}", PlayerDataManager.ClanName), BlueStonez.label_interparkbold_16pt_left);

            //Refresh button
            float timeRemaining = Mathf.Max(ClanDataManager.Instance.NextClanRefresh - Time.time, 0);
            GUITools.PushGUIState();
            GUI.enabled &= timeRemaining == 0;
            if (GUITools.Button(new Rect(rect.width - 130, 5, 120, 19), new GUIContent(string.Format(LocalizedStrings.Refresh + " {0}", (timeRemaining > 0) ? "(" + timeRemaining.ToString("N0") + ")" : string.Empty)), BlueStonez.buttondark_medium))
            {
                ClanDataManager.Instance.RefreshClanData();
            }
            GUITools.PopGUIState();

            //members online
            GUI.Label(new Rect(rect.width - 340, 5, 200, 18), string.Format(LocalizedStrings.NMembersNOnline, PlayerDataManager.Instance.ClanMembersCount, PlayerDataManager.ClanMembersLimit, _onlineMemberCount), BlueStonez.label_interparkmed_11pt_right); //label_interparkmed_18pt_right

            //clan info
            GUI.BeginGroup(new Rect(0, 25, rect.width, 50), BlueStonez.box_grey50);
            {
                GUI.Label(new Rect(10, 7, rect.width / 2, 16), string.Format("Tag: {0}", PlayerDataManager.ClanTag), BlueStonez.label_interparkmed_11pt_left);
                GUI.Label(new Rect(10, 28, rect.width / 2, 16), string.Format(LocalizedStrings.MottoN, PlayerDataManager.ClanMotto), BlueStonez.label_interparkmed_11pt_left);
                GUI.Label(new Rect(rect.width / 2, 7, rect.width / 2, 16), string.Format(LocalizedStrings.CreatedN, PlayerDataManager.ClanFoundingDate.ToShortDateString()), BlueStonez.label_interparkmed_11pt_left);
                GUI.Label(new Rect(rect.width / 2, 28, rect.width / 2, 16), string.Format(LocalizedStrings.LeaderN, PlayerDataManager.ClanOwnerName), BlueStonez.label_interparkmed_11pt_left);
            }
            GUI.EndGroup();
            //GUI.Label(new Rect(10, 80, rect.width - 20, 18), string.Format(LocalizedStrings.NClanRoster, PlayerDataManager.ClanTag), BlueStonez.label_interparkmed_11pt_left);

            //Invite players
            if (PlayerDataManager.Instance.RankInClan != GroupPosition.Member)
            {
                leftOffset = (int)(rect.width - 10 - 120);
                if (GUITools.Button(new Rect(leftOffset, 40, 120, 20), new GUIContent(LocalizedStrings.InvitePlayer), BlueStonez.buttondark_medium))
                {
                    PanelManager.Instance.OpenPanel(PanelType.ClanRequest);
                }
            }
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Drawing list of Clan members
    /// </summary>
    /// <param name="rect"></param>
    private void DrawMembersView(Rect rect)
    {
        GUI.BeginGroup(rect, BlueStonez.box_grey38);
        {
            UpdateColumnWidth();

            // Clan member table header
            int x = 0;
            GUI.Box(new Rect(x, 0, _indicatorWidth, 25), string.Empty, BlueStonez.box_grey50);
            x = _indicatorWidth - 1;
            GUI.Box(new Rect(x, 0, _nameWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + 5, 5, _nameWidth, 25), LocalizedStrings.Player, BlueStonez.label_interparkmed_11pt_left);
            x = _indicatorWidth + _nameWidth - 2;
            GUI.Box(new Rect(x, 0, _positionWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + 5, 5, _positionWidth, 25), LocalizedStrings.Position, BlueStonez.label_interparkmed_11pt_left);
            x = _indicatorWidth + _nameWidth + _positionWidth - 3;
            GUI.Box(new Rect(x, 0, _joinDateWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + 5, 5, _joinDateWidth, 25), LocalizedStrings.JoinDate, BlueStonez.label_interparkmed_11pt_left);
            x = _indicatorWidth + _nameWidth + _positionWidth + _joinDateWidth - 4;
            GUI.Box(new Rect(x, 0, _statusWidth, 25), string.Empty, BlueStonez.box_grey50);

            // draw actual clan members
            int i = 0;
            int height = PlayerDataManager.Instance.ClanMembersCount * 50;
            _clanMembersScrollView = GUITools.BeginScrollView(new Rect(0, 25, rect.width, rect.height - 25), _clanMembersScrollView, new Rect(0, 0, rect.width - 20, height));
            {
                _onlineMemberCount = 0;
                foreach (var m in PlayerDataManager.Instance.ClanMembers)
                {
                    DrawClanMembers(new Rect(0, 50 * i++, rect.width - 20, 50), m);
                }
            }
            GUITools.EndScrollView();
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Draw actual clan members
    /// </summary>
    /// <param name="rect"></param>
    private void DrawClanMembers(Rect rect, ClanMemberView member)
    {
        GUIStyle background = ((rect.Contains(Event.current.mousePosition)) ? BlueStonez.box_grey50 : BlueStonez.box_grey38);

        GUI.BeginGroup(rect, background);
        {
            int x;

            CommUser user;
            if (ChatManager.Instance.TryGetClanUsers(member.Cmid, out user))
                GUI.DrawTexture(new Rect(5, 12, 14, 20), ChatManager.GetPresenceIcon(user.PresenceIndex));

            // name
            x = _indicatorWidth + 3; //label_interparkmed_11pt_left
            GUI.Label(new Rect(x, 12, _nameWidth, 25), member.Name, BlueStonez.label_interparkbold_13pt_left);

            // Position
            x = _indicatorWidth + _nameWidth + 3;
            GUI.Label(new Rect(x, 20, _positionWidth, 25), ConvertClanPosition(member.Position), BlueStonez.label_interparkmed_11pt_left);

            // Join date
            x = _indicatorWidth + _nameWidth + _positionWidth + 3;
            GUI.Label(new Rect(x, 20, _joinDateWidth, 25), member.JoiningDate.ToString("d"), BlueStonez.label_interparkmed_11pt_left);

            // buttons
            float leftOffset = rect.width - 20;

            //Send Message + Private Chat
            if (member.Cmid != PlayerDataManager.Cmid)
            {
                if (user != null && user.IsOnline)
                {
                    _onlineMemberCount++;
                    if (GUITools.Button(new Rect(leftOffset - 120, 15, 100, 20), new GUIContent(LocalizedStrings.PrivateChat), BlueStonez.buttondark_medium))
                    {
                        ChatManager.Instance.CreatePrivateChat(member.Cmid);
                        MenuPageManager.Instance.LoadPage(PageType.Chat);
                        ChatPageGUI.SelectedTab = TabArea.Private;
                    }
                }
                else
                {
                    // last login
                    int days = DateTime.Now.Subtract(member.Lastlogin).Days;
                    string lastOnlineCaption = string.Format(LocalizedStrings.LastOnlineN, (days > 1) ? days.ToString() + " " + LocalizedStrings.DaysAgo : (days == 0) ? LocalizedStrings.Today : LocalizedStrings.Yesterday);
                    GUI.Label(new Rect(leftOffset - 120, 20, 100, 25), lastOnlineCaption, BlueStonez.label_interparkmed_11pt_left);
                }

                if (GUITools.Button(new Rect(leftOffset - 230, 15, 100, 20), new GUIContent(LocalizedStrings.SendMessage), BlueStonez.buttondark_medium))
                {
                    SendMessagePanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.SendMessage) as SendMessagePanelGUI;
                    if (panel) panel.SelectReceiver(member.Cmid, member.Name);
                }
            }

            //Remove
            if (HasHigherPermissionThan(member.Position))
            {
                if (GUITools.Button(new Rect(leftOffset - 10, 14, 20, 20), new GUIContent("x"), BlueStonez.buttondark_medium))
                {
                    int removeFromClanCmid = member.Cmid;
                    string msg = string.Format(LocalizedStrings.RemoveNFromClanN, member.Name, PlayerDataManager.ClanName) + "\n\n" + LocalizedStrings.RemoveMemberWarningMsg;
                    PopupSystem.ShowMessage(LocalizedStrings.RemoveMember, msg, PopupSystem.AlertType.OKCancel,
                         () => ClanDataManager.Instance.RemoveMemberFromClan(removeFromClanCmid), "OK",
                         null, "Cancel", PopupSystem.ActionType.Negative);
                }
                leftOffset = leftOffset - 160;
            }

            x = _indicatorWidth + _nameWidth;
            // TransferLeadership
            if (PlayerDataManager.Instance.RankInClan == GroupPosition.Leader && HasHigherPermissionThan(member.Position))
            {
                if (GUITools.Button(new Rect(x - 140, 4, 130, 20), new GUIContent(LocalizedStrings.TransferLeadership), BlueStonez.buttondark_medium))
                {
                    int newLeader = member.Cmid;
                    string msg = string.Format(LocalizedStrings.TransferClanLeaderhsipToN, member.Name) + "\n\n" + LocalizedStrings.TransferClanWarningMsg;
                    PopupSystem.ShowMessage(LocalizedStrings.TransferLeadership, msg, PopupSystem.AlertType.OKCancel,
                         () => ClanDataManager.Instance.TransferOwnershipTo(newLeader), "TRANSFER",
                         null, "Cancel", PopupSystem.ActionType.Negative);
                }
                leftOffset = leftOffset - 160;
            }
            // Promote to Officer / Demote to Member
            if (PlayerDataManager.Instance.RankInClan == GroupPosition.Leader && HasHigherPermissionThan(member.Position))
            {
                if (member.Position == GroupPosition.Member && GUITools.Button(new Rect(x - 140, 28, 130, 20), new GUIContent(LocalizedStrings.PromoteMember), BlueStonez.buttondark_medium))
                {
                    int memberCmid = member.Cmid;
                    PopupSystem.ShowMessage(LocalizedStrings.PromoteMember, string.Format(LocalizedStrings.ThisWillChangeNPositionToN, member.Name, LocalizedStrings.Officer), PopupSystem.AlertType.OKCancel,
                         () => ClanDataManager.Instance.UpdateMemberTo(memberCmid, GroupPosition.Officer), "PROMOTE",
                         null, "Cancel", PopupSystem.ActionType.Positive);
                }
                else if (member.Position == GroupPosition.Officer && GUITools.Button(new Rect(x - 140, 28, 130, 20), new GUIContent(LocalizedStrings.DemoteMember), BlueStonez.buttondark_medium))
                {
                    int memberCmid = member.Cmid;
                    PopupSystem.ShowMessage(LocalizedStrings.DemoteMember, string.Format(LocalizedStrings.ThisWillChangeNPositionToN, member.Name, LocalizedStrings.Member), PopupSystem.AlertType.OKCancel,
                         () => ClanDataManager.Instance.UpdateMemberTo(memberCmid, GroupPosition.Member), "DEMOTE",
                         null, "Cancel", PopupSystem.ActionType.Negative);
                }
                leftOffset = leftOffset - 160;
            }

            GUI.Label(new Rect(0, rect.height - 1, rect.width, 1), string.Empty, BlueStonez.horizontal_line_grey95);
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Bottom part of Clan Roster
    /// </summary>
    /// <param name="rect"></param>
    private void DrawClanRosterFooter(Rect rect)
    {
        GUI.BeginGroup(rect, BlueStonez.box_grey31);
        {
            if (PlayerDataManager.Instance.RankInClan == GroupPosition.Leader)
            {
                if (GUITools.Button(new Rect(rect.width - 110, 10, 100, 20), new GUIContent(LocalizedStrings.DisbandClan), BlueStonez.buttondark_medium))
                {
                    string msg = string.Format(LocalizedStrings.DisbandClanN, PlayerDataManager.ClanName) + "\n\n" + LocalizedStrings.DisbandClanWarningMsg;
                    PopupSystem.ShowMessage(LocalizedStrings.DisbandClan, msg, PopupSystem.AlertType.OKCancel,
                        () => ClanDataManager.Instance.DisbanClan(), "DISBAND",
                        null, "Cancel", PopupSystem.ActionType.Negative);
                }
            }
            else
            {
                if (GUITools.Button(new Rect(rect.width - 110, 10, 100, 20), new GUIContent(LocalizedStrings.LeaveClan), BlueStonez.buttondark_medium))
                {
                    string msg = string.Format(LocalizedStrings.LeaveClanN, PlayerDataManager.ClanName) + "\n\n" + LocalizedStrings.LeaveClanWarningMsg;
                    PopupSystem.ShowMessage(LocalizedStrings.LeaveClan, msg, PopupSystem.AlertType.OKCancel,
                        () => ClanDataManager.Instance.LeaveClan(), LocalizedStrings.LeaveCaps,
                        null, "Cancel", PopupSystem.ActionType.Negative);
                }
            }
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gp"></param>
    /// <returns></returns>
    private bool HasHigherPermissionThan(GroupPosition gp)
    {
        switch (PlayerDataManager.Instance.RankInClan)
        {
            case GroupPosition.Leader: return gp != GroupPosition.Leader;
            case GroupPosition.Officer: return gp == GroupPosition.Member;
            default: return false;
        }
    }

    /// <summary>
    /// Convert player position in clan from database eanum to user frendly names
    /// </summary>
    /// <param name="gp">Database eanum</param>
    /// <returns>User frendly name</returns>
    private string ConvertClanPosition(GroupPosition gp)
    {
        string result = string.Empty;
        switch (gp)
        {
            case GroupPosition.Officer:
                result = LocalizedStrings.Officer;
                break;
            case GroupPosition.Member:
                result = LocalizedStrings.Member;
                break;
            case GroupPosition.Leader:
                result = LocalizedStrings.Leader;
                break;
            default:
                result = LocalizedStrings.Unknown;
                break;
        }
        return result;
    }

    /// <summary>
    /// Only executed when screen size changes or enter/exit fullscreen
    /// </summary>
    private void UpdateColumnWidth()
    {
        int maxWidth = GUITools.ScreenWidth;
        int spaceLeft = maxWidth - _indicatorWidth - _positionWidth - _joinDateWidth;

        _nameWidth = Mathf.Clamp(Mathf.RoundToInt(spaceLeft * 0.5f), 200, 300);
        _statusWidth = spaceLeft - _nameWidth + 4;
    }

    /// <summary>
    /// Message when player is not in clan
    /// </summary>
    /// <param name="rect"></param>
    private void DrawNoClanMessage(Rect rect)
    {
        Rect comRect = new Rect((rect.width - 480) / 2, (rect.height - 240) / 2, 480, 240);
        GUI.BeginGroup(comRect, BlueStonez.window_standard_grey38);
        {
            GUI.Label(new Rect(0, 0, comRect.width, 56), LocalizedStrings.ClansCaps, BlueStonez.tab_strip);
            GUI.Box(new Rect((comRect.width / 2) - 82, 60, 48, 48), new GUIContent(_level4Icon), BlueStonez.item_slot_large);
            if (ClanDataManager.Instance.HaveLevel)
            {
                GUI.Box(new Rect((comRect.width / 2) - 82, 60, 48, 48), new GUIContent(UberstrikeIcons.CheckMark));
            }
            GUI.Box(new Rect((comRect.width / 2) - 24, 60, 48, 48), new GUIContent(_licenseIcon), BlueStonez.item_slot_large);
            if (ClanDataManager.Instance.HaveLicense)
            {
                GUI.Box(new Rect((comRect.width / 2) - 24, 60, 48, 48), new GUIContent(UberstrikeIcons.CheckMark));
            }
            GUI.Box(new Rect((comRect.width / 2) + 34, 60, 48, 48), new GUIContent(_friendsIcon), BlueStonez.item_slot_large);
            if (ClanDataManager.Instance.HaveFriends)
            {
                GUI.Box(new Rect((comRect.width / 2) + 34, 60, 48, 48), new GUIContent(UberstrikeIcons.CheckMark));
            }

            if (!(ClanDataManager.Instance.HaveLevel && ClanDataManager.Instance.HaveLicense && ClanDataManager.Instance.HaveFriends))
            {
                bool tmp = GUI.enabled;
                GUI.Label(new Rect(comRect.width / 2 - 90, 110, 210, 14), LocalizedStrings.ToCreateAClanYouStillNeedTo, BlueStonez.label_interparkbold_11pt_left);
                GUI.enabled = tmp && !ClanDataManager.Instance.HaveLevel;
                GUI.Label(new Rect(comRect.width / 2 - 90, 110 + 14, 200, 14), LocalizedStrings.ReachLevelFour + (ClanDataManager.Instance.HaveLevel ? string.Format(" ({0})", LocalizedStrings.Done) : string.Empty), BlueStonez.label_interparkbold_11pt_left);
                GUI.enabled = tmp && !ClanDataManager.Instance.HaveFriends;
                GUI.Label(new Rect(comRect.width / 2 - 90, 110 + 28, 200, 14), LocalizedStrings.HaveAtLeastOneFriend + (ClanDataManager.Instance.HaveFriends ? string.Format(" ({0})", LocalizedStrings.Done) : string.Empty), BlueStonez.label_interparkbold_11pt_left);
                GUI.enabled = tmp && !ClanDataManager.Instance.HaveLicense;
                GUI.Label(new Rect(comRect.width / 2 - 90, 110 + 42, 240, 14), LocalizedStrings.BuyAClanLicense + (ClanDataManager.Instance.HaveLicense ? string.Format(" ({0})", LocalizedStrings.Done) : string.Empty), BlueStonez.label_interparkbold_11pt_left);
                GUI.enabled = tmp;

                if (!ClanDataManager.Instance.HaveLicense && GUITools.Button(new Rect((comRect.width - 200) / 2, 110 + 60, 200, 22), new GUIContent(LocalizedStrings.BuyAClanLicense), BlueStonez.buttondark_medium))
                {
                    IUnityItem item = ItemManager.Instance.GetItemInShop(1234);
                    if (item != null && item.ItemView != null)
                    {
                        BuyPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.BuyItem) as BuyPanelGUI;
                        if (panel)
                        {
                            panel.SetItem(item, BuyingLocationType.Shop, BuyingRecommendationType.None);
                        }
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(0, 140, comRect.width, 14), LocalizedStrings.CreateAClanAndInviteYourFriends, BlueStonez.label_interparkbold_11pt);
            }

            GUITools.PushGUIState();
            GUI.enabled &= ClanDataManager.Instance.HaveLevel && ClanDataManager.Instance.HaveLicense && ClanDataManager.Instance.HaveFriends;
            if (GUITools.Button(new Rect((comRect.width - 200) / 2, 200, 200, 30), new GUIContent(LocalizedStrings.CreateAClanCaps), BlueStonez.button_green))
            {
                createAClan = true;
            }
            GUITools.PopGUIState();
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Create Clan dialog
    /// </summary>
    /// <param name="rect"></param>
    private void DrawCreateClanMessage(Rect rect)
    {
        Rect comRect = new Rect((rect.width - 480) / 2, (rect.height - 360) / 2, 480, 360);
        GUI.BeginGroup(comRect, BlueStonez.window_standard_grey38);
        {
            int labelCol = 35;
            int textCol = 120;
            int textWidth = 320;

            int nameRow = 130;
            int tagRow = 190;
            int mottoRow = 250;

            GUI.Label(new Rect(0, 0, comRect.width, 56), LocalizedStrings.CreateAClan, BlueStonez.tab_strip);
            GUI.Label(new Rect(0, 60, comRect.width, 20), LocalizedStrings.HereYouCanCreateYourOwnClan, BlueStonez.label_interparkbold_18pt);
            GUI.Label(new Rect(0, 80, comRect.width, 40), LocalizedStrings.YouCantChangeYourClanInfoOnceCreated, BlueStonez.label_interparkmed_11pt);

            GUI.Label(new Rect(labelCol, nameRow, 100, 20), LocalizedStrings.Name, BlueStonez.label_interparkbold_18pt_left);
            GUI.Label(new Rect(labelCol, tagRow, 100, 20), LocalizedStrings.Tag, BlueStonez.label_interparkbold_18pt_left);
            GUI.Label(new Rect(labelCol, mottoRow, 100, 20), LocalizedStrings.Motto, BlueStonez.label_interparkbold_18pt_left);

            _newClanName = GUI.TextField(new Rect(textCol, nameRow, textWidth, 24), _newClanName, BlueStonez.textField);
            _newClanTag = GUI.TextField(new Rect(textCol, tagRow, textWidth, 24), _newClanTag, BlueStonez.textField);
            _newClanMotto = GUI.TextField(new Rect(textCol, mottoRow, textWidth, 24), _newClanMotto, BlueStonez.textField);

            GUI.Label(new Rect(textCol, nameRow + 25, 300, 20), LocalizedStrings.ThisIsTheOfficialNameOfYourClan, BlueStonez.label_interparkmed_10pt_left);
            GUI.Label(new Rect(textCol, tagRow + 25, 300, 20), LocalizedStrings.ThisTagGetsDisplayedNextToYourName, BlueStonez.label_interparkmed_10pt_left);
            GUI.Label(new Rect(textCol, mottoRow + 25, 300, 20), LocalizedStrings.ThisIsYourOfficialClanMotto, BlueStonez.label_interparkmed_10pt_left);

            if (_newClanName.Length > CommonConfig.GroupNameMaxLength)
            {
                _newClanName = _newClanName.Remove(CommonConfig.GroupNameMaxLength);
            }
            if (_newClanTag.Length > CommonConfig.GroupTagMaxLenght)
            {
                _newClanTag = _newClanTag.Remove(CommonConfig.GroupTagMaxLenght);
            }
            if (_newClanMotto.Length > CommonConfig.GroupMottoMaxLength)
            {
                _newClanMotto = _newClanMotto.Remove(CommonConfig.GroupMottoMaxLength);
            }

            GUITools.PushGUIState();
            GUI.enabled &= (_newClanName.Length >= CommonConfig.GroupNameMinLength) && (_newClanTag.Length >= CommonConfig.GroupTagMinLength) && (_newClanMotto.Length >= CommonConfig.GroupNameMinLength);
            if (GUITools.Button(new Rect(comRect.width - 110 - 110, 310, 100, 30), new GUIContent(LocalizedStrings.CreateCaps), BlueStonez.button_green))
            {
                ClanDataManager.Instance.CreateNewClan(_newClanName, _newClanMotto, _newClanTag);
            }
            GUITools.PopGUIState();

            if (GUITools.Button(new Rect(comRect.width - 110, 310, 100, 30), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                createAClan = false;
            }
        }
        GUI.EndGroup();
    }

    private void SortClanMembers()
    {
        if (PlayerDataManager.Instance.ClanMembers != null)
            PlayerDataManager.Instance.ClanMembers.Sort(new ClanSorter());
    }

    private bool IsNoPopupOpen()
    {
        return !PanelManager.IsAnyPanelOpen && !PopupSystem.IsAnyPopupOpen;
    }

    #region Helper Classes

    private class ClanSorter : IComparer<ClanMemberView>
    {
        public int Compare(ClanMemberView a, ClanMemberView b)
        {
            return CompareClanFunctionList.CompareClanMembers(a, b);
        }
    }

    private static class CompareClanFunctionList
    {
        public static int CompareClanMembers(ClanMemberView a, ClanMemberView b)
        {
            int result = ComparePosition(a, b);

            return (result != 0) ? result : CompareName(a, b);
        }

        public static int ComparePosition(ClanMemberView a, ClanMemberView b)
        {
            int posA = 0;
            int posB = 0;
            if (a.Position == GroupPosition.Leader) posA = 1;
            else if (a.Position == GroupPosition.Officer) posA = 2;
            else if (a.Position == GroupPosition.Member) posA = 3;

            if (b.Position == GroupPosition.Leader) posB = 1;
            else if (b.Position == GroupPosition.Officer) posB = 2;
            else if (b.Position == GroupPosition.Member) posB = 3;

            return posA == posB ? 0 : posA > posB ? 1 : -1;
        }

        public static int CompareName(ClanMemberView a, ClanMemberView b)
        {
            return string.Compare(a.Name, b.Name);
        }
    }

    #endregion

    #region Fields

    [SerializeField]
    private Texture2D _level4Icon;
    [SerializeField]
    private Texture2D _licenseIcon;
    [SerializeField]
    private Texture2D _friendsIcon;

    private bool createAClan = false;
    private int _onlineMemberCount;

    private const int _indicatorWidth = 25;
    private const int _positionWidth = 70;
    private const int _joinDateWidth = 80;
    private int _nameWidth;
    private int _statusWidth;

    private Vector2 _clanMembersScrollView;

    private string _newClanName = string.Empty;
    private string _newClanTag = string.Empty;
    private string _newClanMotto = string.Empty;

    #endregion
}