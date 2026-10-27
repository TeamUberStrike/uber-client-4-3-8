using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using UberStrike.Core.Types;
using UberStrike.Realtime.Common;
using UnityEngine;
using DeviceApplication = UnityEngine.Device.Application;

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

    // Diagnostic: log every fire trigger to pinpoint accidental auto-fire in the Editor preview.
    public static bool LogFireEvents = false;

    public static bool IsSupported
    {
        get
        {
            if (ApplicationDataManager.IsMobile || DeviceApplication.isMobilePlatform) return true;
#if UNITY_EDITOR
            return MobileMenuScale.ForcePreviewInEditor;
#else
            return false;
#endif
        }
    }

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

    // Screen rects of the default (reset) placements seeded so far. Every new default is
    // checked against these by distance and pushed away when they would overlap, so no two
    // default marker rects in the controls editor can ever intersect. Cleared by
    // MobileControlLayout.ResetAll so a reset re-seeds cleanly.
    private static readonly List<KeyValuePair<string, Rect>> _defaultSeedRects =
        new List<KeyValuePair<string, Rect>>();

    public static void ClearDefaultSeedRects()
    {
        _defaultSeedRects.Clear();
    }

    // Gap kept between neighbouring default markers (in pixels).
    private const float DefaultMarkerGap = 24f;

    // Pairs that are never visible at the same time (per input mode) may share a spot: the
    // single/multi scope buttons, and the D-pad versus the standalone fire / jump / crouch /
    // single-input scope buttons (the D-pad only exists in multi-touch mode, those buttons
    // only in single-input mode).
    private static bool AreModeSiblings(string a, string b)
    {
        if ((a == "secondaryFire" && b == "multiSecondaryFire") ||
            (a == "multiSecondaryFire" && b == "secondaryFire"))
            return true;

        bool aSingleOnly = a == "fire" || a == "jump" || a == "crouch" || a == "secondaryFire";
        bool bSingleOnly = b == "fire" || b == "jump" || b == "crouch" || b == "secondaryFire";
        return (a == "dpad" && bSingleOnly) || (b == "dpad" && aSingleOnly);
    }

    // Slides the given rect away from every previously seeded default rect it intersects.
    // Pushes are tried in an order that keeps the marker on screen (up, left, down, right)
    // and the pass repeats until the rect no longer touches any of its neighbours.
    private static Vector2 SeparateFromSeeded(string id, Vector2 c, float halfW, float halfH)
    {
        Rect r = new Rect(c.x - halfW, c.y - halfH, halfW * 2f, halfH * 2f);

        int maxPasses = _defaultSeedRects.Count * 2 + 4;
        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool moved = false;

            for (int i = 0; i < _defaultSeedRects.Count; i++)
            {
                if (AreModeSiblings(id, _defaultSeedRects[i].Key)) continue;
                Rect o = _defaultSeedRects[i].Value;
                if (!r.Overlaps(o)) continue;
                PushApart(ref r, o);
                moved = true;
            }

            if (!moved) break;
        }

        return r.center;
    }

    private static void PushApart(ref Rect r, Rect o)
    {
        // Up first (tall screens have room), then left / down / right — first candidate whose
        // resulting rect stays fully inside the display wins.
        float ny = o.yMin - r.height - DefaultMarkerGap;
        if (ny >= 0f) { r.y = ny; return; }

        float nx = o.xMin - r.width - DefaultMarkerGap;
        if (nx >= 0f) { r.x = nx; return; }

        ny = o.yMax + DefaultMarkerGap;
        if (ny + r.height <= Screen.height) { r.y = ny; return; }

        nx = o.xMax + DefaultMarkerGap;
        if (nx + r.width <= Screen.width) { r.x = nx; return; }

        // No fully on-screen escape — fall back to the axis of least penetration (the final
        // screen clamp may leave a residual overlap, but it is the best possible).
        float left = r.xMax - o.xMin;
        float right = o.xMax - r.xMin;
        float top = r.yMax - o.yMin;
        float bottom = o.yMax - r.yMin;
        float min = Mathf.Min(left, Mathf.Min(right, Mathf.Min(top, bottom)));
        if (min == top) r.y = o.yMin - r.height - DefaultMarkerGap;
        else if (min == bottom) r.y = o.yMax + DefaultMarkerGap;
        else if (min == left) r.x = o.xMin - r.width - DefaultMarkerGap;
        else r.x = o.xMax + DefaultMarkerGap;
    }

    // Records the rect of a freshly seeded default so later defaults keep their distance.
    // DefaultGuiCenterFor / DefaultSlotCenter run as GetOrDefault arguments on every
    // ApplyLayout even after their control is seeded, so skip ids that are already recorded.
    private static void RegisterSeededRect(string id, Vector2 c, float halfW, float halfH)
    {
        for (int i = 0; i < _defaultSeedRects.Count; i++)
        {
            if (_defaultSeedRects[i].Key == id) return;
        }
        _defaultSeedRects.Add(new KeyValuePair<string, Rect>(id,
            new Rect(c.x - halfW, c.y - halfH, halfW * 2f, halfH * 2f)));
    }

    // Quick-item slots seed their defaults from QuickItemGroupHud.DefaultSlotCenter; give them
    // the same separate-and-register treatment as the other default markers.
    public static Vector2 SeparateAndRegisterSlot(string id, Vector2 c, float half)
    {
        c = SeparateFromSeeded(id, c, half, half);
        float maxX = Mathf.Max(half, Screen.width - half);
        float maxY = Mathf.Max(half, Screen.height - half);
        c.x = Mathf.Clamp(c.x, Mathf.Min(half, maxX), maxX);
        c.y = Mathf.Clamp(c.y, Mathf.Min(half, maxY), maxY);
        RegisterSeededRect(id, c, half, half);
        return c;
    }

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
            InGameChatHud.Instance.CancelChat();

            // pop state, but use set to force OnEnter
            _stateMachine.SetState(_previousState);
        }
    }


    private void Start()
    {
        if (!IsSupported)
        {
            gameObject.SetActive(false);
            return;
        }

#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
        if (GetComponent<MobileControlLayoutEditor>() == null)
            gameObject.AddComponent<MobileControlLayoutEditor>();
#endif

        UseMultiTouch = ApplicationDataManager.ApplicationOptions.UseMultiTouch;

        _screenLeftRect = new Rect(0, 0, Screen.width * leftSideRatio, Screen.height);

        CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChanged);

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
        // Crouch behaves identically in both modes: tap to toggle, like the single-input
        // crouch circle (the D-pad crouch zone must NOT be a hold-to-crouch).
        Dpad.CrouchButton.OnTouchBegan += OnCrouchPushed;

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
        // Tap-anywhere-to-fire is intentionally not hooked: firing is always the on-screen Fire
        // button in both joystick and D-pad modes.
        // Button/D-pad/joystick/quick-item zones are fed to the Shooter by RefreshShooterIgnores()
        // (on state changes and layout edits) so they always match the current state, mode and layout.

        AimHelpText = new MeshGUIText("Drag finger to aim", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        HudStyleUtility.Instance.SetDefaultStyle(AimHelpText);
        AimHelpText.Position = new Vector2(Screen.width / 2, Screen.height / 2 - 100);
        AimHelpText.Scale = new Vector2(0.6f, 0.6f);
        AimHelpText.Alpha = 0.0f;

        ShootHelpText = new MeshGUIText("Tap the fire button to shoot", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        HudStyleUtility.Instance.SetDefaultStyle(ShootHelpText);
        ShootHelpText.Position = new Vector2(Screen.width / 2, Screen.height / 2 - 50);
        ShootHelpText.Scale = new Vector2(0.6f, 0.6f);
        ShootHelpText.Alpha = 0.0f;

        UpdateHelpTexts(UseMultiTouch);

        _stateMachine = new StateMachine();
        _stateMachine.RegisterState((int)TouchState.None, new TouchStateNone());
        _stateMachine.RegisterState((int)TouchState.Playing, new TouchStatePlaying());
        _stateMachine.RegisterState((int)TouchState.Sniping, new TouchStateSniping());
        _stateMachine.RegisterState((int)TouchState.Chatting, new TouchStateChatting());
        _stateMachine.RegisterState((int)TouchState.Paused, new TouchStatePaused());
        _stateMachine.RegisterState((int)TouchState.Death, new TouchStateDead());
        _stateMachine.RegisterState((int)TouchState.Scoreboard, new TouchStateScoreboard());

        _stateMachine.SetState((int)TouchState.None);

#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
        // Apply any saved custom on-screen layout (positions/scales) over the default placement.
        ApplyLayout();
#endif
    }

#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
    #region Customizable Layout

    public class LayoutHandle
    {
        public string Id;
        public string DisplayName;
        public Rect Boundary;   // GUI-space rect of the control
        public Texture Icon;    // icon to draw in the editor (may be null)
        public float Scale;
    }

    // Maps a stable layout id -> the button it controls. Only round (TouchButtonCircle) buttons
    // plus the weapon changer are repositionable; the joystick/D-pad/swipe are region-based and
    // keep their default placement (tracked as a follow-up).
    private static readonly KeyValuePair<string, TouchKeyType>[] _layoutKeyMap =
        new KeyValuePair<string, TouchKeyType>[]
    {
        new KeyValuePair<string, TouchKeyType>("fire", TouchKeyType.PrimaryFire),
        new KeyValuePair<string, TouchKeyType>("secondaryFire", TouchKeyType.SecondaryFire),
        new KeyValuePair<string, TouchKeyType>("multiSecondaryFire", TouchKeyType.MultiSecondaryFire),
        new KeyValuePair<string, TouchKeyType>("jump", TouchKeyType.Jump),
        new KeyValuePair<string, TouchKeyType>("crouch", TouchKeyType.Crouch),
        new KeyValuePair<string, TouchKeyType>("menu", TouchKeyType.Menu),
        new KeyValuePair<string, TouchKeyType>("chat", TouchKeyType.Chat),
        new KeyValuePair<string, TouchKeyType>("score", TouchKeyType.Score),
    };

    private float DefaultButtonScale()
    {
        // Comfortably big default controls: ~1.25x the old size, clamped to the same
        // MinScale..MaxScale limits the layout editor enforces.
        return Mathf.Clamp(Screen.height / 720f * 1.25f, MobileControlLayout.MinScale, MobileControlLayout.MaxScale);
    }

    // The D-pad's default scale: the regular button scale, capped so the D-pad's rotated
    // footprint (plus a small margin) still fits across the screen width. On a tall phone the
    // height-based scale reaches 3.75x — a D-pad marker wider than the display that overlapped
    // every other marker in the editor.
    public static float DefaultDpadScale(float baseScale)
    {
        float dw = (MobileIcons.KeyboardDpad != null) ? MobileIcons.KeyboardDpad.width : 399f;
        float dh = (MobileIcons.KeyboardDpad != null) ? MobileIcons.KeyboardDpad.height : 209f;
        float rad = 15f * Mathf.Deg2Rad;
        float aabbW = dw * Mathf.Cos(rad) + dh * Mathf.Sin(rad) + 16f;
        return Mathf.Clamp(baseScale, MobileControlLayout.MinScale,
            Mathf.Min(MobileControlLayout.MaxScale, Screen.width / aabbW));
    }

    private Vector2 DefaultGuiCenterFor(string id)
    {
        // Spread the default positions with the same scale as the button sizes, so bigger
        // buttons do not overlap each other. Every center is then clamped so the control's
        // full footprint stays inside the screen on any resolution/scale.
        float s = DefaultButtonScale();
        Vector2 c;
        float halfW, halfH;
        switch (id)
        {
            case "fire":
                c = new Vector2(Screen.width - 160 * s, Screen.height - 184 * s);
                halfW = TexSize(MobileIcons.FireIcon, 128) * s * 0.5f; halfH = halfW;
                break;
            case "secondaryFire":
                // Slightly further right than the classic spot so its rect stays clear of the
                // fire button's rect.
                c = new Vector2(Screen.width - 48 * s, Screen.height - 234 * s);
                halfW = TexSize(MobileIcons.SecondFireIcon, 128) * s * 0.5f; halfH = halfW;
                break;
            case "multiSecondaryFire":
                c = new Vector2(Screen.width - 60 * s, 440 * s);
                halfW = TexSize(MobileIcons.SecondFireIcon, 128) * s * 0.5f; halfH = halfW;
                break;
            case "jump":
                c = new Vector2(Screen.width - 88 * s, Screen.height - 72 * s);
                halfW = TexSize(MobileIcons.JumpIcon, 128) * s * 0.5f; halfH = halfW;
                break;
            case "crouch":
                // Slightly further left than the classic spot so its rect stays clear of the
                // fire button's rect.
                c = new Vector2(Screen.width - 272 * s, Screen.height - 116 * s);
                halfW = TexSize(MobileIcons.CrouchIcon, 128) * s * 0.5f; halfH = halfW;
                break;
            case "menu":
                c = new Vector2(30 * s, 200 * s);
                halfW = TexSize(MobileIcons.MenuIcon, 64) * s * 0.5f; halfH = halfW;
                break;
            case "chat":
                c = new Vector2(30 * s, 260 * s);
                halfW = TexSize(MobileIcons.ChatIcon, 64) * s * 0.5f; halfH = halfW;
                break;
            case "score":
                c = new Vector2(30 * s, 320 * s);
                halfW = TexSize(MobileIcons.ScoreboardIcon, 64) * s * 0.5f; halfH = halfW;
                break;
            case "weaponChanger":
            {
                c = new Vector2(Screen.width - 95 * s, 105 * s);
                Vector2 wd = WeaponIconDimensions();
                halfW = wd.x * s * 0.5f;
                halfH = wd.y * s * 0.5f;
                break;
            }
            case "dpad":
            {
                float dw = (MobileIcons.KeyboardDpad != null) ? MobileIcons.KeyboardDpad.width : 399f;
                float dh = (MobileIcons.KeyboardDpad != null) ? MobileIcons.KeyboardDpad.height : 209f;
                // The D-pad's default scale is additionally capped by the screen width: the
                // height-based scale reaches 3.75x on a tall phone, making the D-pad marker
                // wider than the display. Seeding happens before the other controls, so they
                // separate around this (capped) footprint.
                float ds = DefaultDpadScale(s);
                c = new Vector2(25f + dw * ds / 2f, Screen.height - 256f + dh * ds / 2f);
                // The default D-pad sits at 15°. Its axis-aligned footprint is wider than the
                // unrotated rect, so clamp with the rotated extents to guarantee it stays
                // fully inside the display at any scale/resolution.
                float rad = 15f * Mathf.Deg2Rad;
                float cosR = Mathf.Cos(rad);
                float sinR = Mathf.Sin(rad);
                halfW = (dw * cosR + dh * sinR) * ds * 0.5f;
                halfH = (dh * cosR + dw * sinR) * ds * 0.5f;
                break;
            }
            default:
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
        // Keep every freshly seeded default marker away from the ones seeded before it, then
        // clamp inside the screen and remember its rect for the next default.
        c = SeparateFromSeeded(id, c, halfW, halfH);
        c = ClampCenterToScreen(c, halfW, halfH);
        RegisterSeededRect(id, c, halfW, halfH);
        return c;
    }

    private static float TexSize(Texture tex, float fallback)
    {
        return (tex != null) ? tex.width : fallback;
    }

    private static Vector2 WeaponIconDimensions()
    {
        if (MobileIcons.WeaponIcons != null && MobileIcons.WeaponIcons.Length > 0 && MobileIcons.WeaponIcons[0] != null)
            return new Vector2(MobileIcons.WeaponIcons[0].width, MobileIcons.WeaponIcons[0].height);
        return new Vector2(64f, 64f);
    }

    // The layout editor draws the weapon changer with the machinegun icon as a generic symbol.
    private static Texture MachineGunIcon()
    {
        if (MobileIcons.WeaponIcons != null && MobileIcons.WeaponIcons.Length > (int)UberstrikeItemClass.WeaponMachinegun)
            return MobileIcons.WeaponIcons[(int)UberstrikeItemClass.WeaponMachinegun];
        return null;
    }

    // Keeps a control's center such that its whole footprint (half extents given) stays on
    // screen. If the control is bigger than the screen, its center is forced to the middle.
    private static Vector2 ClampCenterToScreen(Vector2 center, float halfW, float halfH)
    {
        float maxX = Mathf.Max(halfW, Screen.width - halfW);
        float maxY = Mathf.Max(halfH, Screen.height - halfH);
        center.x = Mathf.Clamp(center.x, Mathf.Min(halfW, maxX), maxX);
        center.y = Mathf.Clamp(center.y, Mathf.Min(halfH, maxY), maxY);
        return center;
    }

    private static string DisplayNameFor(string id)
    {
        switch (id)
        {
            case "fire": return "Fire";
            case "secondaryFire": return "Scope";
            case "multiSecondaryFire": return "Scope (multi)";
            case "jump": return "Jump";
            case "crouch": return "Crouch";
            case "menu": return "Menu";
            case "chat": return "Chat";
            case "score": return "Scores";
            case "weaponChanger": return "Weapons";
            case "dpad": return "D-Pad";
            case "quickItems": return "Quick Items";
            case "quickSlot1": return "Quick Item 1";
            case "quickSlot2": return "Quick Item 2";
            case "quickSlot3": return "Quick Item 3";
            default: return id;
        }
    }

    public void ToggleMovementMode()
    {
        OnModeChangePushed();
        UseMultiTouch = ApplicationDataManager.ApplicationOptions.UseMultiTouch;
        UpdateHelpTexts(UseMultiTouch);
        ApplicationDataManager.ApplicationOptions.SaveApplicationOptions();
        ApplyLayout();

        if (_stateMachine != null)
        {
            int state = _stateMachine.CurrentStateId;
            _stateMachine.SetState(state);
        }
    }

    // Re-applies the saved (or default) placement to every customizable control.
    public void ApplyLayout()
    {
        if (Buttons == null) return;

        // High-DPI phones report big pixel resolutions, so fixed-size buttons look tiny. Scale the
        // default button size with the screen height (same idea as the mobile menu scale).
        float baseScale = DefaultButtonScale();

        // Seed the D-pad FIRST: it is the biggest default marker, so the other controls
        // separate around it instead of the D-pad being pushed into the screen edge.
        if (Dpad != null)
        {
            // The D-pad is editable like the buttons: stored placement (id "dpad") drives its
            // top-left position, its uniform Scale (layout editor drag + pinch) and its
            // Rotation (layout editor twist), seeded with the classic 15° tilt.
            MobileControlLayout.Placement dp = MobileControlLayout.GetOrDefault("dpad", DefaultGuiCenterFor("dpad"), DefaultDpadScale(baseScale), 15f);
            Vector2 c = MobileControlLayout.ToPixels(dp);
            float dw = (MobileIcons.KeyboardDpad != null) ? MobileIcons.KeyboardDpad.width : 399f;
            float dh = (MobileIcons.KeyboardDpad != null) ? MobileIcons.KeyboardDpad.height : 209f;

            // Clamp with the ROTATED footprint (AABB at the D-pad's current degree) so the
            // whole cluster stays inside the display at any scale.
            float rad = dp.Rotation * Mathf.Deg2Rad;
            float cosR = Mathf.Abs(Mathf.Cos(rad));
            float sinR = Mathf.Abs(Mathf.Sin(rad));
            float dHalfW = (dw * cosR + dh * sinR) * dp.Scale * 0.5f;
            float dHalfH = (dh * cosR + dw * sinR) * dp.Scale * 0.5f;
            c = ClampCenterToScreen(c, dHalfW, dHalfH);

            Dpad.Scale = dp.Scale;
            Dpad.TopLeftPosition = new Vector2(c.x - dw * dp.Scale / 2f, c.y - dh * dp.Scale / 2f);
            Dpad.Rotation = dp.Rotation;
        }

        foreach (var entry in _layoutKeyMap)
        {
            TouchButton b;
            if (!Buttons.TryGetValue((int)entry.Value, out b)) continue;
            TouchButtonCircle circle = b as TouchButtonCircle;
            if (circle == null) continue;

            MobileControlLayout.Placement p = MobileControlLayout.GetOrDefault(entry.Key, DefaultGuiCenterFor(entry.Key), baseScale);
            Vector2 center = MobileControlLayout.ToPixels(p);

            // Scale can now reach 3.75x, so a saved center near an edge could push the button
            // off-screen. Clamp the applied center by the button's real (scaled) footprint so
            // every button stays fully on screen at any scale/resolution.
            Texture icon = (circle.Content != null) ? circle.Content.image : null;
            float halfW = ((icon != null) ? icon.width : 64f) * p.Scale * 0.5f;
            float halfH = ((icon != null) ? icon.height : 64f) * p.Scale * 0.5f;
            center = ClampCenterToScreen(center, halfW, halfH);
            circle.SetLayout(center, p.Scale);
        }

        // The left column keeps its classic order no matter what a saved layout says:
        // pause (menu) highest, chat in the middle, score (screen) lowest.
        TouchButton menuBtn, chatBtn, scoreBtn;
        if (Buttons.TryGetValue((int)TouchKeyType.Menu, out menuBtn) &&
            Buttons.TryGetValue((int)TouchKeyType.Chat, out chatBtn) &&
            Buttons.TryGetValue((int)TouchKeyType.Score, out scoreBtn))
        {
            TouchButtonCircle menuCircle = menuBtn as TouchButtonCircle;
            TouchButtonCircle chatCircle = chatBtn as TouchButtonCircle;
            TouchButtonCircle scoreCircle = scoreBtn as TouchButtonCircle;
            if (menuCircle != null && chatCircle != null && scoreCircle != null)
            {
                menuCircle.CenterPosition = new Vector2(30f * baseScale, 200f * baseScale);
                chatCircle.CenterPosition = new Vector2(30f * baseScale, 260f * baseScale);
                scoreCircle.CenterPosition = new Vector2(30f * baseScale, 320f * baseScale);
            }
        }

        // Anchor the right-side action cluster to the lower corner, shoot button included, no
        // matter what a saved layout says (so they can never end up on top of the quick
        // items). Single-input mode only — in D-pad mode fire / jump / crouch live on the
        // D-pad and the multi-input scope keeps its own top-right spot.
        if (!UseMultiTouch)
        {
            TouchButton fireBtn, scopeBtn, jumpBtn, crouchBtn;
            if (Buttons.TryGetValue((int)TouchKeyType.PrimaryFire, out fireBtn) &&
                Buttons.TryGetValue((int)TouchKeyType.SecondaryFire, out scopeBtn) &&
                Buttons.TryGetValue((int)TouchKeyType.Jump, out jumpBtn) &&
                Buttons.TryGetValue((int)TouchKeyType.Crouch, out crouchBtn))
            {
                TouchButtonCircle fireCircle = fireBtn as TouchButtonCircle;
                TouchButtonCircle scopeCircle = scopeBtn as TouchButtonCircle;
                TouchButtonCircle jumpCircle = jumpBtn as TouchButtonCircle;
                TouchButtonCircle crouchCircle = crouchBtn as TouchButtonCircle;
                if (fireCircle != null && scopeCircle != null && jumpCircle != null && crouchCircle != null)
                {
                    fireCircle.CenterPosition = new Vector2(Screen.width - 160f * baseScale, Screen.height - 184f * baseScale);
                    scopeCircle.CenterPosition = new Vector2(Screen.width - 48f * baseScale, Screen.height - 234f * baseScale);
                    jumpCircle.CenterPosition = new Vector2(Screen.width - 88f * baseScale, Screen.height - 72f * baseScale);
                    crouchCircle.CenterPosition = new Vector2(Screen.width - 272f * baseScale, Screen.height - 116f * baseScale);
                }
            }
        }

        if (WeaponChanger != null)
        {
            MobileControlLayout.Placement wp = MobileControlLayout.GetOrDefault("weaponChanger", DefaultGuiCenterFor("weaponChanger"), baseScale);
            Vector2 wc = MobileControlLayout.ToPixels(wp);
            // Use the icon's REAL half extents per axis (width/height differ: 155x77), matching
            // the drag clamp — clamping with the width for both axes pulled the changer back down.
            Vector2 wd = WeaponIconDimensions();
            wc = ClampCenterToScreen(wc, wd.x * wp.Scale * 0.5f, wd.y * wp.Scale * 0.5f);
            WeaponChanger.LayoutScale = wp.Scale;
            WeaponChanger.Position = wc;
        }

        if (HudAssets.Exists && WeaponsHud.Instance.QuickItems.Group != null)
        {
            WeaponsHud.Instance.QuickItems.RefreshPosition();
        }

        // Control positions/scales may have changed (layout editor, mode toggle, resolution),
        // so rebuild the Shooter's ignore zones once here.
        RefreshShooterIgnores();
    }

    // Snapshot of every editable control for the layout editor (id, current rect, icon, scale).
    public List<LayoutHandle> GetLayoutHandles()
    {
        var list = new List<LayoutHandle>();
        if (Buttons == null) return list;

        bool multi = UseMultiTouch;

        foreach (var entry in _layoutKeyMap)
        {
            // In D-pad mode fire / jump / crouch live on the D-pad or tap-anywhere, so their
            // standalone buttons do not exist in the live layout; hide their editor handles too.
            if (multi && (entry.Value == TouchKeyType.PrimaryFire ||
                          entry.Value == TouchKeyType.Jump ||
                          entry.Value == TouchKeyType.Crouch))
                continue;

            // The scope button is per-mode: single input edits its own, multi input its own.
            if (multi && entry.Value == TouchKeyType.SecondaryFire) continue;
            if (!multi && entry.Value == TouchKeyType.MultiSecondaryFire) continue;

            TouchButton b;
            if (!Buttons.TryGetValue((int)entry.Value, out b)) continue;
            TouchButtonCircle circle = b as TouchButtonCircle;
            if (circle == null) continue;

            list.Add(new LayoutHandle
            {
                Id = entry.Key,
                DisplayName = DisplayNameFor(entry.Key),
                Boundary = circle.Boundary,
                Icon = (circle.Content != null) ? circle.Content.image : null,
                Scale = circle.LayoutScale,
            });
        }

        if (WeaponChanger != null)
        {
            list.Add(new LayoutHandle
            {
                Id = "weaponChanger",
                DisplayName = DisplayNameFor("weaponChanger"),
                Boundary = WeaponChanger.Boundary,
                Icon = MachineGunIcon(),
                Scale = WeaponChanger.LayoutScale,
            });
        }

        if (Dpad != null && UseMultiTouch)
        {
            // The D-pad only exists in multi-touch (D-pad) mode — single-input uses the joystick
            // instead, so don't offer a D-Pad handle there.
            MobileControlLayout.Placement dp = MobileControlLayout.GetOrDefault("dpad", DefaultGuiCenterFor("dpad"), DefaultDpadScale(DefaultButtonScale()));
            list.Add(new LayoutHandle
            {
                Id = "dpad",
                DisplayName = DisplayNameFor("dpad"),
                Boundary = Dpad.DpadBoundary,
                Icon = MobileIcons.KeyboardDpad,
                Scale = dp.Scale,
            });
        }

        if (HudAssets.Exists && WeaponsHud.Instance.QuickItems.Group != null)
        {
            // The three items share ONE vertical column; its x follows the first item's
            // placement so the whole column can be moved independently of the weapon changer.
            MobileControlLayout.Placement p0 = MobileControlLayout.GetOrDefault("quickSlot1", QuickItemGroupHud.DefaultSlotCenter(0), 1f);
            float itemColumnX = MobileControlLayout.ToPixels(p0).x;

            for (int i = 0; i < 3; i++)
            {
                QuickItemHud q = WeaponsHud.Instance.QuickItems.GetLoadoutQuickItemHud(i);
                if (q == null) continue;

                MobileControlLayout.Placement qp = MobileControlLayout.GetOrDefault("quickSlot" + (i + 1), QuickItemGroupHud.DefaultSlotCenter(i), 1f);
                Vector2 center = MobileControlLayout.ToPixels(qp);

                Texture icon = q.GetIcon();
                float baseSize = (icon != null) ? Mathf.Max(icon.width, icon.height) : 64f;
                float side = baseSize * 0.8f * qp.Scale;
                center.x = itemColumnX;
                center = ClampCenterToScreen(center, side * 0.5f, side * 0.5f);
                // Items stay below the weapon changer (Y only — their own column x and
                // downward placement remain free), even for saved layouts.
                center = QuickItemGroupHud.KeepBelowWeaponChanger(center, side * 0.5f, i);

                list.Add(new LayoutHandle
                {
                    Id = "quickSlot" + (i + 1),
                    DisplayName = DisplayNameFor("quickSlot" + (i + 1)),
                    Boundary = new Rect(center.x - side / 2, center.y - side / 2, side, side),
                    Icon = icon,
                    Scale = qp.Scale,
                });
            }
        }

        return list;
    }

    #endregion
#endif

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

        // The scope buttons may have flipped on/off; rebuild the Shooter's ignore zones.
        RefreshShooterIgnores();
    }

    // Compact corner zone for the single-input movement wheel, scaled like the other default
    // controls. Kept self-contained (no layout-editor constants) so it also compiles on
    // non-touch platforms, where it is simply unused.
    private static Rect ComputeJoystickRect()
    {
        // Original wheel zone: the whole lower-left quarter of the screen.
        return new Rect(0, Screen.height / 2, Screen.width * 0.4f, Screen.height / 2);
    }

    private void OnScreenResolutionChanged(ScreenResolutionEvent ev)
    {
        // The joystick zone is anchored to the corner in pixels; recompute it when the screen
        // (or the simulated device in the Editor) changes so its marker doesn't end up
        // off-screen after switching devices.
        _joystickRect = ComputeJoystickRect();
        if (Joystick != null)
            Joystick.Boundary = _joystickRect;
    }

    private void SetupRects()
    {
        // Anchor the single-input movement wheel to the lower-left corner: a compact corner
        // zone instead of the whole lower-left quarter.
        _joystickRect = ComputeJoystickRect();

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
        _jumpPos = new Vector2(Screen.width - 88, Screen.height - 72);
        _crouchPos = new Vector2(Screen.width - 255, Screen.height - 116);

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

        if (GameState.HasCurrentGame)
        {
            bool showLoadout = !GamePageManager.IsCurrentPage(PageType.PreGame) &&
                _stateMachine.CurrentStateId == (int)TouchState.Death;
            if (Buttons[(int)TouchKeyType.Loadout].Enabled != showLoadout)
            {
                Buttons[(int)TouchKeyType.Loadout].Enabled = showLoadout;
                RefreshShooterIgnores();
            }
        }

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
            UpdateHelpTexts(UseMultiTouch);

            TouchInput.WishDirection = Vector2.zero;
            TouchInput.WishLook = Vector2.zero;

            // Detach tap-anywhere-to-fire defensively; the state re-entry below re-hooks it
            // when the new mode needs it (D-pad mode).
            Shooter.OnFireStart -= OnFireStart;
            Shooter.OnFireEnd -= OnFireEnd;

            // force state update
            int state = _stateMachine.CurrentStateId;
            _stateMachine.SetState(state);
        }
    }

    // Rebuilds the Shooter's ignore zones from every on-screen control that is currently
    // enabled. Triggered on discrete events only — entering a state, layout changes
    // (ApplyLayout), walking/input toggles, weapon changes and the loadout-button flip — so
    // the zones always match the current state, input mode and custom layout without any
    // per-frame cost. Fingers that start on the D-pad, joystick, fire/jump/... buttons,
    // quick items, weapon changer or scope swipe belong to those controls and must never count
    // as the look (first) or shoot (second) finger.
    private void RefreshShooterIgnores()
    {
        if (Shooter == null) return;

        _shooterIgnoreBuffer.Clear();

        if (Buttons != null)
        {
            // Iterate ALL buttons rather than a fixed key list: the pause/menu button, chat,
            // score, fire, jump, crouch, secondary, loadout — every button claims its current
            // (layout-editor-variable) area while enabled, so a finger that starts on any of
            // them never counts as the aim or fire finger.
            foreach (TouchButton b in Buttons.Values)
            {
                if (b != null && b.Enabled)
                    _shooterIgnoreBuffer.Add(b.Boundary);
            }
        }

        if (WeaponChanger != null && WeaponChanger.Enabled)
            _shooterIgnoreBuffer.Add(WeaponChanger.Boundary);
        if (ConsumableChanger != null && ConsumableChanger.Enabled)
            _shooterIgnoreBuffer.Add(ConsumableChanger.Boundary);
        if (Dpad != null && Dpad.Enabled)
            _shooterIgnoreBuffer.Add(Dpad.DpadBoundary);
        if (Joystick != null && Joystick.Enabled)
            _shooterIgnoreBuffer.Add(Joystick.Boundary);
        if (ScopeSwipe != null && ScopeSwipe.Enabled)
            _shooterIgnoreBuffer.Add(ScopeSwipe.Boundary);

        Shooter.SetIgnoreRects(_shooterIgnoreBuffer);
    }

    private void LateUpdate()
    {
        // Safety net for a lost TouchPhase.Ended or a fire button that got disabled while held:
        // if the fire button no longer owns a live finger, the PrimaryFire input must not stay 1.
        if (!IsFiring) return;

        if (UseMultiTouch)
        {
            // D-pad mode: fire is owned by the Shooter's secondary finger (tap-to-fire), NOT the
            // disabled fire button. Checking the button here released PrimaryFire one frame after
            // every tap -> the weapon never shot.
            if (Shooter == null || !Shooter.IsFireFingerLive)
                ReleasePrimaryFire("shooter fire finger no longer live");
        }
        else
        {
            TouchButton fire;
            if (Buttons == null ||
                !Buttons.TryGetValue((int)TouchKeyType.PrimaryFire, out fire) ||
                fire == null || !fire.Enabled || !fire.IsActive)
            {
                ReleasePrimaryFire("fire button no longer active");
            }
        }
    }

    private void ReleasePrimaryFire(string reason)
    {
        if (!IsFiring) return;

        IsFiring = false;
        if (LogFireEvents) Debug.Log("[TouchFireLog] RELEASE PrimaryFire (" + reason + ")");
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrimaryFire, 0));
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
        if (!GUITools.SaveClickIn(0.6f)) return;
        GUITools.Clicked();

        if (GamePageManager.IsCurrentPage(PageType.None))
        {
            GamePageManager.Instance.LoadPage(PageType.Shop);
        }
        else
        {
            if (!SceneGuiController.ClosePanel(null))
            {
                GamePageManager.Instance.UnloadCurrentPage();
            }
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
        if (LogFireEvents) Debug.Log("[TouchFireLog] FIRE BUTTON pressed at " + obj);
        if (!IsFiring)
        {
            IsFiring = true;
            CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrimaryFire, 1));
        }
    }

    void OnFireTouchEnded(Vector2 obj)
    {
        if (LogFireEvents) Debug.Log("[TouchFireLog] FIRE BUTTON released at " + obj);
        IsFiring = false;
        CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrimaryFire, 0));
    }

    void OnSecondaryFireTouchBegan(Vector2 obj)
    {
        _toggleSecondaryFire = !_toggleSecondaryFire;
        if (LogFireEvents) Debug.Log("[TouchFireLog] SECONDARY-FIRE button at " + obj + " toggle=" + _toggleSecondaryFire + " state=" + _stateMachine.CurrentStateId);

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

    private void UpdateHelpTexts(bool dpadMode)
    {
        if (AimHelpText == null || ShootHelpText == null) return;

        if (dpadMode)
        {
            AimHelpText.Text = "Touch anywhere to aim";
            ShootHelpText.Text = "Tap a second finger to shoot";
        }
        else
        {
            AimHelpText.Text = "Drag finger to aim";
            ShootHelpText.Text = "Tap the fire button to shoot";
        }
    }

    private void UpdateFireMode(bool tapAnywhereFire)
    {
        Shooter.OnFireStart -= OnFireStart;
        Shooter.OnFireEnd -= OnFireEnd;

        if (tapAnywhereFire)
        {
            Shooter.OnFireStart += OnFireStart;
            Shooter.OnFireEnd += OnFireEnd;
        }

        Debug.Log("[ShooterDiag] UpdateFireMode tapAnywhere=" + tapAnywhereFire);
    }

    void OnFireStart()
    {
        Debug.Log("[ShooterDiag] OnFireStart -> PrimaryFire pressed");
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
        WishJump = false;
    }

    void OnCrouchPushed(Vector2 obj)
    {
        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING)) == 0)
            WishCrouch = !WishCrouch;
    }

    void OnJump(Vector2 pos)
    {
        if ((GameState.LocalCharacter.PlayerState & (PlayerStates.DIVING | PlayerStates.SWIMMING)) != 0)
        {
            WishJump = true;
            return;
        }

        if (WishCrouch)
            WishCrouch = false;
        else
            WishJump = true;
    }

    void OnMenu()
    {
        if (!GamePageManager.IsCurrentPage(PageType.None))
        {
            if (!SceneGuiController.ClosePanel(() =>
            {
                if (GameState.LocalPlayer.IsGamePaused) GameState.LocalPlayer.UnPausePlayer();
            }))
            {
                GamePageManager.Instance.UnloadCurrentPage();
                GameState.LocalPlayer.UnPausePlayer();
            }
            return;
        }

        if (GameState.HasCurrentGame && GameState.LocalPlayer.IsGamePaused)
        {
            GameState.LocalPlayer.UnPausePlayer();
            return;
        }

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
        _keyboard = TouchScreenKeyboard.isSupported
            ? TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false)
            : null;
        InGameChatHud.Instance.OpenChat(_keyboard == null);

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

            TouchInput.Instance.RefreshShooterIgnores();
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
                TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = true;
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

