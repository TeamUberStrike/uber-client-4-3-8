using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Realtime.Common.Synchronization;
using Cmune.Realtime.Photon.Client;
using Cmune.Util;
using UberStrike.Helper;
using UnityEngine;

/// <summary>
/// 
/// </summary>
[NetworkClassAttribute(NetworkClassID.CommCenter)]
public class ClientCommCenter : ClientNetworkClass
{
    public ClientCommCenter(RemoteMethodInterface rmi)
        : base(rmi)
    {
        _actors = new ActorList();

        _myInfo = new CommActorInfo(string.Empty, 0, ChannelType.WebPortal);

        CmuneEventHandler.AddListener<LoginEvent>(OnLoginEvent);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        JoinCommServer();
    }

    protected override void OnUninitialized()
    {
        if (CmuneDebug.IsDebugEnabled)
            CmuneDebug.Log("OnUninitialized ClientCommMode");

        MyInfo.ActorId = -1;
        MyInfo.CurrentRoom = CmuneRoomID.Empty;

        _actors.Clear();
        ChatManager.Instance.RefreshAll();

        base.OnUninitialized();
    }

    public void UpdatePlayerRoom(CmuneRoomID room)
    {
        SendMethodToServer(CommRPC.UpdatePlayerRoom, _rmi.Messenger.PeerListener.ActorIdSecure, room);
    }

    public void ResetPlayerRoom()
    {
        SendMethodToServer(CommRPC.ResetPlayerRoom, _rmi.Messenger.PeerListener.ActorIdSecure);
    }

    private void OnLoginEvent(LoginEvent ev)
    {
        JoinCommServer();
    }

    private void UpdateMyInfo()
    {
        //member data
        MyInfo.PlayerName = string.IsNullOrEmpty(PlayerDataManager.ClanTag) ? PlayerDataManager.NameSecure : string.Format("[{0}] {1}", PlayerDataManager.ClanTag, PlayerDataManager.NameSecure);
        MyInfo.ClanTag = string.IsNullOrEmpty(PlayerDataManager.ClanTag) ? PlayerDataManager.ClanTag : string.Empty;
        MyInfo.Cmid = PlayerDataManager.CmidSecure;
        MyInfo.AccessLevel = (int)PlayerDataManager.AccessLevelSecure;
    }

    private void JoinCommServer()
    {
        if (IsInitialized && PlayerDataManager.IsPlayerLoggedIn)
        {
            UpdateMyInfo();

            //realtime data
            MyInfo.ActorId = _rmi.Messenger.PeerListener.ActorIdSecure;
            MyInfo.CurrentRoom = _rmi.Messenger.PeerListener.CurrentRoom;
            MyInfo.Channel = ApplicationDataManager.Channel;

            //CmuneDebug.LogWarning("JoinCommServer " + MyInfo);

            SendMethodToServer(CommRPC.Join, MyInfo);

            SendContactList();

            if (GameConnectionManager.Client.PeerListener.HasJoinedRoom)
            {
                //when we joined the CommServer AND are currently in a game => update room information
                UpdatePlayerRoom(GameConnectionManager.Client.PeerListener.CurrentRoom);
            }
        }
    }

