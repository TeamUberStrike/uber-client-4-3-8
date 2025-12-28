
using System.Collections.Generic;
using System.Linq;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public class LoadoutManager : Singleton<LoadoutManager>
{
    private Dictionary<LoadoutSlotType, int> _loadout;

    public static readonly LoadoutSlotType[] QuickSlots = new LoadoutSlotType[] { LoadoutSlotType.QuickUseItem1, LoadoutSlotType.QuickUseItem2, LoadoutSlotType.QuickUseItem3 };
    public static readonly LoadoutSlotType[] WeaponSlots = new LoadoutSlotType[] { LoadoutSlotType.WeaponMelee, LoadoutSlotType.WeaponPrimary, LoadoutSlotType.WeaponSecondary, LoadoutSlotType.WeaponTertiary };
    public static readonly LoadoutSlotType[] GearSlots = new LoadoutSlotType[] { LoadoutSlotType.GearHead, LoadoutSlotType.GearFace, LoadoutSlotType.GearGloves, LoadoutSlotType.GearUpperBody, LoadoutSlotType.GearLowerBody, LoadoutSlotType.GearBoots };
    public static readonly UberstrikeItemClass[] GearSlotClasses = new UberstrikeItemClass[] { UberstrikeItemClass.GearHead, UberstrikeItemClass.GearFace, UberstrikeItemClass.GearGloves, UberstrikeItemClass.GearUpperBody, UberstrikeItemClass.GearLowerBody, UberstrikeItemClass.GearBoots };

    public static readonly string[] GearSlotNames = new string[] { LocalizedStrings.Head, LocalizedStrings.Face, LocalizedStrings.Gloves, LocalizedStrings.UpperBody, LocalizedStrings.LowerBody, LocalizedStrings.Boots };

    private LoadoutManager()
    {
        _loadout = new Dictionary<LoadoutSlotType, int>()
        {
            { LoadoutSlotType.GearHead, UberStrikeCommonConfig.DefaultHead },
            { LoadoutSlotType.GearFace, UberStrikeCommonConfig.DefaultFace },
            { LoadoutSlotType.GearUpperBody, UberStrikeCommonConfig.DefaultUpperBody },
            { LoadoutSlotType.GearLowerBody, UberStrikeCommonConfig.DefaultLowerBody },
            { LoadoutSlotType.GearGloves, UberStrikeCommonConfig.DefaultGloves },
            { LoadoutSlotType.GearBoots, UberStrikeCommonConfig.DefaultBoots },
        };

        // fill out the default values
        foreach (LoadoutSlotType slot in System.Enum.GetValues(typeof(LoadoutSlotType)))
        {
            if (!_loadout.ContainsKey(slot))
                _loadout[slot] = 0;
        }
    }

    public WeaponItem GetEquippedWeapon(UberstrikeItemClass type)
    {
        InventoryItem item;
        if (TryGetItemInSlot(LoadoutSlotType.WeaponMelee, out item) && item.Item.ItemClass == type)
            return item.Item as WeaponItem;
        else if (TryGetItemInSlot(LoadoutSlotType.WeaponPrimary, out item) && item.Item.ItemClass == type)
            return item.Item as WeaponItem;
        else if (TryGetItemInSlot(LoadoutSlotType.WeaponSecondary, out item) && item.Item.ItemClass == type)
            return item.Item as WeaponItem;
        else if (TryGetItemInSlot(LoadoutSlotType.WeaponTertiary, out item) && item.Item.ItemClass == type)
            return item.Item as WeaponItem;
        else return null;
    }

    public void EquipWeapon(LoadoutSlotType weaponSlot, WeaponItem itemWeapon)
    {
        BaseWeaponDecorator weapon = null;
        if (itemWeapon != null)
        {
            var go = GameObject.Instantiate(ItemManager.Instance.GetPrefab(itemWeapon.ItemId)) as GameObject;
            weapon = go.GetComponent<BaseWeaponDecorator>();
            weapon.EnableShootAnimation = false;
        }

        switch (weaponSlot)
        {
            case LoadoutSlotType.WeaponMelee:
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponMelee, weapon);
                break;
            case LoadoutSlotType.WeaponPrimary:
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponPrimary, weapon);
                break;
            case LoadoutSlotType.WeaponSecondary:
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponSecondary, weapon);
                break;
            case LoadoutSlotType.WeaponTertiary:
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponTertiary, weapon);
                break;
            case LoadoutSlotType.WeaponPickup:
                GameState.LocalDecorator.AssignWeapon(LoadoutSlotType.WeaponPickup, weapon);
                break;
        }

        if (weapon != null)
            GameState.LocalDecorator.ShowWeapon(weaponSlot);
    }

    public int[] SetLoadoutWeapons(int[] weaponIds)
    {
        int[] previousWeaponIds = new int[4];

        for (int i = 0; i < WeaponSlots.Length; i++)
        {
            LoadoutSlotType s = WeaponSlots[i];
            previousWeaponIds[i] = _loadout[s];
            _loadout[s] = weaponIds[i];
        }

        return previousWeaponIds;
    }

    public void RefreshLoadoutFromServerCache(LoadoutView view)
    {
        try
        {
            // gear
            _loadout[LoadoutSlotType.GearHead] = InventoryManager.Instance.GetGearItem(view.Head, UberstrikeItemClass.GearHead);
            _loadout[LoadoutSlotType.GearFace] = InventoryManager.Instance.GetGearItem(view.Face, UberstrikeItemClass.GearFace);
            _loadout[LoadoutSlotType.GearGloves] = InventoryManager.Instance.GetGearItem(view.Gloves, UberstrikeItemClass.GearGloves);
            _loadout[LoadoutSlotType.GearUpperBody] = InventoryManager.Instance.GetGearItem(view.UpperBody, UberstrikeItemClass.GearUpperBody);
            _loadout[LoadoutSlotType.GearLowerBody] = InventoryManager.Instance.GetGearItem(view.LowerBody, UberstrikeItemClass.GearLowerBody);
            _loadout[LoadoutSlotType.GearBoots] = InventoryManager.Instance.GetGearItem(view.Boots, UberstrikeItemClass.GearBoots);
            _loadout[LoadoutSlotType.GearHolo] = InventoryManager.Instance.GetGearItem(view.Webbing, UberstrikeItemClass.GearHolo);

            // quick 
            _loadout[LoadoutSlotType.QuickUseItem1] = view.QuickItem1;
            _loadout[LoadoutSlotType.QuickUseItem2] = view.QuickItem2;
            _loadout[LoadoutSlotType.QuickUseItem3] = view.QuickItem3;

            // funcational 
            _loadout[LoadoutSlotType.FunctionalItem1] = view.FunctionalItem1;
            _loadout[LoadoutSlotType.FunctionalItem2] = view.FunctionalItem2;
            _loadout[LoadoutSlotType.FunctionalItem3] = view.FunctionalItem3;

            // weapons
            _loadout[LoadoutSlotType.WeaponMelee] = view.MeleeWeapon;
            _loadout[LoadoutSlotType.WeaponPrimary] = view.Weapon1;
            _loadout[LoadoutSlotType.WeaponSecondary] = view.Weapon2;
            _loadout[LoadoutSlotType.WeaponTertiary] = view.Weapon3;

            UpdateArmor();
        }
        catch
        {
            throw;
        }
    }

    public bool RemoveDuplicateWeaponClass(InventoryItem baseItem)
    {
        LoadoutSlotType lastRemovedSlot = LoadoutSlotType.None;

        return RemoveDuplicateWeaponClass(baseItem, ref lastRemovedSlot);
    }

    public bool RemoveDuplicateWeaponClass(InventoryItem baseItem, ref LoadoutSlotType updatedSlot)
    {
        bool result = false;

        // Is the Item a Weapon?
        if (baseItem != null && baseItem.Item.ItemType == UberstrikeItemType.Weapon)
        {
            foreach (LoadoutSlotType slot in WeaponSlots)
            {
                InventoryItem item;
                if (TryGetItemInSlot(slot, out item) && item.Item.ItemClass == baseItem.Item.ItemClass && item.Item.ItemId != baseItem.Item.ItemId)
                {
                    GameState.LocalDecorator.AssignWeapon(slot, null);
                    ResetSlot(slot);
                    updatedSlot = slot;

                    result = true;
                    break;
                }
            }
        }

        return result;
    }

    public bool RemoveDuplicateQuickItemClass(QuickItemConfiguration item, ref LoadoutSlotType lastRemovedSlot)
    {
        bool result = false;

        if (item != null && item.ItemType == UberstrikeItemType.QuickUse)
        {
            InventoryItem quickItem;
            if (TryGetItemInSlot(LoadoutSlotType.QuickUseItem1, out quickItem) &&
                quickItem.Item is QuickItem &&
                ((QuickItem)quickItem.Item).Configuration.BehaviourType == item.BehaviourType)
            {
                ResetSlot(LoadoutSlotType.QuickUseItem1);
                result = true;
                lastRemovedSlot = LoadoutSlotType.QuickUseItem1;
            }
            if (TryGetItemInSlot(LoadoutSlotType.QuickUseItem2, out quickItem) &&
                quickItem.Item is QuickItem &&
                ((QuickItem)quickItem.Item).Configuration.BehaviourType == item.BehaviourType)
            {
                ResetSlot(LoadoutSlotType.QuickUseItem2);
                result = true;
                lastRemovedSlot = LoadoutSlotType.QuickUseItem2;
            }
            if (TryGetItemInSlot(LoadoutSlotType.QuickUseItem3, out quickItem) &&
                quickItem.Item is QuickItem &&
                ((QuickItem)quickItem.Item).Configuration.BehaviourType == item.BehaviourType)
            {
                ResetSlot(LoadoutSlotType.QuickUseItem3);
                result = true;
                lastRemovedSlot = LoadoutSlotType.QuickUseItem3;
            }
        }
        return result;
    }

    public bool RemoveDuplicateFunctionalItemClass(InventoryItem inventoryItem, ref LoadoutSlotType lastRemovedSlot)
    {
        bool result = false;

        if (inventoryItem != null && inventoryItem.Item.ItemType == UberstrikeItemType.Functional)
        {
            if (HasLoadoutItem(LoadoutSlotType.FunctionalItem1) && GetItemOnSlot<FunctionalItem>(LoadoutSlotType.FunctionalItem1).ItemClass == inventoryItem.Item.ItemClass)
            {
                ResetSlot(LoadoutSlotType.FunctionalItem1);
                result = true;
                lastRemovedSlot = LoadoutSlotType.FunctionalItem1;
            }
            if (HasLoadoutItem(LoadoutSlotType.FunctionalItem2) && GetItemOnSlot<FunctionalItem>(LoadoutSlotType.FunctionalItem2).ItemClass == inventoryItem.Item.ItemClass)
            {
                ResetSlot(LoadoutSlotType.FunctionalItem2);
                result = true;
                lastRemovedSlot = LoadoutSlotType.FunctionalItem2;
            }
            if (HasLoadoutItem(LoadoutSlotType.FunctionalItem3) && GetItemOnSlot<FunctionalItem>(LoadoutSlotType.FunctionalItem3).ItemClass == inventoryItem.Item.ItemClass)
            {
                ResetSlot(LoadoutSlotType.FunctionalItem3);
                result = true;
                lastRemovedSlot = LoadoutSlotType.FunctionalItem3;
            }
        }
        return result;
    }

    public bool SwitchWeaponsInLoadout(int firstSlot, int secondSlot) // make this nicer
    {
        bool result = true;
        InventoryItem temp;
        if (firstSlot == secondSlot) return true; // exit if swich same slot

        switch (firstSlot)
        {
            case 1:
                temp = GetItemOnSlot(LoadoutSlotType.WeaponPrimary);
                switch (secondSlot)
                {
                    case 2:
                        SetSlot(LoadoutSlotType.WeaponPrimary, GetItemOnSlot(LoadoutSlotType.WeaponSecondary));
                        SetSlot(LoadoutSlotType.WeaponSecondary, temp);
                        break;
                    case 3:
                        SetSlot(LoadoutSlotType.WeaponPrimary, GetItemOnSlot(LoadoutSlotType.WeaponTertiary));
                        SetSlot(LoadoutSlotType.WeaponTertiary, temp);
                        break;
                    default:
                        result = false;
                        break;
                }
                break;
            case 2:
                temp = GetItemOnSlot(LoadoutSlotType.WeaponSecondary);
                switch (secondSlot)
                {
                    case 1:
                        SetSlot(LoadoutSlotType.WeaponSecondary, GetItemOnSlot(LoadoutSlotType.WeaponPrimary));
                        SetSlot(LoadoutSlotType.WeaponPrimary, temp);
                        break;
                    case 3:
                        SetSlot(LoadoutSlotType.WeaponSecondary, GetItemOnSlot(LoadoutSlotType.WeaponTertiary));
                        SetSlot(LoadoutSlotType.WeaponTertiary, temp);
                        break;
                    default:
                        result = false;
                        break;
                }
                break;
            case 3:
                temp = GetItemOnSlot(LoadoutSlotType.WeaponTertiary);
                switch (secondSlot)
                {
                    case 2:
                        SetSlot(LoadoutSlotType.WeaponTertiary, GetItemOnSlot(LoadoutSlotType.WeaponSecondary));
                        SetSlot(LoadoutSlotType.WeaponSecondary, temp);
                        break;
                    case 1:
                        SetSlot(LoadoutSlotType.WeaponTertiary, GetItemOnSlot(LoadoutSlotType.WeaponPrimary));
                        SetSlot(LoadoutSlotType.WeaponPrimary, temp);
                        break;
                    default:
                        result = false;
                        break;
                }
                break;
            default:
                result = false;
                break;
        }

        return result;
    }

    public bool SwitchQuickItemInLoadout(int firstSlot, int secondSlot) // make this nicer
    {
        bool result = true;
        InventoryItem temp;
        if (firstSlot == secondSlot) return true; // exit if swich same slot

        switch (firstSlot)
        {
            case 1:
                temp = GetItemOnSlot(LoadoutSlotType.QuickUseItem1);
                switch (secondSlot)
                {
                    case 2:
                        SetSlot(LoadoutSlotType.QuickUseItem1, GetItemOnSlot(LoadoutSlotType.QuickUseItem2));
                        SetSlot(LoadoutSlotType.QuickUseItem2, temp);
                        break;
                    case 3:
                        SetSlot(LoadoutSlotType.QuickUseItem1, GetItemOnSlot(LoadoutSlotType.QuickUseItem3));
                        SetSlot(LoadoutSlotType.QuickUseItem3, temp);
                        break;
                    default:
                        result = false;
                        break;
                }
                break;
            case 2:
                temp = GetItemOnSlot(LoadoutSlotType.QuickUseItem2);
                switch (secondSlot)
                {
                    case 1:
                        SetSlot(LoadoutSlotType.QuickUseItem2, GetItemOnSlot(LoadoutSlotType.QuickUseItem1));
                        SetSlot(LoadoutSlotType.QuickUseItem1, temp);
                        break;
                    case 3:
                        SetSlot(LoadoutSlotType.QuickUseItem2, GetItemOnSlot(LoadoutSlotType.QuickUseItem3));
                        SetSlot(LoadoutSlotType.QuickUseItem3, temp);
                        break;
                    default:
                        result = false;
                        break;
                }
                break;
            case 3:
                temp = GetItemOnSlot(LoadoutSlotType.QuickUseItem3);
                switch (secondSlot)
                {
                    case 2:
                        SetSlot(LoadoutSlotType.QuickUseItem3, GetItemOnSlot(LoadoutSlotType.QuickUseItem2));
                        SetSlot(LoadoutSlotType.QuickUseItem2, temp);
                        break;
                    case 1:
                        SetSlot(LoadoutSlotType.QuickUseItem3, GetItemOnSlot(LoadoutSlotType.QuickUseItem1));
                        SetSlot(LoadoutSlotType.QuickUseItem1, temp);
                        break;
                    default:
                        result = false;
                        break;
                }
                break;
            default:
                result = false;
                break;
        }

        return result;
    }

    public bool IsWeaponSlotType(LoadoutSlotType slot)
    {
        return slot == LoadoutSlotType.WeaponMelee || slot == LoadoutSlotType.WeaponPrimary || slot == LoadoutSlotType.WeaponSecondary || slot == LoadoutSlotType.WeaponTertiary;
    }

    public bool IsQuickItemSlotType(LoadoutSlotType slot)
    {
        return slot == LoadoutSlotType.QuickUseItem1 || slot == LoadoutSlotType.QuickUseItem2 || slot == LoadoutSlotType.QuickUseItem3;
    }

    public bool IsFunctionalItemSlotType(LoadoutSlotType slot)
    {
        return slot == LoadoutSlotType.FunctionalItem1 || slot == LoadoutSlotType.FunctionalItem2 || slot == LoadoutSlotType.FunctionalItem3;
    }

    public bool SwapLoadoutItems(LoadoutSlotType slotA, LoadoutSlotType slotB)
    {
        bool isSuccess = false;
        if (slotA != slotB)
        {
            if (IsWeaponSlotType(slotA) && IsWeaponSlotType(slotB))
            {
                InventoryItem a = null, b = null;
                TryGetItemInSlot(slotA, out a);
                TryGetItemInSlot(slotB, out b);
                if (a != null || b != null)
                {
                    SetLoadoutItem(slotA, b);
                    SetLoadoutItem(slotB, a);
                    if (b != null) EquipWeapon(slotA, b.Item as WeaponItem);
                    if (a != null) EquipWeapon(slotB, a.Item as WeaponItem);
                    isSuccess = true;
                }
            }
            else if ((IsQuickItemSlotType(slotA) && IsQuickItemSlotType(slotB)) || IsFunctionalItemSlotType(slotA) && IsFunctionalItemSlotType(slotB))
            {
                InventoryItem a = null, b = null;
                TryGetItemInSlot(slotA, out a);
                TryGetItemInSlot(slotB, out b);
                if (a != null || b != null)
                {
                    SetLoadoutItem(slotA, b);
                    SetLoadoutItem(slotB, a);
                    isSuccess = true;
                }
            }
        }

        return isSuccess;
    }

    public void ResetSlot(LoadoutSlotType loadoutSlotType)
    {
        SetLoadoutItem(loadoutSlotType, null);
    }

    public void SetSlot(LoadoutSlotType loadoutSlotType, InventoryItem item)
    {
        SetSlot(loadoutSlotType, item != null ? item.Item : null);
    }

    public void SetSlot(LoadoutSlotType loadoutSlotType, IUnityItem item)
    {
        // unequip item
        InventoryItem inventoryItem;
        if (item == null)
        {
            SetLoadoutItem(loadoutSlotType, null);
        }
        // equip existing item
        else if (InventoryManager.Instance.TryGetInventoryItem(item.ItemId, out inventoryItem) && inventoryItem.IsValid)
        {
            if (item.ItemType == UberstrikeItemType.Weapon)
            {
                RemoveDuplicateWeaponClass(inventoryItem);
                EquipWeapon(loadoutSlotType, inventoryItem.Item as WeaponItem);
            }

            SetLoadoutItem(loadoutSlotType, inventoryItem);
        }
        // renew item since it is expired
        else if (item.ItemView != null)
        {
            BuyPanelGUI panel = PanelManager.Instance.OpenPanel(PanelType.BuyItem) as BuyPanelGUI;
            if (panel)
            {
                panel.SetItem(item, BuyingLocationType.Shop, BuyingRecommendationType.None);
            }
        }
    }

    public void GetArmorValues(out int armorPoints, out int absorbtionRatio)
    {
        armorPoints = absorbtionRatio = 0;

        InventoryItem item;
        if (TryGetItemInSlot(LoadoutSlotType.GearLowerBody, out item) && item.Item is GearItem)
        {
            GearItem i = item.Item as GearItem;
            armorPoints += i.Configuration.ArmorPoints;
            absorbtionRatio += i.Configuration.ArmorAbsorptionPercent;
        }
        if (TryGetItemInSlot(LoadoutSlotType.GearUpperBody, out item) && item.Item is GearItem)
        {
            GearItem i = item.Item as GearItem;
            armorPoints += i.Configuration.ArmorPoints;
            absorbtionRatio += i.Configuration.ArmorAbsorptionPercent;
        }
        if (TryGetItemInSlot(LoadoutSlotType.GearHolo, out item) && item.Item is GearItem)
        {
            GearItem i = item.Item as GearItem;
            armorPoints += i.Configuration.ArmorPoints;
            absorbtionRatio += i.Configuration.ArmorAbsorptionPercent;
        }
    }

    public bool HasLoadoutItem(LoadoutSlotType loadoutSlotType)
    {
        int item;
        return _loadout.TryGetValue(loadoutSlotType, out item) && item > 0;
    }

    public int GetItemIdOnSlot(LoadoutSlotType loadoutSlotType)
    {
        return _loadout[loadoutSlotType];
    }

    private static readonly InventoryItem EmptyItem = new InventoryItem(null);

    public InventoryItem GetItemOnSlot(LoadoutSlotType loadoutSlotType)
    {
        InventoryItem inventoryItem;
        if (InventoryManager.Instance.TryGetInventoryItem(GetItemIdOnSlot(loadoutSlotType), out inventoryItem))
            return inventoryItem;
        else
            return EmptyItem;
    }

    public T GetItemOnSlot<T>(LoadoutSlotType loadoutSlotType) where T : class, IUnityItem
    {
        InventoryItem inventoryItem;
        if (InventoryManager.Instance.TryGetInventoryItem(GetItemIdOnSlot(loadoutSlotType), out inventoryItem))
        {
            return (T)inventoryItem.Item;
        }
        else return default(T);
    }

    public Dictionary<LoadoutSlotType, int> GetCurrentLoadoutIds()
    {
        return new Dictionary<LoadoutSlotType, int>(6)
        {
            { LoadoutSlotType.GearHead, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHead) },
            { LoadoutSlotType.GearFace, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearFace) },
            { LoadoutSlotType.GearGloves, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearGloves) },
            { LoadoutSlotType.GearUpperBody, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearUpperBody) },
            { LoadoutSlotType.GearLowerBody, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearLowerBody) },
            { LoadoutSlotType.GearBoots, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearBoots) },
            { LoadoutSlotType.GearHolo, LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHolo) },
        };
    }

    public void SetLoadoutItem(LoadoutSlotType loadoutSlotType, InventoryItem item)
    {
        _loadout[loadoutSlotType] = (item != null && item.Item != null) ? item.Item.ItemId : 0;

        MonoRoutine.Start(PlayerDataManager.Instance.StartSetLoadout());

        UpdateArmor();
    }

    public bool IsItemEquipped(int itemId)
    {
        return _loadout.Any(i => i.Value == itemId);
    }

    public bool HasItemInSlot(LoadoutSlotType slot)
    {
        InventoryItem item;
        return TryGetItemInSlot(slot, out item);
    }

    public bool TryGetItemInSlot(LoadoutSlotType slot, out InventoryItem item)
    {
        item = null;
        int itemId;
        return _loadout.TryGetValue(slot, out itemId) && InventoryManager.Instance.TryGetInventoryItem(itemId, out item);
    }

    public bool TryGetSlotForItem(int itemId, out LoadoutSlotType slot)
    {
        slot = LoadoutSlotType.None;
        foreach (var v in _loadout)
        {
            if (v.Value == itemId)
            {
                slot = v.Key;
                return true;
            }
        }

        return false;
    }

    public bool ValidateLoadout()
    {
        return (_loadout.Count > 0);
    }

    public void UpdateArmor()
    {
        int armor, absorb;
        GetArmorValues(out armor, out absorb);

        ArmorHud.Instance.ArmorCarried = armor;
        ArmorHud.Instance.DefenseBonus = absorb;
    }
}