using System;
using System.Collections.Generic;
using System.Text;

public static class PlayerChatLog
{
    private static Dictionary<int, ChatParticipant> _participants = new Dictionary<int, ChatParticipant>();
    private static Dictionary<int, ChatContext> _privateContexts = new Dictionary<int, ChatContext>();
    private static ChatContext _ingameContext = new ChatContext("InGame", ChatContextType.Game, 100);
    private static ChatContext _lobbyContext = new ChatContext("Lobby", ChatContextType.Lobby, 100);

    //static PlayerChatLog()
    //{
    //    _participants.Add(6543, new ChatParticipant(6543, "!..[NZR]Leeness..!"));
    //    _participants.Add(954, new ChatParticipant(954, "Tommy"));
    //    _participants.Add(955, new ChatParticipant(955, "Gabriel"));
    //    _participants.Add(953, new ChatParticipant(953, "Shaun"));
    //    _participants.Add(956, new ChatParticipant(956, "Ludovic"));
    //    _participants.Add(2734, new ChatParticipant(2734, "Alex"));
    //    _participants.Add(9812345, new ChatParticipant(9812345, "Neel3d"));
    //    _participants.Add(2345, new ChatParticipant(2345, "Jordan"));
    //    _participants.Add(666, new ChatParticipant(666, "&#162;&#207;Ambrose"));
    //}

    public static void AddMessage(string message, ChatContextType type)
    {
        ChatContext context = null;
        switch (type)
        {
            case ChatContextType.Lobby:
                context = _lobbyContext;
                break;

            case ChatContextType.Game:
                context = _ingameContext;
                break;
        }

        if (context != null)
            context.Add(string.Format("{0}: {3}", DateTime.Now, message));
    }

    public static void AddIncomingMessage(int senderCmid, string senderName, string message, ChatContextType type)
    {
        if (!_participants.ContainsKey(senderCmid))
        {
            _participants.Add(senderCmid, new ChatParticipant(senderCmid, senderName));
        }

        ChatContext context = null;
        switch (type)
        {
            case ChatContextType.Lobby:
                context = _lobbyContext;
                break;

            case ChatContextType.Game:
                context = _ingameContext;
                break;

            case ChatContextType.Private:
                if (!_privateContexts.TryGetValue(senderCmid, out context))
                {
                    context = new ChatContext("Private Chat with " + senderName, ChatContextType.Private, 30);
                    _privateContexts.Add(senderCmid, context);
                }
                break;
        }

        string prefix = string.Empty;
        if (PlayerDataManager.Cmid == senderCmid)
            prefix = "*** ";

        if (context != null)
            context.Add(string.Format("{0}{1} ({2}) \"{3}\": {4}", prefix, DateTime.Now, senderCmid, senderName, message));
    }

    public static void AddOutgoingPrivateMessage(int recieverCmid, string recieverName, string message, ChatContextType type)
    {
        if (!_participants.ContainsKey(recieverCmid))
        {
            _participants.Add(recieverCmid, new ChatParticipant(recieverCmid, recieverName));
        }

        ChatContext context = null;
        switch (type)
        {
            case ChatContextType.Lobby:
                context = _lobbyContext;
                break;

            case ChatContextType.Game:
                context = _ingameContext;
                break;

            case ChatContextType.Private:
                if (!_privateContexts.TryGetValue(recieverCmid, out context))
                {
                    context = new ChatContext("Private Chat with " + recieverName, ChatContextType.Private, 30);
                    _privateContexts.Add(recieverCmid, context);
                }
                break;
        }

        if (context != null)
            context.Add(string.Format("*** {0} ({1}) \"{2}\": {3}", DateTime.Now, PlayerDataManager.CmidSecure, PlayerDataManager.NameSecure, message));
    }

    public static int ParticipantsCount
    {
        get { return _participants.Count; }
    }

    public static ICollection<ChatParticipant> AllParticipants
    {
        get
        {
            return _participants.Values;
        }
    }

    public static string DumpLogs()
    {
        StringBuilder logs = new StringBuilder();

        //Lobby
        if (_lobbyContext != null)
        {
            logs.AppendLine(_lobbyContext.ToString());
        }

        //Ingame
        if (_ingameContext != null)
        {
            logs.AppendLine(_ingameContext.ToString());
        }

        //Privates
        foreach (ChatContext c in _privateContexts.Values)
        {
            logs.AppendLine(c.ToString());
        }

        return logs.ToString();
    }

    public enum ChatContextType
    {
        Lobby,
        Game,
        Private,
        Clan
    }

    private class ChatContext
    {
        public ChatContext(string title, ChatContextType type, int messageCap)
        {
            Title = title;
            Type = type;
            Messages = new Queue<string>(messageCap);
            MaxMessagesCount = messageCap;
        }

        public void Add(string message)
        {
            Messages.Enqueue(message);
            while (Messages.Count > MaxMessagesCount)
            {
                Messages.Dequeue();
            }
        }

        public override string ToString()
        {
            StringBuilder log = new StringBuilder();

            log.AppendLine(Title);
            foreach (string m in Messages)
            {
                log.AppendLine(m);
            }

            return log.ToString();
        }


        public int MaxMessagesCount = 100;
        public string Title = string.Empty;
        public ChatContextType Type;
        public Queue<string> Messages;
    }

    public struct ChatParticipant
    {
        public ChatParticipant(int cmid, string name)
        {
            Cmid = cmid;
            Name = name;
        }

        public int Cmid;
        public string Name;
    }
}
