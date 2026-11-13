using System.Numerics;
using UberStrike.Shared;
using UberStrike.Server;
using UberStrike.Client;

// ---------------------------------------------------------------------------------------
// In-process demo: one human client (entity 1) predicting + reconciling against an
// authoritative server, plus a stationary dummy (entity 2) that gets shot.
// Run: dotnet run --project src/UberStrike.Sandbox
// ---------------------------------------------------------------------------------------

const int   SniperId = 3;
var world   = new FlatCollisionWorld();
var link    = new InProcessLink { LatencySeconds = 0.05 }; // ~100ms round trip

// --- server -------------------------------------------------------------------------
var server = new ServerSimulation(
    world,
    sendSnapshot: (entityId, snap) => link.SendSnapshot(entityId, snap),
    broadcast:    hit => link.Broadcast(hit));

server.AddPlayer(1, "tok-1", new Vector3(0, 0, 0),  SniperId);
server.AddPlayer(2, "tok-2", new Vector3(0, 0, 10), SniperId); // dummy target
server.Get(1)!.SmoothedRtt = 0.10;

// Deliver the client's intent packets into the authoritative server (gateway + sim).
link.InputReceived += p => server.EnqueueInput(p);
link.FireReceived  += f => server.EnqueueFire(f);

// --- client (entity 1) --------------------------------------------------------------
var pred   = new PredictionClient(world, 1, "tok-1", new Vector3(0, 0, 0));
var dummy  = new RemoteInterpolator();
var clock  = new ClockSync();
var sniper = WeaponTable.Get(SniperId);
var combat = new CombatClient(1, "tok-1", 0, sniper.MagSize, sniper.FireInterval);

int hits = 0;
combat.ConfirmedHit += e => { hits++; Console.WriteLine($"  >> server-confirmed hit on {e.Target}: dmg={e.Damage:F0} head={e.Headshot} killed={e.Killed}"); };

link.SnapshotReceived += snap =>
{
    if (snap.Local.EntityId != 1) return;          // this client only owns entity 1
    pred.Reconcile(snap);
    combat.ReconcileAmmo(snap.Local.ActiveAmmo);
    foreach (var o in snap.Others)
        if (o.EntityId == 2) dummy.Ingest(snap.ServerTime, o.Position, o.Yaw, o.Pitch);
};
link.HitReceived += h => combat.OnHitEvent(h);

// --- drive ticks --------------------------------------------------------------------
const int Ticks = 90;
float worstError = 0f;

for (int tick = 1; tick <= Ticks; tick++)
{
    double t = tick * GameConstants.FixedDt;

    // Phase A (ticks 1-30): walk forward — exercises prediction + reconciliation.
    // Phase B (ticks 31+):  aim at the dummy and fire the sniper.
    Vector3 moveDir = tick <= 30 ? new Vector3(0, 0, 1) : Vector3.Zero;

    PlayerState me  = server.Get(1)!;
    PlayerState tgt = server.Get(2)!;
    (float yaw, float pitch) = AimAt(me.Move.Position, tgt.Move.Position);

    var cmd = new InputCmd { MoveDir = moveDir, Jump = false, Crouch = false, Yaw = yaw, Pitch = pitch, ClientTick = (uint)tick };
    link.SendInput(pred.BuildAndPredict(cmd));

    if (tick > 30 && tick % 8 == 0)
    {
        var fi = combat.TryFire(t, (uint)tick);
        if (fi.HasValue) link.SendFire(fi.Value);
    }

    link.Advance(t);   // deliver client->server messages that are due
    server.StepTick();
    link.Advance(t);   // deliver server->client messages that are due

    pred.SmoothErrors(GameConstants.FixedDt);
    worstError = MathF.Max(worstError, pred.LastReconcileError);

    if (tick % 10 == 0)
        Console.WriteLine($"tick {tick,2}: predPos={Fmt(pred.RenderPosition)} reconErr={pred.LastReconcileError:F4} pending={pred.PendingCount} dummyHP={tgt.Health:F0}");
}

Console.WriteLine();
Console.WriteLine($"worst reconciliation error over run: {worstError:F4} (expect tiny — shared movement is deterministic)");
Console.WriteLine($"server-confirmed hits: {hits}, dummy final HP: {server.Get(2)!.Health:F0}");
Console.WriteLine($"shooter anomaly score: {server.Get(1)!.Anomaly.Score:F2} (accuracy {server.Get(1)!.Anomaly.Accuracy:P0})");

static (float yaw, float pitch) AimAt(Vector3 from, Vector3 to)
{
    Vector3 eye = from + new Vector3(0, GameConstants.EyeHeight, 0);
    Vector3 head = to + new Vector3(0, GameConstants.HeadOffset, 0);
    Vector3 d = Vector3.Normalize(head - eye);
    float yaw = MathF.Atan2(d.X, d.Z);
    float pitch = -MathF.Asin(Math.Clamp(d.Y, -1f, 1f));
    return (yaw, pitch);
}

static string Fmt(Vector3 v) => $"({v.X:F2},{v.Y:F2},{v.Z:F2})";
