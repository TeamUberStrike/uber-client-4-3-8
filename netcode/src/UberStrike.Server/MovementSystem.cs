using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Server movement authority. Runs the SAME SharedMovement.Step as the client, then applies
/// speed/teleport/bounds guards on top. Client positions are never trusted.
/// </summary>
public sealed class MovementSystem
{
    private readonly ICollisionWorld _world;
    public MovementSystem(ICollisionWorld world) => _world = world;

    public void Apply(PlayerState s, in InputCmd cmd, float dt)
    {
        Vector3 before  = s.Move.Position;
        Vector3 vBefore = s.Move.Velocity;
        // External impulses are queued by SERVER gameplay code only (never from a client
        // packet), so a tick that consumes one is legitimately allowed any speed.
        bool hadImpulse = s.Move.ExternalForceMode != ForceMode.None;

        SharedMovement.Step(ref s.Move, cmd, dt, _world); // identical to client prediction

        // Horizontal speed/teleport guard: in one tick, speed can't grow past "what was
        // already carried, or walk speed" + tolerance. The server runs the sim itself, so
        // this catches state corruption/NaN rather than client input (the gateway already
        // clamped that) — but it's the last line if anything upstream goes wrong.
        if (!hadImpulse)
        {
            Vector3 d  = s.Move.Position - before;
            Vector3 dh = new(d.X, 0f, d.Z);
            float carried = SharedMovement.Len3(new Vector3(vBefore.X, 0f, vBefore.Z));
            float ceiling = MathF.Max(GameConstants.WalkSpeed, carried) + GameConstants.ExtraSpeedTolerance;
            float maxStep = ceiling * dt;
            if (SharedMovement.Len3(dh) > maxStep)
            {
                s.Anomaly.Bump(AnomalyKind.Teleport);
                dh = Vector3.Normalize(dh) * maxStep;
                s.Move.Position = before + new Vector3(dh.X, d.Y, dh.Z);
            }
        }

        if (!_world.Contains(s.Move.Position))
            s.Move.Position = _world.ClampToBounds(s.Move.Position);

        s.LastProcessedInput = cmd.Seq;
    }
}
