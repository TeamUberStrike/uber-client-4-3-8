using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponSelectorHud
{
    public bool Enabled
    {
        get { return _weaponList.Enabled; }
        set { _weaponList.Enabled = value; }
    }

    public WeaponSelectorHud()
    {
        _loadoutWeapons = new Dictionary<LoadoutSlotType, WeaponItem>();
        _weaponIndicesInList = new Dictionary<LoadoutSlotType, int>();
        _weaponListDisplayTime = 3.0f;

        if (HudAssets.Exists)
        {
            _weaponList = new MeshGUIList(OnDrawWeaponList);
            _weaponList.Enabled = false;
        }
    }

    public void Draw()
    {
        _weaponList.Draw();
    }

    public void Update()
    {
        _weaponList.Update();
    }

    public void SetSlotWeapon(LoadoutSlotType slot, WeaponItem weapon)
    {
        if (weapon != null)
        {
            _loadoutWeapons[slot] = weapon;
        }
        else
        {
            _loadoutWeapons.Remove(slot);
        }
        OnWeaponSlotsChange();
    }

    public WeaponItem GetLoadoutWeapon(LoadoutSlotType loadoutSlotType)
    {
        if (_loadoutWeapons.ContainsKey(loadoutSlotType))
        {
            return _loadoutWeapons[loadoutSlotType];
        }
        return null;
    }

    public void SetActiveWeaponLoadout(LoadoutSlotType loadoutSlotType)
    {
        if (_loadoutWeapons.ContainsKey(loadoutSlotType))
        {
            _weaponList.AnimToIndex(_weaponIndicesInList[loadoutSlotType], 0.1f);
            OnWeaponListTrigger();
        }
    }

    private void OnWeaponSlotsChange()
    {
        ResetWeaponListItems();
        OnWeaponListTrigger();
    }

    private void OnWeaponListTrigger()
    {
        _weaponListHideTime = Time.time + _weaponListDisplayTime;
        _isWeaponListFadingOut = false;
    }

    private void OnDrawWeaponList()
    {
        if (CanWeaponListFadeOut())
        {
            FadeOutWeaponList();
        }
    }

    private bool CanWeaponListFadeOut()
    {
        return Time.time > _weaponListHideTime && _isWeaponListFadingOut == false;
    }

    private void FadeOutWeaponList()
    {
        _isWeaponListFadingOut = true;
        _weaponList.FadeOut(1.0f, EaseType.Out);
    }

    private void ResetWeaponListItems()
    {
        int slotCount = LoadoutSlotType.WeaponPickup - LoadoutSlotType.WeaponMelee + 1;
        int listIndex = 0;
        _weaponIndicesInList.Clear();
        _weaponList.ClearAllItems();
        for (int i = 0; i < slotCount; i++)
        {
            LoadoutSlotType loadoutType = LoadoutSlotType.WeaponMelee + i;
            if (!_loadoutWeapons.ContainsKey(loadoutType))
            {
                continue;
            }
            string weaponName = _loadoutWeapons[loadoutType].Name;
            _weaponIndicesInList.Add(loadoutType, listIndex++);
            _weaponList.AddItem(weaponName);
        }
    }

    private Dictionary<LoadoutSlotType, WeaponItem> _loadoutWeapons;
    private Dictionary<LoadoutSlotType, int> _weaponIndicesInList;
    private MeshGUIList _weaponList;

    private float _weaponListHideTime;
    private float _weaponListDisplayTime;
    private bool _isWeaponListFadingOut;
}
