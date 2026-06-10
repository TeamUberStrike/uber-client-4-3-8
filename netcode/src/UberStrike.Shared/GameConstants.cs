namespace UberStrike.Shared;

/// <summary>
/// Tunables shared by client and server. These MUST be identical on both sides —
/// a single divergent constant (e.g. Gravity) causes permanent misprediction/rubber-banding.
///
/// Movement values are UberStrike 4.3.8's real ones, lifted from the Unity client:
///   LevelEnviroment.cs  -> PlayerWalkSpeed = 7, PlayerJumpSpeed = 15
///   EnviromentSettings  -> GroundAcceleration = 15, GroundFriction = 8, AirAcceleration = 3,
///                          StopSpeed = 8, Gravity = 50
///   CharacterMoveController -> PLAYER_DUCK_SCALE = 0.7, PLAYER_MIN_SCALE = 0.5,
///                              vertical velocity clamped to ±150
///   PlayerAttributes    -> HEIGHT_NORMAL = 1.6, HEIGHT_DUCKED = 0.9
/// </summary>
public static class GameConstants
{
    // --- simulation timestep ---
    public const float TickRate = 30f;
    public const float FixedDt  = 1f / TickRate;

    // --- movement (UberStrike 4.3.8 retail values) ---
    public const float WalkSpeed          = 7f;   // LevelEnviroment.PlayerWalkSpeed
    public const float JumpVelocity       = 15f;  // LevelEnviroment.PlayerJumpSpeed
    public const float Gravity            = 50f;  // EnviromentSettings.Gravity
    public const float GroundAcceleration = 15f;  // EnviromentSettings.GroundAcceleration
    public const float GroundFriction     = 8f;   // EnviromentSettings.GroundFriction
    public const float AirAcceleration    = 3f;   // EnviromentSettings.AirAcceleration
    public const float StopSpeed          = 8f;   // EnviromentSettings.StopSpeed
    public const float DuckSpeedScale     = 0.7f; // PLAYER_DUCK_SCALE
    public const float MinSpeedScale      = 0.5f; // PLAYER_MIN_SCALE
    public const float MaxVerticalSpeed   = 150f; // CharacterMoveController clamps v.y to ±150
    public const float FullStopSpeed      = 0.5f; // below this with no input, halt horizontally

    // The original kept "grounded" through 5 RENDER frames of lost ground contact to absorb
    // downhill jitter. At our 30 Hz tick, 2 ticks (~67 ms) approximates the same grace.
    public const int GroundedGraceTicks = 2;

    // --- anti-cheat tolerances ---
    // Slack above the physical ceiling before the teleport flag. Must exceed the largest
    // legitimate one-tick speed gain: air accel adds up to AirAcceleration*WalkSpeed*dt
    // = 0.7 u/s per tick on top of carried speed.
    public const float ExtraSpeedTolerance = 1.0f;
    public const float TeleportThreshold   = 3f;   // client hard-snap distance during reconcile

    // --- desync detector (client-side, testing/QA signal) ---
    public const float DesyncEpsilon  = 1e-3f; // reconcile error above this counts as desync
    public const int   DesyncTickLimit = 5;    // consecutive desynced reconciles before alarm

    // --- lag compensation / interpolation ---
    public const float MaxRewindSeconds   = 0.25f; // server can rewind at most this far
    public const float InterpDelaySeconds = 0.10f; // remote players rendered this far in the past
    public const uint  MaxSeqGap          = 64;    // reject inputs beyond this gap (replay guard)

    // --- client smoothing ---
    public const float SmoothRate = 15f;           // error decay rate for reconciliation blending

    // --- hitbox geometry (capsule body + sphere head) ---
    public const float EyeHeight  = 1.6f;          // PlayerAttributes.HEIGHT_NORMAL
    public const float BodyRadius = 0.40f;
    public const float BodyBottom = 0.10f;
    public const float BodyTop    = 1.55f;
    public const float HeadOffset = 1.70f;
    public const float HeadRadius = 0.22f;
    public const float HeightDucked = 0.9f;        // PlayerAttributes.HEIGHT_DUCKED
}
