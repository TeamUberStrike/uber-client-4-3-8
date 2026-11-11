using UnityEngine;

// Small always-on FPS counter that renders only when the user enables the
// "Show FPS" toggle in Options → Video. Mirrors the number shown next to the
// OK button inside the options panel, but visible during matches.
// Boots itself via RuntimeInitializeOnLoadMethod so nothing in the scene has
// to wire it up; one DontDestroyOnLoad host lives for the session.
public class FpsOverlayHud : MonoBehaviour
{
    private static FpsOverlayHud _instance;
    private float _smoothDelta = 0.016f;
    private GUIStyle _style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("FpsOverlayHud");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<FpsOverlayHud>();
    }

    private void Update()
    {
        // Time.smoothDeltaTime is already smoothed, but sampling it every frame
        // is cheap and keeps the reading steady even when OnGUI skips frames.
        _smoothDelta = Time.smoothDeltaTime;
    }

    private void OnGUI()
    {
        if (ApplicationDataManager.ApplicationOptions == null) return;
        if (!ApplicationDataManager.ApplicationOptions.VideoShowFps) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = 14;
            _style.fontStyle = FontStyle.Bold;
            _style.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
        }

        float fps = _smoothDelta > 0f ? 1f / _smoothDelta : 0f;
        // Top-left; clear of the weapon/health HUD which sits along the bottom.
        GUI.Label(new Rect(10, 10, 150, 22), "FPS: " + fps.ToString("F1"), _style);
    }
}
