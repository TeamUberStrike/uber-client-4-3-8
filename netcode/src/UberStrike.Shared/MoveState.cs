using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// The portion of a player's state that movement simulation reads and writes.
/// Lives identically on client (predicted) and server (authoritative).
/// </summary>
public struct MoveState
{
    public Vector3 Position;
    public Vector3 Velocity;
    public bool    Grounded;
    public bool    Crouching;
    public float   Stamina;
    public float   Yaw;   // radians
    public float   Pitch; // radians
}
