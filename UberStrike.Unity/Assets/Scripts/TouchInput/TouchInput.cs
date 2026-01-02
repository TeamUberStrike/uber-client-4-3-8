using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.Realtime.Common;
using UnityEngine;

public class TouchInput : MonoSingleton<TouchInput>
{
    public enum TouchKeyType
    {
        None,
        Look,
        Move,
        PrimaryFire,
        SecondaryFire,
        MultiSecondaryFire,
        Forward,
        Backward,
        Left,
        Right,
        Jump,
        Crouch,
        Zoom,
        Score,
        Menu,
        Chat,
        Loadout,
    }

    public enum TouchState
    {
        None,
        Playing,
        Chatting,
        Sniping,
        Death,
        Paused,
        Scoreboard
    }

    public static Vector2 WishLook;
    public static Vector2 WishDirection;
    public static bool WishJump;
    public static bool WishCrouch;
    public static bool IsFiring;


    public Dictionary<int, TouchButton> Buttons;
    public TouchDPad Dpad;
    public TouchShooter Shooter;
    public TouchJoystick Joystick;
    public TouchSwipeBar ScopeSwipe;
    public TouchWeaponChanger WeaponChanger;
    public TouchConsumableChanger ConsumableChanger;

    public MeshGUIText AimHelpText;
    public MeshGUIText ShootHelpText;

    [SerializeField]
    private GUISkin guiSkin;
    [SerializeField]
    private float leftSideRatio = 0.4f;
    [SerializeField]
    private Vector2 lookInteriaRolloff = new Vector2(10.0f, 12.0f);

    public static bool UseMultiTouch;

    public void CheckKeyboardDone()
    {
        // if keyboard is open, detect if it was done
        if (_keyboard != null && _keyboard.done)
        {
            InGameChatHud.Instance.PushMessage(_keyboard.text);
            _keyboard = null;

            // pop state, but use set to force OnEnter
            _stateMachine.SetState(_previousState);
        }
        else if (_keyboard != null && _keyboard.active == false)
        {
            _keyboard = null;

            // pop state, but use set to force OnEnter
            _stateMachine.SetState(_previousState);
        }
    }


