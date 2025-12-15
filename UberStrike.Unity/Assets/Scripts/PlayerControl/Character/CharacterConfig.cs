using Cmune.Realtime.Common;
using UberStrike.Realtime.Common;
using UnityEngine;
using Cmune.Util;
using Cmune.Realtime.Common.Synchronization;

/// <summary>
/// This class contains all the configuration neccesary to represent the avatar locally and remotly the same way.
/// It should not contain interaction methods but merely act as a container of values to represent the player
/// visually and auditive.
/// </summary>
public class CharacterConfig : MonoBehaviour, IShootable
{
    private void Awake()
    {
        IsAnimationEnabled = true;
        MoveSimulator = new CharacterMoveSimulator(transform);
        WeaponSimulator = new WeaponSimulator(this);
        StateController = new CharacterStateAnimationController();
    }

    private void LateUpdate()
    {
        if (_state != null)
        {
            if (Decorator.AnimationController != null)
            {
                //if (!_state.Info.Is(PlayerStates.IDLE))
                //    _state.Info.Set(PlayerStates.IDLE);

                StateController.Update(_state.Info, Decorator.AnimationController);
            }

            WeaponSimulator.Update(_state.Info, IsLocal);

            MoveSimulator.Update(_state.Info);
        }

        if (_sound != null)
            _sound.Update();

        if (_graceTimeAfterSpawn > 0)
        {
            _graceTimeAfterSpawn -= Time.deltaTime;
        }

        if (!_graceTimeOut && _graceTimeAfterSpawn <= 0)
        {
            _graceTimeOut = true;
        }
    }

    public void Initialize(ICharacterState state, AvatarDecorator decorator)
    {
        _state = state;

        _sound = new PlayerSound(state.Info);
        _sound.SetCharacter(this);

        //make sure to unsubscribe when destroying this object
        _state.SubscribeToEvents(this);

        //used for initial position sync of dead players on join
        transform.position = _state.LastPosition;

        SetAvatarDecorator(decorator);

        OnCharacterStateUpdated(SyncObjectBuilder.GetSyncData(state.Info, true));
    }

