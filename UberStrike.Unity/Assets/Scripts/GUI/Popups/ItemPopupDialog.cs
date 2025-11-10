using Cmune.Util;
using UnityEngine;
using System;

public class ItemPopupDialog : IPopupDialog
{
    const int Width = 400;
    const int Height = 300;

    private IUnityItem _item;
    private string _caption = "OK";
    private Action _action;

    private int _frameCount;
    private float _alpha;

    public string Text { get; set; }

    public string Title { get; set; }

    public GuiDepth Depth { get { return GuiDepth.Popup; } }

    public ItemPopupDialog(string title, string text, IUnityItem items, string caption, Action action)
    {
        Title = title;
        Text = text;

        _item = items;
        _caption = caption;
        _action = action;

        InventoryManager.Instance.HighlightItem(_item.ItemId, true);
    }

    private static void OpenInventory()
    {
        MenuPageManager.Instance.LoadPage(PageType.Shop);
        CmuneEventHandler.Route(new SelectShopAreaEvent() { ShopArea = ShopArea.Inventory});
    }

    public ItemPopupDialog(string title, string text, IUnityItem items)
        : this(title, text, items, LocalizedStrings.Inventory, OpenInventory)
    { }

    public void OnGUI()
    {
        UpdateAlpha();

        GUI.color = GUI.color.SetAlpha(_alpha);

        float offsetX = (Screen.width - Width) * 0.5f;
        float offsetY = GlobalUIRibbon.HEIGHT + (Screen.height - GlobalUIRibbon.HEIGHT - Height) * 0.5f;
        GUI.BeginGroup(new Rect(offsetX, offsetY, Width, Height), BlueStonez.window);
        {
            GUITools.OutlineLabel(new Rect(0, 10, Width, 40), Title, BlueStonez.label_interparkbold_32pt, 1, Color.white, ColorScheme.GuiTeamBlue);
            GUI.Label(new Rect(0, 50, Width, 20), Text, BlueStonez.label_interparkbold_16pt);

            GUI.DrawTexture(new Rect(Width * 0.5f - 24, 80, 48, 48), _item.Icon);

            //Name
            GUI.Label(new Rect(17, 150, Width - 34, 20), _item.Name, BlueStonez.label_interparkbold_16pt);

            //Description
            if (_item.ItemView != null)
            {
                string str = _item.ItemView.Description;

                if (string.IsNullOrEmpty(str))
                    str = "No description available.";

                GUI.Label(new Rect(17, 170, Width - 34, 80), str, BlueStonez.label_interparkmed_11pt);
            }

            if (GUI.Button(new Rect(17, Height - 54, Width - 34, 32), _caption, BlueStonez.button_green))
            {
                PopupSystem.HideMessage(this);
                if (_action != null) _action();
            }
        }
        GUI.EndGroup();

        GUI.color = Color.white;
    }

    public void OnHide()
    {
    }

    private void UpdateAlpha()
    {
        if (_frameCount != Time.frameCount)
        {
            _frameCount = Time.frameCount;
            _alpha = Mathf.Clamp01(_alpha + Time.deltaTime * 3);
        }
    }
}