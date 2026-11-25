# UberStrike 4.3.8 — Client Prediction & Reconciliation

**Pairs with:** `uberstrike-server-authority.md`. Same 30 Hz tick, same `InputPacket.Seq`,
same `LastProcessedInput` ack. This is the client half that makes a fully
server-authoritative game *feel* instant despite the server owning everything.

> Unity/C# client. The single most important idea below: **the movement simulation is
> shared code that runs identically on client and server.** If it isn't bit-identical,
> you mispredict every tick and the player rubber-bands forever.

---

## 0. The pairing principle

The server owns truth but lives ~half-an-RTT in the past from the client's view. So the
client runs **three timelines at once**:

| Timeline | Who | How it's rendered |
|---|---|---|
| **Predicted present** | *You* | Apply your input immediately, locally, then correct when the server's ack arrives |
| **Confirmed past** | *You*, authoritative | The last server snapshot of your entity — the anchor you replay from |
| **Interpolated past** | *Other players* | Rendered ~100ms behind, smoothly interpolated between snapshots |

You predict **only your own movement and cosmetic combat FX**. You never predict
authoritative *outcomes* — damage taken, confirmed kills, who you hit. Those arrive as
server events. (You may optimistically predict your own ammo/cooldown for UI snappiness,
then reconcile.)

That "render others in the past" timeline is exactly what the server's lag-compensation
rewind (`viewTime = serverNow - INTERP_DELAY - rttHalf`) is built to match. The two halves
are designed as one system.

---

## 1. Tick & timeline model

```
        you (predicted, "now")            others (interpolated, now - 100ms)
                 |                                   |
   inputs -------+------> send to server             |
                 |                                   |
   server ack ---+ (reconcile: snap + replay)        |
                 v                                   v
        [ local player capsule ]          [ remote capsules lerped between 2 snapshots ]
```

- Fixed client sim step `dt = 33.3ms`, matched to the server.
- Every step: sample input → assign monotonic `Seq` → **predict locally** → send → **store
  the input** in a pending buffer until the server acks it.

---

## 2. Module map

1. **Input + pending buffer** — sample, sequence, send, retain unacked.
2. **Prediction** — apply your input now via shared movement.
3. **Reconciliation** — on server snapshot, snap to truth + replay unacked inputs.
4. **Error smoothing** — blend out small corrections so they're invisible.
5. **Remote interpolation** — render other players in the buffered past.
6. **Combat client** — predicted FX now, authoritative resolution from server events.
7. **Clock/RTT sync** — feeds interp delay and the server's rewind.

---

## 3. Shared movement — the contract

Refactor the server's `StepMovement` and the client's prediction to call the **same**
function. No divergence allowed: same constants, same order of operations, same collision
world representation, deterministic math (avoid platform-divergent float behavior in this
path).

```csharp
// Compiled into BOTH the Unity client and the C# server. The only authority difference
// is who CALLS it and who gets to keep the result.
public static class SharedMovement
{
    public static void Step(ref MoveState m, in InputCmd cmd, float dt, ICollisionWorld world)
    {
        float speed = m.Crouching ? CROUCH_SPEED
                    : cmd.Sprint && m.Stamina > 0 ? RUN_SPEED
                    : WALK_SPEED;

        Vector3 wish = Vector3.ClampMagnitude(cmd.MoveDir, 1f) * speed;

        if (cmd.Jump && m.Grounded) m.Velocity.y = JUMP_VELOCITY;
        m.Velocity.y -= GRAVITY * dt;

        Vector3 delta = new Vector3(wish.x, m.Velocity.y, wish.z) * dt;
        m.Position    = world.CollideAndSlide(m.Position, delta);
        m.Grounded    = world.CheckGrounded(m.Position);
        m.Yaw = cmd.Yaw; m.Pitch = cmd.Pitch;
    }
}

public struct MoveState { public Vector3 Position, Velocity; public bool Grounded, Crouching;
                          public float Stamina, Yaw, Pitch; }
public struct InputCmd  { public uint Seq; public Vector3 MoveDir; public bool Jump, Sprint;
                          public float Yaw, Pitch; public uint ClientTick; }
```

---

