using System.Numerics;

namespace UberStrike.Shared;

// ---- Client -> Server ----------------------------------------------------------------

/// <summary>Wire form of a single input tick. SessionToken is validated against the connection.</summary>
public struct InputPacket
{
    public int      EntityId;
    public string   SessionToken;
    public InputCmd Cmd;
}

/// <summary>Client fire INTENT. Never carries a hit, a target, damage, or an origin.</summary>
public struct FireIntent
{
    public int    EntityId;
    public string SessionToken; // bound to the connection like InputPacket — a fire intent
                                // without proof of ownership lets anyone fire your weapon
    public int    Slot;
    public uint   ClientTick;
}

/// <summary>Client weapon-switch INTENT. The server enforces the per-weapon switch delay.</summary>
public struct SwitchIntent
{
    public int    EntityId;
    public string SessionToken;
    public int    Slot;
}

// ---- Server -> Client ----------------------------------------------------------------

/// <summary>
/// Authoritative per-player state in a snapshot. Must carry EVERY MoveState field the
/// movement step reads across ticks — reconciliation rebuilds MoveState from this and
/// replays unacked inputs; a missing field silently diverges the replay.
/// </summary>
public struct PlayerSnap
{
    public int     EntityId;
    public Vector3 Position;
    public Vector3 Velocity;
    public float   Yaw;
    public float   Pitch;
    public bool    Grounded;
    public bool    Jumping;
    public bool    Ducked;
    public bool    JumpArmed;
    public byte    UngroundedTicks;
    public float   SpeedScale;     // server-owned modifier (client never writes it)
    public float   Health;
    // local-only reconciliation aids:
    public int     ActiveSlot;
    public int     ActiveAmmo;
}

/// <summary>
/// One authoritative frame. <see cref="Local"/> is the recipient's own entity (for
/// reconciliation); <see cref="Others"/> are remote entities (for interpolation).
/// </summary>
public struct Snapshot
{
    public double       ServerTime;
    public uint         LastProcessedInput; // ack: highest input Seq the server simulated
    public PlayerSnap   Local;
    public PlayerSnap[] Others;
}

/// <summary>Authoritative combat outcome. The ONLY source of truth for damage/hits.</summary>
public struct HitEvent
{
    public int     Shooter;
    public int     Target;
    public float   Damage;
    public bool    Headshot;
    public bool    Killed;
    public Vector3 Point;
}
