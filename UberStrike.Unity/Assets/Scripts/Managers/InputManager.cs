using System.Collections.Generic;
using Cmune.Util;
using UnityEngine;

public class InputManager : AutoMonoBehaviour<InputManager>
{
    private void Awake()
    {
        _mouseScrollThreshold = 0.01f;
        SetDefaultKeyMapping();
    }

    private void Update()
    {
        KeyInput.Update();

        if (IsInputEnabled)
        {
            foreach (UserInputMap userInputMap in _keyMapping.Values)
            {
                if (userInputMap != null && userInputMap.Channel != null)
                {
                    userInputMap.Channel.Listen();
                    if (userInputMap.IsEventSender && userInputMap.Channel.IsChanged)
                    {
                        CmuneEventHandler.Route(new InputChangeEvent(userInputMap.Slot, userInputMap.Channel.Value));
                    }
                }
            }
        }

        if (RawValue(GameInputKey.Fullscreen) != 0 && GUITools.SaveClickIn(0.2f))
        {
            GUITools.Clicked();

            ScreenResolutionManager.IsFullScreen = !Screen.fullScreen;
        }
    }

    private void OnGUI()
    {
        KeyInput.OnGUI();

        MouseInput.Instance.OnGUI();

        //Fix for scroll input being broken when SHIFT held down - this is due to shift modifying vertical scroll to horizontal scroll
        if (Event.current.type == EventType.ScrollWheel)
        {
            if (GetKeyAssignmentString(GameInputKey.Crouch) == "Left Shift" && GetValue(GameInputKey.Crouch) == 1)
            {
                // Zoom out or select prev weapon
                if (Event.current.delta.x > 0)
                    CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.PrevWeapon, Event.current.delta.x));

                // Zoom in or select next weapon
                if (Event.current.delta.x < 0)
                    CmuneEventHandler.Route(new InputChangeEvent(GameInputKey.NextWeapon, Event.current.delta.x));
            }
        }
    }

    /// <summary>
    /// GetKeyDown return true for the moment a key is pressed down
    /// The function is designed to be working inside of Update() as well as OnGUI()
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool GetMouseButtonDown(int button)
    {
        return (Event.current == null || Event.current.type == EventType.layout)
            && Input.GetMouseButtonDown(button);
    }

    public bool ListenForNewKeyAssignment(UserInputMap map)
    {
        if (Event.current.keyCode == KeyCode.Escape)
        {
            IsSettingKeymap = false;

            //don't assign any key to the mapping
            return true;
        }

        if (Event.current.keyCode != KeyCode.None)
        {
            map.Channel = new KeyInputChannel(Event.current.keyCode);
        }
        else if (Event.current.shift)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                map.Channel = new KeyInputChannel(KeyCode.LeftShift);

            if (Input.GetKey(KeyCode.RightShift))
                map.Channel = new KeyInputChannel(KeyCode.RightShift);
        }
        else if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(3) || Input.GetMouseButtonDown(4))
        {
            map.Channel = new MouseInputChannel(Event.current.button);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("Mouse ScrollWheel")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("Mouse ScrollWheel", 0.1f, Input.GetAxisRaw("Mouse ScrollWheel") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("GamePadHorizontal1")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("GamePadHorizontal1", 0.1f, Input.GetAxisRaw("GamePadHorizontal1") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("GamePadVertical1")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("GamePadVertical1", 0.1f, Input.GetAxisRaw("GamePadVertical1") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("GamePadHorizontal2")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("GamePadHorizontal2", 0.1f, Input.GetAxisRaw("GamePadHorizontal2") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("GamePadVertical2")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("GamePadVertical2", 0.1f, Input.GetAxisRaw("GamePadVertical2") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("GamePadHorizontal3")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("GamePadHorizontal3", 0.1f, Input.GetAxisRaw("GamePadHorizontal3") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("GamePadVertical3")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("GamePadVertical3", 0.1f, Input.GetAxisRaw("GamePadVertical3") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Mathf.Abs(Input.GetAxisRaw("GamePadTrigger")) > 0.1f)
        {
            map.Channel = new AxisInputChannel("GamePadTrigger", 0.1f, Input.GetAxisRaw("GamePadTrigger") > 0 ? AxisInputChannel.AxisReadingMethod.PositiveOnly : AxisInputChannel.AxisReadingMethod.NegativeOnly);
        }
        else if (Input.GetButton("GamePadButtonA"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonA");
        }
        else if (Input.GetButton("GamePadButtonB"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonB");
        }
        else if (Input.GetButton("GamePadButtonX"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonX");
        }
        else if (Input.GetButton("GamePadButtonY"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonY");
        }
        else if (Input.GetButton("GamePadButtonLB"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonLB");
        }
        else if (Input.GetButton("GamePadButtonRB"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonRB");
        }
        else if (Input.GetButton("GamePadButtonStart"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonStart");
        }
        else if (Input.GetButton("GamePadButtonBack"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonBack");
        }
        else if (Input.GetButton("GamePadButtonLS"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonLS");
        }
        else if (Input.GetButton("GamePadButtonRS"))
        {
            map.Channel = new ButtonInputChannel("GamePadButtonRS");
        }
        else if (Input.GetButton("GamePadButton10"))
        {
            map.Channel = new ButtonInputChannel("GamePadButton10");
        }
        else if (Input.GetButton("GamePadButton11"))
        {
            map.Channel = new ButtonInputChannel("GamePadButton11");
        }
        else
        {
            IsSettingKeymap = true;
            return false;
        }

        CmuneEventHandler.Route(new InputAssignmentEvent());

        //avoid that the GUI event trigers any other kind of action
        Event.current.Use();

        ResolveMultipleAssignment(map);

        WriteAllKeyMappings();

        IsSettingKeymap = false;

        return true;
    }

    public void Reset()
    {
        _keyMapping.Clear();
        SetDefaultKeyMapping();

        IsGamepadEnabled = false;

        WriteAllKeyMappings();
    }

    public float RawValue(GameInputKey slot)
    {
        UserInputMap m;
        if (!IsSettingKeymap && _keyMapping.TryGetValue((int)slot, out m))
        {
            return m.RawValue();
        }
        else return 0;
    }

    public float GetValue(GameInputKey slot)
    {
        UserInputMap m;
        if (!IsSettingKeymap && IsInputEnabled && _keyMapping.TryGetValue((int)slot, out m))
        {
            return m.Value;
        }
        else return 0;
    }

    public bool IsDown(GameInputKey slot)
    {
        UserInputMap m;
        if (!IsSettingKeymap && _keyMapping.TryGetValue((int)slot, out m))
        {
            return m.Value != 0;
        }
        else return false;
    }

    public string GetKeyAssignmentString(GameInputKey slot)
    {
        UserInputMap m;
        if (_keyMapping.TryGetValue((int)slot, out m) && m != null)
        {
            return m.Assignment;
        }
        else
        {
            return "Not set";
        }
    }

    public string GetSlotName(GameInputKey slot)
    {
        switch (slot)
        {
            case GameInputKey.Backward: return "Backward";
            case GameInputKey.Chat: return "Chat";
            case GameInputKey.Crouch: return "Crouch";
            case GameInputKey.ChangeTeam: return "Change Team";
            case GameInputKey.Forward: return "Forward";
            case GameInputKey.Fullscreen: return "Fullscreen";
            case GameInputKey.UseQuickItem: return "Use QuickItem";
            case GameInputKey.NextQuickItem: return "Cycle QuickItems";
            case GameInputKey.HorizontalLook: return "HorizontalLook";
            case GameInputKey.Loadout: return "Loadout";
            case GameInputKey.Jump: return "Jump";
            case GameInputKey.Left: return "Left";
            case GameInputKey.NextWeapon: return "Next Weapon / Zoom In";
            case GameInputKey.None: return "None";
            case GameInputKey.Pause: return "Pause";
            case GameInputKey.PrevWeapon: return "Prev Weapon / Zoom Out";
            case GameInputKey.PrimaryFire: return "Primary Fire";
            case GameInputKey.QuickItem1: return "Quick Item 1";
            case GameInputKey.QuickItem2: return "Quick Item 2";
            case GameInputKey.QuickItem3: return "Quick Item 3";
            case GameInputKey.Right: return "Right";
            case GameInputKey.SecondaryFire: return "Secondary Fire";
            case GameInputKey.Tabscreen: return "Tabscreen";
            case GameInputKey.VerticalLook: return "VerticalLook";
            case GameInputKey.Weapon1: return "Primary Weapon";
            case GameInputKey.Weapon2: return "Secondary Weapon";
            case GameInputKey.Weapon3: return "Tertiary Weapon";
            case GameInputKey.Weapon4: return "Pickup Weapon";
            case GameInputKey.WeaponMelee: return "Melee Weapon";
            default: return "No Name";
        }
    }

    #region private

    private void ResolveMultipleAssignment(UserInputMap map)
    {
        foreach (UserInputMap km in _keyMapping.Values)
        {
            if (km != map && km.Channel != null && km.Channel.ChannelType == map.Channel.ChannelType && map.Assignment == km.Assignment)
            {
                km.Channel = null;
                break;
            }
        }
    }

    private bool IsChannelTaken(IInputChannel channel)
    {
        bool result = false;

        foreach (UserInputMap km in _keyMapping.Values)
        {
            if (km.Channel.Equals(channel))
            {
                result = true;
                break;
            }
        }

        return result;
    }

    private void SetDefaultKeyMapping()
    {
        _keyMapping[(int)GameInputKey.HorizontalLook] = new UserInputMap(GetSlotName(GameInputKey.HorizontalLook), GameInputKey.HorizontalLook, new AxisInputChannel("Mouse X", 0), false, false);
        _keyMapping[(int)GameInputKey.VerticalLook] = new UserInputMap(GetSlotName(GameInputKey.VerticalLook), GameInputKey.VerticalLook, new AxisInputChannel("Mouse Y", 1), false, false);

        _keyMapping[(int)GameInputKey.Pause] = new UserInputMap(GetSlotName(GameInputKey.Pause), GameInputKey.Pause, new KeyInputChannel(KeyCode.Backspace), true, true);

        _keyMapping[(int)GameInputKey.Tabscreen] = new UserInputMap(GetSlotName(GameInputKey.Tabscreen), GameInputKey.Tabscreen, new KeyInputChannel(KeyCode.Tab));
        _keyMapping[(int)GameInputKey.Fullscreen] = new UserInputMap(GetSlotName(GameInputKey.Fullscreen), GameInputKey.Fullscreen, new KeyInputChannel(KeyCode.F), true, true, KeyCode.LeftAlt);

        _keyMapping[(int)GameInputKey.Forward] = new UserInputMap(GetSlotName(GameInputKey.Forward), GameInputKey.Forward, new KeyInputChannel(KeyCode.W));
        _keyMapping[(int)GameInputKey.Left] = new UserInputMap(GetSlotName(GameInputKey.Left), GameInputKey.Left, new KeyInputChannel(KeyCode.A));
        _keyMapping[(int)GameInputKey.Backward] = new UserInputMap(GetSlotName(GameInputKey.Backward), GameInputKey.Backward, new KeyInputChannel(KeyCode.S));
        _keyMapping[(int)GameInputKey.Right] = new UserInputMap(GetSlotName(GameInputKey.Right), GameInputKey.Right, new KeyInputChannel(KeyCode.D));
        _keyMapping[(int)GameInputKey.Jump] = new UserInputMap(GetSlotName(GameInputKey.Jump), GameInputKey.Jump, new KeyInputChannel(KeyCode.Space));
        _keyMapping[(int)GameInputKey.Crouch] = new UserInputMap(GetSlotName(GameInputKey.Crouch), GameInputKey.Crouch, new KeyInputChannel(KeyCode.LeftShift));

        _keyMapping[(int)GameInputKey.PrimaryFire] = new UserInputMap(GetSlotName(GameInputKey.PrimaryFire), GameInputKey.PrimaryFire, new MouseInputChannel(0));
        _keyMapping[(int)GameInputKey.SecondaryFire] = new UserInputMap(GetSlotName(GameInputKey.SecondaryFire), GameInputKey.SecondaryFire, new MouseInputChannel(1));

        _keyMapping[(int)GameInputKey.NextWeapon] = new UserInputMap(GetSlotName(GameInputKey.NextWeapon), GameInputKey.NextWeapon, new AxisInputChannel("Mouse ScrollWheel", _mouseScrollThreshold, AxisInputChannel.AxisReadingMethod.PositiveOnly));
        _keyMapping[(int)GameInputKey.PrevWeapon] = new UserInputMap(GetSlotName(GameInputKey.PrevWeapon), GameInputKey.PrevWeapon, new AxisInputChannel("Mouse ScrollWheel", _mouseScrollThreshold, AxisInputChannel.AxisReadingMethod.NegativeOnly));

        _keyMapping[(int)GameInputKey.WeaponMelee] = new UserInputMap(GetSlotName(GameInputKey.WeaponMelee), GameInputKey.WeaponMelee, new KeyInputChannel(KeyCode.Alpha1));
        _keyMapping[(int)GameInputKey.Weapon1] = new UserInputMap(GetSlotName(GameInputKey.Weapon1), GameInputKey.Weapon1, new KeyInputChannel(KeyCode.Alpha2));
        _keyMapping[(int)GameInputKey.Weapon2] = new UserInputMap(GetSlotName(GameInputKey.Weapon2), GameInputKey.Weapon2, new KeyInputChannel(KeyCode.Alpha3));
        _keyMapping[(int)GameInputKey.Weapon3] = new UserInputMap(GetSlotName(GameInputKey.Weapon3), GameInputKey.Weapon3, new KeyInputChannel(KeyCode.Alpha4));
        _keyMapping[(int)GameInputKey.Weapon4] = new UserInputMap(GetSlotName(GameInputKey.Weapon4), GameInputKey.Weapon4, new KeyInputChannel(KeyCode.Alpha5));

        _keyMapping[(int)GameInputKey.QuickItem1] = new UserInputMap(GetSlotName(GameInputKey.QuickItem1), GameInputKey.QuickItem1, new KeyInputChannel(KeyCode.Alpha6));
        _keyMapping[(int)GameInputKey.QuickItem2] = new UserInputMap(GetSlotName(GameInputKey.QuickItem2), GameInputKey.QuickItem2, new KeyInputChannel(KeyCode.Alpha7));
        _keyMapping[(int)GameInputKey.QuickItem3] = new UserInputMap(GetSlotName(GameInputKey.QuickItem3), GameInputKey.QuickItem3, new KeyInputChannel(KeyCode.Alpha8));

        _keyMapping[(int)GameInputKey.ChangeTeam] = new UserInputMap(GetSlotName(GameInputKey.ChangeTeam), GameInputKey.ChangeTeam, new KeyInputChannel(KeyCode.M), true, true, KeyCode.LeftAlt);

        _keyMapping[(int)GameInputKey.UseQuickItem] = new UserInputMap(GetSlotName(GameInputKey.UseQuickItem), GameInputKey.UseQuickItem, new KeyInputChannel(KeyCode.E));
        _keyMapping[(int)GameInputKey.NextQuickItem] = new UserInputMap(GetSlotName(GameInputKey.NextQuickItem), GameInputKey.NextQuickItem, new KeyInputChannel(KeyCode.R));
    }

    private static CmunePrefs.Key GetPrefsKeyForSlot(int slot)
    {
        switch (slot)
        {
            case (int)GameInputKey.Backward: return CmunePrefs.Key.Keymap_Backward;
            case (int)GameInputKey.Chat: return CmunePrefs.Key.Keymap_Chat;
            case (int)GameInputKey.Crouch: return CmunePrefs.Key.Keymap_Crouch;
            case (int)GameInputKey.ChangeTeam: return CmunePrefs.Key.Keymap_ChangeTeam;
            case (int)GameInputKey.Forward: return CmunePrefs.Key.Keymap_Forward;
            case (int)GameInputKey.Fullscreen: return CmunePrefs.Key.Keymap_Fullscreen;
            case (int)GameInputKey.UseQuickItem: return CmunePrefs.Key.Keymap_UseQuickItem;
            case (int)GameInputKey.NextQuickItem: return CmunePrefs.Key.Keymap_NextQuickItem;
            case (int)GameInputKey.HorizontalLook: return CmunePrefs.Key.Keymap_HorizontalLook;
            case (int)GameInputKey.Loadout: return CmunePrefs.Key.Keymap_Inventory;
            case (int)GameInputKey.Jump: return CmunePrefs.Key.Keymap_Jump;
            case (int)GameInputKey.Left: return CmunePrefs.Key.Keymap_Left;
            case (int)GameInputKey.NextWeapon: return CmunePrefs.Key.Keymap_NextWeapon;
            case (int)GameInputKey.None: return CmunePrefs.Key.Keymap_None;
            case (int)GameInputKey.Pause: return CmunePrefs.Key.Keymap_Pause;
            case (int)GameInputKey.PrevWeapon: return CmunePrefs.Key.Keymap_PrevWeapon;
            case (int)GameInputKey.PrimaryFire: return CmunePrefs.Key.Keymap_PrimaryFire;
            case (int)GameInputKey.QuickItem1: return CmunePrefs.Key.Keymap_QuickItem1;
            case (int)GameInputKey.QuickItem2: return CmunePrefs.Key.Keymap_QuickItem2;
            case (int)GameInputKey.QuickItem3: return CmunePrefs.Key.Keymap_QuickItem3;
            case (int)GameInputKey.Right: return CmunePrefs.Key.Keymap_Right;
            case (int)GameInputKey.SecondaryFire: return CmunePrefs.Key.Keymap_SecondaryFire;
            case (int)GameInputKey.Tabscreen: return CmunePrefs.Key.Keymap_Tabscreen;
            case (int)GameInputKey.VerticalLook: return CmunePrefs.Key.Keymap_VerticalLook;
            case (int)GameInputKey.Weapon1: return CmunePrefs.Key.Keymap_Weapon1;
            case (int)GameInputKey.Weapon2: return CmunePrefs.Key.Keymap_Weapon2;
            case (int)GameInputKey.Weapon3: return CmunePrefs.Key.Keymap_Weapon3;
            case (int)GameInputKey.Weapon4: return CmunePrefs.Key.Keymap_Weapon4;
            case (int)GameInputKey.WeaponMelee: return CmunePrefs.Key.Keymap_WeaponMelee;
            default: return CmunePrefs.Key.Keymap_None;
        }
    }

    private void WriteAllKeyMappings()
    {
        _unassignedKeyMappings = false;

        foreach (KeyValuePair<int, UserInputMap> k in _keyMapping)
        {
            if (!k.Value.IsConfigurable) continue;

            string val = k.Value.GetPrefString();
            CmunePrefs.WriteKey(GetPrefsKeyForSlot(k.Key), val);

            if (k.Value.Channel == null) _unassignedKeyMappings = true;
        }
    }

    public void ReadAllKeyMappings()
    {
        _unassignedKeyMappings = false;

        foreach (KeyValuePair<int, UserInputMap> k in _keyMapping)
        {
            if (!k.Value.IsConfigurable) continue;

            string val;
            if (CmunePrefs.TryGetKey(GetPrefsKeyForSlot(k.Key), out val))
            {
                k.Value.FromPrefString(val);

                if (k.Value.Channel == null) _unassignedKeyMappings = true;
            }
        }
    }

    #endregion

    #region Properties

    public bool IsGamepadEnabled
    {
        get { return _isGamepadEnabled; }
        set
        {
            _isGamepadEnabled = value;

            if (_isGamepadEnabled)
            {
                KeyMapping[(int)GameInputKey.HorizontalLook].Channel = new AxisInputChannel("GamePadHorizontal2", 0);
                KeyMapping[(int)GameInputKey.VerticalLook].Channel = new AxisInputChannel("GamePadVertical2", 0);
            }
            else
            {
                KeyMapping[(int)GameInputKey.HorizontalLook].Channel = new AxisInputChannel("Mouse X", 0);
                KeyMapping[(int)GameInputKey.VerticalLook].Channel = new AxisInputChannel("Mouse Y", 0);
            }
        }
    }

    public Dictionary<int, UserInputMap> KeyMapping
    {
        get { return _keyMapping; }
    }

    public bool IsAnyDown
    {
        get
        {
            if (IsInputEnabled)
            {
                foreach (UserInputMap kmap in _keyMapping.Values)
                    if (kmap.Value != 0) return true;
            }

            return false;
        }
    }

    public bool IsInputEnabled
    {
        get { return _inputEnabled; }
        set
        {
            _inputEnabled = value;

            //when turning input OFF
            if (!_inputEnabled)
            {
                foreach (UserInputMap m in _keyMapping.Values)
                {
                    if (m != null && m.Channel != null)
                    {
                        m.Channel.Reset();
                        if (m.IsEventSender && m.Channel.IsChanged)
                        {
                            CmuneEventHandler.Route(new InputChangeEvent(m.Slot, m.Channel.Value));
                        }
                    }
                }
            }
        }
    }

    public bool IsSettingKeymap
    {
        get;
        private set;
    }

    public bool HasUnassignedKeyMappings
    {
        get { return _unassignedKeyMappings; }
    }

    public string InputChannelForSlot(GameInputKey keySlot)
    {
        UserInputMap map;
        if (KeyMapping.TryGetValue((int)keySlot, out map))
        {
            return map.Assignment;
        }
        else
        {
            return "None";
        }
    }

    #endregion

    #region Fields
    private bool _inputEnabled = false;
    private bool _unassignedKeyMappings = false;
    private Dictionary<int, UserInputMap> _keyMapping = new Dictionary<int, UserInputMap>();
    private bool _isGamepadEnabled = false;
    private float _mouseScrollThreshold;
    #endregion
}

public class InputAssignmentEvent
{

}

public class InputChangeEvent
{
    public InputChangeEvent(GameInputKey key, float value)
    {
        _key = key;
        _value = value;
    }

    public GameInputKey Key
    {
        get { return _key; }
    }

    public bool IsDown
    {
        get { return _value != 0; }
    }

    public float Value
    {
        get { return _value; }
    }

    private GameInputKey _key = GameInputKey.None;

    private float _value = 0;
}