using UnityEngine;

public class ApplicationOptions
{
    // General
    public bool GeneralLimitFrameRate = false;
    public int GeneralTargetFrameRate = 144;
    public int FrameRateCap { get { return GeneralLimitFrameRate ? GeneralTargetFrameRate : -1; } }

    // Video
    public bool IsUsingCustom = false;
    public int VideoQualityLevel = 1;
    public int VideoMaxQueuedFrames = 1;
    public int VideoTextureQuality = 4;
    public int VideoVSyncCount = 0;
    public int VideoAntiAliasing = 0;
    public int VideoWaterMode = 1;
    public int ScreenResolution = 0;
    public bool IsFullscreen = false;
    public bool VideoBloomAndFlares = false;
    public bool VideoVignetting = false;
    public bool VideoMotionBlur = false;

    // Input
    public float InputXMouseSensitivity = 3;
    public float InputYMouseSensitivity = 3;
    public float InputMouseRotationMaxX = 360;
    public float InputMouseRotationMaxY = 90;
    public float InputMouseRotationMinX = -360;
    public float InputMouseRotationMinY = -90;
    public bool InputInvertMouse = false;

    // Mobile
    public float TouchLookSensitivity = 1.0f;
    public float TouchMoveSensitivity = 1.0f;

    public bool UseMultiTouch = false;

    // Gameplay
    public bool GameplayAutoPickupEnabled = true;
    public bool GameplayAutoEquipEnabled = false;

    // Camera
    public float CameraFovMax = 65;
    public float CameraFovMin = 5;

    // Field of View — user-adjustable main camera FOV (Options → Video)
    public const float VideoFOVMin = 60f;
    public const float VideoFOVMax = 110f;
    public const float VideoFOVNormal = 75f;
    public const float VideoFOVWide = 100f;
    public float VideoFOV = VideoFOVNormal;

    // In-match FPS counter overlay + post-processing (Options → Video).
    // PostProcessing now has a 0-100 strength slider — 0 is off, 100 is full.
    // The old bool is kept for back-compat with stored prefs; `VideoPostProcessing`
    // is derived from `VideoPostProcessingStrength > 0`.
    public bool VideoShowFps = false;
    public int VideoPostProcessingStrength = 0; // default OFF — don't alter project look until user opts in
    public bool VideoPostProcessing { get { return VideoPostProcessingStrength > 0; } set { VideoPostProcessingStrength = value ? 60 : 0; } }

    // Audio
    public bool AudioEnabled = true;
    public float AudioEffectsVolume = 0.7f;
    public float AudioMusicVolume = 0.3f;
    public float AudioMasterVolume = 0.5f;

    public void Initialize()
    {
        string currentVersion = PlayerPrefs.GetString("Version", "Invalid");

        // If our version is not equal the version stored in the player prefs, delete the prefs and start over
        bool isReset = false;
        if (ApplicationDataManager.VersionShort != currentVersion)
        {
            isReset = true;
            CmunePrefs.Reset();
#if !UNITY_ANDROID && !UNITY_IPHONE
            QualitySettings.SetQualityLevel(1, true);
#endif
            PlayerPrefs.SetString("Version", ApplicationDataManager.VersionShort);
        }

        // General
        if (CmunePrefs.ReadKey(CmunePrefs.Key.Options_GeneralFrameRateVersion, 0) < 1)
        {
            // old default 200 -> uncapped
            int old = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GeneralTargetFrameRate, 200);
            bool limit = old > 0 && old != 200;
            CmunePrefs.WriteKey(CmunePrefs.Key.Options_GeneralLimitFrameRate, limit);
            CmunePrefs.WriteKey(CmunePrefs.Key.Options_GeneralTargetFrameRate, limit ? old : GeneralTargetFrameRate);
            CmunePrefs.WriteKey(CmunePrefs.Key.Options_GeneralFrameRateVersion, 1);
        }
        GeneralLimitFrameRate = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GeneralLimitFrameRate, GeneralLimitFrameRate);
        GeneralTargetFrameRate = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GeneralTargetFrameRate, GeneralTargetFrameRate);

        // Video
        IsUsingCustom = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoIsUsingCustom, IsUsingCustom);
        VideoWaterMode = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoWaterMode, VideoWaterMode);

        // Water4 High currently not supported on OSX, force to medium
        if ((Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.WebGLPlayer ||
            Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.Android) && VideoWaterMode == 2)
        {
            VideoWaterMode = 1;
        }

        VideoMaxQueuedFrames = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoMaxQueuedFrames, VideoMaxQueuedFrames);
        VideoTextureQuality = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoTextureQuality, VideoTextureQuality);
        VideoVSyncCount = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoVSyncCount, VideoVSyncCount);
        VideoAntiAliasing = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoAntiAliasing, VideoAntiAliasing);
        VideoQualityLevel = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoCurrentQualityLevel, VideoQualityLevel);
        VideoBloomAndFlares = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoBloomAndFlares, VideoBloomAndFlares);
        VideoVignetting = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoColorCorrection, VideoVignetting);
        VideoMotionBlur = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoMotionBlur, VideoMotionBlur);

        IsFullscreen = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoIsFullscreen, true);
        ScreenResolution = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoScreenRes, ScreenResolutionManager.CurrentResolutionIndex);

        VideoFOV = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoFOV, VideoFOV) >= (VideoFOVNormal + VideoFOVWide) * 0.5f ? VideoFOVWide : VideoFOVNormal;
        VideoShowFps = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoShowFps, VideoShowFps);
        VideoPostProcessingStrength = Mathf.Clamp(
            CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoPostProcessingStrength, VideoPostProcessingStrength),
            0, 100);

        // Input
        InputXMouseSensitivity = Mathf.Clamp(CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputXMouseSensitivity, 3.0f), 0.1f, 10.0f);
        InputYMouseSensitivity = Mathf.Clamp(CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputYMouseSensitivity, 3.0f), 0.1f, 10.0f);
        InputMouseRotationMaxX = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMaxX, 360f);
        InputMouseRotationMaxY = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMaxY, 90f);
        InputMouseRotationMinX = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMinX, -360f);
        InputMouseRotationMinY = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMinY, -90f);
        InputInvertMouse = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputInvertMouse, false);
        bool isGamePadEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputEnableGamepad, false);
        InputManager.Instance.IsGamepadEnabled = Input.GetJoystickNames().Length > 0 && isGamePadEnabled;

