using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;

public class ShopRentItemGUI : BaseItemGUI
{
    private float _alpha = 0;
    ItemPrice _pointsPrice;
    ItemPrice _creditsPrice;

    public ShopRentItemGUI(IUnityItem item, BuyingLocationType location, BuyingRecommendationType recommendation = BuyingRecommendationType.None)
        : base(item, location, recommendation)
    {
        _pointsPrice = ShopUtils.GetLowestPrice(item, UberStrikeCurrencyType.Points);
        _creditsPrice = ShopUtils.GetLowestPrice(item, UberStrikeCurrencyType.Credits);
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
                if (!LoadoutManager.Instance.IsItemEquipped(Item.ItemId))
                {
                    DrawEquipButton(new Rect(rect.width - 100, 7, 46, 46), LocalizedStrings.Equip);
                }
            }
            else if (!GameState.HasCurrentGame)
            {
                if (Item.ItemType == UberstrikeItemType.Weapon || (Item.ItemType == UberstrikeItemType.Gear && Item.ItemClass != UberstrikeItemClass.GearHolo))
                {
                    _alpha = Mathf.Lerp(_alpha, selected ? 1 : 0, Time.deltaTime * (selected ? 2 : 10));
                    GUI.color = new Color(1, 1, 1, _alpha);
                    DrawTryButton(new Rect(rect.width - 100, 7, 46, 46));
                    GUI.color = Color.white;
                }
            }

            DrawBuyButton(new Rect(rect.width - 50, 7, 46, 46), LocalizedStrings.Buy);
            DrawGrayLine(rect);
        }
        GUI.EndGroup();
    }
}