#if UNITY_EDITOR || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID
            Rect qiRect = WeaponsHud.Instance.QuickItems.GetSimpleCombinedRect();
#else
            Rect qiRect = WeaponsHud.Instance.QuickItems.GetCombinedRect();
#endif
            if (qiRect.width <= 0 || qiRect.height <= 0)
                qiRect = new Rect(Screen.width * 0.9f, Screen.height * 0.3f, 63, 60);
            TouchInput.Instance.ConsumableChanger.Boundary = new Rect(qiRect.x, qiRect.y, qiRect.width, qiRect.height);

            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;

            TouchInput.Instance.ScopeSwipe.Enabled = false;
            TouchInput.Instance.Shooter.Enabled = true;

            TouchInput.Instance.RefreshShooterIgnores();
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
                // Movement scheme is the only mode difference: D-pad cluster (left) in multi-touch,
                // floating joystick otherwise. The D-pad's integrated Jump/Crouch zones are used in
                // D-pad mode and the right-side Jump/Crouch circles only in joystick mode, so they
                // never duplicate. In D-pad mode there is no Fire button (tap anywhere with a second
                // finger to shoot).
                bool dpadMode = UseMultiTouch;
                TouchInput.Instance.Dpad.Enabled = dpadMode;
                TouchInput.Instance.Joystick.Enabled = !dpadMode;
                if (dpadMode)
                {
                    TouchInput.Instance.Dpad.JumpButton.Enabled = true;
                    TouchInput.Instance.Dpad.CrouchButton.Enabled = true;
                }

                TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = !dpadMode;
                TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = !dpadMode;
                TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = !dpadMode;
                TouchInput.Instance.UpdateFireMode(dpadMode);

                WeaponSlot slot = WeaponController.Instance.GetCurrentWeapon();
                if (slot != null)
                {
                    // The scope button is per-mode: single input uses its own, multi input its own.
                    bool hasScope = slot.Item.Configuration.SecondaryAction != WeaponSecondaryAction.None;
                    TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = hasScope && !dpadMode;
                    TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = hasScope && dpadMode;
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
                TouchInput.Instance.UpdateFireMode(false);
            }

            TouchInput.Instance.RefreshShooterIgnores();
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

            TouchInput.Instance.RefreshShooterIgnores();
        }

        public void OnExit()
        {

        }

        public void OnUpdate()
        {
            TouchInput ti = TouchInput.Instance;
            ti.CheckKeyboardDone();

            if (ti._keyboard == null && !InGameChatHud.Instance.CanInput &&
                ti._stateMachine.CurrentStateId == (int)TouchState.Chatting)
            {
                ti._stateMachine.SetState(ti._previousState);
            }
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
            // Movement scheme is the only mode difference; the shared Secondary button stays visible
            // in both modes. In D-pad mode there is no Fire button (tap anywhere to shoot). Jump/Crouch
            // live on the D-pad in D-pad mode and on the right-side circles in joystick mode.
            bool dpadMode = UseMultiTouch;
            TouchInput.Instance.Dpad.Enabled = dpadMode;
            TouchInput.Instance.Joystick.Enabled = !dpadMode;
            if (dpadMode)
            {
                TouchInput.Instance.Dpad.JumpButton.Enabled = true;
                TouchInput.Instance.Dpad.CrouchButton.Enabled = true;
            }
            TouchInput.Instance.Buttons[(int)TouchKeyType.PrimaryFire].Enabled = !dpadMode;
            TouchInput.Instance.Buttons[(int)TouchKeyType.SecondaryFire].Enabled = !dpadMode;
            TouchInput.Instance.Buttons[(int)TouchKeyType.MultiSecondaryFire].Enabled = dpadMode;

            // A sniper still needs to jump and crouch while scoped.
            TouchInput.Instance.Buttons[(int)TouchKeyType.Jump].Enabled = !dpadMode;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Crouch].Enabled = !dpadMode;
            TouchInput.Instance.UpdateFireMode(dpadMode);
            TouchInput.Instance.Buttons[(int)TouchKeyType.Loadout].Enabled = false;
            TouchInput.Instance.AimHelpText.IsEnabled = false;
            TouchInput.Instance.ShootHelpText.IsEnabled = false;

            ZoomInfo zoomInfo = WeaponController.Instance.GetCurrentWeapon().Item.Configuration.ZoomInformation;
            if (zoomInfo.DefaultMultiplier != 1 && zoomInfo.MaxMultiplier != zoomInfo.MinMultiplier)
                TouchInput.Instance.ScopeSwipe.Enabled = true;
            else
                TouchInput.Instance.ScopeSwipe.Enabled = false;

            TouchInput.Instance.RefreshShooterIgnores();
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
            TouchInput.Instance.Buttons[(int)TouchKeyType.Menu].Enabled = true;
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = true;
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

            TouchInput.Instance.RefreshShooterIgnores();
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
            TouchInput.Instance.Buttons[(int)TouchKeyType.Chat].Enabled = true;
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

            TouchInput.Instance.RefreshShooterIgnores();
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

            TouchInput.Instance.RefreshShooterIgnores();
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
    private void OnApplicationFocus(bool isFocused)
    {
        if (!isFocused)
        {
            ReleasePrimaryFire("application lost focus");
#if !UNITY_EDITOR
            if (_keyboard == null)
            {
                OnMenu();
            }
#endif
        }
    }

