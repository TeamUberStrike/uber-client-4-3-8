using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;
using Cmune.Util;

public class PromotionItemGUI : BaseItemGUI
{
    ItemPrice _pointsPrice;
    ItemPrice _creditsPrice;
    ItemToolTip _tooltip;

    public PromotionItemGUI(IUnityItem item, BuyingLocationType location)
        : base(item, location, BuyingRecommendationType.Manual)
    {
        _pointsPrice = ShopUtils.GetLowestPrice(item, UberStrikeCurrencyType.Points);
        _creditsPrice = ShopUtils.GetLowestPrice(item, UberStrikeCurrencyType.Credits);
        _tooltip = new ItemToolTip();
    }

    public override void Draw(Rect rect, bool selected)
    {
        GUI.BeginGroup(rect);
        {
            DrawIcon(new Rect(4, 4, 48, 48));
            DrawArmorOverlay();
            DrawPromotionalTag();
            DrawName(new Rect(63, 10, 220, 20));
            DrawPrice(new Rect(63, 30, 220, 20), _pointsPrice, _creditsPrice);

            if (PlayerDataManager.PlayerLevel < Item.ItemView.LevelLock)
            {
                GUI.color = new Color(1, 1, 1, 0.1f);
                GUI.DrawTexture(new Rect(rect.width - 100, 7, 46, 46), UberstrikeIcons.LevelLock);
                GUI.color = Color.white;
            }

            if (InventoryManager.Instance.IsItemInInventory(Item.ItemId))
            {
                DrawEquipButton(new Rect(rect.width - 100, 7, 46, 46), LocalizedStrings.Equip);
            }
            else if (!GameState.HasCurrentGame)
            {
                if (Item.ItemType == UberstrikeItemType.Weapon || (Item.ItemType == UberstrikeItemType.Gear && Item.ItemClass != UberstrikeItemClass.GearHolo))
                {
                    if (GUI.Button(new Rect(rect.width - 100, 7, 46, 46), new GUIContent(LocalizedStrings.Try), BlueStonez.buttondark_medium))
                    {
                        MenuPageManager.Instance.LoadPage(PageType.Shop);

                        CmuneEventHandler.Route(new SelectShopAreaEvent()
                        {
                            ShopArea = ShopArea.Shop,
                            ItemClass = Item.ItemClass,
                            ItemType = Item.ItemType,
                        });

                        CmuneEventHandler.Route(new ShopTryEvent() { Item = this.Item });
                    }
                }
            }

            DrawBuyButton(new Rect(rect.width - 50, 7, 46, 46), LocalizedStrings.Buy);

            _tooltip.SetItem(Item, new Rect(4, 4, 48, 48), PopupViewSide.Left, -1, ItemPackGuiUtil.GetDuration(Item));
            _tooltip.OnGui();
        }
        GUI.EndGroup();
    }
}