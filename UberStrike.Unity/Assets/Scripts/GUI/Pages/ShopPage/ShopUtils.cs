
using System.Collections.Generic;
using System.Text;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public static class ShopUtils
{
    public static ItemPrice GetLowestPrice(IUnityItem item, UberStrikeCurrencyType currency = UberStrikeCurrencyType.None)
    {
        ItemPrice ret = null;
        foreach (var p in item.ItemView.Prices)
        {
            if ((currency == UberStrikeCurrencyType.None || p.Currency == currency) && (ret == null || ret.Price > p.Price))
                ret = p;
        }
        return ret;
    }

    public static string PrintDuration(BuyingDurationType duration)
    {
        switch (duration)
        {
            case BuyingDurationType.Permanent: return " " + LocalizedStrings.Permanent;
            case BuyingDurationType.OneDay: return " 1 " + LocalizedStrings.Day;
            case BuyingDurationType.SevenDays: return " 1 " + LocalizedStrings.Week;
            case BuyingDurationType.ThirtyDays: return " 1 " + LocalizedStrings.Month;
            case BuyingDurationType.NinetyDays: return " " + LocalizedStrings.ThreeMonths;
            default: return string.Empty;
        }
    }

    public static string PrintCurrency(UberStrikeCurrencyType currency)
    {
        switch (currency)
        {
            case UberStrikeCurrencyType.Points: return LocalizedStrings.Points;
            case UberStrikeCurrencyType.Credits: return LocalizedStrings.Credits;
            default: return string.Empty;
        }
    }

    public static Texture2D CurrencyIcon(UberStrikeCurrencyType currency)
    {
        switch (currency)
        {
            case UberStrikeCurrencyType.Credits: return UberstrikeTextures.IconCredits20x20;
            case UberStrikeCurrencyType.Points: return UberstrikeTextures.IconPoints20x20;
            default: return null;
        }
    }

    public static WeaponItem GetRecommendedWeaponForMap(CombatRangeTier mapCombatRange, int playerLevel, PlayerStatisticsView stats, List<WeaponItem> inventory, List<WeaponItem> loadout)
    {
        if (inventory == null) inventory = new List<WeaponItem>();
        if (loadout == null) loadout = new List<WeaponItem>();

        //make sure the player level is at least 2
        playerLevel = Mathf.Max(2, playerLevel);

        var candidates = new List<KeyValuePair<WeaponItem, ItemPrice>>();

        var sortByDamage = new List<KeyValuePair<UberstrikeItemClass, int>>()
        {
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponMelee, stats.WeaponStatistics.MeleeTotalDamageDone ),
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponMachinegun, stats.WeaponStatistics.MachineGunTotalDamageDone ),
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponLauncher  , stats.WeaponStatistics.LauncherTotalDamageDone ),
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponCannon, stats.WeaponStatistics.CannonTotalDamageDone  ),
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponHandgun , stats.WeaponStatistics.HandgunTotalDamageDone ),
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponSniperRifle , stats.WeaponStatistics.SniperTotalDamageDone ),
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponSplattergun , stats.WeaponStatistics.SplattergunTotalDamageDone ),
            new KeyValuePair<UberstrikeItemClass, int>( UberstrikeItemClass.WeaponShotgun, stats.WeaponStatistics.ShotgunTotalDamageDone  ),
        };
        sortByDamage.Sort((a, b) => -a.Value.CompareTo(b.Value));
        var damageDone = sortByDamage.ConvertAll<UberstrikeItemClass>(a => a.Key);

        KeyValuePair<WeaponItem, ItemPrice> cheapestFallback = new KeyValuePair<WeaponItem, ItemPrice>(null, new ItemPrice() { Price = int.MaxValue });

        foreach (WeaponItem weapon in ItemManager.Instance.GetShopItems(UberstrikeItemType.Weapon, BuyingMarketType.Shop))
        {
            //check if item is enabled & supported
            if (weapon.ItemView.IsForSale)
            {
                //check the weapon combat range
                if ((weapon.Configuration.CombatRange & mapCombatRange.RangeCategory) != 0 && !InventoryManager.Instance.IsItemInInventory(weapon.ItemId))
                {
                    var creditsPrice = GetLowestPrice(weapon, UberStrikeCurrencyType.Credits);
                    var pointsPrice = GetLowestPrice(weapon, UberStrikeCurrencyType.Points);

                    if (weapon.ItemView.LevelLock <= playerLevel && cheapestFallback.Value.Price > pointsPrice.Price)
                    {
                        cheapestFallback = new KeyValuePair<WeaponItem, ItemPrice>(weapon, pointsPrice);
                    }

                    if (pointsPrice.Price > 0 && pointsPrice.Price <= PlayerDataManager.Points && weapon.ItemView.LevelLock <= playerLevel)
                    {
                        candidates.Add(new KeyValuePair<WeaponItem, ItemPrice>(weapon, pointsPrice));
                    }
                    else if (creditsPrice.Price > 0 && creditsPrice.Price <= PlayerDataManager.Credits)
                    {
                        candidates.Add(new KeyValuePair<WeaponItem, ItemPrice>(weapon, creditsPrice));
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            if (cheapestFallback.Key != null)
            {
                return cheapestFallback.Key;
            }
            else
            {
                return ItemManager.Instance.GetWeaponItemInShop((int)DefaultWeaponId.WeaponMachinegun);
            }
        }
        else
        {
            try
            {
                //sort by DPS
                candidates.Sort((a, b) => -b.Value.Price.CompareTo(a.Value.Price));
                candidates.Sort((a, b) => -damageDone.IndexOf(b.Key.ItemClass).CompareTo(damageDone.IndexOf(a.Key.ItemClass)));

                StringBuilder builder = new StringBuilder();
                candidates.ForEach(w => builder.AppendLine(w.Key.ItemView.LevelLock + " " + w.Key.Name + " " + w.Key.Configuration.DPS + " " + w.Key.ItemClass + " " + w.Key.Configuration.CombatRange + " " + w.Value.Currency + " " + w.Value.Price));
                builder.AppendLine("--");
                damageDone.ForEach(w => builder.AppendLine(w.ToString()));
            }
            catch
            {
                //candidates.ForEach(w => CmuneDebug.LogWarning("GetRecommendedWeaponForMap Item:{0} with ID:{1} has ItemView:{2}", w.Key != null, (w.Key != null ? w.Key.ItemId : 0), (w.Key != null ? w.Key.ItemView != null : false)));
                throw;
            }

            return candidates[0].Key;
        }
    }

    public static GearItem GetRecommendedArmor(int playerLevel, HoloGearItem holo, GearItem upper, GearItem lower)
    {
        int holoAp = holo != null ? holo.Configuration.ArmorPoints : 0;
        int upperAp = upper != null ? upper.Configuration.ArmorPoints : 0;
        int lowerAp = lower != null ? lower.Configuration.ArmorPoints : 0;

        //make sure the player level is at least 2
        playerLevel = Mathf.Max(2, playerLevel);

        var candidates = new List<KeyValuePair<GearItem, ItemPrice>>();

        KeyValuePair<GearItem, ItemPrice> cheapestFallback = new KeyValuePair<GearItem, ItemPrice>(null, new ItemPrice() { Price = int.MaxValue });

        foreach (IUnityItem item in ItemManager.Instance.GetShopItems(UberstrikeItemType.Gear, BuyingMarketType.Shop))
        {
            GearItem gear = item as GearItem;

            if (gear != null)
            {
                //is for sale
                if (gear.Configuration.IsForSale)
                {
                    bool armorIsBetter = (gear.ItemClass == UberstrikeItemClass.GearHolo && gear.Configuration.ArmorPoints >= holoAp && gear != holo) ||
                                         (gear.ItemClass == UberstrikeItemClass.GearUpperBody && gear.Configuration.ArmorPoints >= upperAp && gear != upper) ||
                                         (gear.ItemClass == UberstrikeItemClass.GearLowerBody && gear.Configuration.ArmorPoints >= lowerAp && gear != lower);

                    //check if item is enabled & supported
                    if (gear.Configuration.ArmorPoints > 0 && armorIsBetter)
                    {
                        var price = GetLowestPrice(gear);

                        if (gear.Configuration.LevelLock <= playerLevel && cheapestFallback.Value.Price > price.Price)
                        {
                            cheapestFallback = new KeyValuePair<GearItem, ItemPrice>(gear, price);
                        }

                        if (gear.Configuration.LevelLock <= playerLevel && price.Currency == UberStrikeCurrencyType.Points && price.Price <= PlayerDataManager.Points)
                        {
                            if (armorIsBetter)
                                candidates.Add(new KeyValuePair<GearItem, ItemPrice>(gear, price));
                        }
                        else if (price.Currency == UberStrikeCurrencyType.Credits && price.Price <= PlayerDataManager.Credits)
                        {
                            if (armorIsBetter)
                                candidates.Add(new KeyValuePair<GearItem, ItemPrice>(gear, price));
                        }
                    }
                }
            }
        }

        if (candidates.Count == 0)
        {
            if (cheapestFallback.Key != null)
            {
                return cheapestFallback.Key;
            }
            else
            {
                if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.GearUpperBody))
                    return LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.GearUpperBody).Item as GearItem;
                else
                    return ItemManager.Instance.GetGearItemInShop(0, UberstrikeItemClass.GearUpperBody) as GearItem;
            }
        }
        else
        {
            try
            {
                //sort by price
                candidates.Sort(new PriceComparer<GearItem>());

                //StringBuilder builder = new StringBuilder();
                //candidates.ForEach(w => builder.AppendLine(w.Key.ItemView.LevelLock + " " + w.Key.Name + " " + w.Key.ItemClass + " " + w.Value.Currency + " " + w.Value.Value));
            }
            catch
            {
                //candidates.ForEach(w => CmuneDebug.LogWarning("GetRecommendedArmor Item:{0} with ID:{1} has ItemView:{2}", w.Key != null, (w.Key != null ? w.Key.ItemId : 0), (w.Key != null ? w.Key.ItemView != null : false)));
                throw;
            }

            return candidates[0].Key;
        }
    }

    public class PriceComparer<T> : IComparer<KeyValuePair<T, ItemPrice>>
    {
        public int Compare(KeyValuePair<T, ItemPrice> x, KeyValuePair<T, ItemPrice> y)
        {
            int aval = x.Value.Price + (x.Value.Currency == UberStrikeCurrencyType.Credits ? 1000000 : 0);
            int bval = y.Value.Price + (y.Value.Currency == UberStrikeCurrencyType.Credits ? 1000000 : 0);
            return bval.CompareTo(aval);
        }
    }

    private class DescendedComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return y - x;
        }
    }

    public static bool IsMeleeWeapon(IUnityItem view)
    {
        if (view != null && view.ItemView != null)
        {
            return view.ItemView.ItemClass == UberstrikeItemClass.WeaponMelee;
        }
        else
        {
            return false;
        }
    }

    public static bool IsInstantHitWeapon(IUnityItem view)
    {
        if (view != null && view.ItemView != null)
        {
            return view.ItemView.ItemClass == UberstrikeItemClass.WeaponHandgun ||
                   view.ItemView.ItemClass == UberstrikeItemClass.WeaponMachinegun ||
                   view.ItemView.ItemClass == UberstrikeItemClass.WeaponShotgun ||
                   view.ItemView.ItemClass == UberstrikeItemClass.WeaponSniperRifle;
        }
        else
        {
            return false;
        }
    }

    public static bool IsProjectileWeapon(IUnityItem view)
    {
        if (view != null && view.ItemView != null)
        {
            return view.ItemView.ItemClass == UberstrikeItemClass.WeaponCannon ||
                   view.ItemView.ItemClass == UberstrikeItemClass.WeaponLauncher ||
                   view.ItemView.ItemClass == UberstrikeItemClass.WeaponSplattergun;
        }
        else
        {
            return false;
        }
    }

    public static string GetRecommendationString(RecommendType recommendation)
    {
        switch (recommendation)
        {
            case RecommendType.MostEfficient:
                return LocalizedStrings.MostEfficientWeaponCaps;
            case RecommendType.RecommendedArmor:
                return LocalizedStrings.RecommendedArmorCaps;
            case RecommendType.StaffPick:
                return LocalizedStrings.StaffPickCaps;
            case RecommendType.RecommendedWeapon:
                return LocalizedStrings.RecommendedWeaponCaps;
            default:
                return string.Empty;
        }
    }
}