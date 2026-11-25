using System.Numerics;
using UberStrike.Shared;
using UberStrike.Server;
using UberStrike.Client;

// Minimal zero-dependency test harness so it runs offline with just `dotnet run`.
int failures = 0;
void Check(bool cond, string name)
{
    Console.WriteLine($"[{(cond ? "PASS" : "FAIL")}] {name}");
    if (!cond) failures++;
}

var dt = GameConstants.FixedDt;

// ---------------------------------------------------------------------------------------
// Recorded input stream: 10,000 ticks of pseudo-random movement/jump/crouch from a fixed
// xorshift seed (pure uint math — no Math.random, no wall clock). The SAME list drives
// every determinism run below, i.e. "the same recorded input stream".
// ---------------------------------------------------------------------------------------
List<InputCmd> RecordStream(int ticks)
{
    uint rng = 0xC0FFEE42;
    float NextF()
    {
        rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;
        return ((rng & 0xFFFF) / 65535f) * 2f - 1f;
    }
    var list = new List<InputCmd>(ticks);
    for (uint i = 1; i <= ticks; i++)
    {
        float mx = NextF(), mz = NextF();
        float j = NextF(), c = NextF();
        list.Add(new InputCmd
        {
            Seq = i,
            ClientTick = i,
            MoveDir = new Vector3(mx, 0f, mz),       // Step clamps magnitude itself
            Jump   = j > 0.6f,
            Crouch = c > 0.7f,
            Yaw = mx * 3f, Pitch = mz * 1.5f,
        });
    }
    return list;
}

static uint StateBitsHash(in MoveState m)
{
    uint h = 2166136261u;
    void Mix(float f) { unchecked { h = (h ^ (uint)BitConverter.SingleToInt32Bits(f)) * 16777619u; } }
    Mix(m.Position.X); Mix(m.Position.Y); Mix(m.Position.Z);
    Mix(m.Velocity.X); Mix(m.Velocity.Y); Mix(m.Velocity.Z);
    unchecked { h = (h ^ (uint)((m.Grounded ? 1 : 0) | (m.Jumping ? 2 : 0) | (m.Ducked ? 4 : 0) | (m.JumpArmed ? 8 : 0) | (m.UngroundedTicks << 4))) * 16777619u; }
    return h;
}

static bool BitsEqual(in MoveState a, in MoveState b) =>
    BitConverter.SingleToInt32Bits(a.Position.X) == BitConverter.SingleToInt32Bits(b.Position.X) &&
    BitConverter.SingleToInt32Bits(a.Position.Y) == BitConverter.SingleToInt32Bits(b.Position.Y) &&
    BitConverter.SingleToInt32Bits(a.Position.Z) == BitConverter.SingleToInt32Bits(b.Position.Z) &&
    BitConverter.SingleToInt32Bits(a.Velocity.X) == BitConverter.SingleToInt32Bits(b.Velocity.X) &&
    BitConverter.SingleToInt32Bits(a.Velocity.Y) == BitConverter.SingleToInt32Bits(b.Velocity.Y) &&
    BitConverter.SingleToInt32Bits(a.Velocity.Z) == BitConverter.SingleToInt32Bits(b.Velocity.Z) &&
    a.Grounded == b.Grounded && a.Jumping == b.Jumping && a.Ducked == b.Ducked &&
    a.JumpArmed == b.JumpArmed && a.UngroundedTicks == b.UngroundedTicks;

var stream10k = RecordStream(10_000);

// ---------------------------------------------------------------------------------------
// 1. Phase-1 done-when: 10k ticks, BIT-identical end state across two independent runs.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    MoveState a = default, b = default; a.Grounded = b.Grounded = true;
    uint hashA = 0, hashB = 0;
    foreach (var cmd in stream10k) { SharedMovement.Step(ref a, cmd, dt, w); unchecked { hashA = hashA * 31 + StateBitsHash(a); } }
    foreach (var cmd in stream10k) { SharedMovement.Step(ref b, cmd, dt, w); unchecked { hashB = hashB * 31 + StateBitsHash(b); } }
    Check(BitsEqual(a, b) && hashA == hashB,
        $"10k-tick replay is BIT-identical across runs (trajectory hash 0x{hashA:X8})");
}

// ---------------------------------------------------------------------------------------
// 2. Client predict path == server authority path, bit-for-bit over 10k ticks.
//    (MovementSystem.Apply wraps the same Step; its guards must never perturb a legal run.)
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    MoveState client = default; client.Grounded = true;

    var server = new PlayerState { EntityId = 1, SessionToken = "t" };
    server.Move.Grounded = true;
    var sys = new MovementSystem(w);

    bool diverged = false;
    foreach (var cmd in stream10k)
    {
        SharedMovement.Step(ref client, cmd, dt, w);
        sys.Apply(server, cmd, dt);
        if (!BitsEqual(client, server.Move)) { diverged = true; break; }
    }
    Check(!diverged, "server authority path stays bit-identical to client prediction for 10k ticks");
    Check(server.Anomaly.Score == 0f, "speed/teleport guard never fires on a legal 10k-tick run");
}

// ---------------------------------------------------------------------------------------
// 3. Desync detector: silent on a clean run, alarms when the sims genuinely differ.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var pred = new PredictionClient(w, 1, "t", Vector3.Zero);
    MoveState srv = default; srv.Grounded = true;

    // clean: server runs the same world
    for (int i = 0; i < 100; i++)
    {
        var pkt = pred.BuildAndPredict(new InputCmd { MoveDir = new Vector3(0, 0, 1) });
        SharedMovement.Step(ref srv, pkt.Cmd, dt, w);
        pred.Reconcile(MakeSnap(srv, pkt.Cmd.Seq));
    }
    Check(pred.DesyncAlarms == 0, "desync detector silent when client and server sims agree");

    // broken: server world has different geometry (stand-in for divergent movement code)
    var w2 = new FlatCollisionWorld { GroundY = 0.5f };
    var pred2 = new PredictionClient(w, 2, "t", Vector3.Zero);
    MoveState srv2 = default; srv2.Grounded = true; srv2.Position = new Vector3(0, 0.5f, 0);
    for (int i = 0; i < 100; i++)
    {
        var pkt = pred2.BuildAndPredict(new InputCmd { MoveDir = new Vector3(1, 0, 0), Jump = i % 9 == 0 });
        SharedMovement.Step(ref srv2, pkt.Cmd, dt, w2);
        pred2.Reconcile(MakeSnap(srv2, pkt.Cmd.Seq));
    }
    Check(pred2.DesyncAlarms > 0, "desync detector alarms when the two sims run different geometry");
}

// ---------------------------------------------------------------------------------------
// 4. Reconciliation convergence: replay of unacked inputs lands back on the prediction.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var pred = new PredictionClient(w, 1, "t", Vector3.Zero);
    var cmds = new List<InputCmd>();
    for (int i = 1; i <= 10; i++)
        cmds.Add(pred.BuildAndPredict(new InputCmd { MoveDir = new Vector3(0, 0, 1), Yaw = 0.2f }).Cmd);

    MoveState srv = default; srv.Grounded = true;
    for (int i = 0; i < 6; i++) SharedMovement.Step(ref srv, cmds[i], dt, w);

    pred.Reconcile(MakeSnap(srv, 6));
    Check(pred.LastReconcileError < 1e-3f, $"reconciliation converges (err={pred.LastReconcileError:E2})");
}

// ---------------------------------------------------------------------------------------
// 5. Real-movement behaviors: duck scale, jump edge trigger, bunny-hop speed retention.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();

    // duck: steady-state ground speed scales by 0.7
    MoveState walk = default; walk.Grounded = true;
    MoveState duck = default; duck.Grounded = true;
    var fwd  = new InputCmd { MoveDir = new Vector3(0, 0, 1) };
    var fwdD = new InputCmd { MoveDir = new Vector3(0, 0, 1), Crouch = true };
    for (int i = 0; i < 90; i++) { SharedMovement.Step(ref walk, fwd, dt, w); SharedMovement.Step(ref duck, fwdD, dt, w); }
    float walkSpeed = new Vector3(walk.Velocity.X, 0, walk.Velocity.Z).Length();
    float duckSpeed = new Vector3(duck.Velocity.X, 0, duck.Velocity.Z).Length();
    Check(MathF.Abs(walkSpeed - GameConstants.WalkSpeed) < 0.3f, $"steady walk reaches ~{GameConstants.WalkSpeed} u/s ({walkSpeed:F2})");
    Check(MathF.Abs(duckSpeed - GameConstants.WalkSpeed * GameConstants.DuckSpeedScale) < 0.3f, $"ducked walk reaches ~{GameConstants.WalkSpeed * GameConstants.DuckSpeedScale:F2} u/s ({duckSpeed:F2})");

    // jump edge trigger: HOLDING jump must not re-jump on landing; releasing re-arms.
    MoveState hop = default; hop.Grounded = true; hop.JumpArmed = true;
    var hold = new InputCmd { Jump = true };
    SharedMovement.Step(ref hop, hold, dt, w);
    Check(hop.Jumping && !hop.Grounded, "jump fires on press");
    // ride to landing while still holding
    int safety = 200;
    while (!hop.Grounded && safety-- > 0) SharedMovement.Step(ref hop, hold, dt, w);
    SharedMovement.Step(ref hop, hold, dt, w);
    Check(hop.Grounded && !hop.Jumping, "held jump does NOT auto re-jump on landing");
    SharedMovement.Step(ref hop, new InputCmd(), dt, w);   // release re-arms
    SharedMovement.Step(ref hop, hold, dt, w);
    Check(hop.Jumping, "released-then-pressed jump fires again");

    // bunny hop: a hop must not bleed speed to ground friction (jump skips the friction tick)
    MoveState bh = default; bh.Grounded = true;
    for (int i = 0; i < 90; i++) SharedMovement.Step(ref bh, fwd, dt, w); // reach full speed
    float before = new Vector3(bh.Velocity.X, 0, bh.Velocity.Z).Length();
    SharedMovement.Step(ref bh, new InputCmd { MoveDir = new Vector3(0, 0, 1), Jump = true }, dt, w); // hop
    float after = new Vector3(bh.Velocity.X, 0, bh.Velocity.Z).Length();
    Check(after >= before - 0.05f, $"bunny hop keeps horizontal speed ({before:F2} -> {after:F2})");
}

