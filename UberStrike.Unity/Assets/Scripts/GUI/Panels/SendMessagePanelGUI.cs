using UnityEngine;
using System.Collections;
using Cmune.DataCenter.Common.Entities;
using System;

public class SendMessagePanelGUI : PanelGuiBase
{
    #region Fields

    private const string FocusReceiver = "Message Receiver";
    private const string FocusContent = "Message Content";

    private bool _useFixedReceiver = false;
    private bool _showComposeMessage;
    private bool _showReceiverDropdownList;

    private string _msgReceiver;
    private string _msgContent;
    private string _lastMsgRcvName;

    private int _msgRcvCmid;
    private int _receiverCount;

    private float _rcvDropdownWidth;
    private float _rcvDropdownHeight;

    private Vector2 _friendDropdownScroll;

    #endregion

    private void OnGUI()
    {
        if (!_showComposeMessage) return;

        GUI.depth = (int)GuiDepth.Panel;
        GUI.skin = BlueStonez.Skin;

        Rect rect = new Rect((Screen.width - 480) / 2, (Screen.height - 320) / 2, 480, 320);

        GUI.Box(rect, GUIContent.none, BlueStonez.window);

        DoCompose(rect);

        if (_showReceiverDropdownList) DoReceiverDropdownList(rect);

        _rcvDropdownHeight = Mathf.Lerp(_rcvDropdownHeight, _showReceiverDropdownList ? 146 : 0, Time.deltaTime * 9);
        if (!_showReceiverDropdownList && Mathf.Approximately(_rcvDropdownHeight, 0))
            _rcvDropdownHeight = 0;

        GUI.enabled = true;
    }

    private void DoCompose(Rect rect)
    {
        Rect comRect = new Rect(rect.x + (rect.width - 480) / 2, rect.y + (rect.height - 320) / 2, 480, 290);
        GUI.BeginGroup(comRect, BlueStonez.window);
        {
            int labelCol = 35;
            int textCol = 120;
            int textWidth = 320;

            int toRow = 70;
            //int subRow = 100;
            int msgRow = 100;

            GUI.Label(new Rect(0, 0, comRect.width, 0), LocalizedStrings.NewMessage, BlueStonez.tab_strip);
            GUI.Box(new Rect(12, 55, comRect.width - 24, comRect.height - 101), GUIContent.none, BlueStonez.window_standard_grey38);

            GUI.Label(new Rect(labelCol, toRow, 75, 20), LocalizedStrings.To, BlueStonez.label_interparkbold_18pt_right);
            GUI.Label(new Rect(labelCol, msgRow, 75, 20), LocalizedStrings.Message, BlueStonez.label_interparkbold_18pt_right);

            bool tmp = GUI.enabled;

            //RECEIVER
            GUI.enabled = tmp && !_useFixedReceiver;
            GUI.SetNextControlName(FocusReceiver);
            _msgReceiver = GUI.TextField(new Rect(textCol, toRow, textWidth, 24), _msgReceiver, BlueStonez.textField);
            if (string.IsNullOrEmpty(_msgReceiver) && !GUI.GetNameOfFocusedControl().Equals(FocusReceiver))
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(new Rect(textCol, toRow, textWidth, 24), " " + LocalizedStrings.StartTypingTheNameOfAFriend, BlueStonez.label_interparkbold_11pt_left);
                GUI.color = Color.white;
            }

            //CONTENT
            GUI.enabled = tmp && !_showReceiverDropdownList;
            GUI.SetNextControlName(FocusContent);
            _msgContent = GUI.TextArea(new Rect(textCol, msgRow, textWidth, 108), _msgContent, 140, BlueStonez.textArea);

            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Label(new Rect(textCol, msgRow + 110, textWidth, 24), (140 - _msgContent.Length).ToString(), BlueStonez.label_interparkbold_11pt_right);
            GUI.color = Color.white;

            //SEND
            GUI.enabled = tmp && !_showReceiverDropdownList && _msgRcvCmid != 0 && !string.IsNullOrEmpty(_msgContent);
            if (GUITools.Button(new Rect(comRect.width - 95 - 100, comRect.height - 44, 90, 32), new GUIContent(LocalizedStrings.SendCaps), BlueStonez.button_green))
            {
                InboxManager.Instance.SendPrivateMessage(_msgRcvCmid, _msgReceiver, _msgContent);

                _msgContent = string.Empty;
                _msgReceiver = string.Empty;
                _msgRcvCmid = 0;
                Hide();
            }
            GUI.enabled = tmp;

            //CANCEL
            if (GUITools.Button(new Rect(comRect.width - 100, comRect.height - 44, 90, 32), new GUIContent(LocalizedStrings.DiscardCaps), BlueStonez.button))
            {
                Hide();
            }

            if (!_showReceiverDropdownList && GUI.GetNameOfFocusedControl().Equals(FocusReceiver))
            {
                _showReceiverDropdownList = true;

                _lastMsgRcvName = _msgReceiver;
                _msgReceiver = string.Empty;
            }
        }
        GUI.EndGroup();

