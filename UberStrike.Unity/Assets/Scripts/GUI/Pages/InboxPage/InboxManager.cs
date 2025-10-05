
using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UnityEngine;
using UberStrike.Helper;
using System.Collections;

/// <summary>
/// pulls messages from server
/// </summary>
public class InboxManager : Singleton<InboxManager>
{
    #region Properties
    public bool IsInitialized { get; private set; }
    public IList<InboxThread> AllThreads { get { return _sortedAllThreads; } }
    public int ThreadCount { get { return _sortedAllThreads.Count; } }
    public bool IsLoadingThreads { get; private set; }
    public bool IsNoMoreThreads { get; private set; }

    public float NextInboxRefresh { get; private set; }
    public float NextRequestRefresh { get; private set; }
    #endregion

    #region Fields
    public int _unreadMessageCount;
    private Dictionary<int, InboxThread> _allThreads = new Dictionary<int, InboxThread>();
    private List<InboxThread> _sortedAllThreads = new List<InboxThread>();
    private int _curThreadsPageIndex = 0;
    public List<ContactRequestView> _friendRequests = new List<ContactRequestView>();
    public List<GroupInvitationView> _incomingClanRequests = new List<GroupInvitationView>();
    public List<GroupInvitationView> _outgoingClanRequests = new List<GroupInvitationView>();
    #endregion

    public void Initialize()
    {
        if (!IsInitialized)
        {
            IsInitialized = true;

            LoadNextPageThreads();
            RefreshAllRequests();
        }
    }

