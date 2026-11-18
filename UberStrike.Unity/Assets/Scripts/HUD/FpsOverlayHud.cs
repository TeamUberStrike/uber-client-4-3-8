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
        if (ApplicationDataManager.IsMobile)
        {
            DrawMobile(fps);
            return;
        }
        // Top-left; clear of the weapon/health HUD which sits along the bottom.
        GUI.Label(new Rect(10 + MobileMenuScale.SafeLeftPixels, 10, 150, 22), "FPS: " + fps.ToString("F1"), _style);
    }

    // Mobile: top center (HP left, ammo right), sized to screen, colored by speed.
    private void DrawMobile(float fps)
    {
        if (_mobileStyle == null)
        {
            _mobileStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _backing = new Texture2D(1, 1);
            _backing.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            _backing.Apply();
        }
        int size = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 28f), 14, 64);
        _mobileStyle.fontSize = size;
        _mobileStyle.normal.textColor = fps >= 50f ? new Color(0.45f, 1f, 0.45f) : fps >= 30f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.35f);

        string text = Mathf.RoundToInt(fps) + " FPS";
        Vector2 sz = _mobileStyle.CalcSize(new GUIContent(text));
        float w = sz.x + size, h = sz.y + size * 0.3f;
        Rect r = new Rect((Screen.width - w) / 2f, Mathf.Max(8f, Screen.height * 0.012f) + (Screen.height - Screen.safeArea.yMax), w, h);
        GUI.DrawTexture(r, _backing);
        GUI.Label(r, text, _mobileStyle);
    }

    private GUIStyle _mobileStyle;
    private Texture2D _backing;
}
