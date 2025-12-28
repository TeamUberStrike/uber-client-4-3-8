using System.Collections.Generic;
using Cmune.Realtime.Common.Utils;
using UberStrike.Realtime.Common;
using UnityEngine;

public interface IWeaponController
{
    int NextProjectileId();
    byte PlayerNumber { get; }
    bool IsLocal { get; }
    Vector3 ShootingPoint { get; }
    Vector3 ShootingDirection { get; }
}

public class WeaponSimulator : IWeaponController
{
    public WeaponSimulator(CharacterConfig config)
    {
        _config = config;
        _weaponSlots = new WeaponSlot[5];

        CurrentSlotIndex = -1;
    }

    public void Update(CharacterInfo state, bool isLocal)
    {
        if (_avatar != null && state != null && state.IsAlive)
        {
            if (!isLocal && state.IsFiring)
                Shoot(state);
        }
    }

    /// <summary>
    /// Called when the remote Avatar is firing
    /// </summary>
    public void Shoot(CharacterInfo state)
    {
        if (state != null && _nextShootTime < Time.time)
        {
            if (_currentSlot != null)
            {
                _nextShootTime = Time.time + WeaponConfigurationHelper.GetRateOfFire(_currentSlot.Logic.Config);

                if (_isFullSimulation)
                {
                    BeginShooting();
                    {
                        CmunePairList<BaseGameProp, ShotPoint> hits;
                        _currentSlot.Logic.Shoot(new Ray(state.ShootingPoint + LocalPlayer.EyePosition, state.ShootingDirection), out hits);
                    }
                    EndShooting();
                }
            }
            else
            {
                Debug.LogError("Current weapon is null!");
            }
        }
    }

    public IProjectile EmitProjectile(int actorID, byte playerNumber, Vector3 origin, Vector3 direction, LoadoutSlotType slot, int projectileId, bool explode)
    {
        IProjectile p = null;

        if (_isFullSimulation)
        {
            BeginShooting();
            {
                switch (slot)
                {
                    case LoadoutSlotType.WeaponPrimary:
                        p = ShootProjectileFromSlot(1, origin, direction, projectileId, explode, actorID);
                        break;
                    case LoadoutSlotType.WeaponSecondary:
                        p = ShootProjectileFromSlot(2, origin, direction, projectileId, explode, actorID);
                        break;
                    case LoadoutSlotType.WeaponTertiary:
                        p = ShootProjectileFromSlot(3, origin, direction, projectileId, explode, actorID);
                        break;
                    case LoadoutSlotType.WeaponPickup:
                        p = ShootProjectileFromSlot(4, origin, direction, projectileId, explode, actorID);
                        break;
                }
            }
            EndShooting();
        }

        return p;
    }

