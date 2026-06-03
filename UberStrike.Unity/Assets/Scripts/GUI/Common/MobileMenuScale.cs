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

    public static float Scale
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

    /// <summary>Screen width in the scaled coordinate space — anchor menu rects against this.</summary>
    public static float VirtualWidth { get { return Screen.width / Scale; } }

    /// <summary>Screen height in the scaled coordinate space — anchor menu rects against this.</summary>
    public static float VirtualHeight { get { return Screen.height / Scale; } }

    /// <summary>
    /// Begin a scaled menu OnGUI block. Returns the previous matrix to restore with <see cref="End"/>.
    /// Scales about the top-left origin, composing with any existing matrix.
    /// </summary>
    public static Matrix4x4 Begin()
    {
        Matrix4x4 prev = GUI.matrix;
        float s = Scale;
        if (s != 1f)
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f)) * prev;
        return prev;
    }

    public static void End(Matrix4x4 prev)
    {
        GUI.matrix = prev;
    }
}
