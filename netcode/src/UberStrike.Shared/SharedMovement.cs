using System.Numerics;

namespace UberStrike.Shared;

/// <summary>
/// THE most important file in the project. This is the single deterministic movement step
/// compiled into BOTH the client (for prediction/replay) and the server (for authority).
/// If client and server ever run different logic here, the player rubber-bands forever.
///
/// This is UberStrike 4.3.8's REAL movement (Quake-3 lineage), ported from the Unity client's
/// CharacterMoveController: stop-speed friction, dot-product acceleration (air-strafe and
/// bunny-hop preserving), edge-triggered jump, duck, grounded grace, external impulses.
/// Water / ladder / fly states need volume queries from the collision world and land with
/// Phase 4; ground/air/duck/jump — the states 95% of combat happens in — are complete.
///
/// Rules for editing (see docs/determinism.md):
///  - No engine calls (no UnityEngine.*). No wall-clock time, no randomness, no per-platform
///    branches.
///  - DETERMINISM-CONSTRAINED MATH ONLY: +, -, *, /, comparisons and MathF.Sqrt. These are
///    IEEE-754 exactly-specified and bit-identical on x64 .NET and WASM. NO MathF.Sin/Cos/
///    Exp/Pow (platform libm), NO FusedMultiplyAdd, NO System.Numerics horizontal ops
///    (Vector3.Length/Normalize/Dot may reorder additions under SIMD) inside Step's reach.
///  - Same constants, same order of operations on both sides.
/// </summary>
public static class SharedMovement
{
    public static void Step(ref MoveState m, in InputCmd cmd, float dt, ICollisionWorld world)
    {
        // ---- duck (original CheckDuck: no transitions while airborne from a jump; standing
        //      up requires headroom) ----
        if (!m.Jumping)
        {
            if (cmd.Crouch)
            {
                m.Ducked = true;
            }
            else if (m.Ducked && world.HasHeadroom(m.Position))
            {
                m.Ducked = false;
            }
        }

        // ---- jump edge trigger (original _canJump: must see the key released to re-arm) ----
        if (!cmd.Jump) m.JumpArmed = true;

        bool jumpedThisTick = false;
        if (m.Grounded && cmd.Jump && m.JumpArmed && !m.Ducked)
        {
            m.JumpArmed = false;
            m.Jumping   = true;
            m.Grounded  = false;     // original CheckJump drops GROUNDED before friction runs,
                                     // which is exactly what makes bunny-hopping keep its speed
            m.Velocity.Y = GameConstants.JumpVelocity;
            jumpedThisTick = true;
        }

        // ---- per-state move (original MoveOnGround / MoveInAir) ----
        float effectiveScale = EffectiveSpeedScale(m);
        if (m.Grounded && !jumpedThisTick)
        {
            GroundMove(ref m, cmd, dt, effectiveScale);
        }
        else
        {
            AirMove(ref m, cmd, dt);
        }

        // ---- external impulse (jump pads, explosions — original ApplyForce consumption) ----
        if (m.ExternalForceMode != ForceMode.None)
        {
            if (m.ExternalForceMode == ForceMode.Additive)
            {
                m.Velocity = new Vector3(m.Velocity.X, m.Velocity.Y * 0.5f, m.Velocity.Z) + m.ExternalForce;
            }
            else // Exclusive
            {
                m.Velocity = m.ExternalForce;
            }
            m.ExternalForce = Vector3.Zero;
            m.ExternalForceMode = ForceMode.None;
            m.Jumping = true; // original sets PlayerStates.JUMPING after a force
        }

        // ---- vertical clamp (original: Mathf.Clamp(v.y, -150, 150)) ----
        if (m.Velocity.Y >  GameConstants.MaxVerticalSpeed) m.Velocity.Y =  GameConstants.MaxVerticalSpeed;
        if (m.Velocity.Y < -GameConstants.MaxVerticalSpeed) m.Velocity.Y = -GameConstants.MaxVerticalSpeed;

        // ---- physical move; velocity becomes ACTUAL displacement/dt, exactly like reading
        //      CharacterController.velocity back after Move() (collisions kill velocity) ----
        Vector3 before = m.Position;
        m.Position = world.CollideAndSlide(before, m.Velocity * dt);
        m.Velocity = (m.Position - before) * (1f / dt);

        // ---- grounded update with grace (original _ungroundedCount hysteresis) ----
        bool groundContact = world.CheckGrounded(m.Position);
        if (groundContact)
        {
            m.UngroundedTicks = 0;
            m.Grounded = true;
            m.Jumping  = false; // original clears JUMPING once GROUNDED
        }
        else if (m.Jumping)
        {
            if (m.UngroundedTicks < byte.MaxValue) m.UngroundedTicks++;
            m.Grounded = false; // jumping un-grounds immediately
        }
        else if (m.UngroundedTicks >= GameConstants.GroundedGraceTicks)
        {
            m.Grounded = false; // grace expired (walked off a ledge / downhill jitter over)
        }
        else
        {
            if (m.UngroundedTicks < byte.MaxValue) m.UngroundedTicks++;
            m.Grounded = true;  // still within grace
        }

        m.Yaw   = cmd.Yaw;
        m.Pitch = cmd.Pitch;
    }

