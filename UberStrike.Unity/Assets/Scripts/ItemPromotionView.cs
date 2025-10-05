using Cmune.DataCenter.Common.Entities;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class ItemPromotionView
{
    public PromotionItemGUI ItemGui { get; private set; }
    public DynamicTexture Texture { get; private set; }
    public string Title { get; private set; }

    public ItemPromotionView(WeeklySpecialView view)
    {
        Texture = new DynamicTexture(view.ImageUrl);

        Title = view.Title;

        ItemGui = new PromotionItemGUI(ItemManager.Instance.GetItemInShop(view.ItemId), BuyingLocationType.HomeScreen);
    }
}