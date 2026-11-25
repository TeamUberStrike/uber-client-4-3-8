# UberStrike 4.3.8 — Server-Authoritative Netcode Scaffold

A buildable .NET 8 starting point for shipping UberStrike as a **WebGL build on mobile**
without the game being trivially cheatable. It implements the two halves that have to fit
together: an **authoritative server** that owns all game truth, and a **prediction client**
that makes that authority feel instant.

This repo is a *scaffold to hand to Claude Code* — the architecture, interfaces, core logic,
a runnable demo, and a passing test harness are here; the production-grade pieces (real
collision geometry, WebSocket transport, Unity wiring) are stubbed behind clean seams and
laid out step-by-step in [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md).

---

## Why this architecture

A WebGL build is just JavaScript + WebAssembly running in a browser the player fully
controls. Remote DevTools (`chrome://inspect`, Safari Web Inspector) give a console into live
WASM memory with **no root or jailbreak**, and the IL2CPP metadata ships inside the `.data`
file. So nothing on the client can be trusted — not positions, not hits, not damage, not the
`platform` field in a packet.

The only defense that survives that is **architectural**: the client sends *intent*, the
server owns *outcomes*. Two documents in [`docs/`](docs/) explain the full reasoning:

- [`docs/server-authority.md`](docs/server-authority.md) — the validation/authority layer.
- [`docs/client-prediction.md`](docs/client-prediction.md) — prediction, reconciliation, interpolation.

---

## The loop in one picture

```
   CLIENT (predicted present)                         SERVER (authoritative)
   ─────────────────────────                          ──────────────────────
   sample input ─┐
   predict NOW   │  SharedMovement.Step ──────┐
   render        │   (same code) ─────────────┤──────► InputGateway (auth/schema/replay)
   send intent ──┼──────────────► InputPacket  │        MovementSystem.Apply (+ guards)
                 │                              │        record hitbox history
   reconcile ◄───┼──────────── Snapshot ◄──────┤        CombatSystem.HandleFire
   (snap+replay) │             (+ ack seq)      │          firerate → rewind → raycast
   interpolate   │                              │          → LOS → server damage → score
   others ◄──────┘             HitEvent ◄───────┘        EconomySystem (idempotent)
```

The single most important rule: **`SharedMovement.Step` is one piece of code compiled into
both sides.** If client and server ever simulate movement differently, the player rubber-bands
forever. Everything else is built around protecting that contract.

---

## Project layout

```
uberstrike-netcode/
├── UberStrike.Netcode.sln
├── build.sh                     # dotnet build + run tests + run demo
├── docs/                        # the two design documents
├── src/
│   ├── UberStrike.Shared/       # types compiled into BOTH sides
│   │   ├── GameConstants.cs     #   tunables that MUST match on both ends
│   │   ├── SharedMovement.cs    #   ◄── the deterministic movement step
│   │   ├── Geometry.cs          #   ray/sphere + ray/capsule for hit tests
│   │   ├── ICollisionWorld.cs   #   collision abstraction (+ FlatCollisionWorld stub)
│   │   ├── Weapons.cs           #   authoritative weapon + shop tables
│   │   ├── Protocol.cs          #   wire messages (Input/Fire/Snapshot/HitEvent)
│   │   └── Transport.cs         #   IClientLink/IServerLink + InProcessLink
│   ├── UberStrike.Server/       # authoritative simulation
│   │   ├── ServerSimulation.cs  #   the fixed-tick orchestrator
│   │   ├── InputGateway.cs      #   auth / schema / replay validation
│   │   ├── MovementSystem.cs    #   shared step + speed/teleport/bounds guards
│   │   ├── HitboxHistory.cs     #   ring buffer for lag-compensation rewind
│   │   ├── CombatSystem.cs      #   ◄── the anti-cheat centerpiece
│   │   ├── EconomySystem.cs     #   idempotent currency/purchases
│   │   ├── AnomalyTracker.cs    #   soft suspicion scoring (aimbot/wall tells)
│   │   └── PlayerState.cs       #   authoritative per-player state
│   ├── UberStrike.Client/       # engine-agnostic client core (no Unity dependency)
│   │   ├── PredictionClient.cs  #   predict + reconcile (snap & replay) + smoothing
│   │   ├── RemoteInterpolator.cs#   render remote players in the buffered past
│   │   ├── CombatClient.cs      #   predicted FX, authoritative outcomes
│   │   └── ClockSync.cs         #   server-time / RTT estimation
│   └── UberStrike.Sandbox/      # runnable in-process demo of the whole loop
└── tests/
    └── UberStrike.Tests/        # zero-dependency harness proving the invariants
```

