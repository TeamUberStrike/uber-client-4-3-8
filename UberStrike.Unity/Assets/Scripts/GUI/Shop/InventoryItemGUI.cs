using System;
using Cmune.DataCenter.Common.Entities;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public class InventoryItemGUI : BaseItemGUI
{
    private float _alpha = 0;

    public InventoryItem InventoryItem { get; private set; }

    public InventoryItemGUI(InventoryItem item, BuyingLocationType location)
        : base(item.Item, location, BuyingRecommendationType.None)
    {
        InventoryItem = item;
    }

    public override void Draw(Rect rect, bool selected)
    {
        DrawHighlightedBackground(rect);

        GUI.BeginGroup(rect);
        {
            DrawIcon(new Rect(4, 4, 48, 48));
            DrawName(new Rect(63, 10, 220, 20));
            DrawDaysRemaining(new Rect(63, 30, 220, 20));

            if (Item.ItemId == CommonConfig.NameChangeItem)
            {
                DrawUseButton(new Rect(rect.width - 100, 7, 46, 46));
            }
            else if (ItemManager.IsItemEquippable(Item.ItemId))
            {
                if (InventoryItem.IsPermanent || InventoryItem.DaysRemaining > 0)
                {
                    DrawEquipButton(new Rect(rect.width - 100, 7, 46, 46), LocalizedStrings.Equip);
                }
                else if (!GameState.HasCurrentGame)
                {
                    _alpha = Mathf.Lerp(_alpha, selected ? 1 : 0, Time.deltaTime * (selected ? 2 : 10));
                    GUI.color = new Color(1, 1, 1, _alpha);
                    DrawTryButton(new Rect(rect.width - 100, 7, 46, 46));
                    GUI.color = Color.white;
                }
            }

            if (Item.ItemView.IsForSale)
            {
                if (!InventoryItem.IsPermanent)
                    DrawBuyButton(new Rect(rect.width - 50, 7, 46, 46), LocalizedStrings.Renew, ShopArea.Inventory);
                else if (InventoryItem.AmountRemaining >= 0)
                    DrawBuyButton(new Rect(rect.width - 50, 7, 46, 46), LocalizedStrings.Buy, ShopArea.Inventory);
            }

            DrawGrayLine(rect);

            if (selected)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
                if (Item.ItemType == UberstrikeItemType.Weapon)
                    GUI.Label(new Rect(12, 60, 32, 32), UberstrikeIcons.GetIconForItemClass(Item.ItemClass), GUIStyle.none);
                else if (Item.ItemType == UberstrikeItemType.Gear)
                    GUI.Label(new Rect(12, 60, 32, 32), UberstrikeIcons.GetIconForItemClass(Item.ItemClass), GUIStyle.none);
                GUI.color = Color.white;

                DrawDescription(new Rect(55, 60, 255, 52));

                if (DetailGUI != null)
                {
                    DetailGUI.Draw();
                }
            }
        }
        GUI.EndGroup();
    }

    public void DrawHighlightedBackground(Rect rect)
    {
        if (InventoryItem.IsHighlighted)
        {
            GUI.color = ColorConverter.RgbaToColor(255, 255, 255, 20 * GUITools.FastSinusPulse);
            GUI.DrawTexture(rect, UberstrikeIcons.White);
            GUI.color = Color.white;
        }
    }

    public void DrawDaysRemaining(Rect rect)
    {
        bool drawTimerIcon = true;

        Color remainingColor = Color.white;
        string remaingDays = string.Empty;

        if (InventoryItem.AmountRemaining >= 0)
        {
            if (InventoryItem.AmountRemaining == 1)
            {
                remaingDays = InventoryItem.AmountRemaining + " use remaining";
            }
            else
            {
                remaingDays = InventoryItem.AmountRemaining + " uses remaining";
            }
            drawTimerIcon = false;
        }
        else if (InventoryItem.IsPermanent)
        {
            remaingDays = LocalizedStrings.Permanent;
        }
        else if (InventoryItem.DaysRemaining > 1 && InventoryItem.DaysRemaining < 5)
        {
            // Less than 5 days remaining
            remainingColor = ColorScheme.UberStrikeYellow;
            remaingDays = string.Format("{0} {1}{2}", InventoryItem.DaysRemaining.ToString(), LocalizedStrings.Day, InventoryItem.DaysRemaining == 1 ? string.Empty : "s");
        }
        else if (InventoryItem.DaysRemaining == 1)
        {
            // Last Day
            remainingColor = ColorScheme.UberStrikeYellow;
            remaingDays = LocalizedStrings.LastDay;
        }
        else if (InventoryItem.DaysRemaining <= 0)
        {
            // Expired
            remainingColor = ColorScheme.UberStrikeRed;
            remaingDays = LocalizedStrings.Expired;
        }
        else
        {
            // 5 or more days remaining
            remaingDays = string.Format("{0} {1}{2}", InventoryItem.DaysRemaining.ToString(), LocalizedStrings.Day, InventoryItem.DaysRemaining == 1 ? string.Empty : "s");
        }

        if (drawTimerIcon)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, 16, 16), UberstrikeIcons.ItemExpiration);
        }

        // Days Remaining Label
        GUI.color = remainingColor;
        GUI.Label(new Rect(rect.x + (drawTimerIcon ? 20 : 0), rect.y + 3, rect.width - 20, 16), remaingDays, BlueStonez.label_interparkmed_11pt_left);
        GUI.color = Color.white;
    }
}