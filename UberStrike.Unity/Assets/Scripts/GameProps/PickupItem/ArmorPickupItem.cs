using UnityEngine;
using UberStrike.Realtime.Common;

public class ArmorPickupItem : PickupItem
{
    private SoundEffectType Get2DSoundEffectType()
    {
        switch (_armorPoints)
        {
            case Category.Gold: return SoundEffectType.HUDGoldArmor;
            case Category.Silver: return SoundEffectType.HUDSilverArmor;
            case Category.Bronze: return SoundEffectType.HUDArmorShard;
            default: return SoundEffectType.HUDArmorShard;
        }
    }

    private SoundEffectType Get3DSoundEffectType()
    {
        switch (_armorPoints)
        {
            case Category.Gold: return SoundEffectType.PropsGoldArmor;
            case Category.Silver: return SoundEffectType.PropsSilverArmor;
            case Category.Bronze: return SoundEffectType.PropsArmorShard;
            default: return SoundEffectType.PropsArmorShard;
        }
    }

    protected override bool OnPlayerPickup()
    {
        if (CanPlayerPickup)
        {
            int points = 0;

            switch (_armorPoints)
            {
                case Category.Gold:
                    points = 100;
                    PickupNameHud.Instance.DisplayPickupName("Uber Armor", PickUpMessageType.Armor100);
                    break;
                case Category.Silver:
                    points = 50;
                    PickupNameHud.Instance.DisplayPickupName("Big Armor", PickUpMessageType.Armor50);
                    break;
                case Category.Bronze:
                    points = 5;
                    PickupNameHud.Instance.DisplayPickupName("Mini Armor", PickUpMessageType.Armor5);
                    break;
            }

            //as we want instant feedback about out actions we assign the armor values to the player immediatly
            HpApHud.Instance.AP = Mathf.Clamp(GameState.LocalCharacter.Armor.ArmorPoints + points, 0, 200);

            PlayLocalPickupSound(Get2DSoundEffectType());

            if (GameState.HasCurrentGame)
            {
                GameState.CurrentGame.PickupPowerup(PickupID, PickupItemType.Armor, points);

                if (GameState.IsSinglePlayer)
                    StartCoroutine(StartHidingPickupForSeconds(_respawnTime));
            }

            return true;
        }

        return false;
    }

    protected override void OnRemotePickup()
    {
        PlayRemotePickupSound(Get3DSoundEffectType(), this.transform.position);
    }

    protected override bool CanPlayerPickup
    {
        get
        {
            return GameState.HasCurrentPlayer && GameState.LocalCharacter.Armor.ArmorPoints < 200;
        }
    }

    #region INSPECTOR

    [SerializeField]
    private Category _armorPoints;

    #endregion

    public enum Category
    {
        Gold,
        Silver,
        Bronze,
    }
}