---

## Build & run

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
./build.sh
# or manually:
dotnet build UberStrike.Netcode.sln -c Release
dotnet run --project tests/UberStrike.Tests      # invariant checks (exit 0 = all pass)
dotnet run --project src/UberStrike.Sandbox      # the predict/reconcile/fire demo
```

The sandbox walks a client forward (exercising prediction + reconciliation under simulated
latency), then aims at a dummy and fires — printing reconciliation error, pending input
count, dummy HP dropping from **server-applied** damage, and the shooter's anomaly score.

> No external NuGet packages are used, so it restores/builds offline.

---

## What the tests prove

| Test | Invariant it locks down |
|---|---|
| Shared-movement determinism | Client prediction and server simulation produce identical state from identical input |
| Reconciliation convergence | Snapping to server truth + replaying unacked inputs lands back on the prediction (err ≈ 0) |
| Firerate gate | A second shot inside the weapon's interval is rejected (no rapid-fire) |
| Server-owned damage | Health changes only via the server combat path; a hit reduces target HP |
| Speed-hack clamp | A giant `MoveDir` can't move the player faster than the physical max step |
| Gateway rejection | Replayed sequence, spoofed token, and out-of-range input are all refused |

---

## How the pieces kill specific cheats

| Cheat | Why it fails | Where |
|---|---|---|
| Teleport / fly / speed | Server re-simulates movement and clamps per-tick displacement | `MovementSystem.cs` |
| Position spoofing | Client positions are never written to state; only intent is | `InputGateway` + `MovementSystem` |
| Rapid-fire / no-reload / infinite ammo | Server owns the firerate clock and ammo | `CombatSystem.HandleFire` (steps 1–2) |
| Shoot-through-walls | Server computes origin/aim and runs its own LOS test | `CombatSystem` (steps 3, 7) |
| Damage / hit injection | Damage and hits are computed and broadcast only by the server | `CombatSystem` (step 8) |
| Currency / unlock injection | Mutations go through one idempotent, balance-checked path | `EconomySystem.cs` |
| Packet replay / out-of-order | Strictly increasing, windowed sequence numbers | `InputGateway.cs` |
| Aimbot / triggerbot (soft) | Statistical suspicion scoring for review, not hard bans | `AnomalyTracker.cs` |

---

## Status / what's intentionally stubbed

This is a scaffold; these seams are where the real work goes (full detail in the plan):

- `FlatCollisionWorld` is a ground plane + box. `LineOfSight` always returns `true`. → **Phase 4**
- Transport is an in-process link with simulated latency; no sockets yet. → **Phase 7**
- `SharedMovement` is a clean generic FPS model, not UberStrike's exact movement feel. → **Phase 1**
- Combat hit test uses one capsule + one head sphere (no per-bone hitboxes). → **Phase 5**
- Wire messages are DTOs; no binary (de)serialization / schema versioning yet. → **Phase 2**

---

## Handing this to Claude Code

Point Claude Code at this folder and at [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md).
The plan is ordered, each phase has explicit *done-when* criteria and lists the files it
touches, and the test harness gives Claude Code a green/red signal to work against. Start at
Phase 1 (the others depend on a real, locked-down `SharedMovement`).
