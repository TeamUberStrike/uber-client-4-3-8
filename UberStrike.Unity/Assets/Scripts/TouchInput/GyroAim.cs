using UnityEngine;

// Scope-only gyro aim. Projects angular velocity on gravity-derived yaw/pitch axes (orientation-agnostic, 360).
public static class GyroAim
{
    private const float YawSign = 1f;
    private const float PitchSign = 1f;
    private const float DeadzoneDegPerSec = 0.6f;

    private static bool _running;
    private static readonly OneEuro _yawFilter = new OneEuro();
    private static readonly OneEuro _pitchFilter = new OneEuro();

    public static bool Supported { get { return Application.platform != RuntimePlatform.WebGLPlayer && SystemInfo.supportsGyroscope; } }

    public static void EnsureRunning()
    {
        if (_running || !Supported) return;
        Input.gyro.enabled = true;
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

        Vector3 w = Input.gyro.rotationRateUnbiased;
        Vector3 g = Input.gyro.gravity;
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
