
using System;
using System.Collections;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LocalPlayer : MonoBehaviour
{
    public enum PlayerState
    {
        None,
        FirstPerson,
        ThirdPerson,
        Death,
        FreeMove,
        Overview
    }

    private void Awake()
    {
        Initialize();
    }

    internal void Initialize()
    {
        _moveController = new CharacterMoveController(GetComponent<CharacterController>(), _characterBase);
        _moveController.CharacterLanded += OnCharacterGrounded;
    }

    private void OnEnable()
    {
        _moveController.Init();

        if (HudController.Exists)
            HudController.Instance.enabled = true;

        MonoRoutine.Start(StartPlayerIdentification());
        MonoRoutine.Start(StartUpdatePlayerPingTime(5));
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDisable()
    {

        if (!_isQuitting)
        {
            _isPaused = null;

            Screen.lockCursor = false;

            InputManager.Instance.IsInputEnabled = false;

            if (HudController.Exists)
                HudController.Instance.enabled = false;

            if (GlobalUIRibbon.Exists)
                GlobalUIRibbon.Instance.Show();

            IsInitialized = false;
        }
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Hud;

        if (GameState.HasCurrentPlayer && GameState.HasCurrentGame)
        {
            if (!Screen.lockCursor)
            {
                if (!IsGamePaused)
                {
                    Pause();

                    GlobalUIRibbon.Instance.Show();
                    InGameChatHud.Instance.Pause();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (_moveController != null && GameState.HasCurrentPlayer)
        {
            _moveController.UpdatePlayerMovement();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!IsGamePaused)
            {
                Pause();
            }

            if (GlobalUIRibbon.Exists) GlobalUIRibbon.Instance.Show();
        }
        //check all special key commands here
        else if (!InGameChatHud.Instance.CanInput)
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (!IsGamePaused)
                {
                    Pause();
                }
                if (GlobalUIRibbon.Exists) GlobalUIRibbon.Instance.Show();
            }
        }

#if UNITY_EDITOR
        if (KeyInput.AltPressed)
        {
            if (KeyInput.GetKeyDown(KeyCode.Y))
            {
                AmmoDepot.Reset();
                WeaponController.Instance.UpdateAmmoHUD();
            }

            if (KeyInput.GetKeyDown(KeyCode.K))
            {
                if (_currentCharacter)
                    _currentCharacter.ApplyDamage(new DamageInfo(999));
            }

            if (KeyInput.GetKeyDown(KeyCode.R))
            {
                GameState.CurrentGame.RespawnPlayerInSeconds(0, 0);
            }
        }
#endif

        if (InputManager.Instance.IsInputEnabled)
        {
            //is it generally possible for the player to look around
            if (Screen.lockCursor)
            {
                UserInput.UpdateMouse();
            }

            if (GameState.LocalCharacter.IsAlive)
            {
                UserInput.UpdateDirections();
            }

            if (GameState.HasCurrentPlayer)
            {
                _cameraTarget.localPosition = Vector3.Lerp(_cameraTarget.localPosition, GameState.LocalCharacter.CurrentOffset, 10 * Time.deltaTime);

                DoCameraBob();

                // enter only if mouse look matches internal state
                if (IsMouseLockStateConsistent)
                {
                    UpdateRotation();
                }

                if (_damageFactor != 0)
                {
                    if (_damageFactorDuration > 0)
                        _damageFactorDuration -= Time.deltaTime;

                    if (_damageFactorDuration <= 0 || !GameState.LocalCharacter.IsAlive)
                    {
                        _damageFactor = 0;
                        _damageFactorDuration = 0;
                    }
                }
            }
        }
        else
        {
            UserInput.ResetDirection();
        }
    }

    private void LateUpdate()
    {
        WeaponController.Instance.LateUpdate();
    }

    #region Private Methods

    private void UpdateRotation()
    {
        _cameraTarget.localRotation = _viewPointRotation * UserInput.Rotation;

        GameState.LocalCharacter.HorizontalRotation = UserInput.Rotation;
        GameState.LocalCharacter.VerticalRotation = (UserInput.Mouse.y + 90) / 180f;
    }

    private IEnumerator StartPlayerIdentification()
    {
        Collider hitCollider = null;
        AvatarHudInformation info = null;
        int count = 0;
        while (true)
        {
            yield return new WaitForSeconds(0.3f);

            if (!IsGamePaused && Camera.main && GameState.HasCurrentPlayer)
            {
                RaycastHit hit;

                Vector3 start = GameState.LocalCharacter.ShootingPoint + LocalPlayer.EyePosition;
                Vector3 end = start + GameState.LocalCharacter.ShootingDirection * 1000;

                if (Physics.Linecast(start, end, out hit, LayerUtil.CreateLayerMask(UberstrikeLayer.RemotePlayer)))
                {
                    if (hitCollider == hit.collider)
                    {
                        count++;
                    }
                    else
                    {
                        count = 0;
                        CharacterHitArea h = hit.collider.GetComponent<CharacterHitArea>();
                        if (h && h.Shootable != null && !h.Shootable.IsLocal)
                        {
                            CharacterConfig cc = h.Shootable as CharacterConfig;
                            if (cc != null && cc.AimTrigger)
                            {
                                hitCollider = hit.collider;
                                info = cc.AimTrigger.HudInfo;

                                ReticleHud.Instance.FocusCharacter(cc.Team);
                                SfxManager.Play2dAudioClip(SoundEffectType.GameFocusEnemy);
                            }
                        }
                        else
                        {
                            ReticleHud.Instance.UnFocusCharacter();
                            hitCollider = null;
                            info = null;
                        }
                    }

                    if (info != null)
                    {
                        info.Show(2);
                        info.IsBarVisible = count > 4;
                    }
                }
                else
                {
                    ReticleHud.Instance.UnFocusCharacter();
                    hitCollider = null;
                    count = 0;
                    info = null;
                }
            }
        }
    }

    private IEnumerator StartUpdatePlayerPingTime(int sec)
    {
        while (true)
        {
            if (GameState.HasCurrentPlayer)
            {
                GameState.LocalCharacter.Ping = GameConnectionManager.Client.PeerListener.Ping;
            }
            yield return new WaitForSeconds(sec);
        }
    }

    private void OnCharacterGrounded(float velocity)
    {
        if (GameState.HasCurrentGame && GameState.CurrentGame.IsMatchRunning && LevelCamera.Instance.CurrentBob == BobMode.None && _lastGrounded + Threshold < Time.time)
        {
            if (!GameState.LocalCharacter.Is(PlayerStates.DIVING))
            {
                _lastGrounded = Time.time;

                if (_currentCharacter && _currentCharacter.Decorator)
                {
                    _currentCharacter.Decorator.PlayFootSound(_currentCharacter.WalkingSoundSpeed);

                    if (velocity < -20)
                    {
                        LevelCamera.Instance.DoLandFeedback(true);
                        SfxManager.Play2dAudioClip(SoundEffectType.PcLandingGrunt);
                    }
                    else
                    {
                        LevelCamera.Instance.DoLandFeedback(false);
                    }
                }
            }
        }
    }

    private void DoCameraBob()
    {
        switch (GameState.LocalCharacter.PlayerState)
        {
            case PlayerStates.SWIMMING:
                LevelCamera.SetBobMode(BobMode.Swim);
                break;
            case PlayerStates.DUCKED | PlayerStates.GROUNDED:
                if (UserInput.IsWalking)
                {
                    if (WeaponController.Instance.IsSecondaryAction)
                        LevelCamera.SetBobMode(BobMode.None);
                    else
                        LevelCamera.SetBobMode(BobMode.Crouch);
                }
                else
                {
                    if (WeaponController.Instance.IsSecondaryAction)
                        LevelCamera.SetBobMode(BobMode.None);
                    else
                        LevelCamera.SetBobMode(BobMode.Idle);
                }
                break;
            case PlayerStates.FLYING:
                LevelCamera.SetBobMode(BobMode.Fly);
                break;
            case PlayerStates.GROUNDED:
                {
                    // walking weapon animation
                    if (UserInput.IsWalking)
                    {
                        if (WeaponController.Instance.IsSecondaryAction)
                            LevelCamera.SetBobMode(BobMode.None);
                        else
                            LevelCamera.SetBobMode(BobMode.Run);

                    }
                    else
                    {
                        if (WeaponController.Instance.IsSecondaryAction)
                            LevelCamera.SetBobMode(BobMode.None);
                        else if (!UserInput.IsWalking || _moveController.CurrentVelocity.y < RundownThreshold)
                            LevelCamera.SetBobMode(BobMode.Idle);
                    }
                    break;
                }
            case PlayerStates.IDLE:
                if (!UserInput.IsWalking || _moveController.CurrentVelocity.y < RundownThreshold)
                    LevelCamera.SetBobMode(BobMode.None);
                break;
            case PlayerStates.JUMPING:
                LevelCamera.SetBobMode(BobMode.None);
                break;
            default:
                if (!UserInput.IsWalking || _moveController.CurrentVelocity.y < RundownThreshold)
                    LevelCamera.SetBobMode(BobMode.None);
                break;
        }
    }

    #endregion

    public bool IsInitialized { get; private set; }

    public void InitializePlayer()
    {
        IsInitialized = true;

        try
        {
            if (LevelCamera.Exists)
            {
                LevelCamera.SetBobMode(BobMode.None);
                LevelCamera.Instance.CanDip = true;
                LevelCamera.Instance.IsZoomedIn = false;
                LevelCamera.Instance.EnableLowPassFilter(false);
            }

            if (GameState.LocalCharacter != null)
            {
                GameState.LocalCharacter.ResetState();

                if (HudAssets.Exists)
                {
                    HpApHud.Instance.AP = GameState.LocalCharacter.Armor.ArmorPoints;
                    HpApHud.Instance.HP = GameState.LocalCharacter.Health;
                }

                //update all weapons
                WeaponController.Instance.InitializeAllWeapons(_weaponAttachPoint);

                //here we update the complete loadout of the player - will be synced with the next game frame
                UpdateLocalCharacterLoadout();

                UpdateRotation();
            }
            else
            {
                Debug.LogError("CurrentPlayer is null!");
            }

            if (Decorator != null)
            {
                SetPlayerControlState(PlayerState.FirstPerson, Decorator.Configuration);

                Decorator.DisableRagdoll();
                Decorator.UpdateLayers();
                Decorator.MeshRenderer.enabled = false;
                Decorator.HudInformation.enabled = false;
            }

            IsDead = false;
            _moveController.Start();
            _moveController.ResetDuckMode();

            //if (QuickItemManager.Exists)
            //    QuickItemManager.Instance.ResetAllQuickItems();
            QuickItemController.Instance.Reset();

            if (!PanelManager.IsAnyPanelOpen)
                UnPausePlayer();

            DamageFactor = 0;
        }
        catch (Exception e)
        {
            throw CmuneDebug.Exception(e.InnerException, "InitializePlayer with {0}", CmunePrint.Properties(this));
        }
    }

    public void UpdateLocalCharacterLoadout()
    {
        var info = GameState.LocalCharacter;

        //GEAR
        info.Gear[0] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHead);
        info.Gear[1] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearFace);
        info.Gear[2] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearGloves);
        info.Gear[3] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearUpperBody);
        info.Gear[4] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearLowerBody);
        info.Gear[5] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearBoots);
        info.Gear[6] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.GearHolo);

        //QUICKITEMS
        info.QuickItems[0] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.QuickUseItem1);
        info.QuickItems[1] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.QuickUseItem2);
        info.QuickItems[2] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.QuickUseItem3);

        //FUNCS
        info.FunctionalItems[0] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.FunctionalItem1);
        info.FunctionalItems[1] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.FunctionalItem2);
        info.FunctionalItems[2] = LoadoutManager.Instance.GetItemIdOnSlot(LoadoutSlotType.FunctionalItem3);

        //WEAPONS
        IUnityItem w1 = LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponMelee).Item;
        IUnityItem w2 = LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponPrimary).Item;
        IUnityItem w3 = LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponSecondary).Item;
        IUnityItem w4 = LoadoutManager.Instance.GetItemOnSlot(LoadoutSlotType.WeaponTertiary).Item;
        info.Weapons.SetWeaponSlot(WeaponInfo.SlotType.Melee, (w1 != null) ? w1.ItemId : 0, (w1 != null) ? w1.ItemClass : 0);
        info.Weapons.SetWeaponSlot(WeaponInfo.SlotType.Primary, (w2 != null) ? w2.ItemId : 0, (w2 != null) ? w2.ItemClass : 0);
        info.Weapons.SetWeaponSlot(WeaponInfo.SlotType.Secondary, (w3 != null) ? w3.ItemId : 0, (w3 != null) ? w3.ItemClass : 0);
        info.Weapons.SetWeaponSlot(WeaponInfo.SlotType.Tertiary, (w4 != null) ? w4.ItemId : 0, (w4 != null) ? w4.ItemClass : 0);
        info.Weapons.SetWeaponSlot(WeaponInfo.SlotType.Pickup, 0, 0);

        //SKIN
        info.SkinColor = PlayerDataManager.SkinColor;

        //ARMOR
        int capacity = 0;
        int absorbtion = 0;
        LoadoutManager.Instance.GetArmorValues(out capacity, out absorbtion);

        info.Armor.AbsorbtionPercentage = (byte)absorbtion;
        info.Armor.ArmorPointCapacity = capacity;
        info.Armor.ArmorPoints = capacity;
    }

    /// <summary>
    /// Local player die
    /// </summary>
    public void SetPlayerDead()
    {
        if (!IsDead)
        {
            IsDead = true;

            Killer = null;

            if (!PlayerSpectatorControl.Instance.IsEnabled)
                InputManager.Instance.IsInputEnabled = false;

            UpdateWeaponController();
            CmuneEventHandler.Route(new OnPlayerDeadEvent());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    public void SpawnPlayerAt(Vector3 pos, Quaternion rot)
    {
        try
        {
            transform.position = pos + Vector3.up;

            //_cameraTarget.localPosition = Vector3.zero;
            _cameraTarget.localRotation = rot;

            UserInput.SetRotation(rot.eulerAngles.y);

            if (LevelCamera.Exists)
                LevelCamera.Instance.ResetFeedback();

            if (GameState.HasCurrentGame)
                GameState.CurrentGame.SendPlayerSpawnPosition(pos);

            if (GameState.HasCurrentPlayer)
                GameState.LocalCharacter.Position = pos;

            MoveController.ResetEnviroment();
            MoveController.Platform = null;
        }
        catch (Exception e)
        {
            throw CmuneDebug.Exception(e.InnerException, "SpawnPlayerAt with LocalPlayer {0}", CmunePrint.Properties(GameState.LocalPlayer));
        }
    }

    /// <summary>
    /// Assign the character that should be locally controlled
    /// </summary>
    /// <param name="character"></param>
    public void SetCurrentCharacterConfig(CharacterConfig character)
    {
        _currentCharacter = character;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="s"></param>
    public void SetPlayerControlState(PlayerState s, AvatarDecoratorConfig decorator = null)
    {
        _controlState = s;

        switch (_controlState)
        {
            case PlayerState.FirstPerson:
                {
                    //_cameraTarget.localPosition = _firstPersonView.localPosition;
                    _viewPointRotation = _firstPersonView.localRotation;

                    LevelCamera.Instance.SetTarget(_cameraTarget);
                    LevelCamera.Instance.SetMode(LevelCamera.CameraMode.FirstPerson);
                    LevelCamera.Instance.SetEyePosition(EyePosition.x, EyePosition.y, EyePosition.z);
                    if (LevelCamera.HasCamera)
                    {
                        LevelCamera.Instance.MainCamera.transform.localPosition = Vector3.zero;
                        LevelCamera.Instance.MainCamera.transform.localRotation = Quaternion.identity;
                    }

                    if (GameState.LocalPlayer.WeaponCamera)
                        GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(true);
                    break;
                }
            case PlayerState.ThirdPerson:
                {
                    _viewPointRotation = _firstPersonView.localRotation * Quaternion.Euler(10, 0, 0);

                    LevelCamera.Instance.SetTarget(_cameraTarget);
                    LevelCamera.Instance.SetMode(LevelCamera.CameraMode.ThirdPerson);
                    LevelCamera.Instance.MainCamera.transform.localPosition = new Vector3(2, 3, 0);
                    LevelCamera.Instance.MainCamera.transform.localRotation = Quaternion.Euler(45, 0, 0);

                    if (GameState.LocalPlayer.WeaponCamera)
                        GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(false);
                    break;
                }

            //case CameraState.ThirdPerson:
            //    {
            //        // enable my avatar
            //        if (Instance._currentCharacter && Instance._currentCharacter.Decorator &&
            //            Instance._currentCharacter.Decorator.MeshRenderer)
            //            Instance._currentCharacter.Decorator.MeshRenderer.enabled = true;

            //        Instance._target.localPosition = Instance._thirdPersonView.localPosition;
            //        Instance._viewPointRotation = Instance._firstPersonView.localRotation;

            //        LevelCamera.Instance.SetTarget(Instance._target);
            //        LevelCamera.Instance.SetMode(LevelCamera.CameraMode.ThirdPerson);
            //        LevelCamera.Instance.SetLookAtHeight(0.5f);

            //        WeaponController.Instance.SetFirstPersonEnabled(false);
            //        break;
            //    }
            //case CameraState.RemoteFollow:
            //    {
            //        Instance._target.localPosition = Instance._followPoint.localPosition;
            //        Instance._viewPointRotation = Instance._firstPersonView.localRotation;

            //        LevelCamera.Instance.SetTarget(Instance._target);
            //        LevelCamera.Instance.SetMode(LevelCamera.CameraMode.SmoothFollow);
            //        LevelCamera.Instance.SetLookAtHeight(0);

            //        WeaponController.Instance.SetFirstPersonEnabled(false);
            //        break;
            //    }
            case PlayerState.FreeMove:
                {
                    LevelCamera.Instance.SetLookAtHeight(0);
                    LevelCamera.Instance.SetMode(LevelCamera.CameraMode.Spectator);

                    GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(false);
                    break;
                }
            case PlayerState.Death:
                {
                    //if (decorator != null)
                    //{
                    //    Killer = decorator;
                    //    LevelCamera.Instance.SetTarget(decorator.GetBone(BoneIndex.Hips));
                    //}
                    LevelCamera.Instance.SetMode(LevelCamera.CameraMode.Ragdoll);
                    LevelCamera.Instance.SetLookAtHeight(1f);

                    GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(false);
                    break;
                }
            case PlayerState.Overview:
                {
                    LevelCamera.Instance.SetMode(LevelCamera.CameraMode.Overview);
                    LevelCamera.Instance.SetLookAtHeight(1f);

                    if (GameState.LocalDecorator != null)
                    {
                        GameState.LocalDecorator.SetLayers(UberstrikeLayer.RemotePlayer);
                        GameState.LocalDecorator.MeshRenderer.enabled = true;
                    }
                    GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(false);
                    break;
                }
            default:
                {
                    if (GameState.HasCurrentSpace)
                    {
                        LevelCamera.Instance.SetTarget(GameState.CurrentSpace.DefaultViewPoint);
                        LevelCamera.Instance.SetMode(LevelCamera.CameraMode.SmoothFollow);
                        LevelCamera.Instance.SetLookAtHeight(0);

                        GameState.LocalPlayer.WeaponCamera.SetCameraEnabled(false);
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Pause(bool force = false)
    {
        if (force || !_isPaused.HasValue || !_isPaused.Value)
        {
            _isPaused = true;

            InputManager.Instance.IsInputEnabled = false;
            LevelCamera.SetBobMode(BobMode.Idle);

            if (GameState.HasCurrentPlayer)
            {
                GameState.LocalCharacter.Keys = KeyState.Still;
                GameState.LocalCharacter.IsFiring = false;
            }

            if (GameState.HasCurrentGame && GameState.HasCurrentPlayer) WeaponController.Instance.StopInputHandler();

            Screen.lockCursor = false;

            UpdateWeaponController();

            if (GameState.HasCurrentGame)
            {
                //HudUtil.Instance.OnPause();
                CmuneEventHandler.Route(new OnPlayerPauseEvent());
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void UnPausePlayer()
    {
        PopupSystem.ClearAll();

        _isPaused = false;

        InputManager.Instance.IsInputEnabled = true;
        Screen.lockCursor = true;

        if (GlobalUIRibbon.Exists) GlobalUIRibbon.Instance.Hide();

        UpdateWeaponController();

        if (GameState.HasCurrentGame)
        {
            CmuneEventHandler.Route(new OnPlayerUnpauseEvent());
        }
    }

    public void SetWeaponControlState(PlayerHudState state)
    {
        _weaponControlState = state;

        UpdateWeaponController();
    }

    public void UpdateWeaponController()
    {
        switch (_weaponControlState)
        {
            case PlayerHudState.Playing:
                WeaponController.Instance.IsEnabled = !IsGamePaused && GameState.LocalCharacter.IsAlive;
                break;
            case PlayerHudState.Spectating:
            case PlayerHudState.AfterRound:
            case PlayerHudState.None:
                WeaponController.Instance.IsEnabled = false;
                break;
        }
    }

    #region Properties
    public CharacterConfig Character
    {
        get { return _currentCharacter; }
    }
    public AvatarDecorator Decorator
    {
        get { return _currentCharacter ? _currentCharacter.Decorator : null; }
    }
    public AvatarDecoratorConfig Killer { get; set; }
    public bool IsGamePaused
    {
        get { return _isPaused ?? false; }
    }
    public float DamageFactor
    {
        get { return _damageFactor; }
        set
        {
            _damageFactor = Mathf.Clamp01(value);
            _damageFactorDuration = _damageFactor * 15;

            _damageFactor /= 0.15f;
        }
    }
    public bool IsPlayerRespawned { get; set; }

    public void SetEnabled(bool enabled)
    {
        gameObject.SetActiveRecursively(enabled);
    }
    public bool IsMouseLockStateConsistent
    {
        get { return Screen.lockCursor; }
    }
    public bool IsWalkingEnabled
    {
        get { return _isWalkingEnabled && InputManager.Instance.IsInputEnabled; }
        set
        {
            _isWalkingEnabled = value;
        }
    }
    public bool IsShootingEnabled
    {
        get { return _isShootingEnabled && InputManager.Instance.IsInputEnabled; }
        set
        {
            //set general state
            _isShootingEnabled = value;

            //listen to user input
            WeaponController.Instance.IsEnabled = value;
        }
    }
    public PlayerState CurrentCameraControl
    {
        get { return _controlState; }
    }
    public CharacterMoveController MoveController
    {
        get { return _moveController; }
    }

    public WeaponCamera WeaponCamera { get { return _weaponCamera; } }
    public Transform WeaponAttachPoint { get { return _weaponAttachPoint; } }

    public bool IsDead { get; private set; }
    #endregion

    #region Inspector
    [SerializeField]
    private Transform _cameraTarget;
    [SerializeField]
    private Transform _characterBase;
    [SerializeField]
    private Transform _firstPersonView;

    [SerializeField]
    private Transform _weaponAttachPoint;
    [SerializeField]
    private WeaponCamera _weaponCamera;
    #endregion

    #region Fields
    protected PlayerHudState _weaponControlState = PlayerHudState.None;
    private CharacterMoveController _moveController;
    private CharacterConfig _currentCharacter;
    private PlayerState _controlState = PlayerState.None;
    private Quaternion _viewPointRotation = Quaternion.identity;
    private bool _isWalkingEnabled = true;
    private bool _isShootingEnabled = true;
    private bool? _isPaused;
    private bool _isQuitting = false;

    private float _damageFactor;
    private float _damageFactorDuration;
    private float _lastGrounded = 0;

    private const float RundownThreshold = -300;
    public const float Threshold = 0.5f;
    public static readonly Vector3 EyePosition = new Vector3(0, -0.1f, 0);
    #endregion
}