// ---------------------------------------------------------------------------------------
// 6. External force (jump pad / rocket jump): applied, exceeds walk speed, guard tolerates.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var sys = new MovementSystem(w);
    var ps = new PlayerState { EntityId = 1, SessionToken = "t" };
    ps.Move.Grounded = true;
    ps.Move.ApplyForce(new Vector3(20f, 10f, 0f), ForceMode.Additive);
    sys.Apply(ps, new InputCmd { Seq = 1 }, dt);
    float horiz = MathF.Abs(ps.Move.Velocity.X);
    Check(horiz > GameConstants.WalkSpeed, $"jump-pad impulse exceeds walk speed ({horiz:F1} u/s)");
    Check(ps.Anomaly.Score == 0f, "speed guard does not flag a legitimate external impulse");
}

// ---------------------------------------------------------------------------------------
// 7. Firerate gate + server-owned damage (combat invariants).
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var shooter = MakePlayer(1, 3, Vector3.Zero);              // sniper
    var target  = MakePlayer(2, 3, new Vector3(0, 0, 5));
    target.History.Record(0, target.Move.Position, 0);
    (shooter.Move.Yaw, shooter.Move.Pitch) = AimAt(shooter.Move.Position, target.Move.Position);
    var players = new List<PlayerState> { shooter, target };

    int hitCount = 0;
    var combat = new CombatSystem(w, () => players, _ => hitCount++);

    int magStart = shooter.ActiveWeapon.Ammo;
    float hpStart = target.Health;

    combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = 1 }, 1.00);
    int ammoAfter1 = shooter.ActiveWeapon.Ammo;
    float hpAfter1 = target.Health;

    combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = 2 }, 1.05);
    int ammoAfter2 = shooter.ActiveWeapon.Ammo;

    Check(ammoAfter1 == magStart - 1,         "first shot consumes exactly one ammo");
    Check(ammoAfter2 == ammoAfter1,           "second shot within firerate window is rejected");
    Check(hpAfter1 < hpStart,                 "server applies damage to target health");
    Check(hitCount == 1,                      "exactly one authoritative hit event emitted");
}

// ---------------------------------------------------------------------------------------
// 8. Spread is NOT client-influenceable: ClientTick must not change the shot outcome.
// ---------------------------------------------------------------------------------------
{
    float HpAfterShot(uint clientTick)
    {
        var w = new FlatCollisionWorld();
        var shooter = MakePlayer(1, 1, Vector3.Zero);          // machine gun (has spread)
        var target  = MakePlayer(2, 1, new Vector3(0, 0, 20));
        target.History.Record(0, target.Move.Position, 0);
        (shooter.Move.Yaw, shooter.Move.Pitch) = AimAt(shooter.Move.Position, target.Move.Position);
        var players = new List<PlayerState> { shooter, target };
        var combat = new CombatSystem(w, () => players, _ => { });
        combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = clientTick }, 1.00);
        return target.Health;
    }
    Check(HpAfterShot(1) == HpAfterShot(987654), "ClientTick cannot influence server spread (seed-grind defense)");
}

// ---------------------------------------------------------------------------------------
// 9. Speed-hack clamp: a giant MoveDir cannot exceed the max walk step.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var sys = new MovementSystem(w);
    var ps = MakePlayer(1, 1, Vector3.Zero);
    Vector3 before = ps.Move.Position;
    sys.Apply(ps, new InputCmd { Seq = 1, MoveDir = new Vector3(1000, 0, 0) }, dt);
    float horiz = new Vector3(ps.Move.Position.X - before.X, 0, ps.Move.Position.Z - before.Z).Length();
    float maxStep = (GameConstants.WalkSpeed + GameConstants.ExtraSpeedTolerance) * dt;
    Check(horiz <= maxStep + 1e-3f, $"oversized MoveDir clamped to max walk step ({horiz:F3} <= {maxStep:F3})");
}

// ---------------------------------------------------------------------------------------
// 10. Gateway rejects bad auth / replay / schema; fire intents need the session token.
// ---------------------------------------------------------------------------------------
{
    var ps = MakePlayer(1, 1, Vector3.Zero);
    var ok    = new InputPacket { EntityId = 1, SessionToken = ps.SessionToken, Cmd = new InputCmd { Seq = 1, MoveDir = Vector3.Zero } };
    var dupe  = new InputPacket { EntityId = 1, SessionToken = ps.SessionToken, Cmd = new InputCmd { Seq = 1, MoveDir = Vector3.Zero } };
    var spoof = new InputPacket { EntityId = 1, SessionToken = "wrong",          Cmd = new InputCmd { Seq = 2, MoveDir = Vector3.Zero } };
    var huge  = new InputPacket { EntityId = 1, SessionToken = ps.SessionToken, Cmd = new InputCmd { Seq = 3, MoveDir = new Vector3(5, 0, 0) } };

    Check(InputGateway.Validate(ps, ok,    out _),  "valid first input accepted");
    Check(!InputGateway.Validate(ps, dupe,  out _), "replayed/old sequence rejected");
    Check(!InputGateway.Validate(ps, spoof, out _), "spoofed session token rejected");
    Check(!InputGateway.Validate(ps, huge,  out _), "out-of-range MoveDir rejected");

    var w = new FlatCollisionWorld();
    var sim = new ServerSimulation(w, (_, _) => { }, _ => { });
    sim.AddPlayer(1, "tok-1", Vector3.Zero, 1);
    Check(!sim.EnqueueFire(new FireIntent { EntityId = 1, SessionToken = "stolen", Slot = 0 }),
        "fire intent with a forged/missing session token is rejected");
    Check(sim.EnqueueFire(new FireIntent { EntityId = 1, SessionToken = "tok-1", Slot = 0 }),
        "fire intent with the owner's session token is accepted");
}

// ---------------------------------------------------------------------------------------
// 11. Phase 4 — baked collision world: real LOS (closes wallbang) + wall slide + grounded.
// ---------------------------------------------------------------------------------------
{
    // A room: floor at y=0, a wall plane at x=5 (z in [-10,10], y in [0,4]), a pillar box
    // centred at (0,0,5) size (1,3,1).
    var v = new List<Vector3>(); var idx = new List<int>();
    Quad(v, idx, new Vector3(-10,0,-10), new Vector3(10,0,-10), new Vector3(10,0,10), new Vector3(-10,0,10)); // floor
    Quad(v, idx, new Vector3(5,0,-10), new Vector3(5,4,-10), new Vector3(5,4,10), new Vector3(5,0,10));       // wall x=5
    Box(v, idx, new Vector3(0,0,5), new Vector3(1,3,1));                                                       // pillar
    var room = new BakedCollisionWorld(new TriangleMesh(v.ToArray(), idx.ToArray()));

    Vector3 eye = new(0, 1.5f, 0);
    Check(room.LineOfSight(eye, new Vector3(0, 1.5f, 3)),  "LOS clear across open floor");
    Check(!room.LineOfSight(eye, new Vector3(0, 1.5f, 10)), "LOS blocked through the pillar (wallbang denied)");
    Check(!room.LineOfSight(eye, new Vector3(8, 1.5f, 0)),  "LOS blocked through the wall");
    Check(room.LineOfSight(new Vector3(-2,1.5f,0), new Vector3(2,1.5f,0)), "LOS clear with no geometry between");

    // Walk into the wall: must not tunnel through x=5.
    Vector3 stopped = room.CollideAndSlide(new Vector3(4.0f, 0, 0), new Vector3(2f, 0, 0));
    Check(stopped.X < 4.75f, $"CollideAndSlide stops the player at the wall (x={stopped.X:F2}, did not pass 5)");

    // Slide along the wall: pushing into it diagonally still advances along Z.
    Vector3 slid = room.CollideAndSlide(new Vector3(4.55f, 0, 0), new Vector3(1f, 0, 1f));
    Check(slid.X < 4.75f && slid.Z > 0.3f, $"CollideAndSlide slides along the wall (x={slid.X:F2}, z={slid.Z:F2})");

    Check(room.CheckGrounded(new Vector3(0, 0f, 0)), "grounded standing on the floor");
    Check(!room.CheckGrounded(new Vector3(0, 5f, 0)), "not grounded 5m above the floor");
    Check(room.HasHeadroom(new Vector3(-3, 0, 0)), "headroom to stand in the open");
}

// ---------------------------------------------------------------------------------------
// 12. Phase 4 — movement on a mesh world is bit-identical client vs server over 10k ticks,
//     and the .ubw binary round-trips losslessly.
// ---------------------------------------------------------------------------------------
{
    var v = new List<Vector3>(); var idx = new List<int>();
    Quad(v, idx, new Vector3(-1000,0,-1000), new Vector3(1000,0,-1000), new Vector3(1000,0,1000), new Vector3(-1000,0,1000));
    var verts = v.ToArray(); var indices = idx.ToArray();

    // round-trip the binary format
    using var ms = new MemoryStream();
    BakedCollisionWorld.Write(ms, verts, indices);
    ms.Position = 0;
    var loaded = BakedCollisionWorld.Load(ms);
    Check(loaded.Mesh.TriangleCount == indices.Length / 3, ".ubw round-trips the triangle count");

    var worldA = new BakedCollisionWorld(new TriangleMesh(verts, indices));
    var worldB = loaded; // built from the serialized bytes — must behave identically
    MoveState a = default, b = default; a.Grounded = b.Grounded = true;
    foreach (var cmd in stream10k) SharedMovement.Step(ref a, cmd, dt, worldA);
    foreach (var cmd in stream10k) SharedMovement.Step(ref b, cmd, dt, worldB);
    Check(BitsEqual(a, b), "10k-tick movement on a BAKED mesh world is bit-identical (fresh vs loaded)");
}

