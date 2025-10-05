using System;
using System.Collections.Generic;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public enum AmmoType
{
    Cannon,
    Handgun,
    Launcher,
    Machinegun,
    Shotgun,
    Snipergun,
    Splattergun,
}

public static class AmmoDepot
{
    private static Dictionary<AmmoType, FastSecureInteger> _currentAmmo;
    private static Dictionary<AmmoType, FastSecureInteger> _startAmmo;
    private static Dictionary<AmmoType, FastSecureInteger> _maxAmmo;

    static AmmoDepot()
    {
        _currentAmmo = new Dictionary<AmmoType, FastSecureInteger>(7);
        _maxAmmo = new Dictionary<AmmoType, FastSecureInteger>(7);
        _startAmmo = new Dictionary<AmmoType, FastSecureInteger>(7);

        //_collectedAmmo = new Dictionary<AmmoType, int>(7);
        //_usedAmmo = new Dictionary<AmmoType, int>(7);

        foreach (AmmoType t in Enum.GetValues(typeof(AmmoType)))
        {
            _startAmmo.Add(t, new FastSecureInteger(100));
            _currentAmmo.Add(t, new FastSecureInteger(0));
            _maxAmmo.Add(t, new FastSecureInteger(200));
        }
    }

    public static void Reset()
    {
        _currentAmmo[AmmoType.Handgun].Value = _startAmmo[AmmoType.Handgun].Value;
        _currentAmmo[AmmoType.Machinegun].Value = _startAmmo[AmmoType.Machinegun].Value;
        _currentAmmo[AmmoType.Launcher].Value = _startAmmo[AmmoType.Launcher].Value;
        _currentAmmo[AmmoType.Shotgun].Value = _startAmmo[AmmoType.Shotgun].Value;
        _currentAmmo[AmmoType.Cannon].Value = _startAmmo[AmmoType.Cannon].Value;
        _currentAmmo[AmmoType.Splattergun].Value = _startAmmo[AmmoType.Splattergun].Value;
        _currentAmmo[AmmoType.Snipergun].Value = _startAmmo[AmmoType.Snipergun].Value;
    }

    public static void SetMaxAmmoForType(WeaponItem t, int maxAmmoCount)
    {
        if (PlayerDataManager.IsPlayerLoggedIn)
        {
            switch (t.ItemClass)
            {
                case UberstrikeItemClass.WeaponCannon: _maxAmmo[AmmoType.Cannon].Value = maxAmmoCount; break;
                case UberstrikeItemClass.WeaponHandgun: _maxAmmo[AmmoType.Handgun].Value = maxAmmoCount; break;
                case UberstrikeItemClass.WeaponLauncher: _maxAmmo[AmmoType.Launcher].Value = maxAmmoCount; break;
                case UberstrikeItemClass.WeaponMachinegun: _maxAmmo[AmmoType.Machinegun].Value = maxAmmoCount; break;
                case UberstrikeItemClass.WeaponShotgun: _maxAmmo[AmmoType.Shotgun].Value = maxAmmoCount; break;
                case UberstrikeItemClass.WeaponSniperRifle: _maxAmmo[AmmoType.Snipergun].Value = maxAmmoCount; break;
                case UberstrikeItemClass.WeaponSplattergun: _maxAmmo[AmmoType.Splattergun].Value = maxAmmoCount; break;
            }
        }
    }

    public static void SetStartAmmoForType(WeaponItem t, int startAmmoCount)
    {
        if (PlayerDataManager.IsPlayerLoggedIn)
        {
            switch (t.ItemClass)
            {
                case UberstrikeItemClass.WeaponCannon: _startAmmo[AmmoType.Cannon].Value = startAmmoCount; break;
                case UberstrikeItemClass.WeaponHandgun: _startAmmo[AmmoType.Handgun].Value = startAmmoCount; break;
                case UberstrikeItemClass.WeaponLauncher: _startAmmo[AmmoType.Launcher].Value = startAmmoCount; break;
                case UberstrikeItemClass.WeaponMachinegun: _startAmmo[AmmoType.Machinegun].Value = startAmmoCount; break;
                case UberstrikeItemClass.WeaponShotgun: _startAmmo[AmmoType.Shotgun].Value = startAmmoCount; break;
                case UberstrikeItemClass.WeaponSniperRifle: _startAmmo[AmmoType.Snipergun].Value = startAmmoCount; break;
                case UberstrikeItemClass.WeaponSplattergun: _startAmmo[AmmoType.Splattergun].Value = startAmmoCount; break;
            }
        }
    }

    public static bool CanAddAmmo(AmmoType t)
    {
        UberstrikeItemClass itemClass;
        if (TryGetAmmoTypeFromItemClass(t, out itemClass))
        {
            if (WeaponController.Instance.HasWeaponOfClass(itemClass))
            {
                return _currentAmmo[t].Value < _maxAmmo[t].Value;
            }
        }
        return false; // _currentAmmo[t] < _maxAmmo[t];
    }

