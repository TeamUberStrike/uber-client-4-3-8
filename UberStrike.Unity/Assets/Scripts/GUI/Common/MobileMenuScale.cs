using UnityEngine;
using DeviceApplication = UnityEngine.Device.Application;

/// <summary>
/// Mobile-only uniform scale for the menu / lobby IMGUI (physical-size based, see "Scale model").
///
/// The menu (ribbon, pages, panels, popups) was authored at fixed desktop pixel sizes, so on a
/// high-DPI phone it renders physically tiny. The in-game HUD does NOT have this problem because it
/// sizes everything as a fraction of <c>Screen.height</c>; this helper brings the same idea to the
/// menu by applying a <see cref="GUI.matrix"/> scale at the top of each menu <c>OnGUI</c> so the menu
/// keeps a consistent physical on-screen size across phones and tablets.
///
/// Because each <c>OnGUI</c> resets <see cref="GUI.matrix"/>, every top-level menu draw wraps its body
/// in <see cref="Begin"/>/<see cref="End"/>, and anchors against <see cref="VirtualWidth"/>/
/// <see cref="VirtualHeight"/> (the screen size in the scaled coordinate space) instead of
/// <c>Screen.width</c>/<c>Screen.height</c>, so right/centre-anchored elements stay put under the scale.
///
/// The menu is also laid out inside the display's safe area (see "Safe-area layout"): Begin shifts it clear
/// of the left cutout / curved edge and <see cref="VirtualWidth"/> excludes both side insets, so every menu
/// that uses Begin/VirtualWidth is covered without per-element offsets.
///
/// Desktop is never affected — gated on <see cref="ApplicationDataManager.IsMobile"/> and
/// <c>UnityEngine.Device.Application.isMobilePlatform</c>. In the Editor it can also be force-previewed
/// (Tools ▸ Mobile ▸ Preview Menu Scale) on a desktop channel.
/// </summary>
public static class MobileMenuScale
{
    // ---------------------------------------------------------------------------------------------
    // Scale model (mobile)
    //
    // The menu is sized by PHYSICAL size, so it looks the same on every device: one virtual unit is
    // calibrated to the reference tablet (Lenovo Tab TB311, 10.1" 1920x1200 @ ~224 ppi, where 720
    // virtual units span ~136 mm).
    //
    // Small screens cannot show the authored layout at that physical size, so the scale is capped so
    // that at least MinVirtualHeight x MinVirtualWidth virtual units always remain visible. In
    // practice: every tablet (8"-14") matches the reference tablet; every phone hits the height cap
    // and shows the same 540-unit-tall layout (~60-70% of the tablet's physical size).
    //
    // MinVirtualHeight is therefore the "phone size" knob: lower = bigger UI on phones, but the
    // layout must still fit in that many units.
    // ---------------------------------------------------------------------------------------------

    /// <summary>Virtual units per millimetre on the reference tablet (720 units / ~136 mm).</summary>
    public const float ReferenceUnitsPerMm = 720f / 136f;

    /// <summary>Smallest virtual height the menu layout must always get.</summary>
    public const float MinVirtualHeight = 540f;

    /// <summary>Smallest virtual width the menu layout must always get (4:3 and foldable screens).</summary>
    public const float MinVirtualWidth = 960f;

    /// <summary>Fallback design height, used only when the device reports no usable DPI.</summary>
    public const float ReferenceHeight = 720f;

    /// <summary>Safety upper bound against bogus platform values.</summary>
    public const float MaxScale = 4f;

    // Screen.dpi outside this range is treated as unreported/bogus.
    private const float MinPlausibleDpi = 100f;
    private const float MaxPlausibleDpi = 700f;

#if UNITY_EDITOR
    public const string PreviewPrefKey = "MobileMenuScale.ForcePreviewInEditor";
    public static bool ForcePreviewInEditor
    {
        get { return UnityEditor.EditorPrefs.GetBool(PreviewPrefKey, false); }
        set { UnityEditor.EditorPrefs.SetBool(PreviewPrefKey, value); }
    }
    // Fixed, representative scale for the Editor preview so the effect is visible WITHOUT resizing the
    // Game view (the real device uses the physical-size auto-scale below). Tweak to taste.
    public const float EditorPreviewScale = 1.6f;
#endif

