# Implementation Plan — UberStrike 4.3.8 Netcode

Ordered phases from this scaffold to a shippable server-authoritative WebGL build. Each phase
lists its **goal**, **tasks**, **files**, and a **done-when** bar you can hold Claude Code to.
Phases mostly build on each other — do them in order. Phase numbers match the `// Phase N`
markers left in the code.

> Guiding constraint throughout: the client is hostile. Never add a feature that trusts a
> client-reported position, hit, damage value, or currency amount.

---

## Phase 0 — Baseline (done in this scaffold)

**Goal:** a compiling solution with the architecture in place and invariants under test.

- [x] Shared / Server / Client / Sandbox / Tests projects.
- [x] `SharedMovement` called by both sides; reconciliation; lag-comp rewind; combat core.
- [x] Test harness green; sandbox demonstrates the full loop.

**Done-when:** `./build.sh` builds, tests print `ALL TESTS PASSED`, sandbox runs.

---

## Phase 1 — Real movement model + determinism lock

**Goal:** replace the generic FPS movement with UberStrike's actual feel, and guarantee it is
identical on client (WASM) and server (native).

**Tasks**
- [x] Port UberStrike's movement — done from the Unity client's `CharacterMoveController`
  (Quake-3 lineage): stop-speed friction, dot-gated acceleration (air-strafe/bunny-hop
  preserving), edge-triggered jump, duck + headroom check, grounded grace, external
  impulses (jump pads/explosions), retail constants (walk 7 / jump 15 / gravity 50 /
  accel 15 ground + 3 air / friction 8 / stop-speed 8 / duck ×0.7). UberStrike has no
  sprint/stamina — `InputCmd.Sprint` became `Crouch`. Water/ladder/fly states need volume
  queries from the collision world → land with **Phase 4**.
  One deliberate fixed-tick adaptation (documented in `SharedMovement.ApplyAcceleration`):
  the accel add is capped at addSpeed — the canonical Q3 formula the original approximates
  at per-frame dt; uncapped it overshoots ~40% and oscillates at 30 Hz.
- [x] Pure function — no engine calls, no wall clock, no randomness; determinism-constrained
  math only (see `docs/determinism.md`).
- [x] Desync detector — `PredictionClient.DesyncDetected` fires after `DesyncTickLimit`
  consecutive reconciles above `DesyncEpsilon` (teleport snaps excluded); covered by tests.
- [x] Float-determinism strategy DECIDED: **constrained float** (not fixed-point) —
  rationale + rules in `docs/determinism.md`.

