namespace UberStrike.Server;

public enum AnomalyKind { Teleport, FireRate, WallShot, AimSnap, SchemaViolation }

/// <summary>
/// Rolling per-player suspicion score plus combat statistics. Validation stops IMPOSSIBLE
/// states; this catches POSSIBLE-but-superhuman behavior (aimbot/triggerbot/wallhack tells).
/// </summary>
public sealed class AnomalyTracker
{
    public float Score { get; private set; }

    public int Shots, Hits, Headshots;

    private double _lastAimTime = -1d;
    private float  _lastYaw, _lastPitch;

    public void Bump(AnomalyKind k, float weight = 1f) => Score += Weight(k) * weight;

    public void Decay(float dt) => Score = MathF.Max(0f, Score - dt * 0.1f);

    public void RecordShot(bool landed, bool headshot)
    {
        Shots++;
        if (landed)  Hits++;
        if (headshot) Headshots++;
    }

    public float Accuracy      => Shots > 0 ? (float)Hits / Shots : 0f;
    public float HeadshotRatio => Hits  > 0 ? (float)Headshots / Hits : 0f;

    /// <summary>Flag implausibly fast aim snaps (rough aimbot heuristic). Soft signal only.</summary>
    public void ObserveAimDelta(double now, float yaw, float pitch)
    {
        if (_lastAimTime >= 0d)
        {
            float dt = (float)(now - _lastAimTime);
            if (dt > 1e-4f)
            {
                float dYaw   = AngleDiff(yaw, _lastYaw);
                float dPitch = pitch - _lastPitch;
                float angVel = MathF.Sqrt(dYaw * dYaw + dPitch * dPitch) / dt; // rad/s
                if (angVel > 35f) Bump(AnomalyKind.AimSnap, 0.3f); // ~2000 deg/s instantaneous
            }
        }
        _lastAimTime = now;
        _lastYaw = yaw;
        _lastPitch = pitch;
    }

    private static float AngleDiff(float a, float b)
    {
        // Bounded wrap to (-PI, PI]. Use modulo, not a while-loop: a non-finite or very large
        // delta (e.g. Normalize of a near-zero aim vector) would spin the old while() forever.
        float d = (a - b) % (2f * MathF.PI);
        if (d >  MathF.PI) d -= 2f * MathF.PI;
        else if (d < -MathF.PI) d += 2f * MathF.PI;
        return d;
    }

    private static float Weight(AnomalyKind k) => k switch
    {
        AnomalyKind.Teleport        => 1.0f,
        AnomalyKind.FireRate        => 0.8f,
        AnomalyKind.WallShot        => 1.2f,
        AnomalyKind.AimSnap         => 0.5f,
        AnomalyKind.SchemaViolation => 2.0f,
        _ => 1.0f,
    };
}
