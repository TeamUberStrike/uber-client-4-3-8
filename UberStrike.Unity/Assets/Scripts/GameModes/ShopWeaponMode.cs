
using System.Collections;
using UberStrike.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;
using UberStrike.Realtime.Common;
using UnityEngine;
using Cmune.Realtime.Common.Synchronization;
using UberStrike.Core.Types;

[NetworkClass(-1)]
public class ShopWeaponMode : FpsGameMode
{
    public ShopWeaponMode(RemoteMethodInterface rmi)
        : base(rmi, new GameMetaData(0, string.Empty, 120, 0, 0))
    {
        _targetController = new ShopShootingTargetController();
    }

    protected override void OnUninitialized()
    {
        base.OnUninitialized();

        _targetController.Disable();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        MonoRoutine.Run(StartDecreasingHealthAndArmor);
        MonoRoutine.Run(SimulateGameFrameUpdate);

        ArmorHud.Instance.Enabled = false;
    }

    protected override void OnCharacterLoaded()
    {
        Vector3 pos;
        Quaternion rot;
        SpawnPointManager.Instance.GetSpawnPointAt(0, GameMode.TryWeapon, TeamID.NONE, out pos, out rot);

        SpawnPlayerAt(pos, rot);

        _targetController.Enable();

        if (LevelCamera.Exists)
            LevelCamera.Instance.MainCamera.rect = new Rect(0, 0, 1, 1);
    }

    protected override void OnModeInitialized()
    {
        OnPlayerJoined(SyncObjectBuilder.GetSyncData(GameState.LocalCharacter, true), Vector3.zero);
        IsMatchRunning = true;
        GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);
    }

    public override void PlayerHit(int targetPlayer, short damage, BodyPart part, Vector3 force, int shotCount, int weaponID, UberstrikeItemClass weaponClass, DamageEffectType damageEffectFlag, float damageEffectValue)
    {
        GameState.LocalPlayer.MoveController.ApplyForce(force, CharacterMoveController.ForceType.Additive);
    }

    protected override void ApplyCurrentGameFrameUpdates(SyncObject delta)
    {
        base.ApplyCurrentGameFrameUpdates(delta);

        if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Health) && !GameState.LocalCharacter.IsAlive)
        {
            OnSetNextSpawnPoint(Random.Range(0, SpawnPointManager.Instance.GetSpawnPointCount(GameMode.TryWeapon, TeamID.NONE)), 3);
        }
    }

    public override void RequestRespawn()
    {
        OnSetNextSpawnPoint(Random.Range(0, SpawnPointManager.Instance.GetSpawnPointCount(GameMode.TryWeapon, TeamID.NONE)), 3);
    }

    public override void IncreaseHealthAndArmor(int health, int armor)
    {
        UberStrike.Realtime.Common.CharacterInfo info = GameState.LocalCharacter;
        if (health > 0 && info.Health < 200)
        {
            info.Health = (short)Mathf.Clamp(info.Health + health, 0, 200);
        }

        if (armor > 0 && info.Armor.ArmorPoints < 200)
        {
            info.Armor.ArmorPoints = Mathf.Clamp(info.Armor.ArmorPoints + armor, 0, 200);
        }
    }

    public override void PickupPowerup(int pickupID, PickupItemType type, int value)
    {
        switch ((PickupItemType)type)
        {
            case PickupItemType.Armor:
                {
                    GameState.LocalCharacter.Armor.ArmorPoints += value;
                    break;
                }
            case PickupItemType.Health:
                {
                    GameState.LocalCharacter.Health += (short)value;
                    break;
                }
        }
    }

    private IEnumerator StartDecreasingHealthAndArmor()
    {
        while (IsInitialized)
        {
            yield return new WaitForSeconds(1);
            if (GameState.LocalCharacter.Health > 100) GameState.LocalCharacter.Health--;
            if (GameState.LocalCharacter.Armor.ArmorPoints > GameState.LocalCharacter.Armor.ArmorPointCapacity) GameState.LocalCharacter.Armor.ArmorPoints--;
        }
    }

    private IEnumerator SimulateGameFrameUpdate()
    {
        while (IsInitialized)
        {
            yield return new WaitForSeconds(0.1f);
            if (GameState.LocalPlayer.Character != null)
            {
                SyncObject delta = SyncObjectBuilder.GetSyncData(GameState.LocalCharacter, false);
                if (!delta.IsEmpty)
                {
                    ApplyCurrentGameFrameUpdates(delta);
                    GameState.LocalPlayer.Character.OnCharacterStateUpdated(delta);
                }
            }
        }
    }

    #region Properties

    public ShopShootingTargetController TargetController
    {
        get { return _targetController; }
    }

    #endregion

    #region Fields

    private ShopShootingTargetController _targetController;

    #endregion
}
