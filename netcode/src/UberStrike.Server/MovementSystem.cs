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
        Vector3 before = s.Move.Position;

        SharedMovement.Step(ref s.Move, cmd, dt, _world); // identical to client prediction

        // Horizontal speed/teleport guard: displacement can't exceed the physical maximum.
        Vector3 d  = s.Move.Position - before;
        Vector3 dh = new(d.X, 0f, d.Z);
        float maxStep = (GameConstants.RunSpeed + GameConstants.ExtraSpeedTolerance) * dt;
        if (dh.Length() > maxStep)
        {
            s.Anomaly.Bump(AnomalyKind.Teleport);
            dh = Vector3.Normalize(dh) * maxStep;
            s.Move.Position = before + new Vector3(dh.X, d.Y, dh.Z);
        }

        if (!_world.Contains(s.Move.Position))
            s.Move.Position = _world.ClampToBounds(s.Move.Position);

        s.LastProcessedInput = cmd.Seq;
    }
}