    private void Start()
    {
        UseMultiTouch = ApplicationDataManager.ApplicationOptions.UseMultiTouch;

        _screenLeftRect = new Rect(0, 0, Screen.width * leftSideRatio, Screen.height);

        _currWeapon = 0;
        IsFiring = false;

        SetupRects();

        Buttons = new Dictionary<int, TouchButton>();

        TouchButtonCircle menu = new TouchButtonCircle(MobileIcons.MenuIcon);
        menu.CenterPosition = _backButtonPos;
        menu.OnPushed += OnMenu;
        menu.ShowEffect = false;
        Buttons.Add((int)TouchKeyType.Menu, menu);

        TouchButtonCircle chat = new TouchButtonCircle(MobileIcons.ChatIcon);
        chat.CenterPosition = _chatPos;
        chat.OnPushed += OnChatBegan;
        chat.ShowEffect = false;
        Buttons.Add((int)TouchKeyType.Chat, chat);

        TouchButtonCircle score = new TouchButtonCircle(MobileIcons.ScoreboardIcon);
        score.CenterPosition = _scoreButtonPos;
        score.OnTouchBegan += OnScoreTouchBegan;
        score.OnTouchEnded += OnScoreTouchEnd;
        score.ShowEffect = false;
        score.MinGUIAlpha = 0.5f;
        Buttons.Add((int)TouchKeyType.Score, score);

        WeaponChanger = new TouchWeaponChanger(MobileIcons.WeaponIcons);
        WeaponChanger.Position = _nextWeaponPos;
        WeaponChanger.OnNextWeapon += OnNextWeapon;
        WeaponChanger.OnPrevWeapon += OnPrevWeapon;

        ConsumableChanger = new TouchConsumableChanger();
        ConsumableChanger.OnNextConsumable += OnNextConsumable;
        ConsumableChanger.OnPrevConsumable += OnPrevConsumable;
        ConsumableChanger.OnStartUseConsumable += OnStartUseConsumable;
        ConsumableChanger.OnEndUseConsumable += OnEndUseConsumable;

        ScopeSwipe = new TouchSwipeBar(MobileIcons.SniperSwipeIcon);
        ScopeSwipe.Boundary = _scopeSwipeRect;
        ScopeSwipe.OnSwipeUp += new Action(OnScopeUp);
        ScopeSwipe.OnSwipeDown += new Action(OnScopeDown);

        // set up multi touch controls
        Dpad = new TouchDPad(MobileIcons.KeyboardDpad);
        Dpad.TopLeftPosition = new Vector2(25, Screen.height - 256);
        Dpad.Rotation = 15.0f;
        Dpad.JumpButton.OnTouchBegan += OnJump;
        Dpad.JumpButton.OnTouchEnded += OnJumpTouchEnded;
        Dpad.CrouchButton.OnTouchBegan += OnCrouchBegan;
        Dpad.CrouchButton.OnTouchEnded += OnCrouchEnded;

        TouchButtonCircle jump = new TouchButtonCircle(MobileIcons.JumpIcon);
        jump.CenterPosition = _jumpPos;
        jump.OnTouchBegan += OnJump;
        jump.OnTouchEnded += OnJumpTouchEnded;
        jump.MinGUIAlpha = 1.0f;
        Buttons.Add((int)TouchKeyType.Jump, jump);

        TouchButtonCircle crouch = new TouchButtonCircle(MobileIcons.CrouchIcon);
        crouch.CenterPosition = _crouchPos;
        crouch.OnTouchBegan += OnCrouchPushed;
        crouch.MinGUIAlpha = 1.0f;
        Buttons.Add((int)TouchKeyType.Crouch, crouch);

        // set up single touch controls
        Joystick = new TouchJoystick(MobileIcons.JoystickInner, MobileIcons.JoystickOuter);
        Joystick.Boundary = _joystickRect;
        Joystick.OnJoystickMoved += OnJoystickMoved;
        Joystick.OnJoystickStopped += OnJoystickStopped;

        TouchButtonCircle fire = new TouchButtonCircle(MobileIcons.FireIcon);
        fire.CenterPosition = _firePos;
        fire.OnTouchBegan += OnFireTouchBegan;
        fire.OnTouchEnded += OnFireTouchEnded;
        fire.MinGUIAlpha = 1.0f;
        Buttons.Add((int)TouchKeyType.PrimaryFire, fire);

        TouchButtonCircle secondaryFire = new TouchButtonCircle(MobileIcons.SecondFireIcon);
        secondaryFire.CenterPosition = _secondFirePos;
        secondaryFire.OnTouchBegan += OnSecondaryFireTouchBegan;
        secondaryFire.MinGUIAlpha = 1.0f;
        Buttons.Add((int)TouchKeyType.SecondaryFire, secondaryFire);

        TouchButtonCircle multiSecondaryFire = new TouchButtonCircle(MobileIcons.SecondFireIcon);
        multiSecondaryFire.CenterPosition = _secondFireMultiPos;
        multiSecondaryFire.OnTouchBegan += OnSecondaryFireTouchBegan;
        multiSecondaryFire.MinGUIAlpha = 1.0f;
        Buttons.Add((int)TouchKeyType.MultiSecondaryFire, multiSecondaryFire);

        TouchButton loadout = new TouchButton("Loadout", guiSkin.GetStyle("ButtonBlue"));
        loadout.Boundary = _loadoutRect;
        loadout.OnPushed += new Action(OnLoadoutPushed);
        Buttons.Add((int)TouchKeyType.Loadout, loadout);

        Shooter = new TouchShooter();
        Shooter.Boundary = new Rect(0, 0, Screen.width, Screen.height);
        if (UseMultiTouch)
        {
            Shooter.OnFireStart += OnFireStart;
            Shooter.OnFireEnd += OnFireEnd;
        }
        Shooter.IgnoreRect(WeaponChanger.Boundary);
        Shooter.IgnoreRect(Joystick.Boundary); // includes dpad boundary
        Shooter.IgnoreRect(new Rect(0, menu.Boundary.y, score.Boundary.width, score.Boundary.yMax - menu.Boundary.y));

        AimHelpText = new MeshGUIText("Drag finger to aim", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        HudStyleUtility.Instance.SetDefaultStyle(AimHelpText);
        AimHelpText.Position = new Vector2(Screen.width - 300, Screen.height - 150);
        AimHelpText.Scale = new Vector2(0.6f, 0.6f);
        AimHelpText.Alpha = 0.0f;

        ShootHelpText = new MeshGUIText("Tap second finger to shoot", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        HudStyleUtility.Instance.SetDefaultStyle(ShootHelpText);
        ShootHelpText.Position = new Vector2(Screen.width - 300, Screen.height - 100);
        ShootHelpText.Scale = new Vector2(0.6f, 0.6f);
        ShootHelpText.Alpha = 0.0f;

        _stateMachine = new StateMachine();
        _stateMachine.RegisterState((int)TouchState.None, new TouchStateNone());
        _stateMachine.RegisterState((int)TouchState.Playing, new TouchStatePlaying());
        _stateMachine.RegisterState((int)TouchState.Sniping, new TouchStateSniping());
        _stateMachine.RegisterState((int)TouchState.Chatting, new TouchStateChatting());
        _stateMachine.RegisterState((int)TouchState.Paused, new TouchStatePaused());
        _stateMachine.RegisterState((int)TouchState.Death, new TouchStateDead());
        _stateMachine.RegisterState((int)TouchState.Scoreboard, new TouchStateScoreboard());

        _stateMachine.SetState((int)TouchState.None);
    }

    void OnIdleTime()
    {
        ShootHelpText.FadeAlphaTo(1.0f, 1.0f);
        AimHelpText.FadeAlphaTo(1.0f, 1.0f);
        CheckIdleTime = false;
    }

    void OnStartUseConsumable()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.UseQuickItem, 1));
    }

    void OnEndUseConsumable()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.UseQuickItem, 0));
    }

    void OnPrevConsumable()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrevQuickItem, 1));
    }

    void OnNextConsumable()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.NextQuickItem, 1));
    }

    void OnJoystickStopped()
    {
        WishDirection = Vector2.zero;
        _playerMoving = false;
    }

    void OnJoystickMoved(Vector2 dir)
    {
        if (!_playerMoving)
            _playerMoving = true;
        WishDirection = dir;
    }

    void OnScoreTouchEnd(Vector2 obj)
    {
        // need to push state as popping does not engage OnEnter
        _stateMachine.PopState();
        _stateMachine.PushState(_previousState);
        TabScreenPanelGUI.Instance.ForceShow = false;
    }

    void OnScoreTouchBegan(Vector2 obj)
    {
        _previousState = _stateMachine.CurrentStateId;
        _stateMachine.PushState((int)TouchState.Scoreboard);
        TabScreenPanelGUI.Instance.ForceShow = true;
    }

    void OnWeaponChanged()
    {
        TouchInput.Instance.WeaponChanger.CheckWeaponChanged();
        if (WeaponController.Instance.GetCurrentWeapon().Item.Configuration.SecondaryAction != WeaponSecondaryAction.None
            && _stateMachine.CurrentStateId == (int)TouchState.Playing)
        {
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = !UseMultiTouch;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = UseMultiTouch;
        }
        else
        {
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = false;
        }
    }

    private void SetupRects()
    {
        _joystickRect = new Rect(0, Screen.height / 2, _screenLeftRect.width, Screen.height / 2);

        _backButtonPos = new Vector2(30, 200);
        _chatPos = new Vector2(30, 260);
        _scoreButtonPos = new Vector2(30, 320);

        _nextWeaponPos = new Vector2(Screen.width - 95, 105);
        _secondFireMultiPos = new Vector2(Screen.width - 60, 440);
        _scopeSwipeRect = new Rect(Screen.width - MobileIcons.SniperSwipeIcon.width - 24,
            Screen.height - 274 - MobileIcons.SniperSwipeIcon.height,
            MobileIcons.SniperSwipeIcon.width,
            MobileIcons.SniperSwipeIcon.height);

        // single finger
        _firePos = new Vector2(Screen.width - 160, Screen.height - 170);
        _secondFirePos = new Vector2(Screen.width - 64, Screen.height - 234);
        _jumpPos = new Vector2(Screen.width - 255, Screen.height - 116);
        _crouchPos = new Vector2(Screen.width - 88, Screen.height - 72);

        _loadoutRect = new Rect(20, Screen.height * 0.6f, 200, 50);
        _modeRect = new Rect(20, Screen.height * 0.7f, 250, 50);
    }

    public float IdleTimeBeforeHelp = 10;

    private void Update()
    {

        if (InputManager.Instance.IsDown(GameInputKey.UseQuickItem))
        {
            Debug.Log("Button down: TRUE");
        }
        _stateMachine.Update();

        if (!GameState.HasCurrentGame)
        {
            if (_stateMachine.CurrentStateId != (int)TouchState.None)
            {
                _stateMachine.SetState((int)TouchState.None);
                CheckIdleTime = false;
                ShootHelpText.Alpha = 0.0f;
                AimHelpText.Alpha = 0.0f;
            }
            return;
        }

        if (CheckIdleTime && !HasDisplayedFireHelp && LastFireTime + IdleTimeBeforeHelp < Time.time)
        {
            OnIdleTime();
        }

        // update help text
        AimHelpText.Draw();
        ShootHelpText.Draw();

        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING) & ~PlayerStates.GROUNDED) == 0)
            WishJump = false;

        if (_playerMoving || Dpad.Moving)
        {
            TouchController.Instance.GUIAlpha = Mathf.Lerp(TouchController.Instance.GUIAlpha, 0, Time.deltaTime * 4.0f);
        }
        else
        {
            TouchController.Instance.GUIAlpha = Mathf.Lerp(TouchController.Instance.GUIAlpha, 1, Time.deltaTime * 2.0f);
        }

        // check for change in weapons
        WeaponSlot slot = WeaponController.Instance.GetCurrentWeapon();
        if (slot != null && slot.Item.ItemClass != _currWeapon)
        {
            OnWeaponChanged();
            _currWeapon = slot.Item.ItemClass;
        }

        if (UseMultiTouch != ApplicationDataManager.ApplicationOptions.UseMultiTouch)
        {
            UseMultiTouch = ApplicationDataManager.ApplicationOptions.UseMultiTouch;

            TouchInput.WishDirection = Vector2.zero;
            TouchInput.WishLook = Vector2.zero;

            // unhook extra functions of right finger if in single fire mode
            if (UseMultiTouch)
            {
                Shooter.OnFireStart += OnFireStart;
                Shooter.OnFireEnd += OnFireEnd;
            }
            else
            {
                Shooter.OnFireStart -= OnFireStart;
                Shooter.OnFireEnd -= OnFireEnd;
            }

            // force state update
            int state = _stateMachine.CurrentStateId;
            _stateMachine.SetState(state);
        }
    }

    #region Event Responses

    void OnModeChangePushed()
    {
        ApplicationDataManager.ApplicationOptions.UseMultiTouch = !ApplicationDataManager.ApplicationOptions.UseMultiTouch;

        if (!ApplicationDataManager.ApplicationOptions.UseMultiTouch)
        {
            ShootHelpText.Alpha = 0;
            AimHelpText.Alpha = 0;
        }

        TouchInput.Instance.CheckIdleTime = !HasDisplayedFireHelp && ApplicationDataManager.ApplicationOptions.UseMultiTouch;
        TouchInput.Instance.LastFireTime = Time.time;
    }

    void OnLoadoutPushed()
    {
        if (GamePageManager.IsCurrentPage(PageType.None))
        {
            GamePageManager.Instance.LoadPage(PageType.Shop);
        }
        else
        {
            GamePageManager.Instance.UnloadCurrentPage();
        }
    }

    void OnScopeDown()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrevWeapon, 1));
    }

    void OnScopeUp()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.NextWeapon, 1));
    }

    void OnFireTouchBegan(Vector2 obj)
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrimaryFire, 1));
    }

    void OnFireTouchEnded(Vector2 obj)
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrimaryFire, 0));
    }

    void OnSecondaryFireTouchBegan(Vector2 obj)
    {
        _toggleSecondaryFire = !_toggleSecondaryFire;

        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.SecondaryFire, _toggleSecondaryFire ? 1 : 0));

        WeaponSlot weapon = WeaponController.Instance.GetCurrentWeapon();
        if (weapon == null) return;

        if (_toggleSecondaryFire && weapon.Item.Configuration.SecondaryAction == WeaponSecondaryAction.Zoom)
            _stateMachine.SetState((int)TouchState.Sniping);
        else
            _stateMachine.SetState((int)TouchState.Playing);
    }

    void OnFireEnd()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrimaryFire, 0));
        IsFiring = false;
    }

    void OnFireStart()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrimaryFire, 1));
        IsFiring = true;
        if (!HasDisplayedFireHelp)
        {
            CheckIdleTime = false;
            HasDisplayedFireHelp = true;
            AimHelpText.FadeAlphaTo(0.0f, 0.3f);
            ShootHelpText.FadeAlphaTo(0.0f, 0.3f);
        }
    }


    void OnJumpTouchEnded(Vector2 obj)
    {
        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING)) != 0)
            WishJump = false;
    }

    void OnCrouchPushed(Vector2 obj)
    {
        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING)) == 0)
            WishCrouch = !WishCrouch;
    }

    void OnCrouchBegan(Vector2 obj)
    {
        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING)) == 0)
            WishCrouch = true;
    }

    void OnCrouchEnded(Vector2 obj)
    {
        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING)) == 0)
            WishCrouch = false;
    }

    void OnJump(Vector2 pos)
    {
        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING)) == 0)
        {
            if (WishCrouch)
                WishCrouch = false;
            else
                WishJump = true;
        }
    }

    void OnMenu()
    {
        _stateMachine.PushState((int)TouchState.Paused);

        if (GameState.HasCurrentGame && !GameState.LocalPlayer.IsGamePaused) GameState.LocalPlayer.Pause();

        if (GlobalUIRibbon.Exists) GlobalUIRibbon.Instance.Show();

        CmuneEventHandler.Route(new OnMobileBackPressed());
    }

    void OnNextWeapon()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.NextWeapon, 1));
    }

    void OnPrevWeapon()
    {
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrevWeapon, 1));
    }

    void OnChatBegan()
    {
        _keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);
        InGameChatHud.Instance.OpenChat();

        // need to force push because it doesn't automatically call OnEnter
        _previousState = _stateMachine.CurrentStateId;
        _stateMachine.SetState((int)TouchState.Chatting);
    }

    #endregion

    #region States

    class TouchStateNone : IState
    {
        public void OnEnter()
        {
            TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Score].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;
            TouchInput.Instance.WeaponChanger.Enabled = false;
            TouchInput.Instance.Dpad.Enabled = false;
            TouchInput.Instance.Shooter.Enabled = false;
            TouchInput.Instance.ScopeSwipe.Enabled = false;
            TouchInput.Instance.ConsumableChanger.Enabled = false;
            TouchInput.Instance.AimHelpText.IsEnabled = false;
            TouchInput.Instance.ShootHelpText.IsEnabled = false;

            TouchInput.WishLook = Vector2.zero;
            TouchInput.WishDirection = Vector2.zero;
        }

        public void OnExit()
        {

        }

        public void OnUpdate()
        {

        }

        public void OnGUI()
        {

        }
    }

    class TouchStatePlaying : IState
    {
        public void OnEnter()
        {
            if (UseMultiTouch && !TouchInput.Instance.HasDisplayedFireHelp)
            {
                TouchInput.Instance.CheckIdleTime = true;
                TouchInput.Instance.LastFireTime = Time.time;
            }

            TouchInput.Instance.AimHelpText.IsEnabled = true;
            TouchInput.Instance.ShootHelpText.IsEnabled = true;


            if (GameStateController.Instance.StateMachine.CurrentStateId == (int)GameStateId.TryWeapon)
            {
                TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = true;
                TouchInput.Instance.WeaponChanger.Enabled = true;
                TouchInput.Instance.ConsumableChanger.Enabled = true;
            }
            else if (GameStateController.Instance.StateMachine.CurrentStateId != (int)GameStateId.Tutorial)
            {
                TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = true;
                TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = GameStateController.Instance.StateMachine.CurrentStateId != (int)GameStateId.Training;
                TouchInput.Instance.Buttons[(int)TouchKeyType.Score].Enabled = GameStateController.Instance.StateMachine.CurrentStateId != (int)GameStateId.Training;
                TouchInput.Instance.WeaponChanger.Enabled = true;
                TouchInput.Instance.ConsumableChanger.UpdateConsumablesHeld();
                TouchInput.Instance.ConsumableChanger.Enabled = true;
            }
            else
            {
                TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = false;
                TouchInput.Instance.WeaponChanger.Enabled = false;
                TouchInput.Instance.ConsumableChanger.Enabled = false;
            }

            UpdateWalkingEnabled();

            Vector2 qiPos = WeaponsHud.Instance.QuickItems.Group.GetPosition();
            TouchInput.Instance.ConsumableChanger.Boundary = new Rect(qiPos.x, qiPos.y, 63, 60);
            TouchInput.Instance.Shooter.IgnoreRect(TouchInput.Instance.ConsumableChanger.Boundary);

            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;

            TouchInput.Instance.ScopeSwipe.Enabled = false;
            TouchInput.Instance.Shooter.Enabled = true;
        }

        public void OnExit()
        {
        }

        private void UpdateWalkingEnabled()
        {
            _walkingEnabled = GameState.LocalPlayer.IsWalkingEnabled;
            _inputEnabled = InputManager.Instance.IsInputEnabled;
            if (_walkingEnabled && _inputEnabled)
            {
                if (UseMultiTouch)
                {
                    TouchInput.Instance.Dpad.Enabled = true;

                    TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
                    TouchInput.Instance.Joystick.Enabled = false;
                    TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
                    TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
                }
                else
                {
                    TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = true;

                    TouchInput.Instance.Joystick.Enabled = true;
                    TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = true;
                    TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = true;

                    TouchInput.Instance.Dpad.Enabled = false;
                }
                WeaponSlot slot = WeaponController.Instance.GetCurrentWeapon();
                if (slot != null)
                {
                    if (UseMultiTouch)
                    {
                        TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = slot.Item.Configuration.SecondaryAction != WeaponSecondaryAction.None;
                    }
                    else
                    {
                        TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = slot.Item.Configuration.SecondaryAction != WeaponSecondaryAction.None;

                    }
                }
            }
            else
            {
                TouchInput.Instance.Dpad.Enabled = false;
                TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
                TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = false;
                TouchInput.Instance.Joystick.Enabled = false;
                TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
                TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
                TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = false;
            }
        }

        public void OnUpdate()
        {
            if (_walkingEnabled != GameState.LocalPlayer.IsWalkingEnabled)
                UpdateWalkingEnabled();
            if (_inputEnabled != InputManager.Instance.IsInputEnabled)
                UpdateWalkingEnabled();

            float sensitivityRatio = 1.0f;
            if (TouchInput.IsFiring) sensitivityRatio = 0.75f;

            if (ApplicationDataManager.ApplicationOptions.UseMultiTouch)
                TouchInput.WishDirection = TouchInput.Instance.Dpad.Direction;

            Vector2 lookDelta = TouchInput.Instance.Shooter.Aim * ApplicationDataManager.ApplicationOptions.TouchLookSensitivity * sensitivityRatio;

            TouchInput.WishLook.x = Mathf.Lerp(WishLook.x, lookDelta.x, Time.deltaTime * TouchInput.Instance.lookInteriaRolloff.x);
            TouchInput.WishLook.y = Mathf.Lerp(WishLook.y, lookDelta.y, Time.deltaTime * TouchInput.Instance.lookInteriaRolloff.y);
        }

        public void OnGUI()
        {

        }

        private bool _walkingEnabled;
        private bool _inputEnabled;
    }

    class TouchStateChatting : IState
    {
        public void OnEnter()
        {
            TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = true;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Score].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = false;
            TouchInput.Instance.AimHelpText.IsEnabled = false;
            TouchInput.Instance.ShootHelpText.IsEnabled = false;

            TouchInput.Instance.WeaponChanger.Enabled = false;
            TouchInput.Instance.Dpad.Enabled = false;
            TouchInput.Instance.Shooter.Enabled = false;
            TouchInput.Instance.Joystick.Enabled = false;
            TouchInput.Instance.ScopeSwipe.Enabled = false;
            TouchInput.Instance.ConsumableChanger.Enabled = false;

            TouchInput.WishLook = Vector2.zero;
            TouchInput.WishDirection = Vector2.zero;
        }

        public void OnExit()
        {

        }

        public void OnUpdate()
        {
            TouchInput.Instance.CheckKeyboardDone();
        }

        public void OnGUI()
        {

        }
    }

    class TouchStateSniping : IState
    {
        public void OnEnter()
        {

            TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Score].Enabled = false;
            TouchInput.Instance.Shooter.Enabled = true;
            TouchInput.Instance.WeaponChanger.Enabled = false;
            TouchInput.Instance.ConsumableChanger.Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;
            TouchInput.Instance.AimHelpText.IsEnabled = false;
            TouchInput.Instance.ShootHelpText.IsEnabled = false;

            if (UseMultiTouch)
            {
                TouchInput.Instance.Dpad.Enabled = true;

                TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
                TouchInput.Instance.Joystick.Enabled = false;
            }
            else
            {
                TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = true;
                TouchInput.Instance.Joystick.Enabled = true;

                TouchInput.Instance.Dpad.Enabled = false;

            }
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = !UseMultiTouch;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = UseMultiTouch;

            ZoomInfo zoomInfo = WeaponController.Instance.GetCurrentWeapon().Item.Configuration.ZoomInformation;
            if (zoomInfo.DefaultMultiplier != 1 && zoomInfo.MaxMultiplier != zoomInfo.MinMultiplier)
                TouchInput.Instance.ScopeSwipe.Enabled = true;
            else
                TouchInput.Instance.ScopeSwipe.Enabled = false;
        }

        public void OnExit()
        {

        }

        public void OnUpdate()
        {
            if (ApplicationDataManager.ApplicationOptions.UseMultiTouch)
                TouchInput.WishDirection = TouchInput.Instance.Dpad.Direction;

            float sensitivityRatio = 0.5f;

            Vector2 lookDelta = TouchInput.Instance.Shooter.Aim * ApplicationDataManager.ApplicationOptions.TouchLookSensitivity * sensitivityRatio;
            TouchInput.WishLook.x = Mathf.Lerp(WishLook.x, lookDelta.x, Time.deltaTime * TouchInput.Instance.lookInteriaRolloff.x);
            TouchInput.WishLook.y = Mathf.Lerp(WishLook.y, lookDelta.y, Time.deltaTime * TouchInput.Instance.lookInteriaRolloff.y);
        }

        public void OnGUI()
        {

        }
    }

    class TouchStatePaused : IState
    {
        public void OnEnter()
        {
            TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Score].Enabled = GameStateController.Instance.StateMachine.CurrentStateId != (int)GameStateId.Training;
            TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = false;
            TouchInput.Instance.WeaponChanger.Enabled = false;
            TouchInput.Instance.ConsumableChanger.Enabled = false;
            TouchInput.Instance.Dpad.Enabled = false;
            TouchInput.Instance.Shooter.Enabled = false;
            TouchInput.Instance.Joystick.Enabled = false;
            TouchInput.Instance.ScopeSwipe.Enabled = false;
            TouchInput.Instance.AimHelpText.IsEnabled = false;
            TouchInput.Instance.ShootHelpText.IsEnabled = false;

            TouchInput.WishDirection = Vector2.zero;
            TouchInput.WishLook = Vector2.zero;
        }

        public void OnExit()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnGUI()
        {

        }
    }

    class TouchStateDead : IState
    {
        public void OnEnter()
        {
            TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = !GlobalUIRibbon.Instance.enabled;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = GameStateController.Instance.StateMachine.CurrentStateId != (int)GameStateId.Training;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Score].Enabled = GameStateController.Instance.StateMachine.CurrentStateId != (int)GameStateId.Training;
            TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = true;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = false;
            TouchInput.Instance.WeaponChanger.Enabled = false;
            TouchInput.Instance.ConsumableChanger.Enabled = false;
            TouchInput.Instance.Dpad.Enabled = false;
            TouchInput.Instance.Shooter.Enabled = false;
            TouchInput.Instance.Joystick.Enabled = false;
            TouchInput.Instance.ScopeSwipe.Enabled = false;
            TouchInput.Instance.AimHelpText.IsEnabled = false;
            TouchInput.Instance.ShootHelpText.IsEnabled = false;

            TouchInput.WishDirection = Vector2.zero;
            TouchInput.WishLook = Vector2.zero;
        }

        public void OnExit()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnGUI()
        {

        }
    }

    class TouchStateScoreboard : IState
    {
        public void OnEnter()
        {
            TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Score].Enabled = true;
            TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = false;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = false;
            TouchInput.Instance.WeaponChanger.Enabled = false;
            TouchInput.Instance.ConsumableChanger.Enabled = false;
            TouchInput.Instance.Shooter.Enabled = false;
            TouchInput.Instance.ScopeSwipe.Enabled = false;
            TouchInput.Instance.AimHelpText.IsEnabled = false;
            TouchInput.Instance.ShootHelpText.IsEnabled = false;

            HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlagGroup.Instance.BaseDrawFlag & ~HudDrawFlags.Weapons;

            if (UseMultiTouch)
            {
                TouchInput.Instance.Dpad.Enabled = !GameState.LocalPlayer.IsDead && !GameState.LocalPlayer.IsGamePaused;

                TouchInput.Instance.Joystick.Enabled = false;
            }
            else
            {
                TouchInput.Instance.Joystick.Enabled = !GameState.LocalPlayer.IsDead && !GameState.LocalPlayer.IsGamePaused;

                TouchInput.Instance.Dpad.Enabled = false;
            }
        }

        public void OnExit()
        {
            HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlagGroup.Instance.BaseDrawFlag | HudDrawFlags.Weapons;
        }

        public void OnUpdate()
        {
            if (ApplicationDataManager.ApplicationOptions.UseMultiTouch)
                TouchInput.WishDirection = TouchInput.Instance.Dpad.Direction;
        }

        public void OnGUI()
        {

        }
    }

    #endregion

    #region Life Cycle