**Also fixed during the Phase-A audit of this phase:**
- [x] `FireIntent` now carries `SessionToken`; `ServerSimulation.EnqueueFire` validates
  ownership (any client could previously fire any player's weapon by forging EntityId).
- [x] Server spread no longer seeded from `ClientTick` (a modified client could grind
  candidate ticks until spread ≈ 0); seed components are all server-owned now.
- [x] `PlayerSnap` carries the full movement state (duck/jump-arm/grounded-grace/speed-scale)
  so reconciliation replay can't silently diverge.
- [x] Projects retargeted `net8.0` → `net10.0` (the build box only has the .NET 10 runtime).

**Files:** `SharedMovement.cs`, `MoveState.cs`, `InputCmd.cs`, `GameConstants.cs`,
`ICollisionWorld.cs`, `FlatCollisionWorld.cs`, `Protocol.cs`, `MovementSystem.cs`,
`PredictionClient.cs`, `CombatSystem.cs`, `CombatClient.cs`, `ServerSimulation.cs`,
`docs/determinism.md`, tests.

**Done-when:** running the same recorded input stream through the client core and the server
core yields bit-identical end state across 10k ticks, on both desktop and a WASM build.
**Status: desktop half DONE** — the harness replays a 10k-tick recorded stream twice and
through both code paths, comparing float BITS (and prints a trajectory hash). The WASM half
needs a WASM runtime on the build box (not installed); the printed hash makes it a one-line
comparison when it is. See `docs/determinism.md` § Verification status.

---

## Phase 2 — Wire serialization + schema versioning

**Goal:** turn the in-memory DTOs into a real wire format.

**Tasks**
- Binary (de)serialization for `InputPacket`, `FireIntent`, `Snapshot`, `HitEvent` (compact;
  quantize positions/angles).
- Snapshot **delta compression** against the last acked snapshot; baseline + delta.
- A protocol version byte and a strict reader that rejects malformed/short buffers (feeds the
  gateway's schema check).

**Files:** new `Serialization.cs` in Shared; `Protocol.cs`; gateway schema validation.

**Done-when:** round-trip serialize→deserialize is lossless within quantization tolerance; a
fuzzed/truncated buffer never throws past the reader boundary and is rejected as a schema
violation.

---

## Phase 3 — Unity client integration

**Goal:** drive the client core from Unity and render predicted/interpolated state.

**Tasks**
- MonoBehaviour adapter (see [`unity/README.md`](unity/README.md)): fixed-step input sampling,
  `BuildAndPredict` → send, `Reconcile` on snapshot, `SmoothErrors` per frame.
- `System.Numerics.Vector3` ↔ `UnityEngine.Vector3` conversions at the boundary only.
- Remote avatars driven by `RemoteInterpolator` sampled at `ServerNow - InterpDelay`.
- Hook `CombatClient` events to muzzle flash / tracer / hitmarker / damage feedback.

**Files:** `unity/` adapter scripts (new), referencing `UberStrike.Client`.

**Done-when:** in a Unity scene, the local player moves with zero input latency, remote dummies
interpolate smoothly, and corrections are visually imperceptible on a clean LAN.

---

## Phase 4 — Real collision world + line-of-sight

**Goal:** replace `FlatCollisionWorld` with the actual map geometry, shared by both sides.

**Tasks**
- Export each map's collision (Unity colliders) to a server-loadable format (baked mesh or BVH).
- Implement `CollideAndSlide` and `CheckGrounded` against it; ensure the client uses the same
  data so prediction matches.
- Implement `LineOfSight` as a real raycast against static geometry (this is what makes
  wallbang detection real — currently it always returns `true`).

**Files:** new `BakedCollisionWorld.cs` in Shared (implements `ICollisionWorld`); map export
tooling; `CombatSystem` LOS path.

**Done-when:** prediction matches server movement on real maps within epsilon; shots through
solid geometry are rejected and flagged.

---

## Phase 5 — Full combat

**Goal:** complete the weapon/combat model.

**Tasks**
- Per-bone or multi-capsule hitboxes recorded in `HitboxHistory` (not just one capsule + head).
- Reload, weapon switching across all slots, magazine/reserve management, burst/auto fire modes.
- Per-weapon spread/recoil patterns applied server-side; shotgun pellet spread; projectile
  weapons (splatter) vs hitscan.
- Damage falloff tuning per weapon; armor model finalization.

**Files:** `CombatSystem.cs`, `Weapons.cs`, `HitboxHistory.cs`, `PlayerState.cs`.

**Done-when:** all weapon classes behave correctly server-side; hit registration matches what
the shooter saw within the rewind window; tests cover each weapon's gate and damage.

---

## Phase 6 — Lag-compensation & interpolation tuning

**Goal:** make hit registration fair across realistic mobile RTTs.

**Tasks**
- Tune `InterpDelaySeconds`, `MaxRewindSeconds`, and history buffer length together for the
  expected mobile latency/jitter profile.
- Per-client smoothed RTT feeding the rewind clamp; clamp abusers who inflate RTT.
- Snapshot send-rate vs tick-rate decoupling if needed; interpolation buffer starvation
  handling (brief extrapolation, then freeze).

**Files:** `GameConstants.cs`, `CombatSystem.cs`, `RemoteInterpolator.cs`, `ClockSync.cs`.

**Done-when:** at 80–150ms RTT with jitter, shooters hit what they aim at and victims rarely
die "behind cover" beyond the configured rewind bound.

---

## Phase 7 — Production transport (WebSocket/WSS) + rate limiting

**Goal:** replace `InProcessLink` with real networking suited to a browser client.

**Tasks**
- Server: a WebSocket endpoint (`System.Net.WebSockets` or Photon) implementing `IServerLink`;
  per-connection session/auth handshake binding `EntityId` ↔ `SessionToken`.
- Client: a browser-WebSocket implementation of `IClientLink` (works from WASM).
- **Rate limiting** at the transport: cap inputs/sec, fires/sec, bytes/sec per connection;
  drop and flag floods (the gateway assumes this layer exists).
- Connection lifecycle: join/leave, reconnect with state resync, idle timeout.
- WSS/TLS only in production.

**Files:** new `WebSocketServerLink.cs`, `WebSocketClientLink.cs`; handshake; rate limiter.

**Done-when:** a browser client connects over WSS, plays through the real loop, survives a
brief disconnect/reconnect, and input floods are throttled without affecting other players.

---

## Phase 8 — Anomaly detection + telemetry

**Goal:** catch *possible-but-superhuman* cheating (aimbot/triggerbot/wallhack) that validation
can't refuse outright.

**Tasks**
- Tune `AnomalyTracker` weights and thresholds against real play data; add features: hit-rate
  vs distance, headshot ratio, aim-snap angular velocity distribution, pre-fire-on-unseen-enemy.
- Telemetry pipeline: stream anomaly events + sampled inputs to a store; review dashboard.
- Graduated response: shadow flag → queue for review → action. Avoid auto-banning on a single
  heuristic.

**Files:** `AnomalyTracker.cs`; new telemetry sink; out-of-process dashboard.

**Done-when:** known-cheat replays score clearly above legitimate high-skill play, with a
low false-positive rate on a held-out sample.

---

## Phase 9 — Build hardening

**Goal:** raise the cost of reverse-engineering and tampering with the WebGL client.

**Tasks**
- Encrypt/obfuscate IL2CPP `global-metadata.dat` (decrypt at runtime) to slow `Il2CppDumper`.
- Strip debug symbols; **disable remote debugging in release** builds.
- Resolve the float-determinism decision from Phase 1 (fixed-point movement path is the
  robust option for WASM↔native parity).
- Integrity signals: client build hash, basic tamper detection (treated as soft signals, not
  trusted security).
- Serve assets and sockets over HTTPS/WSS only.

**Files:** Unity build pipeline config; `SharedMovement` (if fixed-point); deployment config.

**Done-when:** a release build resists trivial metadata dumping and live-memory editing via
DevTools, and movement stays deterministic across client and server.

---

## Phase 10 — Scale & operations

**Goal:** run many concurrent matches reliably.

**Tasks**
- Room/match sharding; per-match tick budget and profiling; cap players/room.
- Server tick-loop performance pass (allocation-free hot path; pool buffers).
- Metrics: tick time, snapshot size, bandwidth/player, rewind cost; alerting.
- Load tests at target concurrency.

**Files:** server host/orchestration; profiling harness.

**Done-when:** target concurrent matches run within tick budget and bandwidth goals under load.

---

## Dependency order (quick reference)

```
Phase 1 (movement+determinism)
   ├─► Phase 2 (serialization)
   ├─► Phase 3 (Unity client)
   └─► Phase 4 (collision/LOS) ─► Phase 5 (combat) ─► Phase 6 (lag-comp tuning)
Phase 7 (transport) depends on Phase 2
Phase 8 (anomaly/telemetry) depends on Phase 5
Phase 9 (hardening) depends on Phase 1's determinism decision
Phase 10 (scale) is last
```

Start at **Phase 1** — everything downstream assumes a real, deterministic `SharedMovement`.
