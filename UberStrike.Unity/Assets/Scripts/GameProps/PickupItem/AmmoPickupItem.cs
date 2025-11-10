using UberStrike.Realtime.Common;
using UnityEngine;

public class AmmoPickupItem : PickupItem
{
    #region Fields

    [SerializeField]
    private AmmoType _ammoType;

    #endregion

    protected override bool OnPlayerPickup()
    {
        bool needToPickUp = false;
        needToPickUp = AmmoDepot.CanAddAmmo(_ammoType);
        if (needToPickUp)
        {
            AmmoDepot.AddDefaultAmmoOfType(_ammoType);
            WeaponController.Instance.UpdateAmmoHUD();

            switch (_ammoType)
            {
                case AmmoType.Cannon:
                    PickupNameHud.Instance.DisplayPickupName("Cannon Rockets", PickUpMessageType.AmmoCannon);
                    break;
                case AmmoType.Handgun:
                    PickupNameHud.Instance.DisplayPickupName("Handgun Rounds", PickUpMessageType.AmmoHandgun);
                    break;
                case AmmoType.Launcher:
                    PickupNameHud.Instance.DisplayPickupName("Launcher Grenades", PickUpMessageType.AmmoLauncher);
                    break;
                case AmmoType.Machinegun:
                    PickupNameHud.Instance.DisplayPickupName("Machinegun Ammo", PickUpMessageType.AmmoMachinegun);
                    break;
                case AmmoType.Shotgun:
                    PickupNameHud.Instance.DisplayPickupName("Shotgun Shells", PickUpMessageType.AmmoShotgun);
                    break;
                case AmmoType.Snipergun:
                    PickupNameHud.Instance.DisplayPickupName("Sniper Bullets", PickUpMessageType.AmmoSnipergun);
                    break;
                case AmmoType.Splattergun:
                    PickupNameHud.Instance.DisplayPickupName("Splattergun Cells", PickUpMessageType.AmmoSplattergun);
                    break;
            }

            PlayLocalPickupSound(SoundEffectType.HUDAmmoPickup);

            if (GameState.HasCurrentGame)
            {
                GameState.CurrentGame.PickupPowerup(PickupID, PickupItemType.Ammo, 0);

                if (GameState.IsSinglePlayer)
                    StartCoroutine(StartHidingPickupForSeconds(_respawnTime));
            }
        }

        return needToPickUp;
    }

    protected override void OnRemotePickup()
    {
        PlayRemotePickupSound(SoundEffectType.PropsAmmoPickup, this.transform.position);
    }

    protected override bool CanPlayerPickup
    {
        get
        {
            return AmmoDepot.CanAddAmmo(_ammoType);
        }
    }
}