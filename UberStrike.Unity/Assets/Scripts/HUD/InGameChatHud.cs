using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Helper;

public class InGameChatHud : Singleton<InGameChatHud>
{
    private int _maxMessageLength = 140;

    private bool _isEnabled;
    public bool Enabled { get { return _isEnabled; } set { _isEnabled = value; } }

    public bool CanInput
    {
        get { return _canInput; }
    }

    public void Update()
    {
        if (!PopupSystem.IsAnyPopupOpen && !InputManager.Instance.IsAnyDown &&
            _spamTimer <= 0 && (_chatTimer <= 0 || _canInput) &&
            Input.GetKeyDown(KeyCode.Return))
        {
            if (ClientCommCenter.IsPlayerMuted)
            {
                _muteTimer = 2;
            }
            else
            {
                _canInput = !_canInput;
                Input.ResetInputAxes();

                if (_canInput)
                {
                    BeginChat();
                }
                else
                {
                    EndChat();
                }
            }
        }

        if (_canInput && Input.GetKeyDown(KeyCode.Return))
        {
        }

        for (int i = 0; i < _chatMsgs.Count; i++)
        {
            _chatMsgs[i].Timer -= Time.deltaTime * MsgFadeSpeed;
            if (_chatMsgs[i].Timer < 0) _chatMsgs.RemoveAt(i);
        }

        MsgPosition.height = Screen.height - MsgPosition.y - (GameState.LocalPlayer.IsGamePaused ? 70 : 140);

        if (_chatTimer > 0) _chatTimer -= Time.deltaTime;
        if (_spamTimer > 0) _spamTimer -= Time.deltaTime;
    }

    public void Draw()
    {
        GUI.depth = (int)GuiDepth.Chat;

        if (TabScreenPanelGUI.Enabled) return;

        GUI.BeginGroup(MsgPosition);
        {
            DoChatMessages();

            if (ClientCommCenter.IsPlayerMuted)
                DoMuteMessage();
            else if (_spamTimer > 0)
                DoSpamMessage();
            else if (_canInput)
                DoChatInput();
        }
        GUI.EndGroup();

        if (_doFocusOnChat)
        {
            GUI.FocusControl("input");
            _doFocusOnChat = false;
        }
    }

    private InGameChatHud()
    {
        _chatMsgs = new List<ChatMessage>(10);
        ClearAll();
        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        switch (ev.TeamId)
        {
            case TeamID.BLUE:
            case TeamID.NONE:
                _textFieldStyle = StormFront.InGameChatBlue;
                break;
            case TeamID.RED:
                _textFieldStyle = StormFront.InGameChatRed;
                break;
        }
    }

    private void DoChatMessages()
    {
        MsgStyle.wordWrap = true;

        for (int i = 0; i < _chatMsgs.Count; i++)
        {
            ChatMessage msg = _chatMsgs[i];
            string senderStr = msg.Sender + ": ";
            int nameHeight = Mathf.CeilToInt(MsgStyle.CalcHeight(new GUIContent(senderStr), MsgPosition.width));
            string contentStr = senderStr + msg.Content;

            float alpha = Mathf.Clamp01(msg.Timer);
            GUI.color = msg.Color.SetAlpha(alpha);
            GUI.Label(new Rect(msg.Position.x + 1, msg.Position.y + 1, msg.Position.width, msg.Position.height),
                contentStr, MsgStyle);
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.Label(msg.Position, contentStr, MsgStyle);
            GUI.color = new Color(0, 0, 0, alpha * 0.4f);
            GUI.Label(new Rect(msg.Position.x, msg.Position.y, msg.Position.width, nameHeight), senderStr, MsgStyle);
        }

        MsgStyle.wordWrap = false;
    }

