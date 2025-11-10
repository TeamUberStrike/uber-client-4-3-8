using System.Collections.Generic;
using System.Linq;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Photon.Client;
using UberStrike.Core.Models;
using UberStrike.Realtime.Client;
using UnityEngine;

class LobbyRoom : BaseLobbyRoom
{
    Dictionary<int, CommActorInfo> _actors = new Dictionary<int, CommActorInfo>();

    public static bool IsPlayerMuted { get; set; }

    int _totalContactsCount = 0;

    #region RPCs

    protected override void OnClanChatMessage(int cmid, string name, string message)
    {
        InstantMessage msg = new InstantMessage(cmid, cmid, name, message, 0);
        ChatManager.Instance.AddClanMessage(cmid, msg);

        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Clan);
    }

    protected override void OnDisconnectAndDisablePhoton(string message)
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

    protected override void OnFullPlayerListUpdate(System.Collections.Generic.List<UberStrike.Core.Models.CommActorInfo> players)
    {
        _actors.Clear();

        foreach (var data in players)
        {
            _actors.Add(data.Cmid, data);
        }

        ChatManager.Instance.RefreshAll();
    }

    [System.Obsolete]
    protected override void OnGameInviteMessage(int cmid, string message, UberStrike.Core.Models.CmuneRoomID roomId)
    {
        // NOT IMPLEMENTED
    }

    protected override void OnInGameChatMessage(int cmid, string name, string message, MemberAccessLevel accessLevel, byte context)
    {
        ChatContext senderContext = (ChatContext)context;

        //feed HUD
        if (ChatManager.CanShowMessage(senderContext))
        {
            InGameChatHud.Instance.AddChatMessage(name, message, accessLevel);
        }

        //feed communicator
        ChatManager.Instance.InGameDialog.AddMessage(new InstantMessage(cmid, cmid, name, message, accessLevel, senderContext));

        //log conversation for internal use
        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Game);
    }

    protected override void OnLobbyChatMessage(int cmid, string name, string message)
    {
        //determine access level of sender
        MemberAccessLevel level = 0;
        CommActorInfo actor;
        if (_actors.TryGetValue(cmid, out actor))
        {
            level = (MemberAccessLevel)actor.AccessLevel;
        }

        //feed communicator
        InstantMessage msg = new InstantMessage(cmid, cmid, name, message, level);
        ChatManager.Instance.LobbyDialog.AddMessage(msg);

        //log conversation
        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Lobby);
    }

    protected override void OnModerationCustomMessage(string message)
    {
        if (GameState.HasCurrentGame && !GameState.LocalPlayer.IsGamePaused) GameState.LocalPlayer.Pause();
        PopupSystem.ShowMessage("Administrator Message", message, PopupSystem.AlertType.OK, delegate() { });
    }

    protected override void OnModerationKickGame()
    {
        GameStateController.Instance.UnloadGameMode();
        MenuPageManager.Instance.LoadPage(PageType.Home);

        PopupSystem.ShowMessage("ADMIN MESSAGE", "You were kicked out of the game!", PopupSystem.AlertType.OK, delegate() { });
    }

    protected override void OnModerationMutePlayer(bool isPlayerMuted)
    {
        //Debug.LogError("OnModerationMutePlayer " + isPlayerMuted);

        IsPlayerMuted = isPlayerMuted;
        if (isPlayerMuted)
            PopupSystem.ShowMessage("ADMIN MESSAGE", "You have been muted!", PopupSystem.AlertType.OK, delegate() { });
    }

    protected override void OnPlayerHide(int cmid)
    {
        //only hide player from list of active chatters if following condition is matched
        if (!PlayerDataManager.IsClanMember(cmid) && !PlayerDataManager.IsFriend(cmid) && !ChatManager.Instance.HasDialogWith(cmid))
            OnPlayerLeft(cmid, true);
    }

    protected override void OnPlayerJoined(UberStrike.Core.Models.CommActorInfo data)
    {
        Debug.Log("OnPlayerJoined " + data.Cmid);

        CommActorInfo player;
        if (_actors.TryGetValue(data.Cmid, out player))
        {
            player.Sync(data);
        }
        else
        {
            _actors.Add(data.Cmid, data);
        }
    }

    protected override void OnPlayerLeft(int cmid, bool refreshComm)
    {
        Debug.Log("OnPlayerLeft " + cmid);

        CommActorInfo actor;
        if (_actors.TryGetValue(cmid, out actor))
        {
            _actors.Remove(cmid);

            //invalidate information for whom is referencing this instance
            actor.CurrentRoom = new CmuneRoomID();
        }

        ChatManager.Instance.RefreshAll(refreshComm);
    }

    protected override void OnPlayerUpdate(UberStrike.Core.Models.CommActorInfo data)
    {
        CommActorInfo player;
        if (_actors.TryGetValue(data.Cmid, out player))
        {
            player.Sync(data);
        }
        else
        {
            _actors.Add(data.Cmid, data);
        }

        ChatManager.Instance.RefreshAll();
    }

    protected override void OnPrivateChatMessage(int cmid, string name, string message)
    {
        MemberAccessLevel level = 0;

        CommActorInfo actor;
        if (_actors.TryGetValue(cmid, out actor))
        {
            level = actor.AccessLevel;
        }

        InstantMessage msg = new InstantMessage(cmid, cmid, name, message, level);
        ChatManager.Instance.AddNewPrivateMessage(cmid, msg);

        //log conversation
        PlayerChatLog.AddIncomingMessage(cmid, name, message, PlayerChatLog.ChatContextType.Private);
    }

    protected override void OnUpdateActorsForModeration(System.Collections.Generic.List<UberStrike.Core.Models.CommActorInfo> naughtyList)
    {
        ChatManager.Instance.SetNaughtyList(naughtyList);

        SendContactList();
    }

    protected override void OnUpdateClanData()
    {
        ClanDataManager.Instance.CheckCompleteClanData();
    }

    protected override void OnUpdateClanMembers()
    {
        ClanDataManager.Instance.RefreshClanData(true);
    }

    protected override void OnUpdateContacts(System.Collections.Generic.List<UberStrike.Core.Models.CommActorInfo> updated, System.Collections.Generic.List<int> removed)
    {
        //update actors
        foreach (var data in updated)
        {
            OnPlayerJoined(data);
        }

        //remove actors
        foreach (int cmid in removed)
        {
            OnPlayerLeft(cmid, false);
        }

        ChatManager.Instance.RefreshAll(true);
    }

    protected override void OnUpdateFriendsList()
    {
        MonoRoutine.Start(CommsManager.Instance.GetContactsByGroups());
    }

    protected override void OnUpdateInboxMessages(int messageId)
    {
        InboxManager.Instance.GetMessageWithId(messageId);
    }

    protected override void OnUpdateInboxRequests()
    {
        InboxManager.Instance.RefreshAllRequests();
    }

    [System.Obsolete]
    protected override void OnUpdateIngameGroup(System.Collections.Generic.List<int> actorIDs)
    {
        // NOT IMPLEMENTED
    }

    #endregion

    #region Operations

    public void SendContactList()
    {
        var cmids = new HashSet<int>();

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
            Operations.SendSetContactList(cmids.ToList());
    }

    public void UpdatePlayerRoom(CmuneRoomID room)
    {
        Operations.SendUpdatePlayerRoom(room);
    }

    public void ResetPlayerRoom()
    {
        Operations.SendResetPlayerRoom();
    }

    public void SendUpdatedActorInfo()
    {
        //CmuneDebug.LogError("SendUpdatedActorInfo");
        UpdateMyInfo();

        SendMethodToServer(CommRPC.PlayerUpdate, SyncObjectBuilder.GetSyncData(MyInfo, false));
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

    #endregion
}