    public void SendUpdatedActorInfo()
    {
        //CmuneDebug.LogError("SendUpdatedActorInfo");
        UpdateMyInfo();

        SendMethodToServer(CommRPC.PlayerUpdate, SyncObjectBuilder.GetSyncData(MyInfo, false));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="playerID"></param>
    [NetworkMethodAttribute(CommRPC.Leave)]
    protected void OnPlayerLeft(int cmid)
    {
        CommActorInfo actor;
        if (TryGetActorWithCmid(cmid, out actor))
        {
            OnPlayerLeft(actor, true);
        }
    }

    /// <summary>
    /// This method is called by the comm server, if a member of the active chat list is pushed out, because the list reached it's maximum.
    /// But that doesn't mean the user is actually offline, he is just not member of the active chat anymore, until he starts chatting again
    /// </summary>
    /// <param name="cmid"></param>
    [NetworkMethod(CommRPC.HidePlayer)]
    protected void OnPlayerHide(int cmid)
    {
        CommActorInfo actor;
        if (TryGetActorWithCmid(cmid, out actor))
        {
            //only hide player from list of active chatters if following condition is matched
            if (!PlayerDataManager.IsClanMember(cmid) && !PlayerDataManager.IsFriend(cmid) && !ChatManager.Instance.HasDialogWith(cmid))
                OnPlayerLeft(actor, true);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="playerID"></param>
    protected void OnPlayerLeft(CommActorInfo user, bool refreshComm)
    {
        _actors.Remove(user);

        //invalidate information for whom is referencing this instance
        user.CurrentRoom = CmuneRoomID.Empty;
        user.ActorId = -1;

        ChatManager.Instance.RefreshAll(refreshComm);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="player"></param>
    [NetworkMethod(CommRPC.PlayerUpdate)]
    protected void OnPlayerUpdate(SyncObject data)
    {
        CommActorInfo player;
        if (_actors.TryGetByActorId(data.Id, out player))
        {
            player.ReadSyncData(data);
        }
        else
        {
            if (!data.IsEmpty)
            {
                player = new CommActorInfo(data);
                _actors.Add(player);
            }
            //else
            //{
            //    CmuneDebug.LogError("FAILED OnPlayerUpdate: " + data.Id);
            //}
        }

        ChatManager.Instance.RefreshAll();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="updated"></param>
    /// <param name="removed"></param>
    [NetworkMethod(CommRPC.UpdateContacts)]
    protected void OnUpdateContacts(List<SyncObject> updated, List<int> removed)
    {
        //update actors
        foreach (var data in updated)
        {
            OnPlayerJoined(data);
        }

        //remove actors
        foreach (int cmid in removed)
        {
            CommActorInfo user;
            if (_actors.TryGetByCmid(cmid, out user))
            {
                OnPlayerLeft(user, false);
            }
        }

        ChatManager.Instance.RefreshAll(true);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="players"></param>
    [NetworkMethod(CommRPC.FullPlayerListUpdate)]
    protected void OnFullPlayerListUpdate(List<SyncObject> players)
    {
        //Debug.LogWarning("OnFullPlayerListUpdate: " + players.Count);

        _actors.Clear();

        foreach (var data in players)
        {
            if (data.Id == _rmi.Messenger.PeerListener.ActorId)
            {
                MyInfo.ReadSyncData(data);
                _actors.Add(MyInfo);
            }
            else
            {
                _actors.Add(new CommActorInfo(data));
            }
        }

        ChatManager.Instance.RefreshAll();
    }

    private void OnPlayerJoined(SyncObject data)
    {
        CommActorInfo player;
        if (_actors.TryGetByActorId(data.Id, out player))
        {
            player.ReadSyncData(data);
        }
        else
        {
            if (data.Id == _rmi.Messenger.PeerListener.ActorIdSecure)
            {
                MyInfo.ReadSyncData(data);
                player = MyInfo;
            }
            else
            {
                player = new CommActorInfo(data);
            }

            _actors.Add(player);
        }
    }

    [NetworkMethod(CommRPC.ChatMessageInClan)]
    public void OnClanChatMessage(int cmid, int actorId, string name, string message)
    {
        InstantMessage msg = new InstantMessage(cmid, actorId, name, message, 0);
        ChatManager.Instance.AddClanMessage(cmid, msg);

        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Clan);
    }

    [NetworkMethod(CommRPC.ChatMessageInGame)]
    protected void OnInGameChatMessage(int cmid, int actorID, string name, string message, byte accessLevel, byte context)
    {
        ChatContext senderContext = (ChatContext)context;

        //feed HUD
        if (ChatManager.CanShowMessage(senderContext))
        {
            InGameChatHud.Instance.AddChatMessage(name, message, (MemberAccessLevel)accessLevel);
        }

        //feed communicator
        ChatManager.Instance.InGameDialog.AddMessage(new InstantMessage(cmid, actorID, name, message, (MemberAccessLevel)accessLevel, senderContext));

        //log conversation for internal use
        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Game);
    }

    [NetworkMethod(CommRPC.ChatMessageToAll)]
    protected void OnLobbyChatMessage(int cmid, int actorID, string name, string message)
    {
        //determine access level of sender
        MemberAccessLevel level = 0;
        CommActorInfo actor;
        if (_actors.TryGetByCmid(cmid, out actor))
        {
            level = (MemberAccessLevel)actor.AccessLevel;
        }

        //feed communicator
        InstantMessage msg = new InstantMessage(cmid, actorID, name, message, level);
        ChatManager.Instance.LobbyDialog.AddMessage(msg);

        //log conversation
        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Lobby);
    }

    [NetworkMethod(CommRPC.ChatMessageToPlayer)]
    protected void OnPrivateChatMessage(int cmid, int actorID, string name, string message)
    {
        MemberAccessLevel level = 0;
        CommActorInfo actor;

        if (_actors.TryGetByCmid(cmid, out actor))
        {
            level = (MemberAccessLevel)actor.AccessLevel;
        }

        InstantMessage msg = new InstantMessage(cmid, actorID, name, message, level);
        ChatManager.Instance.AddNewPrivateMessage(cmid, msg);

        //log conversation
        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Private);
    }

    [NetworkMethod(CommRPC.GameInviteToPlayer)]
    protected void OnGameInviteMessage(int actorId, string message, CmuneRoomID roomId)
    {
        //{
        //    CmuneDebug.LogError("FAIL because sender not online anymore");
        //}
    }

    [NetworkMethod(CommRPC.DisconnectAndDisablePhoton)]
    public void OnDisconnectAndDisablePhoton(string message)
    {
        if (PhotonClient.IsPhotonEnabled)
        {
            //disable all future photon interaction completely
            PhotonClient.IsPhotonEnabled = false;

            //unload game and disconnect from game server
            if (GameState.HasCurrentGame)
            {
                GameStateController.Instance.UnloadGameMode();
                MenuPageManager.Instance.LoadPage(PageType.Home);
            }

            //disconnect from lobby & comm server
            LobbyConnectionManager.Stop();
            CommConnectionManager.Stop();

            ApplicationDataManager.Instance.LockApplication(message);
        }
    }

    [NetworkMethod(CommRPC.UpdateIngameGroup)]
    protected virtual void OnUpdateIngameGroup(List<int> actorIDs)
    {
        //ignored
    }

    [NetworkMethod(CommRPC.UpdateInboxRequests)]
    protected virtual void OnUpdateInboxRequests()
    {
        InboxManager.Instance.RefreshAllRequests();
    }

    [NetworkMethod(CommRPC.UpdateFriendsList)]
    protected virtual void OnUpdateFriendsList()
    {
        MonoRoutine.Start(CommsManager.Instance.GetContactsByGroups());
    }

    [NetworkMethod(CommRPC.UpdateInboxMessages)]
    protected void OnUpdateInboxMessages(int messageId)
    {
        InboxManager.Instance.GetMessageWithId(messageId);
    }

    [NetworkMethod(CommRPC.UpdateClanMembers)]
    protected void OnUpdateClanMembers()
    {
        ClanDataManager.Instance.RefreshClanData(true);
    }

    [NetworkMethod(CommRPC.UpdateClanData)]
    protected void OnUpdateClanData()
    {
        ClanDataManager.Instance.CheckCompleteClanData();
    }

    [NetworkMethod(CommRPC.UpdateActorsForModeration)]
    public void OnUpdateActorsForModeration(List<SyncObject> allHackers)
    {
        //Debug.LogError("OnUpdateActorsForModeration " + allHackers.Count);

        List<CommActorInfo> hackers = new List<CommActorInfo>(allHackers.Count);

        foreach (SyncObject o in allHackers)
        {
            CommActorInfo i;
            if (_actors.TryGetByActorId(o.Id, out i) && i != null)
            {
                //Debug.LogError("Add Online User " + i.ToString());
                i.ReadSyncData(o);
            }
            else
            {
                i = new CommActorInfo(o);
            }
            hackers.Add(i);
        }

        ChatManager.Instance.SetNaughtyList(hackers);

        SendContactList();
    }

    #region Moderation

    [NetworkMethod(CommRPC.ModerationCustomMessage)]
    public void OnModerationCustomMessage(string message)
    {
        if (GameState.HasCurrentGame && !GameState.LocalPlayer.IsGamePaused) GameState.LocalPlayer.Pause();
        PopupSystem.ShowMessage("Administrator Message", message, PopupSystem.AlertType.OK, delegate() { });
    }

    [NetworkMethod(CommRPC.ModerationMutePlayer)]
    public void OnModerationMutePlayer(bool isPlayerMuted)
    {
        //Debug.LogError("OnModerationMutePlayer " + isPlayerMuted);

        IsPlayerMuted = isPlayerMuted;
        if (isPlayerMuted)
            PopupSystem.ShowMessage("ADMIN MESSAGE", "You have been muted!", PopupSystem.AlertType.OK, delegate() { });
    }

    [NetworkMethod(CommRPC.ModerationKickGame)]
    public void OnModerationKickGame()
    {
        GameStateController.Instance.UnloadGameMode();
        MenuPageManager.Instance.LoadPage(PageType.Home);

        PopupSystem.ShowMessage("ADMIN MESSAGE", "You were kicked out of the game!", PopupSystem.AlertType.OK, delegate() { });
    }

    public void SendModerationBanPlayer(int cmid)
    {
        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.ModerationBanPlayer, PlayerDataManager.CmidSecure, cmid);
    }

    public void SendModerationBanFromCmune(int cmid)
    {
        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.ModerationPermanentBan, PlayerDataManager.CmidSecure, cmid);
    }

