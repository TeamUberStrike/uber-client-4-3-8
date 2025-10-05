//using System;
//using Cmune.DataCenter.Common.Entities;
//using UnityEngine;

//public class ClanRequest
//{
//    public ClanRequest(GroupInvitationView request)
//    {
//        _request = request;
//    }

//    public void Draw(int y, int width)
//    {
//        Rect reqRect = new Rect(4, y + 4, width - 1, PanelHeight);
//        GUI.BeginGroup(reqRect);

//        Rect rect = new Rect(0, 0, reqRect.width, reqRect.height - 1);
//        if (GUI.enabled && rect.Contains(Event.current.mousePosition))
//        {
//            // highlight background
//            GUI.Box(rect, GUIContent.none, BlueStonez.box_grey50);
//        }

//        GUI.Label(new Rect(120, 5, 200, 20), string.Format("{0}: {1}", LocalizedStrings.ClanInvite, Name), BlueStonez.label_interparkbold_13pt_left);
//        GUI.Label(new Rect(120, 30, 400, 20), Message, BlueStonez.label_interparkmed_11pt_left);

//        if (GUITools.Button(new Rect(reqRect.width - 120 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Accept), BlueStonez.buttondark_medium))
//        {
//            if (InboxPageGUI.IsInitialized)
//                InboxPageGUI.Instance.OnClanAccept(this);
//        }
//        if (GUITools.Button(new Rect(reqRect.width - 50 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Ignore), BlueStonez.buttondark_medium))
//        {
//            if (InboxPageGUI.IsInitialized)
//                InboxPageGUI.Instance.OnClanIgnore(this);
//        }

//        GUI.EndGroup();

//        GUI.Label(new Rect(4, y + PanelHeight + 8, width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
//    }

//    public const int PanelHeight = 50;

//    #region Properties

//    public string Name
//    {
//        get { return string.Format("[{0}] {1}", _request.GroupTag, _request.GroupName); }
//    }

//    public string Message
//    {
//        get { return _request.Message; } // need to change to correct one
//    }

//    public int RequestId
//    {
//        get { return _request.GroupInvitationId; }
//    }

//    public int Cmid
//    {
//        get { return _request.InviterCmid; }
//    }

//    #endregion

//    #region Fields
//    private GroupInvitationView _request;
//    #endregion
//}