    public static bool Active
    {
        get
        {
            if (ApplicationDataManager.IsMobile) return true;
            // Device Simulator (and real mobile builds) report the simulated platform here even when the
            // Editor configuration channel is a desktop one.
            if (DeviceApplication.isMobilePlatform) return true;
#if UNITY_EDITOR
            if (ForcePreviewInEditor) return true;
#endif
            return false;
        }
    }

    // Per-screen extra multiplier on top of the base device scale. A screen that wants to render bigger
    // than the default menu (e.g. the login panel) calls Begin(extra) and the whole block — including
    // the VirtualWidth/Height anchors — scales by base * extra. Reset on End().
    private static float _activeExtra = 1f;

    /// <summary>Base device scale, WITHOUT the per-screen extra multiplier.</summary>
    private static float BaseScale
    {
        get
        {
            if (ApplicationDataManager.IsMobile || DeviceApplication.isMobilePlatform)
                return DeviceScale;
#if UNITY_EDITOR
            // Editor force-preview on a desktop channel: use a fixed scale so it's visible regardless
            // of the Game view resolution (which can't always be changed).
            if (ForcePreviewInEditor)
                return EditorPreviewScale;
#endif
            return 1f;
        }
    }

    /// <summary>
    /// Physical-size based scale, capped so the layout always fits (see "Scale model" above).
    /// Falls back to the old height-based scale when the device reports no usable DPI.
    /// </summary>
    private static float DeviceScale
    {
        get
        {
            float ppmm = RenderPixelsPerMm;
            float wanted = ppmm > 0f ? ppmm / ReferenceUnitsPerMm : Screen.height / ReferenceHeight;
            float fit = Mathf.Min(Screen.height / MinVirtualHeight, SafeWidthPixels / MinVirtualWidth);
            return Mathf.Clamp(Mathf.Min(wanted, fit), 1f, MaxScale);
        }
    }

    /// <summary>
    /// Rendered pixels per millimetre, or 0 when the device reports no plausible DPI.
    /// Screen.dpi describes the physical panel; when the game renders below native resolution
    /// (resolution scaling / Screen.SetResolution) the rendered pixels are larger, so the density is
    /// reduced by render/native. Orientation-independent (compares the longer sides).
    /// </summary>
    private static float RenderPixelsPerMm
    {
        get
        {
            float dpi = Screen.dpi;
            if (dpi < MinPlausibleDpi || dpi > MaxPlausibleDpi)
                return 0f;

            float renderRatio = 1f;
#if !UNITY_EDITOR
            // In the Editor (Device Simulator) Display.main reports the host monitor, not the device.
            float native = Mathf.Max(Display.main.systemWidth, Display.main.systemHeight);
            float render = Mathf.Max(Screen.width, Screen.height);
            if (native > 0f && render > 0f)
                renderRatio = Mathf.Min(render / native, 1f);
#endif
            return dpi / 25.4f * renderRatio;
        }
    }

    public static float Scale { get { return BaseScale * _activeExtra; } }

    /// <summary>
    /// Width of the menu area (the screen minus the left/right safe-area insets) in the scaled coordinate
    /// space — anchor menu rects against this. Equals Screen.width on PC.
    /// </summary>
    public static float VirtualWidth { get { return SafeWidthPixels / Scale; } }

    /// <summary>Screen height in the scaled coordinate space — anchor menu rects against this.</summary>
    public static float VirtualHeight { get { return Screen.height / Scale; } }

