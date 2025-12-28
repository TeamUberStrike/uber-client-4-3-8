
using System.Collections.Generic;
using System.Text;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UnityEngine;
using ItemOffer = System.Collections.Generic.KeyValuePair<WeaponItem, UberStrike.Core.Models.Views.ItemPrice>;

public static class RecommendationUtils
{
    public class WeaponRecommendation
    {
        public string Debug;

        public bool IsComplete { get; set; }
        public WeaponItem ItemWeapon { get; set; }
        public CombatRangeCategory CombatRange { get; set; }
        public LoadoutSlotType LoadoutSlot { get; set; }
        public ItemPrice Price { get; set; }
    }

    public static WeaponRecommendation GetRecommendedWeapon(int playerLevel, CombatRangeTier mapCombatRange, List<WeaponItem> loadout = null, List<WeaponItem> inventory = null)
    {
        if (loadout == null)
        {
            loadout = new List<WeaponItem>(4);
            if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponMelee)) loadout.Add(LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponMelee).Item as WeaponItem);
            if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponPrimary)) loadout.Add(LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponPrimary).Item as WeaponItem);
            if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponSecondary)) loadout.Add(LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponSecondary).Item as WeaponItem);
            if (LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponTertiary)) loadout.Add(LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponTertiary).Item as WeaponItem);
        }

        if (inventory == null)
        {
            inventory = new List<WeaponItem>();
            foreach (var weapon in InventoryManager.Instance.GetAllItems(false))
            {
                if (weapon.Item is WeaponItem && (weapon.DaysRemaining > 0 || weapon.IsPermanent))
                {
                    inventory.Add(weapon.Item as WeaponItem);
                }
            }
        }

        var recommendation = CheckMyLoadout(mapCombatRange, loadout, inventory);
        recommendation.Debug += "[RC: ";

        //the loadout is basically complete, now we try to improve your current set of weapons
        if (recommendation.IsComplete)
        {
            int step = 0;
            foreach (var nextWeakest in GetWeakestItemsInLoadout(loadout, mapCombatRange))
            {
                var offer = GetNextBestAffordableWeapon(nextWeakest, playerLevel, inventory);
                if (offer.Key != null)
                {
                    recommendation.ItemWeapon = offer.Key;
                    recommendation.Price = offer.Value;
                    recommendation.Debug += string.Format("Better in Class, try:{0}] ", step);

                    return recommendation;
                }
                step++;
            }

            //we coudln't find any affordable weapon that is better than the curently equipped.
            // --> find the weapon, that is just slightly better than our existing one, ignoring the price
            var fallbackOffer = GetNextBestWeapon(GetWeakestItemsInLoadout(loadout, mapCombatRange)[0], inventory);
            if (fallbackOffer.Key != null)
            {
                recommendation.ItemWeapon = fallbackOffer.Key;
                recommendation.Price = fallbackOffer.Value;
                recommendation.Debug += "Better in Class | too exp] ";

                return recommendation;
            }
            else
            {
                recommendation.Debug += "NULL] ";

                return recommendation;
            }
        }
        //one combat range is not covered by the inventory items, we have to show a suggestion to buy
        else if (recommendation.ItemWeapon == null)
        {
            var offer = GetAdditionalWeapon(recommendation.CombatRange, playerLevel, inventory, loadout);
            recommendation.ItemWeapon = offer.Key;
            recommendation.Price = offer.Value;
            recommendation.Debug += "Add Weapon] ";

            return recommendation;
        }
        //just return the reccomendation from the CheckLoadout step (fix holes in loadout)
        else
        {
            recommendation.Debug += "None] ";

            return recommendation;
        }
    }

    public static string PrintDPS()
    {
        StringBuilder builder = new StringBuilder();
        var weapons = new Dictionary<UberstrikeItemClass, List<WeaponItem>>();
        foreach (WeaponItem weapon in ItemManager.Instance.GetShopItems(UberstrikeItemType.Weapon, BuyingMarketType.Shop))
        {
            if (weapons.ContainsKey(weapon.ItemClass))
                weapons[weapon.ItemClass].Add(weapon);
            else
                weapons.Add(weapon.ItemClass, new List<WeaponItem>() { weapon });
        }

        foreach (var w in weapons.Values)
        {
            w.Sort((a, b) => b.Configuration.DPS.CompareTo(a.Configuration.DPS));
        }

        foreach (var v in weapons)
        {
            builder.AppendLine("+++" + v.Key + "+++");
            foreach (var w in v.Value)
                builder.AppendLine("Level: " + w.Configuration.LevelLock + "\t Name: " + w.Name + " [" + w.ItemClass + "] \t DPS: " + w.Configuration.DPS + "\t Tier: " + w.Configuration.Tier);
        }
        return builder.ToString();
    }

    public static WeaponItem FallBackWeapon
    {
        get { return LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponMelee).Item as WeaponItem; }
    }

    public static List<CombatRangeCategory> GetCategoriesForCombatRange(CombatRangeTier mapCombatRange)
    {
        int close = mapCombatRange.CloseRange;
        int medium = mapCombatRange.MediumRange;
        int far = mapCombatRange.LongRange;

        List<CombatRangeCategory> categories = new List<CombatRangeCategory>(3);
        if (close > 0)
        {
            categories.Add(CombatRangeCategory.Close);
            close--;
        }
        if (medium > 0)
        {
            categories.Add(CombatRangeCategory.Medium);
            medium--;
        }
        if (far > 0)
        {
            categories.Add(CombatRangeCategory.Far);
            far = 0;
        }

        int freeSlots = 3 - categories.Count;
        for (int i = 0; i < freeSlots; i++)
        {
            if (close > medium)
            {
                categories.Add(CombatRangeCategory.Close);
                close--;
            }
            else
            {
                categories.Add(CombatRangeCategory.Medium);
                medium = Mathf.Max(medium - 1, 0);
            }
        }

        //for (int c = 0; c < Mathf.RoundToInt((3 * Mathf.Max(mapCombatRange.CloseRange - 1, 0) / total) - 0.1f); c++)
        //    categories.Add(CombatRangeCategory.Close);

        //for (int m = 0; m < Mathf.RoundToInt((3 * mapCombatRange.MediumRange / total) + 0.1f); m++)
        //    categories.Add(CombatRangeCategory.Medium);

        //for (int l = 0; l < Mathf.RoundToInt(3 * Mathf.Clamp(mapCombatRange.LongRange, 0, 1) / total); l++)
        //    categories.Add(CombatRangeCategory.Far);
        categories.Sort((i, j) => mapCombatRange.GetTierForRange(j).CompareTo(mapCombatRange.GetTierForRange(i)));
        return categories;
    }


    private static WeaponItem GetBestItemForRange(CombatRangeCategory range, IEnumerable<WeaponItem> items)
    {
        WeaponItem best = null;
        foreach (var item in items)
        {
            if ((item.Configuration.CombatRange & range) != 0 && (best == null || item.Configuration.Tier > best.Configuration.Tier))
                best = item;
        }
        return best;
    }

    public static void DebugRecommendation(WeaponRecommendation recommendation)
    {
        if (recommendation.ItemWeapon != null)
        {
            Debug.Log(recommendation.Debug + recommendation.ItemWeapon.Name + " " + recommendation.ItemWeapon.ItemClass + "/" + recommendation.ItemWeapon.Configuration.Tier + ", " + recommendation.CombatRange + ", Slot: " + recommendation.LoadoutSlot + " " + recommendation.Price.Price + ", Level: " + recommendation.ItemWeapon.ItemView.LevelLock);
        }
        else
        {
            Debug.Log(recommendation.Debug + " NIL " + recommendation.CombatRange + " " + recommendation.LoadoutSlot + ", isComplete: " + recommendation.IsComplete);
        }
    }

    public static LoadoutSlotType FindBestSlotToEquipWeapon(WeaponItem weapon, List<CombatRangeCategory> ranges)
    {
        if (weapon != null)
        {
            //first check if melee item
            if (weapon.ItemClass == UberstrikeItemClass.WeaponMelee) return LoadoutSlotType.WeaponMelee;

            var loadout = new Dictionary<LoadoutSlotType, WeaponItem>()
            {
                { LoadoutSlotType.WeaponPrimary, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponPrimary).Item as WeaponItem },
                { LoadoutSlotType.WeaponSecondary, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponSecondary).Item as WeaponItem},
                { LoadoutSlotType.WeaponTertiary, LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponTertiary).Item as WeaponItem},
            };

            //easy one - just use the place of the item of the same class
            foreach (var i in loadout)
            {
                if (i.Value != null && i.Value.ItemClass == weapon.ItemClass)
                    return i.Key;
            }

            foreach (var i in loadout)
            {
                //take the first free one
                if (i.Value == null)
                    return i.Key;
                //or replace the first weapon that is not matching the range requirements 
                else if (ranges.TrueForAll(r => (r & i.Value.Configuration.CombatRange) == 0))
                    return i.Key;
            }

            //in the last case, return the first slot, that is not a perfect match in terms of range
            foreach (var i in loadout)
            {
                if (i.Value != null)
                {
                    var perfectMatch = ranges.Find(r => r == i.Value.Configuration.CombatRange);
                    if (perfectMatch != 0)
                    {
                        ranges.Remove(perfectMatch);
                    }
                    else
                    {
                        return i.Key;
                    }
                }
            }
        }

        return LoadoutSlotType.None;
    }

    private static WeaponRecommendation CheckMyLoadout(CombatRangeTier mapCombatRange, List<WeaponItem> loadout, List<WeaponItem> inventory)
    {
        var recommendation = new WeaponRecommendation();
        recommendation.Debug += "[LC: ";

        //find the best items in your inventory
        var bestWeaponsPerClass = new Dictionary<UberstrikeItemClass, WeaponItem>();
        foreach (var w in inventory)
        {
            WeaponItem bestItem;
            if (bestWeaponsPerClass.TryGetValue(w.ItemClass, out bestItem))
            {
                if (bestItem.Configuration.Tier < w.Configuration.Tier || (bestItem.Configuration.Tier == w.Configuration.Tier && bestItem.Configuration.DPS < w.Configuration.DPS))
                {
                    bestWeaponsPerClass[w.ItemClass] = w;
                }
            }
            else
            {
                bestWeaponsPerClass[w.ItemClass] = w;
            }
        }

        //get all the item classes that are currently not equipped
        HashSet<UberstrikeItemClass> equippedItemClasses = new HashSet<UberstrikeItemClass>();
        foreach (var w in loadout)
        {
            equippedItemClasses.Add(w.ItemClass);
        }

        if (!equippedItemClasses.Contains(UberstrikeItemClass.WeaponMelee))
        {
            recommendation.ItemWeapon = bestWeaponsPerClass[UberstrikeItemClass.WeaponMelee];
            recommendation.CombatRange = CombatRangeCategory.Close;
            recommendation.LoadoutSlot = LoadoutSlotType.WeaponMelee;
            recommendation.Debug += " No Melee] ";

            return recommendation;
        }

        //consider the melee item already checked
        List<int> processedItemIds = new List<int>();
        var melee = loadout.Find(w => w.ItemClass == UberstrikeItemClass.WeaponMelee);
        if (melee != null)
            processedItemIds.Add(melee.ItemId);

        //check your loadout for obvious holes
        var ranges = GetCategoriesForCombatRange(mapCombatRange);
        foreach (var range in ranges)
        {
            //first choose a weapon that is only covering this specific range (e.g .when looking for 'Close' -> prefer a weapon with range 'Close' to 'CloseMedium' )
            var item = loadout.Find(w => (w.Configuration.CombatRange & range) == w.Configuration.CombatRange && !processedItemIds.Contains(w.ItemId));
            //if that fails -> find a weapon that would potentially cover this combat range
            if (item == null)
            {
                item = loadout.Find(w => (w.Configuration.CombatRange & range) != 0 && !processedItemIds.Contains(w.ItemId));
            }

            if (item != null)
            {
                processedItemIds.Add(item.ItemId);
            }
            //scream if your loadout does not have any weapon equipped for a required range
            else
            {
                //get all the best weapons but remove the categories that are already equipped
                var list = new List<WeaponItem>(bestWeaponsPerClass.Values);
                list.RemoveAll(w => equippedItemClasses.Contains(w.ItemClass));
                var weapon = GetBestItemForRange(range, list);

                recommendation.ItemWeapon = weapon;
                recommendation.CombatRange = range;
                recommendation.LoadoutSlot = FindBestSlotToEquipWeapon(weapon, ranges);
                recommendation.Debug += " Uncovered Range] ";

                return recommendation;
            }
        }

        //get the best piece of equippment from your current inventory
        foreach (var item in loadout)
        {
            WeaponItem betterWeapon;
            //scream if your loadout does not have any weapon equipped for a required range
            if (bestWeaponsPerClass.TryGetValue(item.ItemClass, out betterWeapon) && betterWeapon.Configuration.Tier > item.Configuration.Tier)
            {
                recommendation.ItemWeapon = betterWeapon;
                recommendation.CombatRange = betterWeapon.Configuration.CombatRange;
                recommendation.LoadoutSlot = FindBestSlotToEquipWeapon(betterWeapon, ranges);
                recommendation.Debug += " Better Inventory] ";

                return recommendation;
            }
        }

        recommendation.LoadoutSlot = LoadoutSlotType.None;
        recommendation.IsComplete = true;
        recommendation.Debug += " OK] ";

        return recommendation;
    }

    private static List<WeaponItem> GetWeakestItemsInLoadout(List<WeaponItem> loadout, CombatRangeTier combatRange)
    {
        var list = new List<WeaponItem>(loadout);
        list.Sort((i, j) =>
            {
                int ret = i.Configuration.Tier.CompareTo(j.Configuration.Tier);
                if (ret == 0)
                    ret = combatRange.GetTierForRange(j.Configuration.CombatRange).CompareTo(combatRange.GetTierForRange(i.Configuration.CombatRange));
                return ret;
            });
        
        return list;
    }

    private static ItemOffer GetAdditionalWeapon(CombatRangeCategory range, int playerLevel, List<WeaponItem> inventory, List<WeaponItem> loadout)
    {
        //get all the item classes that are currently not equipped
        HashSet<UberstrikeItemClass> equippedItemClasses = new HashSet<UberstrikeItemClass>();
        foreach (var w in loadout)
        {
            equippedItemClasses.Add(w.ItemClass);
        }

        //make sure the player level is at least 2
        playerLevel = Mathf.Max(2, playerLevel);

        var candidates = new List<KeyValuePair<WeaponItem, ItemPrice>>();

        foreach (WeaponItem weapon in ItemManager.Instance.GetShopItems(UberstrikeItemType.Weapon, BuyingMarketType.Shop))
        {
            //check the weapon combat range
            if ((weapon.Configuration.CombatRange & range) != 0 && inventory != null &&
                !inventory.Exists(w => w.ItemId == weapon.ItemId) && !equippedItemClasses.Contains(weapon.ItemClass))
            {
                var creditsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Credits);
                var pointsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Points);

                if (pointsPrice != null && pointsPrice.Price > 0 && pointsPrice.Price <= PlayerDataManager.Points && weapon.ItemView.LevelLock <= playerLevel)
                {
                    candidates.Add(new ItemOffer(weapon, pointsPrice));
                }
                else if (creditsPrice != null && creditsPrice.Price > 0 && creditsPrice.Price <= PlayerDataManager.Credits)
                {
                    candidates.Add(new ItemOffer(weapon, creditsPrice));
                }
            }
        }

        if (candidates.Count > 0)
        {
            candidates.Sort(new ShopUtils.PriceComparer<WeaponItem>());

            return candidates[0];
        }
        else
        {
            return GetNextBestWeapon(range, inventory);
        }
    }

    private static ItemOffer GetNextBestAffordableWeapon(WeaponItem weakestLink, int playerLevel, List<WeaponItem> inventory)
    {
        //make sure the player level is at least 2
        playerLevel = Mathf.Max(2, playerLevel);

        var regularCandidates = new List<ItemOffer>();

        foreach (WeaponItem weapon in ItemManager.Instance.GetShopItems(UberstrikeItemType.Weapon, BuyingMarketType.Shop))
        {
            //check the weapon combat range
            if (weapon.ItemClass == weakestLink.ItemClass && !inventory.Exists(w => w.ItemId == weapon.ItemId) && (weapon.Configuration.Tier > weakestLink.Configuration.Tier || (weapon.Configuration.Tier == weakestLink.Configuration.Tier && weapon.Configuration.DPS > weakestLink.Configuration.DPS)))
            {
                var creditsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Credits);
                var pointsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Points);

                if (pointsPrice != null && pointsPrice.Price > 0 && pointsPrice.Price <= PlayerDataManager.Points && weapon.ItemView.LevelLock <= playerLevel)
                {
                    regularCandidates.Add(new KeyValuePair<WeaponItem, ItemPrice>(weapon, pointsPrice));
                }
                else if (creditsPrice != null && creditsPrice.Price > 0 && creditsPrice.Price <= PlayerDataManager.Credits)
                {
                    regularCandidates.Add(new KeyValuePair<WeaponItem, ItemPrice>(weapon, creditsPrice));
                }
            }
        }

        if (regularCandidates.Count > 0)
        {
            regularCandidates.Sort(new ShopUtils.PriceComparer<WeaponItem>());

            return regularCandidates[0];
        }
        else
        {
            return new ItemOffer();
        }
    }

    private static ItemOffer GetNextBestWeapon(WeaponItem weakestLink, List<WeaponItem> inventory)
    {
        ItemOffer offer = new ItemOffer();

        foreach (WeaponItem weapon in ItemManager.Instance.GetShopItems(UberstrikeItemType.Weapon, BuyingMarketType.Shop))
        {
            //check the weapon combat range
            if (weapon.ItemClass == weakestLink.ItemClass && !inventory.Exists(w => w.ItemId == weapon.ItemId) && (weapon.Configuration.Tier > weakestLink.Configuration.Tier || (weapon.Configuration.Tier == weakestLink.Configuration.Tier && weapon.Configuration.DPS > weakestLink.Configuration.DPS)))
            {
                var creditsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Credits);
                var pointsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Points);

                if (pointsPrice != null && pointsPrice.Price > 0 && (offer.Key == null || offer.Key.Configuration.DPS > weapon.Configuration.DPS))
                {
                    offer = new KeyValuePair<WeaponItem, ItemPrice>(weapon, pointsPrice);
                }
                else if (creditsPrice != null && creditsPrice.Price > 0 && (offer.Key == null || offer.Key.Configuration.DPS > weapon.Configuration.DPS))
                {
                    offer = new KeyValuePair<WeaponItem, ItemPrice>(weapon, creditsPrice);
                }
            }
        }

        return offer;
    }

    private static ItemOffer GetNextBestWeapon(CombatRangeCategory range, List<WeaponItem> inventory)
    {
        ItemOffer offer = new ItemOffer();

        foreach (WeaponItem weapon in ItemManager.Instance.GetShopItems(UberstrikeItemType.Weapon, BuyingMarketType.Shop))
        {
            //check the weapon combat range
            if ((weapon.Configuration.CombatRange & range) != 0 && inventory != null &&
                !inventory.Exists(w => w.ItemId == weapon.ItemId))
            {
                var creditsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Credits);
                var pointsPrice = ShopUtils.GetLowestPrice(weapon, UberStrikeCurrencyType.Points);

                if (pointsPrice != null && pointsPrice.Price > 0 && (offer.Key == null || offer.Key.Configuration.DPS > weapon.Configuration.DPS))
                {
                    offer = new KeyValuePair<WeaponItem, ItemPrice>(weapon, pointsPrice);
                }
                else if (creditsPrice != null && creditsPrice.Price > 0 && (offer.Key == null || offer.Key.Configuration.DPS > weapon.Configuration.DPS))
                {
                    offer = new KeyValuePair<WeaponItem, ItemPrice>(weapon, creditsPrice);
                }
            }
        }

        return offer;
    }
}