#if !UNITY_EDITOR
    private Coroutine _pauseRoutine;

    private IEnumerator BeginPause()
    {
        // Grace period before a backgrounded app abandons the match. This was 5s, which — together with the
        // broken cancel below — kicked players back to the lobby whenever iOS briefly backgrounded the app
        // (Control Center / screen-recording swipe / a notification). Long enough now that a quick
        // interruption never drops the match, while a genuinely abandoned app still returns to the menu.
        yield return new WaitForSeconds(60.0f);

        _pauseRoutine = null;
        if (GameState.HasCurrentGame)
            GameStateController.Instance.UnloadGameMode();
        if (!MenuPageManager.IsCurrentPage(PageType.Login))
            MenuPageManager.Instance.LoadPage(PageType.Home);
    }
#endif

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused) ReleasePrimaryFire("application paused");
#if !UNITY_EDITOR
        if (isPaused)
        {
            // Only arm while actually in a match, and keep the handle so we can truly cancel it on resume.
            if (_pauseRoutine == null && GameState.HasCurrentGame)
                _pauseRoutine = StartCoroutine(BeginPause());
        }
        else
        {
            // BUG FIX: the old code called StopCoroutine("BeginPause") (by string) on a coroutine started by
            // reference — that does NOT stop it, so resuming never cancelled the pending kick. Stop by the
            // stored handle so returning from a brief background reliably keeps you in the match.
            if (_pauseRoutine != null)
            {
                StopCoroutine(_pauseRoutine);
                _pauseRoutine = null;
            }
        }
#endif
    }
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
        ReleasePrimaryFire("TouchInput disabled");
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
        bool stillDead = GameState.LocalCharacter != null && !GameState.LocalCharacter.IsAlive;
        _stateMachine.SetState(stillDead ? (int)TouchState.Death : (int)TouchState.Playing);
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
    private float _loadoutDebugTimer;

    private StateMachine _stateMachine;

    private int _previousState;

    // Reused buffer for RefreshShooterIgnores() to avoid re-allocating on every refresh.
    private readonly List<Rect> _shooterIgnoreBuffer = new List<Rect>();

    #endregion
}