// ---------------------------------------------------------------------------------------
// 13. Phase 5 — multi-part hitboxes: a leg hit does less damage than a torso hit.
// ---------------------------------------------------------------------------------------
{
    float DamageAimingAt(float targetLocalY)
    {
        var w = new FlatCollisionWorld();
        var shooter = MakePlayer(1, 1, Vector3.Zero);            // machine gun, no headshot
        var target  = MakePlayer(2, 1, new Vector3(0, 0, 6));
        target.History.Record(0, target.Move.Position, 0);
        // aim at a specific height on the target
        Vector3 eye = shooter.Move.Position + new Vector3(0, GameConstants.EyeHeight, 0);
        Vector3 aimPt = target.Move.Position + new Vector3(0, targetLocalY, 0);
        Vector3 d = Vector3.Normalize(aimPt - eye);
        shooter.Move.Yaw = MathF.Atan2(d.X, d.Z);
        shooter.Move.Pitch = -MathF.Asin(Math.Clamp(d.Y, -1f, 1f));
        var players = new List<PlayerState> { shooter, target };
        var combat = new CombatSystem(w, () => players, _ => { });
        float before = target.Health;
        combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = 1 }, 1.0);
        return before - target.Health;
    }
    float torso = DamageAimingAt(1.3f);   // chest height
    float legs  = DamageAimingAt(0.5f);   // shin height
    Check(torso > 0 && legs > 0, "multi-part: both torso and leg shots register");
    Check(legs < torso, $"multi-part: leg hit does less damage than torso ({legs:F1} < {torso:F1})");
}

// ---------------------------------------------------------------------------------------
// 14. Phase 5 — weapon switch delay defeats quick-switch (fire-instantly) exploit.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var def1 = WeaponTable.Get(1); var def3 = WeaponTable.Get(3);
    var shooter = new PlayerState { EntityId = 1, SessionToken = "t",
        Weapons = new[] {
            new WeaponRuntime { WeaponId = 1, Ammo = def1.MagSize },
            new WeaponRuntime { WeaponId = 3, Ammo = def3.MagSize },
        } };
    shooter.Move.Grounded = true;
    var target = MakePlayer(2, 1, new Vector3(0, 0, 6)); target.History.Record(0, target.Move.Position, 0);
    (shooter.Move.Yaw, shooter.Move.Pitch) = AimAt(shooter.Move.Position, target.Move.Position);
    var players = new List<PlayerState> { shooter, target };
    int hits = 0;
    var combat = new CombatSystem(w, () => players, _ => hits++);

    combat.HandleSwitch(shooter, 1, 10.0);                       // switch to sniper at t=10
    combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 1, ClientTick = 1 }, 10.01); // instant
    Check(hits == 0, "switch delay: firing immediately after a switch is rejected");
    combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 1, ClientTick = 2 }, 10.0 + def3.SwitchDelay + 0.01);
    Check(hits == 1, "switch delay: firing after the switch delay is allowed");
}

// ---------------------------------------------------------------------------------------
// 15. Phase 5 — shotgun fires multiple pellets from ONE shell (more damage, one ammo).
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var shooter = MakePlayer(1, 2, Vector3.Zero);               // shotgun, 8 pellets
    var target  = MakePlayer(2, 2, new Vector3(0, 0, 4));       // close, wide target
    target.History.Record(0, target.Move.Position, 0);
    (shooter.Move.Yaw, shooter.Move.Pitch) = AimAt(shooter.Move.Position, target.Move.Position);
    var players = new List<PlayerState> { shooter, target };
    int events = 0; float total = 0;
    var combat = new CombatSystem(w, () => players, e => { events++; total += e.Damage; });
    int ammoBefore = shooter.ActiveWeapon.Ammo;
    combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = 1 }, 1.0);
    Check(shooter.ActiveWeapon.Ammo == ammoBefore - 1, "shotgun: one trigger pull consumes one shell");
    Check(events == 1, "shotgun: pellets aggregate into one hit event per victim");
    Check(total > WeaponTable.Get(2).BaseDamage, $"shotgun: multiple pellets landed (total {total:F0} > one pellet)");
}

// ---------------------------------------------------------------------------------------
// 16. Phase 5 — projectile: splash damage + rocket-jump impulse, and a wall shields splash.
// ---------------------------------------------------------------------------------------
{
    // Open world: projectile detonates on a victim, splashes a nearby third player.
    var w = new FlatCollisionWorld();
    var shooter = MakePlayer(1, 4, Vector3.Zero);               // splattergun
    var victim  = MakePlayer(2, 4, new Vector3(0, 0, 10));
    var nearby  = MakePlayer(3, 4, new Vector3(1.5f, 0, 10));   // within splash radius (4)
    var players = new List<PlayerState> { shooter, victim, nearby };
    // aim the round at the victim's torso so it scores a direct hit
    Vector3 eye = shooter.Move.Position + new Vector3(0, GameConstants.EyeHeight, 0);
    Vector3 tp = victim.Move.Position + new Vector3(0, 0.8f, 0);
    Vector3 ad = Vector3.Normalize(tp - eye);
    shooter.Move.Yaw = MathF.Atan2(ad.X, ad.Z); shooter.Move.Pitch = -MathF.Asin(Math.Clamp(ad.Y, -1f, 1f));
    var combat = new CombatSystem(w, () => players, _ => { });

    var proj = combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = 1 }, 1.0);
    Check(proj != null, "projectile: firing a splatter weapon spawns a server projectile (no instant hit)");
    float vHpBefore = victim.Health, nHpBefore = nearby.Health;
    double tnow = 1.0;
    for (int i = 0; i < 60 && proj != null && !proj.Dead; i++) { tnow += dt; combat.StepProjectile(proj!, dt, tnow); }
    Check(victim.Health < vHpBefore, "projectile: direct/blast damage applied to the victim");
    Check(nearby.Health < nHpBefore, "projectile: splash damages a nearby player");
    Check(nearby.Move.ExternalForceMode == ForceMode.Additive,
        "projectile: splash imparts a knockback impulse (rocket-jump physics)");

    // Walled world: two players equidistant from a blast — the one behind a wall is shielded.
    var v2 = new List<Vector3>(); var i2 = new List<int>();
    Quad(v2, i2, new Vector3(-50,0,-50), new Vector3(50,0,-50), new Vector3(50,0,50), new Vector3(-50,0,50)); // floor
    Quad(v2, i2, new Vector3(0,0,1.0f), new Vector3(0,5,1.0f), new Vector3(0,5,3.0f), new Vector3(0,0,3.0f)); // wall x=0, z[1,3]
    var wallWorld = new BakedCollisionWorld(new TriangleMesh(v2.ToArray(), i2.ToArray()));
    var owner    = MakePlayer(1, 4, new Vector3(-5, 0, 0));
    var exposed  = MakePlayer(2, 4, new Vector3(2f, 0, 2f));     // no wall between blast and here
    var shielded = MakePlayer(3, 4, new Vector3(-2f, 0, 2f));    // wall at x=0 sits between blast and here
    var players2 = new List<PlayerState> { owner, exposed, shielded };
    var combat2 = new CombatSystem(wallWorld, () => players2, _ => { });
    float eHp = exposed.Health, sHp = shielded.Health;
    combat2.Detonate(1, WeaponTable.Get(4), new Vector3(1.5f, 0.8f, 2f), 1.0); // blast on the +x side
    Check(exposed.Health < eHp,  "projectile: blast damages a player in the open");
    Check(shielded.Health == sHp, "projectile: a wall between the blast and a player blocks splash (LOS-gated)");
}

