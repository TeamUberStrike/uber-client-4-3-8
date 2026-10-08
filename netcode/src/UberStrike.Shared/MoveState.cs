using System.Numerics;

namespace UberStrike.Shared;

/// <summary>How a pending external force combines with current velocity (jump pads, explosions).</summary>
public enum ForceMode : byte
{
    None = 0,
    /// <summary>Halve vertical velocity, then add the force (UberStrike's ForceType.Additive).</summary>
    Additive = 1,
    /// <summary>Replace velocity with the force (UberStrike's ForceType.Exclusive).</summary>
    Exclusive = 2,
}

/// <summary>
/// The portion of a player's state that movement simulation reads and writes.
/// Lives identically on client (predicted) and server (authoritative).
///
/// EVERY field here that Step() reads across ticks must also travel in PlayerSnap —
/// reconciliation rebuilds this struct from a snapshot and replays inputs on top, so a
/// field left out of the snapshot silently diverges the replay.
/// </summary>
public struct MoveState
{
    public Vector3 Position;
    public Vector3 Velocity;

    public bool Grounded;
    /// <summary>True from the jump until ground contact (original PlayerStates.JUMPING).</summary>
    public bool Jumping;
    public bool Ducked;
    /// <summary>Edge trigger: jump fires only after the key was seen released (original _canJump).</summary>
    public bool JumpArmed;
    /// <summary>Ticks of lost ground contact; grounded survives a short grace (original _ungroundedCount).</summary>
    public byte UngroundedTicks;

    /// <summary>
    /// Server-owned speed multiplier (gear weight, zoom, damage slowdown — original SpeedModifier).
    /// The CLIENT NEVER sets this; it arrives via snapshots. &lt;= 0 means "unset" and reads as 1.
    /// </summary>
    public float SpeedScale;

    /// <summary>Pending external impulse (jump pad / explosion), consumed by the next Step.</summary>
    public Vector3   ExternalForce;
    public ForceMode ExternalForceMode;

    public float Yaw;   // radians
    public float Pitch; // radians

    /// <summary>Queue an external impulse (server gameplay events; client mirrors via snapshot+events).</summary>
    public void ApplyForce(Vector3 force, ForceMode mode)
    {
        ExternalForce = force;
        ExternalForceMode = mode;
    }
}
