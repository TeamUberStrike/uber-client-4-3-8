using UnityEngine;

public class WeaponsHud : Singleton<WeaponsHud>
{
    #region Properties

    public QuickItemGroupHud QuickItems { get; private set; }

    public WeaponSelectorHud Weapons { get; private set; }

    #endregion

    private WeaponsHud()
    {
        Weapons = new WeaponSelectorHud();
        QuickItems = new QuickItemGroupHud();
        SetEnabled(false);
    }

    public void SetEnabled(bool enabled)
    {
        Weapons.Enabled = enabled;
        QuickItems.Enabled = enabled;
    }

    public void Draw()
    {
        Weapons.Draw();
        QuickItems.Draw();
    }

    public void Update()
    {
        Weapons.Update();
    }

    public void SetActiveLoadout(LoadoutSlotType loadoutSlotType)
    {
        switch (loadoutSlotType)
        {
            case LoadoutSlotType.WeaponMelee:
            case LoadoutSlotType.WeaponPrimary:
            case LoadoutSlotType.WeaponSecondary:
            case LoadoutSlotType.WeaponTertiary:
            case LoadoutSlotType.WeaponPickup:
                SetActiveWeaponLoadout(loadoutSlotType);
                break;

            default:
                Debug.LogError("You passed in an invalid LoadoutSlotType!");
                break;
        }
    }

    public void ResetActiveWeapon()
    {
        SetActiveLoadout(LoadoutSlotType.WeaponPrimary);
    }

    public void SetQuickItemCurrentAmount(int slot, int amount)
    {
        QuickItemHud quickItem = QuickItems.GetLoadoutQuickItemHud(slot);
        if (quickItem != null)
        {
            quickItem.Amount = amount;
        }
    }

    public void SetQuickItemCooldown(int slot, float cooldown)
    {
        QuickItemHud quickItem = QuickItems.GetLoadoutQuickItemHud(slot);
        if (quickItem != null)
        {
            quickItem.Cooldown = cooldown;
        }
    }

    public void SetQuickItemCooldownMax(int slot, float cooldownMax)
    {
        QuickItemHud quickItem = QuickItems.GetLoadoutQuickItemHud(slot);
        if (quickItem != null)
        {
            quickItem.CooldownMax = cooldownMax;
        }
    }

    public void SetQuickItemRecharging(int slot, float recharging)
    {
        QuickItemHud quickItem = QuickItems.GetLoadoutQuickItemHud(slot);
        if (quickItem != null)
        {
            quickItem.Recharging = recharging;
        }
    }

    public void SetQuickItemRechargingMax(int slot, float rechargingMax)
    {
        QuickItemHud quickItem = QuickItems.GetLoadoutQuickItemHud(slot);
        if (quickItem != null)
        {
            quickItem.RechargingMax = rechargingMax;
        }
    }

    #region Private

    private void SetActiveWeaponLoadout(LoadoutSlotType loadoutSlotType)
    {
        if (HudAssets.Exists)
        {
            WeaponItem activeWeapon = Weapons.GetLoadoutWeapon(loadoutSlotType);
            if (activeWeapon != null)
            {
                Weapons.SetActiveWeaponLoadout(loadoutSlotType);

                ReticleHud.Instance.ConfigureReticle(activeWeapon.Configuration);
            }
        }
    }

    #endregion
}