    public static void AddAmmoOfClass(UberstrikeItemClass c)
    {
        AmmoType t;
        if (TryGetAmmoType(c, out t))
        {
            AddDefaultAmmoOfType(t);
        }
    }

    public static void AddDefaultAmmoOfType(AmmoType t)
    {
        AddAmmoOfType(t, _startAmmo[t].Value);
    }

    public static void AddAmmoOfType(AmmoType t, int bullets)
    {
        _currentAmmo[t].Value = Mathf.Clamp(_currentAmmo[t].Value + bullets, 0, _maxAmmo[t].Value);
    }

    public static void AddStartAmmoOfType(AmmoType t, float percentage = 1)
    {
        int bullets = Mathf.CeilToInt(_startAmmo[t].Value * percentage);
        _currentAmmo[t].Value = Mathf.Clamp(_currentAmmo[t].Value + bullets, 0, _maxAmmo[t].Value);
    }

    public static void AddMaxAmmoOfType(AmmoType t, float percentage = 1)
    {
        int bullets = Mathf.CeilToInt(_maxAmmo[t].Value * percentage);
        _currentAmmo[t].Value = Mathf.Clamp(_currentAmmo[t].Value + bullets, 0, _maxAmmo[t].Value);
    }

    public static bool HasAmmoOfType(AmmoType t)
    {
        return _currentAmmo[t].Value > 0;
    }

    public static bool HasAmmoOfClass(UberstrikeItemClass t)
    {
        AmmoType type;

        return t == UberstrikeItemClass.WeaponMelee || (TryGetAmmoType(t, out type) && HasAmmoOfType(type));
    }

    public static int AmmoOfType(AmmoType t)
    {
        return _currentAmmo[t].Value;
    }

    public static int AmmoOfClass(UberstrikeItemClass t)
    {
        AmmoType type;
        if (TryGetAmmoType(t, out type)) return AmmoOfType(type);
        else return 0;
    }

    private static bool TryGetAmmoType(UberstrikeItemClass item, out AmmoType t)
    {
        switch (item)
        {
            case UberstrikeItemClass.WeaponCannon: t = AmmoType.Cannon; return true;
            case UberstrikeItemClass.WeaponHandgun: t = AmmoType.Handgun; return true;
            case UberstrikeItemClass.WeaponLauncher: t = AmmoType.Launcher; return true;
            case UberstrikeItemClass.WeaponMachinegun: t = AmmoType.Machinegun; return true;
            case UberstrikeItemClass.WeaponShotgun: t = AmmoType.Shotgun; return true;
            case UberstrikeItemClass.WeaponSniperRifle: t = AmmoType.Snipergun; return true;
            case UberstrikeItemClass.WeaponSplattergun: t = AmmoType.Splattergun; return true;
            default: t = AmmoType.Handgun; return false;
        }
    }

    private static bool TryGetAmmoTypeFromItemClass(AmmoType t, out UberstrikeItemClass itemClass)
    {
        switch (t)
        {
            case AmmoType.Cannon: itemClass = UberstrikeItemClass.WeaponCannon; return true;
            case AmmoType.Handgun: itemClass = UberstrikeItemClass.WeaponHandgun; return true;
            case AmmoType.Launcher: itemClass = UberstrikeItemClass.WeaponLauncher; return true;
            case AmmoType.Machinegun: itemClass = UberstrikeItemClass.WeaponMachinegun; return true;
            case AmmoType.Shotgun: itemClass = UberstrikeItemClass.WeaponShotgun; return true;
            case AmmoType.Snipergun: itemClass = UberstrikeItemClass.WeaponSniperRifle; return true;
            case AmmoType.Splattergun: itemClass = UberstrikeItemClass.WeaponSplattergun; return true;
            default: itemClass = UberstrikeItemClass.WeaponHandgun; return false;
        }
    }

    public static bool UseAmmoOfType(AmmoType t)
    {
        int ammo = _currentAmmo[t].Value;
        //outOfAmmo = ammo > 1;

        if (ammo > 0)
        {
            _currentAmmo[t].Decrement(1);// = ammo - 1;
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool UseAmmoOfClass(UberstrikeItemClass t)
    {
        AmmoType type;
        if (TryGetAmmoType(t, out type))
            return UseAmmoOfType(type);
        else
            return false;
    }

    public static string ToString(AmmoType t)
    {
        return _currentAmmo[t].ToString();
    }

    public static void RemoveExtraAmmoOfType(UberstrikeItemClass t)
    {
        AmmoType ammoType;
        if (TryGetAmmoType(t, out ammoType))
        {
            if (_currentAmmo[ammoType].Value > _maxAmmo[ammoType].Value)
                _currentAmmo[ammoType].Value = _maxAmmo[ammoType].Value;
        }
    }
}
