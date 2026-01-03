using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Models.Views;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class ItemManager : Singleton<ItemManager>
{
    #region Fields

    private Dictionary<UberstrikeItemClass, int> _defaultItemIdByClass;
    private HashSet<int> _defaultGearItemIds;

    private Dictionary<int, IUnityItem> _unityItems;
    private Dictionary<int, IUnityItem> _shopItems;
    private Dictionary<int, int> _itemRecommendationsPerMap;

    #endregion Fields

    #region Properties

    public IEnumerable<IUnityItem> ShopItems { get { return _shopItems.Values; } }
    public int ShopItemCount { get { return _shopItems.Count; } }

    #endregion

    private ItemManager()
    {
        _unityItems = new Dictionary<int, IUnityItem>();
        _shopItems = new Dictionary<int, IUnityItem>();

        _defaultItemIdByClass = new Dictionary<UberstrikeItemClass, int>()
        {
            { UberstrikeItemClass.GearHead, UberStrikeCommonConfig.DefaultHead },
            { UberstrikeItemClass.GearFace, UberStrikeCommonConfig.DefaultFace },
            { UberstrikeItemClass.GearGloves, UberStrikeCommonConfig.DefaultGloves },
            { UberstrikeItemClass.GearUpperBody, UberStrikeCommonConfig.DefaultUpperBody },
            { UberstrikeItemClass.GearLowerBody, UberStrikeCommonConfig.DefaultLowerBody },
            { UberstrikeItemClass.GearBoots, UberStrikeCommonConfig.DefaultBoots },

            { UberstrikeItemClass.WeaponMelee, (int)DefaultWeaponId.WeaponMelee },
            { UberstrikeItemClass.WeaponHandgun, (int)DefaultWeaponId.WeaponHandgun },
            { UberstrikeItemClass.WeaponMachinegun, (int)DefaultWeaponId.WeaponMachinegun },
            { UberstrikeItemClass.WeaponShotgun, (int)DefaultWeaponId.WeaponShotgun },
            { UberstrikeItemClass.WeaponSniperRifle, (int)DefaultWeaponId.WeaponSniperRifle },
            { UberstrikeItemClass.WeaponSplattergun, (int)DefaultWeaponId.WeaponSplattergun },
            { UberstrikeItemClass.WeaponLauncher, (int)DefaultWeaponId.WeaponLauncher },
            { UberstrikeItemClass.WeaponCannon, (int)DefaultWeaponId.WeaponCannon },
        };

        _defaultGearItemIds = new HashSet<int>()
        {
            UberStrikeCommonConfig.DefaultHead ,
            UberStrikeCommonConfig.DefaultFace ,
            UberStrikeCommonConfig.DefaultGloves ,
            UberStrikeCommonConfig.DefaultUpperBody ,
            UberStrikeCommonConfig.DefaultLowerBody ,
            UberStrikeCommonConfig.DefaultBoots ,
        };
    }

    #region Private Methods

    private void UpdateShopItems(UberStrikeItemShopClientView shopView)
    {
        _shopItems.Clear();

        foreach (var itemView in shopView.GearItems) AddItemToShop(itemView);
        foreach (var itemView in shopView.WeaponItems) AddItemToShop(itemView);
        //foreach (var itemView in shopView.FunctionalItems) AddItemToShop(itemView);

        foreach (var itemView in shopView.QuickItems)
        {
            try
            {

                _unityItems[itemView.ID] = new QuickItem(itemView);

                AddItemToShop(itemView);
            }
            catch (Exception e)
            {
                Debug.LogError("UpdateShopItems failed for item " + itemView.ID + "/" + itemView.Name + " with: " + e.Message);
            }
        }

        CmuneEventHandler.Route(new ShopRefreshCurrentItemListEvent());
    }

    public void AddItemToShop(BaseUberStrikeItemView itemView)
    {
        if (itemView == null)
        {
            throw new ArgumentNullException("itemView");
        }

        IUnityItem baseItem;
        if (_unityItems.TryGetValue(itemView.ID, out baseItem))
        {
            // Only copy properties if the types are compatible
            if (baseItem.ItemView.GetType() == itemView.GetType() || 
                baseItem.ItemView.GetType().IsAssignableFrom(itemView.GetType()) ||
                itemView.GetType().IsAssignableFrom(baseItem.ItemView.GetType()))
            {
                try
                {
                    ItemConfigurationUtil.CopyProperties(baseItem.ItemView, itemView);
                }
                catch (System.Exception ex)
                {
                    CmuneDebug.LogError("Failed to copy properties for item {0} (ID: {1}): {2}", itemView.Name, itemView.ID, ex.Message);
                }
            }
            else
            {
                CmuneDebug.LogWarning("Skipping property copy for item {0} (ID: {1}) due to type mismatch: {2} vs {3}", 
                    itemView.Name, itemView.ID, baseItem.ItemView.GetType().Name, itemView.GetType().Name);
            }

            if (!_shopItems.ContainsKey(baseItem.ItemId))
            {
                _shopItems.Add(baseItem.ItemId, baseItem);
            }
            //else
            //{
            //    CmuneDebug.LogError("AddItemToShop failed because item {0} width id {1} is duplicate.", itemView.Name, itemView.ID);
            //}
        }
        //else
        //{
        //    CmuneDebug.LogWarning("AddItemToShop failed for item '{0}' with id {1} because no corresponding unity item was found. Maybe deprecated?", itemView.Name, itemView.ID);
        //}
    }

    #endregion

    #region Public Methods

    public void ConfigureDefaultGearAndWeapons()
    {
        AddItemToShop(new UberStrikeItemGearView() { ID = UberStrikeCommonConfig.DefaultHead });
        AddItemToShop(new UberStrikeItemGearView() { ID = UberStrikeCommonConfig.DefaultGloves });
        AddItemToShop(new UberStrikeItemGearView() { ID = UberStrikeCommonConfig.DefaultUpperBody });
        AddItemToShop(new UberStrikeItemGearView() { ID = UberStrikeCommonConfig.DefaultLowerBody });
        AddItemToShop(new UberStrikeItemGearView() { ID = UberStrikeCommonConfig.DefaultBoots });

        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponMelee,
            DamageKnockback = 1000,
            DamagePerProjectile = 99,
            AccuracySpread = 0,
            RecoilKickback = 0,
            StartAmmo = 0,
            MaxAmmo = 0,
            MissileTimeToDetonate = 0,
            MissileForceImpulse = 0,
            MissileBounciness = 0,
            RateOfFire = 500,
            SplashRadius = 100,
            ProjectilesPerShot = 1,
            ProjectileSpeed = 0,
            RecoilMovement = 0,
        });
        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponHandgun,
            DamageKnockback = 80,
            DamagePerProjectile = 24,
            AccuracySpread = 3,
            RecoilKickback = 8,
            StartAmmo = 25,
            MaxAmmo = 50,
            MissileTimeToDetonate = 0,
            MissileForceImpulse = 0,
            MissileBounciness = 0,
            RateOfFire = 200,
            SplashRadius = 100,
            ProjectilesPerShot = 1,
            ProjectileSpeed = 0,
            RecoilMovement = 8,
        });
        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponMachinegun,
            DamageKnockback = 50,
            DamagePerProjectile = 13,
            AccuracySpread = 3,
            RecoilKickback = 4,
            StartAmmo = 100,
            MaxAmmo = 300,
            MissileTimeToDetonate = 0,
            MissileForceImpulse = 0,
            MissileBounciness = 0,
            RateOfFire = 125,
            SplashRadius = 100,
            ProjectilesPerShot = 1,
            ProjectileSpeed = 0,
            RecoilMovement = 5,
        });
        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponShotgun,
            DamageKnockback = 160,
            DamagePerProjectile = 9,
            AccuracySpread = 8,
            RecoilKickback = 15,
            StartAmmo = 20,
            MaxAmmo = 50,
            MissileTimeToDetonate = 0,
            MissileForceImpulse = 0,
            MissileBounciness = 0,
            RateOfFire = 1000,
            SplashRadius = 100,
            ProjectilesPerShot = 11,
            ProjectileSpeed = 0,
            RecoilMovement = 10,
        });
        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponSniperRifle,
            DamageKnockback = 150,
            DamagePerProjectile = 70,
            AccuracySpread = 0,
            RecoilKickback = 12,
            StartAmmo = 20,
            MaxAmmo = 50,
            MissileTimeToDetonate = 0,
            MissileForceImpulse = 0,
            MissileBounciness = 0,
            RateOfFire = 1500,
            SplashRadius = 100,
            ProjectilesPerShot = 1,
            ProjectileSpeed = 0,
            RecoilMovement = 15,
        });
        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponSplattergun,
            DamageKnockback = 150,
            DamagePerProjectile = 16,
            AccuracySpread = 0,
            RecoilKickback = 0,
            StartAmmo = 60,
            MaxAmmo = 200,
            MissileTimeToDetonate = 5000,
            MissileForceImpulse = 0,
            MissileBounciness = 80,
            RateOfFire = 90,
            SplashRadius = 80,
            ProjectilesPerShot = 1,
            ProjectileSpeed = 70,
            RecoilMovement = 0,
        });
        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponLauncher,
            DamageKnockback = 450,
            DamagePerProjectile = 70,
            AccuracySpread = 0,
            RecoilKickback = 15,
            StartAmmo = 15,
            MaxAmmo = 30,
            MissileTimeToDetonate = 1250,
            MissileForceImpulse = 0,
            MissileBounciness = 0,
            RateOfFire = 1000,
            SplashRadius = 400,
            ProjectilesPerShot = 1,
            ProjectileSpeed = 20,
            RecoilMovement = 9,
        });
        AddItemToShop(new UberStrikeItemWeaponView()
        {
            ID = (int)DefaultWeaponId.WeaponCannon,
            DamageKnockback = 600,
            DamagePerProjectile = 65,
            AccuracySpread = 0,
            RecoilKickback = 12,
            StartAmmo = 10,
            MaxAmmo = 25,
            MissileTimeToDetonate = 5000,
            MissileForceImpulse = 0,
            MissileBounciness = 0,
            RateOfFire = 1000,
            SplashRadius = 250,
            ProjectilesPerShot = 1,
            ProjectileSpeed = 50,
            RecoilMovement = 32,
        });
    }

    public static bool IsItemEquippable(int itemID)
    {
        return
            itemID != UberStrikeCommonConfig.ClanLeaderLicenseId &&
            itemID != UberStrikeCommonConfig.PrivateerLicenseId &&
            itemID != CommonConfig.NameChangeItem;
    }

    #region Default Items

    public bool TryGetDefaultItemId(UberstrikeItemClass itemClass, out int itemID)
    {
        return _defaultItemIdByClass.TryGetValue(itemClass, out itemID);
    }

    public GearItem GetDefaultGearItem(UberstrikeItemClass itemClass)
    {
        int itemId;
        if (TryGetDefaultItemId(itemClass, out itemId))
            return GetItemInShop(itemId) as GearItem;
        else
            return null;
    }

    public WeaponItem GetDefaultMeleeWeapon()
    {
        return GetShopItemOfType<WeaponItem>(UberStrikeCommonConfig.DefaultMeleeWeapon);
    }

    public bool IsDefaultGearItem(int itemId)
    {
        return _defaultGearItemIds.Contains(itemId);
    }

    #endregion

    #region Shop Items

    public List<IUnityItem> GetShopItems(UberstrikeItemType itemType, BuyingMarketType marketType)
    {
        List<IUnityItem> shopItemList = GetShopItems();
        //take only the items of correct itemType
        shopItemList.RemoveAll(item => item.ItemType != itemType);
        return shopItemList;
    }

    public List<IUnityItem> GetShopItems(UberstrikeItemType itemType, UberstrikeItemClass itemClass)
    {
        List<IUnityItem> shopItemList = GetShopItems();
        //take only the items of correct type and class
        shopItemList.RemoveAll(item => item.ItemType != itemType && item.ItemClass != itemClass);
        return shopItemList;
    }

    public List<IUnityItem> GetShopItems()
    {
        List<IUnityItem> items = new List<IUnityItem>(_shopItems.Values);
        items.RemoveAll(item => !item.ItemView.IsForSale);
        return items;
    }

    public List<IUnityItem> GetFeaturedShopItems()
    {
        List<IUnityItem> shopItemList = GetShopItems();
        //take only the feature items
        shopItemList.RemoveAll(i => i.ItemView.ShopHighlightType == ItemShopHighlightType.None);
        return shopItemList;
    }

    public IUnityItem GetItemInShop(int itemId)
    {
        return GetShopItemOfType<IUnityItem>(itemId);
    }

    public IUnityItem GetGearItemInShop(int itemId, UberstrikeItemClass itemClass)
    {
        IUnityItem item = GetShopItemOfType<IUnityItem>(itemId);
        if (item == null)
        {
            item = GetDefaultGearItem(itemClass);
        }
        return item;
    }

    public IUnityItem GetRecommendedItem(int mapId)
    {
        int itemId;
        if (_itemRecommendationsPerMap != null && _itemRecommendationsPerMap.TryGetValue(mapId, out itemId))
        {
            return GetItemInShop(itemId);
        }
        else
        {
            return GetItemInShop((int)DefaultWeaponId.WeaponMachinegun);
        }
    }

    public WeaponItem GetWeaponItemInShop(int itemId)
    {
        return GetShopItemOfType<WeaponItem>(itemId);
    }

    public QuickItem GetQuickItemInShop(int itemId)
    {
        return GetShopItemOfType<QuickItem>(itemId);
    }

    public FunctionalItem GetFunctionalItemInShop(int itemId)
    {
        return GetShopItemOfType<FunctionalItem>(itemId);
    }

    private T GetShopItemOfType<T>(int itemID) where T : class, IUnityItem
    {
        IUnityItem item;
        _shopItems.TryGetValue(itemID, out item);
        return item as T;
    }

    #endregion

    public bool ValidateItemMall()
    {
        return (_shopItems.Count > 0);
    }

    #endregion

    #region Web Services

    public IEnumerator StartGetShop()
    {
        yield return UberStrike.WebService.Unity.ShopWebServiceClient.GetShop(ApplicationDataManager.VersionLong,
            (shop) =>
            {
                if (shop != null)
                {
                    _itemRecommendationsPerMap = shop.ItemsRecommendationPerMap;
                    UpdateShopItems(shop);
                    WeaponConfigurationHelper.UpdateWeaponStatistics(shop);
                }
                else
                {
                    CmuneDebug.LogError("ShopWebServiceClient.GetShop returned with NULL");
                }
            },
            (ex) =>
            {
                DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
            });
    }

    public IEnumerator StartGetInventory(bool showProgress)
    {
        if (_shopItems.Count == 0)
        {
            PopupSystem.ShowMessage("Error Getting Shop Data", "The shop is empty, perhaps there\nwas an error getting the Shop data?", PopupSystem.AlertType.OK, null);
            yield break;
        }
        else
        {
            if (showProgress)
            {
                IPopupDialog popupDialog = PopupSystem.ShowMessage(LocalizedStrings.UpdatingInventory, LocalizedStrings.WereUpdatingYourInventoryPleaseWait, PopupSystem.AlertType.None);
                yield return UberStrike.WebService.Unity.UserWebServiceClient.GetInventory(PlayerDataManager.CmidSecure,
                    InventoryManager.Instance.UpdateInventoryItems,
                    (ex) =>
                    {
                        DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    });
                PopupSystem.HideMessage(popupDialog);
            }
            else
            {
                yield return UberStrike.WebService.Unity.UserWebServiceClient.GetInventory(PlayerDataManager.CmidSecure,
                    InventoryManager.Instance.UpdateInventoryItems,
                    (ex) =>
                    {
                        DebugConsoleManager.SendExceptionReport(ex.Message, ex.StackTrace);
                    });
            }
        }
    }

    #endregion

    public void AddUnityItems(IUnityItem[] items)
    {
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item.ItemId > 0)
                    _unityItems.Add(item.ItemId, item);
                else
                    Debug.LogError("AddUnityItems failed with: " + item.Name + " " + item.ItemId);
            }
        }
    }

    internal GameObject GetPrefab(int itemId)
    {
        IUnityItem item = null;
        if (itemId > 0 && _unityItems.TryGetValue(itemId, out item) && item != null && item is MonoBehaviour)
        {
            return ((MonoBehaviour)item).gameObject;
        }
        else
        {
            return null;
        }
    }

    internal GameObject Instantiate(int p)
    {
        return GameObject.Instantiate(GetPrefab(p)) as GameObject;
    }
}