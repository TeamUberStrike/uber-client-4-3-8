using UnityEngine;

public class ApplicationOptions
{
    // General
    public int GeneralTargetFrameRate = 200;

    // Video
    public bool IsUsingCustom = false;
    public int VideoQualityLevel = 1;
    public int VideoMaxQueuedFrames = 10;   // default: slider maxed (per request)
    public int VideoTextureQuality = 0;     // globalTextureMipmapLimit; 0 = full-res = Texture Quality slider 5 (best)
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

    // Touch / mobile input (used by the on-screen touch controls). Persisted via PlayerPrefs
    // directly (below) so we don't have to extend the CmunePrefs.Key enum.
    public bool UseMultiTouch = false;
    public float TouchLookSensitivity = 1.5f;
    public float TouchMoveSensitivity = 1.0f;

    // Gyroscope aim — mobile, SCOPE-ONLY (only drives the view while a weapon is scoped). High-end
    // "game" gyro: integrates the device's angular velocity into the look. Strength is a sensitivity
    // multiplier (1 ≈ 1:1 device→view rotation, scaled by the scope's zoom); InvertY flips the vertical
    // axis. PlayerPrefs-backed like the other touch fields.
    public bool GyroAimEnabled = false;
    public float GyroStrength = 1.5f;
    public bool GyroInvertY = false;
    public bool GyroInvertX = false;

    // Interface — opt-in "classic" lobby HUD (4.3.10.1-style Play/Shop + Profile/Inbox/Clans/Options ring).
    // Default OFF so production keeps the current lobby unless the player switches it in Options.
    // PlayerPrefs-backed (like the touch fields) so we don't extend the CmunePrefs.Key enum.
    public bool UseClassicLobby = false;

    // Gameplay
    public bool GameplayAutoPickupEnabled = true;
    public bool GameplayAutoEquipEnabled = false;

    // Camera
    public float CameraFovMax = 65;
    public float CameraFovMin = 5;

    // Field of View — user-adjustable main camera FOV (Options → Video)
    public const float VideoFOVMin = 60f;
    public const float VideoFOVMax = 110f;
    public float VideoFOV = 75f;

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
            QualitySettings.SetQualityLevel(1, true);
            PlayerPrefs.SetString("Version", ApplicationDataManager.VersionShort);
        }

        // General
        GeneralTargetFrameRate = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GeneralTargetFrameRate, 200);

        // Video
        IsUsingCustom = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoIsUsingCustom, IsUsingCustom);
        VideoWaterMode = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoWaterMode, VideoWaterMode);

        // Water4 High currently not supported on OSX, force to medium
        if ((Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WebGLPlayer) && VideoWaterMode == 2) VideoWaterMode = 1;

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

        VideoFOV = Mathf.Clamp(CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoFOV, VideoFOV), VideoFOVMin, VideoFOVMax);
        VideoShowFps = CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoShowFps, VideoShowFps);
        VideoPostProcessingStrength = Mathf.Clamp(
            CmunePrefs.ReadKey(CmunePrefs.Key.Options_VideoPostProcessingStrength, VideoPostProcessingStrength),
            0, 100);

        // Input
        InputXMouseSensitivity = Mathf.Clamp(CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputXMouseSensitivity, 3.0f), 1.0f, 10.0f);
        InputYMouseSensitivity = Mathf.Clamp(CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputYMouseSensitivity, 3.0f), 1.0f, 10.0f);
        InputMouseRotationMaxX = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMaxX, 360f);
        InputMouseRotationMaxY = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMaxY, 90f);
        InputMouseRotationMinX = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMinX, -360f);
        InputMouseRotationMinY = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputMouseRotationMinY, -90f);
        InputInvertMouse = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputInvertMouse, false);

        // Touch input (PlayerPrefs-backed; see fields above)
        UseMultiTouch = PlayerPrefs.GetInt("Options_UseMultiTouch", 0) != 0;
        TouchLookSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("Options_TouchLookSensitivity", 1.5f), 0.5f, 3.0f);
        TouchMoveSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("Options_TouchMoveSensitivity", 1.0f), 0.5f, 3.0f);

        // Gyroscope aim (PlayerPrefs-backed)
        GyroAimEnabled = PlayerPrefs.GetInt("Options_GyroAimEnabled", 0) != 0;
        GyroStrength = Mathf.Clamp(PlayerPrefs.GetFloat("Options_GyroStrength", 1.5f), 0.2f, 4.0f);
        GyroInvertY = PlayerPrefs.GetInt("Options_GyroInvertY", 0) != 0;
        GyroInvertX = PlayerPrefs.GetInt("Options_GyroInvertX", 0) != 0;

        // Interface (PlayerPrefs-backed)
        UseClassicLobby = PlayerPrefs.GetInt("Options_UseClassicLobby", 0) != 0;

        bool isGamePadEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_InputEnableGamepad, false);
        InputManager.Instance.IsGamepadEnabled = Input.GetJoystickNames().Length > 0 && isGamePadEnabled;

        // Gameplay
        GameplayAutoPickupEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GameplayAutoPickupEnabled, true);
        GameplayAutoEquipEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_GameplayAutoEquipEnabled, false);

        // Audio
        AudioEnabled = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioEnabled, true);
        AudioEffectsVolume = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioEffectsVolume, 0.7f);
        AudioMusicVolume = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioMusicVolume, 0.3f);
        AudioMasterVolume = CmunePrefs.ReadKey(CmunePrefs.Key.Options_AudioMasterVolume, 0.5f);

        // One-time migration so EXISTING installs also land on the new video defaults (Target FPS 200,
        // Max Queued Frames 10, Texture Quality 5 = full-res) once — then the player's later changes stick.
        // New installs already get these from the field defaults; this covers devices with older saved prefs
        // (which would otherwise keep showing their previous values).
        if (PlayerPrefs.GetInt("Options_VideoDefaults_v2", 0) == 0)
        {
            GeneralTargetFrameRate = 200;
            VideoMaxQueuedFrames = 10;
            VideoTextureQuality = 0;
            PlayerPrefs.SetInt("Options_VideoDefaults_v2", 1);
            SaveApplicationOptions();
        }

        if (isReset) SaveApplicationOptions();
    }

    public void SaveApplicationOptions()
    {
        // General
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

        // Touch input (PlayerPrefs-backed)
        PlayerPrefs.SetInt("Options_UseMultiTouch", UseMultiTouch ? 1 : 0);
        PlayerPrefs.SetFloat("Options_TouchLookSensitivity", TouchLookSensitivity);
        PlayerPrefs.SetFloat("Options_TouchMoveSensitivity", TouchMoveSensitivity);

        // Gyroscope aim (PlayerPrefs-backed)
        PlayerPrefs.SetInt("Options_GyroAimEnabled", GyroAimEnabled ? 1 : 0);
        PlayerPrefs.SetFloat("Options_GyroStrength", GyroStrength);
        PlayerPrefs.SetInt("Options_GyroInvertY", GyroInvertY ? 1 : 0);
        PlayerPrefs.SetInt("Options_GyroInvertX", GyroInvertX ? 1 : 0);

        // Interface (PlayerPrefs-backed)
        PlayerPrefs.SetInt("Options_UseClassicLobby", UseClassicLobby ? 1 : 0);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMaxX, InputMouseRotationMaxX);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMaxY, InputMouseRotationMaxY);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMinX, InputMouseRotationMinX);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputMouseRotationMinY, InputMouseRotationMinY);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputInvertMouse, InputInvertMouse);
        CmunePrefs.WriteKey(CmunePrefs.Key.Options_InputEnableGamepad, InputManager.Instance.IsGamepadEnabled);

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