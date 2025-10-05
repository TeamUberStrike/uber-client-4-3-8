using Cmune.DataCenter.Common.Entities;
using System;

public class InstantMessage
{
    public int Cmid { get; private set; }
    public int ActorID { get; private set; }
    public string PlayerName { get; private set; }
    public string MessageText { get; private set; }
    public string MessageDateTime { get; private set; }
    public MemberAccessLevel AccessLevel { get; private set; }
    public bool IsFriend { get; private set; }
    public bool IsClan { get; private set; }
    public ChatContext Context { get; private set; }

    public InstantMessage(int cmid, int actorID, string playerName, string messageText, MemberAccessLevel level)
        : this(cmid, actorID, playerName, messageText, level, ChatContext.None)
    { }

    public InstantMessage(int cmid, int actorID, string playerName, string messageText, MemberAccessLevel level, ChatContext context)
    {
        Cmid = cmid;
        ActorID = actorID;
        PlayerName = playerName;
        MessageText = messageText;
        AccessLevel = level;
        MessageDateTime = DateTime.Now.ToString("t");
        Context = context;

        IsFriend = PlayerDataManager.IsFriend(Cmid);
        IsClan = PlayerDataManager.IsClanMember(Cmid);
    }

    public InstantMessage(InstantMessage instantMessage)
    {
        Cmid = instantMessage.Cmid;
        ActorID = instantMessage.ActorID;
        PlayerName = instantMessage.PlayerName;
        MessageText = instantMessage.MessageText;
        MessageDateTime = instantMessage.MessageDateTime;
        AccessLevel = instantMessage.AccessLevel;
        Context = instantMessage.Context;

        IsFriend = PlayerDataManager.IsFriend(Cmid);
        IsClan = PlayerDataManager.IsClanMember(Cmid);
    }
}