    /// <summary>Server-owned modifier × duck scale, floored (original SpeedModifier, min 0.5).</summary>
    private static float EffectiveSpeedScale(in MoveState m)
    {
        float scale = m.SpeedScale <= 0f ? 1f : m.SpeedScale; // default(MoveState) reads as 1
        if (m.Grounded && m.Ducked) scale *= GameConstants.DuckSpeedScale;
        if (scale < GameConstants.MinSpeedScale) scale = GameConstants.MinSpeedScale;
        if (scale > 1f) scale = 1f;
        return scale;
    }

    private static void GroundMove(ref MoveState m, in InputCmd cmd, float dt, float speedScale)
    {
        Vector3 wish = ClampMagnitudeXZ(cmd.MoveDir);
        ApplyFriction(ref m, wish, dt, grounded: true);

        // original MoveOnGround normalizes wishDir (direction only; speed comes from us)
        Vector3 wishDir = NormalizeXZ(wish);
        ApplyAcceleration(ref m, wishDir, GameConstants.WalkSpeed * speedScale,
                          GameConstants.GroundAcceleration, dt);

        // constant stick-to-ground gravity (original: _currentVelocity[1] = -Gravity, where
        // Gravity is pre-multiplied by dt)
        m.Velocity.Y = -GameConstants.Gravity * dt;
    }

    private static void AirMove(ref MoveState m, in InputCmd cmd, float dt)
    {
        Vector3 wish = ClampMagnitudeXZ(cmd.MoveDir);
        ApplyFriction(ref m, wish, dt, grounded: false); // no ground friction in air; only the
                                                         // tiny-speed full stop can apply

        // original MoveInAir: wishDir NOT normalized, FULL walk speed (no duck/zoom modifier)
        ApplyAcceleration(ref m, wish, GameConstants.WalkSpeed, GameConstants.AirAcceleration, dt);

        m.Velocity.Y -= GameConstants.Gravity * dt;
    }

    /// <summary>
    /// Original ApplyFriction: drop = max(StopSpeed, speed) * GroundFriction * dt while
    /// grounded; nothing in air. Below FullStopSpeed with no input the original zeroed
    /// horizontal velocity outright (its check used the previous frame's acceleration; we
    /// use the current wish, which is the same intent without dragging extra state).
    /// </summary>
    private static void ApplyFriction(ref MoveState m, in Vector3 wish, float dt, bool grounded)
    {
        float speed = Len3(m.Velocity);
        if (speed == 0f) return;

        bool noInput = wish.X == 0f && wish.Z == 0f;
        if (speed < GameConstants.FullStopSpeed && noInput)
        {
            m.Velocity.X = 0f;
            m.Velocity.Z = 0f;
            return;
        }

        if (!grounded) return;

        float control = speed > GameConstants.StopSpeed ? speed : GameConstants.StopSpeed;
        float drop    = control * GameConstants.GroundFriction * dt;

        float newSpeed = speed - drop;
        if (newSpeed < 0f) newSpeed = 0f;
        m.Velocity *= newSpeed / speed;
    }

