using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class CustomWeaponPickupItem : PickupItem
{
    void Start()
    {
        WeaponItem item = ItemManager.Instance.GetWeaponItemInShop(_weaponId);
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
            //CmuneDebug.LogWarning("WeaponPickupItem failed at getting Weapon with ID '{0}'", _weaponId);
        }
    }

    protected override bool OnPlayerPickup()
    {
        if (ApplicationDataManager.ApplicationOptions.GameplayAutoPickupEnabled)
        {
            WeaponController.Instance.SetPickupWeapon(_weaponId);

            if (GameState.HasCurrentGame)
            {
                GameState.CurrentGame.PickupPowerup(PickupID, PickupItemType.Weapon, 0);
                PickupNameHud.Instance.DisplayPickupName(_weaponName, PickUpMessageType.ChangeWeapon);
                WeaponController.Instance.ResetPickupWeaponSlotInSeconds(0);

                PlayLocalPickupSound(SoundEffectType.HUDWeaponPickup);

                if (GameState.IsSinglePlayer)
                    StartCoroutine(StartHidingPickupForSeconds(_respawnTime));
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

    private void Update()
    {
        if (_pickupItem)
        {
            _pickupItem.Rotate(Vector3.up, 150 * Time.deltaTime, Space.Self);
        }
    }

    private string _weaponName = string.Empty;

    [SerializeField]
    int _weaponId;
}