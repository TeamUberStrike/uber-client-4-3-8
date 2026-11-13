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

    public void OpenChat(bool showHudInput)
    {
        if (showHudInput)
            _canInput = true;

        BeginChat();
    }

    public void CancelChat()
    {
        _canInput = false;
        _inputContent = string.Empty;
        GUIUtility.keyboardControl = 0;
        EndChat();
    }

    public void PushMessage(string input)
    {
        _inputContent = input;

        EndChat();
    }

    public void Update()
    {
        // click away: close on release, else click hits gun
        if (_closeOnMouseUp && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            CloseInput();

        if (!InGameChatPageGUI.IsOpen && !PopupSystem.IsAnyPopupOpen &&
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

        for (int i = _chatMsgs.Count - 1; i >= 0; i--)
        {
            _chatMsgs[i].Timer -= Time.deltaTime * MsgFadeSpeed;
            if (_chatMsgs[i].Timer < 0) _chatMsgs.RemoveAt(i);
        }

        // mid left like steam: input line 30% above bottom
        float screenHeight = Screen.height / Scale;
        float top = screenHeight * 0.3f;
        float height = screenHeight * 0.4f;
        if (top != MsgPosition.y || height != MsgPosition.height)
        {
            MsgPosition.y = top;
            MsgPosition.height = height;
            UpdateMessagePosition();
        }

        if (_chatTimer > 0) _chatTimer -= Time.deltaTime;
        if (_spamTimer > 0) _spamTimer -= Time.deltaTime;
    }

    public void Draw()
    {
        GUI.depth = (int)GuiDepth.Chat;

        if (TabScreenPanelGUI.Enabled) return;

        EnsureStyles();

        GUI.BeginGroup(Px(MsgPosition));
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
        CmuneEventHandler.AddListener<OnPlayerRespawnEvent>(OnPlayerRespawn);
    }

    private void OnPlayerRespawn(OnPlayerRespawnEvent ev)
    {
        CloseIfAbandoned();
    }

    // respawn: focused input stays, abandoned one closes, draft kept
    public void CloseIfAbandoned()
    {
        if (!_canInput) return;

        if (_closeOnMouseUp || !_inputFocused)
        {
            CloseInput();
            return;
        }

        if (!GameState.LocalPlayer.IsGamePaused) _paused = false;
        InputManager.Instance.IsInputEnabled = false;
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
            Rect p = Px(msg.Position);
            int nameHeight = Mathf.CeilToInt(MsgStyle.CalcHeight(new GUIContent(senderStr), p.width));
            string contentStr = senderStr + msg.Content;

            float alpha = Mathf.Clamp01(msg.Timer);
            GUI.color = msg.Color.SetAlpha(alpha);
            GUI.Label(new Rect(p.x + Scale, p.y + Scale, p.width, p.height), contentStr, MsgStyle);
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.Label(p, contentStr, MsgStyle);
            GUI.color = new Color(0, 0, 0, alpha * 0.4f);
            GUI.Label(new Rect(p.x, p.y, p.width, nameHeight), senderStr, MsgStyle);
        }

        MsgStyle.wordWrap = false;
    }

    private void DoChatInput()
    {
        Rect pos = Px(new Rect(44, MsgPosition.height - InputHeight, MsgPosition.width - 44, InputHeight));

        if (Event.current.rawType == EventType.MouseDown &&
            !Px(new Rect(0, MsgPosition.height - InputHeight, MsgPosition.width, InputHeight)).Contains(Event.current.mousePosition))
        {
            _closeOnMouseUp = true;
        }

        GUI.color = Color.white;
        GUI.SetNextControlName("input");
        _inputContent = GUI.TextField(pos, _inputContent, _maxMessageLength, TextFieldStyle);
        if (Event.current.type == EventType.Repaint)
            _inputFocused = GUI.GetNameOfFocusedControl() == "input";
        _inputContent = _inputContent.Trim(new char[] { '\n', '\t' });

        DrawInputLineLabel(LocalizedStrings.Chat + ":", Color.black, Color.white);

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return &&
            Event.current.type == EventType.KeyUp)
        {
            if (!_skipEnterUp)
            {
                _canInput = false;
                GUIUtility.keyboardControl = 0;
                Event.current.Use();
                EndChat();
            }
            else
            {
                _skipEnterUp = false;
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

        DrawInputLineLabel(_muteMessage, Color.red, Color.white);

        _muteTimer -= Time.deltaTime;
    }

    private void DoSpamMessage()
    {
        DrawInputLineLabel(_spamMessage, Color.red, Color.white);
    }

    private void DrawInputLineLabel(string text, Color shadow, Color color)
    {
        Rect r = Px(new Rect(0, MsgPosition.height - InputHeight, MsgPosition.width, InputHeight));
        GUI.color = shadow;
        GUI.Label(new Rect(r.x + Scale, r.y + Scale, r.width, r.height), text, MsgStyle);
        GUI.color = color;
        GUI.Label(r, text, MsgStyle);
    }

    public void AddChatMessage(string sender, string message, MemberAccessLevel accessLevel)
    {
        string senderStr = sender + ": ";
        string contentStr = senderStr + message;
        int height = MessageHeight(contentStr);

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
        _skipEnterUp = false;
        _inputContent = string.Empty;
        _chatMsgs.Clear();
    }

    private void CloseInput()
    {
        _paused = false;
        _skipEnterUp = false;
        _closeOnMouseUp = false;

        if (!_canInput) return;

        _canInput = false;
        GUIUtility.keyboardControl = 0;

        if (GameState.HasCurrentGame && GameState.CurrentGame.IsMatchRunning && !GameState.LocalPlayer.IsGamePaused)
            InputManager.Instance.IsInputEnabled = true;
    }

    public void CloseForPanel()
    {
        CloseInput();
        _doFocusOnChat = false;
        _inputFocused = false;
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
        _skipEnterUp = true;
        _closeOnMouseUp = false;
        _inputFocused = true;
        //_enableTime = Time.time;

        InputManager.Instance.IsInputEnabled = false;
    }

    private void EndChat()
    {
        SendChatMessage();

        _chatTimer = 0.1f;

        if (_paused)
        {
            _paused = false;

            // dead: the death screen unlocked the cursor, keep it so the Respawn button stays
            if (GameState.HasCurrentGame && GameState.CurrentGame.IsMatchRunning &&
                GameState.HasCurrentPlayer && GameState.LocalCharacter.IsAlive)
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
        }

        if (remove) _chatMsgs.RemoveRange(index, _chatMsgs.Count - index);
    }

    private float Scale
    {
        get { return Mathf.Max(1f, Screen.height / 1080f) * 1.2f; }
    }

    // Chat is drawn at screen resolution with a dynamic font sized by Scale, instead of a 17 px bitmap font stretched by GUI.matrix.
    private Rect Px(Rect r)
    {
        float s = Scale;
        return new Rect(r.x * s, r.y * s, r.width * s, r.height * s);
    }

    private GUIStyle MsgStyle
    {
        get
        {
            EnsureStyles();
            return _msgStyleCache;
        }
    }

    private GUIStyle TextFieldStyle
    {
        get
        {
            EnsureStyles();
            if (_textFieldStyleCache == null || _textFieldStyleSource != _textFieldStyle)
            {
                _textFieldStyleSource = _textFieldStyle;
                _textFieldStyleCache = ScaledStyle(_textFieldStyle, _styleScale);
            }
            return _textFieldStyleCache;
        }
    }

    private void EnsureStyles()
    {
        float scale = Scale;
        if (_msgStyleCache != null && scale == _styleScale) return;

        _styleScale = scale;
        _msgStyleCache = ScaledStyle(BlueStonez.label_ingamechat, scale);
        _textFieldStyleCache = null;

        foreach (var msg in _chatMsgs)
        {
            msg.Height = MessageHeight(msg.Sender + ": " + msg.Content);
            msg.Position.height = msg.Height;
        }
        UpdateMessagePosition();
    }

    private GUIStyle ScaledStyle(GUIStyle source, float scale)
    {
        if (_chatFont == null) _chatFont = Resources.Load<Font>("InterparkGothicBold_Dynamic");

        var style = new GUIStyle(source);
        int baseSize = source.fontSize > 0 ? source.fontSize : (source.font != null ? source.font.fontSize : 17);
        if (_chatFont != null && source.font != null && source.font.name.StartsWith("InterparkGothicBold")) style.font = _chatFont;
        style.fontSize = Mathf.RoundToInt(baseSize * scale);
        style.padding = ScaledOffset(source.padding, scale);
        style.margin = ScaledOffset(source.margin, scale);
        style.overflow = ScaledOffset(source.overflow, scale);
        style.contentOffset = source.contentOffset * scale;
        style.fixedWidth = source.fixedWidth * scale;
        style.fixedHeight = source.fixedHeight * scale;
        return style;
    }

    private static RectOffset ScaledOffset(RectOffset o, float scale)
    {
        return new RectOffset(Mathf.RoundToInt(o.left * scale), Mathf.RoundToInt(o.right * scale), Mathf.RoundToInt(o.top * scale), Mathf.RoundToInt(o.bottom * scale));
    }

    // Height in layout units (before Scale), as the message list is laid out unscaled.
    private int MessageHeight(string content)
    {
        MsgStyle.wordWrap = true;
        float pixels = MsgStyle.CalcHeight(new GUIContent(content), MsgPosition.width * _styleScale);
        MsgStyle.wordWrap = false;
        return Mathf.CeilToInt(pixels / _styleScale);
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
    private GUIStyle _textFieldStyleCache;
    private GUIStyle _textFieldStyleSource;
    private float _styleScale;
    private Font _chatFont;
    private bool _paused;
    private bool _doFocusOnChat;
    private bool _skipEnterUp;
    private bool _closeOnMouseUp;
    private bool _inputFocused;
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