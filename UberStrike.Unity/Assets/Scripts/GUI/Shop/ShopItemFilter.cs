using UnityEngine;
using System.Collections;
using UberStrike.DataCenter.Common.Entities;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Types;

public interface IShopItemFilter
{
    bool CanPass(IUnityItem item);
}

public class InventoryItemFilter : IShopItemFilter
{
    public bool CanPass(IUnityItem item)
    {
        //don;t show default items in the inventory
        return !LoadoutManager.Instance.IsItemEquipped(item.ItemId)
            //hide default gear
             && item.ItemId != UberStrikeCommonConfig.DefaultHead && item.ItemId != UberStrikeCommonConfig.DefaultFace
             && item.ItemId != UberStrikeCommonConfig.DefaultGloves && item.ItemId != UberStrikeCommonConfig.DefaultUpperBody
             && item.ItemId != UberStrikeCommonConfig.DefaultLowerBody && item.ItemId != UberStrikeCommonConfig.DefaultBoots;

    }
}

public class SpecialItemFilter : IShopItemFilter
{
    public bool CanPass(IUnityItem item)
    {
        return item.ItemView.ShopHighlightType != ItemShopHighlightType.None;
    }
}

public class ItemByTypeFilter : IShopItemFilter
{
    private UberstrikeItemType _itemType;

    public ItemByTypeFilter(UberstrikeItemType itemType)
    {
        _itemType = itemType;
    }

    public bool CanPass(IUnityItem item)
    {
        return item.ItemType == _itemType;
    }
}

public class ItemByClassFilter : IShopItemFilter
{
    private UberstrikeItemType _itemType;
    private UberstrikeItemClass _itemClass;

    public ItemByClassFilter(UberstrikeItemType itemType, UberstrikeItemClass itemClass)
    {
        _itemType = itemType;
        _itemClass = itemClass;
    }

    public bool CanPass(IUnityItem item)
    {
        return item.ItemType == _itemType && item.ItemClass == _itemClass;
    }
}