    /// <summary>
    /// Original ApplyAcceleration (Quake style): the dot-product gate means acceleration
    /// stops once velocity-along-wish reaches wishSpeed — but speed gained PERPENDICULAR to
    /// wish is never destroyed. That asymmetry IS air-strafing and bunny-hopping; keep it.
    ///
    /// One deliberate fixed-tick adaptation: the original adds the full accel*wishspeed*dt
    /// each step without capping it at addSpeed. At its per-render-frame dt (~1/200s) that
    /// uncapped add is tiny and converges to wishSpeed; at our 30 Hz tick it overshoots by
    /// ~40% and oscillates (friction and acceleration leapfrogging each other). Capping the
    /// add at addSpeed — the canonical Q3 formula the original approximates at high fps —
    /// restores the retail steady-state speed (7 u/s) at any tick rate.
    /// </summary>
    private static void ApplyAcceleration(ref MoveState m, in Vector3 wishDir, float wishSpeed,
                                          float accel, float dt)
    {
        float currentSpeed = Dot3(m.Velocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;

        float accelSpeed = accel * wishSpeed * dt;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;

        m.Velocity += wishDir * accelSpeed;
    }

    // ---------------------------------------------------------------------------------------
    // Determinism-constrained vector helpers. Fixed association order; sqrt only.
    // Do NOT replace with Vector3.Length()/Normalize()/Dot() — SIMD horizontal reductions
    // may associate additions differently across platforms (see docs/determinism.md).
    // ---------------------------------------------------------------------------------------

    public static float Dot3(in Vector3 a, in Vector3 b)
        => ((a.X * b.X) + (a.Y * b.Y)) + (a.Z * b.Z);

    public static float Len3(in Vector3 v)
        => MathF.Sqrt(((v.X * v.X) + (v.Y * v.Y)) + (v.Z * v.Z));

    /// <summary>Cross product, component-wise (no horizontal reduction — determinism-safe).</summary>
    public static Vector3 Cross3(in Vector3 a, in Vector3 b)
        => new((a.Y * b.Z) - (a.Z * b.Y),
               (a.Z * b.X) - (a.X * b.Z),
               (a.X * b.Y) - (a.Y * b.X));

    /// <summary>Full 3D normalize using the fixed-order Len3. Returns zero for a zero vector.</summary>
    public static Vector3 Normalize3(in Vector3 v)
    {
        float len = Len3(v);
        if (len <= 0f) return Vector3.Zero;
        float inv = 1f / len;
        return new Vector3(v.X * inv, v.Y * inv, v.Z * inv);
    }

    /// <summary>Direction-only normalize on the XZ plane (Y forced to 0).</summary>
    public static Vector3 NormalizeXZ(in Vector3 v)
    {
        float lenSq = (v.X * v.X) + (v.Z * v.Z);
        if (lenSq <= 0f) return Vector3.Zero;
        float inv = 1f / MathF.Sqrt(lenSq);
        return new Vector3(v.X * inv, 0f, v.Z * inv);
    }

    /// <summary>Clamp an XZ intent vector to unit magnitude (Y stripped). Client input guard.</summary>
    public static Vector3 ClampMagnitudeXZ(in Vector3 v)
    {
        float lenSq = (v.X * v.X) + (v.Z * v.Z);
        if (lenSq <= 1f) return new Vector3(v.X, 0f, v.Z);
        float inv = 1f / MathF.Sqrt(lenSq);
        return new Vector3(v.X * inv, 0f, v.Z * inv);
    }

    /// <summary>
    /// Forward direction from yaw/pitch (radians). Right-handed, +Z forward at yaw 0.
    /// Uses sin/cos, so it is NOT determinism-safe across platforms — combat/aim use on the
    /// SERVER only; never call from Step().
    /// </summary>
    public static Vector3 DirFromAngles(float yaw, float pitch)
    {
        float cy = MathF.Cos(yaw), sy = MathF.Sin(yaw);
        float cp = MathF.Cos(pitch), sp = MathF.Sin(pitch);
        return Vector3.Normalize(new Vector3(sy * cp, -sp, cy * cp));
    }
}
