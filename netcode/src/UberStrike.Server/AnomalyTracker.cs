namespace UberStrike.Server;

public enum AnomalyKind { Teleport, FireRate, WallShot, AimSnap, SchemaViolation, Triggerbot, Accuracy, RevealReaction }

/// <summary>
/// Rolling per-player suspicion score plus combat statistics. Validation stops IMPOSSIBLE
/// states; this catches POSSIBLE-but-superhuman behavior (aimbot/triggerbot/wallhack tells).
///
/// Phase 8: score is tracked PER KIND so the response policy can require multiple
/// independent signals — one hot heuristic alone must never escalate past a flag.
/// Detection signals here are soft evidence for humans to review, never auto-punishment.
/// </summary>
public sealed class AnomalyTracker
{
    public float Score { get; private set; }

    public int Shots, Hits, Headshots;

    private readonly Dictionary<AnomalyKind, float> _kindScores = new();

    private double _lastAimTime = -1d;
    private float  _lastYaw, _lastPitch;

    // triggerbot: rolling acquire->fire reaction-time window
    private int _reactSamples, _reactFast;

    // aimbot: rolling fog-of-war reveal->damage reaction-time window
    private int _revealSamples, _revealFast;

    // windowed shot stats (bit0 landed, bit1 headshot), last 40 shots
    private readonly byte[] _shotWin = new byte[40];
    private int _shotIdx, _shotWinCount;

    /// <summary>Raised on every bump — the telemetry hook (kind, added, new total score).</summary>
    public event Action<AnomalyKind, float, float>? Bumped;

    public float ScoreOf(AnomalyKind k) => _kindScores.TryGetValue(k, out float v) ? v : 0f;

    /// <summary>Kinds contributing meaningfully (≥0.3) — the "independent signals" count.</summary>
    public int DistinctKindCount
    {
        get
        {
            int n = 0;
            foreach (float v in _kindScores.Values) if (v >= 0.3f) n++;
            return n;
        }
    }

    public void Bump(AnomalyKind k, float weight = 1f)
    {
        float add = Weight(k) * weight;
        _kindScores[k] = ScoreOf(k) + add;
        Score += add;
        Bumped?.Invoke(k, add, Score);
    }

    public void Decay(float dt)
    {
        if (Score <= 0f) return;
        float dec = dt * 0.1f;                    // documented decay rate on the TOTAL
        float total = Score;
        float newTotal = 0f;
        foreach (AnomalyKind k in _kindScores.Keys.ToArray())
        {
            float v = _kindScores[k];
            v = MathF.Max(0f, v - dec * (v / total)); // proportional share of the decay
            _kindScores[k] = v;
            newTotal += v;
        }
        Score = newTotal;
    }

    public void RecordShot(bool landed, bool headshot)
    {
        Shots++;
        if (landed)  Hits++;
        if (headshot) Headshots++;

        _shotWin[_shotIdx] = (byte)((landed ? 1 : 0) | (headshot ? 2 : 0));
        _shotIdx = (_shotIdx + 1) % _shotWin.Length;
        if (_shotWinCount < _shotWin.Length) _shotWinCount++;

        // Sustained near-perfect windowed accuracy is a soft aimbot tell. Low weight on
        // purpose: a camping sniper is legitimately accurate — the policy needs OTHER kinds
        // to corroborate before this means anything.
        if (_shotWinCount >= 30)
        {
            int wHits = 0, wHead = 0;
            for (int i = 0; i < _shotWinCount; i++)
            {
                if ((_shotWin[i] & 1) != 0) wHits++;
                if ((_shotWin[i] & 2) != 0) wHead++;
            }
            float acc = (float)wHits / _shotWinCount;
            if (acc > 0.85f) Bump(AnomalyKind.Accuracy, 0.05f);
            if (wHits >= 10 && (float)wHead / wHits > 0.8f) Bump(AnomalyKind.Accuracy, 0.08f);
        }
    }

    /// <summary>
    /// Triggerbot signal: acquire→fire reaction time sample (true = sub-human-fast).
    /// Humans bottom out around 150–250 ms; a bot fires the tick the crosshair lands.
    /// One fast shot is luck/prediction; two-thirds of a window is a machine.
    /// </summary>
    public void RecordReaction(bool fast)
    {
        _reactSamples++;
        if (fast) _reactFast++;
        if (_reactSamples >= 8)
        {
            if (_reactFast * 3 >= _reactSamples * 2) Bump(AnomalyKind.Triggerbot, 0.6f);
            _reactSamples = 0; _reactFast = 0;
        }
    }

    /// <summary>
    /// Aimbot signal: fog-of-war reveal→damage reaction sample (true = sub-human-fast).
    /// The server knows the exact moment a target STARTED streaming to this player
    /// (VisibilitySystem reveal transition); landing damage on it faster than a human can
    /// perceive + aim, sustained over a window, is machine assistance. One fast hit is a
    /// pre-aimed corner; two-thirds of a window is a machine. Independent of Triggerbot
    /// (crosshair-dwell) — SuspicionPolicy requires multiple kinds, and this is its own kind.
    /// </summary>
    public void RecordRevealReaction(bool fast)
    {
        _revealSamples++;
        if (fast) _revealFast++;
        if (_revealSamples >= 6)
        {
            if (_revealFast * 3 >= _revealSamples * 2) Bump(AnomalyKind.RevealReaction, 0.6f);
            _revealSamples = 0; _revealFast = 0;
        }
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
        AnomalyKind.Triggerbot      => 1.0f,
        AnomalyKind.Accuracy        => 1.0f,
        AnomalyKind.RevealReaction  => 1.1f, // server knows the reveal moment exactly — strong signal
        _ => 1.0f,
    };
}
