using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class InviteToClanPanelGUI : PanelGuiBase
{
    #region Fields

    private bool _showReceiverDropdownList;
    private Vector2 _friendListScroll;
    private float _receiverDropdownHeight;

    private int _cmid;
    private string _message = string.Empty;
    private string _name = string.Empty;
    private bool _fixReceiver;

    #endregion

    void OnGUI()
    {
        DrawInvitePlayerMessage(new Rect(0, GlobalUIRibbon.Instance.GetHeight(), GUITools.ScreenWidth, GUITools.ScreenHeight - GlobalUIRibbon.Instance.GetHeight()));
    }

    /// <summary>
    /// Dialog for selecting player to be invited
    /// </summary>
    /// <param name="rect"></param>
    private void DrawInvitePlayerMessage(Rect rect)
    {
        GUI.depth = (int)GuiDepth.Panel;

        GUI.enabled = true;
        Rect comRect = new Rect(rect.x + (rect.width - 480) / 2, rect.y + (rect.height - 320) / 2, 480, 320);
        GUI.BeginGroup(comRect, BlueStonez.window);
        {
            int labelCol = 25;
            int textCol = 120;
            int textWidth = 320;

            int toRow = 70;
            int subRow = 100;
            int msgRow = 132;

            GUI.Label(new Rect(0, 0, comRect.width, 0), LocalizedStrings.InvitePlayer, BlueStonez.tab_strip);
            GUI.Label(new Rect(12, 55, comRect.width - 24, 208), GUIContent.none, BlueStonez.window_standard_grey38);

            GUI.Label(new Rect(labelCol, toRow, 400, 20), LocalizedStrings.UseThisFormToSendClanInvitations, BlueStonez.label_interparkbold_11pt);
            GUI.Label(new Rect(labelCol, subRow, 90, 20), LocalizedStrings.PlayerCaps, BlueStonez.label_interparkbold_18pt_right);
            GUI.Label(new Rect(labelCol, msgRow, 90, 20), LocalizedStrings.MessageCaps, BlueStonez.label_interparkbold_18pt_right);

            GUI.SetNextControlName("Message Receiver");
            GUI.enabled = !_fixReceiver;
            _name = GUI.TextField(new Rect(textCol, subRow, textWidth, 24), _name, BlueStonez.textField);
            if (string.IsNullOrEmpty(_name) && !GUI.GetNameOfFocusedControl().Equals("Message Receiver"))
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(new Rect(textCol, subRow, textWidth, 24), " " + LocalizedStrings.StartTypingTheNameOfAFriend, BlueStonez.label_interparkbold_11pt_left);
                GUI.color = Color.white;
            }

            GUI.enabled = !_showReceiverDropdownList;
            GUI.SetNextControlName("Description");
            _message = GUI.TextArea(new Rect(textCol, msgRow, textWidth, 108), _message, BlueStonez.textArea);
            _message = _message.Trim(new char[] { '\n', '\t' });

            GUI.enabled = _cmid != 0;

            if (GUITools.Button(new Rect(comRect.width - 155 - 155, comRect.height - 44, 150, 32), new GUIContent(LocalizedStrings.SendCaps), BlueStonez.button_green))
            {
                UberStrike.WebService.Unity.ClanWebServiceClient.InviteMemberToJoinAGroup(PlayerDataManager.ClanIDSecure, PlayerDataManager.CmidSecure, _cmid, _message,
                    (ev) =>
                    {
                        //do nothing
                    },
                    (ex) =>
                    {
                        DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    });
                PanelManager.Instance.ClosePanel(PanelType.ClanRequest);
            }

            GUI.enabled = true;

            if (GUITools.Button(new Rect(comRect.width - 155, comRect.height - 44, 150, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                _message = string.Empty;
                PanelManager.Instance.ClosePanel(PanelType.ClanRequest);
            }

            if (!_fixReceiver)
            {
                if (!_showReceiverDropdownList && GUI.GetNameOfFocusedControl().Equals("Message Receiver"))
                {
                    _cmid = 0;
                    _showReceiverDropdownList = true;
                }

                if (_showReceiverDropdownList)
                {
                    DoReceiverDropdownList(new Rect(textCol, subRow + 24, textWidth, _receiverDropdownHeight));
                }
            }
        }
        GUI.EndGroup();
    }

    private void Update()
    {
        _receiverDropdownHeight = Mathf.Lerp(_receiverDropdownHeight, _showReceiverDropdownList ? 146 : 0, Time.deltaTime * 9);
        if (!_showReceiverDropdownList && Mathf.Approximately(_receiverDropdownHeight, 0))
            _receiverDropdownHeight = 0;
    }

    private void DoReceiverDropdownList(Rect rect)
    {
        GUI.BeginGroup(rect, BlueStonez.window);
        {
            int firstFriend = -1;

            if (PlayerDataManager.Instance.FriendsCount > 0)
            {
                int j = 0;
                int i = 0;
                _friendListScroll = GUITools.BeginScrollView(new Rect(0, 0, rect.width, rect.height), _friendListScroll, new Rect(0, 0, rect.width - 20, PlayerDataManager.Instance.FriendsCount * 24));
                foreach (var friend in PlayerDataManager.Instance.FriendList)
                {
                    if (_name.Length > 0 && !friend.Name.ToLower().Contains(_name.ToLower()))
                    {
                        continue;
                    }

                    if (firstFriend == -1) firstFriend = i;

                    bool isInYourClan = !string.IsNullOrEmpty(PlayerDataManager.ClanTag) && friend.GroupTag == PlayerDataManager.ClanTag;
                    Rect r = new Rect(0, j * 24, rect.width, 24);

                    if (GUI.enabled && r.Contains(Event.current.mousePosition) && GUI.Button(r, GUIContent.none, BlueStonez.box_grey50))
                    {
                        if (!isInYourClan)
                        {
                            _cmid = friend.Cmid;
                            _name = friend.Name;

                            _showReceiverDropdownList = false;
                            GUI.FocusControl("Description");
                        }
                    }

                    string name = string.IsNullOrEmpty(friend.GroupTag) ? friend.Name : string.Format("[{0}] {1}", friend.GroupTag, friend.Name);
                    GUI.Label(new Rect(8, j * 24 + 4, rect.width, rect.height), name, BlueStonez.label_interparkmed_11pt_left);

                    if (isInYourClan)
                    {
                        GUI.contentColor = Color.gray;
                        GUI.Label(new Rect(rect.width - 100, j * 24 + 4, 100, rect.height), LocalizedStrings.InMyClan, BlueStonez.label_interparkmed_11pt_left);
                        GUI.contentColor = Color.white;
                    }
                    j++;
                }

                GUITools.EndScrollView();
            }
            else
            {
                GUI.Label(new Rect(0, 0, rect.width, rect.height), LocalizedStrings.YouHaveNoFriends, BlueStonez.label_interparkmed_11pt);
            }
        }
        GUI.EndGroup();

        //click away
        if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
        {
            GUI.FocusControl("Description");
            _showReceiverDropdownList = false;
            PublicProfileView view;
            if (PlayerDataManager.TryGetFriend(_cmid, out view))
            {
                _name = view.Name;
            }
            else
            {
                _name = string.Empty;
                _cmid = 0;
            }
        }
    }

    public override void Show()
    {
        base.Show();

        _message = string.Format(LocalizedStrings.HiYoureInvitedToJoinMyClanN, PlayerDataManager.ClanName);
    }

    public override void Hide()
    {
        base.Hide();

        _name = string.Empty;
        _fixReceiver = false;
        _cmid = 0;
    }

    public void SelectReceiver(int cmid, string name)
    {
        _cmid = cmid;
        _name = name;

        _fixReceiver = (_cmid != 0);
    }
}