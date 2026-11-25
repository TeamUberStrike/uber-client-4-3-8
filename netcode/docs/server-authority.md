# UberStrike 4.3.8 — Server-Authoritative Validation Layer

**Context:** WebGL build, served to any browser (mobile *or* spoofed-mobile PC). The
client is 100% attacker-controlled and fully inspectable. This document specifies the
server-side layer that makes that not matter.

> Language here is C# (matches UberStrike's Photon/Unity heritage). The *logic* is
> portable — every module maps 1:1 to Go/Node/Rust. Nothing here depends on the client
> being honest.

---

## 0. The one rule everything derives from

The client may send **intent**. The server owns **state**. If a packet asserts state, drop
it and flag the sender.

| Client may SEND (intent) | Server OWNS (authoritative) |
|---|---|
| Movement input (dir, jump, sprint) | Position, velocity, grounded state |
| Look / aim angles | Health, armor |
| Fire button down/up + weapon slot | Ammo, reload state, weapon cooldowns |
| Interact / switch-weapon intent | **Hit registration & damage** |
| Input sequence # + client tick | Kills, deaths, score |
| | Currency, XP, unlocks |
| | Spawn, team, match state |

The client **never** sends: "my HP is X", "I hit player Y", "my damage is D", "I have Z
coins". Any such field → packet dropped, anomaly score incremented. A memory editor that
sets local HP to 9999 only changes the cheater's cosmetic view; the server never reads it.

---

## 1. Tick & message model

- **Fixed server tick**, 30 Hz (`dt = 33.3ms`). 60 Hz if your hosting budget allows.
- Client sends input packets tagged with a **monotonic input sequence** + its client tick.
- Server buffers inputs in a small **jitter buffer**, consumes one input set per sim step
  *in sequence order*.
- Each tick the server simulates movement + combat, then broadcasts a **delta-compressed
  snapshot** plus each client's reconciled state (`lastProcessedInputSeq`).
- Client-side prediction + reconciliation is fine — it's purely local UX. The server
  correction is law.

```
client                          server (30Hz authoritative sim)
  |  input(seq=N, tick, dir, look, fire) |
  |------------------------------------->| gateway: schema/rate/seq/auth
  |                                      | movement sim (server owns pos)
  |                                      | combat: firerate→rewind→raycast→damage
  |  snapshot(delta) + reconcile(seq=N)  |
  |<-------------------------------------|
```

---

## 2. Module map

1. **Gateway** — schema, rate limit, sequence/replay, auth binding.
2. **Movement** — server simulates from inputs; speed/collision/teleport/bounds.
3. **Combat (the core)** — firerate gate → lag-comp rewind → server raycast → server damage.
4. **Economy** — server-only mutation, idempotent purchases.
5. **Telemetry** — statistical anomaly detection for "possible but superhuman."

---

## 3. Server-owned player state

```csharp
public sealed class PlayerState
{
    public int    EntityId;
    public string SessionToken;        // bound to connection at auth
    public uint   LastProcessedInput;  // for reconciliation
    public uint   LastSeenInputSeq;    // replay/ordering guard

    // --- authoritative, never written from client packets ---
    public Vector3 Position;
    public Vector3 Velocity;
    public bool    Grounded;
    public float   Yaw, Pitch;         // server's notion of aim

    public float   Health = 100f;
    public float   Armor  =   0f;
    public int     TeamId;

    public WeaponSlot[] Weapons;       // server-side stats, ammo, cooldowns
    public int     ActiveSlot;

    public long    Currency;           // mutated only by validated events
    public int     Kills, Deaths;

    // anti-cheat
    public AnomalyTracker Anomaly = new();
}

public sealed class WeaponSlot
{
    public int   WeaponId;
    public int   Ammo;
    public int   ReserveAmmo;
    public double NextFireTime;        // server time the weapon may fire again
    public bool   Reloading;
}
```

`WeaponDef` (damage, fire interval, spread, falloff, headshot mult, max range) lives in a
**server-side table** keyed by `WeaponId`. The client sends only the slot index.

---

## 4. Gateway — input sanitization

Runs on **every** inbound packet before anything else touches it.

```csharp
bool AcceptInput(Connection c, InputPacket p)
{
    // 1. Auth: packet must belong to the entity this session controls
    if (p.EntityId != c.OwnedEntityId || p.SessionToken != c.SessionToken)
        return Reject(c, "auth_mismatch");

    // 2. Schema / range — reject malformed or out-of-range fields outright
    if (!p.LooksWellFormed() ||
        !Finite(p.MoveDir) || p.MoveDir.SqrMagnitude() > 1.01f ||  // dir is normalized
        Mathf.Abs(p.Pitch) > 90f)
        return Reject(c, "schema");

    // 3. Replay / ordering — strictly increasing within a window
    if (p.Seq <= c.State.LastSeenInputSeq ||
        p.Seq  > c.State.LastSeenInputSeq + MAX_SEQ_GAP)
        return Reject(c, "seq");
    c.State.LastSeenInputSeq = p.Seq;

    // 4. Rate limit — token bucket per connection
    if (!c.RateBucket.TryConsume(1))
        return Reject(c, "rate");           // repeated → throttle, then kick

    // 5. Client tick sanity — must sit inside the rewindable window
    if (!WithinRewindWindow(p.ClientTick))
        return Reject(c, "tick");

    c.JitterBuffer.Enqueue(p);              // consumed in order during sim
    return true;
}
```