        if (_showReceiverDropdownList)
            DoReceiverDropdownList(rect);
    }

    private void DoReceiverDropdownList(Rect rect)
    {
        Rect rcvRect = new Rect(rect.x + 120, rect.y + 94, 320, _rcvDropdownHeight);
        GUI.BeginGroup(rcvRect, BlueStonez.window);

        if (PlayerDataManager.Instance.FriendsCount > 0)
        {
            int j = 0;
            _friendDropdownScroll = GUITools.BeginScrollView(new Rect(0, 0, rcvRect.width, rcvRect.height), _friendDropdownScroll, new Rect(0, 0, _rcvDropdownWidth, _receiverCount * 24));
            foreach (var friend in PlayerDataManager.Instance.FriendList)
            {
                if (_msgReceiver.Length > 0 && !friend.Name.ToLower().Contains(_msgReceiver.ToLower())) continue;

                Rect r = new Rect(0, j * 24, rcvRect.width, 24);

                if (GUI.enabled && r.Contains(Event.current.mousePosition) && GUI.Button(r, GUIContent.none, BlueStonez.box_grey50))
                {
                    _msgRcvCmid = friend.Cmid;
                    _msgReceiver = friend.Name;
                    _showReceiverDropdownList = false;

                    GUI.FocusControl(FocusContent);
                }

                GUI.Label(new Rect(8, j * 24 + 4, rcvRect.width, rcvRect.height), friend.Name, BlueStonez.label_interparkmed_11pt_left);
                j++;
            }

            _receiverCount = j;

            if (_receiverCount * 24 > rcvRect.height)
                _rcvDropdownWidth = rcvRect.width - 22;
            else
                _rcvDropdownWidth = rcvRect.width - 8;

            GUITools.EndScrollView();
        }
        else GUI.Label(new Rect(0, 0, rcvRect.width, rcvRect.height), LocalizedStrings.YouHaveNoFriends, BlueStonez.label_interparkmed_11pt);

        GUI.EndGroup();

        if (Event.current.type == EventType.MouseDown && !rcvRect.Contains(Event.current.mousePosition))
        {
            _showReceiverDropdownList = false;

            if (_msgRcvCmid == 0) _msgReceiver = _lastMsgRcvName;
        }
    }

    public override void Show()
    {
        base.Show();

        _msgRcvCmid = 0;
        _msgContent = string.Empty;
        _msgReceiver = string.Empty;

        _showComposeMessage = true;
        _showReceiverDropdownList = false;
        _useFixedReceiver = false;

        GUI.FocusControl(FocusReceiver);
    }

    public override void Hide()
    {
        base.Hide();

        _showComposeMessage = false;
        _showReceiverDropdownList = false;
    }

    public void SelectReceiver(int cmid, string name)
    {
        _useFixedReceiver = true;
        _msgRcvCmid = cmid;
        _msgReceiver = name;

        GUI.FocusControl(FocusContent);
    }
}