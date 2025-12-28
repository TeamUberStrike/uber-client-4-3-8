using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using UberStrike.Helper;
using UberStrike.Realtime.Common;
using UnityEngine;
using System.Collections.Generic;

public class ChatPageGUI : PageGUI
{
    private Rect _mainRect;
    private Vector2 _dialogScroll;

    private const float TitleHeight = 24;

    private int _lastMessageCount = 0;

    private void Awake()
    {
        _playerMenu = new PopupMenu();
        IsOnGUIEnabled = true;
    }

    private void Start()
    {
        _playerMenu.AddMenuItem(LocalizedStrings.AddAsFriend, MenuCmdAddFriend, MenuChkAddFriend);
        _playerMenu.AddMenuItem(LocalizedStrings.PrivateChat, MenuCmdChat, MenuChkChat);
        _playerMenu.AddMenuItem(LocalizedStrings.SendMessage, MenuCmdSendMessage, MenuChkSendMessage);
        _playerMenu.AddMenuItem(LocalizedStrings.JoinGame, MenuCmdJoinGame, MenuChkJoinGame);
        _playerMenu.AddMenuItem(LocalizedStrings.InviteToClan, MenuCmdInviteClan, MenuChkInviteClan);

        if (PlayerDataManager.AccessLevelSecure >= MemberAccessLevel.SeniorModerator)
        {
            _playerMenu.AddMenuItem("MODERATE", MenuCmdModeratePlayer, delegate { return true; });
        }
    }

    private void Update()
    {
        if (_lastMessageSentTimer < 0.3f)
            _lastMessageSentTimer += Time.deltaTime;

        if (_yPosition < 0)
            _yPosition = Mathf.Lerp(_yPosition, 0.1f, Time.deltaTime * 8.0f);
        else
            _yPosition = 0;
    }

    private void OnGUI()
    {
        if (IsOnGUIEnabled)
        {
            GUI.skin = BlueStonez.Skin;
            GUI.depth = (int)GuiDepth.Chat;

            _mainRect = new Rect(0, GlobalUIRibbon.Instance.GetHeight(), Screen.width, Screen.height - GlobalUIRibbon.Instance.GetHeight());

            DrawGUI(_mainRect);

            if (PopupMenu.Current != null)
                PopupMenu.Current.Draw();
        }
    }

    public override void DrawGUI(Rect rect)
    {
        GUI.BeginGroup(rect, BlueStonez.window);
        {
            if (CommConnectionManager.Client.ConnectionState == Cmune.Realtime.Photon.Client.PhotonClient.ConnectionStatus.RUNNING)
            {
                DoTabs(new Rect(2, 0, TAB_WIDTH, 22));

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                    GUIUtility.keyboardControl = 0;

                Rect paneRect = new Rect(0, 21, TAB_WIDTH, rect.height - 21);

                Rect headerRect = new Rect(TAB_WIDTH - 1, 0, rect.width - TAB_WIDTH, 22);
                Rect dialogRect = new Rect(TAB_WIDTH, 22, rect.width - TAB_WIDTH, rect.height - 22 - 36);
                Rect footerRect = new Rect(TAB_WIDTH - 1, rect.height - 37, rect.width - TAB_WIDTH + 1, 37);

                ChatGroupPanel pane = ChatManager.Instance._commPanes[(int)SelectedTab];

                switch (SelectedTab)
                {
                    case TabArea.Lobby:
                        {
                            DoDialogFooter(footerRect, pane, ChatManager.Instance.LobbyDialog);
                            DoLobbyCommPane(paneRect, pane);
                            DoDialogHeader(headerRect, ChatManager.Instance.LobbyDialog);
                            DoDialog(dialogRect, pane, ChatManager.Instance.LobbyDialog);
                            break;
                        }
                    case TabArea.Private:
                        {
                            DoDialogFooter(footerRect, pane, ChatManager.Instance._selectedDialog);
                            DrawCommPane(paneRect, pane);
                            DoPrivateDialogHeader(headerRect, ChatManager.Instance._selectedDialog);
                            DoDialog(dialogRect, pane, ChatManager.Instance._selectedDialog);
                            break;
                        }
                    case TabArea.Clan:
                        {
                            DoDialogFooter(footerRect, pane, ChatManager.Instance.ClanDialog);
                            DrawCommPane(paneRect, pane);
                            DoDialogHeader(headerRect, ChatManager.Instance.ClanDialog);
                            DoDialog(dialogRect, pane, ChatManager.Instance.ClanDialog);
                            break;
                        }
                    case TabArea.InGame:
                        {
                            DoDialogFooter(footerRect, pane, ChatManager.Instance.InGameDialog);
                            DrawCommPane(paneRect, pane);
                            DoDialogHeader(headerRect, ChatManager.Instance.InGameDialog);
                            DoDialog(dialogRect, pane, ChatManager.Instance.InGameDialog);
                            break;
                        }
                    case TabArea.Moderation:
                        {
                            DoModeratorPaneFooter(footerRect, pane);
                            DrawModeratorCommPane(paneRect, pane);
                            DoDialogHeader(headerRect, ChatManager.Instance.ModerationDialog);
                            DoModeratorDialog(dialogRect, pane);
                            break;
                        }
                }
            }
            else
            {
                GUI.color = Color.gray;
                switch (CommConnectionManager.Client.ConnectionState)
                {
                    case Cmune.Realtime.Photon.Client.PhotonClient.ConnectionStatus.STOPPED:
                        GUI.Label(new Rect(0, rect.height / 2, rect.width, 20), LocalizedStrings.ServerIsNotReachable, BlueStonez.label_interparkbold_11pt);
                        break;
                    default:
                        GUI.Label(new Rect(0, rect.height / 2, rect.width, 20), LocalizedStrings.ConnectingToServer, BlueStonez.label_interparkbold_11pt);
                        break;
                }
                GUI.color = Color.white;
            }
        }
        GUI.EndGroup();

        if (_checkForPassword)
            PasswordCheck(new Rect((Screen.width - 280) * 0.5f, (Screen.height - 200) * 0.5f, 280, 200));

        GuiManager.DrawTooltip();
    }

