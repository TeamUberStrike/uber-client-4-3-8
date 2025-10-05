using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client.Network;
using Cmune.Util;
using UnityEngine;

public class ModerationPanelGUI : PanelGuiBase
{
    private void Awake()
    {
        _moderations = new List<Moderation>();

        CmuneEventHandler.AddListener<LoginEvent>((ev) => InitModerations(ev.AccessLevel));
    }

    private void OnGUI()
    {
        _rect = new Rect(GUITools.ScreenHalfWidth - 320, GUITools.ScreenHalfHeight - 202, 640, 404);
        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.window_standard_grey38);
        DrawModerationPanel();
        GUI.EndGroup();
    }

    public override void Show()
    {
        base.Show();
        _moderationSelection = 0;
    }

    public override void Hide()
    {
        base.Hide();
        _moderationSelection = 0;
        _filterText = string.Empty;
    }

    public void SetSelectedUser(CommUser user)
    {
        if (user != null)
        {
            _selectedCommUser = user;
            _filterText = user.Name;
        }
    }

    private void InitModerations(MemberAccessLevel level)
    {
        if (level >= MemberAccessLevel.ChatModerator)
        {
            Moderation mod1 = new Moderation(MemberAccessLevel.ChatModerator, Actions.UNMUTE_PLAYER, "Unmute Player",
                "Player is un-muted and un-ghosted immediately", "Unmute player", DrawModeration);
            _moderations.Add(mod1);

            Moderation mod2 = new Moderation(MemberAccessLevel.ChatModerator, Actions.GHOST_PLAYER, "Ghost Player",
                "Chat messages from player only appear in their own chat window, but not the windows of other players.", "Ghost player", DrawModeration,
                new GUIContent[] { new GUIContent("1 min"), new GUIContent("5 min"), new GUIContent("30 min"), new GUIContent("6 hrs") });
            _moderations.Add(mod2);

            Moderation mod3 = new Moderation(MemberAccessLevel.ChatModerator, Actions.MUTE_PLAYER, "Mute Player",
                "Chat messages from player do not appear in anyones chat window.", "Mute player", DrawModeration,
                new GUIContent[] { new GUIContent("1 min"), new GUIContent("5 min"), new GUIContent("30 min"), new GUIContent("6 hrs") });
            _moderations.Add(mod3);
        }

        if (level >= MemberAccessLevel.JuniorModerator)
        {
            Moderation mod4 = new Moderation(MemberAccessLevel.JuniorModerator, Actions.SEND_MESSAGE, "Send Custom Message",
                "Popup a window on a player's screen with a message from an admin.", "Send custom message to player", DrawSendMessage);
            _moderations.Add(mod4);

            Moderation mod5 = new Moderation(MemberAccessLevel.JuniorModerator, Actions.KICK_FROM_GAME, "Kick from Game",
                "Player is removed from the game he is currently in and dumped on the home screen.", "Kick player from game", DrawModeration);
            _moderations.Add(mod5);
        }

        if (level >= MemberAccessLevel.SeniorModerator)
        {
            Moderation mod6 = new Moderation(MemberAccessLevel.SeniorModerator, Actions.KICK_FROM_APP, "Kick from Application",
                "Player is disconnected from all realtime connections for the current session.", "Kick player from application", DrawModeration);
            _moderations.Add(mod6);
        }

        if (level >= MemberAccessLevel.Admin)
        {
            Moderation mod7 = new Moderation(MemberAccessLevel.Admin, Actions.BAN_FROM_CMUNE, "PERMANENT BAN",
                "Player is disconnected from all realtime connections. Player is banned permanently from CMUNE and can't login again", "PERMANENT BAN", DrawModeration);
            _moderations.Add(mod7);
        }
    }

    private void DrawModerationPanel()
    {
        GUI.skin = BlueStonez.Skin;
        GUI.depth = (int)GuiDepth.Panel;

        GUI.Label(new Rect(0, 0, _rect.width, 56), "MODERATION DASHBOARD", BlueStonez.tab_strip);

        DoModerationDashboard(new Rect(10, 55, _rect.width - 20, _rect.height - 55 - 52));

        if (PlayerDataManager.AccessLevel >= MemberAccessLevel.SeniorModerator)
        {
            GUI.enabled = _nextUpdate < Time.time;
            if (GUITools.Button(new Rect(10, _rect.height - 10 - 32, 150, 32), new GUIContent(_nextUpdate < Time.time ? "GET ALL PLAYERS" : string.Format("Next Update ({0:N0})", _nextUpdate - Time.time)), BlueStonez.buttondark_medium))
            {
                ChatPageGUI.IsCompleteLobbyLoaded = true;
                _selectedCommUser = null;
                _filterText = string.Empty;
                _nextUpdate = Time.time + 10;
                CommConnectionManager.CommCenter.SendUpdateAllPlayers();
            }
        }

        GUI.enabled = (_selectedCommUser != null && _moderationSelection != 0);
        if (GUITools.Button(new Rect(_rect.width - 120 - 140, _rect.height - 10 - 32, 140, 32), new GUIContent("APPLY ACTION!"), GUI.enabled ? BlueStonez.button_red : BlueStonez.button))
        {
            ApplyModeration();
        }

        GUI.enabled = true;
        if (GUITools.Button(new Rect(_rect.width - 10 - 100, _rect.height - 10 - 32, 100, 32), new GUIContent("CLOSE"), BlueStonez.button))
        {
            PanelManager.Instance.ClosePanel(PanelType.Moderation);
        }
    }

    private void DoModerationDashboard(Rect position)
    {
        GUI.BeginGroup(position, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            float width = 200;

            DoPlayerModeration(new Rect(20 + width, 10, position.width - 30 - width, position.height - 20));
            DoPlayerSelection(new Rect(10, 10, width, position.height - 20));
        }
        GUI.EndGroup();
    }

    private void DoPlayerSelection(Rect position)
    {
        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(0, 0, position.width, 18), "SELECT PLAYER", BlueStonez.label_interparkbold_18pt_left);

            bool hasSearchString = !string.IsNullOrEmpty(_filterText);

            GUI.SetNextControlName("Filter");
            _filterText = GUI.TextField(new Rect(0, 26, hasSearchString ? position.width - 26 : position.width, 24), _filterText, 20, BlueStonez.textField);

            if (!hasSearchString && GUI.GetNameOfFocusedControl() != "Filter")
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                if (GUI.Button(new Rect(7, 32, position.width, 24), "Enter player name", BlueStonez.label_interparkmed_11pt_left))
                    GUI.FocusControl("Filter");
                GUI.color = Color.white;
            }

            if (hasSearchString && GUI.Button(new Rect(position.width - 24, 26, 24, 24), "x", BlueStonez.panelquad_button))
            {
                _filterText = string.Empty;
                GUIUtility.keyboardControl = 0;
            }

            string online = string.Format("PLAYERS ONLINE ({0})", _playerCount);

            GUI.Label(new Rect(0, 52, position.width, 25), GUIContent.none, BlueStonez.box_grey50);
            GUI.Label(new Rect(10, 52, position.width, 25), online, BlueStonez.label_interparkbold_18pt_left);
            GUI.Label(new Rect(0, 76, position.width, position.height - 76), GUIContent.none, BlueStonez.box_grey50);

            _playerScroll = GUI.BeginScrollView(new Rect(0, 77, position.width, position.height - 78), _playerScroll, new Rect(0, 0, position.width - 20, _playerCount * 20));
            {
                int i = 0;
                string s = _filterText.ToLower();
                ICollection<CommUser> commUsers = GameState.HasCurrentGame ? ChatManager.Instance.GameUsers : ChatManager.Instance.LobbyUsers;

                foreach (CommUser user in commUsers)
                {
                    if (string.IsNullOrEmpty(s) || user.Name.ToLower().Contains(s))
                    {
                        if ((i & 1) == 0) GUI.Label(new Rect(1, i * 20, position.width - 2, 20), GUIContent.none, BlueStonez.box_grey38);

                        if (_selectedCommUser != null && _selectedCommUser.Cmid == user.Cmid)
                        {
                            GUI.color = new Color(ColorScheme.UberStrikeBlue.r, ColorScheme.UberStrikeBlue.g, ColorScheme.UberStrikeBlue.b, 0.5f);
                            GUI.Label(new Rect(1, i * 20, position.width - 2, 20), GUIContent.none, BlueStonez.box_white);
                            GUI.color = Color.white;
                        }

                        if (GUI.Button(new Rect(10, i * 20, position.width, 20), user.Name, BlueStonez.label_interparkmed_10pt_left))
                        {
                            _selectedCommUser = user;
                        }

                        GUI.color = Color.white;
                        i++;
                    }
                }

                _playerCount = i;
            }
            GUI.EndScrollView();
        }
        GUI.EndGroup();
    }

    private void DoPlayerModeration(Rect position)
    {
        int moderationHeight = _moderations.Count * 100;
        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, BlueStonez.box_grey50);

            _moderationScroll = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), _moderationScroll, new Rect(0, 1, position.width - 20, moderationHeight));
            {
                for (int i = 0, j = 0; i < _moderations.Count; i++)
                {
                    _moderations[i].Draw(_moderations[i], new Rect(10, j++ * 100, 360, 100));
                }
            }
            GUI.EndScrollView();
        }
        GUI.EndGroup();
    }

    private void DrawModeration(Moderation moderation, Rect position)
    {
        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(21, 0, position.width, 30), moderation.Title, BlueStonez.label_interparkbold_13pt);
            GUI.Label(new Rect(0, 30, 356, 40), moderation.Content, BlueStonez.label_itemdescription);
            GUI.Label(new Rect(0, 0, position.width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);

            bool result = GUI.Toggle(new Rect(0, 7, position.width, 16), moderation.Selected, GUIContent.none, BlueStonez.radiobutton);

            if (result && !moderation.Selected)
            {
                moderation.Selected = true;
                SelectModeration(moderation.ID);

                //update 
                switch (moderation.SubSelectionIndex)
                {
                    case 0: _banDurationIndex = 1; break;
                    case 1: _banDurationIndex = 5; break;
                    case 2: _banDurationIndex = 30; break;
                    case 3: _banDurationIndex = 6 * 60; break;
                    default: _banDurationIndex = 1; break;
                }

                GUIUtility.keyboardControl = 0;
            }

            if (moderation.SubSelection != null)
            {
                GUI.enabled = moderation.Selected;
                GUI.changed = false;
                //visualize empty sub-selection for better visiblity if moderator option is disabled
                if (moderation.Selected)
                    moderation.SubSelectionIndex = GUI.SelectionGrid(new Rect(0, position.height - 25, position.width, 20), moderation.SubSelectionIndex, moderation.SubSelection, moderation.SubSelection.Length, BlueStonez.panelquad_toggle);
                else
                    GUI.SelectionGrid(new Rect(0, position.height - 25, position.width, 20), -1, moderation.SubSelection, moderation.SubSelection.Length, BlueStonez.panelquad_toggle);

                if (GUI.changed)
                {
                    switch (moderation.SubSelectionIndex)
                    {
                        case 0: _banDurationIndex = 1; break;
                        case 1: _banDurationIndex = 5; break;
                        case 2: _banDurationIndex = 30; break;
                        case 3: _banDurationIndex = 6 * 60; break;
                        default: _banDurationIndex = 1; break;
                    }
                }
                GUI.enabled = true;
            }
        }
        GUI.EndGroup();
    }

    private void DrawSendMessage(Moderation moderation, Rect position)
    {
        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(21, 0, position.width, 30), moderation.Title, BlueStonez.label_interparkbold_13pt);
            GUI.Label(new Rect(0, 30, 356, 40), moderation.Content, BlueStonez.label_itemdescription);
            GUI.Label(new Rect(0, 0, position.width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);

            GUI.enabled = (_moderationSelection == Actions.SEND_MESSAGE);
            GUI.SetNextControlName("ModCustom");
            _message = GUI.TextField(new Rect(0, 70, 250, 24), _message, BlueStonez.textField);
            GUI.enabled = true;

            bool result = GUI.Toggle(new Rect(0, 7, position.width, 16), moderation.Selected, GUIContent.none, BlueStonez.radiobutton);

            if (result && !moderation.Selected)
            {
                moderation.Selected = true;
                SelectModeration(moderation.ID);

                GUI.FocusControl("ModCustom");
            }
        }
        GUI.EndGroup();
    }

    private void SelectModeration(Actions id)
    {
        _moderationSelection = id;

        for (int i = 0; i < _moderations.Count; i++)
        {
            if (id != _moderations[i].ID) _moderations[i].Selected = false;
        }
    }

    private void ApplyModeration()
    {
        if (PlayerDataManager.AccessLevelSecure > 0 && _moderations.Exists(m => m.ID == _moderationSelection))
        {
            switch (_moderationSelection)
            {
                case Actions.UNMUTE_PLAYER:
                    {
                        if (_selectedCommUser.IsInGame)
                        {
                            ServerRequest.Run(this, _selectedCommUser.CurrentGame.Server, GameApplicationRPC.UnmutePlayer, _selectedCommUser.CurrentGame.Number, _selectedCommUser.ActorId, false);
                        }
                        else
                        {
                            CommConnectionManager.CommCenter.SendGhostPlayer(_selectedCommUser.Cmid, _selectedCommUser.ActorId, 0);
                            CommConnectionManager.CommCenter.SendUnmutePlayer(_selectedCommUser.ActorId);
                        }
                        PopupSystem.ShowMessage("Action Executed", string.Format("The Player '{0}' was unmuted.", _selectedCommUser.Name));
                    } break;

                case Actions.GHOST_PLAYER:
                    {
                        if (_selectedCommUser.IsInGame)
                        {
                        }
                        else
                        {
                            CommConnectionManager.CommCenter.SendGhostPlayer(_selectedCommUser.Cmid, _selectedCommUser.ActorId, _banDurationIndex);
                        }
                        PopupSystem.ShowMessage("Action Executed", string.Format("The Player '{0}' was ghosted for {1} minutes.", _selectedCommUser.Name, _banDurationIndex));
                    } break;

                case Actions.MUTE_PLAYER:
                    {
                        if (_selectedCommUser.IsInGame)
                        {
                            ServerRequest.Run(this, _selectedCommUser.CurrentGame.Server, GameApplicationRPC.MutePlayer, _selectedCommUser.CurrentGame.Number, _selectedCommUser.ActorId, true);
                        }
                        else
                        {
                            CommConnectionManager.CommCenter.SendMutePlayer(_selectedCommUser.Cmid, _selectedCommUser.ActorId, _banDurationIndex);
                        }
                        PopupSystem.ShowMessage("Action Executed", string.Format("The Player '{0}' was muted for {1} minutes.", _selectedCommUser.Name, _banDurationIndex));
                    } break;

                case Actions.SEND_MESSAGE:
                    {
                        if (_selectedCommUser.IsInGame)
                        {
                            ServerRequest.Run(this, _selectedCommUser.CurrentGame.Server, GameApplicationRPC.CustomMessage, _selectedCommUser.CurrentGame.Number, _selectedCommUser.ActorId, _message);
                        }
                        else
                        {
                            CommConnectionManager.CommCenter.SendCustomMessage(_selectedCommUser.ActorId, _message);
                        }

                        PopupSystem.ShowMessage("Action Executed", string.Format("The Message was sent to Player '{0}'", _selectedCommUser.Name));
                        _message = string.Empty;
                    } break;

                case Actions.KICK_FROM_GAME:
                    if (_selectedCommUser.IsInGame)
                    {
                        int duration = 0;
                        int cmid = _selectedCommUser.Cmid;
                        int roomNumber = _selectedCommUser.CurrentGame.Number;
                        string server = _selectedCommUser.CurrentGame.Server;

                        ServerRequest.Run(this, server, GameApplicationRPC.BanPlayer, cmid, roomNumber, duration);
                        //CommConnectionManager.CommCenter.SendKickFromGame(_selectedCommUser.ActorId);
                        PopupSystem.ShowMessage("Action Executed", string.Format("The Player '{0}' was kicked out of his current game!", _selectedCommUser.Name));
                    }
                    else
                    {
                        PopupSystem.ShowMessage("Warning", string.Format("The Player '{0}' is currently not in a game!", _selectedCommUser.Name));
                    }
                    break;

                case Actions.KICK_FROM_APP:
                    {
                        CommConnectionManager.CommCenter.SendModerationBanPlayer(_selectedCommUser.Cmid);
                        PopupSystem.ShowMessage("Action Executed", string.Format("The Player '{0}' was disconnected from all servers!", _selectedCommUser.Name));
                    } break;

                case Actions.BAN_FROM_CMUNE:
                    {
                        CommConnectionManager.CommCenter.SendModerationBanFromCmune(_selectedCommUser.Cmid);
                        PopupSystem.ShowMessage("Action Executed", string.Format("The Player '{0}' was banned from CMUNE!", _selectedCommUser.Name));
                    } break;
            }

            _moderationSelection = Actions.NONE;
            //_selectedUserInfo = null;
            foreach (var v in _moderations)
            {
                v.Selected = false;
            }
        }
    }

    #region Fields

    private float _nextUpdate = 0;
    //private CommActorInfo _selectedUserInfo;
    private CommUser _selectedCommUser;
    private Vector2 _playerScroll = Vector2.zero;
    private Vector2 _moderationScroll = Vector2.zero;
    private Rect _rect;

    private List<Moderation> _moderations;

    private string _message = string.Empty;
    private string _filterText = string.Empty;

    private int _banDurationIndex = 1;
    private Actions _moderationSelection;
    private int _playerCount;

    private enum Actions
    {
        NONE = 0,
        UNMUTE_PLAYER = 1,
        GHOST_PLAYER = 2,
        MUTE_PLAYER = 3,
        SEND_MESSAGE = 4,
        KICK_FROM_GAME = 5,
        KICK_FROM_APP = 6,
        BAN_FROM_CMUNE = 7,
    }

    #endregion

    private class Moderation
    {
        public Moderation(MemberAccessLevel level, Actions id, string title, string context, string option, Action<Moderation, Rect> draw)
            : this(level, id, title, context, option, draw, null)
        { }

        public Moderation(MemberAccessLevel level, Actions id, string title, string context, string option, Action<Moderation, Rect> draw, GUIContent[] subselection)
        {
            Level = level;
            ID = id;
            Title = title;
            Content = context;
            Draw = draw;
            SubSelection = subselection;
        }

        public MemberAccessLevel Level { get; private set; }
        public Actions ID { get; private set; }
        public string Title { get; private set; }
        public string Content { get; private set; }
        public string Option { get; private set; }
        public Action<Moderation, Rect> Draw { get; private set; }
        public GUIContent[] SubSelection { get; private set; }

        public int SubSelectionIndex { get; set; }
        public bool Selected { get; set; }

    }
}