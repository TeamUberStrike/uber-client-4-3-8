using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;

public static class ShopSorting
{
    public abstract class ItemComparer<T> : IComparer<T>
    {
        public bool Ascending { get; protected set; }

        public ShopSortedColumns Column { get; set; }

        public void SwitchOrder()
        {
            Ascending = !Ascending;
        }

        public abstract int Compare(T a, T b);
    }

    public class PriceComparer : ItemComparer<BaseItemGUI>
    {
        public PriceComparer()
        {
            Column = ShopSortedColumns.PriceShop;
            Ascending = false;
        }

        public override int Compare(BaseItemGUI a, BaseItemGUI b)
        {
            int result = CompareTag(a.Item, b.Item, false);

            if (result == 0)
                result = ComparePrice(a.Item, b.Item, Ascending);

            return result;
        }
    }

    public class NameComparer : ItemComparer<BaseItemGUI>
    {
        public NameComparer()
        {
            Column = ShopSortedColumns.Name;
            Ascending = true;
        }

        public override int Compare(BaseItemGUI a, BaseItemGUI b)
        {
            return CompareName(a.Item, b.Item, Ascending);
        }
    }

    public class LevelComparer : ItemComparer<BaseItemGUI>
    {
        public LevelComparer()
        {
            Column = ShopSortedColumns.Level;
            Ascending = true;
        }

        public override int Compare(BaseItemGUI a, BaseItemGUI b)
        {
            return CompareLevel(a.Item, b.Item, Ascending);
        }
    }

    public class DurationComparer : ItemComparer<BaseItemGUI>
    {
        public DurationComparer()
        {
            Column = ShopSortedColumns.Duration;
            Ascending = false;
        }

        public override int Compare(BaseItemGUI a, BaseItemGUI b)
        {
            return Compare(a as InventoryItemGUI, b as InventoryItemGUI);
        }

        public int Compare(InventoryItemGUI a, InventoryItemGUI b)
        {
            return CompareDuration(a.InventoryItem, b.InventoryItem, Ascending);
        }
    }

    #region sorting algorithms

    private static int CompareDuration(InventoryItem a, InventoryItem b, bool ascending)
    {
        if (a.IsPermanent && b.IsPermanent)
            return CompareName(a.Item, b.Item, ascending);
        else if (a.IsPermanent) return ascending ? 1 : -1;
        else if (b.IsPermanent) return ascending ? -1 : 1;
        else if (a.DaysRemaining > b.DaysRemaining) return ascending ? 1 : -1;
        else if (a.DaysRemaining < b.DaysRemaining) return ascending ? -1 : 1;
        else return CompareName(a.Item, b.Item, ascending);
    }

    private static int ComparePrice(IUnityItem a, IUnityItem b, bool ascending)
    {
        ItemPrice p1 = ShopUtils.GetLowestPrice(a);
        ItemPrice p2 = ShopUtils.GetLowestPrice(b);

        if (p1.Currency == p2.Currency)
        {
            if (p1.Price > p2.Price) return ascending ? 1 : -1;
            else if (p1.Price < p2.Price) return ascending ? -1 : 1;
            else return CompareName(a, b, ascending);
        }
        else
        {
            if (p1.Currency == UberStrikeCurrencyType.Credits) return -1;
            else return 1;
        }
    }

    private static int CompareLevel(IUnityItem a, IUnityItem b, bool ascending)
    {
        if (a.ItemView.LevelLock < b.ItemView.LevelLock) return ascending ? -1 : 1;
        else if (a.ItemView.LevelLock > b.ItemView.LevelLock) return ascending ? 1 : -1;
        return CompareName(a, b, ascending);
    }

