using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UnityEngine;

public class InGameItemGUI : BaseItemGUI
{
    private string _promotionalText = string.Empty;

    public InGameItemGUI(IUnityItem item, string promotionalText, BuyingLocationType location, BuyingRecommendationType recommendation)
        : base(item, location, recommendation)
    {
        _promotionalText = promotionalText;
    }

    public override void Draw(Rect rect, bool selected)
    {
        GUI.BeginGroup(rect);
        {
            DrawIcon(new Rect(4, (rect.height - 48) / 2, 48, 48));

            GUI.contentColor = ColorScheme.UberStrikeYellow;
            GUI.Label(new Rect(60, rect.height / 2 - 18, rect.width, 18), _promotionalText, BlueStonez.label_interparkbold_16pt_left);
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(60, rect.height / 2 + 2, rect.width, 16), Item.Name, BlueStonez.label_interparkbold_16pt_left);

            InventoryItem inventoryItem;
            if (InventoryManager.Instance.TryGetInventoryItem(Item.ItemId, out inventoryItem))
            {
                if (LoadoutManager.Instance.IsItemEquipped(Item.ItemId))
                {
                    GUI.Label(new Rect(rect.width - 80, rect.height / 2 - 25, 80, 22), new GUIContent("EQUIPPED", UberstrikeIcons.CheckMarkForEquippedItem), BlueStonez.label_interparkbold_11pt_left);
                }
                else
                {
                    DrawEquipButton(new Rect(rect.width - 80, rect.height / 2 - 25, 80, 22), "EQUIP NOW");
                }
            }
            else
            {
                DrawBuyButton(new Rect(rect.width - 80, rect.height / 2 - 25, 80, 22), "BUY NOW");
            }
        }
        GUI.EndGroup();

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            CmuneEventHandler.Route(new SelectShopItemEvent() { Item = Item });
        }
    }
}