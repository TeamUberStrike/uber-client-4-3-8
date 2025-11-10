
using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Helper;

/// A thread is a group of messages that share the same subject.
/// the subject for a thread has three parts:
/// Part 1: 2 chars to identify the version;
/// Part 2: 4 chars to identify the first PrivateMessageId in the thread;
/// Part 3: rest chars are the subject text of the thread;
public class InboxThread
{
    #region Fields
    public Vector2 Scroll;

    // 767 is currently the cmdi we use for admin messages
    public const int AdminCmid = 767;

    public static InboxThread Current { get; set; }

    private bool _messagesLoaded = false;
    private MessageThreadView _threadView;
    private SortedList<int, InboxMessage> _messages;
    private int _curPageIndex = 0;

    public const int NameWidth = 100;
    public const int ThreadHeight = 76;
    #endregion

    #region Properties
    public bool IsLoading { get; set; }
    public DateTime LastServerUpdate { get; private set; }
    public int ThreadId { get { return _threadView.ThreadId; } }
    public string Name { get { return _threadView.ThreadName; } }
    public DateTime LastMessageDateTime { get { return _threadView.LastUpdate; } }
    public IEnumerable<InboxMessage> Messages { get { return _messages.Values; } }
    public bool HasUnreadMessage { get { return _threadView.HasNewMessages; } }
    private string Date
    {
        get { return _threadView.LastUpdate.ToString("yyyy MMM ") + " " + _threadView.LastUpdate.Day.ToString() + " at " + _threadView.LastUpdate.ToShortTimeString(); }
    }
    public bool IsAdmin
    {
        get { return ThreadId == InboxThread.AdminCmid; }
    }
    #endregion

    public InboxThread(MessageThreadView threadView)
    {
        _threadView = threadView;
        _messages = new SortedList<int, InboxMessage>(threadView.MessageCount, new MessageSorter());
        LastServerUpdate = _threadView.LastUpdate;
    }

    private class MessageSorter : IComparer<int>
    {
        public int Compare(int obj1, int obj2)
        {
            return obj1 - obj2;
        }
    }

    public bool Contains(string keyword)
    {
        bool result = false;
        string lower = keyword.ToLower();

        if (_threadView.ThreadName.ToLower().Contains(lower))
            return true;

        foreach (InboxMessage msg in _messages.Values)
        {
            if (msg.Content.ToLower().Contains(lower))
            {
                result = true;
                break;
            }
        }

        return result;
    }

