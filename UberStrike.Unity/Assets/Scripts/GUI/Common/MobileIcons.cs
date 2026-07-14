using System.Collections.Generic;
using UberStrike.Core.Types;
using UnityEngine;

/// <summary>
/// Code-only replacement for the mobile branch's MobileIcons. The original loaded every icon
/// from Inspector-assigned serialized fields on a scene/prefab object; this version loads them
/// from <c>Resources/TouchControls</c> in Awake, so the whole touch system can be created from
/// code (see <see cref="MobileControlsBootstrap"/>) with no scene wiring.
///
/// Static accessors guard with <see cref="MonoSingleton{T}.Exists"/> (which does NOT throw) so a
/// missing instance returns null rather than the NullReferenceException that <c>Instance</c> raises.
/// </summary>
public class MobileIcons : MonoSingleton<MobileIcons>
{
    private const string Dir = "TouchControls/";

    private Texture _fireIcon;
    private Texture _jumpIcon;
    private Texture _crouchIcon;
    private Texture _secondFireIcon;
    private Texture _keyboardDpad;
    private Texture _sniperSwipeIcon;
    private Texture _chatIcon;
    private Texture _menuIcon;
    private Texture _scoreboardIcon;
    private Texture _leftIcon;
    private Texture _rightIcon;
    private Texture _joystickInner;
    private Texture _joystickOuter;
    private Texture2D[] _weaponIcons;

    public static Texture FireIcon { get { return Exists ? Instance._fireIcon : null; } }
    public static Texture JumpIcon { get { return Exists ? Instance._jumpIcon : null; } }
    public static Texture CrouchIcon { get { return Exists ? Instance._crouchIcon : null; } }
    public static Texture SecondFireIcon { get { return Exists ? Instance._secondFireIcon : null; } }
    public static Texture KeyboardDpad { get { return Exists ? Instance._keyboardDpad : null; } }
    public static Texture SniperSwipeIcon { get { return Exists ? Instance._sniperSwipeIcon : null; } }
    public static Texture ChatIcon { get { return Exists ? Instance._chatIcon : null; } }
    public static Texture MenuIcon { get { return Exists ? Instance._menuIcon : null; } }
    public static Texture ScoreboardIcon { get { return Exists ? Instance._scoreboardIcon : null; } }
    public static Texture LeftIcon { get { return Exists ? Instance._leftIcon : null; } }
    public static Texture RightIcon { get { return Exists ? Instance._rightIcon : null; } }
    public static Texture JoystickInner { get { return Exists ? Instance._joystickInner : null; } }
    public static Texture JoystickOuter { get { return Exists ? Instance._joystickOuter : null; } }
    public static Texture2D[] WeaponIcons { get { return Exists ? Instance._weaponIcons : null; } }

    private void Awake()
    {
        _fireIcon = Load("touch_fire_button");
        _jumpIcon = Load("touch_jump_button");
        _crouchIcon = Load("touch_crouch_button");
        _secondFireIcon = Load("touch_second_fire_button");
        _keyboardDpad = Load("touch_keyboard_dpad");
        _sniperSwipeIcon = Load("touch_zoom_scrollbar");
        _chatIcon = Load("touch_chat_button");
        _menuIcon = Load("touch_menu_button");
        _scoreboardIcon = Load("touch_scoreboard_button");
        _leftIcon = Load("touch_arrow_left");
        _rightIcon = Load("touch_arrow_right");
        _joystickInner = Load("touch_move_inner");
        _joystickOuter = Load("touch_move_outer");

        BuildWeaponIcons();
    }

    private static Texture2D Load(string name)
    {
        Texture2D tex = Resources.Load<Texture2D>(Dir + name);
        if (tex == null)
            Debug.LogWarning("[MobileIcons] Missing touch icon: Resources/" + Dir + name);
        return tex;
    }

    // The weapon changer indexes its icon array by (int)UberstrikeItemClass, so we build a dense
    // array large enough to hold the highest weapon-class value and fill every slot (defaulting to
    // the handgun icon) to avoid null textures / index-out-of-range at runtime.
    private void BuildWeaponIcons()
    {
        var map = new Dictionary<UberstrikeItemClass, string>
        {
            { UberstrikeItemClass.WeaponMelee, "touch_weapon_melee" },
            { UberstrikeItemClass.WeaponHandgun, "touch_weapon_handgun" },
            { UberstrikeItemClass.WeaponMachinegun, "touch_weapon_machinegun" },
            { UberstrikeItemClass.WeaponShotgun, "touch_weapon_shotgun" },
            { UberstrikeItemClass.WeaponSniperRifle, "touch_weapon_sniperrifle" },
            { UberstrikeItemClass.WeaponSplattergun, "touch_weapon_splattergun" },
            { UberstrikeItemClass.WeaponLauncher, "touch_weapon_launcher" },
            { UberstrikeItemClass.WeaponCannon, "touch_weapon_cannon" },
        };

        int max = 0;
        foreach (var key in map.Keys)
            max = Mathf.Max(max, (int)key);

        Texture2D fallback = Load("touch_weapon_handgun");
        _weaponIcons = new Texture2D[max + 1];
        for (int i = 0; i < _weaponIcons.Length; i++)
            _weaponIcons[i] = fallback;

        foreach (var kv in map)
        {
            Texture2D tex = Load(kv.Value);
            if (tex != null)
                _weaponIcons[(int)kv.Key] = tex;
        }
    }
}
