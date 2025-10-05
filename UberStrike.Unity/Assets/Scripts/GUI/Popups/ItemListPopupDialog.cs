using System.Collections.Generic;
using Cmune.Util;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

/// <summary>
/// 
/// </summary>
public class ItemListPopupDialog : BasePopupDialog
{
    private List<IUnityItem> _items;

    private ItemListPopupDialog()
    {
        _cancelCaption = LocalizedStrings.OkCaps;
        _alertType = PopupSystem.AlertType.Cancel;
    }

    public ItemListPopupDialog(string title, string text, List<IUnityItem> items)
        : this()
    {
        Title = title;
        Text = text;

        _size.y = 320;
        _items = new List<IUnityItem>(items);

        foreach (IUnityItem i in _items)
            if (i != null) InventoryManager.Instance.HighlightItem(i.ItemId, true); //i.IsHighlighted = true;

        //don;t show the items but offer to go to the inventory
        if (items.Count > 1)
        {
            _alertType = PopupSystem.AlertType.OK;
            _actionType = PopupSystem.ActionType.Positive;
            _okCaption = LocalizedStrings.Inventory;

            _callbackOk = delegate
            {
                MenuPageManager.Instance.LoadPage(PageType.Shop);
                CmuneEventHandler.Route(new SelectShopAreaEvent() { ShopArea = ShopArea.Inventory });
            };
        }
    }

    public ItemListPopupDialog(IUnityItem item)
        : this()
    {
        Title = LocalizedStrings.NewItem;

        if (item != null)
        {
            _items = new List<IUnityItem>() { item };

            foreach (IUnityItem i in _items)
                if (i != null) InventoryManager.Instance.HighlightItem(i.ItemId, true); // i.IsHighlighted = true;

            if (item.ItemType == UberstrikeItemType.Gear || item.ItemType == UberstrikeItemType.Weapon || item.ItemType == UberstrikeItemType.QuickUse)
            {
                _alertType = PopupSystem.AlertType.OKCancel;
                _actionType = PopupSystem.ActionType.Positive;

                _okCaption = LocalizedStrings.Equip;
                _cancelCaption = LocalizedStrings.NotNow;

                _callbackOk = () =>
                {
                    IUnityItem i = _items[0];
                    if (i != null)
                    {
                        InventoryManager.Instance.EquipItem(i.ItemId);

                        CmuneEventHandler.Route(new UpdateRecommendationEvent());
                    }
                };

                _callbackCancel = () => CmuneEventHandler.Route(new UpdateRecommendationEvent());
            }
        }
        else
        {
            _items = new List<IUnityItem>();
        }
    }

    protected override void DrawPopupWindow()
    {
        if (_items.Count == 0)
        {
            GUI.Label(new Rect(17, 115, _size.x - 34, 20), "There are no items", BlueStonez.label_interparkbold_13pt);
        }
        else if (_items.Count == 1)
        {
            if (_items[0] != null)
            {
                //Icon
                GUI.Label(new Rect(_size.x * 0.5f - 32, 55, 64, 64), _items[0].Icon);

                //Name
                GUI.Label(new Rect(17, 115, _size.x - 34, 20), _items[0].Name, BlueStonez.label_interparkbold_13pt);

                //Description
                if (_items[0].ItemView != null)
                {
                    string str = _items[0].ItemView.Description;

                    if (string.IsNullOrEmpty(str))
                        str = "No description available.";

                    GUI.Label(new Rect(17, 140, _size.x - 34, 40), str, BlueStonez.label_interparkmed_11pt);
                }
            }
        }
        else if (_items.Count <= 4)
        {
            DrawItemsInColumns(2);
        }
        else if (_items.Count <= 6)
        {
            DrawItemsInColumns(3);
        }
        else if (_items.Count <= 8)
        {
            DrawItemsInColumns(4);
        }
        else
        {
            GUI.Label(new Rect(17, 150, _size.x - 34, 20), Text, BlueStonez.label_interparkbold_13pt);
        }
    }

    private void DrawItemsInColumns(int columns)
    {
        int i = 0, j = 0;
        float xstart = _size.x * 0.5f - (64 * columns / 2f) - (15 * (columns - 1) / 2f);
        foreach (IUnityItem item in _items)
        {
            if (item != null)
            {
                GUI.Label(new Rect(xstart + (i % columns * 79), 55 + (j * 70), 64, 64), item.Icon, BlueStonez.label_interparkbold_11pt);
                GUI.Label(new Rect(xstart + (i % columns * 79) - 7, 110 + (j * 70), 79, 20), item.Name, BlueStonez.label_interparkmed_11pt);
            }

            i++;
            j = i / columns;
        }

        GUI.Label(new Rect(17, 220, _size.x - 34, 40), Text, BlueStonez.label_interparkbold_13pt);
    }
}