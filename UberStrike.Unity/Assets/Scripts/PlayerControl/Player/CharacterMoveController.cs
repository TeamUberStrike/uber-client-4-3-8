using System;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class CharacterMoveController
{
    #region Fields

    public const float POWERUP_HASTE_SCALE = 1.3f;
    public const float PLAYER_WADE_SCALE = 0.8f;
    public const float PLAYER_SWIM_SCALE = 0.6f;
    public const float PLAYER_DUCK_SCALE = 0.7f;
    public const float PLAYER_TERMINAL_GRAVITY = -100;
    public const float PLAYER_INITIAL_GRAVITY = -1;
    public const float PLAYER_ZOOM_SCALE = 0.7f;
    public const float PLAYER_MIN_SCALE = 0.5f;
    public const float PLAYER_IRON_SIGHT = 1;
    public bool IsLowGravity = false;

    public event Action<float> CharacterLanded;

    private readonly CharacterController _controller;
    private readonly PlayerAttributes _attributes;
    private readonly Transform _transform;
    private readonly Transform _playerBase;

    private MovingPlatform _platform;
    private EnviromentSettings _currentEnviroment;
    private CollisionFlags _collisionFlag;

    private Vector3 _currentVelocity;

    private Vector3 _acceleration;
    private bool _isOnLatter = false;
    private bool _isGrounded = true;
    private bool _canJump = true;
    private int _ungroundedCount;
    private int _waterLevel;
    private float _waterEnclosure;

    private ForceType _forceType = ForceType.Additive;
    private Vector3 _externalForce;
    private bool _hasExternalForce;

    #endregion

    #region Properties

    public bool IsJumpDisabled { get; set; }

    public float DamageSlowDown { get; set; }

    public float PlayerHeight
    {
        get { return GameState.LocalCharacter.Is(PlayerStates.DUCKED) ? PlayerAttributes.HEIGHT_DUCKED : PlayerAttributes.HEIGHT_NORMAL; }
    }

    public Vector3 Velocity
    {
        get { return _currentVelocity; }
    }

    public float SpeedModifier
    {
        get
        {
            float speedModifier = 1;

            float zoomFactor = WeaponController.Instance.IsSecondaryAction ? PLAYER_ZOOM_SCALE : 1;     //0.7
            float ironFactor = WeaponFeedbackManager.Instance.IsIronSighted ? PLAYER_IRON_SIGHT : 1;    //0.75
            float gearFactor = 1 - Mathf.Clamp01(PlayerDataManager.Instance.GearWeight) * 0.3f;         //0.7
            float damageFactor = 1 - Mathf.Clamp01(GameState.LocalPlayer.DamageFactor) * 0.3f;         //0.7

            speedModifier *= Mathf.Min(1, zoomFactor, ironFactor, gearFactor, damageFactor);

            // reduce the speed lower if wading or walking in water
            if (WaterLevel > 0)
            {
                if (WaterLevel == 3)
                {
                    speedModifier *= PLAYER_SWIM_SCALE;     //0.8
                }
                else
                {
                    speedModifier *= PLAYER_WADE_SCALE;     //0.7
                }
            }
            else
            {
                // reduce the speed lower if ducking
                if (IsGrounded && GameState.LocalCharacter.Is(PlayerStates.DUCKED))
                {
                    speedModifier *= PLAYER_DUCK_SCALE;     //0.7
                }
            }

            return Mathf.Max(PLAYER_MIN_SCALE, speedModifier);      //0.5 - 1
        }
    }

    public MovingPlatform Platform
    {
        get { return _platform; }
        set { _platform = value; }
    }

    public Vector3 CurrentVelocity
    {
        get { return _currentVelocity; }
    }

    public int WaterLevel
    {
        get { return _waterLevel; }
        private set
        {
            _waterLevel = value;
        }
    }

    public bool IsGrounded
    {
        get { return _isGrounded; }
        private set { _isGrounded = value; }
    }

    public CharacterController DebugController { get { return _controller; } }

    public UnityEngine.Bounds DebugEnvBounds { get { return _currentEnviroment.EnviromentBounds; } }

    #endregion

    #region Enums

    public enum ForceType
    {
        Additive,
        Exclusive,
    }

    #endregion

    #region Public Methods

    public CharacterMoveController(CharacterController controller, Transform characterBase)
    {
        _controller = controller;
        _transform = _controller.transform;

        _attributes = new PlayerAttributes();

        _playerBase = characterBase;

        CmuneEventHandler.AddListener<InputChangeEvent>(OnInputChanged);
    }

    public void Init()
    {
        if (LevelEnviroment.Instance != null)
            _currentEnviroment = LevelEnviroment.Instance.Settings;
        else
            Debug.LogWarning("You are trying to access the LevelEnvironment Instance that has not had Awake called.");
    }

    public void Start()
    {
        Reset();
    }

    public void UpdatePlayerMovement()
    {
        if (GameState.HasCurrentPlayer)
        {
            UpdateMovementStates();

            UpdateMovement();
        }
    }

    public void ResetDuckMode()
    {
        _controller.height = PlayerAttributes.HEIGHT_NORMAL;//PLAYER_HEIGHT;
        _controller.center = new Vector3(0, 0, 0);
    }

    public static bool HasCollision(Vector3 pos)
    {
        return UnityEngine.Physics.CheckSphere(pos, 0.6f, UberstrikeLayerMasks.CrouchMask);
    }

    public void ApplyForce(Vector3 v, ForceType type)
    {
        //Debug.Log(Time.realtimeSinceStartup + " ApplyForce " + v);
        _hasExternalForce = true;
        _externalForce = v;
        _forceType = type;
    }

    public void ClearForce()
    {
        _externalForce = Vector3.zero;
    }

    public void ResetEnviroment()
    {
        _currentEnviroment = LevelEnviroment.Instance.Settings;
        _currentEnviroment.EnviromentBounds = new UnityEngine.Bounds();

        _isOnLatter = false;
    }

    public void SetEnviroment(EnviromentSettings settings, UnityEngine.Bounds bounds)
    {
        _currentEnviroment = settings;
        _currentEnviroment.EnviromentBounds = new UnityEngine.Bounds(bounds.center, bounds.size);

        _isOnLatter = _currentEnviroment.Type == EnviromentSettings.TYPE.LATTER;
    }

    #endregion

    #region Private Methods

    private void UpdateMovementStates()
    {
        //HACK - the trigger for the latter system sometimes fails to detect a OnTriggerExit. 
        //Here we check the enclosure for the player manually and reset the environment if there is no intersection anymore
        if (_currentEnviroment.Type == EnviromentSettings.TYPE.LATTER)
        {
            if (!_currentEnviroment.EnviromentBounds.Intersects(_controller.bounds))
            {
                ResetEnviroment();
            }
        }

        if (_currentEnviroment.Type == EnviromentSettings.TYPE.WATER)
        {

            //when moving in water we want to check how DEEP the player is inside
            _currentEnviroment.CheckPlayerEnclosure(_playerBase.position, PlayerHeight, out _waterEnclosure);

            //PlayerManager.Instance.Depth = _waterEnclosure;

            //based on the percentage of enclosure [0,1] we set the waterlevel to 
            //-0 above water
            //-1 walking on water (only visual/audial effects)
            //-2 wading in water (speed and fallDamage affected)
            //-3 fully under water (speed, fallDamage and oxygenDamage affected)

            int level = 1;
            if (_waterEnclosure >= 0.8f) level = 3;
            else if (_waterEnclosure >= 0.4f) level = 2;

            //only change if needed
            if (WaterLevel != level) SetWaterlevel(level);
        }
        else
        {
            if (WaterLevel != 0) SetWaterlevel(0);
        }

        if ((GameState.LocalCharacter.Keys & KeyState.Jump) == 0)
        {
            _canJump = true;
        }

        if (GameState.LocalCharacter.Is(PlayerStates.GROUNDED | PlayerStates.JUMPING))
        {
            GameState.LocalCharacter.Set(PlayerStates.JUMPING, false);
        }
    }

    private void UpdateMovement()
    {
        CheckDuck();

        if (GameState.LocalCharacter.Is(PlayerStates.FLYING))
        {
            FlyInAir();
        }
        else if (WaterLevel > 2)
        {
            MoveInWater();
        }
        else if (_isOnLatter)
        {
            MoveOnLatter();
        }
        // walking on ground
        else if (IsGrounded)
        {
            MoveOnGround();
        }
        else if (WaterLevel == 2)
        {
            MoveOnWaterRim();
        }
        // airborne
        else
        {
            MoveInAir();
        }

        //APPLY EXTERNAL FORCES
        if (_hasExternalForce)
        {
            switch (_forceType)
            {
                case ForceType.Additive: _currentVelocity = Vector3.Scale(_currentVelocity, new Vector3(1, 0.5f, 1)) + (_externalForce * LevelEnviroment.Modifier); break;
                case ForceType.Exclusive: _currentVelocity = _externalForce * LevelEnviroment.Modifier; break;
            }

            _externalForce *= 0;
            _hasExternalForce = false;

            GameState.LocalCharacter.Set(PlayerStates.JUMPING, true);
        }

        Vector3 external;
        if (IsGrounded && Platform)
        {
            external = Platform.GetMovementDelta();
            //if movement goes up we remove the constant gravity
            if (external.y > 0)
                _currentVelocity.y = 0;
        }
        else
        {
            external = Vector3.zero;
        }

        //clamp general vertical velocity
        _currentVelocity[1] = Mathf.Clamp(_currentVelocity[1], -150, 150);

        //perform physical movement
        _collisionFlag = _controller.Move(_currentVelocity * Time.deltaTime);

        if (IsGrounded && Platform)
        {
            //move character on platform outside of physics (yhis is only needed if we use the controllers velocity to correct our own currentVelocity)
            _transform.localPosition += external;
        }

        _currentVelocity = _controller.velocity;

        //check collision
        bool isGrounded = (_collisionFlag & CollisionFlags.CollidedBelow) != 0;

        //SET GROUNDED FLAG
        if (isGrounded)
        {
            //call all grounded methods
            if (_ungroundedCount > 5 && CharacterLanded != null)
                CharacterLanded(_currentVelocity.y);

            _ungroundedCount = 0;
            IsGrounded = true;
        }
        else
        {
            //when jumping we set the state to UNgrounded immedieatly
            if (GameState.LocalCharacter.Is(PlayerStates.JUMPING))
            {
                _ungroundedCount++;
                IsGrounded = false;
            }
            //in case of being unbrounded more tha 5 frames we set the state to UNgrounded
            //this is catching Ungrounded jitter while walking down a hill
            else if (_ungroundedCount > 5)
            {
                IsGrounded = false;
            }
            else
            {
                _ungroundedCount++;
                IsGrounded = true;
            }
        }

        GameState.LocalCharacter.Set(PlayerStates.GROUNDED, IsGrounded);
        GameState.LocalCharacter.Position = _controller.transform.position;
    }

    private void OnInputChanged(InputChangeEvent ev)
    {
        if (GameState.LocalCharacter != null)
        {
            if (GameState.LocalPlayer.IsWalkingEnabled)
            {
                if (ev.IsDown)
                    GameState.LocalCharacter.Keys |= UserInput.GetkeyState(ev.Key);
                else
                    GameState.LocalCharacter.Keys &= ~UserInput.GetkeyState(ev.Key);
            }
        }
    }

    private void Reset()
    {
        SetWaterlevel(0);

        _currentVelocity = Vector3.zero;

        _forceType = ForceType.Additive;
        _hasExternalForce = false;
        _externalForce = Vector3.zero;

        _canJump = true;
        _isGrounded = true;
        _ungroundedCount = 0;

        _platform = null;
        IsJumpDisabled = false;
    }

    private void ApplyFriction()
    {
        Vector3 v = _currentVelocity;
        float speed = v.magnitude;

        //No friction because movement to small
        if (speed == 0)
        {
            return;
        }
        else if (speed < 0.5f && _acceleration.sqrMagnitude == 0)// && accelspeed == 0)
        {
            if (_isOnLatter) _currentVelocity[1] = 0;
            _currentVelocity[0] = 0;
            _currentVelocity[2] = 0;		// allow sinking underwater
        }
        else
        {
            float newspeed, control, drop = 0;

            // apply ground friction
            if (WaterLevel < 3)
            {
                if (_isOnLatter || GameState.LocalCharacter.Is(PlayerStates.GROUNDED))
                {
                    control = Mathf.Max(_currentEnviroment.StopSpeed, speed);
                    drop += control * _currentEnviroment.GroundFriction;
                }
            }

            // apply water friction even if just wading
            else if (WaterLevel > 0)
            {
                drop += Mathf.Max(_currentEnviroment.StopSpeed, speed) * _currentEnviroment.WaterFriction * WaterLevel / 3;// frameTime;
            }

            // apply flying friction
            if (GameState.LocalCharacter.Is(PlayerStates.FLYING))
            {
                control = Mathf.Max(_currentEnviroment.StopSpeed, speed);
                drop += control * _currentEnviroment.FlyFriction;
            }

            //if (GameState.CurrentPlayer.Is(PlayerStates.SPECTATOR))
            //{
            //    control = Mathf.Max(_currentEnviroment.StopSpeed, speed);
            //    drop += control * _currentEnviroment.SpectatorFriction;
            //}

            drop *= Time.deltaTime;

            // scale the velocity
            newspeed = speed - drop;

            if (newspeed < 0)
            {
                newspeed = 0;
            }
            newspeed /= speed;

            _currentVelocity *= newspeed;
        }
    }

    private void ApplyAcceleration(Vector3 wishdir, float wishspeed, float accel, bool clamp = false)
    {
        float currentspeed = Vector3.Dot(_currentVelocity, wishdir);
        float addspeed = wishspeed - currentspeed;

        if (addspeed <= 0)
        {
            _acceleration = Vector3.zero;
            return;
        }

        _acceleration = accel * wishspeed * wishdir * Time.deltaTime;

        //only accelerate if we didn't hit the max speed
        Vector3 v = _currentVelocity + _acceleration;

        float speed = v.magnitude;
        if (speed < wishspeed)
        {
            _currentVelocity += _acceleration;
        }
#if !UNITY_ANDROID && !UNITY_IPHONE
        else if (clamp)
        {
            _currentVelocity = (_currentVelocity + _acceleration).normalized * wishspeed;
            _currentVelocity[1] = v[1];
        }
        else
        {
            _currentVelocity = (_currentVelocity + _acceleration).normalized * speed;
        }
#endif
    }

    private void CheckDuck()
    {
        if (WaterLevel < 3 && GameState.HasCurrentPlayer && !GameState.LocalCharacter.Is(PlayerStates.JUMPING) && !GameState.LocalCharacter.Is(PlayerStates.FLYING))
        {
            if (UserInput.IsPressed(KeyState.Crouch) && !GameState.LocalCharacter.Is(PlayerStates.DUCKED))
            {
                GameState.LocalCharacter.Set(PlayerStates.DUCKED, true);
                _controller.height = PlayerAttributes.HEIGHT_DUCKED;
                _controller.center = new Vector3(0, PlayerAttributes.CENTER_OFFSET_DUCKED, 0);
            }
            else
            {
                if (!UserInput.IsPressed(KeyState.Crouch) && GameState.LocalCharacter.Is(PlayerStates.DUCKED))
                {
                    if (!HasCollision(_playerBase.position + new Vector3(0, 1.4f, 0)))
                    {
                        GameState.LocalCharacter.Set(PlayerStates.DUCKED, false);
                        _controller.height = PlayerAttributes.HEIGHT_NORMAL;
                        _controller.center = new Vector3(0, PlayerAttributes.CENTER_OFFSET_NORMAL, 0);
                    }
                }
            }
        }
    }

    private bool CheckJump()
    {
        if (IsJumpDisabled || GameState.LocalCharacter.Is(PlayerStates.DUCKED) ||
            (GameState.LocalCharacter.Keys & KeyState.Jump) == 0) // not holding jump
        {
            // don't allow jump until all buttons are up
            return false;
        }

        if (_isOnLatter)
        {
            return true;
        }
        // must wait for jump to be released
        else if (_canJump)
        {
            _canJump = false;

            GameState.LocalCharacter.Set(PlayerStates.GROUNDED, false);
            GameState.LocalCharacter.Set(PlayerStates.JUMPING, true);

            _currentVelocity.y = _attributes.JumpForce;

            return true;
        }
        else
        {
            // clear upmove so cmdscale doesn't lower running speed
            UserInput.HorizontalDirection.y = 0;
            return false;
        }
    }

    private bool CheckWaterJump()
    {
        if ((GameState.LocalCharacter.Keys & KeyState.Jump) != 0 && (_collisionFlag & CollisionFlags.CollidedSides) != 0)
        {
            // must wait for jump to be released
            if (!_canJump)
            {
                // clear upmove so cmdscale doesn't lower running speed
                UserInput.HorizontalDirection.y = 0;
                return false;
            }
            else
            {
                GameState.LocalCharacter.Set(PlayerStates.JUMPING, true);
                _currentVelocity.y = _attributes.JumpForce;
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    private void FlyInAir()
    {
        ApplyFriction();

        Vector3 wishDir = Vector3.zero;

        if (UserInput.IsWalking)
        {
            wishDir = UserInput.Rotation * UserInput.HorizontalDirection;
        }

        if (UserInput.VerticalDirection.y != 0)
        {
            wishDir.y = UserInput.VerticalDirection.y;
        }

        ApplyAcceleration(wishDir, _attributes.Speed, _currentEnviroment.FlyAcceleration);

        //PlayerManager.Instance.WishDir = wishDir;
        //PlayerManager.Instance.Velocity = _currentVelocity;
        //PlayerManager.Instance.Method = "FlyInAir";
    }

    private void MoveInWater()
    {
        ApplyFriction();

        Vector3 wishDir = Vector3.zero;

        if (UserInput.IsWalking)
        {
            wishDir = UserInput.Rotation * UserInput.HorizontalDirection;
        }

        if (UserInput.IsMovingVertically)
        {
            wishDir.y = UserInput.VerticalDirection.y;
        }

        ApplyAcceleration(wishDir, _attributes.Speed * SpeedModifier, _currentEnviroment.WaterAcceleration);

        if (_currentVelocity[1] > -3)
            _currentVelocity[1] -= Gravity * 0.1f;
        else
            _currentVelocity[1] = Mathf.Lerp(_currentVelocity[1], -3, Time.deltaTime * 6);
    }

    private void MoveOnLatter()
    {
        //if (IsGrounded)
        //{
        //    if (!CheckJump())
        //    {
        //        MoveOnGround();
        //        return;
        //    }
        //}

        ApplyFriction();

        Vector3 wishDir = Vector3.zero;

        if (UserInput.IsWalking)
        {
            wishDir = UserInput.Rotation * UserInput.HorizontalDirection;
        }

        if (UserInput.IsMovingVertically)
        {
            wishDir.y = UserInput.VerticalDirection.y;
        }

        ApplyAcceleration(wishDir, _attributes.Speed * SpeedModifier, _currentEnviroment.GroundAcceleration);
    }

    private void MoveOnWaterRim()
    {
        ApplyFriction();

        Vector3 wishDir = Vector3.zero;

        if (UserInput.IsWalking)
        {
            wishDir = UserInput.Rotation * UserInput.HorizontalDirection;
        }

        if (UserInput.IsMovingDown)
        {
            wishDir.y = UserInput.VerticalDirection.y;
        }
        else if (UserInput.IsMovingUp && _waterEnclosure > 0.8f)
        {
            wishDir.y = UserInput.VerticalDirection.y * 0.5f;
        }
        else
        {
            wishDir.y = 0;
        }

        ApplyAcceleration(wishDir, _attributes.Speed * SpeedModifier, _currentEnviroment.WaterAcceleration, true);

        if (_waterEnclosure < 0.7f || !UserInput.IsMovingVertically)
        {
            if (_currentVelocity[1] > -3)
                _currentVelocity[1] -= Gravity * 0.1f;
            else
                _currentVelocity[1] = Mathf.Lerp(_currentVelocity[1], -3, Time.deltaTime * 6);
        }
        else if (_currentVelocity[1] > 0 && _waterEnclosure < 0.8f)
        {
            //clamp velocity if moving up the rim
            _currentVelocity[1] = Mathf.Lerp(_currentVelocity[1], -1, Time.deltaTime * 4);
        }

        CheckWaterJump();
    }

    private void MoveInAir()
    {
        ApplyFriction();

        Vector3 wishDir = UserInput.Rotation * UserInput.HorizontalDirection;
        wishDir[1] = 0;

        ApplyAcceleration(wishDir, _attributes.Speed, _currentEnviroment.AirAcceleration);

        _currentVelocity[1] -= Gravity;
    }

    private void MoveOnGround()
    {
        if (CheckJump())
        {
            // jumped away
            if (WaterLevel > 1)
            {
                MoveInWater();
            }
            else
            {
                MoveInAir();
            }
            return;
        }

        ApplyFriction();

        Vector3 wishDir = UserInput.Rotation * UserInput.HorizontalDirection;


        wishDir[1] = 0;
#if !UNITY_ANDROID && !UNITY_IPHONE
        wishDir.Normalize();
#endif

        // perform acceleration
        ApplyAcceleration(wishDir, _attributes.Speed * SpeedModifier, _currentEnviroment.GroundAcceleration);

        //keep the gravity factor constant
        _currentVelocity[1] = -Gravity;
    }

    private void SetWaterlevel(int level)
    {
        _waterLevel = level;

        if (GameState.HasCurrentPlayer)
        {
            GameState.LocalCharacter.Set(PlayerStates.DIVING, level == 3);
            GameState.LocalCharacter.Set(PlayerStates.SWIMMING, level == 2);
            GameState.LocalCharacter.Set(PlayerStates.WADING, level == 1);
        }
        else
        {
            Debug.LogError("Failed to set water level!");
        }
    }

    private float Gravity { get { return (IsLowGravity ? 0.4f : 1) * _currentEnviroment.Gravity * Time.deltaTime; } }

    #endregion
}
