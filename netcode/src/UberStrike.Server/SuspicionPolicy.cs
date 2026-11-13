namespace UberStrike.Server;

public enum ResponseLevel { None, Flag, Review, Action }

/// <summary>
/// Phase 8 — graduated response. Turns a player's anomaly state into a recommendation:
///
///   None → Flag (silent, telemetry only)
///        → Review (queued for a human — requires ≥2 INDEPENDENT signal kinds)
///        → Action (kick/ban RECOMMENDATION — also ≥2 kinds, and the score must have
///          stayed at Review level for a sustained period)
///
/// Two hard rules, encoded structurally:
///   1. NEVER escalate past Flag on a single heuristic — one hot signal (a lucky aim snap
///      streak, an accurate sniper) can only ever flag. Review/Action require independent
///      corroboration (DistinctKindCount ≥ 2).
///   2. The policy only ever RECOMMENDS — the host decides what Action means. Nothing in
///      the netcode auto-bans.
///
/// De-escalation is hysteretic: a level is only left when the score decays below HALF its
/// entry threshold, so a borderline player doesn't flap between levels.
/// </summary>
public sealed class SuspicionPolicy
{
    public const float  FlagScore            = 3f;
    public const float  ReviewScore          = 6f;
    public const float  ActionScore          = 10f;
    public const double ActionSustainSeconds = 30d;

    public ResponseLevel Level { get; private set; }

    private double _reviewEligibleSince = -1d;

    public ResponseLevel Evaluate(AnomalyTracker t, double now)
    {
        int kinds = t.DistinctKindCount;

        // sustain clock for Action: runs while the score sits at Review level WITH corroboration
        if (t.Score >= ReviewScore && kinds >= 2)
        {
            if (_reviewEligibleSince < 0d) _reviewEligibleSince = now;
        }
        else
        {
            _reviewEligibleSince = -1d;
        }

        ResponseLevel target;
        if (t.Score >= ActionScore && kinds >= 2 &&
            _reviewEligibleSince >= 0d && now - _reviewEligibleSince >= ActionSustainSeconds)
            target = ResponseLevel.Action;
        else if (t.Score >= ReviewScore && kinds >= 2)
            target = ResponseLevel.Review;
        else if (t.Score >= FlagScore)
            target = ResponseLevel.Flag;
        else
            target = ResponseLevel.None;

        if (target > Level)
        {
            Level = target;                      // escalate immediately
        }
        else if (target < Level)
        {
            // hysteresis: only step down when the score is clearly below the current level
            float exitFloor = Level switch
            {
                ResponseLevel.Action => ActionScore * 0.5f,
                ResponseLevel.Review => ReviewScore * 0.5f,
                ResponseLevel.Flag   => FlagScore * 0.5f,
                _ => 0f,
            };
            if (t.Score < exitFloor) Level = target;
        }
        return Level;
    }
}
