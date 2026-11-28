using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Scope-only gyro aim. Projects angular velocity on gravity-derived yaw/pitch axes (orientation-agnostic, 360).
public static class GyroAim
{
    private const float YawSign = 1f;
    private const float PitchSign = 1f;
    private const float DeadzoneDegPerSec = 0.6f;

    private static bool _running;
    private static readonly OneEuro _yawFilter = new OneEuro();
    private static readonly OneEuro _pitchFilter = new OneEuro();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void UberGyroStart();
    [DllImport("__Internal")] private static extern int UberGyroPermission();
    [DllImport("__Internal")] private static extern void UberGyroRead(float[] values);
    private static readonly float[] _web = new float[9];

    public static bool Supported { get { return ApplicationDataManager.IsMobile; } }

    // 0 waiting for tap, 1 granted, 2 denied, 3 no prompt needed.
    public static int WebPermission { get { return _running ? UberGyroPermission() : 0; } }

    // One line for the options panel, so a tester can see what the browser actually delivers.
    public static string WebStatus
    {
        get
        {
            if (!_running) return string.Empty;
            int perm = UberGyroPermission();
            if (perm == 0) return "Tap the screen to allow motion";
            if (perm == 2) return "Motion blocked: allow in browser settings";
            if (perm == 4) return "Gyro needs the HTTPS page";
            UberGyroRead(_web);
            if (_web[7] < 1f) return "No motion data from the browser";
            return "Motion " + Mathf.RoundToInt(_web[7]) + " Hz" + (_web[8] > 0.5f ? "" : " (calibrating)");
        }
    }
#else
    public static bool Supported { get { return SystemInfo.supportsGyroscope; } }

    public static int WebPermission { get { return 3; } }

    public static string WebStatus { get { return string.Empty; } }
#endif

    public static void EnsureRunning()
    {
        if (_running || !Supported) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        UberGyroStart();
#else
        Input.gyro.enabled = true;
#endif
        _running = true;
    }

    public static void Reset()
    {
        _yawFilter.Reset();
        _pitchFilter.Reset();
    }

    // Degrees this frame: x = yaw, y = pitch.
    public static Vector2 LookDelta(float strength, bool invertY, bool invertX)
    {
        if (!Supported) return Vector2.zero;
        EnsureRunning();

        float dt = Time.deltaTime;
        if (dt <= 0f) return Vector2.zero;

        Vector3 w, g;
        if (!Sample(out w, out g)) return Vector2.zero;
        if (g.sqrMagnitude < 1e-4f) return Vector2.zero;
        Vector3 up = -g.normalized;

        Vector3 right = Vector3.Cross(up, new Vector3(0f, 0f, 1f));
        if (right.sqrMagnitude < 1e-4f)
            right = Vector3.Cross(up, new Vector3(0f, 1f, 0f));
        right.Normalize();

        float yawRate = Vector3.Dot(w, up) * Mathf.Rad2Deg * YawSign;
        float pitchRate = Vector3.Dot(w, right) * Mathf.Rad2Deg * PitchSign;
        if (invertY) pitchRate = -pitchRate;
        if (invertX) yawRate = -yawRate;

        yawRate = _yawFilter.Filter(yawRate, dt);
        pitchRate = _pitchFilter.Filter(pitchRate, dt);

        if (Mathf.Abs(yawRate) < DeadzoneDegPerSec) yawRate = 0f;
        if (Mathf.Abs(pitchRate) < DeadzoneDegPerSec) pitchRate = 0f;

        return new Vector2(yawRate, pitchRate) * (strength * dt);
    }

    // Device axes, rad/s + gravity (down). WebGL: browser rates are deg/s; gravity from accelerationIncludingGravity.
    private static bool Sample(out Vector3 w, out Vector3 g)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UberGyroRead(_web);
        w = g = Vector3.zero;
        if (_web[0] < 0.5f) return false;
        w = new Vector3(_web[1], _web[2], _web[3]) * Mathf.Deg2Rad;
        // gravity from devicemotion (no beta/gamma singularity when the phone is held in landscape)
        g = new Vector3(_web[4], _web[5], _web[6]);
        return true;
#else
        w = Input.gyro.rotationRateUnbiased;
        g = Input.gyro.gravity;
        return true;
#endif
    }

    // 1-euro filter (Casiez 2012).
    private class OneEuro
    {
        private const float MinCutoff = 1.0f;
        private const float Beta = 0.02f;
        private const float DCutoff = 1.0f;

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
