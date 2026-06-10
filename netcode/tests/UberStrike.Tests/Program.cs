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

Console.WriteLine();
Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
return failures == 0 ? 0 : 1;

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