    public void SendModerationUnbanPlayer(int cmid)
    {
        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.ModerationUnbanPlayer, cmid);
    }

    public void SendKickFromGame(int actorId)
    {
        CommConnectionManager.CommCenter.SendMethodToPlayer(actorId, CommRPC.ModerationKickGame);
        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.ModerationKickGame, actorId, PlayerDataManager.CmidSecure);
    }

    public void SendCustomMessage(int actorId, string message)
    {
        CommConnectionManager.CommCenter.SendMethodToPlayer(actorId, CommRPC.ModerationCustomMessage, message);
    }

    public void SendMutePlayer(int cmid, int actorId, int minutes)
    {
        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.ModerationMutePlayer, cmid, minutes, actorId, true);
    }

    public void SendGhostPlayer(int cmid, int actorId, int minutes)
    {
        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.ModerationMutePlayer, cmid, minutes, actorId, false);
    }

    public void SendUnmutePlayer(int actorId)
    {
        CommConnectionManager.CommCenter.SendMethodToPlayer(actorId, CommRPC.ModerationMutePlayer, false);
    }

    public void SendUpdateClanMembers(IEnumerable<ClanMemberView> list)
    {
        List<int> clanMembers = new List<int>();
        foreach (var m in list)
        {
            clanMembers.Add(m.Cmid);
        }
        clanMembers.RemoveAll(id => id == PlayerDataManager.CmidSecure);

        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.UpdateClanMembers, clanMembers);
    }

    public void SendRefreshClanData(int cmid)
    {
        CommConnectionManager.CommCenter.SendMethodToServer(CommRPC.UpdateClanData, cmid);
    }

    #endregion

    public void SendContactList()
    {
        HashSet<int> cmids = new HashSet<int>();

        if (PlayerDataManager.Instance.FriendList != null)
        {
            foreach (var view in PlayerDataManager.Instance.FriendList)
                cmids.Add(view.Cmid);
        }

        if (PlayerDataManager.Instance.ClanMembers != null)
        {
            foreach (var view in PlayerDataManager.Instance.ClanMembers)
                cmids.Add(view.Cmid);
        }

        foreach (var view in ChatManager.Instance._modUsers.Values)
            cmids.Add(view.Cmid);

        foreach (var view in ChatManager.Instance.OtherUsers)
            cmids.Add(view.Cmid);

        _totalContactsCount = cmids.Count;

        //Debug.LogWarning("SetContactList " + cmids.Count);
        if (_totalContactsCount > 0)
            SendMethodToServer(CommRPC.SetContactList, PlayerDataManager.CmidSecure, cmids);
    }

    int _totalContactsCount = 0;

    public void UpdateContacts()
    {
        //just don't send any mesage to the server if there are no contacts to update
        if (_totalContactsCount > 0)
            SendMethodToServer(CommRPC.UpdateContacts, PlayerDataManager.CmidSecure);
    }

    public void SendUpdateResetLobby()
    {
        _actors.Clear();
        _actors.Add(MyInfo);
        ChatManager.Instance.RefreshAll();

        SendMethodToServer(CommRPC.FullPlayerListUpdate, _rmi.Messenger.PeerListener.ActorIdSecure);
    }

    public void SendUpdateAllPlayers()
    {
        SendMethodToServer(CommRPC.UpdateAllActors, _rmi.Messenger.PeerListener.ActorIdSecure);
    }

    public void SendSpeedhackDetection(IEnumerable<float> timeDifference)
    {
        SendMethodToServer(CommRPC.SpeedhackDetectionNew, PlayerDataManager.CmidSecure, (object)timeDifference);
    }

    public void UpdateActorsForModeration()
    {
        SendMethodToServer(CommRPC.UpdateActorsForModeration, _rmi.Messenger.PeerListener.ActorIdSecure);
    }

    public void SendClanChatMessage(int cmid, int playerId, string name, string message)
    {
        message = CleanupChatMessage(message);

        if (!string.IsNullOrEmpty(message))
        {
            SendMethodToPlayer(playerId, CommRPC.ChatMessageInClan, cmid, playerId, name, message);
        }
    }

    public bool SendLobbyChatMessage(string message)
    {
        bool result = CheckSpamFilter(message);

        message = CleanupChatMessage(message);

        if (result && !string.IsNullOrEmpty(message))
        {
            OnLobbyChatMessage(PlayerDataManager.CmidSecure, _rmi.Messenger.PeerListener.ActorIdSecure, PlayerDataManager.NameSecure, message);
            SendMethodToServer(CommRPC.ChatMessageToAll, _rmi.Messenger.PeerListener.ActorIdSecure, message);
            return true;
        }
        else
        {
            //Debug.LogWarning("SPAM BLOCKED");
            return false;
        }
    }

    public bool SendInGameChatMessage(string message, ChatContext context)
    {
        bool result = CheckSpamFilter(message);

        message = CleanupChatMessage(message);

        if (result && !string.IsNullOrEmpty(message))
        {
            OnInGameChatMessage(PlayerDataManager.CmidSecure, _rmi.Messenger.PeerListener.ActorIdSecure, PlayerDataManager.NameSecure, message, (byte)PlayerDataManager.AccessLevelSecure, (byte)ChatManager.CurrentChatContext);
            SendMethodToServer(CommRPC.ChatMessageInGame, _rmi.Messenger.PeerListener.ActorIdSecure, message, (byte)ChatManager.CurrentChatContext);
        }

        return result;
    }

    public void SendPrivateChatMessage(CommActorInfo info, string message)
    {
        message = CleanupChatMessage(message);

        if (!string.IsNullOrEmpty(message))
        {
            int cmid = PlayerDataManager.CmidSecure;

            var msg = new InstantMessage(cmid, _rmi.Messenger.PeerListener.ActorIdSecure, PlayerDataManager.NameSecure, message, PlayerDataManager.AccessLevelSecure);
            ChatManager.Instance.AddNewPrivateMessage(info.Cmid, msg);

            SendMethodToServer(CommRPC.ChatMessageToPlayer, _rmi.Messenger.PeerListener.ActorIdSecure, info.ActorId, message);

            //log conversation
            PlayerChatLog.AddOutgoingPrivateMessage(cmid, info.PlayerName, message, PlayerChatLog.ChatContextType.Private);
        }
    }

    private bool CheckSpamFilter(string message)
    {
        bool isMessageSpam = false;
        bool isMessageDuplicate = false;
        float spamRate = 0;
        float lastTime = 0;
        int msgCount = 0;
        string lastMessage = string.Empty;
        foreach (Message s in _lastMessages)
        {
            if (s.Time + 5 > Time.time)
            {
                if (message.StartsWith(s.Text, StringComparison.InvariantCultureIgnoreCase))
                {
                    s.Time = Time.time;
                    s.Count++;
                    isMessageSpam = s.Count > 1;
                    isMessageDuplicate = true;
                }

                if (lastTime != 0)
                {
                    spamRate += Mathf.Clamp(1 - (s.Time - lastTime), 0, 1);
                    msgCount++;
                }
            }
            lastTime = s.Time;
            lastMessage = s.Text;
        }

        //if not duplicate we now add this message to the list
        if (!isMessageDuplicate)
            _lastMessages.Enqueue(new Message(message));

        //avoid plain repeat of text (with basic 10 seconds timeout)
        if (message.Equals(lastMessage, StringComparison.InvariantCultureIgnoreCase) && lastTime + 10 > Time.time)
            isMessageSpam = true;

        //check type frequency
        if (msgCount > 0) spamRate /= msgCount;
        isMessageSpam |= spamRate > 0.3f;

        //Debug.LogWarning(spamRate + " " + isMessageSpam);

        return !isMessageSpam;
    }

    private string CleanupChatMessage(string msg)
    {
        return TextUtilities.ShortenText(TextUtilities.Trim(msg), 140, false);
    }

    public void SendPrivateGameInvitation(CommActorInfo info, string message, CmuneRoomID roomId)
    {
        //CmuneDebug.LogErrorFormat("SendPrivateGameInvitation to {0} on {1}: {2}", info.ActorId, roomId, message);

        SendMethodToPlayer(info.ActorId, CommRPC.GameInviteToPlayer, _rmi.Messenger.PeerListener.ActorIdSecure, message, roomId);
    }

    public void SendPlayerReport(int[] players, MemberReportType type, string details)
    {
        /* Add chat history here */
        string log = ChatManager.Instance.GetAllChatMessagesForPlayerReport();

        SendMethodToServer(CommRPC.ReportPlayers, PlayerDataManager.CmidSecure, players, (int)type, details, log);
    }

    public void SendClearAllFlags(int cmid)
    {
        SendMethodToServer(CommRPC.ClearModeratorFlags, cmid);
    }

    public bool TryGetActorWithCmid(int cmid, out CommActorInfo info)
    {
        return _actors.TryGetByCmid(cmid, out info);
    }

    public bool HasActorWithCmid(int cmid)
    {
        CommActorInfo info;
        return _actors.TryGetByCmid(cmid, out info) && info != null;
    }

    public void UpdateInboxRequest(int actorId)
    {
        SendMethodToPlayer(actorId, CommRPC.UpdateInboxRequests);
    }

    public void NotifyFriendUpdate(int cmid)
    {
        SendMethodToServer(CommRPC.UpdateFriendsList, cmid);
    }

    public void MessageSentWithId(int messageId, int cmid)
    {
        SendMethodToServer(CommRPC.UpdateInboxMessages, cmid, messageId);
    }

    public void SendGetPlayersWithMatchingName(string str)
    {
        _actors.Clear();
        ChatManager.Instance.RefreshAll();

        SendMethodToServer(CommRPC.SendPlayerNameSearchString, PlayerDataManager.CmidSecure, str);
    }

    #region PROPERTIES

    public IEnumerable<CommActorInfo> Players
    {
        get { return _actors.Actors; }
    }

    public int PlayerCount
    {
        get { return _actors.ActorCount; }
    }

    public CommActorInfo MyInfo
    {
        get { return _myInfo; }
    }

    public static bool IsPlayerMuted
    {
        get { return _isPlayerMuted; }
        private set { _isPlayerMuted = value; }
    }

    #endregion

    #region FIELDS

    private LimitedQueue<Message> _lastMessages = new LimitedQueue<Message>(5);

    private CommActorInfo _myInfo;
    private ActorList _actors;

    private static bool _isPlayerMuted = false;

    #endregion

    private class ActorList
    {
        private Dictionary<int, CommActorInfo> _actorsByCmid = new Dictionary<int, CommActorInfo>();
        private Dictionary<int, int> _cmidByActorId = new Dictionary<int, int>();

        public void Add(CommActorInfo actor)
        {
            if (actor == null || actor.Cmid <= 0 || actor.ActorId <= 0) return;

            //remove any possible traces of a previous user with the same actorId
            if (_cmidByActorId.ContainsKey(actor.ActorId))
            {
                _actorsByCmid.Remove(_cmidByActorId[actor.ActorId]);
                _cmidByActorId.Remove(actor.ActorId);
            }
            //remove any possible traces of a previous user with the same cmid
            if (_actorsByCmid.ContainsKey(actor.Cmid))
            {
                _cmidByActorId.Remove(_actorsByCmid[actor.Cmid].ActorId);
                _actorsByCmid.Remove(actor.Cmid);
            }

            _actorsByCmid.Add(actor.Cmid, actor);
            _cmidByActorId.Add(actor.ActorId, actor.Cmid);
        }

        public void Remove(CommActorInfo actor)
        {
            _actorsByCmid.Remove(actor.Cmid);
            _cmidByActorId.Remove(actor.ActorId);
        }

        public bool TryGetByActorId(int actorId, out CommActorInfo actor)
        {
            int cmid;
            actor = null;
            return _cmidByActorId.TryGetValue(actorId, out cmid) && TryGetByCmid(cmid, out actor);
        }

        public bool TryGetByCmid(int cmid, out CommActorInfo actor)
        {
            actor = null;
            return cmid > 0 && _actorsByCmid.TryGetValue(cmid, out actor) && actor != null;
        }

        public void Clear()
        {
            _actorsByCmid.Clear();
            _cmidByActorId.Clear();
        }

        public IEnumerable<CommActorInfo> Actors { get { return _actorsByCmid.Values; } }
        public int ActorCount { get { return _actorsByCmid.Count; } }
    }

    private class Message
    {
        public Message(string text)
        {
            Text = text;
            Time = UnityEngine.Time.time;
        }

        public float Time;
        public string Text;
        public int Count;
    }
}