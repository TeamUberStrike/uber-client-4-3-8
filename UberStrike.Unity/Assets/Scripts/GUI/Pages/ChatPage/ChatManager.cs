
using System;
using System.Collections.Generic;
using System.Text;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class ChatManager : Singleton<ChatManager>
{
    /// Store the temprate dialogs with others
    private List<CommUser> _otherUsers;
    private List<CommUser> _friendUsers;
    private List<CommUser> _lobbyUsers;
    public Dictionary<int, CommUser> _modUsers;
    private Dictionary<int, CommUser> _clanUsers;
    private List<CommUser> _ingameUsers;
    private List<CommUser> _lastgameUsers;
    private Dictionary<int, CommUser> _allTimePlayers;
    private HashSet<TabArea> _tabAreas;

    private float _nextRefreshTime = 0;
    public int _selectedCmid = 0;
    public ChatDialog _selectedDialog;
    public bool MarkPrivateMessageTab { get; set; }
    public bool MarkClanMessageTab { get; set; }

    public ChatGroupPanel[] _commPanes;
    public Dictionary<int, ChatDialog> _dialogsByCmid;

    public ChatDialog ClanDialog { get; private set; }
    public ChatDialog LobbyDialog { get; private set; }
    public ChatDialog InGameDialog { get; private set; }
    public ChatDialog ModerationDialog { get; private set; }

    public ICollection<CommUser> OtherUsers { get { return _otherUsers; } }
    public ICollection<CommUser> FriendUsers { get { return _friendUsers; } }
    public ICollection<CommUser> LobbyUsers { get { return _lobbyUsers; } }
    public ICollection<CommUser> ClanUsers { get { return _clanUsers.Values; } }
    public ICollection<CommUser> NaughtyUsers { get { return _modUsers.Values; } }
    public ICollection<CommUser> GameUsers { get { return _ingameUsers; } }
    public ICollection<CommUser> GameHistoryUsers { get { return _lastgameUsers; } }

    private ChatManager()
    {
        //USER LISTS
        _otherUsers = new List<CommUser>();
        _friendUsers = new List<CommUser>();
        _lobbyUsers = new List<CommUser>();
        _clanUsers = new Dictionary<int, CommUser>();
        _modUsers = new Dictionary<int, CommUser>();
        _ingameUsers = new List<CommUser>();
        _lastgameUsers = new List<CommUser>();
        _allTimePlayers = new Dictionary<int, CommUser>();
        _dialogsByCmid = new Dictionary<int, ChatDialog>();

        //DIALOGS
        ClanDialog = new ChatDialog(string.Empty);
        LobbyDialog = new ChatDialog(string.Empty);
        ModerationDialog = new ChatDialog(string.Empty);
        InGameDialog = new ChatDialog(string.Empty);
        InGameDialog.CanShow = CanShowMessage;

        _commPanes = new ChatGroupPanel[5];
        _commPanes[(int)TabArea.Lobby] = new ChatGroupPanel();
        _commPanes[(int)TabArea.Private] = new ChatGroupPanel();
        _commPanes[(int)TabArea.Clan] = new ChatGroupPanel();
        _commPanes[(int)TabArea.InGame] = new ChatGroupPanel();
        _commPanes[(int)TabArea.Moderation] = new ChatGroupPanel();

        _tabAreas = new HashSet<TabArea>()
        {
             TabArea.Lobby,
             TabArea.Private,
        };

        ClanDialog.Title = LocalizedStrings.ChatInClan;
        LobbyDialog.Title = LocalizedStrings.ChatInLobby;
        ModerationDialog.Title = LocalizedStrings.Moderate;

        _commPanes[(int)TabArea.Lobby].AddGroup(UserGroups.None, LocalizedStrings.Lobby, LobbyUsers);
        _commPanes[(int)TabArea.Private].AddGroup(UserGroups.Friend, LocalizedStrings.Friends, FriendUsers);
        _commPanes[(int)TabArea.Private].AddGroup(UserGroups.Other, LocalizedStrings.Others, OtherUsers);
        _commPanes[(int)TabArea.Clan].AddGroup(UserGroups.None, LocalizedStrings.Clan, ClanUsers);
        _commPanes[(int)TabArea.InGame].AddGroup(UserGroups.None, LocalizedStrings.Game, GameUsers);
        _commPanes[(int)TabArea.InGame].AddGroup(UserGroups.Other, "History", GameHistoryUsers);
        _commPanes[(int)TabArea.Moderation].AddGroup(UserGroups.None, "Naughty List", NaughtyUsers);

        CmuneEventHandler.AddListener<LoginEvent>(OnLoginEvent);
    }

    protected override void OnDispose()
    {
        CmuneEventHandler.RemoveListener<LoginEvent>(OnLoginEvent);
    }

    private void OnLoginEvent(LoginEvent ev)
    {
        if (ev.AccessLevel > 0) _tabAreas.Add(TabArea.Moderation);
    }

    public int TabCounter
    {
        get { return _tabAreas.Count + (ShowTab(TabArea.InGame) ? 1 : 0) + (ShowTab(TabArea.Clan) ? 1 : 0) + (ShowTab(TabArea.Moderation) ? 1 : 0); }
    }

    public bool ShowTab(TabArea tab)
    {
        switch (tab)
        {
            case TabArea.InGame: return (GameState.HasCurrentGame || ChatManager.Instance.GameHistoryUsers.Count > 0);
            case TabArea.Clan: return PlayerDataManager.IsPlayerInClan;
            case TabArea.Moderation: return PlayerDataManager.AccessLevel > MemberAccessLevel.Default;
            default: return _tabAreas.Contains(tab);
        }
    }

    public static ChatContext CurrentChatContext
    {
        get { return PlayerSpectatorControl.Instance.IsEnabled ? ChatContext.Spectator : ChatContext.Player; }
    }

    public static bool CanShowMessage(ChatContext ctx)
    {
        if (GameState.HasCurrentGame && GameState.CurrentGameMode == GameMode.TeamElimination && GameState.CurrentGame.IsMatchRunning)
        {
            ChatContext myctx = PlayerSpectatorControl.Instance.IsEnabled ? ChatContext.Spectator : ChatContext.Player;
            return ctx == myctx;
        }
        else
        {
            return true;
        }
    }

    public bool HasDialogWith(int cmid)
    {
        return _dialogsByCmid.ContainsKey(cmid);
    }

    public void UpdateClanSection()
    {
        ChatManager.Instance._clanUsers.Clear();
        foreach (var m in PlayerDataManager.Instance.ClanMembers)
        {
            ChatManager.Instance._clanUsers[m.Cmid] = new CommUser(m);
        }

        RefreshAll(true);
    }

    public void RefreshAll(bool forceRefresh = false)
    {
        //Debug.LogWarning("---- RefreshAll " + CommConnectionManager.CommCenter.PlayerCount + " " + (_nextRefreshTime < Time.time));

        if (forceRefresh || _nextRefreshTime < Time.time)
        {
            _nextRefreshTime = Time.time + 5;

            CommActorInfo actor;

            //update lobby users
            _lobbyUsers.Clear();
            foreach (var a in CommConnectionManager.CommCenter.Players)
            {
                if (a.ActorId > 0)
                {
                    CommUser user = new CommUser(a);
                    user.IsClanMember = PlayerDataManager.IsClanMember(a.Cmid);
                    user.IsFriend = PlayerDataManager.IsFriend(a.Cmid);
                    _lobbyUsers.Add(user);
                }
            }
            _lobbyUsers.Sort(new CommUserNameComparer());
            _lobbyUsers.Sort(new CommUserFriendsComparer());

            //update game history
            foreach (var user in ChatManager.Instance._lastgameUsers)
            {
                user.IsClanMember = PlayerDataManager.IsClanMember(user.Cmid);
                user.IsFriend = PlayerDataManager.IsFriend(user.Cmid);

                if (CommConnectionManager.CommCenter.TryGetActorWithCmid(user.Cmid, out actor))
                    user.SetActor(actor);
                else
                    user.SetActor(null);
            }
            ChatManager.Instance._lastgameUsers.Sort(new CommUserPresenceComparer());

            ////update game history
            //foreach (var user in _allTimePlayers.Values)
            //{
            //    user.IsClanMember = PlayerDataManager.IsClanMember(user.Cmid);
            //    user.IsFriend = PlayerDataManager.IsFriend(user.Cmid);

            //    if (CommConnectionManager.CommCenter.TryGetActorWithCmid(user.Cmid, out actor))
            //        user.Actor = actor;
            //    else
            //        user.Actor = null;
            //}

            //update friends
            foreach (var user in ChatManager.Instance._friendUsers)
            {
                if (CommConnectionManager.CommCenter.TryGetActorWithCmid(user.Cmid, out actor))
                    user.SetActor(actor);
                else
                    user.SetActor(null);
            }
            ChatManager.Instance._friendUsers.Sort(new CommUserPresenceComparer());

            //update clan
            foreach (var user in ChatManager.Instance._clanUsers.Values)
            {
                if (CommConnectionManager.CommCenter.TryGetActorWithCmid(user.Cmid, out actor))
                    user.SetActor(actor);
                else
                    user.SetActor(null);
            }
            //no sort on dictionary

            //update others
            foreach (var user in ChatManager.Instance._otherUsers)
            {
                if (CommConnectionManager.CommCenter.TryGetActorWithCmid(user.Cmid, out actor))
                    user.SetActor(actor);
                else
                    user.SetActor(null);
            }
            ChatManager.Instance._otherUsers.Sort(new CommUserNameComparer());

            foreach (var user in ChatManager.Instance._modUsers)
            {
                if (CommConnectionManager.CommCenter.TryGetActorWithCmid(user.Key, out actor))
                    user.Value.SetActor(actor);
                else
                    user.Value.SetActor(null);
            }
        }
    }

    public void UpdateFriendSection()
    {
        List<CommUser> oldList = new List<CommUser>(ChatManager.Instance._friendUsers);

        ChatManager.Instance._friendUsers.Clear();
        foreach (var f in PlayerDataManager.Instance.FriendList)
        {
            ChatManager.Instance._friendUsers.Add(new CommUser(f));
        }

        //remove all conversations of people who are now our friends
        foreach (var f in ChatManager.Instance._friendUsers)
        {
            ChatDialog d;
            if (ChatManager.Instance._otherUsers.RemoveAll(u => u.Cmid == f.Cmid) > 0 && ChatManager.Instance._dialogsByCmid.TryGetValue(f.Cmid, out d))
            {
                d.Group = UserGroups.Friend;
            }
        }

        //put users that were our friends back to the list of other conversations
        foreach (var f in oldList)
        {
            ChatDialog d;
            if (ChatManager.Instance._dialogsByCmid.TryGetValue(f.Cmid, out d) &&  //we have an existing dialog
                !ChatManager.Instance._friendUsers.Exists(u => u.Cmid == f.Cmid) && //not with a friend
                !ChatManager.Instance._otherUsers.Exists(u => u.Cmid == f.Cmid)) //but it's not even in the 'others' section
            {
                ChatManager.Instance._otherUsers.Add(f);
                d.Group = UserGroups.Other;
            }
        }

        ChatManager.Instance.RefreshAll();
    }

    public static Texture GetPresenceIcon(CommActorInfo user)
    {
        if (user != null)
        {
            return GetPresenceIcon(user.IsInGame ? PresenceType.InGame : PresenceType.Online);
        }
        else
        {
            return GetPresenceIcon(PresenceType.Offline);
        }
    }

    public static Texture GetPresenceIcon(PresenceType index)
    {
        //return InGame icon if GameSection is active
        //if (_selectedTab == TabArea.TAB_GAME) return _presenceIcons[2];

        switch (index)
        {
            case PresenceType.InGame: return UberstrikeIcons.PresenceInGame;
            case PresenceType.Online: return UberstrikeIcons.PresenceOnline;
            case PresenceType.Offline: return UberstrikeIcons.PresenceOffline;
            default: return UberstrikeIcons.PresenceOffline;
        }
    }

    public void SetGameSection(CmuneRoomID roomId, IEnumerable<UberStrike.Realtime.Common.CharacterInfo> actors)
    {
        _ingameUsers.Clear();

        //update history to complete list of players
        _lastgameUsers.Clear();
        _lastgameUsers.AddRange(_allTimePlayers.Values);

        foreach (UberStrike.Realtime.Common.CharacterInfo v in actors)
        {
            CommUser user = new CommUser(v);
            user.CurrentGame = roomId;
            user.IsClanMember = PlayerDataManager.IsClanMember(user.Cmid);
            user.IsFriend = PlayerDataManager.IsFriend(user.Cmid);

            _ingameUsers.Add(user);
            _lastgameUsers.RemoveAll(p => p.Cmid == v.Cmid);

            //add to history
            if (v.Cmid != PlayerDataManager.Cmid && !_allTimePlayers.ContainsKey(v.Cmid))
            {
                user = new CommUser(v);
                user.CurrentGame = roomId;

                _allTimePlayers[v.Cmid] = user;
            }
        }
        _ingameUsers.Sort(new CommUserNameComparer());
    }

    public List<CommUser> GetCommUsersToReport()
    {
        // There are two types of players to report:
        // 1, In game players who is hacking
        // 2, Lobby players who use bad word
        // 3, Private chat players who use bad word

        int count = _ingameUsers.Count + _lobbyUsers.Count + _otherUsers.Count;

        Dictionary<int, CommUser> users = new Dictionary<int, CommUser>(count);

        foreach (CommUser u in _ingameUsers)
            users[u.Cmid] = u;

        foreach (CommUser u in _otherUsers)
            users[u.Cmid] = u;

        foreach (CommUser u in _lobbyUsers)
            users[u.Cmid] = u;

        return new List<CommUser>(users.Values);
    }

    public bool TryGetClanUsers(int cmid, out CommUser user)
    {
        return _clanUsers.TryGetValue(cmid, out user) && user != null;
    }

    public bool TryGetLobbyCommUser(int cmid, out CommUser user)
    {
        user = null;

        foreach (CommUser u in _lobbyUsers)
        {
            if (u.Cmid == cmid)
            {
                user = u;
                return true;
            }
        }

        return false;
    }

    public bool TryGetFriend(int cmid, out CommUser user)
    {
        foreach (var friend in _friendUsers)
        {
            if (friend.Cmid == cmid)
            {
                user = friend;
                return true;
            }
        }

        user = null;
        return false;
    }

    public void CreatePrivateChat(int cmid)
    {
        ChatDialog dialog = null;

        //already chatting with this actor
        ChatDialog d;
        if (_dialogsByCmid.TryGetValue(cmid, out d) && d != null)
        {
            dialog = d;
        }
        //actor is a friend
        else
        {
            CommUser user = null;
            CommActorInfo actor = null;

            if (PlayerDataManager.IsFriend(cmid))
            {
                user = _friendUsers.Find(u => u.Cmid == cmid);

                if (user != null && user.PresenceIndex != PresenceType.Offline)
                {
                    dialog = new ChatDialog(user, UserGroups.Friend);
                }
            }
            //actor is somebody else
            else if (CommConnectionManager.CommCenter.TryGetActorWithCmid(cmid, out actor))
            {
                //add to others list as Clan member
                ClanMemberView member;
                if (PlayerDataManager.TryGetClanMember(cmid, out member))
                {
                    user = new CommUser(member);
                    user.SetActor(actor);
                }
                else
                {
                    user = new CommUser(actor);
                }

                _otherUsers.Add(user);
                dialog = new ChatDialog(user, UserGroups.Other);
            }

            if (dialog != null)
            {
                _dialogsByCmid.Add(cmid, dialog);
            }
        }

        if (dialog != null)
        {
            ChatPageGUI.SelectedTab = TabArea.Private;

            _selectedDialog = dialog;
            _selectedCmid = cmid;
        }
        else
        {
            Debug.LogError(string.Format("Player with cmuneID {0} not found in communicator!", cmid));
        }
    }

    public string GetAllChatMessagesForPlayerReport()
    {
        ICollection<InstantMessage> msgs;
        StringBuilder strBuilder = new StringBuilder();

        msgs = ChatManager.Instance.InGameDialog.AllMessages;
        if (msgs.Count > 0)
        {
            strBuilder.AppendLine("In Game Chat:");
            foreach (InstantMessage msg in msgs)
                strBuilder.AppendLine(msg.PlayerName + " : " + msg.MessageText);
            strBuilder.AppendLine();
        }

        foreach (ChatDialog dlg in ChatManager.Instance._dialogsByCmid.Values)
        {
            msgs = dlg.AllMessages;
            if (msgs.Count > 0)
            {
                strBuilder.AppendLine("Private Chat:");
                foreach (InstantMessage msg in msgs)
                    strBuilder.AppendLine(msg.PlayerName + " : " + msg.MessageText);
                strBuilder.AppendLine();
            }
        }

        msgs = ChatManager.Instance.ClanDialog.AllMessages;
        if (msgs.Count > 0)
        {
            strBuilder.AppendLine("Clan Chat:");
            foreach (InstantMessage msg in msgs)
                strBuilder.AppendLine(msg.PlayerName + " : " + msg.MessageText);
            strBuilder.AppendLine();
        }

        msgs = ChatManager.Instance.LobbyDialog.AllMessages;
        if (msgs.Count > 0)
        {
            strBuilder.AppendLine("Lobby Chat:");
            foreach (InstantMessage msg in msgs)
                strBuilder.AppendLine(msg.PlayerName + " : " + msg.MessageText);
            strBuilder.AppendLine();
        }

        return strBuilder.ToString();
    }

    public void UpdateLastgamePlayers()
    {
        CommActorInfo actor;

        ChatManager.Instance._lastgameUsers.Clear();
        foreach (var user in ChatManager.Instance._allTimePlayers.Values)
        {
            user.IsInGame = false;
            user.IsClanMember = PlayerDataManager.IsClanMember(user.Cmid);
            user.IsFriend = PlayerDataManager.IsFriend(user.Cmid);

            if (CommConnectionManager.CommCenter.TryGetActorWithCmid(user.Cmid, out actor))
                user.SetActor(actor);
            else
                user.SetActor(null);

            ChatManager.Instance._lastgameUsers.Add(user);
        }
        ChatManager.Instance._lastgameUsers.Sort(new CommUserPresenceComparer());
    }

    public void SetNaughtyList(List<CommActorInfo> hackers)
    {
        foreach (var v in hackers)
            _modUsers[v.Cmid] = new CommUser(v);
    }

    public void SetNaughtyList(List<UberStrike.Core.Models.CommActorInfo> hackers)
    {
        foreach (var v in hackers)
            _modUsers[v.Cmid] = new CommUser(v);
    }

    public void AddClanMessage(int cmid, InstantMessage msg)
    {
        ClanDialog.AddMessage(msg);

        if (cmid != PlayerDataManager.Cmid && ChatPageGUI.SelectedTab != TabArea.Clan)
        {
            MarkClanMessageTab = true;
            SfxManager.Play2dAudioClip(SoundEffectType.UINewMessage);
        }
    }

    public void AddNewPrivateMessage(int cmid, InstantMessage msg)
    {
        ChatDialog d;
        try
        {
            if (!_dialogsByCmid.TryGetValue(cmid, out d))
            {
                CommActorInfo user;
                if (CommConnectionManager.CommCenter.TryGetActorWithCmid(cmid, out user))
                {
                    CommUser i = new CommUser(user);

                    //add new dialog
                    d = AddNewDialog(i);

                    // check which panel to insert the dialog
                    if (!_friendUsers.Exists(p => p.Cmid == cmid))
                    {
                        _otherUsers.Add(i);
                    }
                }
                else
                {
                    //CmuneDebug.LogError("Message from " + cmid + " received but user not found");
                    CommUser i = new CommUser(new CommActorInfo(msg.PlayerName, 0, ChannelType.WebPortal) { Cmid = cmid, AccessLevel = (int)msg.AccessLevel });

                    //add new dialog
                    d = AddNewDialog(i);

                    if (!_friendUsers.Exists(p => p.Cmid == cmid))
                    {
                        _otherUsers.Add(i);
                        CommConnectionManager.CommCenter.SendContactList();
                    }
                }
            }

            if (d != null)
            {
                d.AddMessage(msg);

                if (ChatPageGUI.SelectedTab != TabArea.Private || d != _selectedDialog)
                    d.HasUnreadMessage = true;
            }

            if (msg.Cmid != PlayerDataManager.Cmid && ChatPageGUI.SelectedTab != TabArea.Private)
            {
                MarkPrivateMessageTab = true;
                SfxManager.Play2dAudioClip(SoundEffectType.UINewMessage);
            }
        }
        catch (Exception e)
        {
            throw CmuneDebug.Exception(e.InnerException, "AddNewPrivateMessage from cmid={0}", cmid);
        }
    }

    public ChatDialog AddNewDialog(CommUser user)
    {
        ChatDialog d = null;
        if (user != null)
        {
            if (!_dialogsByCmid.TryGetValue(user.Cmid, out d))
            {
                if (PlayerDataManager.IsFriend(user.Cmid))
                    d = new ChatDialog(user, UserGroups.Friend);
                else
                    d = new ChatDialog(user, UserGroups.Other);

                _dialogsByCmid.Add(user.Cmid, d);
            }
        }
        return d;
    }

    internal void RemoveDialog(ChatDialog d)
    {
        _dialogsByCmid.Remove(d.UserCmid);
        _otherUsers.RemoveAll(u => u.Cmid == d.UserCmid);
        _selectedDialog = null;
    }
}