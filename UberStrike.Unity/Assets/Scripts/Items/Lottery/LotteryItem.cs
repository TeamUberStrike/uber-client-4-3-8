using Cmune.DataCenter.Common.Entities;
using UnityEngine;

public class LotteryItem
{
    public Texture Icon { get; private set; }
    public BuyingDurationType Duration { get; private set; }
    public IUnityItem UnityItem { get; private set; }
    public int ItemId { get; private set; }
    public int Credits { get; private set; }
    public int Points { get; private set; }

    public LotteryItem(BundleItemView itemView)
    {
        var unityItem = ItemManager.Instance.GetItemInShop(itemView.ItemId);
        if (unityItem != null)
        {
            Icon = unityItem.Icon;
        }
        else
        {
            Icon = UberstrikeIcons.White;
        }

        Points = 0;
        Credits = 0;
        UnityItem = unityItem;
        ItemId = itemView.ItemId;
        Duration = itemView.Duration;
    }

    public LotteryItem(UberStrikeCurrencyType currency, int price)
    {
        switch (currency)
        {
            case UberStrikeCurrencyType.Credits:
                Icon = UberstrikeTextures.CreditsIcon48x48;
                UnityItem = new CreditsUnityItem(price);
                Points = 0;
                Credits = price;
                break;
            case UberStrikeCurrencyType.Points:
                Icon = UberstrikeTextures.Points48x48;
                UnityItem = new PointsUnityItem(price);
                Credits = 0;
                Points = price;
                break;
            default:
                Icon = UberstrikeIcons.White;
                UnityItem = null;
                Points = 0;
                Credits = 0;
                break;
        }

        ItemId = 0;
        Duration = BuyingDurationType.None;
    }
}