#if UNITY_ANDROID || UNITY_IPHONE
        // Mobile
        UseMultiTouch = CmunePrefs.ReadKey(CmunePrefs.Key.Options_MobileUseMultiTouch, false);
        TouchLookSensitivity = Mathf.Clamp(CmunePrefs.ReadKey(CmunePrefs.Key.Options_MobileTouchLookSensitivity, 1.0f), 0.5f, 3.0f);
        TouchMoveSensitivity = Mathf.Clamp(CmunePrefs.ReadKey(CmunePrefs.Key.Options_MobileTouchMoveSensitivity, 1.0f), 0.5f, 3.0f);
#endif

        // Gameplay
        GameplayAutoPickupEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GameplayAutoPickupEnabled, true);
        GameplayAutoEquipEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GameplayAutoEquipEnabled, false);

        // Audio
        AudioEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioEnabled, true);
        AudioEffectsVolume = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioEffectsVolume, 0.7f);
        AudioMusicVolume = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioMusicVolume, 0.3f);
        AudioMasterVolume = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioMasterVolume, 0.5f);

        if (isReset) SaveApplicationOptions();
    }

    public void SaveApplicationOptions()
    {
        // General
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_GeneralLimitFrameRate, GeneralLimitFrameRate);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_GeneralTargetFrameRate, GeneralTargetFrameRate);

        // Video
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoIsUsingCustom, IsUsingCustom);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoMaxQueuedFrames, VideoMaxQueuedFrames);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoTextureQuality, VideoTextureQuality);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoVSyncCount, VideoVSyncCount);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoAntiAliasing, VideoAntiAliasing);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoWaterMode, VideoWaterMode);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoCurrentQualityLevel, VideoQualityLevel);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoBloomAndFlares, VideoBloomAndFlares);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoColorCorrection, VideoVignetting);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoMotionBlur, VideoMotionBlur);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoScreenRes, ScreenResolution);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoIsFullscreen, IsFullscreen);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoFOV, VideoFOV);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoShowFps, VideoShowFps);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_VideoPostProcessingStrength, VideoPostProcessingStrength);

        // Input
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputXMouseSensitivity, InputXMouseSensitivity);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputYMouseSensitivity, InputYMouseSensitivity);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMaxX, InputMouseRotationMaxX);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMaxY, InputMouseRotationMaxY);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMinX, InputMouseRotationMinX);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMinY, InputMouseRotationMinY);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputInvertMouse, InputInvertMouse);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputEnableGamepad, InputManager.Instance.IsGamepadEnabled);

#if UNITY_ANDROID || UNITY_IPHONE
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_MobileUseMultiTouch, UseMultiTouch);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_MobileTouchLookSensitivity, TouchLookSensitivity);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_MobileTouchMoveSensitivity, TouchMoveSensitivity);
#endif

        // Gameplay
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_GameplayAutoPickupEnabled, GameplayAutoPickupEnabled);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_GameplayAutoEquipEnabled, GameplayAutoEquipEnabled);

        // Audio
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_AudioEnabled, AudioEnabled);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_AudioEffectsVolume, AudioEffectsVolume);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_AudioMusicVolume, AudioMusicVolume);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_AudioMasterVolume, AudioMasterVolume);
    }
}