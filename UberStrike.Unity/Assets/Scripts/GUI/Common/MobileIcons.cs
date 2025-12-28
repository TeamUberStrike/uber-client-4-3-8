using UnityEngine;

public class MobileIcons : MonoSingleton<MobileIcons>
{
    [SerializeField]
    private Texture _fireIcon;
    public static Texture FireIcon { get { return (Instance) ? Instance._fireIcon : null; } }

    [SerializeField]
    private Texture _jumpIcon;
    public static Texture JumpIcon { get { return (Instance) ? Instance._jumpIcon : null; } }

    [SerializeField]
    private Texture _crouchIcon;
    public static Texture CrouchIcon { get { return (Instance) ? Instance._crouchIcon : null; } }

    [SerializeField]
    private Texture _secondFireIcon;
    public static Texture SecondFireIcon { get { return (Instance) ? Instance._secondFireIcon : null; } }

    [SerializeField]
    private Texture _keyboardDpad;
    public static Texture KeyboardDpad { get { return (Instance) ? Instance._keyboardDpad : null; } } 

    [SerializeField]
    private Texture _sniperSwipeIcon;
    public static Texture SniperSwipeIcon { get { return (Instance) ? Instance._sniperSwipeIcon : null; } }

    [SerializeField]
    private Texture _chatIcon;
    public static Texture ChatIcon { get { return (Instance) ? Instance._chatIcon : null; } }

    [SerializeField]
    private Texture _menuIcon;
    public static Texture MenuIcon { get { return (Instance) ? Instance._menuIcon : null; } }

    [SerializeField]
    private Texture _scoreboardIcon;
    public static Texture ScoreboardIcon { get { return (Instance) ? Instance._scoreboardIcon : null; } }

    [SerializeField]
    private Texture _leftIcon;
    public static Texture LeftIcon { get { return (Instance) ? Instance._leftIcon : null; } }

    [SerializeField]
    private Texture _rightIcon;
    public static Texture RightIcon { get { return (Instance) ? Instance._rightIcon : null; } }

    [SerializeField]
    private Texture _joystickInner;
    public static Texture JoystickInner { get { return (Instance) ? Instance._joystickInner : null; } }

    [SerializeField]
    private Texture _joystickOuter;
    public static Texture JoystickOuter { get { return (Instance) ? Instance._joystickOuter : null; } }

    [SerializeField]
    private Texture2D[] _weaponIcons;
    public static Texture2D[] WeaponIcons { get { return (Instance) ? Instance._weaponIcons : null; } }
}
