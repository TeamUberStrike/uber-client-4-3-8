
using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class InventoryManager : Singleton<InventoryManager>
{
    private IDictionary<int, InventoryItem> _inventoryItems;

    public LoadoutSlotType CurrentWeaponSlot = LoadoutSlotType.WeaponPrimary;
    public LoadoutSlotType CurrentQuickItemSot = LoadoutSlotType.QuickUseItem1;
    public LoadoutSlotType CurrentFunctionalSlot = LoadoutSlotType.FunctionalItem1;

    public IEnumerable<InventoryItem> InventoryItems { get { return _inventoryItems.Values; } }

    private InventoryManager()
    {
        _inventoryItems = new Dictionary<int, InventoryItem>();
    }

    public IEnumerator StartUpdateInventoryAndEquipNewItem(int itemId, bool autoEquip = false)
    {
        Debug.Log("StartUpdateInventoryAndEquipNewItem " + itemId + " " + autoEquip);

        var item = ItemManager.Instance.GetItemInShop(itemId);
        Debug.Log("GetItemInShop " + (item != null));
        if (item != null)
        {
            IPopupDialog popupDialog = PopupSystem.ShowMessage(LocalizedStrings.UpdatingInventory, LocalizedStrings.WereUpdatingYourInventoryPleaseWait, PopupSystem.AlertType.None);

            // Get the players inventory, since we bought a new item
            yield return MonoRoutine.Start(ItemManager.Instance.StartGetInventory(false));
            Debug.Log("StartGetInventory done");

            PopupSystem.HideMessage(popupDialog);

            if (autoEquip)
            {
                EquipItem(itemId);
            }
            //remain slient in case we bought a weapon during a game
            else if (GameState.HasCurrentGame && GameState.CurrentGame.IsMatchRunning)
            {
                HudPopup.Instance.Show("Your purchase was successful!\nIt will be available next life!", item.Icon);
                EquipItem(itemId);
            }
            else
            {
                PopupSystem.ShowItem(item);
            }

            // Update the players points and credits balance
            yield return MonoRoutine.Start(PlayerDataManager.Instance.StartGetMember());
        }
    }

    public bool EquipItem(int itemId)
    {
        var slotType = LoadoutSlotType.None;

        // Auto Equip the Item if we dropped from the shop
        InventoryItem item;
        if (TryGetInventoryItem(itemId, out item) && item.IsValid && item.Item.ItemType == UberstrikeItemType.Weapon)
        {
            if (GameState.CurrentSpace != null)
                slotType = RecommendationUtils.FindBestSlotToEquipWeapon(item.Item as WeaponItem, RecommendationUtils.GetCategoriesForCombatRange(GameState.CurrentSpace.CombatRangeTiers));
        }

        return EquipItemOnSlot(itemId, slotType);
    }

    public void UnequipWeaponSlot(LoadoutSlotType slotType)
    {
        switch (slotType)
        {
            case LoadoutSlotType.WeaponMelee:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponMelee);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponMelee, null);
                break;
            case LoadoutSlotType.WeaponPrimary:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponPrimary);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponPrimary, null);
                break;
            case LoadoutSlotType.WeaponSecondary:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponSecondary);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponSecondary, null);
                break;
            case LoadoutSlotType.WeaponTertiary:
                LoadoutManager.Instance.ResetSlot(LoadoutSlotType.WeaponTertiary);
                GameState.LocalDecorator.SetActiveWeaponSlot(slotType);
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponTertiary, null);
                break;
        }
    }

    public bool EquipItemOnSlot(int itemId, LoadoutSlotType slotType)
    {
        // Auto Equip the Item if we dropped from the shop
        InventoryItem item;
        if (TryGetInventoryItem(itemId, out item) && item.IsValid)
        {
            if (LoadoutManager.Instance.IsItemEquipped(itemId))
            {
                LoadoutSlotType s;
                if (LoadoutManager.Instance.TryGetSlotForItem(itemId, out s))
                {
                    CmuneEventHandler.Route(new ShopHighlightSlotEvent() { SlotType = s });

                    //reset the slot in case we had another item equipped at this position
                    TemporaryLoadoutManager.Instance.SetGearLoadout(s, null);
                }
            }
            else
            {
                HighlightItem(itemId, false);

                switch (item.Item.ItemType)
                {
                    case UberstrikeItemType.Gear:
                        {
                            slotType = PlayerDataManager.GetSlotTypeForItemClass(item.Item.ItemClass);
                            LoadoutManager.Instance.SetSlot(slotType, item);

                            TemporaryLoadoutManager.Instance.SetGearLoadout(slotType, item.Item);

                            SfxManager.Play2dAudioClip(SoundEffectType.UIEquipGear);

                            if (GameState.LocalDecorator)
                                GameState.LocalDecorator.HideWeapons();
                        } break;

                    case UberstrikeItemType.Weapon:
                        {
                            if (item.Item.ItemClass == UberstrikeItemClass.WeaponMelee)
                            {
                                slotType = LoadoutSlotType.WeaponMelee;

                                LoadoutManager.Instance.RemoveDuplicateWeaponClass(item);
                                LoadoutManager.Instance.SetSlot(slotType, item);

                                LoadoutManager.Instance.EquipWeapon(slotType, item.Item as WeaponItem);
                                SfxManager.Play2dAudioClip(SoundEffectType.WeaponWeaponSwitch);
                            }
                            else
                            {
                                if (slotType == LoadoutSlotType.None)
                                    slotType = GetNextFreeWeaponSlot();

                                LoadoutSlotType newslot = slotType;
                                if (LoadoutManager.Instance.RemoveDuplicateWeaponClass(item, ref newslot) && slotType != newslot)
                                {
                                    LoadoutManager.Instance.SwapLoadoutItems(slotType, newslot);
                                }

                                LoadoutManager.Instance.SetSlot(slotType, item);
                                LoadoutManager.Instance.EquipWeapon(slotType, item.Item as WeaponItem);
                                SfxManager.Play2dAudioClip(SoundEffectType.UIEquipWeapon);
                            }
                        }
                        break;

                    case UberstrikeItemType.QuickUse:
                        EquipQuickItemOnSlot(item, slotType);
                        break;

                    case UberstrikeItemType.Functional:
                        {
                            if (ItemManager.IsItemEquippable(item.Item.ItemId))
                            {
                                if (slotType == LoadoutSlotType.None) slotType = GetNextFreeFunctionalSlot();

                                LoadoutSlotType newslot = slotType;
                                if (LoadoutManager.Instance.RemoveDuplicateFunctionalItemClass(item, ref newslot) && slotType != newslot)
                                {
                                    LoadoutManager.Instance.SwapLoadoutItems(slotType, newslot);
                                }

                                LoadoutManager.Instance.SetSlot(slotType, item);
                                SfxManager.Play2dAudioClip(SoundEffectType.UIEquipItem);
                            }
                        } break;

                    default:
                        SfxManager.Play2dAudioClip(SoundEffectType.UIEquipItem);
                        Debug.LogError("Equip item of type: " + item.Item.ItemType);
                        break;
                }

                bool resetAnimations = item.Item.ItemClass == UberstrikeItemClass.GearHolo;
                AvatarAnimationManager.Instance.SetAnimationState(PageType.Shop, item.Item.ItemClass, resetAnimations);

                CmuneEventHandler.Route(new ShopHighlightSlotEvent() { SlotType = slotType });
            }

            return true;
        }

        return false;
    }

    private void EquipQuickItemOnSlot(InventoryItem item, LoadoutSlotType slotType)
    {
        if (slotType < LoadoutSlotType.QuickUseItem1 ||
            slotType > LoadoutSlotType.QuickUseItem3)
        {
            slotType = GetNextFreeQuickItemSlot();
        }

        LoadoutSlotType newslot = slotType;
        if (LoadoutManager.Instance.RemoveDuplicateQuickItemClass(((QuickItem)item.Item).Configuration, ref newslot) &&
            slotType != newslot)
        {
            LoadoutManager.Instance.SwapLoadoutItems(slotType, newslot);
        }

        SfxManager.Play2dAudioClip(SoundEffectType.UIEquipItem);
        LoadoutManager.Instance.SetSlot(slotType, item);
    }

    private LoadoutSlotType GetNextFreeWeaponSlot()
    {
        // check for empty spaces;
        if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponPrimary))
            return LoadoutSlotType.WeaponPrimary;
        else if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponSecondary))
            return LoadoutSlotType.WeaponSecondary;
        else if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.WeaponTertiary))
            return LoadoutSlotType.WeaponTertiary;

        // if the _currentWeaponSlot is not a valid slot, we set it to the primary
        if (CurrentWeaponSlot == LoadoutSlotType.WeaponPrimary ||
            CurrentWeaponSlot == LoadoutSlotType.WeaponSecondary ||
            CurrentWeaponSlot == LoadoutSlotType.WeaponTertiary)
            return CurrentWeaponSlot;

        //fallback
        return LoadoutSlotType.WeaponPrimary;
    }

    private LoadoutSlotType GetNextFreeFunctionalSlot()
    {
        // check for empty spaces;
        if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.FunctionalItem1))
            return LoadoutSlotType.FunctionalItem1;
        else if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.FunctionalItem2))
            return LoadoutSlotType.FunctionalItem2;
        else if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.FunctionalItem3))
            return LoadoutSlotType.FunctionalItem3;

        // if there was no empty spaces, then just select next in line
        switch (CurrentFunctionalSlot)
        {
            case LoadoutSlotType.FunctionalItem1:
                return LoadoutSlotType.FunctionalItem2;
            case LoadoutSlotType.FunctionalItem2:
                return LoadoutSlotType.FunctionalItem3;
            case LoadoutSlotType.FunctionalItem3:
                return LoadoutSlotType.FunctionalItem1;
            default:
                return CurrentFunctionalSlot;
        }
    }

    private LoadoutSlotType GetNextFreeQuickItemSlot()
    {
        // check for empty spaces;
        if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.QuickUseItem1))
            return LoadoutSlotType.QuickUseItem1;
        if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.QuickUseItem2))
            return LoadoutSlotType.QuickUseItem2;
        if (!LoadoutManager.Instance.HasLoadoutItem(LoadoutSlotType.QuickUseItem3))
            return LoadoutSlotType.QuickUseItem3;

        // if there was no empty spaces, then just select next in line
        switch (CurrentQuickItemSot)
        {
            case LoadoutSlotType.QuickUseItem1:
                return LoadoutSlotType.QuickUseItem2;
            case LoadoutSlotType.QuickUseItem2:
                return LoadoutSlotType.QuickUseItem3;
            case LoadoutSlotType.QuickUseItem3:
                return LoadoutSlotType.QuickUseItem1;
            default:
                return CurrentQuickItemSot;
        }
    }

    public static LoadoutSlotType GetSlotTypeForGear(GearItem gearItem)
    {
        if (gearItem != null)
        {
            switch (gearItem.ItemClass)
            {
                case UberstrikeItemClass.GearHead: return LoadoutSlotType.GearHead;
                case UberstrikeItemClass.GearFace: return LoadoutSlotType.GearFace;
                case UberstrikeItemClass.GearUpperBody: return LoadoutSlotType.GearUpperBody;
                case UberstrikeItemClass.GearGloves: return LoadoutSlotType.GearGloves;
                case UberstrikeItemClass.GearLowerBody: return LoadoutSlotType.GearLowerBody;
                case UberstrikeItemClass.GearBoots: return LoadoutSlotType.GearBoots;
                case UberstrikeItemClass.GearHolo: return LoadoutSlotType.GearHolo;
                default: return LoadoutSlotType.None;
            }
        }
        else
        {
            return LoadoutSlotType.None;
        }
    }

    public List<InventoryItem> GetAllItems(bool ignoreEquippedItems)
    {
        var inventory = new List<InventoryItem>();
        foreach (var item in _inventoryItems.Values)
        {
            bool expiredItems = item.DaysRemaining <= 0 && (item.Item.ItemView.Prices?.Count ?? 0) > 0;

            if (item.DaysRemaining > 0 || item.IsPermanent || expiredItems)
            {
                if (ignoreEquippedItems)
                {
                    if (!LoadoutManager.Instance.IsItemEquipped(item.Item.ItemId))
                        inventory.Add(item);
                }
                else
                {
                    inventory.Add(item);
                }
            }
        }

        return inventory;
    }

    public int GetGearItem(int itemID, UberstrikeItemClass itemClass)
    {
        InventoryItem item;
        if (_inventoryItems.TryGetValue(itemID, out item) && item != null && item.Item.ItemType == UberstrikeItemType.Gear)
        {
            return item.Item.ItemId;
        }
        else
        {
            int itemId;
            ItemManager.Instance.TryGetDefaultItemId(itemClass, out itemId);
            return itemId;
        }
    }

    public InventoryItem GetItem(int itemID)
    {
        InventoryItem item;
        if (_inventoryItems.TryGetValue(itemID, out item) && item != null)
        {
            return item;
        }
        else
        {
            return EmptyItem;
        }
    }

    public InventoryItem GetWeaponItem(int itemId)
    {
        InventoryItem item;

        if (_inventoryItems.TryGetValue(itemId, out item) && item != null && item.Item.ItemType == UberstrikeItemType.Weapon)
        {
            return item;
        }
        else
        {
            return EmptyItem;
        }
    }

    public bool TryGetInventoryItem(int itemID, out InventoryItem item)
    {
        return _inventoryItems.TryGetValue(itemID, out item) && item != null && item.Item != null;
    }

    private static readonly InventoryItem EmptyItem = new InventoryItem(null);

    public bool HasPrivateersLicense()
    {
        return IsItemInInventory(UberStrikeCommonConfig.PrivateerLicenseId);
    }

    public bool HasClanLicense()
    {
        return IsItemInInventory(UberStrikeCommonConfig.ClanLeaderLicenseId);
    }

    public bool IsItemValidForDays(InventoryItem item, int days)
    {
        return item != null && (item.DaysRemaining > days || item.IsPermanent);
    }

    public bool IsItemInInventory(int itemId)
    {
        InventoryItem item;
        return _inventoryItems.TryGetValue(itemId, out item) && IsItemValidForDays(item, 0);
    }

    public void UpdateInventoryItems(List<ItemInventoryView> inventory)
    {
        // If we haven't got any items in the mall, exit method
        if (ItemManager.Instance.ShopItemCount == 0)
        {
            return;
        }

        //highlight all new items
        var previousItems = new HashSet<int>(_inventoryItems.Keys);

        // First, clear the inventory
        _inventoryItems.Clear();

        // Add my inventory from the server
        foreach (var item in inventory)
        {
            // Only add items that have been validated in the master item List
            IUnityItem shopItem = ItemManager.Instance.GetItemInShop(item.ItemId);
            if (shopItem != null && shopItem.ItemId == item.ItemId)
            {
                if (!_inventoryItems.ContainsKey(item.ItemId))
                {
                    _inventoryItems.Add(shopItem.ItemId, new InventoryItem(shopItem)
                    {
                        IsPermanent = !item.ExpirationDate.HasValue,
                        AmountRemaining = item.AmountRemaining,
                        ExpirationDate = item.ExpirationDate ?? DateTime.MinValue,
                        IsHighlighted = previousItems.Count > 0 && !previousItems.Contains(shopItem.ItemId),
                    });
                }
            }
        }

        CmuneEventHandler.Route(new ShopRefreshCurrentItemListEvent());
    }

    internal void HighlightItem(int itemId, bool isHighlighted)
    {
        InventoryItem item;
        if (_inventoryItems.TryGetValue(itemId, out item) && item != null)
        {
            item.IsHighlighted = isHighlighted;
        }
    }

#if UNITY_EDITOR
    public void EnableAllItems()
    {
        Debug.Log("PopulateCompleteInventory");

        // First, clear the inventory
        _inventoryItems.Clear();

        // Add my inventory from the server
        foreach (IUnityItem item in ItemManager.Instance.ShopItems)
        {
            _inventoryItems.Add(item.ItemId, new InventoryItem(item)
            {
                IsPermanent = true,
                AmountRemaining = 0,
                ExpirationDate = DateTime.MaxValue,
            });
        }
    }
#endif
}