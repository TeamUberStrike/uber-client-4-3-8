using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// The anti-cheat centerpiece. Every shot: state gate -> consume -> server origin/aim ->
/// server spread -> lag-comp rewind -> server raycast -> server LOS -> server damage -> score.
/// The client only ever sends "I pressed fire on slot N at tick T".
/// </summary>
public sealed class CombatSystem
{
    private readonly ICollisionWorld _world;
    private readonly Func<IEnumerable<PlayerState>> _players;
    private readonly Action<HitEvent> _broadcast;

    public CombatSystem(ICollisionWorld world, Func<IEnumerable<PlayerState>> players, Action<HitEvent> broadcast)
    {
        _world = world; _players = players; _broadcast = broadcast;
    }

    public void HandleFire(PlayerState shooter, in FireIntent f, double serverNow)
    {
        if (f.Slot != shooter.ActiveSlot) return;                 // can't fire a stowed weapon
        WeaponRuntime w = shooter.ActiveWeapon;
        if (!WeaponTable.TryGet(w.WeaponId, out WeaponDef def)) return;

        // (1) STATE GATE — defeats rapidfire / no-reload / infinite ammo.
        if (w.Reloading || w.Ammo <= 0) return;
        if (serverNow < w.NextFireTime) { shooter.Anomaly.Bump(AnomalyKind.FireRate, 0.5f); return; }

        // (2) CONSUME — server owns ammo and the firerate clock.
        w.Ammo--;
        w.NextFireTime = serverNow + def.FireInterval;

        // (3) ORIGIN + AIM are OURS — never a client-supplied origin (kills shoot-through-walls).
        Vector3 origin = shooter.Move.Position + new Vector3(0f, GameConstants.EyeHeight, 0f);
        shooter.Anomaly.ObserveAimDelta(serverNow, shooter.Move.Yaw, shooter.Move.Pitch);

        // (4) SPREAD applied server-side — "no-spread" hacks only desync the cheater's view.
        // Seeded ONLY from server state: seeding from f.ClientTick let a modified client grind
        // candidate ticks offline until it found one whose spread ≈ 0 (Phase A audit finding).
        (float yaw, float pitch) = ApplyServerSpread(shooter, def, serverNow);
        Vector3 aim = SharedMovement.DirFromAngles(yaw, pitch);

        // (5) REWIND window, clamped — can't claim ancient ticks.
        double rttHalf  = Math.Clamp(shooter.SmoothedRtt * 0.5, 0d, GameConstants.MaxRewindSeconds);
        double viewTime = serverNow - GameConstants.InterpDelaySeconds - rttHalf;
        viewTime = Math.Max(viewTime, serverNow - GameConstants.MaxRewindSeconds);

        // (6) RAYCAST vs rewound hitboxes.
        PlayerState? best = null; float bestT = float.MaxValue; bool bestHead = false; Vector3 bestPoint = default;
        foreach (PlayerState t in _players())
        {
            if (ReferenceEquals(t, shooter) || !t.Alive) continue;
            if (!t.History.Rewind(viewTime, out Vector3 basePos, out _)) basePos = t.Move.Position;

            Vector3 head  = basePos + new Vector3(0f, GameConstants.HeadOffset, 0f);
            Vector3 bodyA = basePos + new Vector3(0f, GameConstants.BodyBottom, 0f);
            Vector3 bodyB = basePos + new Vector3(0f, GameConstants.BodyTop,    0f);

            // Test head and body; keep nearest forward hit across all targets.
            if (Geometry.RaySphere(origin, aim, head, GameConstants.HeadRadius, def.Range, out float th) && th < bestT)
            { best = t; bestT = th; bestHead = true; bestPoint = origin + aim * th; }
            if (Geometry.RayCapsule(origin, aim, bodyA, bodyB, GameConstants.BodyRadius, def.Range, out float tb) && tb < bestT)
            { best = t; bestT = tb; bestHead = false; bestPoint = origin + aim * tb; }
        }

        if (best is null) { shooter.Anomaly.RecordShot(false, false); return; }

        // (7) SERVER LOS against present geometry — kills wallbang / ESP-firing.
        if (!_world.LineOfSight(origin, bestPoint))
        { shooter.Anomaly.Bump(AnomalyKind.WallShot, 0.7f); shooter.Anomaly.RecordShot(false, false); return; }

        // (8) SERVER DAMAGE — client never reports damage or hits.
        float dmg = def.BaseDamage * def.RangeFalloff(bestT) * (bestHead ? def.HeadshotMult : 1f);
        dmg = ApplyArmor(best, dmg);
        best.Health -= dmg;
        bool killed = best.Health <= 0f;
        shooter.Anomaly.RecordShot(true, bestHead);

        _broadcast(new HitEvent
        {
            Shooter = shooter.EntityId, Target = best.EntityId,
            Damage = dmg, Headshot = bestHead, Killed = killed, Point = bestPoint,
        });

        // (9) SERVER SCORE.
        if (killed) ResolveKill(shooter, best);
    }

    private static float ApplyArmor(PlayerState t, float dmg)
    {
        if (t.Armor <= 0f) return dmg;
        float absorbed = MathF.Min(t.Armor, dmg * 0.5f);
        t.Armor -= absorbed;
        return dmg - absorbed;
    }

    private static void ResolveKill(PlayerState shooter, PlayerState victim)
    {
        shooter.Kills++;
        victim.Deaths++;
        shooter.Currency += 100; // server-awarded; client cannot inject this
    }

    // Deterministic server spread so the result is reproducible and not client-controlled.
    // Every seed component is server-owned (entity, server tick of the shot, server shot count).
    private static (float yaw, float pitch) ApplyServerSpread(PlayerState s, WeaponDef def, double serverNow)
    {
        float spreadRad = def.MaxSpreadDeg * (MathF.PI / 180f);
        if (spreadRad <= 0f) return (s.Move.Yaw, s.Move.Pitch);

        uint serverTick = (uint)(serverNow * GameConstants.TickRate);
        uint seed = (uint)(s.EntityId * 73856093) ^ (serverTick * 19349663u) ^ ((uint)s.Anomaly.Shots * 83492791u);
        if (seed == 0u) seed = 0x9E3779B9u;
        float rx = NextUnit(ref seed);
        float ry = NextUnit(ref seed);
        return (s.Move.Yaw + rx * spreadRad, s.Move.Pitch + ry * spreadRad);
    }

    private static float NextUnit(ref uint state)
    {
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        return (state / (float)uint.MaxValue) * 2f - 1f;
    }
}
