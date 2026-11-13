using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Phase 8 — server-side crosshair tracking that feeds the triggerbot reaction-time signal.
/// Each tick the server notes whether a player's aim ray is on a visible enemy; CombatSystem
/// reads the acquire timestamp when a REAL shot fires and samples acquire→fire reaction time.
/// All inputs are server-owned (server aim state, server positions, server LOS).
/// </summary>
public static class AimWatch
{
    /// <summary>~3° half-angle cone counts as "on target" (generous: includes near-misses a
    /// triggerbot would fire on).</summary>
    public const float ConeCos = 0.9986f;
    public const float MaxRange = 150f;
    /// <summary>Sustained acquire→fire below this is sub-human (humans bottom out ~150–250 ms).</summary>
    public const double ReactionFloorSeconds = 0.12;

    public static void Update(PlayerState p, IEnumerable<PlayerState> players,
                              ICollisionWorld world, double serverNow)
    {
        bool on = p.Alive && IsOnTarget(p, players, world);
        if (on && !p.WasOnTarget) p.AimAcquiredTime = serverNow;
        p.WasOnTarget = on;
    }

    private static bool IsOnTarget(PlayerState shooter, IEnumerable<PlayerState> players, ICollisionWorld world)
    {
        Vector3 eye = shooter.Move.Position + new Vector3(0f, GameConstants.EyeHeight, 0f);
        Vector3 aim = SharedMovement.DirFromAngles(shooter.Move.Yaw, shooter.Move.Pitch);

        foreach (PlayerState t in players)
        {
            if (ReferenceEquals(t, shooter) || !t.Alive) continue;
            if (shooter.TeamId != 0 && shooter.TeamId == t.TeamId) continue;

            Vector3 torso = t.Move.Position + new Vector3(0f, (GameConstants.BodyBottom + GameConstants.BodyTop) * 0.5f, 0f);
            Vector3 to = torso - eye;
            float dist = to.Length();
            if (dist < 1e-3f || dist > MaxRange) continue;

            float cos = Vector3.Dot(aim, to / dist);
            if (cos < ConeCos) continue;
            if (!world.LineOfSight(eye, torso)) continue;   // tracking a WALL is not "on target"
            return true;
        }
        return false;
    }
}
