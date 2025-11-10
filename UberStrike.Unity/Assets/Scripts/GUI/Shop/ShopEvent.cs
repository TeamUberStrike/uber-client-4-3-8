using UnityEngine;
using System.Collections;
using UberStrike.DataCenter.Common.Entities;
using UberStrike.Core.Types;

public class ShopHighlightSlotEvent
{
    public LoadoutSlotType SlotType { get; set; }
}

public class SelectShopAreaEvent
{
    public ShopArea ShopArea { get; set; }
    public UberstrikeItemClass ItemClass { get; set; }
    public UberstrikeItemType ItemType { get; set; }
}

public class SelectLoadoutAreaEvent
{
    public LoadoutArea Area { get; set; }
}

public class LoadoutAreaChangedEvent
{
    public LoadoutArea Area { get; set; }
}

public class SelectShopItemEvent
{
    public IUnityItem Item { get; set; }
}

public class ShopRefreshCurrentItemListEvent
{
    public ShopRefreshCurrentItemListEvent()
    {
        UseCurrentSelection = true;
    }
    public ShopRefreshCurrentItemListEvent(UberstrikeItemClass itemClass, UberstrikeItemType itemType)
    {
        UseCurrentSelection = false;
        ItemClass = itemClass;
        ItemType = itemType;
    }

    public bool UseCurrentSelection { get; private set; }
    public UberstrikeItemClass ItemClass { get; private set; }
    public UberstrikeItemType ItemType { get; private set; }
}

public class ShopBuyEvent
{
    public IUnityItem Item { get; set; }
}

public class ShopTryEvent
{
    public IUnityItem Item { get; set; }
}