    /// <summary>
    /// To avoid hitting the own colliders when raycasting, we put all colliders into the ignore layer until the raycast is performed
    /// Always pair this call with EndShooting();
    /// </summary>
    private void BeginShooting()
    {
        foreach (CharacterHitArea a in _avatar.HitAreas)
        {
            a.gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void EndShooting()
    {
        foreach (CharacterHitArea a in _avatar.HitAreas)
        {
            a.gameObject.layer = _avatar.gameObject.layer;
        }
    }

    private IProjectile ShootProjectileFromSlot(int slot, Vector3 origin, Vector3 direction, int projectileID, bool explode, int actorID)
    {
        if (_weaponSlots.Length > slot && _weaponSlots[slot] != null)
        {
            ProjectileWeapon pw = _weaponSlots[slot].Logic as ProjectileWeapon;
            if (pw != null)
            {
                pw.Decorator.PlayShootSound();

                if (explode)
                    pw.ShowExplosionEffect(origin, Vector3.up, direction, projectileID);
                else
                    return pw.EmitProjectile(new Ray(origin, direction), projectileID, actorID);
            }
        }

        return null;
    }

    public int CurrentSlotIndex { get; private set; }

    public void UpdateWeaponSlot(int slotIndex, bool isLocal)
    {
        CurrentSlotIndex = slotIndex;

        switch (slotIndex)
        {
            case 0:
                _currentSlot = _weaponSlots[0];
                if (!isLocal)
                    _avatar.ShowWeapon(LoadoutSlotType.WeaponMelee);
                break;
            case 1:
                _currentSlot = _weaponSlots[1];
                if (!isLocal)
                    _avatar.ShowWeapon(LoadoutSlotType.WeaponPrimary);
                break;
            case 2:
                _currentSlot = _weaponSlots[2];
                if (!isLocal)
                    _avatar.ShowWeapon(LoadoutSlotType.WeaponSecondary);
                break;
            case 3:
                _currentSlot = _weaponSlots[3];
                if (!isLocal)
                    _avatar.ShowWeapon(LoadoutSlotType.WeaponTertiary);
                break;
            case 4:
                _currentSlot = _weaponSlots[4];
                if (!isLocal)
                    _avatar.ShowWeapon(LoadoutSlotType.WeaponPickup);
                break;
        }
    }

    /// <summary>
    /// Call this function to to equip weapons for remote player.
    /// </summary>
    public void UpdateWeapons(int currentWeaponSlot, IList<int> weaponItemIds, IList<int> quickItemIds)
    {
        if (_avatar != null)
        {
            WeaponItem[] weapons = new WeaponItem[] {
                ItemManager.Instance.GetWeaponItemInShop(weaponItemIds[0]),
                ItemManager.Instance.GetWeaponItemInShop(weaponItemIds[1]),
                ItemManager.Instance.GetWeaponItemInShop(weaponItemIds[2]),
                ItemManager.Instance.GetWeaponItemInShop(weaponItemIds[3]),
                ItemManager.Instance.GetWeaponItemInShop(weaponItemIds[4])
            };

            LoadoutSlotType[] weaponSlotTypes = new LoadoutSlotType[] {
                LoadoutSlotType.WeaponMelee,
                LoadoutSlotType.WeaponPrimary,
                LoadoutSlotType.WeaponSecondary,
                LoadoutSlotType.WeaponTertiary,
                LoadoutSlotType.WeaponPickup
            };

            int currentWeaponIndex = -1;
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                if (_weaponSlots[i] != null && _weaponSlots[i].Decorator != null)
                    GameObject.Destroy(_weaponSlots[i].Decorator.gameObject);

                if (weapons[i] != null && _avatar.WeaponAttachPoint)
                {
                    WeaponSlot slot = new WeaponSlot(weaponSlotTypes[i], weapons[i], _avatar.WeaponAttachPoint, this);

                    if (slot.Decorator)
                    {
                        if (currentWeaponIndex < 0)
                            currentWeaponIndex = i;

                        slot.Decorator.EnableShootAnimation = false;
                        slot.Decorator.DefaultPosition = Vector3.zero;

                        _avatar.AssignWeapon(weaponSlotTypes[i], slot.Decorator);
                    }
                    else
                    {
                        Debug.LogError("WeaponDecorator is NULL!");
                    }

                    _weaponSlots[i] = slot;
                }
                else
                {
                    _weaponSlots[i] = null;
                }
            }

            // set the current weapon visible
            if (CurrentSlotIndex >= 0 && _weaponSlots[CurrentSlotIndex] != null && _weaponSlots[CurrentSlotIndex].Decorator != null)
            {
                _weaponSlots[CurrentSlotIndex].Decorator.IsEnabled = true;
            }
        }
    }

    public void SetAvatarDecorator(AvatarDecorator decorator)
    {
        _avatar = decorator;
    }

    #region FIELDS

    private bool _isFullSimulation = true;

    private AvatarDecorator _avatar;
    private CharacterConfig _config;

    private float _nextShootTime;

    private WeaponSlot _currentSlot;

    private WeaponSlot[] _weaponSlots;
    //private QuickItemSlot[] _quickItemSlots;

    #endregion

    private int _projectileId = 0;

    public int NextProjectileId()
    {
        return ProjectileManager.CreateGlobalProjectileID(PlayerNumber, ++_projectileId);
    }

    public byte PlayerNumber
    {
        get { return _config.State.PlayerNumber; }
    }

    public Vector3 ShootingPoint
    {
        get { return _config.State.ShootingPoint; }
    }

    public Vector3 ShootingDirection
    {
        get { return _config.State.ShootingDirection; }
    }

    public bool IsLocal
    {
        get { return false; }
    }
}