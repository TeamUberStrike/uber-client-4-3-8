using System.Collections.Generic;
using UnityEngine;
using Cmune.Util;

public class TemporaryLoadoutManager : Singleton<TemporaryLoadoutManager>
{
    private Dictionary<LoadoutSlotType, int> _temporaryGearLoadout;

    private TemporaryLoadoutManager()
    {
        _temporaryGearLoadout = new Dictionary<LoadoutSlotType, int>();
    }

    public void SetGearLoadout(LoadoutSlotType slot, IUnityItem item)
    {
        if (item != null)
            _temporaryGearLoadout[slot] = item.ItemId;
        else
            _temporaryGearLoadout.Remove(slot);

        UpdateGearLoadout();
    }

    public void UpdateGearLoadout()
    {
        List<IUnityItem> items = new List<IUnityItem>(_temporaryGearLoadout.Count);
        foreach (var i in _temporaryGearLoadout.Values)
        {
            var item = ItemManager.Instance.GetItemInShop(i);
            if (item != null)
            {
                items.Add(item);
            }
        }

        AvatarBuilder.Instance.UpdateLocalAvatarGear(items);

        //check if the current shop loadout is matching the REAL loadout fo the player
        IsGearLoadoutModified = false;

        foreach (var slot in _temporaryGearLoadout)
        {
            if (slot.Value != LoadoutManager.Instance.GetItemIdOnSlot(slot.Key))
            {
                IsGearLoadoutModified = true;
            }
        }

        if (GameState.LocalDecorator != null)
            GameState.LocalDecorator.HideWeapons();
    }

    public bool IsGearLoadoutModified { get; private set; }

    public bool IsGearLoadoutModifiedOnSlot(LoadoutSlotType slot)
    {
        int id;
        return _temporaryGearLoadout.TryGetValue(slot, out id) && id != LoadoutManager.Instance.GetItemIdOnSlot(slot);
    }

    public void ResetGearLoadout(LoadoutSlotType slot)
    {
        _temporaryGearLoadout.Remove(slot);
    }

    public void ResetGearLoadout()
    {
        bool needsUpdate = false;

        foreach (var slot in _temporaryGearLoadout.Keys)
        {
            if (IsGearLoadoutModifiedOnSlot(slot))
            {
                needsUpdate = true;
            }
        }

        _temporaryGearLoadout.Clear();

        if (needsUpdate)
            UpdateGearLoadout();
    }
}