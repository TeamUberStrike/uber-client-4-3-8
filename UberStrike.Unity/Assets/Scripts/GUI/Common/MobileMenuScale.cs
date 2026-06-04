using UnityEngine;

/// <summary>
/// Mobile-only uniform scale for the menu / lobby IMGUI.
///
/// The menu (ribbon, pages, panels, popups) was authored at fixed desktop pixel sizes, so on a
/// high-DPI phone it renders physically tiny. The in-game HUD does NOT have this problem because it
/// sizes everything as a fraction of <c>Screen.height</c>; this helper brings the same idea to the
/// menu by applying a <see cref="GUI.matrix"/> scale at the top of each menu <c>OnGUI</c> so the menu
/// keeps a consistent on-screen size relative to the screen height.
///
/// Because each <c>OnGUI</c> resets <see cref="GUI.matrix"/>, every top-level menu draw wraps its body
/// in <see cref="Begin"/>/<see cref="End"/>, and anchors against <see cref="VirtualWidth"/>/
/// <see cref="VirtualHeight"/> (the screen size in the scaled coordinate space) instead of
/// <c>Screen.width</c>/<c>Screen.height</c>, so right/centre-anchored elements stay put under the scale.
///
/// Desktop is never affected — gated on <see cref="ApplicationDataManager.IsMobile"/>. In the Editor
/// it can be force-previewed (Tools ▸ Mobile ▸ Preview Menu Scale) on a desktop channel.
/// </summary>
public static class MobileMenuScale
{
    // The menu's effective design height. On mobile the menu is kept this "tall" in virtual pixels and
    // scaled up to fill taller screens, so a 1080-tall phone gets ~1.5x, a 1536-tall tablet ~2.1x.
    public const float ReferenceHeight = 720f;
    public const float MaxScale = 2.25f;

#if UNITY_EDITOR
    public const string PreviewPrefKey = "MobileMenuScale.ForcePreviewInEditor";
    public static bool ForcePreviewInEditor
    {
        get { return UnityEditor.EditorPrefs.GetBool(PreviewPrefKey, false); }
        set { UnityEditor.EditorPrefs.SetBool(PreviewPrefKey, value); }
    }
    // Fixed, representative scale for the Editor preview so the effect is visible WITHOUT resizing the
    // Game view (the real device uses the height-based auto-scale below). Tweak to taste.
    public const float EditorPreviewScale = 1.6f;
#endif

    public static bool Active
    {
        get
        {
            if (ApplicationDataManager.IsMobile) return true;
#if UNITY_EDITOR
            if (ForcePreviewInEditor) return true;
#endif
            return false;
        }
    }

    // Per-screen extra multiplier on top of the base height scale. A screen that wants to render bigger
    // than the default menu (e.g. the login panel, the training map list) calls Begin(extra) and the
    // whole block — including the VirtualWidth/Height anchors — scales by base * extra. Reset on End().
    private static float _activeExtra = 1f;

    /// <summary>Base height-derived scale, WITHOUT the per-screen extra multiplier.</summary>
    private static float BaseScale
    {
        get
        {
            if (ApplicationDataManager.IsMobile)
                return Mathf.Clamp(Screen.height / ReferenceHeight, 1f, MaxScale);
#if UNITY_EDITOR
            // Editor force-preview on a desktop channel: use a fixed scale so it's visible regardless
            // of the Game view resolution (which can't always be changed).
            if (ForcePreviewInEditor)
                return EditorPreviewScale;
#endif
            return 1f;
        }
    }

    public static float Scale { get { return BaseScale * _activeExtra; } }

    /// <summary>Screen width in the scaled coordinate space — anchor menu rects against this.</summary>
    public static float VirtualWidth { get { return Screen.width / Scale; } }

    /// <summary>Screen height in the scaled coordinate space — anchor menu rects against this.</summary>
    public static float VirtualHeight { get { return Screen.height / Scale; } }

    // Base tap margin (virtual px) kept between interactive edge elements and the screen edge, so they
    // are not flush to the edge (easier to tap) — on top of the device's notch/home-indicator safe area.
    public const float EdgeMargin = 18f;

    /// <summary>Inset (virtual px) to keep right-anchored interactive elements off the right edge/notch. 0 on desktop.</summary>
    public static float RightInset
    {
        get { return Active ? Mathf.Max(Screen.width - Screen.safeArea.xMax, 0f) / Scale + EdgeMargin : 0f; }
    }

    // Smaller tap margin for the top-ribbon right cluster (OPTIONS / GET CREDITS / build info) so they sit
    // closer to the edge — a tight gap. The notch/safe-area contribution is CAPPED (not added in full) so a
    // device with the notch on the right doesn't push the cluster way in; the cluster sits in the safe band
    // below the status bar, so a small fixed gap clears the rounded corner without a big empty space.
    public const float EdgeMarginTight = 6f;
    public const float RightNotchCap = 10f;   // max virtual px of right safe-area inset to honour here

    /// <summary>Like RightInset but with a tight, notch-capped margin — for the ribbon's right cluster. 0 on desktop.</summary>
    public static float RightInsetTight
    {
        get
        {
            if (!Active) return 0f;
            float notch = Mathf.Max(Screen.width - Screen.safeArea.xMax, 0f) / Scale;
            return Mathf.Min(notch, RightNotchCap) + EdgeMarginTight;
        }
    }

    /// <summary>Inset (virtual px) to keep left-anchored interactive elements off the left edge/notch. 0 on desktop.</summary>
    public static float LeftInset
    {
        get { return Active ? Mathf.Max(Screen.safeArea.x, 0f) / Scale + EdgeMargin : 0f; }
    }

    /// <summary>
    /// Begin a scaled menu OnGUI block. Returns the previous matrix to restore with <see cref="End"/>.
    /// Scales about the top-left origin, composing with any existing matrix.
    /// </summary>
    public static Matrix4x4 Begin()
    {
        return Begin(1f);
    }

    /// <summary>
    /// Begin a scaled menu OnGUI block at base*extra scale. <paramref name="extra"/> > 1 makes this one
    /// screen render bigger than the default menu (login panel, map lists). VirtualWidth/Height honour the
    /// extra for the duration, so rects anchored against them stay correct. Must be paired with End().
    /// </summary>
    public static Matrix4x4 Begin(float extra)
    {
        // The extra multiplier is a mobile-only enlargement; desktop keeps its authored size.
        _activeExtra = Active ? Mathf.Max(0.1f, extra) : 1f;
        Matrix4x4 prev = GUI.matrix;
        float s = Scale;
        if (s != 1f)
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f)) * prev;
        return prev;
    }

    public static void End(Matrix4x4 prev)
    {
        GUI.matrix = prev;
        _activeExtra = 1f;
    }
}