    public int DrawThread(int y, int width)
    {
        Rect rect = new Rect(8, y + 8, width - 8, ThreadHeight - 8);

        //is currently selected
        if (Current == this)
        {
            GUI.Box(new Rect(4, y + 4, width, ThreadHeight), GUIContent.none, BlueStonez.box_grey50);
        }

        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(0, 0, width, 18), string.Format("{0} ({1})", _threadView.ThreadName, _threadView.MessageCount), BlueStonez.label_interparkbold_13pt);
            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Label(new Rect(0, 20, width, 10), Date, BlueStonez.label_interparkmed_10pt_left);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 50, width, 18), _threadView.LastMessagePreview, BlueStonez.label_interparkmed_10pt_left);
        }
        GUI.EndGroup();

        Rect btnRect = new Rect(width - 18, y + 9, 16, 16);

        if (GUI.enabled && rect.Contains(Event.current.mousePosition))
        {
            GUI.Box(new Rect(4, y + 4, width, ThreadHeight), GUIContent.none, BlueStonez.group_grey81);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !btnRect.Contains(Event.current.mousePosition))
            {
                Current = this;

                Scroll.y = float.MinValue;

                //load messages if not loaded yet
                if (!_messagesLoaded)
                {
                    _messagesLoaded = true;
                    InboxManager.Instance.LoadMessagesForThread(this, 0);
                }

                //set all messages to read = true
                if (_threadView.HasNewMessages)
                {
                    _threadView.HasNewMessages = false;
                    InboxManager.Instance.MarkThreadAsRead(_threadView.ThreadId);
                }

                Event.current.Use();
            }
        }

        if (_threadView.HasNewMessages)
            GUI.Label(new Rect(width - 40, y + 5, 29, 29), UberstrikeIcons.NewMessageIcon);

        return y + InboxThread.ThreadHeight + 8;
    }

    public int DrawMessageList(int y, int scrollRectWidth, float scrollRectHeight, float curScrollY)
    {
        for (int i = _messages.Values.Count - 1; i >= 0; i--)
        {
            InboxMessage msg = _messages.Values[i];
            y += DrawContent(msg, y + 12, scrollRectWidth) + 16;
        }

        if (_messages.Count == 0)
        {
            GUI.Label(new Rect(0, y, scrollRectWidth, 100), "This thread is empty", BlueStonez.label_interparkbold_13pt);
        }
        else
        {
            float maxScrollY = y - scrollRectHeight;
            maxScrollY = Mathf.Clamp(maxScrollY, 0, maxScrollY);
            if (curScrollY >= maxScrollY && //down to the bottom
                _threadView.MessageCount > _messages.Count &&
                IsLoading == false)
            {
                ++_curPageIndex;
                InboxManager.Instance.LoadMessagesForThread(this, _curPageIndex);
            }
        }

        if (IsLoading)
        {
            GUI.Label(new Rect(0, y, scrollRectWidth, 30), "Loading messages...", BlueStonez.label_interparkbold_13pt);
            y += 30;
        }

        return y;
    }

    public int DrawContent(InboxMessage msg, int y, int width)
    {
        if (msg.IsMine)
        {
            return DrawMyMessage(msg, 100, y, width - 100);
        }
        else
        {
            return DrawOtherMessage(msg, 0, y, width - 100);
        }
    }

    private int DrawOtherMessage(InboxMessage msg, int x, int y, int width)
    {
        int height = Mathf.RoundToInt(BlueStonez.speechbubble_left.CalcHeight(new GUIContent(msg.Content), width)) + 30;
        Rect msgRect = new Rect(x, y, width, height);

        GUI.color = new Color(0.5f, 0.5f, 0.5f);
        int nameWidth = (int)BlueStonez.label_interparkbold_11pt_left.CalcSize(new GUIContent(msg.SenderName)).x;
        int dateWidth = (int)BlueStonez.label_interparkmed_10pt_left.CalcSize(new GUIContent(msg.SentDateString)).x;

        GUI.Label(new Rect(msgRect.x + 28, msgRect.y - 16, msgRect.width, 12), msg.SenderName, BlueStonez.label_interparkbold_11pt_left);
        GUI.Label(new Rect(msgRect.x + nameWidth + 34, msgRect.y - 15, msgRect.width, 12), msg.SentDateString, BlueStonez.label_interparkmed_10pt_left);
        if (!msg.IsAdmin && GUITools.Button(new Rect(msgRect.x + nameWidth + dateWidth + 40, msgRect.y - 17, 36, 12), new GUIContent(LocalizedStrings.Report), BlueStonez.label_interparkmed_11pt_url))
        {
            ReportPlayerPanelGUI.ReportInboxPlayer(msg.MessageView, msg.SenderName);
        }
        GUI.color = Color.white;

        GUI.BeginGroup(msgRect);
        {
            GUI.backgroundColor = new Color(1, 1, 1, 0.5f);
            GUI.TextArea(new Rect(0, 0, msgRect.width, height), msg.Content, BlueStonez.speechbubble_left);
            GUI.backgroundColor = Color.white;
        }
        GUI.EndGroup();

        return height;
    }

    private int DrawMyMessage(InboxMessage msg, int x, int y, int width)
    {
        int height = Mathf.RoundToInt(BlueStonez.speechbubble_right.CalcHeight(new GUIContent(msg.Content), width)) + 30;
        Rect msgRect = new Rect(x, y, width, height);

        GUI.color = new Color(0.5f, 0.5f, 0.5f);
        int nameWidth = (int)BlueStonez.label_interparkbold_11pt_left.CalcSize(new GUIContent(msg.SenderName)).x;
        int dateWidth = (int)BlueStonez.label_interparkmed_10pt_left.CalcSize(new GUIContent(msg.SentDateString)).x;
        GUI.Label(new Rect((msgRect.x + msgRect.width) - (dateWidth + nameWidth + 40), msgRect.y - 16, nameWidth + 2, 12), msg.SenderName, BlueStonez.label_interparkbold_11pt_left);
        GUI.Label(new Rect((msgRect.x + msgRect.width) - (dateWidth + 32), msgRect.y - 15, dateWidth + 2, 12), msg.SentDateString, BlueStonez.label_interparkmed_10pt_left);
        GUI.color = Color.white;

        GUI.BeginGroup(msgRect);
        {
            GUI.backgroundColor = new Color(0.376f, 0.631f, 0.886f, 0.5f);
            GUI.TextArea(new Rect(msgRect.width - msgRect.width, 0, msgRect.width, height), msg.Content, BlueStonez.speechbubble_right);
            GUI.backgroundColor = Color.white;
        }
        GUI.EndGroup();

        return height;
    }

    internal void UpdateThread(MessageThreadView newThreadView)
    {
        if (newThreadView.MessageCount != _threadView.MessageCount)
            _messagesLoaded = false;

        _threadView = newThreadView;

        LastServerUpdate = _threadView.LastUpdate;
    }

    internal void AddMessage(PrivateMessageView message)
    {
        if (!_messages.ContainsKey(message.PrivateMessageId))
        {
            _messages.Add(message.PrivateMessageId, new InboxMessage(message, message.FromCmid == PlayerDataManager.Cmid ? PlayerDataManager.Name : _threadView.ThreadName));

            _threadView.MessageCount++;
            if (!message.IsRead && message.ToCmid == PlayerDataManager.Cmid)
                _threadView.HasNewMessages = true;

            if (message.DateSent > _threadView.LastUpdate)
            {
                _threadView.LastUpdate = message.DateSent;
                _threadView.LastMessagePreview = TextUtilities.ShortenText(message.ContentText, 25, true);
            }
        }

        Scroll.y = float.MinValue;
    }

    internal void AddMessages(List<PrivateMessageView> messages)
    {
        foreach (var msg in messages)
        {
            if (!_messages.ContainsKey(msg.PrivateMessageId))
                _messages.Add(msg.PrivateMessageId, new InboxMessage(msg, msg.FromCmid == PlayerDataManager.Cmid ? PlayerDataManager.Name : _threadView.ThreadName));
        }
    }
}