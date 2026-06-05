namespace UberStrike.Shared;

/// <summary>
/// Tunables shared by client and server. These MUST be identical on both sides —
/// a single divergent constant (e.g. Gravity) causes permanent misprediction/rubber-banding.
/// </summary>
public static class GameConstants
{
    // --- simulation timestep ---
    public const float TickRate = 30f;
    public const float FixedDt  = 1f / TickRate;

    // --- movement ---
    public const float WalkSpeed   = 6f;
    public const float RunSpeed    = 9f;
    public const float CrouchSpeed = 3f;
    public const float JumpVelocity = 7f;
    public const float Gravity      = 20f;

    // --- anti-cheat tolerances ---
    public const float ExtraSpeedTolerance = 0.5f; // units/sec slack before teleport flag
    public const float TeleportThreshold   = 3f;   // client hard-snap distance during reconcile

    // --- lag compensation / interpolation ---
    public const float MaxRewindSeconds   = 0.25f; // server can rewind at most this far
    public const float InterpDelaySeconds = 0.10f; // remote players rendered this far in the past
    public const uint  MaxSeqGap          = 64;    // reject inputs beyond this gap (replay guard)

    // --- client smoothing ---
    public const float SmoothRate = 15f;           // error decay rate for reconciliation blending

    // --- hitbox geometry (capsule body + sphere head) ---
    public const float EyeHeight  = 1.6f;
    public const float BodyRadius = 0.40f;
    public const float BodyBottom = 0.10f;
    public const float BodyTop    = 1.55f;
    public const float HeadOffset = 1.70f;
    public const float HeadRadius = 0.22f;
}
