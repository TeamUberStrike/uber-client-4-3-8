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
// 1. Determinism: identical inputs -> identical state (client predict == server apply).
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    MoveState a = default, b = default; a.Grounded = b.Grounded = true; a.Stamina = b.Stamina = 10; b.Stamina = 10;
    for (uint i = 1; i <= 120; i++)
    {
        var cmd = new InputCmd
        {
            Seq = i,
            MoveDir = Vector3.Normalize(new Vector3(MathF.Sin(i * 0.3f), 0, MathF.Cos(i * 0.2f))),
            Jump = i % 17 == 0,
            Sprint = i % 2 == 0,
            Yaw = i * 0.05f, Pitch = 0.1f,
        };
        SharedMovement.Step(ref a, cmd, dt, w);
        SharedMovement.Step(ref b, cmd, dt, w);
    }
    Check(a.Position == b.Position && a.Velocity == b.Velocity, "shared movement is bit-deterministic across two runs");
}

// ---------------------------------------------------------------------------------------
// 2. Reconciliation convergence: replay of unacked inputs lands back on the prediction.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var pred = new PredictionClient(w, 1, "t", Vector3.Zero);
    var cmds = new List<InputCmd>();
    for (int i = 1; i <= 10; i++)
    {
        var pkt = pred.BuildAndPredict(new InputCmd { MoveDir = new Vector3(0, 0, 1), Yaw = 0.2f });
        cmds.Add(pkt.Cmd);
    }
    // Server has only processed the first 6 inputs.
    MoveState srv = default; srv.Grounded = true;
    for (int i = 0; i < 6; i++) SharedMovement.Step(ref srv, cmds[i], dt, w);

    pred.Reconcile(new Snapshot
    {
        LastProcessedInput = 6,
        Local = new PlayerSnap { EntityId = 1, Position = srv.Position, Velocity = srv.Velocity, Grounded = srv.Grounded, Yaw = srv.Yaw, Pitch = srv.Pitch },
        Others = Array.Empty<PlayerSnap>(),
    });
    Check(pred.LastReconcileError < 1e-3f, $"reconciliation converges (err={pred.LastReconcileError:E2})");
}

// ---------------------------------------------------------------------------------------
// 3. Firerate gate + 4. server-owned damage.
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

    // Second fire well within the 1.2s sniper interval -> must be rejected.
    combat.HandleFire(shooter, new FireIntent { EntityId = 1, Slot = 0, ClientTick = 2 }, 1.05);
    int ammoAfter2 = shooter.ActiveWeapon.Ammo;

    Check(ammoAfter1 == magStart - 1,         "first shot consumes exactly one ammo");
    Check(ammoAfter2 == ammoAfter1,           "second shot within firerate window is rejected");
    Check(hpAfter1 < hpStart,                 "server applies damage to target health");
    Check(hitCount == 1,                      "exactly one authoritative hit event emitted");
}

// ---------------------------------------------------------------------------------------
// 5. Speed-hack clamp: a giant MoveDir cannot exceed the max per-tick step.
// ---------------------------------------------------------------------------------------
{
    var w = new FlatCollisionWorld();
    var sys = new MovementSystem(w);
    var ps = MakePlayer(1, 1, Vector3.Zero); ps.Move.Stamina = 10;
    Vector3 before = ps.Move.Position;
    sys.Apply(ps, new InputCmd { Seq = 1, MoveDir = new Vector3(1000, 0, 0), Sprint = true }, dt);
    float horiz = new Vector3(ps.Move.Position.X - before.X, 0, ps.Move.Position.Z - before.Z).Length();
    float maxStep = (GameConstants.RunSpeed + GameConstants.ExtraSpeedTolerance) * dt;
    Check(horiz <= maxStep + 1e-3f, $"oversized MoveDir clamped to max step ({horiz:F3} <= {maxStep:F3})");
}

// ---------------------------------------------------------------------------------------
// 6. Gateway rejects bad auth / replay / schema.
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
}

Console.WriteLine();
Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
return failures == 0 ? 0 : 1;

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
