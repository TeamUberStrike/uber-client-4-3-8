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

    #region Private Methods

    void OnEnable()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientLight = ambientLight;
        RenderSettings.skybox = skyBox;

        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        if (LevelCamera.Exists) LevelCamera.Instance.EnableLowPassFilter(false);
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
