
using System.Collections;
using UberStrike.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Realtime.Common.Utils;
using Cmune.Realtime.Photon.Client;
using UberStrike.Realtime.Common;
using UnityEngine;
using Cmune.Realtime.Common.Synchronization;
using UberStrike.Core.Types;

[NetworkClass(-1)]
public class TrainingFpsMode : FpsGameMode
{
    public TrainingFpsMode(RemoteMethodInterface rmi)
        : base(rmi, new GameMetaData(0, string.Empty, 120, 0, (short)GameMode.Training))
    {
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        MonoRoutine.Run(StartDecreasingHealthAndArmor);
        MonoRoutine.Run(SimulateGameFrameUpdate);
    }

    protected override void OnCharacterLoaded()
    {
        OnSetNextSpawnPoint(Random.Range(0, SpawnPointManager.Instance.GetSpawnPointCount(GameMode.Training, TeamID.NONE)), 0);
    }

    protected override void OnModeInitialized()
    {
        OnPlayerJoined(SyncObjectBuilder.GetSyncData(GameState.LocalCharacter, true), Vector3.zero);
        IsMatchRunning = true;
    }

    public override void PlayerHit(int targetPlayer, short damage, BodyPart part, Vector3 force, int shotCount, int weaponID, UberstrikeItemClass weaponClass, DamageEffectType damageEffectFlag, float damageEffectValue)
    {
        if (MyCharacterState.Info.IsAlive)
        {
            byte angle = Conversion.Angle2Byte(Vector3.Angle(Vector3.forward, force));

            MyCharacterState.Info.Health -= MyCharacterState.Info.Armor.AbsorbDamage(damage, part);

            DamageFeedbackHud.Instance.AddDamageMark(Mathf.Clamp01(damage / 50f), Conversion.Byte2Angle(angle));
            HpApHud.Instance.HP = GameState.LocalCharacter.Health;
            HpApHud.Instance.AP = GameState.LocalCharacter.Armor.ArmorPoints;

            GameState.LocalPlayer.MoveController.ApplyForce(force, CharacterMoveController.ForceType.Additive);
        }
    }

    protected override void ApplyCurrentGameFrameUpdates(SyncObject delta)
    {
        base.ApplyCurrentGameFrameUpdates(delta);

        if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Health) && !GameState.LocalCharacter.IsAlive)
        {
            OnSetNextSpawnPoint(Random.Range(0, SpawnPointManager.Instance.GetSpawnPointCount(GameMode.Training, TeamID.NONE)), 3);
        }
    }

    public override void RequestRespawn()
    {
        OnSetNextSpawnPoint(Random.Range(0, SpawnPointManager.Instance.GetSpawnPointCount(GameMode.Training, TeamID.NONE)), 3);
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
                    switch (value)
                    {
                        case 5:
                        case 100:
                            {
                                if (GameState.LocalCharacter.Health < 200)
                                {
                                    GameState.LocalCharacter.Health = (short)Mathf.Clamp(GameState.LocalCharacter.Health + value, 0, 200);
                                }
                            } break;
                        case 25:
                        case 50:
                            {
                                if (GameState.LocalCharacter.Health < 100)
                                {
                                    GameState.LocalCharacter.Health = (short)Mathf.Clamp(GameState.LocalCharacter.Health + value, 0, 100);
                                }
                            } break;
                    }
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

    public override float GameTime
    {
        get { return Time.realtimeSinceStartup; }
    }
}