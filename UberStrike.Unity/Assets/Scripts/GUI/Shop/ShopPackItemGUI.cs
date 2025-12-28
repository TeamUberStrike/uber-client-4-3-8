using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UnityEngine;

public class ShopConsumableItemGUI : BaseItemGUI
{
    ItemPrice _pointsPrice;
    ItemPrice _creditsPrice;

    public ShopConsumableItemGUI(IUnityItem item, BuyingLocationType location)
        : base(item, location, BuyingRecommendationType.None)
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
                DrawEquipButton(new Rect(rect.width - 100, 7, 46, 46), LocalizedStrings.Equip);
            }
            //else if (!GameState.HasCurrentGame)
            //{
            //    _alpha = Mathf.Lerp(_alpha, selected ? 1 : 0, Time.deltaTime * (selected ? 2 : 10));
            //    GUI.color = new Color(1, 1, 1, _alpha);
            //    DrawTryButton(new Rect(rect.width - 100, 7, 46, 46));
            //    GUI.color = Color.white;
            //}

            DrawBuyButton(new Rect(rect.width - 50, 7, 46, 46), LocalizedStrings.Buy);
            DrawGrayLine(rect);
        }
        GUI.EndGroup();
    }

    private void DrawPackPrice(Rect rect)
    {
        string price = string.Format("{0}", _creditsPrice.Price == 0 ? "FREE" : _creditsPrice.Price.ToString("N0"));

        GUI.DrawTexture(new Rect(rect.x, rect.y, 16, 16), ShopUtils.CurrencyIcon(_creditsPrice.Currency));
        GUI.Label(new Rect(rect.x + 20, rect.y + 3, rect.width - 20, 16), price, BlueStonez.label_interparkmed_11pt_left);
    }
}