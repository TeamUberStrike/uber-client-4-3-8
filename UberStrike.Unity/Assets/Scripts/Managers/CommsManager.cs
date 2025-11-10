using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using Cmune.Util;
using UberStrike.Helper;

public class CommsManager : Singleton<CommsManager>
{
    public float NextFriendsRefresh { get; private set; }

    private CommsManager() { }

    public void SendFriendRequest(int cmid, string message)
    {
        message = TextUtilities.ShortenText(TextUtilities.Trim(message), 140, false);
        UberStrike.WebService.Unity.RelationshipWebServiceClient.SendContactRequest(PlayerDataManager.CmidSecure, cmid, message,
            (ev) =>
            {
                CommActorInfo actor;
                if (CommConnectionManager.CommCenter.TryGetActorWithCmid(cmid, out actor))
                    CommConnectionManager.CommCenter.UpdateInboxRequest(actor.ActorId);
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
    }

    public IEnumerator GetContactsByGroups()
    {
        NextFriendsRefresh = Time.time + 30;

        yield return UberStrike.WebService.Unity.RelationshipWebServiceClient.GetContactsByGroups(PlayerDataManager.CmidSecure, UberStrikeCommonConfig.ApplicationId,
            (ev) =>
            {
                List<PublicProfileView> allFriends = new List<PublicProfileView>();

                foreach (var group in ev)
                {
                    foreach (var contact in group.Contacts)
                        allFriends.Add(contact);
                }
                PlayerDataManager.Instance.FriendList = allFriends;

                UpdateCommunicator();
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
    }

    public void UpdateCommunicator()
    {
        //set friends list on comserver to get regular updates on request
        CommConnectionManager.CommCenter.SendContactList();

        //update all friends list
        ChatManager.Instance.UpdateFriendSection();
    }
}