## 4. Input + pending buffer

```csharp
class PredictionClient
{
    uint _seq;
    readonly List<InputCmd> _pending = new();   // unacked inputs, ordered by Seq
    MoveState _predicted;                        // what we render for the local player

    void FixedTick(float dt)
    {
        var cmd = SampleInput();                 // read keys/stick/look this tick
        cmd.Seq = ++_seq;
        cmd.ClientTick = ClientTick;

        // (a) PREDICT NOW — apply locally for zero-latency response
        SharedMovement.Step(ref _predicted, cmd, dt, _collisionWorld);

        // (b) RETAIN for reconciliation, then send
        _pending.Add(cmd);
        Net.SendInput(cmd);

        RenderLocalPlayer(_predicted);           // before any server reply
    }
}
```

---

## 5. Reconciliation — snap to truth, replay the rest

This is the heart of the client half. When the server snapshot arrives it tells us the
authoritative state of our entity **and** the last input it processed (`LastProcessedInput`
from the server doc). We discard acked inputs, reset to the server's truth, then re-apply
every input the server hasn't seen yet — landing back at a corrected "present."

```csharp
void OnSnapshot(Snapshot snap)
{
    uint acked = snap.LastProcessedInput;

    // 1. Drop inputs the server has already accounted for.
    _pending.RemoveAll(c => c.Seq <= acked);

    // 2. Reset our predicted state to the server's authoritative truth.
    MoveState corrected = snap.LocalPlayer.ToMoveState();   // position/velocity/grounded

    // 3. Replay every still-unacked input on top of the authoritative state.
    foreach (var cmd in _pending)                            // already Seq-ordered
        SharedMovement.Step(ref corrected, cmd, FixedDt, _collisionWorld);

    // 4. Compare to what we were rendering and fold the error into the smoother.
    Vector3 error = corrected.Position - _predicted.Position;
    if (error.magnitude > TELEPORT_THRESHOLD)
        _predicted = corrected;                              // hard snap (server teleported us / respawn)
    else
        _positionError += error;                             // smooth it out (step 6)

    _predicted.Velocity = corrected.Velocity;
    _predicted.Position = corrected.Position;                // logical truth...
    // ...but RENDER position = _predicted.Position - _positionError (smoothed toward 0)
}
```

If your shared movement is truly identical, `error` is ~zero almost every tick and the
player never notices reconciliation happening. Non-zero error means either real server
correction (you got blocked/teleported) or — the bug to hunt — client/server movement drift.

---

## 6. Error smoothing

Snapping the camera on every small correction looks awful. Accumulate the error and bleed
it toward zero over a few frames; render at `logical - residualError`.

```csharp
Vector3 _positionError;

Vector3 SmoothedRenderPosition(float frameDt)
{
    // exponential decay; tune SMOOTH_RATE (~10–20) for feel
    _positionError = Vector3.Lerp(_positionError, Vector3.zero, 1f - Mathf.Exp(-SMOOTH_RATE * frameDt));
    return _predicted.Position - _positionError;
}
```

---

## 7. Remote players — interpolate in the buffered past

Other players are **not** predicted. Buffer their snapshots and render them at
`renderTime = serverNow - INTERP_DELAY` (≈100ms, i.e. 2–3 snapshots at 30 Hz), lerping
between the two snapshots that straddle that time. This is what gives the server something
consistent to rewind to during hit validation.

```csharp
class RemoteEntity
{
    readonly RingBuffer<EntitySnap> _buf = new(32);   // {serverTime, pos, yaw, pitch, animState}

    public void Ingest(EntitySnap s) => _buf.Push(s);

    public void Render(double renderTime)
    {
        if (!_buf.Bracket(renderTime, out var a, out var b)) {
            if (_buf.Latest(out var last)) Place(last); // buffer dry → hold last (brief)
            return;
        }
        float t = (float)((renderTime - a.ServerTime) / (b.ServerTime - a.ServerTime));
        Place(new EntitySnap {
            Pos   = Vector3.Lerp(a.Pos, b.Pos, t),
            Yaw   = Mathf.LerpAngle(a.Yaw, b.Yaw, t),
            Pitch = Mathf.LerpAngle(a.Pitch, b.Pitch, t),
        });
    }
}
```