    public void SendPrivateMessage(int cmidId, string name, string rawMessage)
    {
        string msg = TextUtilities.ShortenText(TextUtilities.Trim(rawMessage), 140, false);

        if (!string.IsNullOrEmpty(msg))
        {
            if (!_allThreads.ContainsKey(cmidId))
            {
                InboxThread thread = new InboxThread(new MessageThreadView()
                {
                    HasNewMessages = false,
                    ThreadName = name,
                    LastMessagePreview = string.Empty,
                    ThreadId = cmidId,
                    LastUpdate = DateTime.Now,
                    MessageCount = 0
                });

                _allThreads.Add(thread.ThreadId, thread);
                _sortedAllThreads.Add(thread);
            }

            UberStrike.WebService.Unity.PrivateMessageWebServiceClient.SendMessage(PlayerDataManager.CmidSecure, cmidId, msg,
                (pm) => OnPrivateMessageSent(cmidId, pm),
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, LocalizedStrings.YourMessageHasNotBeenSent);
                });

        }
        else
        {
            //CmuneDebug.LogWarning("Message not sent because empty");
        }
    }

    public void UpdateNewMessageCount()
    {
        _sortedAllThreads.Sort((t1, t2) => t2.LastMessageDateTime.CompareTo(t1.LastMessageDateTime));

        _unreadMessageCount = 0;
        foreach (InboxThread th in _sortedAllThreads)
        {
            if (th.HasUnreadMessage)
            {
                _unreadMessageCount++;
            }
        }
    }

    public bool HasUnreadMessages
    {
        get { return _unreadMessageCount > 0; }
    }

    public bool HasUnreadRequests
    {
        get { return _incomingClanRequests.Count > 0 || _friendRequests.Count > 0; }
    }

    public void RemoveFriend(int friendCmid)
    {
        PlayerDataManager.Instance.RemoveFriend(friendCmid);
        UberStrike.WebService.Unity.RelationshipWebServiceClient.DeleteContact(PlayerDataManager.CmidSecure, friendCmid,
            (ev) =>
            {
                if (ev == MemberOperationResult.Ok)
                {
                    CommConnectionManager.CommCenter.NotifyFriendUpdate(friendCmid);
                    CommsManager.Instance.UpdateCommunicator();
                }
                else
                {
                    CmuneDebug.LogError("DeleteContact failed with: " + ev);
                }
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            });
    }

    public void AcceptContactRequest(int requestId)
    {
        _friendRequests.RemoveAll(r => r.RequestId == requestId);
        UberStrike.WebService.Unity.RelationshipWebServiceClient.AcceptContactRequest(requestId, PlayerDataManager.CmidSecure, CommonConfig.ApplicationIdUberstrike,
            (view) =>
            {
                if (view != null && view.ActionResult == 0 && view.Contact != null)
                {
                    PlayerDataManager.Instance.AddFriend(view.Contact);

                    CommConnectionManager.CommCenter.NotifyFriendUpdate(view.Contact.Cmid);
                    CommsManager.Instance.UpdateCommunicator();
                }
                else
                {
                    PopupSystem.ShowMessage(LocalizedStrings.Clan, "Failed accepting friend request", PopupSystem.AlertType.OK);
                }
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            }
            );
    }

    public void DeclineContactRequest(int requestId)
    {
        _friendRequests.RemoveAll(r => r.RequestId == requestId);
        UberStrike.WebService.Unity.RelationshipWebServiceClient.DeclineContactRequest(requestId, PlayerDataManager.CmidSecure,
            (ev) =>
            {
                //Debug.Log("DeclineContactRequest " + ev.ActionResult);
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            });
    }

    public void AcceptClanRequest(int requestId)
    {
        _incomingClanRequests.RemoveAll(r => r.GroupInvitationId == requestId);
        UberStrike.WebService.Unity.ClanWebServiceClient.AcceptClanInvitation(requestId, PlayerDataManager.CmidSecure,
            (ev) =>
            {
                if (ev != null && ev.ActionResult == 0)
                {
                    //ask the user to go to the clan page
                    PopupSystem.ShowMessage(LocalizedStrings.Clan, LocalizedStrings.JoinClanSuccessMsg, PopupSystem.AlertType.OKCancel,
                        () => MenuPageManager.Instance.LoadPage(PageType.Clans), "Go to Clans",
                        null, "Not now",
                        PopupSystem.ActionType.Positive);

                    ClanDataManager.Instance.SetClanData(ev.ClanView);

                    CommConnectionManager.CommCenter.SendUpdateClanMembers(PlayerDataManager.Instance.ClanMembers);
                }
                else
                {
                    PopupSystem.ShowMessage(LocalizedStrings.Clan, LocalizedStrings.JoinClanErrorMsg, PopupSystem.AlertType.OK);
                }
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            }
            );
    }

    public void DeclineClanRequest(int requestId)
    {
        _incomingClanRequests.RemoveAll(r => r.GroupInvitationId == requestId);
        UberStrike.WebService.Unity.ClanWebServiceClient.DeclineClanInvitation(requestId, PlayerDataManager.CmidSecure,
            (ev) =>
            {
                //Debug.Log("DeclineClanRequest " + ev.ActionResult);
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            }
        );
    }

    #region Private

    private InboxManager()
    { }

    internal void LoadNextPageThreads()
    {
        if (!IsNoMoreThreads || NextInboxRefresh - Time.time < 0)
        {
            IsLoadingThreads = true;
            NextInboxRefresh = Time.time + 30;

            UberStrike.WebService.Unity.PrivateMessageWebServiceClient.GetAllMessageThreadsForUser(PlayerDataManager.CmidSecure, _curThreadsPageIndex,
                OnFinishLoadingNextPageThreads,
                (ex) =>
                {
                    DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                });
        }
    }

    private void OnFinishLoadingNextPageThreads(List<MessageThreadView> listView)
    {
        IsLoadingThreads = false;
        if (listView.Count > 0)
        {
            ++_curThreadsPageIndex;
            OnGetThreads(listView);
            IsNoMoreThreads = false;
        }
        else
        {
            IsNoMoreThreads = true;
        }
    }

    internal void LoadMessagesForThread(InboxThread inboxThread, int pageIndex)
    {
        inboxThread.IsLoading = true;
        UberStrike.WebService.Unity.PrivateMessageWebServiceClient.GetThreadMessages(PlayerDataManager.CmidSecure, inboxThread.ThreadId, pageIndex,
            (list) =>
            {
                inboxThread.IsLoading = false;
                OnGetMessages(inboxThread.ThreadId, list);
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
    }

    private void OnGetThreads(List<MessageThreadView> threadView)
    {
        foreach (var i in threadView)
        {
            InboxThread thread;
            if (_allThreads.TryGetValue(i.ThreadId, out thread))
            {
                thread.UpdateThread(i);
            }
            else
            {
                thread = new InboxThread(i);
                _allThreads.Add(thread.ThreadId, thread);
                _sortedAllThreads.Add(thread);
            }
        }

        UpdateNewMessageCount();
    }

    private void OnGetMessages(int threadId, List<PrivateMessageView> messages)
    {
        InboxThread thread;
        if (_allThreads.TryGetValue(threadId, out thread))
        {
            thread.AddMessages(messages);
        }
        else
        {
            CmuneDebug.LogError("Getting messages of non existing thread " + threadId);
        }
    }

    private void OnPrivateMessageSent(int threadId, PrivateMessageView privateMessage)
    {
        if (privateMessage != null)
        {
            CommConnectionManager.CommCenter.MessageSentWithId(privateMessage.PrivateMessageId, privateMessage.ToCmid);

            privateMessage.IsRead = true;
            AddMessageToThread(threadId, privateMessage);
        }
        else
        {
            CmuneDebug.LogError("PrivateMessage sending failed");
            PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.YourMessageHasNotBeenSent);
        }
    }

    private void AddMessage(PrivateMessageView privateMessage)
    {
        if (privateMessage != null)
        {
            AddMessageToThread(privateMessage.FromCmid, privateMessage);
        }
        else
        {
            CmuneDebug.LogError("AddMessage called with NULL message");
        }
    }

    private void AddMessageToThread(int threadId, PrivateMessageView privateMessage)
    {
        InboxThread thread;
        if (!_allThreads.TryGetValue(threadId, out thread))
        {
            thread = new InboxThread(new MessageThreadView()
            {
                ThreadName = privateMessage.FromName,
                ThreadId = threadId,
            });

            _allThreads.Add(thread.ThreadId, thread);
            _sortedAllThreads.Add(thread);
        }

        thread.AddMessage(privateMessage);

        UpdateNewMessageCount();
    }

    internal void MarkThreadAsRead(int threadId)
    {
        UberStrike.WebService.Unity.PrivateMessageWebServiceClient.MarkThreadAsRead(PlayerDataManager.CmidSecure, threadId,
            () =>
            {
                //Debug.Log("Thread marked as read: " + threadId);
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        UpdateNewMessageCount();
    }

    internal void DeleteThread(int threadId)
    {
        UberStrike.WebService.Unity.PrivateMessageWebServiceClient.DeleteThread(PlayerDataManager.CmidSecure, threadId,
            () =>
            {
                OnDeleteThread(threadId);
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace, "There was a problem. Please try again later.");
            });
    }

    private void OnDeleteThread(int threadId)
    {
        _allThreads.Remove(threadId);
        _sortedAllThreads.RemoveAll(t => t.ThreadId == threadId);

        UpdateNewMessageCount();
    }

    internal void GetMessageWithId(int messageId)
    {
        UberStrike.WebService.Unity.PrivateMessageWebServiceClient.GetMessageWithId(messageId, PlayerDataManager.CmidSecure,
            AddMessage,
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
    }

    internal void RefreshAllRequests()
    {
        NextRequestRefresh = Time.time + 30;

        UberStrike.WebService.Unity.RelationshipWebServiceClient.GetContactRequests(PlayerDataManager.CmidSecure,
            OnGetContactRequests,
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        UberStrike.WebService.Unity.ClanWebServiceClient.GetAllGroupInvitations(PlayerDataManager.CmidSecure, CommonConfig.ApplicationIdUberstrike,
            OnGetAllGroupInvitations,
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });

        if (PlayerDataManager.Instance.RankInClan != GroupPosition.Member)
        {
            UberStrike.WebService.Unity.ClanWebServiceClient.GetPendingGroupInvitations(PlayerDataManager.ClanIDSecure,
            OnGetPendingGroupInvitations,
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
        }
    }

    private void OnGetContactRequests(List<ContactRequestView> requests)
    {
        _friendRequests = requests;

        if (_friendRequests.Count > 0)
        {
            SfxManager.Play2dAudioClip(SoundEffectType.UINewRequest);
        }
    }

    private void OnGetAllGroupInvitations(List<GroupInvitationView> requests)
    {
        _incomingClanRequests = requests;

        if (_incomingClanRequests.Count > 0)
        {
            SfxManager.Play2dAudioClip(SoundEffectType.UINewRequest);
        }
    }

    private void OnGetPendingGroupInvitations(List<GroupInvitationView> requests)
    {
        _outgoingClanRequests = requests;
    }

    #endregion
}