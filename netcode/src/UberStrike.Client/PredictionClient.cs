using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Client;

/// <summary>
/// Local-player prediction + server reconciliation. Predicts movement immediately for zero
/// input latency, retains unacked inputs, and on each snapshot snaps to the authoritative
/// state then REPLAYS the unacked inputs via the SAME SharedMovement.Step the server runs.
/// </summary>
public sealed class PredictionClient
{
    private readonly ICollisionWorld _world;
    public int    EntityId { get; }
    public string Token    { get; }

    private uint _seq;
    private readonly List<InputCmd> _pending = new();
    private MoveState _state;          // logical predicted state (== corrected after reconcile)
    private Vector3   _positionError;  // residual blended toward zero for smooth rendering

    public PredictionClient(ICollisionWorld world, int entityId, string token, Vector3 spawn)
    {
        _world = world; EntityId = entityId; Token = token;
        _state.Position = spawn; _state.Grounded = true;
    }

    public MoveState State => _state;
    public Vector3   RenderPosition => _state.Position - _positionError;
    public int       PendingCount => _pending.Count;
    public float     LastReconcileError { get; private set; }

    /// <summary>Sequence + predict locally + retain for reconciliation. Returns the packet to send.</summary>
    public InputPacket BuildAndPredict(InputCmd cmd)
    {
        cmd.Seq = ++_seq;
        SharedMovement.Step(ref _state, cmd, GameConstants.FixedDt, _world); // predict NOW
        _pending.Add(cmd);
        return new InputPacket { EntityId = EntityId, SessionToken = Token, Cmd = cmd };
    }

    public void Reconcile(in Snapshot snap)
    {
        // 1. Drop inputs the server already accounted for.
        uint acked = snap.LastProcessedInput;   // hoist: can't capture an 'in' param in a lambda
        _pending.RemoveAll(c => c.Seq <= acked);

        // 2. Reset to authoritative truth.
        MoveState corrected = new()
        {
            Position = snap.Local.Position,
            Velocity = snap.Local.Velocity,
            Grounded = snap.Local.Grounded,
            Yaw = snap.Local.Yaw,
            Pitch = snap.Local.Pitch,
        };

        // 3. Replay the still-unacked inputs on top.
        foreach (InputCmd c in _pending)
            SharedMovement.Step(ref corrected, c, GameConstants.FixedDt, _world);

        // 4. Fold the correction into the smoother (or hard-snap on big jumps).
        Vector3 err = corrected.Position - _state.Position;
        LastReconcileError = err.Length();
        if (LastReconcileError > GameConstants.TeleportThreshold)
            _positionError = Vector3.Zero;        // teleport/respawn: don't smooth
        else
            _positionError += err;                // smooth small corrections

        _state = corrected;
    }

    public void SmoothErrors(float frameDt)
    {
        float k = 1f - MathF.Exp(-GameConstants.SmoothRate * frameDt);
        _positionError = Vector3.Lerp(_positionError, Vector3.Zero, k);
    }
}
