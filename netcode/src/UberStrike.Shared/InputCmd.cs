using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// One tick of player INTENT. The client may send this; the server simulates from it.
/// MoveDir is a normalized intent on the XZ plane — the server supplies the speed.
/// (UberStrike has no sprint; Crouch mirrors the retail duck key.)
/// </summary>
public struct InputCmd
{
    public uint    Seq;        // monotonic per-player input sequence
    public uint    ClientTick; // client's local tick when sampled
    public Vector3 MoveDir;    // normalized intent (XZ); magnitude clamped server-side
    public bool    Jump;       // jump key HELD (edge-trigger lives in MoveState.JumpArmed)
    public bool    Crouch;     // duck key held
    public float   Yaw;        // radians
    public float   Pitch;      // radians
}