    private void PasswordCheck(Rect position)
    {
        if (_game == null) return;

        GUITools.PushGUIState();
        GUI.BeginGroup(position, GUIContent.none, BlueStonez.window);
        {
            GUI.Label(new Rect(0, 0, position.width, 56), LocalizedStrings.EnterPassword, BlueStonez.tab_strip);

            GUI.Box(new Rect(16, 55, position.width - 32, position.height - 56 - 64), GUIContent.none, BlueStonez.window_standard_grey38);

            GUI.SetNextControlName("@EnterPassword");
            Rect nameRect = new Rect((position.width - 188) / 2, 56 + 24, 188, 24);

            _inputedPassword = GUI.PasswordField(nameRect, _inputedPassword, '*', 18, BlueStonez.textField);
            _inputedPassword = _inputedPassword.Trim(new char[] { '\n' });

            if (string.IsNullOrEmpty(_inputedPassword) && GUI.GetNameOfFocusedControl() != "@EnterPassword")
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(nameRect, LocalizedStrings.TypePasswordHere, BlueStonez.label_interparkmed_11pt);
                GUI.color = Color.white;
            }
            GUI.enabled = Time.time > _nextPasswordCheck;

            if (GUITools.Button(new Rect((position.width - 100 - 10), 200 - 32 - 16, 100, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button) ||
                (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.Layout && Time.time > _nextPasswordCheck))
            {
                if (_inputedPassword == _game.Password)
                {
                    _checkForPassword = false;
                    _inputedPassword = string.Empty;
                    _isPasswordOk = true;

                    GameLoader.Instance.JoinGame(_game);

                    _game = null;
                }
                else
                {
                    _inputedPassword = string.Empty;
                    _isPasswordOk = false;
                    _nextPasswordCheck = Time.time + CHECK_PASSWORD_DELAY;
                }
            }
            GUI.enabled = true;
            if (GUITools.Button(new Rect(10, 200 - 32 - 16, 100, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                _isPasswordOk = true;
                _checkForPassword = false;
                _inputedPassword = string.Empty;
                _game = null;
            }

            if (!_isPasswordOk && string.IsNullOrEmpty(_inputedPassword))
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(((position.width - 188) / 2), 56 + 54, 188, 24), LocalizedStrings.PasswordIncorrect, BlueStonez.label_interparkbold_11pt);
                GUI.color = Color.white;
            }
        }
        GUI.EndGroup();
        GUITools.PopGUIState();
    }

    public void JoinRoom(CmuneRoomID roomId)
    {
        GameConnectionManager.RequestRoomMetaData(roomId, OnRequestRoomMetaData);
    }

