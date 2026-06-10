# Anti-cheat by cheat — what we prevent, how, and who proves it works

For each major cheat class: the **defense**, **who uses it** in production (with a source), **how**
it works, **why** it works, and the **honest status** in this PR. The sources are deliberately
from games/companies with *strong* anti-cheat reputations (Valve, Riot/VALORANT, Blizzard/
Overwatch) and the canonical networking literature — not from cheat-ridden titles whose
"anti-cheat" is a bypassable client-side scanner bolted onto a trusting client.

## The framing: why server authority, not a client scanner

The credible model is **defense in depth**, and VALORANT states the ordering explicitly: *"building
the game to be cheat-resistant (e.g., Fog of War and server authoritative movement netcode), making
cheat development prohibitive (e.g., Vanguard and anti-tamper), and detecting and removing cheaters"*
([VALORANT Anti-Cheat: What, Why, and How](https://playvalorant.com/en-us/news/dev/valorant-anti-cheat-what-why-and-how/)).

Cheat-resistant **architecture comes first**; the kernel driver and the detection ML come *after*,
and explicitly as backups — never as the primary defense. Games "full of cheaters" usually invert
this: a trusting client plus a client-side scanner that attackers patch out. In a WebGL/WASM build
the client is *fully* attacker-controlled (live memory editable from DevTools, IL2CPP metadata in
the `.data` file), so a client scanner is even weaker than usual. **PR #77 is the first pillar —
server authority.** PR #71 (UberBeat scanner) is the third pillar, useful only where the client has
some integrity (Windows standalone), never on WebGL.

The single principle under everything below:

> **The client sends INTENT; the server owns OUTCOMES.** *"Clients only send their controller
> inputs and the server has full authority of what actually happens."* — Overwatch netcode,
> [GDC 2017, Tim Ford](https://www.gdcvault.com/play/1024001/-Overwatch-Gameplay-Architecture-and).

---

## 1. Speed hack / teleport / fly / noclip

- **Defense:** server-authoritative movement — the server simulates movement from inputs; client
  position is never read from packets, only predicted locally and reconciled.
- **Used by:** Valve Source engine ([Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)),
  VALORANT ("server authoritative movement netcode"), Overwatch (GDC 2017). The client-prediction/
  server-reconciliation pattern is canonically documented by
  [Gabriel Gambetta](https://www.gabrielgambetta.com/client-side-prediction-server-reconciliation.html).
- **How:** the client predicts immediately for responsiveness, but the authoritative server re-runs
  the *same* movement step and its snapshot is ground truth; a per-tick displacement guard rejects
  anything past the physical maximum.
- **Why:** a hacked client that sets its position to anything simply gets corrected on the next
  snapshot — the server never adopts a client-supplied position.
- **Status in PR #77:** ✅ **DONE (Phase 1).** `SharedMovement.Step` is the real UberStrike movement,
  run identically on `PredictionClient` and the server `MovementSystem`; the speed/teleport guard is
  in `MovementSystem.Apply`. 10k-tick bit-identical determinism test green.

## 2. No-recoil / no-spread

- **Defense:** the server computes spread/recoil; the client's version is cosmetic only.
- **Used by:** Valve (recoil/spread are server-simulated in the authoritative model,
  [Source Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking));
  CS2 reconstructs the exact shot server-side from a sub-tick timestamp
  ([Counter-Strike 2, Valve Developer Community](https://developer.valvesoftware.com/wiki/Counter-Strike_2)
  — sub-tick: "servers know the exact instant that motion starts, a shot is fired").
- **How:** the server applies the spread cone to the shot itself, seeded only from server-owned
  values; a client that zeroes its local recoil just sees a wrong tracer.
- **Why:** the cheater's "no-spread" never reaches the server's hit math, so it changes nothing but
  their own screen.
- **Status:** ✅ **DONE.** `CombatSystem.ApplyServerSpread`. Hardened this PR: the seed no longer
  uses the client's tick (which a modified client could grind for a zero-spread roll) — all seed
  components are server-owned, with a test asserting `ClientTick` can't change the outcome.

## 3. Rapidfire / no-reload / infinite ammo / fire-while-stowed

- **Defense:** the server owns the weapon state machine — firerate clock, ammo count, reload timer,
  active slot — and gates every shot on it.
- **Used by:** standard in every server-authoritative shooter; Overwatch predicts abilities but the
  server holds the cooldown/resource truth and rolls back mispredictions
  ([GDC 2017](https://www.gdcvault.com/play/1024001/-Overwatch-Gameplay-Architecture-and)).
- **How:** `state gate → consume`: a shot is rejected unless the server's own `NextFireTime`,
  `Ammo`, `Reloading` and `ActiveSlot` allow it; the server, not the client, decrements ammo.
- **Why:** the client can spam fire intents all it wants; the server discards every one that breaks
  its own timing/resource invariant.
- **Status:** ✅ **DONE.** `CombatSystem.HandleFire` steps (1) state gate + (2) consume.

## 4. Aimbot

- **Defense (two layers):** (a) the server owns hit registration, so an aimbot can only *aim* — it
  can't conjure a hit the geometry doesn't support; (b) statistical/behavioral **detection** of
  superhuman aim, because perfect aim is still *legal input* and can't be prevented outright.
- **Used by:** server-authoritative hit-reg is universal (Valve, CS2, Overwatch). Detection layer:
  VALORANT uses *"Vanguard cheat detection and machine learning"*
  ([What, Why, How](https://playvalorant.com/en-us/news/dev/valorant-anti-cheat-what-why-and-how/));
  this is the explicit "detection" pillar that sits *behind* architecture.
- **How:** server raycast from the server-owned muzzle decides hits; on top, an anomaly tracker
  scores angular-velocity snaps, accuracy and headshot ratios for review — never an instant ban on
  one heuristic.
- **Why:** architecture caps what an aimbot can *achieve*; detection catches the residual (a human
  can't flick 2000°/s onto heads every shot). Graduated response avoids false bans.
- **Status:** ⚠️ **PARTIAL.** Server hit-reg ✅ (`CombatSystem` raycast). Anomaly scoring is a
  **soft scaffold** (`AnomalyTracker`: aim-snap/accuracy/headshot signals) — real tuning against
  play data + a review pipeline is **Phase 8**, not built. Honest: we *raise the floor*, we don't
  yet *detect* aimbots well.

## 5. Triggerbot

- **Defense:** same as aimbot — server decides the hit + firerate gate + detection of inhuman
  reaction-time-to-fire.
- **Used by:** same sources; triggerbots are a detection target for the ML/heuristic pillar
  (VALORANT/Vanguard).
- **How:** the firerate gate and server hit-reg already bound it; `AnomalyTracker` is where a
  "fires within N ms of crosshair-on-target, every time" signal would live.
- **Why:** the server gates the rate and owns the hit; detection handles the "too perfect timing"
  residual.
- **Status:** ⚠️ **PARTIAL** — gating ✅, the specific reaction-time signal is **Phase 8** (not built).

## 6. Wallhack / ESP (seeing enemies through geometry)

- **Defense — the gold standard: don't send what the player can't see.** Server-side visibility
  culling (a "Fog of War") withholds the positions of out-of-sight enemies, so there is nothing in
  client memory for a wallhack to read.
- **Used by:** VALORANT's **Fog of War** —
  [Demolishing Wallhacks with VALORANT's Fog of War](https://playvalorant.com/en-us/news/dev/demolishing-wallhacks-valorants-fog-war/):
  *"If an opponent was behind a wall, we wouldn't send their location to enemy players."* Built with
  server-side occlusion culling (Potentially Visible Sets over map voxels) plus *"looking into the
  future"* — expanding the actor's bounding box by velocity × look-ahead so peeks don't pop in.
- **How:** each tick the server runs a visibility check per viewer and only networks actors that are
  (or are about to be) visible.
- **Why:** *"there would be nothing for the wallhack to see"* — you can't render information the
  client never received.
- **Status:** ❌ **NOT BUILT — biggest open gap.** Snapshots currently send ALL other players to
  every client (`ServerSimulation.BuildSnapshot` → `Others`). An ESP cheat reading WASM memory sees
  everyone. **This is the highest-value next feature** and I'd add it as a new phase: per-viewer
  relevance culling against the baked collision world (which Phase 4 brings), exactly the VALORANT
  model. Flagging it loudly rather than pretending the current design stops ESP — it doesn't.

## 7. Wallbang / ESP-assisted firing through geometry

- **Defense:** server line-of-sight check — even if a client fires at a target through a wall, the
  server rejects the hit if geometry blocks the shot.
- **Used by:** implicit in all server-authoritative hit-reg with real collision (Valve, CS2).
- **How:** `CombatSystem.HandleFire` step (7) tests `world.LineOfSight(origin, hitPoint)` before
  applying damage.
- **Why:** the hit is only credited if the server agrees there's a clear line — ESP can't turn an
  occluded target into a kill.
- **Status:** ❌ **STUBBED — OPEN.** `FlatCollisionWorld.LineOfSight` currently returns `true`
  always. The *call site is wired*, but until **Phase 4** exports the real baked map colliders, LOS
  enforces nothing. Wallbang is open today. (This is why I recommend Phase 4 next over Phase 2.)

## 8. Lag switch / backtrack / fake-latency abuse

- **Defense:** lag compensation that rewinds the world to what the shooter saw — but with the rewind
  **clamped** and bound to *server-observed* RTT, not client-claimed latency.
- **Used by:** Valve's lag compensation
  ([Lag Compensation](https://developer.valvesoftware.com/wiki/Lag_Compensation)): the server
  *"uses a player's latency to rewind time when processing a usercmd, in order to see what the
  player saw."* Overwatch *"rewinds the world back to the shooter's frame of reference"* (GDC 2017).
  CS2 reconstructs from a server-validated sub-tick timestamp.
- **How:** the server rewinds target hitboxes to `serverNow − interpDelay − ½·RTT`, clamped to a max
  window (`HitboxHistory.Rewind`, `MaxRewindSeconds`).
- **Why:** an attacker inflating latency to "backtrack" onto where you stood a second ago is capped
  by the clamp; binding to server-measured RTT (not a client field) removes the lever.
- **Status:** ⚠️ **PARTIAL.** Clamped rewind ✅ (`HitboxHistory` + `MaxRewindSeconds = 0.25s`). The
  RTT is currently a field on `PlayerState` — it **must be measured server-side** when real transport
  lands (**Phase 7**); until then the clamp protects but the RTT source isn't yet trustworthy. Noted
  in the code.

## 9. Currency / XP / score injection

- **Defense:** economy state mutates only through validated, **idempotent** server events; balances
  are never read from the client.
- **Used by:** the idempotency-key pattern is the payments-industry standard — Stripe:
  *"no matter how many times a request is retried … exactly one charge is created"*
  ([Designing robust APIs with idempotency](https://stripe.com/blog/idempotency)); a key reused with
  a different payload is rejected (replay protection). Same shape applies to in-game currency.
- **How:** `EconomySystem.TryPurchase` checks a per-player `SeenPurchaseKeys` set (no double-spend on
  a retried request), validates price server-side, and debits atomically.
- **Why:** the client can't report a balance, can't replay a purchase to dupe an item, and can't
  set a price.
- **Status:** ✅ **DONE** (idempotent + server-priced). Kill/score awards are server-only
  (`CombatSystem.ResolveKill`).

## 10. Packet replay / forgery / spoofing (incl. firing another player's weapon)

- **Defense:** authenticate every packet to the session that owns the entity; enforce strictly
  increasing input sequence numbers within a bounded window; range-check schema.
- **Used by:** general server-authority practice; the sequence-number + bound-window approach is the
  replay defense in the snapshot/ack model documented by
  [Glenn Fiedler](https://gafferongames.com/post/reliable_ordered_messages/) and Gambetta.
- **How:** `InputGateway.Validate` checks session token == entity owner, `Seq` strictly increasing
  and within `MaxSeqGap`, and finite/in-range input. Fire intents pass the same ownership check.
- **Why:** a replayed/reordered packet is dropped; a spoofed entity id without the matching session
  token is rejected.
- **Status:** ✅ **DONE.** `InputGateway` + (hardened this PR) `ServerSimulation.EnqueueFire`
  session-token check — closing a real hole where a forged `EntityId` let anyone fire any player's
  weapon.

## 11. Memory editing (local HP / ammo = 9999)

- **Defense:** architectural — the server never reads health/ammo/etc. from the client, so a local
  edit changes only the cheater's own screen.
- **Used by:** the stated rationale of every server-authoritative design (VALORANT's "cheat-resistant
  architecture" pillar; the Overwatch "server has full authority" principle).
- **How:** all authoritative state (`Health`, `Armor`, `Ammo`, `Currency`, position) lives only on
  the server `PlayerState`; snapshots flow server→client, never the reverse.
- **Why:** *the trust boundary is the server* — editing client memory is cosmetic.
- **Status:** ✅ **DONE by design.** No authoritative field is ever assigned from a client packet.

## 12. Desync / determinism exploitation

- **Defense:** make the shared simulation deterministic so client and server agree bit-for-bit, and
  **detect** any divergence.
- **Used by:** deterministic-lockstep lineage — Age of Empires' *"1500 Archers on a 28.8"*
  ([Bettner & Terrano, GDC 2001](https://www.gamedevs.org/uploads/1500-archers-on-a-28.8-network-programming-in-age-of-empires-and-beyond.pdf)),
  whose hard-won lesson is that *any* tiny divergence breaks the sim and must be checksum-detected.
  The float-determinism constraints follow
  [Glenn Fiedler, Floating Point Determinism](https://gafferongames.com/post/floating_point_determinism/).
- **How:** the movement path uses only IEEE-exact ops (no transcendentals/FMA/SIMD horizontal
  reductions — see `determinism.md`); a desync detector alarms when reconcile error persists.
- **Why:** if the two sims can't diverge, a client can't exploit a divergence; if they do diverge,
  we find out immediately.
- **Status:** ✅ **DONE (Phase 1).** Constrained-float decision + `PredictionClient` desync detector
  + 10k-tick bit-identity test.

---

## Honest scorecard

| Cheat | Defense | Status |
|---|---|---|
| Speedhack/teleport/fly | server movement authority | ✅ done (P1) |
| No-recoil/no-spread | server-side spread | ✅ done |
| Rapidfire/no-reload/infinite ammo | server weapon state gate | ✅ done |
| Aimbot | server hit-reg + detection | ⚠️ floor done; detection = P8 |
| Triggerbot | gate + detection | ⚠️ gate done; detection = P8 |
| **Wallhack/ESP (seeing)** | **Fog of War (don't send)** | ❌ **not built — top gap** |
| Wallbang (firing through walls) | server LOS | ❌ stubbed (LOS=true until P4) |
| Lag switch/backtrack | clamped rewind, server RTT | ⚠️ clamp done; RTT source = P7 |
| Currency/XP/score injection | idempotent server economy | ✅ done |
| Packet replay/forgery/spoof | auth + seq + schema | ✅ done |
| Memory editing | server never reads client state | ✅ done by design |
| Desync exploitation | determinism + detector | ✅ done (P1) |

**Six of twelve fully defended, four partial, two open (ESP + wallbang) — both blocked on the same
thing: the real collision world / visibility (Phase 4), which is why I recommend it as the next
phase.** Nothing here is overstated: where a defense is stubbed, it's marked stubbed.
