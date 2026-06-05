using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// THE most important file in the project. This is the single deterministic movement step
/// compiled into BOTH the client (for prediction/replay) and the server (for authority).
/// If client and server ever run different logic here, the player rubber-bands forever.
///
/// Rules for editing:
///  - No engine calls (no UnityEngine.*). Pure System.Numerics math only.
///  - No wall-clock time, no randomness, no per-platform branches.
///  - Same constants, same order of operations on both sides.
/// </summary>
public static class SharedMovement
{
    public static void Step(ref MoveState m, in InputCmd cmd, float dt, ICollisionWorld world)
    {
        float speed = m.Crouching ? GameConstants.CrouchSpeed
                    : (cmd.Sprint && m.Stamina > 0f) ? GameConstants.RunSpeed
                    : GameConstants.WalkSpeed;

        // Treat MoveDir as a clamped unit vector on the XZ plane. The CLIENT never sets speed.
        Vector3 flat = new(cmd.MoveDir.X, 0f, cmd.MoveDir.Z);
        Vector3 wish = ClampMagnitude(flat, 1f) * speed;

        if (cmd.Jump && m.Grounded) m.Velocity.Y = GameConstants.JumpVelocity;
        m.Velocity.Y -= GameConstants.Gravity * dt;

        Vector3 delta = new(wish.X * dt, m.Velocity.Y * dt, wish.Z * dt);
        m.Position = world.CollideAndSlide(m.Position, delta);
        m.Grounded = world.CheckGrounded(m.Position);
        if (m.Grounded && m.Velocity.Y < 0f) m.Velocity.Y = 0f;

        m.Yaw   = cmd.Yaw;
        m.Pitch = cmd.Pitch;
    }

    public static Vector3 ClampMagnitude(Vector3 v, float max)
    {
        float sq = v.LengthSquared();
        if (sq > max * max && sq > 0f) return v / MathF.Sqrt(sq) * max;
        return v;
    }

    /// <summary>Forward direction from yaw/pitch (radians). Right-handed, +Z forward at yaw 0.</summary>
    public static Vector3 DirFromAngles(float yaw, float pitch)
    {
        float cy = MathF.Cos(yaw), sy = MathF.Sin(yaw);
        float cp = MathF.Cos(pitch), sp = MathF.Sin(pitch);
        return Vector3.Normalize(new Vector3(sy * cp, -sp, cy * cp));
    }
}
