using Cmune.Realtime.Common;
using UnityEngine;

public class FriendRequestPanelGUI : PanelGuiBase
{
    private string _friendMessage;
    private string _friendName;
    private Vector2 _friReqScroll;
    private float _friReqWidth;
    private int _msgRcvCmid;

    private bool _isSelectionLocked;

    public void Show(int cmid, string name)
    {
        _msgRcvCmid = cmid;
        _friendName = name;
        _isSelectionLocked = true;
    }

    public override void Show()
    {
        base.Show();

        _msgRcvCmid = 0;
        _isSelectionLocked = false;

        _friendName = string.Empty;
        _friendMessage = string.Empty;
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Panel;
        GUI.skin = BlueStonez.Skin;

        Rect rect = new Rect((Screen.width - 480) / 2, (Screen.height - 320) / 2, 480, 320);

        GUI.Box(rect, GUIContent.none, BlueStonez.window);

        DoInvite(rect);

        GUI.enabled = true;
    }

    private void DoInvite(Rect rect)
    {
        Rect comRect = new Rect(rect.x, rect.y, 480, 320);
        GUI.BeginGroup(comRect, BlueStonez.window);
        {
            int labelCol = 35;
            int textCol = 120;
            int textWidth = 320;

            int nameRow = 70;
            int msgRow = 220;

            GUI.Label(new Rect(0, 0, comRect.width, 0), LocalizedStrings.FriendRequestCaps, BlueStonez.tab_strip);
            GUI.Label(new Rect(12, 55, comRect.width - 24, 208), GUIContent.none, BlueStonez.window_standard_grey38);

            GUI.Label(new Rect(labelCol - 5, nameRow + 2, 85, 20), LocalizedStrings.PlayerCaps, BlueStonez.label_interparkbold_18pt_right);
            GUI.Label(new Rect(labelCol - 5, msgRow + 2, 85, 20), LocalizedStrings.MessageCaps, BlueStonez.label_interparkbold_18pt_right);

            bool tmp = GUI.enabled;

            GUI.enabled = tmp && !_isSelectionLocked;
            _friendName = GUI.TextField(new Rect(textCol, nameRow, textWidth, 24), _friendName, BlueStonez.textField);

            GUI.enabled = tmp;
            GUI.changed = false;
            _friendMessage = GUI.TextField(new Rect(textCol, msgRow, textWidth, 24), _friendMessage, BlueStonez.textField);
            _friendMessage = _friendMessage.Trim(new char[] { '\n' });

            // player list
            Rect pRect = new Rect(textCol, nameRow + 24, textWidth, 116);
            GUI.Box(pRect, GUIContent.none, BlueStonez.window);

            int y = 0;
            if (string.IsNullOrEmpty(_friendName))
                _msgRcvCmid = 0;

            _friReqScroll = GUITools.BeginScrollView(pRect, _friReqScroll, new Rect(0, 0, _friReqWidth, pRect.height));
            foreach (CommActorInfo i in CommConnectionManager.CommCenter.Players)
            {
                if (i.Cmid == PlayerDataManager.Cmid || PlayerDataManager.IsFriend(i.Cmid))
                    continue;

                if (!string.IsNullOrEmpty(_friendName) && !i.PlayerName.ToLower().Contains(_friendName.ToLower()))
                    continue;

                Rect iRect = new Rect(0, y, _friReqWidth, 24);

                if (_msgRcvCmid == i.Cmid)
                    GUI.Label(iRect, GUIContent.none, BlueStonez.window_standard_grey38);

                GUI.Label(new Rect(iRect.x + 8, iRect.y + 4, iRect.width, iRect.height), i.PlayerName, BlueStonez.label_interparkmed_11pt_left);
                if (GUI.Button(iRect, GUIContent.none, GUIStyle.none))
                {
                    _msgRcvCmid = i.Cmid;
                    _friendName = i.PlayerName;
                }

                y += 24;
            }
            GUITools.EndScrollView();

            if (y == 0)
                GUI.Label(pRect, LocalizedStrings.Empty, BlueStonez.label_interparkmed_11pt);
            if (y > pRect.height)
                _friReqWidth = (int)pRect.width - 22;
            else
                _friReqWidth = (int)pRect.width;

            if (GUITools.Button(new Rect(comRect.width - 95, comRect.height - 44, 90, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                _friendName = string.Empty;
                _friendMessage = string.Empty;

                Hide();
            }

            GUI.enabled = GUI.enabled && _msgRcvCmid != 0 && !string.IsNullOrEmpty(_friendMessage);
            if (GUITools.Button(new Rect(comRect.width - 95 - 95, comRect.height - 44, 90, 32), new GUIContent(LocalizedStrings.SendCaps), BlueStonez.button_green))
            {
                GUIUtility.keyboardControl = 0;
                CommsManager.Instance.SendFriendRequest(_msgRcvCmid, _friendMessage);
                Hide();
            }
            GUI.enabled = tmp;
        }
        GUI.EndGroup();
    }
}