    // ---------------------------------------------------------------------------------------------
    // Safe-area layout (camera cutouts / notch / curved edges)
    //
    // The menu is laid out in the screen area between the left and right insets:
    //   - Begin() shifts the menu right by the left inset, and VirtualWidth excludes both insets, so
    //     menu x = 0 .. VirtualWidth is always clear of cutouts. No per-element offsets are needed.
    //   - Code that needs screen pixels (camera viewports, touch areas) converts with MenuToScreenX /
    //     ScreenToMenu instead of multiplying by Scale itself.
    //
    // Insets come from Screen.safeArea at full size, so a notch is always cleared (iOS reports notch +
    // rounded corners on both sides; Android reports the cutout side only). Both sides get the same
    // inset (OppositeEdgeFactor = 1): the menu stays centred on the screen, does not shift when the
    // phone is flipped, and the side without a cutout is clear of its rounded corners. Devices that
    // report no cutout get no inset and keep the stock layout. The bottom inset (iPhone home
    // indicator) is intentionally not applied.
    //
    // NOTE (Android): Player Settings > Resolution and Presentation > "Render outside safe area" must be
    // ON, otherwise Unity letterboxes the cutout and safeArea always equals the full screen.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Share of the cutout side's inset applied to the opposite side. 1 = uniform insets (menu centred,
    /// no shift when the phone is flipped); lower values give that side more width back.
    /// </summary>
    public const float OppositeEdgeFactor = 1f;

    /// <summary>Upper bound for one side's inset as a fraction of the screen width (bogus platform values).</summary>
    private const float MaxInsetFraction = 0.15f;

    /// <summary>
    /// Rendered pixels per millimetre. Falls back to an estimate (landscape phone ~65 mm tall) when the
    /// platform does not report a plausible DPI.
    /// </summary>
    public static float PixelsPerMm
    {
        get
        {
            float ppmm = RenderPixelsPerMm;
            return ppmm > 0f ? ppmm : Screen.height / 65f;
        }
    }

    // The insets are read many times per OnGUI (every VirtualWidth), so they are computed once per frame.
    private static int _insetsFrame = -1;
    private static int _insetsScreenWidth;
    private static float _insetLeftPixels;
    private static float _insetRightPixels;

    private static void GetInsetsPixels(out float left, out float right)
    {
        if (_insetsFrame != Time.frameCount || _insetsScreenWidth != Screen.width)
        {
            _insetsFrame = Time.frameCount;
            _insetsScreenWidth = Screen.width;
            _insetLeftPixels = 0f;
            _insetRightPixels = 0f;

            if (Active)
            {
                Rect safe = Screen.safeArea;
                float cutoutLeft = Mathf.Max(safe.xMin, 0f);
                float cutoutRight = Mathf.Max(Screen.width - safe.xMax, 0f);
                float minimum = Mathf.Max(cutoutLeft, cutoutRight) * OppositeEdgeFactor;
                float maximum = Screen.width * MaxInsetFraction;
                _insetLeftPixels = Mathf.Min(Mathf.Max(cutoutLeft, minimum), maximum);
                _insetRightPixels = Mathf.Min(Mathf.Max(cutoutRight, minimum), maximum);
            }
        }

        left = _insetLeftPixels;
        right = _insetRightPixels;
    }

    /// <summary>Left safe-area inset in screen pixels. 0 on PC and on devices without a cutout.</summary>
    public static float InsetLeftPixels
    {
        get { float left, right; GetInsetsPixels(out left, out right); return left; }
    }

    /// <summary>Right safe-area inset in screen pixels. 0 on PC and on devices without a cutout.</summary>
    public static float InsetRightPixels
    {
        get { float left, right; GetInsetsPixels(out left, out right); return right; }
    }

    /// <summary>Width of the menu area (screen minus both insets) in screen pixels.</summary>
    private static float SafeWidthPixels
    {
        get { float left, right; GetInsetsPixels(out left, out right); return Screen.width - left - right; }
    }

    /// <summary>
    /// Distance in menu units from the left side of the menu area (x = 0) to the real screen edge.
    /// Only for extending backgrounds; content must NOT add it (Begin already applies the inset).
    /// </summary>
    public static float BleedLeft { get { return InsetLeftPixels / Scale; } }

