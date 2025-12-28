using UberStrike.Realtime.Common;
using UnityEngine;

public class HealthPickupItem : PickupItem
{
    private SoundEffectType Get3DSoundEffectType()
    {
        switch (_healthPoints)
        {
            case Category.HP_100: return SoundEffectType.PropsMegaHealth;
            case Category.HP_50: return SoundEffectType.PropsBigHealth;
            case Category.HP_25: return SoundEffectType.PropsMediumHealth;
            case Category.HP_5: return SoundEffectType.PropsSmallHealth;
            default: return SoundEffectType.PropsSmallHealth;
        }
    }

    private SoundEffectType Get2DSoundEffectType()
    {
        switch (_healthPoints)
        {
            case Category.HP_100: return SoundEffectType.HUDMegaHealth;
            case Category.HP_50: return SoundEffectType.HUDBigHealth;
            case Category.HP_25: return SoundEffectType.HUDMediumHealth;
            case Category.HP_5: return SoundEffectType.HUDSmallHealth;
            default: return SoundEffectType.HUDSmallHealth;
        }
    }

    protected override bool OnPlayerPickup()
    {
        int healthPoints = 0;
        int maxPoints = 100;

        switch (_healthPoints)
        {
            case Category.HP_5: healthPoints = 5; maxPoints = 200; break;
            case Category.HP_25: healthPoints = 25; maxPoints = 100; break;
            case Category.HP_50: healthPoints = 50; maxPoints = 100; break;
            case Category.HP_100: healthPoints = 100; maxPoints = 200; break;
            default: healthPoints = 0; maxPoints = 100; break;
        }

        if (GameState.HasCurrentPlayer && GameState.HasCurrentGame)
        {

            if (GameState.LocalCharacter.Health < maxPoints)
            {
                //as we want instant feedback about out actions we assign the armor values to the player immediatly
                HpApHud.Instance.HP = Mathf.Clamp(GameState.LocalCharacter.Health + healthPoints, 0, maxPoints);

                //call actual game mode method
                GameState.CurrentGame.PickupPowerup(PickupID, PickupItemType.Health, healthPoints);

                //HUD feedback
                switch (_healthPoints)
                {
                    case Category.HP_5:
                        PickupNameHud.Instance.DisplayPickupName("Mini Health", PickUpMessageType.Health5);
                        break;
                    case Category.HP_25:
                        PickupNameHud.Instance.DisplayPickupName("Medium Health", PickUpMessageType.Health25);
                        break;
                    case Category.HP_50:
                        PickupNameHud.Instance.DisplayPickupName("Big Health", PickUpMessageType.Health50);
                        break;
                    case Category.HP_100:
                        PickupNameHud.Instance.DisplayPickupName("Uber Health", PickUpMessageType.Health100);
                        break;
                }

                PlayLocalPickupSound(Get2DSoundEffectType());

                if (GameState.IsSinglePlayer)
                    StartCoroutine(StartHidingPickupForSeconds(_respawnTime));

                return true;
            }
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
            return GameState.LocalCharacter.Health < 100;
        }
    }

    #region INSPECTOR

    [SerializeField]
    private Category _healthPoints;

    #endregion

    public enum Category
    {
        HP_100,
        HP_50,
        HP_25,
        HP_5,
    }
}