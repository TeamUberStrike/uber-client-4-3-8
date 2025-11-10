using System.Collections;
using UberStrike.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;
using UberStrike.Core.Types;

public class PlayerDropPickupItem : PickupItem
{
    public int WeaponItemId = 0;

    //private string _weaponName = string.Empty;
    private UberstrikeItemClass _weaponType = UberstrikeItemClass.WeaponMelee;

    private float _timeout;

    private IEnumerator Start()
    {
        if (WeaponItemId != 0)
        {
            WeaponItem item = ItemManager.Instance.GetWeaponItemInShop(WeaponItemId);
            if (item != null)
            {
                IsAvailable = false;
                _weaponType = item.ItemClass;
                // set tag
                gameObject.tag = "DynamicProp";

                BaseWeaponDecorator decorator = ItemManager.Instance.Instantiate(item.ItemId).GetComponent<BaseWeaponDecorator>();
                decorator.transform.parent = _pickupItem;
                decorator.transform.localPosition = new Vector3(0, 0, -0.3f);
                decorator.transform.localRotation = Quaternion.identity;
                decorator.transform.localScale = Vector3.one;

                LayerUtil.SetLayerRecursively(decorator.transform, UberstrikeLayer.Props);

                //don's show the weapon immedeatly
                _renderers = decorator.GetComponentsInChildren<MeshRenderer>(true);
                foreach (Renderer r in _renderers)
                    r.enabled = false;

                yield return new WaitForSeconds(0.5f);

                SetItemAvailable(true);

                BaseWeaponEffect[] effects = decorator.GetComponentsInChildren<BaseWeaponEffect>();
                foreach (BaseWeaponEffect ef in effects)
                {
                    ef.Hide();
                }
            }
            else
            {
                //CmuneDebug.LogWarning("WeaponPickupItem failed at getting Weapon with ID '{0}'", WeaponItemId);
            }
        }
        else
        {
            Debug.LogError("No Pickup Weapon assigned: " + WeaponItemId);
        }

        //find the clostest position on the ground
        Vector3 oldpos = transform.position;
        Vector3 newpos = oldpos;
        RaycastHit hit;

        if (UnityEngine.Physics.Raycast(oldpos + Vector3.up, Vector3.down, out hit, 100, UberstrikeLayerMasks.ProtectionMask))
        {
            if (oldpos.y > (hit.point.y + 1))
                newpos = hit.point + Vector3.up;
        }

        //slowly move down and wait until countdown is finished
        _timeout = Time.time + 10;
        float time = 0;
        while (_timeout > Time.time)
        {
            yield return new WaitForEndOfFrame();
            time += Time.deltaTime;

            transform.position = Vector3.Lerp(oldpos, newpos, time);
        }

        IsAvailable = false;

        yield return new WaitForSeconds(0.2f);

        Destroy(gameObject);
    }

    private void Update()
    {
        if (_pickupItem)
        {
            _pickupItem.Rotate(Vector3.up, 150 * Time.deltaTime, Space.Self);
        }
    }

    protected override bool OnPlayerPickup()
    {
        bool weaponCheck = false;

        if (ApplicationDataManager.ApplicationOptions.GameplayAutoPickupEnabled)
        {
            weaponCheck = WeaponController.Instance.HasWeaponOfClass(_weaponType);
            WeaponController.Instance.SetPickupWeapon(WeaponItemId, false, false);

            if (WeaponController.Instance.CheckPlayerWeaponInPickupSlot(WeaponItemId))
            {
                WeaponController.Instance.ResetPickupWeaponSlotInSeconds(10);
            }

            if (GameState.HasCurrentGame)
            {
                GameState.CurrentGame.PickupPowerup(PickupID, PickupItemType.Weapon, 0);

                // showing ammo we picked up, weapon name will be added automatically in hud manager
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

                    PlayLocalPickupSound(SoundEffectType.HUDWeaponPickup);

                    StartCoroutine(StartHidingPickupForSeconds(0));
                }
            }

            return true;
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
}