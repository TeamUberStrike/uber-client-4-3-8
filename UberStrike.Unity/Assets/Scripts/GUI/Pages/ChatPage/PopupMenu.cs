
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class PopupMenu
{
    private class MenuItem
    {
        public string Item;
        public Action<CommUser> Callback;
        public IsEnabledForUser CheckItem;

        public bool Enabled;
    }

    public PopupMenu()
    {
        _items = new List<MenuItem>();
    }

    public void AddMenuItem(string item, Action<CommUser> action, IsEnabledForUser isEnabledForUser)
    {
        MenuItem menu = new MenuItem();

        menu.Item = item;
        menu.Callback = action;
        menu.CheckItem = isEnabledForUser;
        menu.Enabled = false;

        _items.Add(menu);
    }

    private void Configure()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].Enabled = _items[i].CheckItem(_selectedUser);
        }
    }

    public static void Hide()
    {
        Current = null;
    }

    public void Show(Vector2 screenPos, CommUser user)
    {
        Show(screenPos, user, this);
    }

    public static void Show(Vector2 screenPos, CommUser user, PopupMenu menu)
    {
        if (menu != null)
        {
            menu._selectedUser = user;

            menu.Configure();

            menu._position.height = Height * menu._items.Count;
            menu._position.width = Width;
            menu._position.x = screenPos.x - 1;

            if (screenPos.y + menu._position.height > Screen.height)
                menu._position.y = screenPos.y - menu._position.height + 1;
            else
                menu._position.y = screenPos.y - 1;

            Current = menu;
        }
    }

    public void Draw()
    {
        GUI.BeginGroup(new Rect(_position.x, _position.y, _position.width, _position.height + 6), BlueStonez.window);
        {
            GUI.Label(new Rect(1, 1, _position.width - 2, _position.height + 4), GUIContent.none, BlueStonez.box_grey50);
            GUI.Label(new Rect(0, 0, _position.width, _position.height + 6), GUIContent.none, BlueStonez.box_grey50);

            for (int i = 0; i < _items.Count; i++)
            {
                GUITools.PushGUIState();
                GUI.enabled = _items[i].Enabled;

                GUI.Label(new Rect(8, 8 + i * Height, _position.width - 8, Height), _items[i].Item, BlueStonez.label_interparkmed_11pt_left);
                if (GUI.Button(new Rect(2, 3 + i * Height, _position.width - 4, Height), GUIContent.none, BlueStonez.dropdown_list))
                {
                    Current = null;
                    _items[i].Callback(_selectedUser);
                }
                GUITools.PopGUIState();
            }
        }
        GUI.EndGroup();

        if (Event.current.type == EventType.MouseUp && !_position.Contains(Event.current.mousePosition))
        {
            Current = null;
        }
    }

    private const int Height = 24;
    private const int Width = 105;
    private Rect _position;
    private List<MenuItem> _items;
    private CommUser _selectedUser;

    public static PopupMenu Current { get; private set; }
    public static bool IsEnabled { get { return Current != null; } }

    public delegate bool IsEnabledForUser(CommUser user);
}