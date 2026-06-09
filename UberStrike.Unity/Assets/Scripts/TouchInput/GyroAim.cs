using UnityEngine;

/// <summary>
/// High-end "game" gyroscope aim for the mobile build — active ONLY while a weapon is scoped.
///
/// Integrates the device's bias-corrected angular velocity (rad/s) into a per-frame look delta and feeds it
/// straight into the look angle (bypassing the touch-drag smoothing in <see cref="TouchInput.WishLook"/>),
/// so the view tracks the device 1:1 and STOPS the instant the device stops — the relative "game gyro"
/// model used by CoD/PUBG Mobile, not absolute attitude aiming (no drift, no recentre needed).
///
/// Mapped for landscape orientation. Tunable via the Strength slider and Invert Vertical option, previewed
/// in the Try-Weapons testing area ("Try Gyroscope"). Scope-only gating lives in <c>UserInput.UpdateMouse</c>
/// (guarded by <see cref="TouchInput.IsScoped"/>), matching the design requirement.
/// </summary>
public static class GyroAim
{
    private static bool _running;

    public static bool Supported { get { return SystemInfo.supportsGyroscope; } }

    /// <summary>Lazily start the hardware gyro stream (no-op if unsupported or already running).</summary>
    public static void EnsureRunning()
    {
        if (_running || !SystemInfo.supportsGyroscope) return;
        Input.gyro.enabled = true;
        _running = true;
    }

    /// <summary>
    /// Per-frame look delta in DEGREES (x = yaw, y = pitch), framerate-compensated.
    /// <paramref name="strength"/> is the user multiplier (1 ≈ true 1:1 device→view rotation);
    /// <paramref name="invertY"/> flips the vertical axis.
    /// </summary>
    public static Vector2 LookDelta(float strength, bool invertY)
    {
        if (!SystemInfo.supportsGyroscope) return Vector2.zero;
        EnsureRunning();

        // rotationRateUnbiased is the device-LOCAL angular velocity (right-handed, rad/s) and is NOT
        // re-mapped by Screen.orientation, so we remap it ourselves. In PORTRAIT the FPS convention is
        // yaw←y / pitch←x; rotating the phone into LANDSCAPE swaps which physical axis is world-vertical
        // vs world-horizontal, so in landscape it becomes yaw←x / pitch←y. Signs differ between the two
        // landscape orientations. (Verified axis-assignment; exact signs are confirmed on-device — if yaw
        // is reversed it's a one-line flip, and pitch has the user-facing Invert Vertical toggle.)
        Vector3 r = Input.gyro.rotationRateUnbiased;

        float yaw, pitch;
        switch (Screen.orientation)
        {
            case ScreenOrientation.LandscapeRight:
                yaw   = -r.x;
                pitch =  r.y;
                break;
            case ScreenOrientation.LandscapeLeft:
            default:
                yaw   =  r.x;
                pitch = -r.y;
                break;
        }

        if (invertY) pitch = -pitch;

        // rad/s → deg/frame. Rad2Deg makes strength 1 a true 1:1 mapping (rotate the device N° → the view
        // turns N°); the slider scales from there. Multiplying by deltaTime integrates the rate, so the
        // total pan equals the total angle the device was rotated, independent of framerate.
        float s = strength * Mathf.Rad2Deg * Time.deltaTime;
        return new Vector2(yaw * s, pitch * s);
    }
}