    private void DoChatInput()
    {
        Rect pos = new Rect(44, MsgPosition.height - InputHeight, MsgPosition.width - 44, InputHeight);

        GUI.color = Color.white;
        GUI.SetNextControlName("input");
        _inputContent = GUI.TextField(pos, _inputContent, _maxMessageLength, _textFieldStyle);
        _inputContent = _inputContent.Trim(new char[] { '\n', '\t' });

        GUI.color = Color.black;
        GUI.Label(new Rect(1, MsgPosition.height - InputHeight + 1, MsgPosition.width, InputHeight), LocalizedStrings.Chat + ":", MsgStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, MsgPosition.height - InputHeight, MsgPosition.width, InputHeight), LocalizedStrings.Chat + ":", MsgStyle);

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return &&
            Event.current.type == EventType.KeyUp)
        {
            if (_chatTimer <= 0)
            {
                _canInput = false;
                GUIUtility.keyboardControl = 0;
                Event.current.Use();
                EndChat();
            }
            else
            {
                Event.current.Use();
            }
        }

        if (Event.current.keyCode == KeyCode.Escape)
        {
            _inputContent = string.Empty;
            _canInput = false;
            GUIUtility.keyboardControl = 0;
            Event.current.Use();

            EndChat();
        }
    }

    private void DoMuteMessage()
    {
        if (_muteTimer <= 0) return;

        GUI.color = Color.red;
        GUI.Label(new Rect(1, MsgPosition.height - InputHeight + 1, MsgPosition.width, InputHeight), _muteMessage, MsgStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, MsgPosition.height - InputHeight, MsgPosition.width, InputHeight), _muteMessage, MsgStyle);

        _muteTimer -= Time.deltaTime;
    }

    private void DoSpamMessage()
    {
        GUI.color = Color.red;
        GUI.Label(new Rect(1, MsgPosition.height - InputHeight + 1, MsgPosition.width, InputHeight), _spamMessage, MsgStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, MsgPosition.height - InputHeight, MsgPosition.width, InputHeight), _spamMessage, MsgStyle);
    }

    public void AddChatMessage(string sender, string message, MemberAccessLevel accessLevel)
    {
        MsgStyle.wordWrap = true;
        string senderStr = sender + ": ";
        string contentStr = senderStr + message;
        int height = Mathf.CeilToInt(MsgStyle.CalcHeight(new GUIContent(contentStr), MsgPosition.width));

        ChatMessage msg = new ChatMessage()
        {
            Sender = sender,
            Content = message,
            Timer = MsgLifespan,
            Height = height,
            Position = new Rect(0, 0, MsgPosition.width, height),
            Color = accessLevel == MemberAccessLevel.Admin ? ColorScheme.ChatNameAdminUser : (accessLevel == MemberAccessLevel.Default ? Color.black : ColorScheme.ChatNameModeratorUser),
        };

        _chatMsgs.Insert(0, msg);

        UpdateMessagePosition();
    }

    public void ClearHistory()
    {
        _chatMsgs.Clear();
    }

    public void SendChatMessage()
    {
        string msg = TextUtilities.Trim(_inputContent);

        if (msg.Length == 0) return;

        if (!ClientCommCenter.IsPlayerMuted)
        {
            if (!CommConnectionManager.CommCenter.SendInGameChatMessage(msg, ChatContext.Player))
                _spamTimer = 5;
        }

        _inputContent = string.Empty;
    }

    public void ClearAll()
    {
        _canInput = false;
        _inputContent = string.Empty;
        _chatMsgs.Clear();
    }

    public void Pause()
    {
        if (_canInput) _paused = true;
    }

    public void OnFullScreen()
    {
        UpdateMessagePosition();
    }

    #region Private
    private void BeginChat()
    {
        _doFocusOnChat = true;
        _chatTimer = 0.5f;
        //_enableTime = Time.time;

        InputManager.Instance.IsInputEnabled = false;
    }

    private void EndChat()
    {
        SendChatMessage();

        _chatTimer = 0.3f;

        if (_paused)
        {
            _paused = false;

            if (GameState.HasCurrentGame && GameState.CurrentGame.IsMatchRunning)
                GameState.LocalPlayer.UnPausePlayer();
        }

        if (GameState.HasCurrentGame && GameState.CurrentGame.IsMatchRunning && !GameState.LocalPlayer.IsGamePaused)
            InputManager.Instance.IsInputEnabled = true;
    }

    private void UpdateMessagePosition()
    {
        int length = 0;
        int index = 0;
        bool remove = false;

        for (int i = 0; i < _chatMsgs.Count; i++)
        {
            ChatMessage msg = _chatMsgs[i];

            msg.Position.y = MsgPosition.height - InputHeight - msg.Height - length;
            length += msg.Height;

            if (msg.Position.y < 0)
            {
                index = i;
                remove = true;
                break;
            }

            if (remove) _chatMsgs.RemoveRange(index, _chatMsgs.Count - index);
        }
    }

    private GUIStyle MsgStyle
    {
        get
        {
            if (_msgStyleCache == null)
                _msgStyleCache = BlueStonez.label_ingamechat;
            return _msgStyleCache;
        }
    }

    private bool _canInput = false;

    private Rect MsgPosition = new Rect(10, 160, 300, 360);
    private float MsgLifespan = 10;
    private float MsgFadeSpeed = 1;

    private const int InputHeight = 30;

    private string _inputContent = string.Empty;
    private string _muteMessage = "You are not allowed to chat!";
    private string _spamMessage = "Don't spam!";
    private GUIStyle _msgStyleCache;
    private bool _paused;
    private bool _doFocusOnChat;
    private List<ChatMessage> _chatMsgs;
    private float _chatTimer;
    private float _muteTimer;
    private float _spamTimer;
    private GUIStyle _textFieldStyle = StormFront.InGameChatBlue;

    private class ChatMessage
    {
        public string Sender;
        public string Content;
        public float Timer;
        public int Height;
        public Rect Position;
        public Color Color;
    }

    private enum State
    {
        Normal,
        Ghost,
        Mute
    }
    #endregion
}