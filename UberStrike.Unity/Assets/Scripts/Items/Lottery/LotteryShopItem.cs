using UnityEngine;
using UberStrike.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;

public abstract class LotteryShopItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DynamicTexture Icon { get; set; }
    public ItemPrice Price { get; set; }
    public BundleCategoryType Category { get; set; }

    public abstract string Description { get; }
    public abstract void Use();
}