using Cmune.Util;
using UnityEngine;

class QuickItemEventListener : Singleton<QuickItemEventListener>
{
    private QuickItemEventListener()
    {
        CmuneEventHandler.AddListener<QuickItemAmountChangedEvent>(ev => QuickItemController.Instance.UpdateQuickSlotAmount());
        CmuneEventHandler.AddListener<HealthIncreaseEvent>(OnHealthIncrease);
        CmuneEventHandler.AddListener<ArmorIncreaseEvent>(OnArmorIncrease);
        CmuneEventHandler.AddListener<AddAmmoIncreaseEvent>(OnAmmoIncrease);
        CmuneEventHandler.AddListener<AmmoAddStartEvent>(OnAddStartAmmo);
        CmuneEventHandler.AddListener<AmmoAddMaxEvent>(OnAddMaxAmmo);
    }

    public void Initialize() { }

    private void OnHealthIncrease(HealthIncreaseEvent ev)
    {
        if (GameState.LocalCharacter != null && GameState.CurrentGame != null)
        {
            //call actual game mode method
            GameState.CurrentGame.IncreaseHealthAndArmor(ev.Health, 0);

            //as we want instant feedback about out actions we assign the health values to the player immediatly
            HpApHud.Instance.HP = Mathf.Clamp(GameState.LocalCharacter.Health + ev.Health, 0, 200);
        }
    }

    private void OnArmorIncrease(ArmorIncreaseEvent ev)
    {
        //call actual game mode method
        GameState.CurrentGame.IncreaseHealthAndArmor(0, ev.Armor);

        //as we want instant feedback about out actions we assign the armor values to the player immediatly
        HpApHud.Instance.AP = Mathf.Clamp(GameState.LocalCharacter.Armor.ArmorPoints + ev.Armor, 0, 200);
    }

    private void OnAmmoIncrease(AddAmmoIncreaseEvent ev)
    {
        foreach (AmmoType ammo in System.Enum.GetValues(typeof(AmmoType)))
        {
            AmmoDepot.AddAmmoOfType(ammo, ev.Amount);
        }

        WeaponController.Instance.UpdateAmmoHUD();
    }

    private void OnAddMaxAmmo(AmmoAddMaxEvent ev)
    {
        foreach (AmmoType ammo in System.Enum.GetValues(typeof(AmmoType)))
        {
            AmmoDepot.AddMaxAmmoOfType(ammo, ev.Percent / 100f);
        }

        WeaponController.Instance.UpdateAmmoHUD();
    }

    private void OnAddStartAmmo(AmmoAddStartEvent ev)
    {
        foreach (AmmoType ammo in System.Enum.GetValues(typeof(AmmoType)))
        {
            AmmoDepot.AddStartAmmoOfType(ammo, ev.Percent / 100f);
        }

        WeaponController.Instance.UpdateAmmoHUD();
    }
}