    /// <summary>
    /// Linkage between data changes and scene updates
    /// </summary>
    /// <param name="delta"></param>
    public void OnCharacterStateUpdated(SyncObject delta)
    {
        try
        {
            if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Weapons))
            {
                WeaponSimulator.UpdateWeapons(_state.Info.CurrentWeaponSlot, _state.Info.Weapons.ItemIDs, _state.Info.QuickItems);
                WeaponSimulator.UpdateWeaponSlot(_state.Info.CurrentWeaponSlot, _isLocalPlayer);
            }
            else if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.CurrentWeaponSlot))
            {
                WeaponSimulator.UpdateWeaponSlot(_state.Info.CurrentWeaponSlot, _isLocalPlayer);
            }

            if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Gear) && !IsLocal)
            {
                AvatarBuilder.Instance.UpdateRemoteAvatar(
                    Decorator,
                    _state.Info.Gear.ToArray(),
                    _state.Info.SkinColor);
            }

            //MOVEMENT
            if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.MoveState))
            {
                if (_state.Info.Is(PlayerStates.GROUNDED) && TimeLastGrounded + 0.5f < Time.time)
                {
                    if (!_state.Info.Is(PlayerStates.DIVING))
                    {
                        TimeLastGrounded = Time.time;

                        if (Decorator)
                            Decorator.PlayFootSound(WalkingSoundSpeed);
                    }
                }
            }

            //HEALTH
            if (delta.Contains(UberStrike.Realtime.Common.CharacterInfo.FieldTag.Health))
            {
                int newHealth = (short)delta.Data[UberStrike.Realtime.Common.CharacterInfo.FieldTag.Health];

                // prepare for respawn
                if (IsDead && newHealth > 0)
                {
                    //clean up dead body
                    Decorator.DisableRagdoll();

                    IsDead = false;

                    _graceTimeOut = false;
                    _graceTimeAfterSpawn = 2;
                }
                //HERE THE PLAYER DIES VISUALLY
                else if (!IsDead && newHealth <= 0)
                {
                    IsDead = true;

                    //WeaponSimulator.ClearAllWeapons();
                    ProjectileManager.Instance.RemoveAllProjectilesFromPlayer(State.PlayerNumber);
                    QuickItemSfxController.Instance.DestroytSfxFromPlayer(State.PlayerNumber);
                    Decorator.HudInformation.Hide();

                    var decorator = Decorator.SpawnDeadRagdoll(_lastShotInfo);
                    if (IsLocal)
                    {
                        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.Death, decorator);
                    }

                    //only drop points and weapons if I'm in the game already
                    if (GameState.CurrentGame.IsLocalAvatarLoaded)
                    {
                        Vector3 position = Decorator.transform.position + Vector3.up;

                        //Drop the latest weapon of the player on DIE
                        if (_playerDropWeapon != null && !IsLocal && _state.Info.CurrentWeaponSlot != 4)
                        {
                            PlayerDropPickupItem drop = GameObject.Instantiate(_playerDropWeapon, position, Quaternion.identity) as PlayerDropPickupItem;
                            if (drop)
                            {
                                drop.PickupID = _state.Info.PlayerNumber;
                                drop.WeaponItemId = _state.Info.CurrentWeaponID;
                            }
                        }

                        Decorator.PlayDieSound();
                    }
                    if (!_isLocalPlayer)
                        Decorator.HideWeapons();
                }

                Decorator.HudInformation.SetHealthBarValue(_state.Info.Health / 100f);
            }
        }
        catch (System.Exception e)
        {
            e.Data.Add("OnCharacterStateUpdated", delta);
            throw;// CmuneDebug.Exception(e.InnerException, "OnCharacterStateUpdated failed delta={0}", delta);
        }
    }

    public bool IsDead { get; private set; }

    public void ApplyDamage(DamageInfo d)
    {
        _lastShotInfo = d;

        // there is a chance the _state is not set
        if (_state != null && GameState.HasCurrentGame)
        {
            if (_state.Info.Health > 0)
            {
                GameState.CurrentGame.PlayerHit(_state.Info.ActorId, d.Damage, d.BodyPart, d.Force, d.ShotID, d.WeaponID, d.WeaponClass, d.DamageEffectFlag, d.DamageEffectValue);
            }

            if (!IsLocal && GameState.LocalCharacter != null)
            {
                if (_state.Info.TeamID == TeamID.NONE || _state.Info.TeamID != GameState.LocalCharacter.TeamID)
                {
                    ReticleHud.Instance.EnableEnemyReticle();
                }

                if (_state.Info.TeamID == TeamID.NONE || _state.Info.TeamID != GameState.LocalCharacter.TeamID)
                {
                    ShowDamageFeedback(d);
                }
            }

            PlayDamageSound();
        }
    }

    public virtual void ApplyForce(Vector3 position, Vector3 force)
    {
        if (IsLocal)
        {
            GameState.LocalPlayer.MoveController.ApplyForce(force, CharacterMoveController.ForceType.Additive);
        }
        else
        {
            GameState.CurrentGame.SendPlayerHitFeedback(ActorID, force);
        }
    }

    public float WalkingSoundSpeed
    {
        get { return _walkingSoundSpeed; }
    }

    private void SetAvatarDecorator(AvatarDecorator decorator)
    {
        Decorator = decorator;

        decorator.GetComponent<Renderer>().receiveShadows = false;
        decorator.GetComponent<Renderer>().castShadows = true;

        decorator.transform.parent = transform;
        decorator.SetPosition(new Vector3(0, -0.98f, 0), Quaternion.identity);
        decorator.HudInformation.SetCharacterInfo(_state.Info);
        decorator.SetFootStep(GameState.HasCurrentSpace ? GameState.CurrentSpace.DefaultFootStep : FootStepSoundType.Rock);
        decorator.SetSkinColor(_state.Info.SkinColor);
        decorator.SetLayers(IsLocal ? UberstrikeLayer.LocalPlayer : UberstrikeLayer.RemotePlayer);

        WeaponSimulator.SetAvatarDecorator(decorator);
        WeaponSimulator.UpdateWeapons(_state.Info.CurrentWeaponSlot, _state.Info.Weapons.ItemIDs, _state.Info.QuickItems);
        WeaponSimulator.UpdateWeaponSlot(_state.Info.CurrentWeaponSlot, _isLocalPlayer);

        gameObject.name = string.Format("Player{0}_{1}", _state.Info.ActorId, _state.Info.PlayerName);

        foreach (CharacterHitArea hitBox in decorator.HitAreas)
        {
            hitBox.Shootable = this;
        }

        //Rigidbody rb = LevelCamera.Instance.GetComponent<Rigidbody>();
        //if (rb) decorator.IgnoreCollision(rb.collider);

        // outline teammates
        if (GameState.HasCurrentGame)
        {
            GameState.CurrentGame.ChangePlayerOutline(this);
        }
    }

    public void AddFollowCamera()
    {
        MoveSimulator.AddPositionObserver(LevelCamera.Instance);
    }

    public void RemoveFollowCamera()
    {
        MoveSimulator.RemovePositionObserver();
    }

    internal void Destroy()
    {
        //WeaponSimulator.ClearAllWeapons();
        ProjectileManager.Instance.RemoveAllProjectilesFromPlayer(State.PlayerNumber);
        QuickItemSfxController.Instance.DestroytSfxFromPlayer(State.PlayerNumber);

        _state.UnSubscribeAll();

        if (Decorator)
        {
            Decorator.DestroyCurrentRagdoll();

            //make sure that the character will be never destroyed in the process of deactivating/cleaning up a finished game
            //maybe we should change this dependency of re-using the local character in game too (created already a lot of special cases)
            if (IsLocal)
                Decorator.transform.parent = null;
        }

        AvatarBuilder.Destroy(gameObject);
    }

    private void PlayDamageSound()
    {
        if (IsLocal)
        {
            if (_state.Info.Armor.HasArmor && _state.Info.Armor.HasArmorPoints)
            {
                SfxManager.Play2dAudioClip(SoundEffectType.PcLocalPlayerHitArmorRemaining);
            }
            else
            {
                if (_state.Info.Health < 25)
                    SfxManager.Play2dAudioClip(SoundEffectType.PcLocalPlayerHitNoArmorLowHealth);
                else
                    SfxManager.Play2dAudioClip(SoundEffectType.PcLocalPlayerHitNoArmor);
            }
        }
    }

    private void ShowDamageFeedback(DamageInfo shot)
    {
        PlayerDamageEffect effect = GameObject.Instantiate(_damageFeedback, shot.Hitpoint, Quaternion.LookRotation(shot.Force)) as PlayerDamageEffect;
        if (effect)
        {
            effect.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            effect.Show(shot);
        }
    }

    public bool IsLocal
    {
        get { return _isLocalPlayer; }
    }

    #region Fields

    private ICharacterState _state;
    private PlayerSound _sound;
    private DamageInfo _lastShotInfo;

    private float _graceTimeAfterSpawn = 0;
    private bool _graceTimeOut = true;
    #endregion

    #region Inspector
    [SerializeField]
    private bool _isLocalPlayer = false;
    [SerializeField]
    private PlayerDamageEffect _damageFeedback;
    [SerializeField]
    private CharacterTrigger _aimTrigger;
    [SerializeField]
    private float _walkingSoundSpeed = 0.38f;
    [SerializeField]
    private PlayerDropPickupItem _playerDropWeapon;
    #endregion

    #region Properties

    public bool IsVulnerable
    {
        get { return _graceTimeAfterSpawn <= 0; }
    }

    public bool IsAnimationEnabled
    {
        get;
        set;
    }

    public float TimeLastGrounded { get; private set; }

    public AvatarDecorator Decorator { get; private set; }

    public CharacterMoveSimulator MoveSimulator { get; private set; }

    public WeaponSimulator WeaponSimulator { get; private set; }

    public CharacterStateAnimationController StateController { get; private set; }

    public int ActorID
    {
        get { return State != null ? State.ActorId : 0; }
    }

    public TeamID Team
    {
        get { return State != null ? State.TeamID : 0; }
    }

    public UberStrike.Realtime.Common.CharacterInfo State
    {
        get { return _state != null ? _state.Info : null; }
    }

    public CharacterTrigger AimTrigger
    {
        get { return _aimTrigger; }
    }

    #endregion
}