using UberStrike.Realtime.Common;
using UnityEngine;

public class RenderSettingsController : MonoBehaviour
{
    #region Fields

    [SerializeField]
    private Color ambientLight;

    [SerializeField]
    private Material skyBox;

    [SerializeField]
    private int fogStart;

    [SerializeField]
    private int fogEnd;

    [SerializeField]
    private Color32 fogColor;

    [SerializeField]
    private int fogUnderWaterStart;

    [SerializeField]
    private int fogUnderWaterEnd;

    [SerializeField]
    private Color32 fogUnderWaterColor;

    [SerializeField]
    private float fogDiveSpeed = 1.0f;

    [SerializeField]
    private float fogSurfaceSpeed = 2.0f;

    #endregion

    // Mirror Steam's RenderSettingsController.Instance entry point so the options
    // panel can hit it when the Post Processing toggle flips. Set in OnEnable, not
    // a full Singleton<> inheritance — the class is referenced from scene YAML and
    // changing its base type would break every map's prefab.
    public static RenderSettingsController Instance { get; private set; }

    #region Private Methods

    void OnEnable()
    {
        Instance = this;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientLight = ambientLight;
        RenderSettings.skybox = skyBox;

        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        if (LevelCamera.Exists) LevelCamera.Instance.EnableLowPassFilter(false);

        // Apply the saved pref on scene enter so a match boots into the right state.
        EnableImageEffects();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    // Scoped toggle: only the PostProcessingRTX image effect component on the
    // main camera. Earlier versions also flipped global QualitySettings
    // (softParticles, realtimeReflectionProbes, anisotropicFiltering), which
    // regressed pickup particles and other project visuals that depend on
    // those being ON by default. We also don't walk every OnRenderImage
    // MonoBehaviour — enabling MobileBloom throws a NullReferenceException
    // in its CheckSupport/OnEnable path on this build.
    public void EnableImageEffects()
    {
        int strength = ApplicationDataManager.ApplicationOptions != null
            ? ApplicationDataManager.ApplicationOptions.VideoPostProcessingStrength
            : 0;
        bool hasEffect = strength > 0;

        var cam = LevelCamera.Exists ? LevelCamera.Instance.MainCamera : Camera.main;
        if (cam == null) return;

        var rtx = cam.GetComponent<PostProcessingRTX>();
        if (hasEffect)
        {
            if (rtx == null) rtx = cam.gameObject.AddComponent<PostProcessingRTX>();
            rtx.Strength = strength / 100f;
            rtx.enabled = true;
        }
        else if (rtx != null)
        {
            rtx.enabled = false;
        }
    }

    void Update()
    {
        if (LevelCamera.Exists)
        {
            if (GameState.HasCurrentPlayer && GameState.LocalCharacter.Is(PlayerStates.DIVING) && !PlayerSpectatorControl.Instance.IsEnabled)
            {
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogUnderWaterColor, Time.deltaTime * fogDiveSpeed);
                RenderSettings.fogStartDistance = Mathfx.Lerp(RenderSettings.fogStartDistance, fogUnderWaterStart, Time.deltaTime * fogDiveSpeed);
                RenderSettings.fogEndDistance = Mathfx.Lerp(RenderSettings.fogEndDistance, fogUnderWaterEnd, Time.deltaTime * fogDiveSpeed);

                if (!LevelCamera.Instance.LowpassFilterEnabled)
                    LevelCamera.Instance.EnableLowPassFilter(true);
            }
            else
            {
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogColor, Time.deltaTime * fogSurfaceSpeed);
                RenderSettings.fogStartDistance = Mathfx.Lerp(RenderSettings.fogStartDistance, fogStart, Time.deltaTime * fogSurfaceSpeed);
                RenderSettings.fogEndDistance = Mathfx.Lerp(RenderSettings.fogEndDistance, fogEnd, Time.deltaTime * fogSurfaceSpeed);

                if (LevelCamera.Instance.LowpassFilterEnabled)
                    LevelCamera.Instance.EnableLowPassFilter(false);
            }
        }
    }

    #endregion
}