    private static int CompareClass(IUnityItem a, IUnityItem b, bool ascending)
    {
        int r = ascending ? 1 : -1;

        int firstItem = (a.ItemType == UberstrikeItemType.Weapon ? 10 : 100);
        int secondItem = (b.ItemType == UberstrikeItemType.Weapon ? 10 : 100);

        switch (a.ItemClass)
        {
            case UberstrikeItemClass.GearBoots:
                firstItem += 4;
                break;
            case UberstrikeItemClass.GearFace:
                firstItem += 1;
                break;
            case UberstrikeItemClass.GearGloves:
                firstItem += 5;
                break;
            case UberstrikeItemClass.GearHead:
                firstItem += 0;
                break;
            case UberstrikeItemClass.GearLowerBody:
                firstItem += 3;
                break;
            case UberstrikeItemClass.GearUpperBody:
                firstItem += 2;
                break;
            case UberstrikeItemClass.GearHolo:
                firstItem += 6;
                break;
            case UberstrikeItemClass.WeaponCannon:
                firstItem += 1;
                break;
            case UberstrikeItemClass.WeaponHandgun:
                firstItem += 6;
                break;
            case UberstrikeItemClass.WeaponLauncher:
                firstItem += 3;
                break;
            case UberstrikeItemClass.WeaponMachinegun:
                firstItem += 0;
                break;
            case UberstrikeItemClass.WeaponMelee:
                firstItem += 7;
                break;
            case UberstrikeItemClass.WeaponShotgun:
                firstItem += 4;
                break;
            case UberstrikeItemClass.WeaponSniperRifle:
                firstItem += 2;
                break;
            case UberstrikeItemClass.WeaponSplattergun:
                firstItem += 5;
                break;
        }

        switch (b.ItemClass)
        {
            case UberstrikeItemClass.GearBoots:
                secondItem += 4;
                break;
            case UberstrikeItemClass.GearFace:
                secondItem += 1;
                break;
            case UberstrikeItemClass.GearGloves:
                secondItem += 5;
                break;
            case UberstrikeItemClass.GearHead:
                secondItem += 0;
                break;
            case UberstrikeItemClass.GearLowerBody:
                secondItem += 3;
                break;
            case UberstrikeItemClass.GearUpperBody:
                secondItem += 2;
                break;
            case UberstrikeItemClass.GearHolo:
                secondItem += 6;
                break;
            case UberstrikeItemClass.WeaponCannon:
                secondItem += 1;
                break;
            case UberstrikeItemClass.WeaponHandgun:
                secondItem += 6;
                break;
            case UberstrikeItemClass.WeaponLauncher:
                secondItem += 3;
                break;
            case UberstrikeItemClass.WeaponMachinegun:
                secondItem += 0;
                break;
            case UberstrikeItemClass.WeaponMelee:
                secondItem += 7;
                break;
            case UberstrikeItemClass.WeaponShotgun:
                secondItem += 4;
                break;
            case UberstrikeItemClass.WeaponSniperRifle:
                secondItem += 2;
                break;
            case UberstrikeItemClass.WeaponSplattergun:
                secondItem += 5;
                break;
        }

        if (firstItem == secondItem)
            return 0;

        return (firstItem > secondItem ? r : r * (-1));
    }

    private static int CompareName(IUnityItem a, IUnityItem b, bool ascending)
    {
        if (ascending)
        {
            return string.Compare(a.ItemView.Name, b.ItemView.Name);
        }
        else
        {
            return string.Compare(b.ItemView.Name, a.ItemView.Name);
        }
    }

    private static int CompareTag(IUnityItem a, IUnityItem b, bool ascending)
    {
        int r = ascending ? 1 : -1;

        if (a.ItemView.ShopHighlightType == b.ItemView.ShopHighlightType)
            return 0;

        return (a.ItemView.ShopHighlightType > b.ItemView.ShopHighlightType ? r : r * (-1));
    }

    private static int CompareArmorPoints(IUnityItem a, IUnityItem b, bool ascending)
    {
        int r = ascending ? 1 : -1;

        int firstItem = (a.ItemType == UberstrikeItemType.Gear ? ((GearItem)a).Configuration.ArmorPoints : 0);
        int secondItem = (b.ItemType == UberstrikeItemType.Gear ? ((GearItem)b).Configuration.ArmorPoints : 0);

        if (firstItem == secondItem)
            return 0;

        return (firstItem > secondItem ? r : r * (-1));
    }

    #endregion
}