
using System.Collections.Generic;
using UnityEngine;

public class ChatGroupPanel
{
    public ChatGroupPanel()
    {
        SearchText = string.Empty;
        _groups = new List<ChatGroup>();
    }

    public void AddGroup(UserGroups group, string name, ICollection<CommUser> users)
    {
        _groups.Add(new ChatGroup(group, name, users));
    }

    public Vector2 Scroll { get; set; }
    public string SearchText { get; set; }
    public float ContentHeight { get; set; }
    public IEnumerable<ChatGroup> Groups { get { return _groups; } }

    private readonly List<ChatGroup> _groups;
}