#if !UNITY_EDITOR
    private void OnApplicationFocus(bool isFocused)
    {

        if (!isFocused && _keyboard == null)
        {
            OnMenu();
        }
    }

    private IEnumerator BeginPause()
    {
        yield return new WaitForSeconds(5.0f);

        if (GameState.HasCurrentGame)
            GameStateController.Instance.UnloadGameMode();
        if (!MenuPageManager.IsCurrentPage(PageType.Login))
            MenuPageManager.Instance.LoadPage(PageType.Home);
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            StartCoroutine(BeginPause());
        }
        else
        {
            StopCoroutine("BeginPause");
        }
    }
#endif
    #endregion

    #region Events

    private void OnEnable()
    {
        CmuneEventHandler.AddListener<OnPlayerDeadEvent>(OnPlayerDead);
        CmuneEventHandler.AddListener<OnPlayerRespawnEvent>(OnPlayerRespawned);
        CmuneEventHandler.AddListener<OnPlayerPauseEvent>(OnPlayerPaused);
        CmuneEventHandler.AddListener<OnPlayerUnpauseEvent>(OnPlayerUnpaused);
        CmuneEventHandler.AddListener<OnModeInitializedEvent>(OnModeInitialized);
        CmuneEventHandler.AddListener<OnMatchEndEvent>(OnMatchEndEvent);
    }

    private void OnDisable()
    {
        CmuneEventHandler.RemoveListener<OnPlayerDeadEvent>(OnPlayerDead);
        CmuneEventHandler.RemoveListener<OnPlayerRespawnEvent>(OnPlayerRespawned);
        CmuneEventHandler.RemoveListener<OnPlayerPauseEvent>(OnPlayerPaused);
        CmuneEventHandler.RemoveListener<OnPlayerUnpauseEvent>(OnPlayerUnpaused);
        CmuneEventHandler.RemoveListener<OnModeInitializedEvent>(OnModeInitialized);
        CmuneEventHandler.RemoveListener<OnMatchEndEvent>(OnMatchEndEvent);
    }

    private void OnMatchEndEvent(OnMatchEndEvent ev)
    {
#if UNITY_IPHONE
        EtceteraBinding.askForReview(2, 24, "Review!", "Enjoying UberStrike? Please give us a good review!", "https://userpub.itunes.apple.com/WebObjects/MZUserPublishing.woa/wa/addUserReview?id=541843276&type=Purple+Software");
#endif

        // Show pause menu
        if (GameState.HasCurrentGame && !GameState.LocalPlayer.IsGamePaused) GameState.LocalPlayer.Pause();

        if (GlobalUIRibbon.Exists) GlobalUIRibbon.Instance.Show();

    }

    private void OnPlayerDead(OnPlayerDeadEvent ev)
    {
        _stateMachine.SetState((int)TouchState.Death);
    }

    private void OnPlayerRespawned(OnPlayerRespawnEvent ev)
    {
        _stateMachine.SetState((int)TouchState.Playing);
    }

    private void OnPlayerPaused(OnPlayerPauseEvent ev)
    {
        _stateMachine.SetState((int)TouchState.Paused);
    }

    private void OnPlayerUnpaused(OnPlayerUnpauseEvent ev)
    {
        _stateMachine.SetState((int)TouchState.Playing);
    }

    private void OnModeInitialized(OnModeInitializedEvent ev)
    {
        HasDisplayedFireHelp = false;
    }

    #endregion

    #region Fields

    // screen segments
    private Rect _screenLeftRect;

    // single touch only
    private Vector2 _firePos;
    private Vector2 _secondFirePos;
    private Vector2 _jumpPos;
    private Vector2 _crouchPos;
    private Rect _joystickRect;

    // both
    private Vector2 _nextWeaponPos;
    private Rect _scopeSwipeRect;
    private Vector2 _secondFireMultiPos;
    private Vector2 _scoreButtonPos;
    private Vector2 _backButtonPos;
    private Vector2 _chatPos;
    private Rect _loadoutRect;
    private Rect _modeRect;

    private bool _toggleSecondaryFire = false;
    private bool _playerMoving = false;

    public float LastFireTime = 0;
    public bool HasDisplayedFireHelp = false;
    public bool CheckIdleTime = false;

    private UberstrikeItemClass _currWeapon;

    private TouchScreenKeyboard _keyboard;

    private StateMachine _stateMachine;

    private int _previousState;

    #endregion
}