Bigger `INTERP_DELAY` = smoother remote motion but the server rewinds further (more
"shot-behind-cover" feel for victims). 100ms is a sane default; tune against your tick rate
and average RTT.

---

## 8. Combat on the client — predicted feel, authoritative truth

Fire the cosmetics immediately for responsiveness, but treat **all outcomes as the
server's**. Never show a confirmed kill or apply damage locally.

```csharp
void OnLocalFire()
{
    var w = _weapons[_activeSlot];
    if (w.Reloading || w.Ammo <= 0 || Time.now < w.NextFire) return;  // mirror server gate

    // Predicted, cosmetic only:
    w.Ammo--;                              // optimistic; reconciled by server ammo state
    w.NextFire = Time.now + w.FireInterval;
    PlayMuzzleFlash(); PlayTracer(); ApplyViewRecoil();   // pure UX

    // Send intent; the server decides if anything was hit.
    Net.SendFire(new FireIntent { Slot = _activeSlot, ClientTick = ClientTick });
    // Optional: show a *tentative* hitmarker on local raycast for feel — but kill feed,
    // damage numbers, and actual damage ONLY come from the server's HitEvent below.
}

void OnServerHitEvent(HitEvent e)         // authoritative — from server combat resolution
{
    if (e.Shooter == MyEntityId) ShowHitmarker(e.Headshot);
    if (e.Target  == MyEntityId) ApplyDamageFeedback(e.Damage);   // screen flash, etc.
    SpawnImpactFx(e.Point);
}

void OnServerStateReconcile(WeaponState authoritative)
{
    _weapons[_activeSlot].Ammo = authoritative.Ammo;   // correct optimistic prediction
}
```

Your local HP bar should track the server's broadcast HP, not a predicted value — predicting
damage you can't compute (you don't know the server's rewind result) just causes flicker.

---

## 9. Clock & RTT sync

Both interpolation delay and the server's rewind need a shared sense of time. Estimate
server time + smoothed RTT from periodic ping/pong (or snapshot send-timestamps) and keep
an EMA:

```csharp
void OnPong(double clientSent, double serverTime, double now)
{
    double rtt = now - clientSent;
    _smoothedRtt = _smoothedRtt <= 0 ? rtt : Mathf.Lerp((float)_smoothedRtt, (float)rtt, 0.1f);
    _serverClockOffset = serverTime + rtt * 0.5 - now;     // est. server "now" = localNow + offset
}
double ServerNow => Time.nowD + _serverClockOffset;
```

`renderTime` for remote entities = `ServerNow - INTERP_DELAY`. The server uses its own
`SmoothedRtt` for the rewind clamp, so these two clocks must roughly agree — drift here
shows up as hits that feel "off" to one side.

---

## 10. Pitfalls (the ones that actually bite)

- **Movement drift** — the #1 bug. Client and server must call identical `SharedMovement.Step`
  with identical constants and timestep. A single different gravity constant = permanent
  rubber-band. Build a desync detector: log when reconciliation `error` exceeds a small
  epsilon for N consecutive ticks.
- **Predicting outcomes** — never predict damage, kills, or who you hit. Predict movement
  and cosmetic FX only.
- **Unbounded pending buffer** — if the server goes silent, `_pending` grows forever. Cap
  it; if it overflows, stop predicting and show a connection warning.
- **Interp delay too small** — remote players stutter when a packet is late. Too large and
  victims die behind cover. Tune together with the server's `MAX_REWIND`.
- **Float determinism** — keep the shared movement path off operations that diverge across
  platforms; the WebGL/WASM client and the C# server must agree.

---

## 11. Build order

1. Clock/RTT sync (everything downstream needs a shared time).
2. Input sampling + pending buffer + send.
3. Local prediction via `SharedMovement.Step` (refactor server to share it).
4. Reconciliation (snap + replay) — verify `error ≈ 0` against the server in a 2-player test.
5. Error smoothing.
6. Remote entity interpolation.
7. Combat client (predicted FX + authoritative HitEvent handling).
8. Desync detector + connection-loss handling (prediction safety rails).