// ---------------------------------------------------------------------------------------
// 17. Fog of War (ESP defense): snapshots only carry players the recipient could see.
//     Wall room: floor + a wall plane at x=5 covering z in [-10,2] (open corridor at z>2).
// ---------------------------------------------------------------------------------------
{
    var v = new List<Vector3>(); var idx = new List<int>();
    Quad(v, idx, new Vector3(-50,0,-50), new Vector3(50,0,-50), new Vector3(50,0,50), new Vector3(-50,0,50)); // floor
    Quad(v, idx, new Vector3(5,0,-10), new Vector3(5,4,-10), new Vector3(5,4,2), new Vector3(5,0,2));         // wall x=5, z[-10,2]
    var fogRoom = new BakedCollisionWorld(new TriangleMesh(v.ToArray(), idx.ToArray()));

    var snaps = new Dictionary<int, Snapshot>();
    var sim = new ServerSimulation(fogRoom, (id, s) => snaps[id] = s, _ => { });
    var p1 = sim.AddPlayer(1, "tok-1", new Vector3(0, 0, 0), 1);  // viewer
    var p2 = sim.AddPlayer(2, "tok-2", new Vector3(8, 0, 0), 1);  // enemy behind the wall
    var p3 = sim.AddPlayer(3, "tok-3", new Vector3(0, 0, 3), 1);  // enemy in the open (viewer's side)

    sim.StepTick();
    Check(!Sees(snaps[1], 2), "fog of war: enemy behind a wall is NOT in the snapshot (ESP has nothing to read)");
    Check(Sees(snaps[1], 3),  "fog of war: enemy in the open IS in the snapshot");
    Check(!Sees(snaps[2], 1) && !Sees(snaps[2], 3), "fog of war: culling is per-viewer (walled player sees no one)");
    Check(snaps[2].Local.EntityId == 2, "fog of war: the recipient's own state is never culled");

    // hysteresis: a visible enemy who breaks LOS keeps streaming for the grace window, then drops
    p3.Move.Position = new Vector3(8, 0, -3);          // teleport behind the wall
    sim.StepTick();
    Check(Sees(snaps[1], 3), "fog of war: grace window keeps a just-hidden enemy streaming (no pop-out)");
    for (int i = 0; i < (int)(GameConstants.VisGraceSeconds * GameConstants.TickRate) + 3; i++) sim.StepTick();
    Check(!Sees(snaps[1], 3), "fog of war: grace window expires — hidden enemy culled");

    // gunfire reveals the shooter even through the wall (it is audible + traced in-world)
    sim.EnqueueFire(new FireIntent { EntityId = 2, SessionToken = "tok-2", Slot = 0 });
    sim.StepTick();
    Check(Sees(snaps[1], 2), "fog of war: firing reveals a hidden shooter");
    for (int i = 0; i < (int)((GameConstants.FireRevealSeconds + GameConstants.VisGraceSeconds) * GameConstants.TickRate) + 3; i++) sim.StepTick();
    Check(!Sees(snaps[1], 2), "fog of war: fire-reveal expires — shooter re-hidden");

    // teammates are always relevant, walls or not
    p1.TeamId = 1; p2.TeamId = 1;
    sim.StepTick();
    Check(Sees(snaps[1], 2) && !Sees(snaps[1], 3), "fog of war: teammate always sent; hidden enemy still culled");

    // "look into the future": a hidden enemy moving toward the corridor opening is revealed
    // by velocity look-ahead BEFORE its current position has line-of-sight
    var p4 = sim.AddPlayer(4, "tok-4", new Vector3(8, 0, 1.0f), 1);
    sim.StepTick();
    Check(!Sees(snaps[1], 4), "fog of war: stationary enemy near the corridor edge is still hidden");
    p4.Move.Velocity = new Vector3(0, 0, 12f);         // sprinting toward the opening at z>2
    sim.StepTick();
    Check(Sees(snaps[1], 4), "fog of war: velocity look-ahead reveals a peek before it pops in");

    // body-sample test: a head visible over a LOW wall reveals, even with the torso occluded
    var v2 = new List<Vector3>(); var i2 = new List<int>();
    Quad(v2, i2, new Vector3(-50,0,-50), new Vector3(50,0,-50), new Vector3(50,0,50), new Vector3(-50,0,50));
    Quad(v2, i2, new Vector3(5,0,-10), new Vector3(5,1.3f,-10), new Vector3(5,1.3f,10), new Vector3(5,0,10)); // low wall
    var lowRoom = new BakedCollisionWorld(new TriangleMesh(v2.ToArray(), i2.ToArray()));
    var snaps2 = new Dictionary<int, Snapshot>();
    var sim2 = new ServerSimulation(lowRoom, (id, s) => snaps2[id] = s, _ => { });
    sim2.AddPlayer(1, "tok-1", new Vector3(0, 0, 0), 1);
    sim2.AddPlayer(2, "tok-2", new Vector3(8, 0, 0), 1);
    sim2.StepTick();
    Check(Sees(snaps2[1], 2), "fog of war: a head over a low wall is enough to reveal (multi-sample LOS)");

    // death: a dead viewer spectates everyone; a dead target's position is no longer secret
    p3.Health = 0f;
    sim.StepTick();
    Check(Sees(snaps[3], 2) && Sees(snaps[3], 4), "fog of war: a dead viewer receives everyone (spectate/kill-cam)");
    Check(Sees(snaps[1], 3), "fog of war: a dead player is sent (kill already broadcast the position)");
}

// ---------------------------------------------------------------------------------------
// 18. Phase 2 — wire format: lossless round-trips, delta compression, strict reader.
// ---------------------------------------------------------------------------------------
{
    // input packet: server simulates FROM these floats — they must round-trip bit-exact
    var inPkt = new InputPacket { EntityId = 7, SessionToken = "tok-7", Cmd = new InputCmd {
        Seq = 1234, ClientTick = 99, MoveDir = new Vector3(-0.7071f, 0f, 0.7071f),
        Jump = true, Crouch = false, Yaw = -2.5f, Pitch = 0.33f } };
    Check(Wire.TryDecodeInput(Wire.EncodeInput(inPkt), out InputPacket inBack)
        && inBack.EntityId == 7 && inBack.SessionToken == "tok-7" && inBack.Cmd.Seq == 1234
        && BitConverter.SingleToInt32Bits(inBack.Cmd.MoveDir.X) == BitConverter.SingleToInt32Bits(inPkt.Cmd.MoveDir.X)
        && BitConverter.SingleToInt32Bits(inBack.Cmd.Yaw) == BitConverter.SingleToInt32Bits(inPkt.Cmd.Yaw)
        && inBack.Cmd.Jump && !inBack.Cmd.Crouch,
        "wire: InputPacket round-trips bit-exact");

    var fire = new FireIntent { EntityId = 7, SessionToken = "tok-7", Slot = 1, ClientTick = 55 };
    Check(Wire.TryDecodeFire(Wire.EncodeFire(fire), out FireIntent fBack)
        && fBack.EntityId == 7 && fBack.SessionToken == "tok-7" && fBack.Slot == 1 && fBack.ClientTick == 55,
        "wire: FireIntent round-trips");

    var hit = new HitEvent { Shooter = 1, Target = 2, Damage = 47.5f, Headshot = true, Killed = false,
        Point = new Vector3(12.345f, 1.618f, -77.7f) };
    Check(Wire.TryDecodeHit(Wire.EncodeHit(hit), out HitEvent hBack)
        && hBack.Shooter == 1 && hBack.Target == 2 && hBack.Headshot && !hBack.Killed
        && (hBack.Point - hit.Point).Length() < 0.01f,
        "wire: HitEvent round-trips within quantization");

    // snapshot: Local must be BIT-exact (reconciliation replays from it); Others quantized
    var enc = new SnapshotEncoder();
    var dec = new SnapshotDecoder();
    var snap = new Snapshot
    {
        ServerTime = 12.3456789,
        LastProcessedInput = 4242,
        Local = new PlayerSnap { EntityId = 1, Position = new Vector3(1.234567f, 5.4321f, -9.87f),
            Velocity = new Vector3(-3.3f, 0.001f, 7.0001f), Yaw = 1.234f, Pitch = -0.456f,
            Grounded = true, JumpArmed = true, UngroundedTicks = 1, SpeedScale = 0.7f,
            Health = 87.5f, ActiveSlot = 0, ActiveAmmo = 31 },
        Others = new[]
        {
            new PlayerSnap { EntityId = 2, Position = new Vector3(10.5f, 0f, -20.25f),
                Velocity = new Vector3(7f, -1f, 0f), Yaw = 3.0f, Pitch = -0.8f,
                Grounded = true, Ducked = true, Health = 64.2f, ActiveSlot = 1, SpeedScale = 1f },
            new PlayerSnap { EntityId = 3, Position = new Vector3(-5f, 2f, 8f),
                Velocity = Vector3.Zero, Yaw = -1.5f, Pitch = 1.2f, Health = 100f, SpeedScale = 1f },
        },
    };
    byte[] full = enc.Encode(snap);
    Check(dec.TryDecode(full, out Snapshot got), "wire: snapshot decodes");
    Check(BitConverter.SingleToInt32Bits(got.Local.Position.X) == BitConverter.SingleToInt32Bits(snap.Local.Position.X)
        && BitConverter.SingleToInt32Bits(got.Local.Velocity.Z) == BitConverter.SingleToInt32Bits(snap.Local.Velocity.Z)
        && got.Local.JumpArmed && got.Local.UngroundedTicks == 1
        && BitConverter.SingleToInt32Bits(got.Local.SpeedScale) == BitConverter.SingleToInt32Bits(snap.Local.SpeedScale)
        && got.Local.ActiveAmmo == 31 && got.ServerTime == snap.ServerTime && got.LastProcessedInput == 4242,
        "wire: snapshot LOCAL state is bit-exact (reconciliation-safe)");
    Check(got.Others.Length == 2
        && (got.Others[0].Position - snap.Others[0].Position).Length() < 2f / Wire.PosScale
        && MathF.Abs(got.Others[0].Yaw - snap.Others[0].Yaw) < 0.001f
        && MathF.Abs(got.Others[0].Pitch - snap.Others[0].Pitch) < 0.001f   // negative pitch survives the u16 wrap
        && got.Others[0].Ducked && MathF.Abs(got.Others[0].Health - 64.2f) < 0.06f,
        "wire: snapshot OTHERS within quantization tolerance (incl. negative pitch)");

    // delta: an unchanged remote costs only id+mask on the next snapshot
    byte[] second = enc.Encode(snap);
    Check(second.Length < full.Length - 20,
        $"wire: delta-encoded repeat snapshot is much smaller ({second.Length} vs {full.Length} bytes)");
    Check(dec.TryDecode(second, out Snapshot got2) && got2.Others.Length == 2
        && (got2.Others[1].Position - snap.Others[1].Position).Length() < 2f / Wire.PosScale,
        "wire: delta decode reproduces unchanged values");

    // fog of war interplay: entity culled from a snapshot, then reappears — values intact
    var culled = snap; culled.Others = new[] { snap.Others[0] };           // 3 hidden
    Check(dec.TryDecode(enc.Encode(culled), out Snapshot got3) && got3.Others.Length == 1,
        "wire: culled entity absent after fog-of-war drop");
    var back = snap;                                                        // 3 re-revealed, unchanged
    Check(dec.TryDecode(enc.Encode(back), out Snapshot got4) && got4.Others.Length == 2
        && (got4.Others[1].Position - snap.Others[1].Position).Length() < 2f / Wire.PosScale,
        "wire: re-revealed entity decodes correctly from cached baseline");

    // strict reader: EVERY truncation of a valid buffer is rejected, no exception escapes
    bool truncSafe = true;
    for (int len = 0; len < full.Length; len++)
    {
        var cut = new byte[len];
        Array.Copy(full, cut, len);
        var freshDec = new SnapshotDecoder();
        try { if (freshDec.TryDecode(cut, out _)) { /* short prefix decoding = reader hole */ truncSafe = len == 0 ? truncSafe : false; } }
        catch { truncSafe = false; }
    }
    for (int len = 0; len < 16; len++)
    {
        var cut = new byte[len];
        Array.Copy(Wire.EncodeInput(inPkt), cut, Math.Min(len, Wire.EncodeInput(inPkt).Length));
        try { if (Wire.TryDecodeInput(cut, out _)) truncSafe = false; }
        catch { truncSafe = false; }
    }
    Check(truncSafe, "wire: every truncated buffer is rejected without an exception escaping");

    // fuzz: seeded xorshift garbage through every decoder — must never throw
    bool fuzzSafe = true;
    uint frng = 0xDEADBEEF;
    byte NextB() { frng ^= frng << 13; frng ^= frng >> 17; frng ^= frng << 5; return (byte)frng; }
    for (int i = 0; i < 2000 && fuzzSafe; i++)
    {
        var buf = new byte[NextB() % 96];
        for (int j = 0; j < buf.Length; j++) buf[j] = NextB();
        if (i % 4 == 0 && buf.Length >= 2) { buf[0] = Wire.ProtocolVersion; buf[1] = (byte)(1 + (NextB() % 9)); } // valid header, garbage body
        try
        {
            Wire.TryDecodeInput(buf, out _); Wire.TryDecodeFire(buf, out _);
            Wire.TryDecodeSwitch(buf, out _); Wire.TryDecodeHit(buf, out _);
            Wire.TryDecodePing(buf, out _); Wire.TryDecodePong(buf, out _, out _);
            Wire.TryDecodeHello(buf, out _); Wire.TryDecodeWelcome(buf, out _, out _);
            new SnapshotDecoder().TryDecode(buf, out _);
        }
        catch { fuzzSafe = false; }
    }
    Check(fuzzSafe, "wire: 2000 fuzzed buffers never throw past the reader boundary");

    // version gate
    byte[] wrongVer = Wire.EncodeFire(fire); wrongVer[0] = 99;
    Check(!Wire.TryDecodeFire(wrongVer, out _), "wire: wrong protocol version rejected");

    // trailing-garbage gate (forged oversized packet)
    byte[] padded = Wire.EncodeFire(fire);
    Array.Resize(ref padded, padded.Length + 3);
    Check(!Wire.TryDecodeFire(padded, out _), "wire: trailing bytes rejected");
}

