using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class InboxPageGUI : MonoBehaviour
{
    #region Fields
    [SerializeField]
    private Texture2D _sideTexture;
    [SerializeField]
    private Texture2D _newMessage;

    private const int PanelHeight = 50;

    private int _threadWidth;
    private int _selectedTab;

    private const int TAB_MESSAGE = 0;
    private const int TAB_REQUEST = 1;
    private const int TAB_FRIEND = 2;

    private GUIContent[] _tabContents;

    private int _threadViewWidth;
    private int _threadViewHeight;

    private Vector2 _threadScroll;
    private Vector2 _friendScroll;
    private Vector2 _requestScroll;

    private int _messageViewWidth;
    private int _messageViewHeight;

    private string _replyMessage = string.Empty;
    private string _searchMessage = string.Empty;
    private string _searchFriend = string.Empty;

    private int _friendWidth;
    private int _friendHeight;

    private int _requestWidth;
    private int _requestHeight;

    #endregion

    private void Start()
    {
        _tabContents = new GUIContent[] { new GUIContent(LocalizedStrings.MessagesCaps), new GUIContent(LocalizedStrings.RequestsCaps), new GUIContent(LocalizedStrings.FriendsCaps) };
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;
        GUI.skin = BlueStonez.Skin;

        Rect rect = new Rect(0, GlobalUIRibbon.Instance.GetHeight(), Screen.width, Screen.height - GlobalUIRibbon.Instance.GetHeight());
        _threadWidth = (int)rect.width / 4;

        GUI.BeginGroup(rect, BlueStonez.window_standard_grey38);
        {
            GUI.enabled = PlayerDataManager.IsPlayerLoggedIn && IsNoPanelOpen();

            DrawInbox(new Rect(0, 0, rect.width, rect.height));

            GUI.enabled = true;
        }
        GUI.EndGroup();
    }

    private void DrawInbox(Rect rect)
    {
        DoTitle(new Rect(1, 0, rect.width - 2, 72));

        switch (_selectedTab)
        {
            case TAB_MESSAGE:
                {
                    DoToolbarMessage(new Rect(1, 72, rect.width - 2, 40));
                    DoThreads(new Rect(1, 112, _threadWidth, rect.height - 112));
                    DoMessages(new Rect(_threadWidth, 110, rect.width - _threadWidth, rect.height - 112));
                } break;
            case TAB_REQUEST:
                {
                    float timeRemaining = Mathf.Max(InboxManager.Instance.NextRequestRefresh - Time.time, 0);
                    GUITools.PushGUIState();
                    GUI.enabled &= timeRemaining == 0;
                    if (GUITools.Button(new Rect(rect.width - 131, 80, 123, 24), new GUIContent(string.Format(LocalizedStrings.Refresh + " {0}", (timeRemaining > 0) ? "(" + timeRemaining.ToString("N0") + ")" : string.Empty)), BlueStonez.buttondark_medium))
                    {
                        InboxManager.Instance.RefreshAllRequests();
                    }
                    GUITools.PopGUIState();

                    DoRequests(new Rect(0, 112, rect.width, rect.height - 112));

                } break;
            case TAB_FRIEND:
                {
                    DoToolbarFriend(new Rect(1, 72, rect.width - 2, 40));
                    DrawFriends(new Rect(0, 112, rect.width, rect.height - 112));
                } break;

        }
    }

    private void DoTitle(Rect rect)
    {
        GUI.BeginGroup(rect, BlueStonez.tab_strip_large);
        {
            int tab = GUI.SelectionGrid(new Rect(1, 32, 508, 40), _selectedTab, _tabContents, _tabContents.Length, BlueStonez.tab_large);
            if (GUI.changed)
            {
                GUI.changed = false;
                SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
            }

            if (tab != _selectedTab)
            {
                GUIUtility.keyboardControl = 0;
                _selectedTab = tab;
            }

            if (InboxManager.Instance.HasUnreadMessages)
                GUI.DrawTexture(new Rect(133, 32, 20, 20), _newMessage);

            if (InboxManager.Instance.HasUnreadRequests)
                GUI.DrawTexture(new Rect(311, 32, 20, 20), _newMessage);
        }
        GUI.EndGroup();
    }

    private void DoToolbarMessage(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(8, 8, 206, 24), string.Format(LocalizedStrings.YouHaveNNewMessages, InboxManager.Instance._unreadMessageCount), BlueStonez.label_interparkbold_16pt_left);

            if (_selectedTab == TAB_MESSAGE)
            {
                Rect editRect = new Rect(rect.width - 368, 8, 140, 24);

                GUI.SetNextControlName("SearchMessage");
                _searchMessage = GUI.TextField(editRect, _searchMessage, BlueStonez.textField);
                if (string.IsNullOrEmpty(_searchMessage) && GUI.GetNameOfFocusedControl() != "SearchMessage")
                {
                    GUI.color = new Color(1, 1, 1, 0.3f);
                    GUI.Label(editRect, " " + LocalizedStrings.SearchMessages, BlueStonez.label_interparkbold_11pt_left);
                    GUI.color = Color.white;
                }
            }

            if (GUITools.Button(new Rect(rect.width - 224, 8, 106, 24), new GUIContent(LocalizedStrings.NewMessage), BlueStonez.buttondark_medium))
            {
                PanelManager.Instance.OpenPanel(PanelType.SendMessage);
            }

            float timeRemaining = Mathf.Max(InboxManager.Instance.NextInboxRefresh - Time.time, 0);
            GUITools.PushGUIState();
            GUI.enabled &= timeRemaining == 0;
            if (GUITools.Button(new Rect(rect.width - 114, 8, 106, 24), new GUIContent(string.Format(LocalizedStrings.CheckMail + " {0}", (timeRemaining > 0) ? "(" + timeRemaining.ToString("N0") + ")" : string.Empty)), BlueStonez.buttondark_medium))
            {
                InboxManager.Instance.LoadNextPageThreads();
            }
            GUITools.PopGUIState();
        }
        GUI.EndGroup();
    }

    private bool IsNoPanelOpen()
    {
        return !PanelManager.IsAnyPanelOpen;
    }

    private void DoToolbarFriend(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            if (GUITools.Button(new Rect(8, 8, 106, 24), new GUIContent(LocalizedStrings.AddFriends), BlueStonez.buttondark_medium))
            {
                PanelManager.Instance.OpenPanel(PanelType.FriendRequest);
            }

            GUI.SetNextControlName("SearchFriend");
            _searchFriend = GUI.TextField(new Rect(rect.width - 258, 8, 123, 24), _searchFriend, BlueStonez.textField);
            if (string.IsNullOrEmpty(_searchFriend) && GUI.GetNameOfFocusedControl() != "SearchFriend")
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(new Rect(rect.width - 258, 8, 123, 24), " " + LocalizedStrings.Search, BlueStonez.label_interparkbold_11pt_left);
                GUI.color = Color.white;
            }

            float timeRemaining = Mathf.Max(CommsManager.Instance.NextFriendsRefresh - Time.time, 0);
            GUITools.PushGUIState();
            GUI.enabled &= timeRemaining == 0;
            if (GUITools.Button(new Rect(rect.width - 131, 8, 123, 24), new GUIContent(string.Format(LocalizedStrings.Refresh + " {0}", (timeRemaining > 0) ? "(" + timeRemaining.ToString("N0") + ")" : string.Empty)), BlueStonez.buttondark_medium))
            {
                StartCoroutine(CommsManager.Instance.GetContactsByGroups());
            }
            GUITools.PopGUIState();

            string friendCountText = string.Format(LocalizedStrings.YouHaveNFriends, PlayerDataManager.Instance.FriendsCount);

            if (PlayerDataManager.Instance.FriendsCount == 0)
                friendCountText = LocalizedStrings.YouHaveNoFriends;
            else if (PlayerDataManager.Instance.FriendsCount == 1)
                friendCountText = LocalizedStrings.YouHaveOnlyOneFriend;

            GUI.Label(new Rect(0, 0, rect.width, rect.height), friendCountText, BlueStonez.label_interparkbold_16pt);
        }
        GUI.EndGroup();
    }

    private void DoThreads(Rect rect)
    {
        rect = new Rect(rect.x + 8, rect.y, rect.width - 8, rect.height - 8);
        GUI.Box(rect, GUIContent.none, BlueStonez.window);

        if (InboxManager.Instance.ThreadCount > 0)
        {
            Vector2 newThreadScroll = GUI.BeginScrollView(rect, _threadScroll, new Rect(0, 0, _threadViewWidth, _threadViewHeight));
            bool isScrollYIncreased = newThreadScroll.y > _threadScroll.y;
            _threadScroll = newThreadScroll;
            int y = 0;

            // foreach (var thread in InboxManager.Instance.AllThreads)
            for (int i = 0; i < InboxManager.Instance.ThreadCount; i++)
            {
                var thread = InboxManager.Instance.AllThreads[i];

                if (!string.IsNullOrEmpty(_searchMessage) && !thread.Contains(_searchMessage))
                    continue;

                y = thread.DrawThread(y, _threadViewWidth);

                GUI.Label(new Rect(4, y, _threadViewWidth, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
            }

            if (InboxManager.Instance.IsLoadingThreads)
            {
                GUI.Label(new Rect(0, y, rect.width, 30), "Loading threads...", BlueStonez.label_interparkmed_11pt);
                y += 30;
            }
            else
            {
                if (InboxManager.Instance.IsNoMoreThreads)
                {
                    GUI.contentColor = Color.gray;
                    GUI.Label(new Rect(0, y, rect.width, 30), "No more threads", BlueStonez.label_interparkmed_11pt);
                    GUI.contentColor = Color.white;
                }

                y += 30;

                float maxScrollY = Mathf.Max(y - rect.height, 0);
                if (isScrollYIncreased && _threadScroll.y >= maxScrollY)
                {
                    InboxManager.Instance.LoadNextPageThreads();
                }
            }
            _threadViewHeight = y;
            _threadViewWidth = (int)((_threadViewHeight > rect.height) ? rect.width - 22 : rect.width - 8);

            GUI.EndScrollView();
        }
        else
        {
            if (InboxManager.Instance.IsLoadingThreads)
            {
                GUI.Label(rect, "Loading threads...", BlueStonez.label_interparkbold_13pt);
            }
            else
            {
                GUI.Label(rect, LocalizedStrings.Empty, BlueStonez.label_interparkmed_11pt);
            }
        }
    }

    private void DoMessages(Rect rect)
    {
        InboxThread thread = InboxThread.Current;

        bool isAdminThread = thread != null && thread.IsAdmin;

        Rect r = new Rect(rect.x + 8, rect.y + 2, rect.width - 8 * 2, rect.height - 8);
        GUI.Box(r, GUIContent.none, BlueStonez.box_grey50);

        string conversationBetween = LocalizedStrings.NoConversationSelected;

        if (thread != null)
        {
            conversationBetween = string.Format(LocalizedStrings.BetweenYouAndN, thread.Name);

            if (GUI.Button(new Rect(r.x + 10, r.y + 10, 150, 20), "Delete Conversation", BlueStonez.buttondark_medium))
            {
                InboxThread.Current = null;
                InboxManager.Instance.DeleteThread(thread.ThreadId);
            }
        }

        GUI.contentColor = new Color(1, 1, 1, 0.75f);
        GUI.Label(new Rect(r.x + 10, r.y, r.width - 20, 40), conversationBetween, BlueStonez.label_interparkmed_11pt_right);
        GUI.contentColor = Color.white;
        GUI.Label(new Rect(r.x + 4, r.y + 40, r.width - 8, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);

        int y = 8;
        Rect msgRect = new Rect(r.x + 8, r.y + 48, r.width - 8, r.height - (isAdminThread ? 49 : 90));
        if (InboxThread.Current != null)
        {
            thread.Scroll = GUI.BeginScrollView(msgRect, thread.Scroll, new Rect(0, 0, _messageViewWidth, _messageViewHeight));
            {
                y = thread.DrawMessageList(y, _messageViewWidth, msgRect.height, thread.Scroll.y);

                // draw reply area
                if (y > msgRect.height)
                {
                    _messageViewHeight = y;
                    _messageViewWidth = (int)(msgRect.width - 22);
                }
                else
                {
                    _messageViewHeight = (int)msgRect.height;
                    _messageViewWidth = (int)msgRect.width - 8;
                }
            }
            GUI.EndScrollView();
        }
        else
        {
            GUI.Label(msgRect, "Select a message thread", BlueStonez.label_interparkbold_13pt);
        }

        //never reply to an admin thread
        if (!isAdminThread)
        {
            GUITools.PushGUIState();
            GUI.enabled &= InboxThread.Current != null;
            GUI.Box(new Rect(rect.x + 8, rect.y + rect.height - 51, rect.width - 16, 45), GUIContent.none, BlueStonez.window_standard_grey38);
            DoReply(new Rect(rect.x, rect.y + rect.height - 51, rect.width, 45));
            GUITools.PopGUIState();
        }
    }

    private void DoReply(Rect rect)
    {
        Rect replyRect = new Rect(rect.x + (rect.width - 420) / 2, rect.y + 12, 420, rect.height);
        GUI.BeginGroup(replyRect);
        {
            GUI.SetNextControlName("Reply Edit");
            _replyMessage = GUI.TextField(new Rect(0, 0, replyRect.width - 64, 24), _replyMessage, 140, BlueStonez.textField);
            _replyMessage = _replyMessage.Trim(new char[] { '\n' });

            if (GUI.GetNameOfFocusedControl().Equals("Reply Edit") && !string.IsNullOrEmpty(_replyMessage) &&
                Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
            {
                SendMessage();
            }

            GUITools.PushGUIState();
            GUI.enabled &= !string.IsNullOrEmpty(_replyMessage);
            if (GUITools.Button(new Rect(replyRect.width - 64, 0, 64, 24), new GUIContent(LocalizedStrings.Reply), BlueStonez.buttondark_medium))
            {
                SendMessage();
            }
            GUITools.PopGUIState();
        }
        GUI.EndGroup();
    }

    private void SendMessage()
    {
        if (InboxThread.Current != null)
        {
            InboxManager.Instance.SendPrivateMessage(InboxThread.Current.ThreadId, InboxThread.Current.Name, _replyMessage);
            _replyMessage = string.Empty;

            GUIUtility.keyboardControl = 0;
        }
    }

    private void DrawFriends(Rect rect)
    {
        Rect friRect = new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height - 8);
        GUI.BeginGroup(friRect, BlueStonez.window);
        {
            _friendWidth = (int)friRect.width - 240;

            if (_friendHeight > friRect.height)
                _friendWidth -= 22;
            else
                _friendWidth -= 8;

            if (_sideTexture)
            {
                GUI.DrawTexture(new Rect(0, friRect.height - _sideTexture.height, _sideTexture.width, _sideTexture.height), _sideTexture);
            }

            Rect r = new Rect(240, 0, friRect.width - 240, friRect.height);

            if (PlayerDataManager.Instance.FriendsCount > 0)
            {
                _friendScroll = GUI.BeginScrollView(r, _friendScroll, new Rect(0, 0, _friendWidth, _friendHeight));

                int j = 0;
                string search = _searchFriend.ToLower();

                foreach (var friend in PlayerDataManager.Instance.FriendList)
                {
                    if (!string.IsNullOrEmpty(search) && !friend.Name.ToLower().Contains(search))
                        continue;

                    DrawFriend(friend, j, _friendWidth);

                    j += PanelHeight + 10;
                }

                _friendHeight = j - 2;
                GUI.EndScrollView();
            }
            else
            {
                GUI.Label(r, "You have no friends ... yet!\n\nOpen the chat window and connect to other Uberstrikers.\nUse the 'Add as Friend' function to send out a friend request :)", BlueStonez.label_interparkbold_13pt);
            }
        }
        GUI.EndGroup();
    }

    private void DrawFriend(PublicProfileView view, int y, int width)
    {
        bool onFocus = !PopupSystem.IsAnyPopupOpen && GUI.enabled && new Rect(0, y - 1, width, PanelHeight + 10).Contains(Event.current.mousePosition);

        Rect friRect = new Rect(4, y + 4, width - 1, PanelHeight);
        GUI.BeginGroup(friRect);
        {
            CommUser commUser;
            ChatManager.Instance.TryGetFriend(view.Cmid, out commUser);

            Rect rect = new Rect(0, 0, friRect.width, friRect.height - 1);
            if (onFocus)
            {
                GUI.Box(rect, GUIContent.none, BlueStonez.box_grey50);
            }

            Texture presenceIcon = ChatManager.GetPresenceIcon(null);

            if (commUser != null)
            {
                presenceIcon = ChatManager.GetPresenceIcon(commUser.PresenceIndex);
            }

            GUI.DrawTexture(new Rect(10, 8, 14, 20), presenceIcon);
            GUI.Label(new Rect(40, 8, 250, 20), view.Name, BlueStonez.label_interparkbold_16pt_left);

            if (onFocus)
            {
                if (GUITools.Button(new Rect(friRect.width - 20 - 10, 8, 20, 20), new GUIContent("x"), BlueStonez.buttondark_medium))
                {
                    int friendCmid = view.Cmid;
                    PopupSystem.ShowMessage(LocalizedStrings.RemoveFriendCaps, string.Format(LocalizedStrings.DoYouReallyWantToRemoveNFromYourFriendsList, view.Name), PopupSystem.AlertType.OKCancel,
                        () => InboxManager.Instance.RemoveFriend(friendCmid), LocalizedStrings.Remove,
                        null, LocalizedStrings.Cancel,
                        PopupSystem.ActionType.Negative);
                }
                GUITools.PushGUIState();

                if (commUser != null)
                {
                    GUI.enabled &= commUser.IsOnline;
                }

                if (GUITools.Button(new Rect(friRect.width - 90 - 40, 8, 90, 20), new GUIContent(LocalizedStrings.PrivateChat), BlueStonez.buttondark_medium))
                {
                    ChatManager.Instance.CreatePrivateChat(view.Cmid);
                    MenuPageManager.Instance.LoadPage(PageType.Chat);
                }
                GUITools.PopGUIState();

                if (GUITools.Button(new Rect(friRect.width - 90 - 140, 8, 90, 20), new GUIContent(LocalizedStrings.SendMessage), BlueStonez.buttondark_medium))
                {
                    SendMessagePanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.SendMessage) as SendMessagePanelGUI;
                    if (panel) panel.SelectReceiver(view.Cmid, view.Name);
                }
            }
        }
        GUI.EndGroup();

        GUI.Label(new Rect(2, y + PanelHeight + 8, width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
    }

    private void DoRequests(Rect rect)
    {
        Rect reqRect = new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height - 8);
        GUI.BeginGroup(reqRect, BlueStonez.window);
        {
            int voffset = 5;
            _requestHeight = 3 * 60
                + InboxManager.Instance._friendRequests.Count * (PanelHeight + 10)
                + InboxManager.Instance._incomingClanRequests.Count * (PanelHeight + 10)
                + InboxManager.Instance._outgoingClanRequests.Count * (PanelHeight + 10);

            _requestWidth = (int)reqRect.width - (_requestHeight > reqRect.height ? 22 : 8);

            //FRIEND REQUESTS
            _requestScroll = GUI.BeginScrollView(new Rect(0, voffset, reqRect.width, reqRect.height), _requestScroll, new Rect(0, 0, _requestWidth, _requestHeight));
            {
                GUI.Box(new Rect(4, 0, _requestWidth, 50), GUIContent.none, BlueStonez.box_grey38);
                GUI.Label(new Rect(14, 0, _requestWidth - 10, 50), string.Format(LocalizedStrings.FriendRequestsYouHaveNPendingRequests, InboxManager.Instance._friendRequests.Count.ToString(), (InboxManager.Instance._friendRequests.Count != 1 ? "s" : string.Empty)), BlueStonez.label_interparkmed_18pt_left);
                voffset += 50;
                for (int i = 0; i < InboxManager.Instance._friendRequests.Count; i++)
                {
                    DrawFriendRequestView(InboxManager.Instance._friendRequests[i], voffset, _requestWidth);

                    GUI.Label(new Rect(25, voffset + Mathf.RoundToInt((PanelHeight - 32) * 0.5f), 32, 32), (i + 1).ToString(), BlueStonez.label_interparkbold_32pt);

                    voffset += PanelHeight + 10;
                }

                //CLAN REQUESTS
                GUI.Box(new Rect(4, voffset, _requestWidth, 50), GUIContent.none, BlueStonez.box_grey38);
                GUI.Label(new Rect(14, voffset, _requestWidth - 10, 50), string.Format("Clan Requests - You have {0} incoming invite{1}", InboxManager.Instance._incomingClanRequests.Count, (InboxManager.Instance._incomingClanRequests.Count != 1 ? "s" : string.Empty)), BlueStonez.label_interparkmed_18pt_left);
                voffset += 55;
                for (int i = 0; i < InboxManager.Instance._incomingClanRequests.Count; i++)
                {
                    DrawIncomingClanInvitation(InboxManager.Instance._incomingClanRequests[i], voffset, _requestWidth);

                    GUI.Label(new Rect(25, voffset + Mathf.RoundToInt((PanelHeight - 32) * 0.5f), 32, 32), (i + 1).ToString(), BlueStonez.label_interparkbold_32pt);

                    voffset += PanelHeight + 10;
                }

                //SENT CLAN REQUESTS
                GUI.Box(new Rect(4, voffset, _requestWidth, 50), GUIContent.none, BlueStonez.box_grey38);
                GUI.Label(new Rect(14, voffset, _requestWidth - 10, 50), string.Format("Clan Requests - You have {0} outgoing invite{1}", InboxManager.Instance._outgoingClanRequests.Count, (InboxManager.Instance._outgoingClanRequests.Count != 1 ? "s" : string.Empty)), BlueStonez.label_interparkmed_18pt_left);
                voffset += 55;
                for (int i = 0; i < InboxManager.Instance._outgoingClanRequests.Count; i++)
                {
                    DrawOutgoingClanInvitation(InboxManager.Instance._outgoingClanRequests[i], voffset, _requestWidth);

                    GUI.Label(new Rect(25, voffset + Mathf.RoundToInt((PanelHeight - 32) * 0.5f), 32, 32), (i + 1).ToString(), BlueStonez.label_interparkbold_32pt);

                    voffset += PanelHeight + 10;
                }

            }
            GUI.EndScrollView();

        }
        GUI.EndGroup();
    }

    public void DrawFriendRequestView(ContactRequestView request, float y, int width)
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
            GUI.Label(new Rect(80, 5, reqRect.width - 250, 20), string.Format("{0}: {1}", LocalizedStrings.FriendRequest, request.InitiatorName), BlueStonez.label_interparkbold_13pt_left);
            GUI.Label(new Rect(80, 30, reqRect.width - 250, 20), "> " + request.InitiatorMessage, BlueStonez.label_interparkmed_11pt_left);

            if (GUITools.Button(new Rect(reqRect.width - 120 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Accept), BlueStonez.buttondark_medium))
            {
                InboxManager.Instance.AcceptContactRequest(request.RequestId);
            }
            if (GUITools.Button(new Rect(reqRect.width - 50 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Ignore), BlueStonez.buttondark_medium))
            {
                InboxManager.Instance.DeclineContactRequest(request.RequestId);
            }
        }
        GUI.EndGroup();

        GUI.Label(new Rect(4, y + PanelHeight + 8, width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
    }

    private void DrawIncomingClanInvitation(GroupInvitationView view, int y, int width)
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

            GUI.Label(new Rect(80, 5, reqRect.width - 250, 20), string.Format("{0}: {1}", LocalizedStrings.ClanInvite, view.GroupName), BlueStonez.label_interparkbold_13pt_left);
            GUI.Label(new Rect(80, 30, reqRect.width - 250, 20), "> " + view.Message, BlueStonez.label_interparkmed_11pt_left);

            if (GUITools.Button(new Rect(reqRect.width - 120 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Accept), BlueStonez.buttondark_medium))
            {
                //for convenience we check if player is in clan BEFORE we call the webservice
                if (PlayerDataManager.IsPlayerInClan)
                {
                    PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.YouAlreadyInClanMsg, PopupSystem.AlertType.OK);
                }
                else
                {
                    int requestId = view.GroupInvitationId;
                    PopupSystem.ShowMessage(LocalizedStrings.Accept, "Do you want to accept this clan invitation?", PopupSystem.AlertType.OKCancel,
                        () => InboxManager.Instance.AcceptClanRequest(requestId), "Join",
                        null, "Cancel",
                        PopupSystem.ActionType.Positive);
                }
            }
            if (GUITools.Button(new Rect(reqRect.width - 50 - 18, 5, 60, 20), new GUIContent(LocalizedStrings.Ignore), BlueStonez.buttondark_medium))
            {
                InboxManager.Instance.DeclineClanRequest(view.GroupInvitationId);
            }
        }
        GUI.EndGroup();

        GUI.Label(new Rect(4, y + PanelHeight + 8, width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
    }

    private void DrawOutgoingClanInvitation(GroupInvitationView view, int y, int width)
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

            GUI.Label(new Rect(80, 5, reqRect.width - 250, 20), string.Format("You invited: {0}", view.InviteeName), BlueStonez.label_interparkbold_13pt_left);
            GUI.Label(new Rect(80, 30, reqRect.width - 250, 20), "> " + view.Message, BlueStonez.label_interparkmed_11pt_left);

            if (GUITools.Button(new Rect(reqRect.width - 140, 5, 120, 20), new GUIContent(LocalizedStrings.CancelInvite), BlueStonez.buttondark_medium))
            {
                int groupInvitationId = view.GroupInvitationId;

                //remove the invite from the list and send notification to backend
                if (InboxManager.Instance._outgoingClanRequests.Remove(view))
                {
                    UberStrike.WebService.Unity.ClanWebServiceClient.CancelInvitation(groupInvitationId, PlayerDataManager.CmidSecure,
                    null,
                    (ex) =>
                    {
                        DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
                    });
                }
            }
        }
        GUI.EndGroup();

        GUI.Label(new Rect(4, y + PanelHeight + 8, width, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);
    }
}