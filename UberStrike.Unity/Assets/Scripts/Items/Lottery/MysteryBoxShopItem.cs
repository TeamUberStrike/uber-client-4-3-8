using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;

public class MysteryBoxShopItem : LotteryShopItem
{
    public DynamicTexture Image { get; set; }
    public MysteryBoxUnityView View { get; set; }
    public List<IUnityItem> Items { get; set; }

    public override string Description
    {
        get { return View.Description; }
    }

    public override void Use()
    {
        LotteryManager.Instance.RunMysteryBox(this);
    }
}