// ---------------------------------------------------------------------------------------
// 19. Phase 6 — lag-comp tuning: hits land under jittery RTT, backtrack abuse is clamped,
//     RTT growth is rate-limited, interpolation extrapolates briefly then freezes.
// ---------------------------------------------------------------------------------------
{
    // (a) hit-reg under 120ms RTT with ±30ms jitter against a strafing target.
    // Sniper (0.1° spread) so the only variable under test is the REWIND, not weapon spread.
    // History is a 64-tick ring (~2.1s), so record continuously and fire at RECENT times.
    var w = new FlatCollisionWorld();
    var shooter = MakePlayer(1, 3, Vector3.Zero);
    var target  = MakePlayer(2, 3, new Vector3(0, 0, 10));
    var players = new List<PlayerState> { shooter, target };
    int hits = 0;
    var combat = new CombatSystem(w, () => players, _ => hits++);
    Vector3 PosAt(double t) => new(7f * (float)t, 0f, 10f);          // strafes +X at walk speed

    shooter.ObserveRtt(0.12);                                       // server-measured RTT
    float[] jitter = { -0.03f, 0.0f, 0.03f };
    int fired = 0;
    int gtick = 0;
    void RecordTo(double until) { while ((gtick + 1) * dt <= until + 1e-9) { gtick++; double t = gtick * dt; target.History.Record(t, PosAt(t), 0f); } }
    for (int i = 0; i < jitter.Length; i++)
    {
        double now = 0.5 + i * 1.3;                                 // recent + respects 1.2s FireInterval
        RecordTo(now);
        double clientRtt = 0.12 + jitter[i];                        // what the CLIENT experienced
        double clientViewTime = now - GameConstants.InterpDelaySeconds - clientRtt * 0.5;
        // the client aimed at the target where it RENDERED it (torso height)
        Vector3 eye = shooter.Move.Position + new Vector3(0, GameConstants.EyeHeight, 0);
        Vector3 aimPt = PosAt(clientViewTime) + new Vector3(0, 0.8f, 0);
        Vector3 d = Vector3.Normalize(aimPt - eye);
        shooter.Move.Yaw = MathF.Atan2(d.X, d.Z);
        shooter.Move.Pitch = -MathF.Asin(Math.Clamp(d.Y, -1f, 1f));
        target.Health = 100f;
        combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = (uint)(100 + i) }, now);
        fired++;
    }
    Check(hits == fired, $"lag-comp: all {fired} shots land at 120ms RTT with ±30ms jitter (hits={hits})");

    // (b) backtrack abuse: inflated RTT cannot rewind past MaxRewindSeconds.
    int abuseHits = 0;
    var combat2 = new CombatSystem(w, () => players, _ => abuseHits++);
    double nowAbuse = gtick * dt + 1.3;                             // next allowed sniper shot
    RecordTo(nowAbuse);
    for (int i = 0; i < 50; i++) shooter.ObserveRtt(2.0);           // hold fake 2s RTT
    double staleTime = nowAbuse - 1.0;                              // aim ~1s back (rewind floor is 0.25s)
    Vector3 eye2 = shooter.Move.Position + new Vector3(0, GameConstants.EyeHeight, 0);
    Vector3 stalePt = PosAt(staleTime) + new Vector3(0, 0.8f, 0);
    Vector3 d2 = Vector3.Normalize(stalePt - eye2);
    shooter.Move.Yaw = MathF.Atan2(d2.X, d2.Z);
    shooter.Move.Pitch = -MathF.Asin(Math.Clamp(d2.Y, -1f, 1f));
    target.Health = 100f;
    combat2.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = 999 }, nowAbuse);
    // rewind floor = now - MaxRewind = now-0.25; the target there is ~5.25u from the 1s-stale
    // position the cheater aimed at — far outside the 0.4u hitbox.
    Check(abuseHits == 0, "lag-comp: inflated RTT cannot land a hit beyond the MaxRewind window");

    // (c) RttTracker: init cap, rate-limited growth, free fall
    var rt = new RttTracker();
    rt.Observe(1.5);
    Check(rt.Seconds <= RttTracker.InitCap + 1e-9, $"rtt: first observation capped ({rt.Seconds:F3}s)");
    var rt2 = new RttTracker();
    rt2.Observe(0.05);
    rt2.Observe(2.0);
    Check(rt2.Seconds <= 0.05 + RttTracker.MaxGrowPerObservation + 1e-9,
        $"rtt: growth rate-limited per observation ({rt2.Seconds:F3}s)");
    double before = rt2.Seconds;
    rt2.Observe(0.05);
    Check(rt2.Seconds < before, "rtt: estimate falls freely when latency improves");

    // (d) interpolation buffer starvation: brief extrapolation, then freeze
    var interp = new RemoteInterpolator();
    interp.Ingest(0.9, new Vector3(0, 0, 0), 0f, 0f);
    interp.Ingest(1.0, new Vector3(1, 0, 0), 0f, 0f);                // 10 u/s along +X
    interp.Sample(1.05, out Vector3 ep1, out _, out _);
    Check(MathF.Abs(ep1.X - 1.5f) < 0.01f, $"interp: extrapolates through a short gap (x={ep1.X:F2})");
    interp.Sample(5.0, out Vector3 ep2, out _, out _);
    float frozenX = 1f + 10f * GameConstants.MaxExtrapolateSeconds;
    Check(MathF.Abs(ep2.X - frozenX) < 0.01f, $"interp: freezes at the extrapolation cap (x={ep2.X:F2})");

    // (e) PingMeasurement: server-measured RTT bookkeeping (the source the rewind clamp trusts).
    var pm = new PingMeasurement();
    uint n1 = pm.Issue(10.0);
    Check(pm.Resolve(n1, 10.12, out double measured) && Math.Abs(measured - 0.12) < 1e-9,
        $"rtt: echoed nonce resolves to the server-measured round-trip ({measured:F3}s)");
    Check(!pm.Resolve(n1, 10.20, out _), "rtt: a REPLAYED nonce yields no second sample (consumed on resolve)");
    Check(!pm.Resolve(123456u, 10.0, out _), "rtt: an unknown/forged nonce is rejected");
    // a client that hoards stale nonces can't: the outstanding set is capped + oldest-evicted.
    var pm2 = new PingMeasurement();
    uint first = pm2.Issue(0.0);
    for (int i = 0; i < PingMeasurement.MaxOutstanding + 4; i++) pm2.Issue(1.0 + i);
    Check(pm2.OutstandingCount <= PingMeasurement.MaxOutstanding, "rtt: outstanding pings are capped (no unbounded growth)");
    Check(!pm2.Resolve(first, 100.0, out _), "rtt: the oldest stale nonce was evicted → can't echo it for a huge RTT");
    // clock guard: a non-monotonic 'now' never yields a negative RTT.
    var pm3 = new PingMeasurement();
    uint n3 = pm3.Issue(5.0);
    Check(pm3.Resolve(n3, 4.9, out double neg) && neg == 0d, "rtt: never reports a negative round-trip");
}

