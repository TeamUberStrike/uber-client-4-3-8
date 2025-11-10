using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class LotteryItemGrid
{
    protected const int MAX_COLUMN = 6;

    protected ItemToolTip _tooltip;
    protected List<LotteryItem> _items;

    public List<bool> HighlightState { get; set; }

    public bool Show { get; set; }

    public List<LotteryItem> Items
    {
        get { return _items; }
    }

    public LotteryItemGrid(List<BundleItemView> items, int credits, int points)
    {
        _items = new List<LotteryItem>(items.Count + 2);
        foreach (var item in items)
        {
            _items.Add(new LotteryItem(item));
        }

        if (credits > 0)
        {
            _items.Add(new LotteryItem(UberStrikeCurrencyType.Credits, credits));
        }

        if (points > 0)
        {
            _items.Add(new LotteryItem(UberStrikeCurrencyType.Points, points));
        }
    }

    public void SetTooltip(ItemToolTip tooltip)
    {
        _tooltip = tooltip;
    }

    float offset = -300;

    public void Draw(Rect rect)
    {
        float size = rect.width / MAX_COLUMN;
        int rows = _items.Count / MAX_COLUMN + ((_items.Count % MAX_COLUMN) > 0 ? 1 : 0);

        offset = Show ? Mathf.Lerp(offset, 0, Time.deltaTime * 5) : Mathf.Lerp(offset, -rows * size, Time.deltaTime * 5);

        GUI.BeginGroup(rect);
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < MAX_COLUMN; col++)
            {
                int i = row * MAX_COLUMN + col;
                Rect itemRect = new Rect(col * size, rect.height - (row + 1) * size - offset, size, size);

                if (i < _items.Count)
                {
                    if (HighlightState != null)
                    {
                        if (HighlightState[i])
                        {
                            GUI.Button(itemRect, _items[i].Icon, BlueStonez.item_slot_small);

                            GUI.color = GUI.color.SetAlpha(GUITools.FastSinusPulse);
                            GUI.DrawTexture(itemRect, UberstrikeTextures.ItemSlotSelected);
                            GUI.color = Color.white;

                            if (_tooltip != null && Show && itemRect.Contains(Event.current.mousePosition) && offset < size)
                                _tooltip.SetItem(_items[i].UnityItem, itemRect, PopupViewSide.Top, -1, _items[i].Duration);
                        }
                        else
                        {
                            GUI.enabled = false;
                            GUI.Label(itemRect, _items[i].Icon, BlueStonez.item_slot_alpha);
                            GUI.enabled = true;
                        }
                    }
                    else
                    {
                        GUI.Button(itemRect, _items[i].Icon, BlueStonez.item_slot_alpha);
                        if (_tooltip != null && Show && itemRect.Contains(Event.current.mousePosition) && offset < size)
                            _tooltip.SetItem(_items[i].UnityItem, itemRect, PopupViewSide.Top, -1, _items[i].Duration);
                    }
                }
                else
                {

                    GUI.enabled = false;
                    GUI.Label(itemRect, GUIContent.none, BlueStonez.item_slot_alpha);
                    GUI.enabled = true;
                }
            }
        }
        GUI.EndGroup();
    }
}