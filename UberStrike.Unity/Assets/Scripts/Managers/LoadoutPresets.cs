using System.Collections.Generic;
using System.Text;
using UberStrike.Core.Types;
using UnityEngine;

// loadout templates: 3 per account in PlayerPrefs. load = equip each saved item that is still owned + valid
public static class LoadoutPresets
{
    public const int Count = 3;

    static readonly LoadoutSlotType[] Slots =
    {
        LoadoutSlotType.WeaponMelee, LoadoutSlotType.WeaponPrimary, LoadoutSlotType.WeaponSecondary, LoadoutSlotType.WeaponTertiary,
        LoadoutSlotType.QuickUseItem1, LoadoutSlotType.QuickUseItem2, LoadoutSlotType.QuickUseItem3,
        LoadoutSlotType.GearHead, LoadoutSlotType.GearFace, LoadoutSlotType.GearGloves, LoadoutSlotType.GearUpperBody,
        LoadoutSlotType.GearLowerBody, LoadoutSlotType.GearBoots, LoadoutSlotType.GearHolo,
    };

    static string Key(int preset)
    {
        return "LoadoutPreset" + preset + "_" + PlayerDataManager.CmidSecure;
    }

    static int CurrentId(LoadoutSlotType slot)
    {
        return LoadoutManager.Instance.HasLoadoutItem(slot) ? LoadoutManager.Instance.GetItemIdOnSlot(slot) : 0;
    }

    public static bool Has(int preset)
    {
        return PlayerPrefs.HasKey(Key(preset));
    }

    public static void Save(int preset)
    {
        var sb = new StringBuilder();
        foreach (LoadoutSlotType slot in Slots)
            sb.Append((int)slot).Append('=').Append(CurrentId(slot)).Append(';');
        PlayerPrefs.SetString(Key(preset), sb.ToString());
        PlayerPrefs.Save();
        Invalidate();
    }

    public static Dictionary<LoadoutSlotType, int> Get(int preset)
    {
        var ids = new Dictionary<LoadoutSlotType, int>();
        foreach (string pair in PlayerPrefs.GetString(Key(preset), string.Empty).Split(';'))
        {
            string[] kv = pair.Split('=');
            int slot, id;
            if (kv.Length == 2 && int.TryParse(kv[0], out slot) && int.TryParse(kv[1], out id))
                ids[(LoadoutSlotType)slot] = id;
        }
        return ids;
    }

    static bool Fits(LoadoutSlotType slot, InventoryItem item)
    {
        IUnityItem i = item.Item;
        if (LoadoutManager.Instance.IsWeaponSlotType(slot))
            return i.ItemType == UberstrikeItemType.Weapon && (i.ItemClass == UberstrikeItemClass.WeaponMelee) == (slot == LoadoutSlotType.WeaponMelee);
        if (LoadoutManager.Instance.IsQuickItemSlotType(slot))
            return i.ItemType == UberstrikeItemType.QuickUse;
        return i.ItemType == UberstrikeItemType.Gear && PlayerDataManager.GetSlotTypeForItemClass(i.ItemClass) == slot;
    }

    // returns how many saved items could not be equipped (sold / expired / wrong slot)
    public static int Load(int preset)
    {
        int skipped = 0;
        foreach (KeyValuePair<LoadoutSlotType, int> p in Get(preset))
        {
            LoadoutSlotType slot = p.Key;
            if (CurrentId(slot) == p.Value) continue;

            InventoryItem item;
            bool ok = p.Value > 0 && InventoryManager.Instance.TryGetInventoryItem(p.Value, out item) && item.IsValid && Fits(slot, item);
            if (ok)
            {
                InventoryManager.Instance.EquipItemOnSlot(p.Value, slot);
            }
            else
            {
                if (p.Value > 0) skipped++;
                // gear keeps what is on (default gear); weapon + quick slots follow the template
                if (LoadoutManager.Instance.IsWeaponSlotType(slot)) InventoryManager.Instance.UnequipWeaponSlot(slot);
                else if (LoadoutManager.Instance.IsQuickItemSlotType(slot)) LoadoutManager.Instance.ResetSlot(slot);
            }
        }
        return skipped;
    }

    // OnGUI cache: PlayerPrefs + names only rebuilt when the loadout or a template changes
    static readonly bool[] _has = new bool[Count + 1];
    static readonly string[] _desc = new string[Count + 1];
    static int _cachedVersion = -1;
    static int _cachedCmid = -1;

    public static void Invalidate()
    {
        _cachedVersion = -1;
    }

    public static bool HasCached(int preset)
    {
        Refresh();
        return _has[preset];
    }

    public static string DescribeCached(int preset)
    {
        Refresh();
        return _desc[preset];
    }

    static void Refresh()
    {
        int v = LoadoutManager.Instance.EquippedVersion;
        if (v == _cachedVersion && PlayerDataManager.CmidSecure == _cachedCmid) return;
        _cachedVersion = v;
        _cachedCmid = PlayerDataManager.CmidSecure;
        for (int i = 1; i <= Count; i++)
        {
            _has[i] = Has(i);
            _desc[i] = Describe(i);
        }
    }

    public static string Describe(int preset)
    {
        if (!Has(preset)) return "Empty. Save stores your current loadout here.";
        var sb = new StringBuilder("Template " + preset + ":");
        foreach (KeyValuePair<LoadoutSlotType, int> p in Get(preset))
        {
            InventoryItem item;
            if (p.Value > 0 && LoadoutManager.Instance.IsWeaponSlotType(p.Key) && InventoryManager.Instance.TryGetInventoryItem(p.Value, out item) && item.Item != null)
                sb.Append('\n').Append(item.Item.Name);
        }
        return sb.ToString();
    }
}