// ---------------------------------------------------------------------------------------
// 20. Phase 8 — detection signals, graduated response, telemetry.
// ---------------------------------------------------------------------------------------
{
    // triggerbot window: human-speed reactions never bump; sustained sub-120ms does
    var human = new AnomalyTracker();
    for (int i = 0; i < 16; i++) human.RecordReaction(false);
    Check(human.ScoreOf(AnomalyKind.Triggerbot) == 0f, "phase8: human reaction times never bump triggerbot");
    var mixed = new AnomalyTracker();
    for (int i = 0; i < 8; i++) mixed.RecordReaction(i < 2);          // 2 fast in 8 = lucky, not a bot
    Check(mixed.ScoreOf(AnomalyKind.Triggerbot) == 0f, "phase8: occasional fast reactions don't bump");
    var bot = new AnomalyTracker();
    for (int i = 0; i < 8; i++) bot.RecordReaction(true);
    Check(bot.ScoreOf(AnomalyKind.Triggerbot) > 0f, "phase8: sustained sub-human reactions bump triggerbot");

    // windowed accuracy: near-perfect over 30+ shots bumps; 50% never does
    var laser = new AnomalyTracker();
    for (int i = 0; i < 35; i++) laser.RecordShot(true, true);
    Check(laser.ScoreOf(AnomalyKind.Accuracy) > 0f, "phase8: sustained near-perfect accuracy bumps");
    var normal = new AnomalyTracker();
    for (int i = 0; i < 40; i++) normal.RecordShot(i % 2 == 0, false);
    Check(normal.ScoreOf(AnomalyKind.Accuracy) == 0f, "phase8: 50% accuracy never bumps");

    // policy rule 1: a single heuristic can NEVER pass Flag, no matter how hot
    var oneKind = new AnomalyTracker();
    var pol1 = new SuspicionPolicy();
    for (int i = 0; i < 80; i++) oneKind.Bump(AnomalyKind.AimSnap, 1f);  // score 40 from ONE kind
    pol1.Evaluate(oneKind, 0);
    pol1.Evaluate(oneKind, 60);
    Check(pol1.Level == ResponseLevel.Flag,
        $"phase8: one heuristic alone caps at Flag even at score {oneKind.Score:F0} ({pol1.Level})");

    // policy escalation: two independent kinds → Review now, Action only after sustain
    var multi = new AnomalyTracker();
    var pol2 = new SuspicionPolicy();
    for (int i = 0; i < 6; i++) multi.Bump(AnomalyKind.Triggerbot, 1f);  // 6.0
    for (int i = 0; i < 5; i++) multi.Bump(AnomalyKind.WallShot, 1f);    // +6.0 → 12, two kinds
    Check(pol2.Evaluate(multi, 100) == ResponseLevel.Review, "phase8: two corroborating kinds → Review");
    Check(pol2.Evaluate(multi, 120) == ResponseLevel.Review, "phase8: Action withheld before the sustain window");
    Check(pol2.Evaluate(multi, 131) == ResponseLevel.Action, "phase8: sustained multi-signal score → Action recommendation");

    // hysteresis: decay to just under the entry threshold doesn't flap the level
    var fading = new AnomalyTracker();
    var pol3 = new SuspicionPolicy();
    for (int i = 0; i < 4; i++) fading.Bump(AnomalyKind.Triggerbot, 1f);
    for (int i = 0; i < 3; i++) fading.Bump(AnomalyKind.WallShot, 1f);   // ~7.6, two kinds → Review
    Check(pol3.Evaluate(fading, 0) == ResponseLevel.Review, "phase8: enters Review");
    while (fading.Score >= 3.5f) fading.Decay(1f);                       // decay to ~3.4 (≥ half of 6)
    Check(pol3.Evaluate(fading, 10) == ResponseLevel.Review, "phase8: hysteresis holds Review above half-threshold");
    while (fading.Score >= 1.0f) fading.Decay(1f);
    Check(pol3.Evaluate(fading, 20) < ResponseLevel.Review, "phase8: level releases once the score clearly decays");

    // full-loop triggerbot through ServerSimulation: aim-away → snap-on + fire same tick
    var w = new FlatCollisionWorld();
    var sim = new ServerSimulation(w, (_, _) => { }, _ => { });
    var ps  = sim.AddPlayer(1, "tok-1", Vector3.Zero, 1);                // machine gun
    var tgt = sim.AddPlayer(2, "tok-2", new Vector3(0, 0, 10), 1);
    // aim values for on-target (at the torso) and away
    Vector3 eye = new(0, GameConstants.EyeHeight, 0);
    Vector3 torso = tgt.Move.Position + new Vector3(0, (GameConstants.BodyBottom + GameConstants.BodyTop) * 0.5f, 0);
    Vector3 dOn = Vector3.Normalize(torso - eye);
    float yawOn = MathF.Atan2(dOn.X, dOn.Z), pitchOn = -MathF.Asin(Math.Clamp(dOn.Y, -1f, 1f));
    int telemetryEvents = 0;
    sim.Telemetry.Emitted += _ => telemetryEvents++;
    for (int cycle = 0; cycle < 12; cycle++)
    {
        tgt.Health = 100f;                                              // keep the dummy alive to test the signal, not the kill
        ps.Move.Yaw = MathF.PI; ps.Move.Pitch = 0f;                      // aim away
        for (int i = 0; i < 3; i++) sim.StepTick();
        tgt.Health = 100f;
        ps.Move.Yaw = yawOn; ps.Move.Pitch = pitchOn;                    // snap on + fire SAME tick
        sim.EnqueueFire(new FireIntent { EntityId = 1, SessionToken = "tok-1", Slot = 0, ClientTick = (uint)cycle });
        sim.StepTick();
    }
    Check(ps.Anomaly.ScoreOf(AnomalyKind.Triggerbot) > 0f,
        $"phase8: full-loop snap-and-fire pattern bumps triggerbot (score {ps.Anomaly.ScoreOf(AnomalyKind.Triggerbot):F1})");
    Check(telemetryEvents > 0, "phase8: anomaly bumps stream to the telemetry sink");
    bool foundJsonl = false;
    foreach (TelemetryEvent e in sim.Telemetry.Recent())
        if (e.Kind == "anomaly" && TelemetrySink.ToJsonl(e).Contains("\"kind\":\"anomaly\"")) { foundJsonl = true; break; }
    Check(foundJsonl, "phase8: telemetry events serialize to JSONL for the host pipeline");
}

// ---------------------------------------------------------------------------------------
// 21. Phase 7 — real WebSocket transport over loopback: handshake binds EntityId<->token,
//     packets reach the sim, snapshots reach the client, floods are throttled, reconnect works.
// ---------------------------------------------------------------------------------------
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    double Now() => sw.Elapsed.TotalSeconds;
    async Task<bool> WaitUntil(Func<bool> cond, int ms)
    {
        var until = Now() + ms / 1000.0;
        while (Now() < until) { if (cond()) return true; await Task.Delay(5); }
        return cond();
    }

    using var server = new UberStrike.Server.WebSocketServerLink(0, Now);
    server.Authenticate = tok => tok == "tok-1" ? 1 : (tok == "tok-2" ? 2 : (int?)null);

    int inputsSeen = 0, firesSeen = 0, connects = 0, disconnects = 0;
    server.InputReceived  += _ => inputsSeen++;
    server.FireReceived   += _ => firesSeen++;
    server.ClientConnected    += _ => connects++;
    server.ClientDisconnected += _ => disconnects++;
    server.InputsPerSec = 90; server.InputBurst = 10;   // tight burst so the flood test trips fast
    server.RttPingIntervalSeconds = 0.05;               // fast heartbeat so the RTT test is quick
    int rttSamples = 0; double lastRtt = -1; int lastRttEntity = -1;
    server.RttSample += (id, rtt) => { rttSamples++; lastRtt = rtt; lastRttEntity = id; };
    server.Start();
    var uri = new Uri($"ws://127.0.0.1:{server.Port}/");

    // (a) handshake: bad token is rejected
    var bad = new WebSocketClientLink();
    bool badOk = await bad.ConnectAsync(uri, "nope");
    Check(!badOk, "ws: connection with an unknown session token is rejected at handshake");
    bad.Dispose();

    // (b) valid handshake binds EntityId<->token
    int clientEntity = -1;
    var client = new WebSocketClientLink();
    client.Welcomed += id => clientEntity = id;
    Snapshot? lastSnap = null;
    client.SnapshotReceived += s => lastSnap = s;
    int hitsAtClient = 0;
    client.HitReceived += _ => hitsAtClient++;
    bool ok = await client.ConnectAsync(uri, "tok-1");
    Check(ok && clientEntity == 1, $"ws: valid token completes Hello->Welcome with the server's EntityId ({clientEntity})");
    Check(await WaitUntil(() => connects == 1, 1000), "ws: server raised ClientConnected");

    // (b2) server-MEASURED RTT: the heartbeat SvPing → client SvPong echo round-trips and the
    // server times it on its own clock (replaces the old always-0 stub). The host wires this to
    // PlayerState.ObserveRtt → the lag-comp rewind clamp.
    Check(await WaitUntil(() => rttSamples >= 1, 1500),
        $"ws: server-initiated heartbeat yields a server-measured RTT sample (n={rttSamples})");
    Check(lastRttEntity == 1 && lastRtt >= 0 && lastRtt < 0.5,
        $"ws: measured RTT is attributed to the right entity and is a finite loopback value ({lastRtt:F4}s)");

    // (c) inputs/fires flow to the server and get the authenticated EntityId stamped
    InputPacket forged = new() { EntityId = 999, SessionToken = "spoofed", Cmd = new InputCmd { Seq = 1, MoveDir = new Vector3(0,0,1) } };
    InputPacket capturedInput = default;
    server.InputReceived += p => capturedInput = p;
    client.SendInput(forged);
    Check(await WaitUntil(() => inputsSeen >= 1, 1000), "ws: client input reaches the server");
    Check(capturedInput.EntityId == 1 && capturedInput.SessionToken == "tok-1",
        "ws: server STAMPS the authenticated EntityId/token, ignoring the packet's forged ids");
    client.SendFire(new FireIntent { EntityId = 1, Slot = 0, ClientTick = 1 });
    Check(await WaitUntil(() => firesSeen >= 1, 1000), "ws: client fire reaches the server");

    // (d) server -> client snapshot + hit decode
    var snapOut = new Snapshot
    {
        ServerTime = 1.0, LastProcessedInput = 7,
        Local = new PlayerSnap { EntityId = 1, Position = new Vector3(3.5f, 0f, -2.25f), Health = 80f, SpeedScale = 1f, ActiveAmmo = 12 },
        Others = new[] { new PlayerSnap { EntityId = 2, Position = new Vector3(9f, 0f, 4f), Health = 55f, SpeedScale = 1f } },
    };
    server.SendSnapshot(1, snapOut);
    Check(await WaitUntil(() => lastSnap.HasValue, 1000) &&
        lastSnap!.Value.Local.EntityId == 1 &&
        (lastSnap.Value.Local.Position - snapOut.Local.Position).Length() < 0.01f &&
        lastSnap.Value.Others.Length == 1 && lastSnap.Value.Others[0].EntityId == 2,
        "ws: server snapshot decodes correctly on the client over the real socket");
    server.Broadcast(new HitEvent { Shooter = 2, Target = 1, Damage = 18f, Killed = false, Point = Vector3.Zero });
    Check(await WaitUntil(() => hitsAtClient >= 1, 1000), "ws: broadcast HitEvent reaches the client");

    // (e) rate limiting: an input flood is throttled (flagged) without killing the connection
    for (int i = 0; i < 200; i++) client.SendInput(new InputPacket { Cmd = new InputCmd { Seq = (uint)(100 + i) } });
    Check(await WaitUntil(() => server.FlaggedCount(1) > 0, 1500),
        $"ws: input flood is rate-limited and flagged (flagged={server.FlaggedCount(1)})");
    // the connection survives the flood — a fresh snapshot still gets through
    lastSnap = null;
    server.SendSnapshot(1, snapOut);
    Check(await WaitUntil(() => lastSnap.HasValue, 1000), "ws: connection survives the flood (snapshot still delivered)");

    // (f) reconnect/resync: same token rebinds to the same entity with a fresh baseline
    client.Dispose();
    Check(await WaitUntil(() => disconnects == 1, 1500), "ws: server sees the disconnect");
    var client2 = new WebSocketClientLink();
    int reEntity = -1; client2.Welcomed += id => reEntity = id;
    Snapshot? reSnap = null; client2.SnapshotReceived += s => reSnap = s;
    bool reOk = await client2.ConnectAsync(uri, "tok-1");
    Check(reOk && reEntity == 1, "ws: reconnect with the same token rebinds to the same EntityId");
    server.SendSnapshot(1, snapOut);   // fresh encoder => full baseline, all fields present
    Check(await WaitUntil(() => reSnap.HasValue && reSnap!.Value.Others.Length == 1 &&
        (reSnap.Value.Others[0].Position - snapOut.Others[0].Position).Length() < 0.01f, 1000),
        "ws: post-reconnect snapshot decodes from a fresh full baseline");
    client2.Dispose();
}

