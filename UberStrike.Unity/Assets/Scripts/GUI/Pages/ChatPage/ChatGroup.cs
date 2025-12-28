
using System.Collections.Generic;

public class ChatGroup
{
    public ChatGroup(UserGroups group, string title, ICollection<CommUser> players)
    {
        GroupId = group;
        Title = title;
        Players = players;
    }

    public bool HasUnreadMessages()
    {
        if (Players != null)
        {
            foreach (CommUser i in Players)
            {
                ChatDialog d;
                if (ChatManager.Instance._dialogsByCmid.TryGetValue(i.Cmid, out d) && d != null && d.HasUnreadMessage)
                    return true;
            }
        }
        return false;
    }

    public UserGroups GroupId { get; private set; }
    public string Title { get; private set; }
    public ICollection<CommUser> Players { get; private set; }
}