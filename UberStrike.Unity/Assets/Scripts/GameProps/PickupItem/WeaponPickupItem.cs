using UberStrike.DataCenter.Common.Entities;
using UberStrike.Realtime.Common;

using UnityEngine;
using Cmune.Util;
using UberStrike.Core.Types;

public class WeaponPickupItem : PickupItem
{
    void Start()
    {
        int weaponItemId;
        if (ItemManager.Instance.TryGetDefaultItemId(_weaponType, out weaponItemId))
        {
            WeaponItem item = ItemManager.Instance.GetWeaponItemInShop(weaponItemId);
            if (item != null)
            {
                _weaponName = item.Name;
                BaseWeaponDecorator decorator = ItemManager.Instance.Instantiate(item.ItemId).GetComponent<BaseWeaponDecorator>();
                decorator.transform.parent = _pickupItem;
                decorator.transform.localPosition = new Vector3(0, 0, -0.3f);
                decorator.transform.localRotation = Quaternion.identity;
                decorator.transform.localScale = Vector3.one;

                LayerUtil.SetLayerRecursively(decorator.transform, UberstrikeLayer.Default);

                _renderers = decorator.GetComponentsInChildren<MeshRenderer>(true);
            }
            else
            {
                //CmuneDebug.LogWarning("WeaponPickupItem failed at getting Weapon with ID '{0}'", weaponItemId);
            }
        }
        else
        {
            Debug.LogError("No Default Weapon found for Class " + _weaponType);
        }
    }

    protected override bool OnPlayerPickup()
    {
        if (ApplicationDataManager.ApplicationOptions.GameplayAutoPickupEnabled)
        {
            int weaponItemId;
            bool weaponCheck = false;

            if (ItemManager.Instance.TryGetDefaultItemId(_weaponType, out weaponItemId))
            {
                weaponCheck = WeaponController.Instance.HasWeaponOfClass(_weaponType);
                WeaponController.Instance.SetPickupWeapon(weaponItemId);

                if (GameState.HasCurrentGame)
                {
                    GameState.CurrentGame.PickupPowerup(PickupID, PickupItemType.Weapon, 0);
                    if (weaponCheck)
                    {
                        switch (_weaponType)
                        {
                            case UberstrikeItemClass.WeaponCannon:
                                PickupNameHud.Instance.DisplayPickupName("Cannon Rockets", PickUpMessageType.AmmoCannon);
                                break;
                            case UberstrikeItemClass.WeaponHandgun:
                                PickupNameHud.Instance.DisplayPickupName("Handgun Rounds", PickUpMessageType.AmmoHandgun);
                                break;
                            case UberstrikeItemClass.WeaponLauncher:
                                PickupNameHud.Instance.DisplayPickupName("Launcher Grenades", PickUpMessageType.AmmoLauncher);
                                break;
                            case UberstrikeItemClass.WeaponMachinegun:
                                PickupNameHud.Instance.DisplayPickupName("Machinegun Ammo", PickUpMessageType.AmmoMachinegun);
                                break;
                            case UberstrikeItemClass.WeaponShotgun:
                                PickupNameHud.Instance.DisplayPickupName("Shotgun Shells", PickUpMessageType.AmmoShotgun);
                                break;
                            case UberstrikeItemClass.WeaponSniperRifle:
                                PickupNameHud.Instance.DisplayPickupName("Sniper Bullets", PickUpMessageType.AmmoSnipergun);
                                break;
                            case UberstrikeItemClass.WeaponSplattergun:
                                PickupNameHud.Instance.DisplayPickupName("Splattergun Cells", PickUpMessageType.AmmoSplattergun);
                                break;
                        }
                    }
                    else
                    {
                        PickupNameHud.Instance.DisplayPickupName(_weaponName, PickUpMessageType.ChangeWeapon);
                        WeaponController.Instance.ResetPickupWeaponSlotInSeconds(0);
                    }

                    PlayLocalPickupSound(SoundEffectType.HUDWeaponPickup);

                    if (GameState.IsSinglePlayer)
                        StartCoroutine(StartHidingPickupForSeconds(_respawnTime));
                }

                return true;
            }
            else
            {
                //Debug.LogError("Cannot get default item of type: " + _weaponType.ToString());
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    protected override void OnRemotePickup()
    {
        PlayRemotePickupSound(SoundEffectType.PropsWeaponPickup, this.transform.position);
    }

    private void Update()
    {
        if (_pickupItem)
        {
            _pickupItem.Rotate(Vector3.up, 150 * Time.deltaTime, Space.Self);
        }
    }

    private string _weaponName = string.Empty;

    [SerializeField]
    UberstrikeItemClass _weaponType;
}