// ---------------------------------------------------------------------------------------
// 22. Phase 10 — room sharding, capacity, reaping, and a load test within tick budget.
// ---------------------------------------------------------------------------------------
{
    var mgr = new MatchManager(() => new FlatCollisionWorld(), roomCapacity: 8);

    // capacity + overflow: 20 joins at cap 8 → 3 rooms (8/8/4)
    for (int i = 0; i < 20; i++) mgr.Join($"tok-{i}", new Vector3(i % 8, 0, 0), 1);
    Check(mgr.RoomCount == 3, $"phase10: 20 joins at cap 8 shard into 3 rooms ({mgr.RoomCount})");
    int capped = 0; foreach (Room r in mgr.Rooms) { if (r.PlayerCount > r.Capacity) capped++; }
    Check(capped == 0, "phase10: no room ever exceeds its capacity");

    // reaping: empty a room, reap it
    Room first = mgr.Rooms.First();
    var ids = first.Sim.Players.Select(p => p.EntityId).ToList();
    foreach (int id in ids) first.Remove(id);
    int beforeReap = mgr.RoomCount;
    mgr.ReapEmptyRooms();
    Check(mgr.RoomCount == beforeReap - 1, "phase10: empty rooms are reaped");

    // load test: 20 rooms × 8 bots = 160 sims, 300 ticks, measure tick time + snapshot bytes
    long snapBytes = 0; int snapCount = 0;
    var load = new MatchManager(() => new FlatCollisionWorld(), roomCapacity: 8);
    var encoders = new Dictionary<int, SnapshotEncoder>();
    void Send(int entityId, Snapshot s)
    {
        if (!encoders.TryGetValue(entityId, out SnapshotEncoder? e)) { e = new SnapshotEncoder(); encoders[entityId] = e; }
        try { snapBytes += e.Encode(s).Length; snapCount++; } catch { }
    }
    int globalEntity = 0;
    for (int room = 0; room < 20; room++)
        for (int p = 0; p < 8; p++)
            load.Join($"load-{globalEntity++}", new Vector3(p * 2, 0, room * 2), 1, Send);
    Check(load.RoomCount == 20, $"phase10: load harness built 20 rooms ({load.RoomCount})");

    // drive movement + a fire each tick so the sim does real work (combat, fog-of-war, history)
    uint seq = 0;
    uint lrng = 0x1234567;
    float LF() { lrng ^= lrng << 13; lrng ^= lrng >> 17; lrng ^= lrng << 5; return ((lrng & 0xFFFF) / 65535f) * 2f - 1f; }
    for (int tick = 0; tick < 300; tick++)
    {
        seq++;
        foreach (Room r in load.Rooms)
            foreach (PlayerState ps2 in r.Sim.Players)
            {
                r.Sim.EnqueueInput(new InputPacket { EntityId = ps2.EntityId, SessionToken = ps2.SessionToken,
                    Cmd = new InputCmd { Seq = seq, ClientTick = seq, MoveDir = new Vector3(LF(), 0, LF()),
                        Jump = LF() > 0.7f, Yaw = LF() * 3f, Pitch = LF() } });
                if (tick % 6 == 0)
                    r.Sim.EnqueueFire(new FireIntent { EntityId = ps2.EntityId, SessionToken = ps2.SessionToken, Slot = 0, ClientTick = seq });
            }
        load.StepAll();
    }

    Console.Error.WriteLine($"[load] rooms=20 players=160 ticks=300 avg={load.Metrics.AvgMs:F3}ms " +
        $"p99={load.Metrics.PercentileMs(0.99):F3}ms max={load.Metrics.MaxMs:F3}ms budget={load.Metrics.BudgetMs:F2}ms " +
        $"avgSnap={(snapCount>0 ? (double)snapBytes/snapCount : 0):F0}B");
    Check(load.Metrics.AvgMs < load.Metrics.BudgetMs,
        $"phase10: average per-room tick is within the {load.Metrics.BudgetMs:F1}ms budget (avg {load.Metrics.AvgMs:F3}ms)");
    Check(load.Metrics.PercentileMs(0.99) < load.Metrics.BudgetMs * 2,
        $"phase10: p99 per-room tick is well-bounded (p99 {load.Metrics.PercentileMs(0.99):F3}ms)");
    Check(load.Metrics.TotalTicks == 20 * 300, $"phase10: metrics counted every room-tick ({load.Metrics.TotalTicks})");
    Check(snapCount > 0 && snapBytes / snapCount < 1200, $"phase10: snapshot bandwidth is modest (~{snapBytes/Math.Max(1,snapCount)}B/snapshot)");
}

// ---------------------------------------------------------------------------------------
// 23. Phase 9 — integrity registry (soft build-hash gate).
// ---------------------------------------------------------------------------------------
{
    var reg = new IntegrityRegistry();
    Check(reg.IsAccepted("anything"), "phase9: with no allow-list, all hashes accepted (don't lock players out pre-config)");
    reg.Allow("abc123");
    reg.Allow("def456");                                   // previous build during a rollout window
    Check(reg.IsAccepted("abc123") && reg.IsAccepted("DEF456"), "phase9: registered build hashes accepted (case-insensitive)");
    Check(!reg.IsAccepted("tampered"), "phase9: an unregistered (stale/tampered) build hash is rejected");
    Check(!reg.IsAccepted(""), "phase9: empty hash rejected once an allow-list exists");
}

// ---------------------------------------------------------------------------------------
// 24. Phase 7.5 — WebSocket frame manipulation: envelope round-trip + TransportGuard.
// ---------------------------------------------------------------------------------------
{
    // envelope round-trips and is order-preserving
    byte[] payload = Wire.EncodeFire(new FireIntent { EntityId = 1, SessionToken = "t", Slot = 0 });
    byte[] framed = TransportEnvelope.Wrap(42, payload);
    Check(framed.Length == payload.Length + TransportEnvelope.PrefixBytes, "ws-guard: envelope adds a 4-byte prefix");
    Check(TransportEnvelope.TryUnwrap(framed, out uint gotSeq, out byte[] gotPayload) && gotSeq == 42
        && gotPayload.Length == payload.Length && Wire.TryDecodeFire(gotPayload, out _),
        "ws-guard: envelope unwraps to the original seq + payload");
    Check(!TransportEnvelope.TryUnwrap(new byte[] { 1, 2 }, out _, out _), "ws-guard: a too-short frame has no valid envelope");

    // monotonic stream is accepted
    var g = new TransportGuard();
    bool allOk = true;
    for (uint s = 0; s < 100; s++) if (g.Inspect(s, s * 0.01) != FrameVerdict.Ok) allOk = false;
    Check(allOk && g.Strikes == 0, "ws-guard: a normal monotonic frame stream is accepted with no strikes");

    // exact replay of an already-seen seq is rejected as Replay
    Check(g.Inspect(50, 1.01) == FrameVerdict.Replay, "ws-guard: replaying a seen frame seq is rejected (Replay)");
    // a stale/decreasing seq outside the dedup window is rejected as OutOfOrder
    var g2 = new TransportGuard();
    for (uint s = 0; s < 400; s++) g2.Inspect(s, s * 0.02); // 50/s, spread over time so no flood
    Check(g2.Inspect(10, 9.0) == FrameVerdict.OutOfOrder, "ws-guard: a stale reordered seq (past the dedup window) is rejected");
    // a forged far-future jump is rejected as Gap
    var g3 = new TransportGuard();
    g3.Inspect(5, 0.0);
    Check(g3.Inspect(5 + TransportGuard.MaxForwardGap + 1, 0.01) == FrameVerdict.Gap, "ws-guard: a forged far-future seq jump is rejected (Gap)");

    // a frame-rate flood (many fresh seqs in one second) is DROPPED (Flood) but doesn't disconnect
    var g4 = new TransportGuard();
    uint fs = 0; int floods = 0;
    for (int i = 0; i < (int)TransportGuard.MaxFramesPerSec + 30; i++)
        if (g4.Inspect(fs++, 0.5) == FrameVerdict.Flood) floods++;   // all within the same 1s window
    Check(floods > 0, "ws-guard: a single-second frame flood is detected (dropped, not a disconnect)");
    Check(!g4.ShouldDisconnect, "ws-guard: a burst alone does NOT disconnect (legit catch-up is allowed)");

    // sustained TAMPERING (repeated replays) DOES trip the disconnect threshold
    var g6 = new TransportGuard();
    g6.Inspect(100, 0.0);
    for (int i = 0; i < TransportGuard.StrikeLimit + 2; i++) g6.Inspect(100, 0.001 * (i + 1)); // replay same seq
    Check(g6.ShouldDisconnect, "ws-guard: sustained replay/tamper trips the disconnect threshold");

    // replay does NOT accumulate state: a legit higher seq after a rejected replay still works
    var g5 = new TransportGuard();
    g5.Inspect(0, 0.0); g5.Inspect(1, 0.01);
    g5.Inspect(1, 0.02); // replay (strike)
    Check(g5.Inspect(2, 0.03) == FrameVerdict.Ok, "ws-guard: a valid next frame after a rejected replay is still accepted");
}

