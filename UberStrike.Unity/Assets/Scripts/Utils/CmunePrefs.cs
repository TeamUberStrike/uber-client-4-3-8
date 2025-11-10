using Cmune.Util;
using UnityEngine;

public static class CmunePrefs
{
    public enum Key
    {
        //MenuGui_Advanced = 0,
        //MenuGui_ShowGeneralHelpscreen = 1,
        //MenuGui_ShowItemHelpscreen = 2,

        Player_Name = 50,
        Player_Email = 51,
        Player_Password = 52,
        //Player_ItemsEquipped = 53,
        Player_AutoLogin = 54,

        Options_VideoIsUsingCustom = 95,
        Options_VideoMaxQueuedFrames = 96,
        Options_VideoTextureQuality = 97,
        Options_VideoVSyncCount = 98,
        Options_VideoAntiAliasing = 99,
        Options_GeneralTargetFrameRate = 100,
        Options_VideoWaterMode = 101,
        Options_VideoCurrentQualityLevel = 102,
        Options_VideoAdvancedWater = 103,
        Options_VideoBloomAndFlares = 104,
        Options_VideoColorCorrection = 105,
        Options_VideoMotionBlur = 106,

        Options_InputXMouseSensitivity = 107,
        Options_InputYMouseSensitivity = 108,
        Options_InputMouseRotationMaxX = 109,
        Options_InputMouseRotationMaxY = 110,
        Options_InputMouseRotationMinX = 111,
        Options_InputMouseRotationMinY = 112,
        Options_InputInvertMouse = 113,
        Options_GameplayAutoPickupEnabled = 114,
        Options_GameplayAutoEquipEnabled = 115,
        Options_GameplayRagdollEnabled = 116,
        Options_InputEnableGamepad = 117,

        Options_AudioEnabled = 120,
        Options_AudioEffectsVolume = 121,
        Options_AudioMusicVolume = 122,
        Options_AudioMasterVolume = 123,
        Options_VideoHardcoreMode = 124,
        Options_VideoScreenRes = 125,
        Options_VideoIsFullscreen = 126,

        Keymap_None = 300,
        Keymap_HorizontalLook = 301,
        Keymap_VerticalLook = 302,
        Keymap_Forward = 303,
        Keymap_Backward = 304,
        Keymap_Left = 305,
        Keymap_Right = 306,
        Keymap_Jump = 307,
        Keymap_Crouch = 308,
        Keymap_PrimaryFire = 309,
        Keymap_SecondaryFire = 310,
        Keymap_Weapon1 = 311,
        Keymap_Weapon2 = 312,
        Keymap_Weapon3 = 313,
        Keymap_Weapon4 = 314,
        Keymap_WeaponMelee = 315,
        Keymap_QuickItem1 = 316,
        Keymap_QuickItem2 = 317,
        Keymap_QuickItem3 = 318,
        Keymap_NextWeapon = 319,
        Keymap_PrevWeapon = 320,
        Keymap_Pause = 321,
        Keymap_Fullscreen = 322,
        Keymap_Tabscreen = 323,
        Keymap_Chat = 324,
        Keymap_Inventory = 325,
        Keymap_UseQuickItem = 326,
        Keymap_ChangeTeam = 327,
        Keymap_NextQuickItem = 328,

        Shop_RecentlyUsedItems = 400,

        App_ClientRegistered = 500,
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteAll();
    }

    public static bool TryGetKey<T>(Key k, out T value)
    {
        if (PlayerPrefs.HasKey(k.ToString()))
        {
            value = ReadKey(k, default(T));
            return true;
        }
        else
        {
            value = default(T);
            return false;
        }
    }

    public static bool HasKey(Key k)
    {
        return PlayerPrefs.HasKey(k.ToString());
    }

    public static T ReadKey<T>(Key k, T defaultValue)
    {
        T ret = defaultValue;

        if (typeof(T) == typeof(bool))
            ret = (T)(object)(PlayerPrefs.GetInt(k.ToString(), (bool)(object)defaultValue ? 1 : 0) == 1);
        else if (typeof(T) == typeof(int))
            ret = (T)(object)PlayerPrefs.GetInt(k.ToString(), (int)(object)defaultValue);
        else if (typeof(T) == typeof(float))
            ret = (T)(object)PlayerPrefs.GetFloat(k.ToString(), (float)(object)defaultValue);
        else if (typeof(T) == typeof(string))
            ret = (T)(object)PlayerPrefs.GetString(k.ToString(), (string)(object)defaultValue);
        else
            CmuneDebug.LogError("Key {0} couldn't be read because type {1} not supported.", k.ToString(), typeof(T).ToString());

        return ret;
    }

    public static T ReadKey<T>(Key k)
    {
        return ReadKey<T>(k, default(T));
    }

    public static void WriteKey<T>(Key k, T val)
    {
        if (typeof(T) == typeof(bool))
            PlayerPrefs.SetInt(k.ToString(), ((bool)(object)val) ? 1 : 0);
        else if (typeof(T) == typeof(int))
            PlayerPrefs.SetInt(k.ToString(), (int)(object)val);
        else if (typeof(T) == typeof(float))
            PlayerPrefs.SetFloat(k.ToString(), (float)(object)val);
        else if (typeof(T) == typeof(string))
            PlayerPrefs.SetString(k.ToString(), (string)(object)val);
        else
            CmuneDebug.LogError("Key {0} couldn't be read because type {1} not supported.", k.ToString(), typeof(T).ToString());
    }
}