    /// <summary>Distance in menu units from the right side of the menu area (x = VirtualWidth) to the real screen edge.</summary>
    public static float BleedRight { get { return InsetRightPixels / Scale; } }

    /// <summary>Menu-space x (inside Begin/End) to screen pixels. Identity on PC.</summary>
    public static float MenuToScreenX(float x)
    {
        return InsetLeftPixels + x * Scale;
    }

    /// <summary>
    /// Screen pixels with a top-left origin (e.g. GUIUtility.GUIToScreenPoint) to top-level menu space.
    /// Identity on PC.
    /// </summary>
    public static Vector2 ScreenToMenu(Vector2 screenPoint)
    {
        float s = Scale;
        return new Vector2((screenPoint.x - InsetLeftPixels) / s, screenPoint.y / s);
    }

    /// <summary>
    /// Extra multiplier (on top of <see cref="Scale"/>) that makes an element of <paramref name="virtualSize"/>
    /// at least <paramref name="minMm"/> tall on the physical screen. Returns 1 when mobile scaling is
    /// inactive, so desktop keeps its authored size.
    /// </summary>
    public static float TouchBoost(float virtualSize, float minMm, float maxBoost)
    {
        if (!Active || virtualSize <= 0f)
            return 1f;

        float currentPx = virtualSize * Scale;
        return Mathf.Clamp(minMm * PixelsPerMm / currentPx, 1f, maxBoost);
    }

    /// <summary>
    /// Scales subsequent GUI drawing around <paramref name="pivot"/>, where the pivot is given in the
    /// CURRENT GUI coordinate space (e.g. virtual menu coordinates inside Begin/End).
    /// Unlike GUIUtility.ScaleAroundPivot (which pre-multiplies, i.e. treats the pivot as being in
    /// screen space), this stays correct when GUI.matrix already carries the mobile menu scale.
    /// Save GUI.matrix before calling and restore it afterwards.
    /// </summary>
    public static void ScaleAroundLocalPivot(Vector2 scale, Vector2 pivot)
    {
        GUI.matrix = GUI.matrix
            * Matrix4x4.TRS(new Vector3(pivot.x, pivot.y, 0f), Quaternion.identity, Vector3.one)
            * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale.x, scale.y, 1f))
            * Matrix4x4.TRS(new Vector3(-pivot.x, -pivot.y, 0f), Quaternion.identity, Vector3.one);
    }

    /// <summary>
    /// Begin a scaled menu OnGUI block. Returns the previous matrix to restore with <see cref="End"/>.
    /// Scales about the top-left origin and shifts right by the left safe-area inset, composing with any
    /// existing matrix. Do not nest Begin blocks.
    /// </summary>
    public static Matrix4x4 Begin()
    {
        return Begin(1f);
    }

    /// <summary>
    /// Begin a scaled menu OnGUI block at base*extra scale. <paramref name="extra"/> &gt; 1 makes this one
    /// screen render bigger than the default menu (login panel). VirtualWidth/Height honour the extra for
    /// the duration, so rects anchored against them stay correct. Must be paired with End().
    /// </summary>
    public static Matrix4x4 Begin(float extra)
    {
        // The extra multiplier is a mobile-only enlargement; desktop keeps its authored size.
        _activeExtra = Active ? Mathf.Max(0.1f, extra) : 1f;
        Matrix4x4 prev = GUI.matrix;
        float s = Scale;
        float inset = InsetLeftPixels;
        // Menu point p lands at screen pixel (inset + s * p.x, s * p.y).
        if (s != 1f || inset != 0f)
            GUI.matrix = Matrix4x4.TRS(new Vector3(inset, 0f, 0f), Quaternion.identity, new Vector3(s, s, 1f)) * prev;
        return prev;
    }

    public static void End(Matrix4x4 prev)
    {
        GUI.matrix = prev;
        _activeExtra = 1f;
    }
}
