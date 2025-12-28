using System.Collections.Generic;
using System.Text;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class ChatDialog
{
    public ChatDialog(string title)
    {
        Title = title;
        UserName = string.Empty;

        _msgHeight = new List<float>();
        _msgQueue = new Queue<InstantMessage>();

        AddMessage(new InstantMessage(0, 0, "Disclaimer", "Do not share your password or any other confidential information with anybody. The members of Cmune and the Uberstrike Moderators will never ask you to provide such information.", MemberAccessLevel.Admin));
    }

    public ChatDialog(CommUser user, UserGroups group)
        : this(string.Empty)
    {
        Group = group;

        if (user != null)
        {
            UserName = user.ShortName;
            UserCmid = user.Cmid;
        }
    }

    public void AddMessage(InstantMessage msg)
    {
        _reset = true;

        while (_msgQueue.Count > 200)
        {
            _msgQueue.Dequeue();
        }

        _msgQueue.Enqueue(msg);

        //don't scroll if user is scrolling right now
        if (!Input.GetMouseButton(0))
        {
            _scroll.y = float.MaxValue;
        }
    }

    public void ScrollToEnd()
    {
        _scroll.y = Mathf.Infinity;
    }

    public void Clear()
    {
        _msgQueue.Clear();
    }

    public bool CanChat
    {
        get { return UserCmid == 0 || CommConnectionManager.IsPlayerOnline(UserCmid); }
    }

    public void CheckSize(Rect rect)
    {
        if (_reset || rect.width != _frameSize.x || rect.height != _frameSize.y)
        {
            float total = 0;

            _reset = false;

            _frameSize.x = rect.width;
            _frameSize.y = rect.height;

            _contentSize.x = rect.width;
            _contentSize.y = rect.height;

            _msgHeight.Clear();

            foreach (InstantMessage msg in _msgQueue)
            {
                float height = BlueStonez.label_interparkbold_11pt_left_wrap.CalcHeight(new GUIContent(msg.MessageText), _contentSize.x - 8);

                height += 24;
                _msgHeight.Add(height);
                total += height;
            }

            //add scrollbars dreceases the total width by 17px
            if (total > rect.height)
            {
                total = 0;
                _msgHeight.Clear();

                _contentSize.x = rect.width - 17;

                foreach (InstantMessage msg in _msgQueue)
                {
                    float height = BlueStonez.label_interparkbold_11pt_left_wrap.CalcHeight(new GUIContent(msg.MessageText), _contentSize.x - 8);

                    height += 24;
                    _msgHeight.Add(height);
                    total += height;
                }

                _contentSize.y = total;
            }
        }
    }

    public override string ToString()
    {
        StringBuilder s = new StringBuilder();
        s.AppendLine("Title: " + Title);
        s.AppendLine("Group: " + Group);
        s.AppendLine("User: " + UserName + " " + UserCmid);
        s.AppendLine("CanChat: " + CanChat);
        return s.ToString();
    }

    public CanShowMessage CanShow;

    public delegate bool CanShowMessage(ChatContext c);

    public string Title
    {
        set { _title = value; }
        get
        {
            if (UserCmid > 0)
            {
                return CommConnectionManager.IsPlayerOnline(UserCmid) ? "Chat with " + UserName : UserName + " is offline";
            }
            else
            {
                return _title;
            }
        }
    }
    public string UserName { get; private set; }
    public int UserCmid { get; private set; }
    public UserGroups Group { get; set; }
    public bool HasUnreadMessage { get; set; }

    public ICollection<InstantMessage> AllMessages { get { return new List<InstantMessage>(_msgQueue.ToArray()); } }

    #region Fields

    public List<float> _msgHeight;
    public Queue<InstantMessage> _msgQueue;

    private bool _reset;
    private string _title;
    private Vector2 _scroll;
    public Vector2 _frameSize;
    public Vector2 _contentSize;

    public float _heightCache;

    #endregion
}