Note: **`MoveDir` is treated as a unit vector and the server supplies the speed.** A client
sending `MoveDir = (1000, 0, 0)` gains nothing — magnitude is clamped, speed is ours.

---

## 5. Movement — server simulates, never accepts positions

```csharp
void StepMovement(PlayerState s, InputPacket p, float dt)
{
    // Speed is OURS, chosen by movement state — not by the client.
    float speed = s.Crouching ? CROUCH_SPEED
                : p.Sprint && s.Stamina > 0 ? RUN_SPEED
                : WALK_SPEED;

    Vector3 wish = ClampMagnitude(p.MoveDir, 1f) * speed;

    // Gravity & jump are server-side; jump only when grounded.
    if (p.Jump && s.Grounded) s.Velocity.y = JUMP_VELOCITY;
    s.Velocity.y -= GRAVITY * dt;

    Vector3 delta = new Vector3(wish.x, s.Velocity.y, wish.z) * dt;

    // Collide & slide against the SERVER collision world → kills noclip/fly.
    Vector3 next = CollideAndSlide(s.Position, delta);

    // Teleport / speed-hack guard: displacement can't exceed the physical max.
    float maxStep = (RUN_SPEED + EXTRA_TOLERANCE) * dt;
    if ((next - s.Position).magnitude > maxStep)
    {
        s.Anomaly.Bump(AnomalyKind.Teleport, weight: 1f);
        next = s.Position + (next - s.Position).normalized * maxStep; // clamp
    }

    // Hard map bounds / kill volumes
    if (!MapBounds.Contains(next)) next = MapBounds.ClampOrKill(s, next);

    s.Position = next;
    s.Grounded = CheckGrounded(s.Position);
    s.Yaw = p.Yaw; s.Pitch = p.Pitch;     // store aim for combat + heuristics
    s.LastProcessedInput = p.Seq;
}
```

Full server simulation (above) is the strong model. If CPU-bound, the fallback is
"client predicts, server validates against an envelope" — same clamp/teleport checks, but
trusting the client's position *unless* it leaves the envelope. The full sim is preferred
because it leaves zero room for a forged position.

---

## 6. Combat — the centerpiece (firerate → rewind → raycast → damage)

This is where client-authoritative legacy FPS code gets you killed. Every step closes a
specific cheat.

### 6.1 Lag-compensation history

```csharp
// Per player: ring buffer of recent hitbox snapshots, ~1s deep.
struct HitboxSnapshot { public double Time; public Capsule Body; public Sphere Head; }

class HitboxHistory
{
    readonly RingBuffer<HitboxSnapshot> _buf = new(capacity: 64);
    public void Record(double t, PlayerState s) =>
        _buf.Push(new HitboxSnapshot { Time = t, Body = s.BodyCapsule(), Head = s.HeadSphere() });

    // Interpolate hitboxes to the time the shooter actually "saw" the world.
    public (Capsule body, Sphere head) Rewind(double targetTime) => _buf.Sample(targetTime);
}
```

### 6.2 The fire handler

```csharp
void HandleFire(PlayerState shooter, FireIntent f, double serverNow)
{
    WeaponSlot w  = shooter.Weapons[shooter.ActiveSlot];
    WeaponDef def = WeaponTable[w.WeaponId];

    // (1) STATE GATE — owns fire rate, ammo, reload. Defeats rapidfire/infinite-ammo.
    if (f.Slot != shooter.ActiveSlot)        return;              // can't fire stowed weapon
    if (w.Reloading || w.Ammo <= 0)          return;              // no ammo → no shot
    if (serverNow < w.NextFireTime)          {                    // fired too fast
        shooter.Anomaly.Bump(AnomalyKind.FireRate, 0.5f); return;
    }

    // (2) CONSUME — server owns the cost.
    w.Ammo--;
    w.NextFireTime = serverNow + def.FireInterval;

    // (3) ORIGIN IS OURS. Never use a client-supplied origin → kills "shoot from anywhere".
    Vector3 origin = shooter.Position + def.MuzzleOffset(shooter.Yaw, shooter.Pitch);
    Vector3 aim    = DirFromAngles(shooter.Yaw, shooter.Pitch);   // server's stored aim

    // Optional soft aimbot heuristic: implausible angular snap since last tick.
    shooter.Anomaly.ObserveAimDelta(serverNow, shooter.Yaw, shooter.Pitch);

    // (4) SPREAD/RECOIL applied SERVER-SIDE → "no-spread" hacks only desync client visuals.
    aim = ApplyServerSpread(aim, def, shooter.RecoilState, seed: f.ClientTick);

    // (5) REWIND targets to the shooter's view-time, clamped to a max.
    double rttHalf = Clamp(shooter.SmoothedRtt * 0.5, 0, MAX_REWIND);
    double viewTime = serverNow - INTERP_DELAY - rttHalf;
    viewTime = Math.Max(viewTime, serverNow - MAX_REWIND);        // can't claim ancient ticks

    // (6) RAYCAST against rewound hitboxes, from OUR origin.
    HitResult hit = RaycastWorldAndPlayers(origin, aim, def.Range, viewTime, exclude: shooter);
    if (!hit.HitPlayer) return;

    // (7) LINE OF SIGHT against static geometry at present → kills wallbang/ESP-firing.
    if (!ServerHasLineOfSight(origin, hit.Point)) {
        shooter.Anomaly.Bump(AnomalyKind.WallShot, 0.7f); return;
    }

    // (8) DAMAGE is computed and applied SERVER-SIDE. Client never reports damage or hits.
    PlayerState target = hit.Player;
    float dmg = def.BaseDamage
              * def.RangeFalloff(hit.Distance)
              * (hit.Headshot ? def.HeadshotMult : 1f);
    dmg = ApplyArmor(target, dmg);

    target.Health -= dmg;
    Broadcast(new HitEvent(shooter.EntityId, target.EntityId, dmg, hit.Headshot, hit.Point));
    shooter.Anomaly.RecordShot(landed: true, headshot: hit.Headshot);

    // (9) DEATH & SCORE are server-owned.
    if (target.Health <= 0f) ResolveKill(shooter, target, def);
}
```

