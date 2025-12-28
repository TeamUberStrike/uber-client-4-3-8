
using UnityEngine;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;

public static class ItemPackGuiUtil
{
    public const int Columns = 6;
    public const int Rows = 2;

    public static int DrawIconGrid(Rect rect, List<IUnityItem> items, Vector2 scroll, int currentPage, ItemToolTip itemTooltip = null)
    {
        bool showItemDetails = rect.Contains(Event.current.mousePosition);

        int pageCount = Mathf.FloorToInt(items.Count / (Rows * Columns)) + 1;
        int itemCount = pageCount * Columns;
        GUI.BeginGroup(new Rect(rect));
        {
            Rect topRect, bottomRect;
            int topIndex = 0, bottomIndex = 1;
            for (int i = 0; i < itemCount; i++, topIndex = i * 2, bottomIndex = i * 2 + 1)
            {
                topRect = new Rect(i * 48, 0, 48, 48).OffsetBy(-scroll);
                bottomRect = new Rect(i * 48, 48, 48, 48).OffsetBy(-scroll);

                // Top slot
                if (items.Count > topIndex)
                {
                    GUI.Label(topRect, items[topIndex].Icon, BlueStonez.item_slot_large);
                    if (showItemDetails && topRect.Contains(Event.current.mousePosition))
                    {
                        if (itemTooltip != null)
                        {
                            IUnityItem item = items[topIndex];
                            itemTooltip.SetItem(item, topRect, PopupViewSide.Top, -1, GetDuration(item));
                        }
                    }
                }
                else
                {
                    GUI.Label(topRect, GUIContent.none, BlueStonez.item_slot_large);
                }

                // Bottom slot
                if (items.Count > bottomIndex)
                {
                    GUI.Label(bottomRect, items[bottomIndex].Icon, BlueStonez.item_slot_large);
                    if (showItemDetails && bottomRect.Contains(Event.current.mousePosition))
                    {
                        if (itemTooltip != null)
                        {
                            IUnityItem item = items[bottomIndex];
                            itemTooltip.SetItem(item, bottomRect, PopupViewSide.Top, -1, GetDuration(item));
                        }
                    }
                }
                else
                {
                    GUI.Label(bottomRect, GUIContent.none, BlueStonez.item_slot_large);
                }
            }
        }
        GUI.EndGroup();

        return currentPage;
    }

    public static BuyingDurationType GetDuration(IUnityItem item)
    {
        BuyingDurationType duration = BuyingDurationType.None;

        IEnumerator<ItemPrice> iter = item.ItemView.Prices.GetEnumerator();
        if (iter.MoveNext())
        {
            duration = iter.Current.Duration;
        }

        return duration;
    }
}