    private void OnRequestRoomMetaData(int returncode, GameMetaData data)
    {
        MemberAccessLevel accessLevel = PlayerDataManager.AccessLevelSecure;
        switch (returncode)
        {
            case 0:
                {
                    if (data.ConnectedPlayers == data.MaxPlayers && accessLevel != MemberAccessLevel.Admin)
                    {
                        PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ThisGameIsFull, PopupSystem.AlertType.OK, null);
                    }
                    else
                    {
                        if (data.IsPublic || accessLevel == MemberAccessLevel.Admin)
                        {
                            if (data.IsLevelAllowed(PlayerDataManager.PlayerLevelSecure) || accessLevel == MemberAccessLevel.Admin)
                            {
                                GameLoader.Instance.JoinGame(data);

                                _game = null;
                                _checkForPassword = false;
                            }
                            else
                            {
                                PopupSystem.ShowMessage(LocalizedStrings.Error, string.Format(LocalizedStrings.YouHaveToReachLevelNToJoinThisGame, data.LevelMin));
                            }
                        }
                        else
                        {
                            _game = data;
                            _checkForPassword = true;
                        }
                    }
                } break;

            case 1:
                {
                    PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ThisGameNoLongerExists, PopupSystem.AlertType.OK, null);
                } break;
            case 2:
                {
                    PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ServerIsNotReachable, PopupSystem.AlertType.OK, null);
                } break;
        }
    }

    private int DoModeratorControlPanel(Rect rect, ChatGroupPanel pane)
    {
        if (PlayerDataManager.AccessLevel >= MemberAccessLevel.JuniorModerator)
        {
            int offset = 0;

            bool drawListUpdate = PlayerDataManager.AccessLevel >= MemberAccessLevel.JuniorModerator;
            bool drawListReset = drawListUpdate && ChatPageGUI.IsCompleteLobbyLoaded;

            rect = new Rect(rect.x, rect.yMax - SEARCH_HEIGHT - (drawListReset ? 60 : drawListUpdate ? 30 : 0) - 1, rect.width, 1 + SEARCH_HEIGHT + (drawListReset ? 60 : drawListUpdate ? 30 : 0));
            GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
            {
                if (drawListUpdate)
                {
                    GUI.enabled = _nextNaughtyListUpdate < Time.time;
                    if (GUITools.Button(new Rect(6, rect.height - 61, (rect.width - 12) * 0.5f, 26), new GUIContent(_nextNaughtyListUpdate < Time.time ? "Update Naughty List" : string.Format("Next Update ({0:N0})", _nextNaughtyListUpdate - Time.time)), BlueStonez.buttondark_medium))
                    {
                        _nextNaughtyListUpdate = Time.time + 10;
                        CommConnectionManager.CommCenter.UpdateActorsForModeration();
                    }
                    GUI.enabled = true;
                    GUI.enabled = _nextNaughtyListUpdate < Time.time;
                    if (GUITools.Button(new Rect(6 + (rect.width - 12) * 0.5f, rect.height - 61, (rect.width - 12) * 0.5f, 26), new GUIContent(_nextNaughtyListUpdate < Time.time ? "Unban Next 50" : string.Format("Next Update ({0:N0})", _nextNaughtyListUpdate - Time.time)), BlueStonez.buttondark_medium))
                    {
                        var list = new List<CommUser>(ChatManager.Instance.NaughtyUsers);
                        int count = 0;
                        foreach (var user in list)
                        {
                            if (user.Name.StartsWith("Banned:"))
                            {
                                CommConnectionManager.CommCenter.SendClearAllFlags(user.Cmid);
                                ChatManager.Instance._selectedCmid = 0;
                                ChatManager.Instance._modUsers.Remove(user.Cmid);

                                if (++count > 50) break;
                            }
                        }
                    }
                    GUI.enabled = true;

                    offset += ChatPageGUI.IsCompleteLobbyLoaded ? 60 : 30;
                }

                bool hasSearchString = !string.IsNullOrEmpty(pane.SearchText);

                GUI.SetNextControlName("@ModSearch");

                GUI.changed = false;
                pane.SearchText = GUI.TextField(new Rect(6, rect.height - 30, rect.width - (hasSearchString ? 37 : 12), SEARCH_HEIGHT - 12), pane.SearchText, 20, BlueStonez.textField);

                if (!hasSearchString && GUI.GetNameOfFocusedControl() != "@ModSearch")
                {
                    GUI.color = new Color(1, 1, 1, 0.3f);
                    GUI.Label(new Rect(12, rect.height - 30, rect.width - 20, SEARCH_HEIGHT - 12), LocalizedStrings.Search, BlueStonez.label_interparkmed_10pt_left); //"Search"
                    GUI.color = Color.white;
                }

                if (hasSearchString && GUITools.Button(new Rect(rect.width - 28, rect.height - 30, 22, 22), new GUIContent("x"), BlueStonez.panelquad_button))
                {
                    pane.SearchText = string.Empty;
                    GUIUtility.keyboardControl = 0;
                }

                pane.SearchText = TextUtilities.Trim(pane.SearchText);
            }
            GUI.EndGroup();

            offset += SEARCH_HEIGHT;

            return offset;
        }
        else
        {
            return 0;
        }
    }

    public void DrawCommPane(Rect rect, ChatGroupPanel pane)
    {
        GUI.BeginGroup(rect);
        {
            bool tmp = GUI.enabled;
            GUI.enabled &= (!PopupMenu.IsEnabled && !_checkForPassword);

            float windowHeight = rect.height;
            float contentHeight = Mathf.Max(windowHeight, pane.ContentHeight);
            float y = 0;
            pane.Scroll = GUITools.BeginScrollView(new Rect(0, 0, rect.width, windowHeight), pane.Scroll, new Rect(0, 0, rect.width - 17, contentHeight), false, true);
            {
                GUI.BeginGroup(new Rect(0, 0, rect.width, windowHeight + pane.Scroll.y));
                {
                    foreach (ChatGroup g in pane.Groups)
                    {
                        y += DrawPlayerGroup(g, y, rect.width - 17, pane.SearchText.ToLower());
                    }
                }
                GUI.EndGroup();
            }
            GUITools.EndScrollView();
            pane.ContentHeight = y;

            GUI.enabled = tmp;
        }
        GUI.EndGroup();
    }

    private void DoLobbyCommPane(Rect rect, ChatGroupPanel pane)
    {
        GUI.BeginGroup(rect);
        {
            bool tmp = GUI.enabled;
            GUI.enabled &= (!PopupMenu.IsEnabled && !_checkForPassword);

            int controlHeight = DoLobbyControlPanel(new Rect(0, 0, rect.width, rect.height), pane);

            float windowHeight = rect.height - controlHeight;
            float contentHeight = Mathf.Max(windowHeight, pane.ContentHeight);
            float y = 0;
            pane.Scroll = GUITools.BeginScrollView(new Rect(0, 0, rect.width, windowHeight), pane.Scroll, new Rect(0, 0, rect.width - 17, contentHeight), false, true);
            {
                GUI.BeginGroup(new Rect(0, 0, rect.width, windowHeight + pane.Scroll.y));
                {
                    foreach (ChatGroup g in pane.Groups)
                    {
                        y += DrawPlayerGroup(g, y, rect.width - 17, pane.SearchText.ToLower());
                    }
                }
                GUI.EndGroup();
            }
            GUITools.EndScrollView();
            pane.ContentHeight = y;

            GUI.enabled = tmp;
        }
        GUI.EndGroup();
    }

    private void DrawModeratorCommPane(Rect rect, ChatGroupPanel pane)
    {
        GUI.BeginGroup(rect);
        {
            bool tmp = GUI.enabled;
            GUI.enabled &= (PopupMenu.Current == null && !_checkForPassword);

            int controlHeight = 0;
            controlHeight = DoModeratorControlPanel(new Rect(0, 0, rect.width, rect.height), pane);

            float windowHeight = rect.height - controlHeight;
            float contentHeight = Mathf.Max(windowHeight, pane.ContentHeight);
            float y = 0;
            pane.Scroll = GUITools.BeginScrollView(new Rect(0, 0, rect.width, windowHeight), pane.Scroll, new Rect(0, 0, rect.width - 17, contentHeight), false, true);
            {
                GUI.BeginGroup(new Rect(0, 0, rect.width, windowHeight + pane.Scroll.y));
                {
                    foreach (ChatGroup g in pane.Groups)
                    {
                        y += DrawPlayerGroup(g, y, rect.width - 17, pane.SearchText.ToLower(), true);
                        if (y > pane.Scroll.y + windowHeight) break;
                    }
                }
                GUI.EndGroup();
            }
            GUITools.EndScrollView();
            pane.ContentHeight = y;

            GUI.enabled = tmp;
        }
        GUI.EndGroup();
    }

    private int DoLobbyControlPanel(Rect rect, ChatGroupPanel pane)
    {
        int offset = 0;

        bool drawListUpdate = PlayerDataManager.AccessLevel >= MemberAccessLevel.JuniorModerator;
        bool drawListReset = drawListUpdate && ChatPageGUI.IsCompleteLobbyLoaded;

        rect = new Rect(rect.x, rect.yMax - SEARCH_HEIGHT - (drawListReset ? 60 : drawListUpdate ? 30 : 0) - 1, rect.width, 1 + SEARCH_HEIGHT + (drawListReset ? 60 : drawListUpdate ? 30 : 0));
        GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            if (drawListUpdate)
            {
                GUI.enabled = _nextFullLobbyUpdate < Time.time;
                if (drawListReset && GUITools.Button(new Rect(6, 5, rect.width - 12, 26), new GUIContent("Reset Lobby"), BlueStonez.buttondark_medium))
                {
                    ChatPageGUI.IsCompleteLobbyLoaded = false;
                    _nextFullLobbyUpdate = Time.time + 10;
                    CommConnectionManager.CommCenter.SendUpdateResetLobby();
                }
                if (GUITools.Button(new Rect(6, rect.height - 61, rect.width - 12, 26), new GUIContent(_nextFullLobbyUpdate < Time.time ? "Get All Players " : string.Format("Next Update ({0:N0})", _nextFullLobbyUpdate - Time.time)), BlueStonez.buttondark_medium))
                {
                    ChatPageGUI.IsCompleteLobbyLoaded = true;
                    _nextFullLobbyUpdate = Time.time + 10;
                    CommConnectionManager.CommCenter.SendUpdateAllPlayers();
                }
                GUI.enabled = true;

                offset += ChatPageGUI.IsCompleteLobbyLoaded ? 60 : 30;
            }

            bool hasSearchString = !string.IsNullOrEmpty(pane.SearchText);

            GUI.SetNextControlName("@LobbySearch");

            GUI.changed = false;
            pane.SearchText = GUI.TextField(new Rect(6, rect.height - 30, rect.width - (hasSearchString ? 37 : 12), SEARCH_HEIGHT - 12), pane.SearchText, 20, BlueStonez.textField);

            if (!hasSearchString && GUI.GetNameOfFocusedControl() != "@LobbySearch")
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(new Rect(12, rect.height - 30, rect.width - 20, SEARCH_HEIGHT - 12), LocalizedStrings.Search, BlueStonez.label_interparkmed_10pt_left); //"Search"
                GUI.color = Color.white;
            }

            if (hasSearchString && GUITools.Button(new Rect(rect.width - 28, rect.height - 30, 22, 22), new GUIContent("x"), BlueStonez.panelquad_button))
            {
                pane.SearchText = string.Empty;
                GUIUtility.keyboardControl = 0;
            }

            pane.SearchText = TextUtilities.Trim(pane.SearchText);
        }
        GUI.EndGroup();

        offset += SEARCH_HEIGHT;

        return offset;
    }

    public float DrawPlayerGroup(ChatGroup group, float vOffset, float width, string search, bool allowSelfSelection = false)
    {
        Rect rect = new Rect(0, vOffset, width, TitleHeight);
        GUITools.PushGUIState();
        GUI.enabled &= !PopupMenu.IsEnabled;

        GUI.Label(rect, GUIContent.none, BlueStonez.window_standard_grey38);
        if (group.Players != null)
        {
            GUI.Label(rect, group.Title + " (" + group.Players.Count + ")", BlueStonez.label_interparkbold_11pt);
        }
        GUITools.PopGUIState();

        vOffset += TitleHeight;

        //do players
        int i = 0;
        if (group.Players != null)
        {
            GUITools.PushGUIState();
            GUI.enabled &= !PopupMenu.IsEnabled;
            //this group makes sure to be able to collape and expand each group
            GUI.BeginGroup(new Rect(0, vOffset, width, group.Players.Count * CHAT_USER_HEIGHT));
            {
                foreach (var u in group.Players)
                {
                    if (string.IsNullOrEmpty(search) || u.Name.ToLower().Contains(search))
                        GroupDrawUser((i++) * CHAT_USER_HEIGHT, width, u, allowSelfSelection);
                }
            }
            GUI.EndGroup();
            GUITools.PopGUIState();
        }

        return TitleHeight + group.Players.Count * CHAT_USER_HEIGHT;
    }

    private void DoTabs(Rect rect)
    {
        float width = Mathf.Floor(rect.width / ChatManager.Instance.TabCounter);
        bool contextChanged = false;
        bool toggle = false;
        int position = 0;

        toggle = GUI.Toggle(new Rect(rect.x + position * width, rect.y, width, rect.height), SelectedTab == TabArea.Lobby, LocalizedStrings.Lobby, BlueStonez.tab_medium);
        if (toggle && SelectedTab != TabArea.Lobby)
        {
            SelectedTab = TabArea.Lobby;
            contextChanged = true;
        }
        position++;

        toggle = GUI.Toggle(new Rect(rect.x + position * width, rect.y, width, rect.height), SelectedTab == TabArea.Private, LocalizedStrings.Private, BlueStonez.tab_medium);
        if (toggle && SelectedTab != TabArea.Private)
        {
            SelectedTab = TabArea.Private;
            contextChanged = true;

            ChatManager.Instance.MarkPrivateMessageTab = false;
        }

        if (ChatManager.Instance.MarkPrivateMessageTab)
            GUI.DrawTexture(new Rect(rect.x + position * width, rect.y + 1, 18, 18), UberstrikeIcons.NewMessageIcon);

        position++;

        if (ChatManager.Instance.ShowTab(TabArea.Clan))
        {
            toggle = GUI.Toggle(new Rect(rect.x + position * width, rect.y, width, rect.height), SelectedTab == TabArea.Clan, LocalizedStrings.Clan, BlueStonez.tab_medium);
            if (toggle && SelectedTab != TabArea.Clan)
            {
                SelectedTab = TabArea.Clan;
                contextChanged = true;

                ChatManager.Instance.MarkClanMessageTab = false;
            }

            if (PlayerDataManager.IsPlayerInClan && ChatManager.Instance.MarkClanMessageTab)
                GUI.DrawTexture(new Rect(rect.x + position * width, rect.y + 1, 18, 18), UberstrikeIcons.NewMessageIcon);

            position++;
        }

        if (ChatManager.Instance.ShowTab(TabArea.InGame))
        {
            toggle = GUI.Toggle(new Rect(rect.x + position * width, rect.y, width, rect.height), SelectedTab == TabArea.InGame, LocalizedStrings.Game, BlueStonez.tab_medium);
            if (toggle && SelectedTab != TabArea.InGame)
            {
                SelectedTab = TabArea.InGame;
                _currentChatMessage = string.Empty;
                contextChanged = true;
            }
            position++;
        }

        if (ChatManager.Instance.ShowTab(TabArea.Moderation))
        {
            toggle = GUI.Toggle(new Rect(rect.x + position * width, rect.y, width, rect.height), SelectedTab == TabArea.Moderation, LocalizedStrings.Admin, BlueStonez.tab_medium);
            if (toggle && SelectedTab != TabArea.Moderation && PlayerDataManager.AccessLevelSecure > 0 && PlayerDataManager.AccessLevelSecure <= MemberAccessLevel.Admin)
            {
                SelectedTab = TabArea.Moderation;
                _currentChatMessage = string.Empty;
                contextChanged = true;
            }
            position++;
        }

        if (contextChanged)
        {
            _currentChatMessage = string.Empty;

            PopupMenu.Hide();
            GUIUtility.keyboardControl = 0;
        }
    }

    private void DoDialog(Rect rect, ChatGroupPanel pane, ChatDialog dialog)
    {
        if (dialog == null) return;

        dialog.CheckSize(rect);

        GUI.BeginGroup(new Rect(rect.x, rect.y + Mathf.Clamp(rect.height - dialog._heightCache, 0, rect.height), rect.width, rect.height));
        {
            int i = 0;
            float y = 0;

            if (_lastMessageCount != dialog._msgQueue.Count)
            {
                if (!Input.GetMouseButton(0))
                    _dialogScroll.y = float.MaxValue;

                _lastMessageCount = dialog._msgQueue.Count;
            }

            _dialogScroll = GUITools.BeginScrollView(new Rect(0, 0, dialog._frameSize.x, dialog._frameSize.y), _dialogScroll, new Rect(0, 0, dialog._contentSize.x, dialog._contentSize.y));
            foreach (InstantMessage msg in dialog._msgQueue)
            {
                if (dialog.CanShow == null || dialog.CanShow(msg.Context))
                {
                    if (i % 2 == 0)
                    {
                        GUI.Label(new Rect(0, y, dialog._contentSize.x - 1, dialog._msgHeight[i]), GUIContent.none, BlueStonez.box_grey38);
                    }

                    GUI.color = GetNameColor(msg);

                    GUI.Label(new Rect(4, y, dialog._contentSize.x - 8, 20), msg.PlayerName + ":", BlueStonez.label_interparkbold_11pt_left);

                    GUI.color = new Color(0.9f, 0.9f, 0.9f);
                    GUI.Label(new Rect(4, y + 20, dialog._contentSize.x - 8, dialog._msgHeight[i] - 20), msg.MessageText, BlueStonez.label_interparkmed_11pt_left);

                    GUI.color = new Color(1, 1, 1, 0.5f);
                    GUI.Label(new Rect(4, y, dialog._contentSize.x - 8, 20), msg.MessageDateTime, BlueStonez.label_interparkmed_10pt_right);

                    GUI.color = Color.white;
                    y += dialog._msgHeight[i];
                    i++;
                }
            }
            GUITools.EndScrollView();

            dialog._heightCache = y;
        }
        GUI.EndGroup();
    }

    private void DoModeratorDialog(Rect rect, ChatGroupPanel pane)
    {
        if (PlayerDataManager.AccessLevel >= MemberAccessLevel.SeniorModerator)
        {
            //Rect rect = new Rect(210, 70, rect.width - 20, rect.height - 10);
            GUI.BeginGroup(rect, GUIContent.none);
            {
                CommUser user;// = _modUsers.Find(p => p.Cmid == SelectedCommPane.SelectedUserCmid);
                if (ChatManager.Instance._modUsers.TryGetValue(ChatManager.Instance._selectedCmid, out user) && user != null)
                {
                    GUI.TextField(new Rect(10, 15, rect.width, 20), "Name: " + user.Name, BlueStonez.label_interparkbold_11pt_left);
                    GUI.TextField(new Rect(10, 37, rect.width, 20), "Cmid: " + user.Cmid, BlueStonez.label_interparkmed_11pt_left);
                    if (PlayerDataManager.AccessLevel == MemberAccessLevel.Admin)
                    {
                        if (Application.isWebPlayer)
                        {
                            GUI.TextField(new Rect(10, 52, rect.width, 20), "http://instrumentation.cmune.com/Members/SeeMember.aspx?cmid=" + user.Cmid, BlueStonez.label_interparkbold_11pt_left);
                        }
                        else
                        {
                            if (GUITools.Button(new Rect(10, 52, 70, 20), new GUIContent("Open Profile"), BlueStonez.label_interparkmed_11pt_url))
                                Application.OpenURL("http://instrumentation.cmune.com/Members/SeeMember.aspx?cmid=" + user.Cmid);
                        }
                    }

                    float width = rect.width - 20;
                    GUI.BeginGroup(new Rect(10, 80, width, rect.height - 70), GUIContent.none, BlueStonez.box_grey50);
                    {
                        if (GUITools.Button(new Rect(5, 5, width - 10, 20), new GUIContent("Clear and Unban"), BlueStonez.buttondark_medium))
                        {
                            CommConnectionManager.CommCenter.SendClearAllFlags(user.Cmid);
                            ChatManager.Instance._selectedCmid = 0;
                            ChatManager.Instance._modUsers.Remove(user.Cmid);
                        }

                        int y = 40;
                        if ((user.ModerationFlag & (byte)CommActorInfo.ModerationTag.Banned) != 0)
                        { GUI.Label(new Rect(8, y, width - 10, 20), "- BANNED", BlueStonez.label_interparkbold_11pt_left); y += 20; }
                        if ((user.ModerationFlag & (byte)CommActorInfo.ModerationTag.Ghosted) != 0)
                        { GUI.Label(new Rect(8, y, width - 10, 20), "- Ghosted", BlueStonez.label_interparkmed_11pt_left); y += 20; }
                        if ((user.ModerationFlag & (byte)CommActorInfo.ModerationTag.Muted) != 0)
                        { GUI.Label(new Rect(8, y, width - 10, 20), "- Muted", BlueStonez.label_interparkmed_11pt_left); y += 20; }
                        if ((user.ModerationFlag & (byte)CommActorInfo.ModerationTag.Speedhacking) != 0)
                        { GUI.Label(new Rect(8, y, width - 10, 20), "- Speed " + user.ModerationInfo, BlueStonez.label_interparkmed_11pt_left); y += 20; }
                        if ((user.ModerationFlag & (byte)CommActorInfo.ModerationTag.Spamming) != 0)
                        { GUI.Label(new Rect(8, y, width - 10, 20), "- Spamming", BlueStonez.label_interparkmed_11pt_left); y += 20; }
                        if ((user.ModerationFlag & (byte)CommActorInfo.ModerationTag.Language) != 0)
                        { GUI.Label(new Rect(8, y, width - 10, 20), "- CrudeLanguage", BlueStonez.label_interparkmed_11pt_left); y += 20; }

                        GUI.Label(new Rect(8, y + 20, width - 10, 100), user.ModerationInfo, BlueStonez.label_interparkmed_11pt_left);
                    }
                    GUI.EndGroup();
                }
                else
                {
                    GUI.Label(new Rect(0, rect.height / 2, rect.width, 20), "No user selected", BlueStonez.label_interparkmed_11pt);
                }
            }
            GUI.EndGroup();
        }
    }

    private void DoDialogHeader(Rect rect, ChatDialog d)
    {
        GUI.Label(rect, GUIContent.none, BlueStonez.window_standard_grey38);

        GUI.Label(rect, d.Title, BlueStonez.label_interparkbold_11pt);
    }

    private void DoPrivateDialogHeader(Rect rect, ChatDialog d)
    {
        GUI.Label(rect, GUIContent.none, BlueStonez.window_standard_grey38);

        if (d != null && d.UserCmid > 0)
        {
            GUI.Label(rect, d.Title, BlueStonez.label_interparkbold_11pt);
            if (GUITools.Button(new Rect(rect.x + rect.width - 20, rect.y + 3, 16, 16), new GUIContent("x"), BlueStonez.panelquad_button))
            {
                ChatManager.Instance.RemoveDialog(d);
            }
        }
        else
        {
            GUI.Label(rect, LocalizedStrings.PrivateChat, BlueStonez.label_interparkbold_11pt);
        }
    }

    private void DoModeratorPaneFooter(Rect rect, ChatGroupPanel pane)
    {
        GUI.BeginGroup(rect, BlueStonez.window_standard_grey38);
        {
            CommUser user;
            //CommConnectionManager.CommCenter.TryGetActorWithCmid(ChatManager.Instance._selectedCmid, out user)
            if (ChatManager.Instance._selectedCmid > 0 && ChatManager.Instance.TryGetLobbyCommUser(ChatManager.Instance._selectedCmid, out user) && user != null)
            {
                if (GUITools.Button(new Rect(5, 6, rect.width - 10, rect.height - 12), new GUIContent("Moderate User"), BlueStonez.buttondark_medium))
                {
                    ModerationPanelGUI gui = PanelManager.Instance.OpenPanel(PanelType.Moderation) as ModerationPanelGUI;
                    if (gui) gui.SetSelectedUser(user);
                }
            }
            else
            {
                if (GUITools.Button(new Rect(5, 6, rect.width - 10, rect.height - 12), new GUIContent("Open Moderator"), BlueStonez.buttondark_medium))
                {
                    PanelManager.Instance.OpenPanel(PanelType.Moderation);
                }
            }
        }
        GUI.EndGroup();
    }

    private void DoDialogFooter(Rect rect, ChatGroupPanel pane, ChatDialog dialog)
    {
        GUI.BeginGroup(rect, BlueStonez.window_standard_grey38);
        {
            GUI.SetNextControlName("@CurrentChatMessage");

            bool tmp = GUI.enabled;
            GUI.enabled &= !ClientCommCenter.IsPlayerMuted && dialog != null && dialog.CanChat;

            if (SelectedTab == TabArea.InGame)
                GUI.enabled &= (GameState.HasCurrentGame && GameState.CurrentGame.IsGameStarted);

            // Chat Input Textfield
            _currentChatMessage = GUI.TextField(new Rect(6, 6, rect.width - 60, rect.height - 12), _currentChatMessage, 140, BlueStonez.textField);
            _currentChatMessage = _currentChatMessage.Trim(new char[] { '\n' });

            if (_spammingNotificationTime > Time.time)
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(15, 6, rect.width - 66, rect.height - 12), LocalizedStrings.DontSpamTheLobbyChat, BlueStonez.label_interparkmed_10pt_left);
                GUI.color = Color.white;
            }
            // Input tip label
            else
            {
                string chatMsg = string.Empty;// "Type a message here";

                if (dialog != null && dialog.UserCmid > 0)// && d.User != null)
                {
                    if (dialog.CanChat)
                        chatMsg = LocalizedStrings.EnterAMessageHere; // "Type a message to " + d.UserName + " here";
                    else
                        chatMsg = dialog.UserName + LocalizedStrings.Offline;
                }
                else
                {
                    chatMsg = LocalizedStrings.EnterAMessageHere;
                }

                if (string.IsNullOrEmpty(_currentChatMessage) && GUI.GetNameOfFocusedControl() != "@CurrentChatMessage")
                {
                    GUI.color = new Color(1, 1, 1, 0.3f);
                    GUI.Label(new Rect(10, 6, rect.width - 66, rect.height - 12), chatMsg, BlueStonez.label_interparkmed_10pt_left);
                    GUI.color = Color.white;
                }
            }

            // If we hit enter, make sure we don't lose focus of the chat input textfield
            if (GUI.GetNameOfFocusedControl() == "@CurrentChatMessage" && Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
            {
                GUI.FocusControl("@CurrentChatMessage");
            }

            if (GUITools.Button(new Rect(rect.width - 51, 6, 45, rect.height - 12), new GUIContent(LocalizedStrings.Send), BlueStonez.buttondark_small))
            {
                if (!string.IsNullOrEmpty(_currentChatMessage))
                {
                    GUIUtility.keyboardControl = 0;
                    SendChatMessage();
                }
            }

            GUI.enabled = tmp;

            // Send the message if player pushes ENTER
            if ((Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return) &&
                !ClientCommCenter.IsPlayerMuted && (_lastMessageSentTimer > 0.29f))
            {
                if (!string.IsNullOrEmpty(_currentChatMessage))
                {
                    GUIUtility.keyboardControl = 0;
                    SendChatMessage();
                }
            }
        }
        GUI.EndGroup();
    }

    public static bool IsChatActive
    {
        get { return GUI.GetNameOfFocusedControl() == "@CurrentChatMessage"; }
    }

    private void GroupDrawUser(float vOffset, float width, CommUser user, bool allowSelfSelection = false)
    {
        int myCmid = PlayerDataManager.Cmid;

        Rect rect = new Rect(3, vOffset, width - 3, CHAT_USER_HEIGHT);

        if (ChatManager.Instance._selectedCmid == user.Cmid)
        {
            GUI.color = new Color(ColorScheme.UberStrikeBlue.r, ColorScheme.UberStrikeBlue.g, ColorScheme.UberStrikeBlue.b, 0.5f);
            GUI.Label(rect, GUIContent.none, BlueStonez.box_white);
            GUI.color = Color.white;
        }

        bool tmp = GUI.enabled;

        // RCLK to show menu
        if (user.Cmid != myCmid && CheckMouseClickIn(rect, 1))
        {
            SelectCommUser(user);

            _playerMenu.Show(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), user);
        }

        // DBLCLK to chat with 
        if (MouseInput.Instance.DoubleClick(rect))
        {
            // user is not me and user is online
            if (user.Cmid != myCmid && user.PresenceIndex != PresenceType.Offline)
            {
                if (SelectedTab != TabArea.Private || ChatManager.Instance._selectedCmid != user.Cmid)
                {
                    SelectCommUser(user);

                    ChatManager.Instance.CreatePrivateChat(user.Cmid);

                    Event.current.Use();
                    //exit here to prevent GUI inconsistencies
                    return;
                }
            }
        }

        //single click
        if ((allowSelfSelection || user.Cmid != myCmid) && CheckMouseClickIn(new Rect(rect.x, rect.y, rect.width - 20, rect.height)))
        {
            SelectCommUser(user);
        }

        // Presence Icon
        GUI.Label(new Rect(10, vOffset + 3, 11.2f, 16), ChatManager.GetPresenceIcon(user.PresenceIndex), GUIStyle.none);

        // Channel Icon
        GUI.Label(new Rect(6 + 20 - 3, vOffset + 3, 16, 16), UberstrikeIcons.GetIconForChannel(user.Channel), GUIStyle.none);

        // Player Name
        GUI.color = ColorScheme.ChatNameOtherUser;
        if (user.Cmid == PlayerDataManager.Cmid)
        {
            GUI.color = ColorScheme.ChatNameCurrentUser;
        }
        else
        {
            if (user.IsFriend || user.IsClanMember) GUI.color = ColorScheme.ChatNameFriendsUser;
            else if (user.AccessLevel == MemberAccessLevel.Admin) GUI.color = ColorScheme.ChatNameAdminUser;
            else if (user.AccessLevel > 0) GUI.color = ColorScheme.ChatNameModeratorUser;
        }
        GUI.Label(new Rect(6 + 20 + 18, vOffset, width - 66, CHAT_USER_HEIGHT), user.Name, BlueStonez.label_interparkmed_10pt_left);

        GUI.color = Color.white;
        if (user.Cmid != myCmid)
        {
            if (GUI.Button(new Rect(rect.width - 17, vOffset + 1, 18, 18), GUIContent.none, BlueStonez.button_context))
            {
                SelectCommUser(user);

                _playerMenu.Show(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), user);
            }
        }

        GUI.Box(rect.ExpandBy(0, -1), GUIContent.none, BlueStonez.dropdown_list);

        //new message icon
        ChatDialog d;
        if (SelectedTab == TabArea.Private &&
            ChatManager.Instance._dialogsByCmid.TryGetValue(user.Cmid, out d) && d != null && d.HasUnreadMessage &&
            d != ChatManager.Instance._selectedDialog)
        {
            GUI.Label(new Rect(rect.width - 50, vOffset, 25, 25), UberstrikeIcons.NewMessageIcon);
        }

        //speedhack icon
        if (PlayerDataManager.AccessLevel > 0)
        {
            int speedhackers = user.ModerationFlag & (byte)(CommActorInfo.ModerationTag.Speedhacking | CommActorInfo.ModerationTag.Banned);
            if (speedhackers == (byte)CommActorInfo.ModerationTag.Speedhacking)
                GUI.Label(new Rect(width - 50, vOffset + 3, 20, 20), _speedhackerIcon);
        }

        GUI.enabled = tmp;
    }

    private void SelectCommUser(CommUser user)
    {
        //Debug.LogError("SelectCommUser " + user.Name + " " + (user.Actor != null));
        ChatManager.Instance._selectedCmid = user.Cmid;

        if (SelectedTab == TabArea.Private)
        {
            ChatDialog d;
            if (ChatManager.Instance._dialogsByCmid.TryGetValue(user.Cmid, out d))
            {
                d.HasUnreadMessage = false;
            }
            else// if (user.PresenceIndex != PresenceType.Offline)
            {
                d = ChatManager.Instance.AddNewDialog(user);
            }

            ChatManager.Instance._selectedDialog = d;
            _currentChatMessage = string.Empty;
        }
    }

    private void SendChatMessage()
    {
        _currentChatMessage = TextUtilities.ShortenText(TextUtilities.Trim(_currentChatMessage), 140, false);

        switch (SelectedTab)
        {
            case TabArea.InGame:
                {
                    CommConnectionManager.CommCenter.SendInGameChatMessage(_currentChatMessage, ChatContext.Player);
                } break;
            case TabArea.Lobby:
                {
                    if (!CommConnectionManager.CommCenter.SendLobbyChatMessage(_currentChatMessage))
                    {
                        _spammingNotificationTime = Time.time + 5;
                    }
                } break;
            case TabArea.Clan:
                {
                    string name = PlayerDataManager.NameSecure;
                    int cmid = PlayerDataManager.CmidSecure;
                    foreach (var user in ChatManager.Instance.ClanUsers)
                    {
                        if (user.IsOnline)
                            CommConnectionManager.CommCenter.SendClanChatMessage(cmid, user.ActorId, name, _currentChatMessage);
                    }
                } break;
            case TabArea.Private:
                {
                    CommActorInfo actor;
                    if (ChatManager.Instance._selectedDialog != null && CommConnectionManager.TryGetActor(ChatManager.Instance._selectedDialog.UserCmid, out actor))
                    {
                        CommConnectionManager.CommCenter.SendPrivateChatMessage(actor, _currentChatMessage);
                    }
                } break;
        }

        _lastMessageSentTimer = 0;
        _currentChatMessage = string.Empty;
    }

    private Color GetNameColor(InstantMessage msg)
    {
        Color color = ColorScheme.ChatNameOtherUser;

        if (msg.Cmid == PlayerDataManager.Cmid)
        {
            color = ColorScheme.ChatNameCurrentUser;
        }
        else if (msg.IsFriend || msg.IsClan)
        {
            color = ColorScheme.ChatNameFriendsUser;
        }
        else if (msg.AccessLevel == MemberAccessLevel.Admin)
        {
            color = ColorScheme.ChatNameAdminUser;
        }
        else if (msg.AccessLevel > 0)
        {
            color = ColorScheme.ChatNameModeratorUser;
        }

        return color;
    }

    #region PopupMenu

    private bool CheckMouseClickIn(Rect rect, int mouse = 0)
    {
        return InputManager.GetMouseButtonDown(mouse) && rect.Contains(Event.current.mousePosition);
    }

    private void MenuCmdAddFriend(CommUser user)
    {
        if (user != null)
        {
            FriendRequestPanelGUI gui = PanelManager.Instance.OpenPanel(PanelType.FriendRequest) as FriendRequestPanelGUI;
            if (gui != null)
                gui.Show(user.Cmid, user.Name);
        }
    }

    private void MenuCmdChat(CommUser user)
    {
        if (user != null) ChatManager.Instance.CreatePrivateChat(user.Cmid);
    }

    private void MenuCmdSendMessage(CommUser user)
    {
        if (user != null)
        {
            SendMessagePanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.SendMessage) as SendMessagePanelGUI;
            if (panel) panel.SelectReceiver(user.Cmid, user.Name);
        }
    }

    private void MenuCmdJoinGame(CommUser user)
    {
        if (user != null && !user.CurrentGame.IsEmpty)
        {
            JoinRoom(user.CurrentGame);
        }
    }

    private void MenuCmdInviteClan(CommUser user)
    {
        if (user != null)
        {
            InviteToClanPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.ClanRequest) as InviteToClanPanelGUI;
            if (panel) panel.SelectReceiver(user.Cmid, user.ShortName);
        }
    }

    private void MenuCmdModeratePlayer(CommUser user)
    {
        CommActorInfo info;
        if (user != null && CommConnectionManager.CommCenter.TryGetActorWithCmid(ChatManager.Instance._selectedCmid, out info) && info != null)
        {
            ModerationPanelGUI gui = PanelManager.Instance.OpenPanel(PanelType.Moderation) as ModerationPanelGUI;
            if (gui) gui.SetSelectedUser(user);
        }
    }

    // menu check

    private bool MenuChkAddFriend(CommUser user)
    {
        return user != null && user.Cmid != PlayerDataManager.Cmid && user.AccessLevel <= PlayerDataManager.AccessLevel && !PlayerDataManager.IsFriend(user.Cmid);
    }

    private bool MenuChkChat(CommUser user)
    {
        return user != null && user.Cmid != PlayerDataManager.Cmid && user.IsOnline;
    }

    private bool MenuChkSendMessage(CommUser user)
    {
        return user != null && user.Cmid != PlayerDataManager.Cmid && !GameState.HasCurrentGame;
    }

    private bool MenuChkJoinGame(CommUser user)
    {
        return user != null && user.Cmid != PlayerDataManager.Cmid && user.IsOnline && !user.CurrentGame.IsEmpty && user.CurrentGame != CommConnectionManager.CurrentRoomID;
    }

    private bool MenuChkInviteClan(CommUser user)
    {
        return user != null && user.Cmid != PlayerDataManager.Cmid && user.AccessLevel <= PlayerDataManager.AccessLevel && PlayerDataManager.IsPlayerInClan && PlayerDataManager.CanInviteToClan && !PlayerDataManager.IsClanMember(user.Cmid);
    }

    #endregion

    #region Properties

    public static TabArea SelectedTab { get; set; }

    #endregion

    #region Fields

    public static bool IsCompleteLobbyLoaded = false;

    //[SerializeField]
    //private Texture2D _newMessageIcon;
    [SerializeField]
    private Texture2D _speedhackerIcon;

    private const int SEARCH_HEIGHT = 36;
    private float _nextFullLobbyUpdate;


    /// IO functions
    private float _spammingNotificationTime = 0;
    private float _nextNaughtyListUpdate;

    private bool _checkForPassword;
    private bool _isPasswordOk = true;

    private float _yPosition;
    private float _lastMessageSentTimer = 0.3f;
    private float _nextPasswordCheck;

    private string _inputedPassword = string.Empty;
    private string _currentChatMessage = string.Empty;

    private GameMetaData _game;
    private PopupMenu _playerMenu;

    private const int TAB_WIDTH = 300;
    private const int CHAT_USER_HEIGHT = 24;
    private const int CHAT_TEXTFIELD_HEIGHT = 36;
    private const float CHECK_PASSWORD_DELAY = 2f;

    #endregion
}