### What each step kills

| Step | Cheat it defeats |
|---|---|
| (1) state gate | rapidfire, no-reload, infinite ammo, fire-while-stowed |
| (2) consume | ammo editing (server owns the count) |
| (3) server origin | "shoot from anywhere", forged muzzle position |
| (4) server spread | no-spread / no-recoil (only desyncs cheater's view) |
| (5) clamped rewind | fake-latency / ancient-tick "backtrack" hits |
| (6) server raycast | client-reported hits, hit-everything |
| (7) server LOS | wallbang, ESP-assisted firing through geometry |
| (8) server damage | damage multipliers, one-shot hacks |
| (9) server score | kill/score injection, currency-on-kill abuse |

> **Projectile weapons** (rockets/grenades): same gate (1–4), but instead of an instant
> raycast you spawn a *server-simulated* projectile and resolve its collisions on later
> ticks against live hitboxes. Never trust a client "explosion happened here."

---

## 7. Economy

```csharp
bool TryPurchase(PlayerState s, int itemId, string idempotencyKey)
{
    if (s.SeenPurchaseKeys.Contains(idempotencyKey)) return true; // dup request, no double-spend
    var item = ShopTable[itemId];                                 // price is server-side
    if (s.Currency < item.Price) return false;                    // server checks balance

    s.Currency -= item.Price;            // atomic on the authoritative state
    GrantUnlock(s, item);
    s.SeenPurchaseKeys.Add(idempotencyKey);
    PersistAtomic(s);                    // single transaction; never client-driven
    return true;
}
```

Currency/XP mutate **only** through validated server events (a resolved kill, match-end
payout, or a balance-checked purchase). The client requests; it never asserts a balance.

---

## 8. Telemetry — catching the possible-but-superhuman

Validation stops *impossible* states. Statistics catch aimbots/triggerbots/wallhacks that
only do *possible* things very well.

```csharp
class AnomalyTracker
{
    public float Score;                  // rolling, decays over time
    // signals to feed it:
    //  - accuracy %, headshot %
    //  - reaction time: ms from target-becomes-visible → first shot (bots cluster low)
    //  - aim angular velocity / snap-to-target spikes
    //  - fire-timing variance (bots are too regular)
    //  - prefire: shots fired at occluded enemies (wallhack tell)
    public void Bump(AnomalyKind k, float weight) { Score += Weight(k) * weight; }
    // thresholds: soft flag → shadow-review queue → auto-action, with input traces logged
}
```

Tiered response: **flag → review → shadow (e.g. matchmake flagged players together) →
ban**, always logging the input trace as evidence so bans survive appeals.

---

## 9. Build hardening (defense in depth, not a substitute)

These slow casual cheaters; server authority is what actually protects you.

- **Encrypt/obfuscate `global-metadata.dat`** so Il2CppDumper-class tools don't trivially
  recover your class/field layouts from the WebGL `.data` file.
- **Disable remote debugging in release**: Android WebView `setWebContentsDebuggingEnabled(false)`;
  iOS WKWebView `isInspectable = false`. Don't ship anything that opens `chrome://inspect`
  or Safari Web Inspector to the live game.
- **WSS + per-session tokens**, message auth, sequence validation, rate limiting — covered
  by the gateway above. Assume forged and replayed packets.

---

## 10. Build order (suggested)

1. Gateway + auth binding + server-owned `PlayerState` (everything else needs this).
2. Server movement sim with clamp/collision/teleport guards.
3. Combat: state gate → server raycast → server damage (ship without lag-comp first; add
   the rewind buffer once hit-feel needs it).
4. Snapshot broadcast + client reconciliation.
5. Economy server-side.
6. Telemetry/anomaly scoring last — it's tuning, and it needs real traffic to calibrate.
