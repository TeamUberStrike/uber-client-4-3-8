# Float-determinism decision (Phase 1)

**Decision: constrained float, not fixed-point.** The shared movement path uses only
IEEE-754 exactly-specified operations, so a native x64 server and a browser-WASM client
produce bit-identical results from the same inputs. We do NOT convert the simulation to
fixed-point.

## Why constrained float is sufficient

The cross-platform float horror stories come from three sources, all avoidable here:

1. **Transcendentals** (`sin`, `cos`, `exp`, `pow`) — implemented by platform libm with
   no exactness guarantee; results differ between glibc, MSVC CRT, and WASM shims.
2. **FMA contraction** — a compiler fusing `a*b + c` into one rounded operation on some
   targets but not others.
3. **SIMD horizontal reductions** — `Vector3.Length()`/`Dot()` style helpers may
   associate the adds differently depending on instruction set (`(x+y)+z` vs `(x+z)+y`),
   and float addition is not associative.

What IS exact, by spec, on both sides:

- `+ - * /` and `MathF.Sqrt` are **correctly rounded** under IEEE-754, and both the .NET
  JIT (x64) and the WebAssembly spec implement them strictly. WASM in particular has *no*
  relaxed-math mode in the MVP spec — `f32.add/mul/div/sqrt` are deterministic.
- The .NET JIT does **not** contract `a*b + c` into FMA (it preserves IEEE semantics
  unless you explicitly call `MathF.FusedMultiplyAdd`).

## The rules (enforced in `SharedMovement`)

Inside `SharedMovement.Step` and everything it calls:

- Only `+ - * /`, comparisons, and `MathF.Sqrt`.
- **No** `MathF.Sin/Cos/Tan/Exp/Pow/Atan2`, no `MathF.FusedMultiplyAdd`.
- **No** `Vector3.Length()/LengthSquared()/Normalize()/Dot()` — use the `Dot3`/`Len3`/
  `NormalizeXZ` helpers, which fix the association order explicitly:
  `((x*x) + (y*y)) + (z*z)`.
- `Vector3` remains the data carrier; component-wise `+ - *scalar` operators are fine
  (they lower to independent scalar IEEE ops — no horizontal reduction).
- No `double` in the movement path (mixed precision invites divergent rounding); the
  combat/timing layer may use `double` freely because it is **server-only**.
- Angle→direction (`DirFromAngles`) uses sin/cos and is therefore banned from the
  movement path; it is only called by the server's combat code, where cross-platform
  parity is irrelevant.

The client converts yaw + WASD into the world-space `MoveDir` vector **before** building
the `InputCmd` (that conversion may use sin/cos — both sides then consume the identical
vector), so the shared step itself never needs an angle function.

## Why not fixed-point

- Every retail tuning constant (walk 7, jump 15, gravity 50, friction 8, accel 15/3)
  would need re-deriving in fixed-point, and the "feel" verification would start over.
- Q16.16-style fixed-point overflows fast in `speed * accel * wishspeed` products and
  needs careful headroom analysis everywhere.
- The Unity client (Phase 3) natively computes in float; a fixed-point core would add a
  conversion boundary in the hottest per-frame path.
- The one real risk of constrained float — someone adding a banned op later — is cheap
  to police: the 10k-tick bit-identity test in `tests/` fails loudly if determinism
  breaks *within* a platform, and the cross-platform run (below) catches the rest.

## Verification status

- `tests/UberStrike.Tests` locks bit-identity across two 10k-tick replays and across the
  client-prediction vs server-authority code paths (same process).
- **Cross-platform half still owed (Phase 9 gate):** run the same harness once under
  `dotnet` on x64 and once compiled to WASM (e.g. `wasmtime` via the `wasi-experimental`
  workload, or a browser run of the Unity WebGL build's IL2CPP output), comparing the
  printed trajectory hash. The harness already prints the hash (`0x…`) for exactly this
  comparison. This needs a WASM runtime on the build box, which isn't installed today.
