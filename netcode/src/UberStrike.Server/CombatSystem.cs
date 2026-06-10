using System.Numerics;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>A server-simulated projectile in flight (splatter/cannon).</summary>
public sealed class Projectile
{
    public int     Owner;
    public int     WeaponId;
    public Vector3 Pos;
    public Vector3 Vel;
    public double  DieTime;
    public bool    Dead;
}

/// <summary>
/// The anti-cheat centerpiece. Every shot: switch-ready gate → state gate → consume →
/// server origin/aim → server spread → lag-comp rewind → server raycast (multi-part hitbox)
/// → server LOS → server damage → score. Shotguns fire N server-spread pellets; projectile
/// weapons spawn a server-simulated round that detonates with LOS-gated splash + knockback.
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

    /// <summary>Validated weapon switch: arms the per-weapon switch delay (anti quick-switch-exploit).</summary>
    public void HandleSwitch(PlayerState s, int slot, double serverNow)
    {
        if (slot < 0 || slot >= s.Weapons.Length || slot == s.ActiveSlot) return;
        s.ActiveSlot = slot;
        WeaponDef def = WeaponTable.Get(s.ActiveWeapon.WeaponId);
        s.SwitchReadyTime = serverNow + def.SwitchDelay;
    }

    /// <summary>Returns a spawned projectile to enqueue, or null for hitscan/shotgun/rejected shots.</summary>
    public Projectile? HandleFire(PlayerState shooter, in FireIntent f, double serverNow)
    {
        if (f.Slot != shooter.ActiveSlot) return null;            // can't fire a stowed weapon
        WeaponRuntime w = shooter.ActiveWeapon;
        if (!WeaponTable.TryGet(w.WeaponId, out WeaponDef def)) return null;

        // (0) SWITCH GATE — a freshly-selected weapon can't fire instantly (quick-switch exploit).
        if (serverNow < shooter.SwitchReadyTime) { shooter.Anomaly.Bump(AnomalyKind.FireRate, 0.3f); return null; }

        // (1) STATE GATE — defeats rapidfire / no-reload / infinite ammo.
        if (w.Reloading || w.Ammo <= 0) return null;
        if (serverNow < w.NextFireTime) { shooter.Anomaly.Bump(AnomalyKind.FireRate, 0.5f); return null; }

        // (2) CONSUME — server owns ammo and the firerate clock (one trigger pull = one ammo).
        w.Ammo--;
        w.NextFireTime = serverNow + def.FireInterval;
        shooter.LastFireTime = serverNow; // fog-of-war fire-reveal (only a REAL shot reveals)

        // (3) ORIGIN + AIM are OURS — never a client-supplied origin (kills shoot-through-walls).
        Vector3 origin = shooter.Move.Position + new Vector3(0f, GameConstants.EyeHeight, 0f);
        shooter.Anomaly.ObserveAimDelta(serverNow, shooter.Move.Yaw, shooter.Move.Pitch);

        if (def.Kind == WeaponKind.Projectile)
        {
            // (4p) spawn a server-simulated round; detonation/damage happens as it travels.
            (float py, float pp) = ApplyServerSpread(shooter, def, serverNow, 0);
            Vector3 pdir = SharedMovement.DirFromAngles(py, pp);
            return new Projectile
            {
                Owner = shooter.EntityId, WeaponId = def.Id,
                Pos = origin, Vel = pdir * def.ProjectileSpeed,
                DieTime = serverNow + def.ProjectileLife,
            };
        }

        // Hitscan / shotgun: 1 or N pellets, each with independent server spread.
        double viewTime = RewindTime(shooter, serverNow);
        int pellets = def.Kind == WeaponKind.Shotgun ? def.Pellets : 1;

        // accumulate damage per target so one HitEvent per victim carries the shell's total
        var dmgByTarget = new Dictionary<int, (PlayerState t, float dmg, bool head, Vector3 pt)>();
        for (int p = 0; p < pellets; p++)
        {
            (float yaw, float pitch) = ApplyServerSpread(shooter, def, serverNow, p);
            Vector3 aim = SharedMovement.DirFromAngles(yaw, pitch);

            if (!RaycastPlayers(shooter, origin, aim, def, viewTime,
                                out PlayerState? hitT, out float dist, out bool head, out Vector3 point)
                || hitT is null)
            { shooter.Anomaly.RecordShot(false, false); continue; }

            // (7) SERVER LOS — kills wallbang / ESP-firing.
            if (!_world.LineOfSight(origin, point))
            { shooter.Anomaly.Bump(AnomalyKind.WallShot, 0.5f); shooter.Anomaly.RecordShot(false, false); continue; }

            // (8) SERVER DAMAGE — multi-part multiplier × falloff.
            float partMult = head ? def.HeadshotMult : PartMultAt(point.Y - hitT.Move.Position.Y);
            float dmg = def.BaseDamage * def.RangeFalloff(dist) * partMult;
            shooter.Anomaly.RecordShot(true, head);

            if (dmgByTarget.TryGetValue(hitT.EntityId, out var acc))
                dmgByTarget[hitT.EntityId] = (hitT, acc.dmg + dmg, acc.head || head, point);
            else
                dmgByTarget[hitT.EntityId] = (hitT, dmg, head, point);
        }

        foreach (var kv in dmgByTarget)
        {
            (PlayerState t, float raw, bool head, Vector3 pt) = kv.Value;
            float dmg = ApplyArmor(t, raw);
            t.Health -= dmg;
            bool killed = t.Health <= 0f;
            _broadcast(new HitEvent
            {
                Shooter = shooter.EntityId, Target = t.EntityId,
                Damage = dmg, Headshot = head, Killed = killed, Point = pt,
            });
            if (killed) ResolveKill(shooter, t);
        }
        return null;
    }

    /// <summary>Advance + resolve a projectile for one tick; returns false once it's spent.</summary>
    public bool StepProjectile(Projectile pr, float dt, double serverNow)
    {
        if (pr.Dead) return false;
        if (!WeaponTable.TryGet(pr.WeaponId, out WeaponDef def)) return false;

        Vector3 step = pr.Vel * dt;
        float stepLen = SharedMovement.Len3(step);
        Vector3 dir = stepLen > 1e-6f ? new Vector3(step.X / stepLen, step.Y / stepLen, step.Z / stepLen) : Vector3.Zero;

        // direct hit on a player this step?
        float bestT = stepLen; bool hitSomething = false; Vector3 impact = pr.Pos + step;
        foreach (PlayerState t in _players())
        {
            if (t.EntityId == pr.Owner || !t.Alive) continue;
            Vector3 a = t.Move.Position + new Vector3(0f, GameConstants.BodyBottom, 0f);
            Vector3 b = t.Move.Position + new Vector3(0f, GameConstants.BodyTop, 0f);
            if (Geometry.RayCapsule(pr.Pos, dir, a, b, GameConstants.BodyRadius, bestT, out float th) && th < bestT)
            { bestT = th; hitSomething = true; impact = pr.Pos + dir * th; }
        }
        // world hit this step?
        if (_world.Raycast(pr.Pos, dir, bestT, out float wt) && wt < bestT)
        { bestT = wt; hitSomething = true; impact = pr.Pos + dir * wt; }

        if (hitSomething) { Detonate(pr.Owner, def, impact, serverNow); pr.Dead = true; return false; }

        pr.Pos += step;
        if (serverNow >= pr.DieTime) { Detonate(pr.Owner, def, pr.Pos, serverNow); pr.Dead = true; return false; }
        return true;
    }

    /// <summary>Splash damage + knockback to everyone within radius WITH line-of-sight to the blast.</summary>
    public void Detonate(int ownerId, WeaponDef def, Vector3 center, double serverNow)
    {
        PlayerState? owner = null;
        foreach (PlayerState p in _players()) if (p.EntityId == ownerId) { owner = p; break; }

        foreach (PlayerState t in _players())
        {
            if (!t.Alive) continue;

            Vector3 mid = t.Move.Position + new Vector3(0f, (GameConstants.BodyBottom + GameConstants.BodyTop) * 0.5f, 0f);
            Vector3 to = mid - center;
            float d = SharedMovement.Len3(to);
            if (d > def.SplashRadius) continue;
            if (!_world.LineOfSight(center, mid)) continue;       // a wall shields you from the blast

            float k = 1f - d / def.SplashRadius;                  // linear falloff
            float dmg = ApplyArmor(t, def.BaseDamage * k);
            t.Health -= dmg;

            // knockback impulse (= rocket jump for the owner): away from the blast
            Vector3 push = d > 1e-4f ? new Vector3(to.X / d, to.Y / d, to.Z / d) : new Vector3(0f, 1f, 0f);
            t.Move.ApplyForce(push * (def.SplashImpulse * k), ForceMode.Additive);

            bool killed = t.Health <= 0f;
            _broadcast(new HitEvent
            {
                Shooter = ownerId, Target = t.EntityId,
                Damage = dmg, Headshot = false, Killed = killed, Point = center,
            });
            if (killed && owner != null && t.EntityId != ownerId) ResolveKill(owner, t);
        }
    }

    // --- helpers --------------------------------------------------------------------------

    private static double RewindTime(PlayerState shooter, double serverNow)
    {
        double rttHalf  = Math.Clamp(shooter.SmoothedRtt * 0.5, 0d, GameConstants.MaxRewindSeconds);
        double viewTime = serverNow - GameConstants.InterpDelaySeconds - rttHalf;
        return Math.Max(viewTime, serverNow - GameConstants.MaxRewindSeconds);
    }

    /// <summary>Nearest forward hit across all targets, tested against head + torso + legs parts.</summary>
    private bool RaycastPlayers(PlayerState shooter, Vector3 origin, Vector3 aim, WeaponDef def,
        double viewTime, out PlayerState? best, out float bestT, out bool bestHead, out Vector3 bestPoint)
    {
        best = null; bestT = float.MaxValue; bestHead = false; bestPoint = default;
        foreach (PlayerState t in _players())
        {
            if (ReferenceEquals(t, shooter) || !t.Alive) continue;
            if (!t.History.Rewind(viewTime, out Vector3 basePos, out _)) basePos = t.Move.Position;

            // head sphere
            Vector3 head = basePos + new Vector3(0f, GameConstants.HeadOffset, 0f);
            if (Geometry.RaySphere(origin, aim, head, GameConstants.HeadRadius, def.Range, out float th) && th < bestT)
            { best = t; bestT = th; bestHead = true; bestPoint = origin + aim * th; }

            // torso capsule
            Vector3 tA = basePos + new Vector3(0f, GameConstants.TorsoBottom, 0f);
            Vector3 tB = basePos + new Vector3(0f, GameConstants.BodyTop, 0f);
            if (Geometry.RayCapsule(origin, aim, tA, tB, GameConstants.BodyRadius, def.Range, out float tt) && tt < bestT)
            { best = t; bestT = tt; bestHead = false; bestPoint = origin + aim * tt; }

            // legs capsule
            Vector3 lA = basePos + new Vector3(0f, GameConstants.BodyBottom, 0f);
            Vector3 lB = basePos + new Vector3(0f, GameConstants.LegsTop, 0f);
            if (Geometry.RayCapsule(origin, aim, lA, lB, GameConstants.BodyRadius, def.Range, out float tl) && tl < bestT)
            { best = t; bestT = tl; bestHead = false; bestPoint = origin + aim * tl; }
        }
        return best != null;
    }

    /// <summary>Non-head damage multiplier from the hit height relative to the target's feet.</summary>
    private static float PartMultAt(float localY)
        => localY < GameConstants.LegsTop ? GameConstants.LegsMult : GameConstants.TorsoMult;

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

    // Deterministic server spread; every seed component is server-owned (entity, server tick,
    // server shot count, pellet index) — nothing client-controlled, so no seed-grinding.
    private static (float yaw, float pitch) ApplyServerSpread(PlayerState s, WeaponDef def, double serverNow, int pellet)
    {
        float spreadRad = def.MaxSpreadDeg * (MathF.PI / 180f);
        if (spreadRad <= 0f) return (s.Move.Yaw, s.Move.Pitch);

        uint serverTick = (uint)(serverNow * GameConstants.TickRate);
        uint seed = (uint)(s.EntityId * 73856093) ^ (serverTick * 19349663u)
                  ^ ((uint)s.Anomaly.Shots * 83492791u) ^ ((uint)(pellet + 1) * 2654435761u);
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
