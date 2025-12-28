using System;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class FriendRequest
{
    public FriendRequest(ContactRequestView request, Action<FriendRequest> onAccept, Action<FriendRequest> onIgnore, CanRespond canRespond)
    {
        _request = request;
        _onAccept = onAccept;
        _onIgnore = onIgnore;
        _canRespond = canRespond;
    }

    public void Draw(int y, int width)
    {
        Rect reqRect = new Rect(4, y + 4, width - 1, PanelHeight);
        GUI.BeginGroup(reqRect);
        {
            Rect rect = new Rect(0, 0, reqRect.width, reqRect.height - 1);
            if (GUI.enabled && rect.Contains(Event.current.mousePosition))
            {
                // highlight background
                GUI.Box(rect, GUIContent.none, BlueStonez.box_grey50);
            }
            GUI.Label(new Rect(120, 5, 200, 20), string.Format("{0}: {1}", LocalizedStrings.FriendRequest, Name), BlueStonez.label_interparkbold_13pt_left);
            GUI.Label(new Rect(120, 30, 400, 20), Message, BlueStonez.label_interparkmed_11pt_left);

            bool tmp = GUI.enabled;
            GUI.enabled = tmp && _canRespond();

            if (GUITools.Button(new Rect(reqRect.width - 120 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Accept), BlueStonez.buttondark_medium))
            {
                if (_onAccept != null)
                    _onAccept(this);
            }
            if (GUITools.Button(new Rect(reqRect.width - 50 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Ignore), BlueStonez.buttondark_medium))
            {
                if (_onIgnore != null)
                    _onIgnore(this);
            }

            GUI.enabled = tmp;
        }
        GUI.EndGroup();

        GUI.Label(new Rect(4, y + PanelHeight + 8, width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
    }

    public const int PanelHeight = 50;

    #region Properties

    public string Name
    {
        get { return _request.InitiatorName; }
    }

    public string Message
    {
        get { return _request.InitiatorMessage; }
    }

    public int RequestId
    {
        get { return _request.RequestId; }
    }

    public int Cmid
    {
        get { return _request.InitiatorCmid; }
    }

    #endregion

    #region Fields
    public delegate bool CanRespond();

    private Action<FriendRequest> _onAccept;
    private Action<FriendRequest> _onIgnore;
    private CanRespond _canRespond;
    private ContactRequestView _request;
    #endregion
}