// ---------------------------------------------------------------------------------------
// 25. Phase 7.5 — over a REAL socket: enveloped play works; a raw replayed frame is rejected.
// ---------------------------------------------------------------------------------------
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    double Now() => sw.Elapsed.TotalSeconds;
    async Task<bool> WaitUntil(Func<bool> cond, int ms)
    {
        var until = Now() + ms / 1000.0;
        while (Now() < until) { if (cond()) return true; await Task.Delay(5); }
        return cond();
    }

    using var server = new UberStrike.Server.WebSocketServerLink(0, Now);
    server.Authenticate = tok => tok == "tok-1" ? 1 : (tok == "tok-2" ? 2 : (int?)null);
    int inputsSeen = 0;
    server.InputReceived += _ => inputsSeen++;
    server.Start();
    var uri = new Uri($"ws://127.0.0.1:{server.Port}/");

    // the normal client wraps every frame with a monotonic seq → handshake + play work
    var client = new WebSocketClientLink();
    int ent = -1; client.Welcomed += id => ent = id;
    bool ok = await client.ConnectAsync(uri, "tok-1");
    Check(ok && ent == 1, "ws-guard: enveloped Hello completes the handshake (real socket)");
    client.SendInput(new InputPacket { Cmd = new InputCmd { Seq = 1, MoveDir = new Vector3(0,0,1) } });
    Check(await WaitUntil(() => inputsSeen >= 1, 1000), "ws-guard: enveloped input is accepted over the real socket");
    int flaggedBefore = server.FlaggedCount(1);
    client.Dispose();

    // a hostile client that REPLAYS captured raw frames (a fixed/duplicated seq) is rejected.
    // We drive a raw ClientWebSocket and resend the SAME enveloped frame many times.
    var raw = new System.Net.WebSockets.ClientWebSocket();
    await raw.ConnectAsync(uri, CancellationToken.None);
    // hello must still be a fresh-seq enveloped frame to get past the handshake
    uint seq = 0;
    async Task SendFramed(byte[] body, uint s) {
        try {
            byte[] f = TransportEnvelope.Wrap(s, body);
            await raw.SendAsync(f, System.Net.WebSockets.WebSocketMessageType.Binary, true, CancellationToken.None);
        } catch { /* server may have dropped the connection — expected under tamper */ }
    }
    await SendFramed(Wire.EncodeHello("tok-2"), seq++);
    var rbuf = new byte[256];
    try { await raw.ReceiveAsync(rbuf, CancellationToken.None); } catch { }   // welcome
    int flaggedStart = server.FlaggedCount(2);
    // capture ONE input frame (seq 10), then REPLAY it with the SAME seq — the classic replay
    // attack. Stay just under the disconnect threshold so the flag is observable (a real flood of
    // replays would correctly disconnect, which removes the conn and zeroes FlaggedCount).
    byte[] inputBody = Wire.EncodeInput(new InputPacket { Cmd = new InputCmd { Seq = 5, MoveDir = new Vector3(1,0,0) } });
    for (int i = 0; i < TransportGuard.StrikeLimit - 3; i++)   // 1 fresh accept + replays under the limit
        await SendFramed(inputBody, 10);
    Check(await WaitUntil(() => server.FlaggedCount(2) > flaggedStart, 1500),
        $"ws-guard: replayed raw frames (fixed seq) are flagged by the server (flagged={server.FlaggedCount(2)})");
    try { raw.Dispose(); } catch { }
}

// ---------------------------------------------------------------------------------------
// 26. Phase 8 — reveal-reaction aimbot signal: fog-of-war reveal -> damage faster than human,
//     sustained, bumps RevealReaction; a continuous sighting does NOT re-arm the reveal clock.
// ---------------------------------------------------------------------------------------
{
    var flat = new FlatCollisionWorld();
    var vis = new UberStrike.Server.VisibilitySystem(flat);
    var viewer = new UberStrike.Server.PlayerState { EntityId = 1 };
    var target = new UberStrike.Server.PlayerState { EntityId = 2 };
    target.Move.Position = new Vector3(0, 0, 10);

    Check(double.IsNegativeInfinity(vis.RevealedAt(1, 2)), "reveal-reaction: unknown pair has no reveal time");
    vis.ShouldSend(viewer, target, 10.0);
    Check(vis.RevealedAt(1, 2) == 10.0, "reveal-reaction: first sighting stamps the reveal time");
    vis.ShouldSend(viewer, target, 10.1);
    Check(vis.RevealedAt(1, 2) == 10.0, "reveal-reaction: continuous sighting does NOT re-arm the reveal clock");
    // long occlusion (beyond the grace window), then seen again -> NEW reveal
    vis.ShouldSend(viewer, target, 10.1 + GameConstants.VisGraceSeconds + 5.0);
    Check(vis.RevealedAt(1, 2) > 10.0, "reveal-reaction: re-sighting after occlusion is a fresh reveal");

    // sustained sub-floor reveal->damage samples bump RevealReaction; slow ones never do
    var bot = new UberStrike.Server.AnomalyTracker();
    for (int i = 0; i < 6; i++) bot.RecordRevealReaction(true);
    Check(bot.ScoreOf(UberStrike.Server.AnomalyKind.RevealReaction) > 0f,
        "reveal-reaction: sustained sub-human reveal->damage reaction bumps the signal");
    var human = new UberStrike.Server.AnomalyTracker();
    for (int i = 0; i < 12; i++) human.RecordRevealReaction(false);
    Check(human.ScoreOf(UberStrike.Server.AnomalyKind.RevealReaction) == 0f,
        "reveal-reaction: human-speed reactions never bump it");
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
return failures == 0 ? 0 : 1;

static bool Sees(in Snapshot s, int entityId)
{
    foreach (PlayerSnap o in s.Others) if (o.EntityId == entityId) return true;
    return false;
}

static Snapshot MakeSnap(in MoveState srv, uint acked) => new()
{
    LastProcessedInput = acked,
    Local = new PlayerSnap
    {
        EntityId = 1,
        Position = srv.Position, Velocity = srv.Velocity,
        Grounded = srv.Grounded, Jumping = srv.Jumping, Ducked = srv.Ducked,
        JumpArmed = srv.JumpArmed, UngroundedTicks = srv.UngroundedTicks,
        SpeedScale = srv.SpeedScale, Yaw = srv.Yaw, Pitch = srv.Pitch,
    },
    Others = Array.Empty<PlayerSnap>(),
};

static PlayerState MakePlayer(int id, int weaponId, Vector3 pos)
{
    var def = WeaponTable.Get(weaponId);
    var p = new PlayerState
    {
        EntityId = id,
        SessionToken = "tok-" + id,
        ActiveSlot = 0,
        Weapons = new[] { new WeaponRuntime { WeaponId = weaponId, Ammo = def.MagSize, Reserve = def.MagSize } },
    };
    p.Move.Position = pos;
    p.Move.Grounded = true;
    return p;
}

static (float yaw, float pitch) AimAt(Vector3 from, Vector3 to)
{
    Vector3 eye = from + new Vector3(0, GameConstants.EyeHeight, 0);
    Vector3 head = to + new Vector3(0, GameConstants.HeadOffset, 0);
    Vector3 d = Vector3.Normalize(head - eye);
    return (MathF.Atan2(d.X, d.Z), -MathF.Asin(Math.Clamp(d.Y, -1f, 1f)));
}

// --- mesh builders for the Phase 4 collision tests ---
static void Quad(List<Vector3> v, List<int> t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
{
    int b = v.Count;
    v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
    t.Add(b); t.Add(b + 1); t.Add(b + 2);
    t.Add(b); t.Add(b + 2); t.Add(b + 3);
}

static void Box(List<Vector3> v, List<int> t, Vector3 center, Vector3 size)
{
    Vector3 s = size * 0.5f;
    var c = new Vector3[8]; int k = 0;
    for (int xi = -1; xi <= 1; xi += 2)
        for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
                c[k++] = center + new Vector3(s.X * xi, s.Y * yi, s.Z * zi);
    int[] f = { 0,1,3, 0,3,2, 4,6,7, 4,7,5, 0,4,5, 0,5,1, 2,3,7, 2,7,6, 0,2,6, 0,6,4, 1,5,7, 1,7,3 };
    int bse = v.Count;
    foreach (Vector3 corner in c) v.Add(corner);
    foreach (int fi in f) t.Add(bse + fi);
}
