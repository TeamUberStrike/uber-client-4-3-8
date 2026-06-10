using UnityEngine;

/// <summary>
/// High-end "game" gyroscope aim for the mobile build — active ONLY while a weapon is scoped.
///
/// Robust, orientation-agnostic, 360°. Instead of mapping the raw device angular-velocity axes by
/// <c>Screen.orientation</c> (which leaked pitch into yaw and produced the shaky ~25° diagonal), it
/// PROJECTS the device angular velocity onto two physical axes derived from gravity:
///   • yaw   = rotation about world-up        (turn left/right)
///   • pitch = rotation about camera-right     (look up/down)
/// Because both axes come from the live gravity vector in the SAME device frame as the angular velocity,
/// the mapping is correct in any landscape/tilt and the two axes are exactly orthogonal — tilting the
/// phone up/down can no longer drift the aim sideways, at any heading (full 360°).
///
/// A per-axis 1€ filter (Casiez et al.) removes sensor/hand jitter when holding still (kills the "shaky"
/// feel) while staying 1:1 and low-latency when you move the phone fast, and a small post-filter deadzone
/// guarantees a perfectly still view from a still hand. Tunable via the Strength slider and Invert Vertical
/// option; previewed in the Try-Weapons range ("Try Gyroscope"). Scope-only gating lives in
/// <c>UserInput.UpdateMouse</c> (guarded by <c>LevelCamera.Instance.IsZoomedIn</c>).
/// </summary>
public static class GyroAim
{
    private static bool _running;
    private static readonly OneEuro _yawFilter = new OneEuro();
    private static readonly OneEuro _pitchFilter = new OneEuro();

    // The ONLY orientation knobs left. The gravity projection fixes the AXES (no more diagonal); these two
    // just set which way each axis turns the view. Pitch also has the user-facing Invert Vertical toggle.
    // If a direction comes out reversed on device, flip the matching sign here (one character).
    private const float YawSign = 1f;
    private const float PitchSign = 1f;

    // Rotation slower than this (deg/s) after smoothing is treated as residual tremor/noise and zeroed, so
    // a steady hand gives an absolutely steady view.
    private const float DeadzoneDegPerSec = 0.6f;

    public static bool Supported { get { return SystemInfo.supportsGyroscope; } }

    /// <summary>Lazily start the hardware gyro stream (no-op if unsupported or already running).</summary>
    public static void EnsureRunning()
    {
        if (_running || !SystemInfo.supportsGyroscope) return;
        Input.gyro.enabled = true;
        _running = true;
    }

    /// <summary>Clear the filter history (call when (re)entering scope so the first frame doesn't jump).</summary>
    public static void Reset()
    {
        _yawFilter.Reset();
        _pitchFilter.Reset();
    }

    /// <summary>
    /// Per-frame look delta in DEGREES (x = yaw, y = pitch), framerate-compensated.
    /// <paramref name="strength"/> is the user multiplier (1 ≈ true 1:1 device→view rotation);
    /// <paramref name="invertY"/> flips the vertical axis.
    /// </summary>
    public static Vector2 LookDelta(float strength, bool invertY, bool invertX)
    {
        if (!SystemInfo.supportsGyroscope) return Vector2.zero;
        EnsureRunning();

        float dt = Time.deltaTime;
        if (dt <= 0f) return Vector2.zero;

        // Both vectors are in the device-local frame (x right, y up, z toward viewer in portrait). We only
        // ever take dot products between them, so whatever global handedness/orientation convention Unity
        // applies cancels out — the result depends only on the physical geometry. That is what makes this
        // robust across orientations without a per-orientation switch.
        Vector3 w = Input.gyro.rotationRateUnbiased;   // angular velocity, rad/s
        Vector3 g = Input.gyro.gravity;                // gravity in device frame (~unit), points "down"

        if (g.sqrMagnitude < 1e-4f) return Vector2.zero;
        Vector3 up = -g.normalized;                    // world-up in the device frame

        // Camera-right = the horizontal screen axis the player tilts about to look up/down: perpendicular to
        // gravity, in the screen plane. screen-out is +Z in the device frame. Guard the degenerate case of
        // the phone pointing straight up/down (gravity parallel to screen-out).
        Vector3 right = Vector3.Cross(up, new Vector3(0f, 0f, 1f));
        if (right.sqrMagnitude < 1e-4f)
            right = Vector3.Cross(up, new Vector3(0f, 1f, 0f));
        right.Normalize();

        float yawRate = Vector3.Dot(w, up) * Mathf.Rad2Deg * YawSign;       // deg/s about vertical
        float pitchRate = Vector3.Dot(w, right) * Mathf.Rad2Deg * PitchSign; // deg/s about camera-right
        if (invertY) pitchRate = -pitchRate;
        if (invertX) yawRate = -yawRate;

        // 1€ filter: smooth when slow (no jitter holding aim), responsive when fast (1:1, no lag).
        yawRate = _yawFilter.Filter(yawRate, dt);
        pitchRate = _pitchFilter.Filter(pitchRate, dt);

        // Deadzone AFTER filtering so a still hand yields exactly zero.
        if (Mathf.Abs(yawRate) < DeadzoneDegPerSec) yawRate = 0f;
        if (Mathf.Abs(pitchRate) < DeadzoneDegPerSec) pitchRate = 0f;

        // deg/s * s = degrees this frame; strength scales from the true 1:1 mapping at strength 1.
        return new Vector2(yawRate, pitchRate) * (strength * dt);
    }

    /// <summary>
    /// Minimal 1€ filter (Casiez, Roussel, Vogel 2012) on a scalar signal. Adaptive cutoff: heavy smoothing
    /// at low speed (kills jitter), light smoothing at high speed (low latency). Exactly the "no shake when
    /// still, snappy when moving" behaviour wanted for gyro aim.
    /// </summary>
    private class OneEuro
    {
        private const float MinCutoff = 1.0f;   // Hz — lower = smoother (more lag) when nearly still
        private const float Beta = 0.02f;       // speed coefficient — higher = less lag during fast motion
        private const float DCutoff = 1.0f;     // Hz — derivative cutoff

        private bool _has;
        private float _xPrev;
        private float _dxPrev;

        public void Reset()
        {
            _has = false;
            _xPrev = 0f;
            _dxPrev = 0f;
        }

        public float Filter(float x, float dt)
        {
            if (!_has)
            {
                _has = true;
                _xPrev = x;
                _dxPrev = 0f;
                return x;
            }

            float dx = (x - _xPrev) / dt;
            float dxHat = _dxPrev + Alpha(DCutoff, dt) * (dx - _dxPrev);
            float cutoff = MinCutoff + Beta * Mathf.Abs(dxHat);
            float xHat = _xPrev + Alpha(cutoff, dt) * (x - _xPrev);

            _xPrev = xHat;
            _dxPrev = dxHat;
            return xHat;
        }

        private static float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f / (1f + tau / dt);
        }
    }
}
