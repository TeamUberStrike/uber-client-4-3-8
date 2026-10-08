# Phase 9 — Build hardening (WebGL/IL2CPP)

Raising the cost of reverse-engineering and tampering with the browser client. **None of this
is trusted as security** — a WebGL build is fully attacker-controlled, so the only real defense
is the server authority this PR builds (see `server-authority.md` §9). Hardening is
defense-in-depth: it slows casual cheaters and catches stale/naive tampering, nothing more.

## Determinism decision (carried from Phase 1 — the load-bearing one)

**Constrained `System.Numerics` float, NOT fixed-point.** Decided in `determinism.md` and proven
by the 10k-tick bit-identical test (movement + baked-mesh world). The movement/geometry hot path:
- uses only `+ - * /`, `MathF.Sqrt`, and fixed-order vector ops (no `MathF.Sin/Cos/Tan/FMA` in
  `SharedMovement.Step` — transcendentals live only in server-side aim code that never feeds
  reconciliation);
- avoids reordering that would change rounding;
- runs identically as native C# on the server and WASM on the client.

WASM mandates IEEE-754 single/double with no implicit `x87`-style extended precision, which is
exactly why constrained `System.Numerics` is enough and fixed-point's complexity isn't needed.
**Owed:** the cross-runtime confirmation (run the determinism harness compiled to WASM and match
the trajectory hash the native test prints) — needs a WASM runtime on the build box.

## Unity / IL2CPP build settings (release)

In `LocalIOSBuild`-style build code or Player Settings for the WebGL target:
- **Managed stripping:** `ManagedStrippingLevel.High` with the project `link.xml` (already on the
  WebGL branch) preserving `UberStrike.UnitySdk` + the netcode types. (The diagnostic build flips
  this to `Low` + `FullWithStacktrace` to name crashes — never ship that profile.)
- **Strip engine code / strip debug symbols**; **`Il2CppCompilerConfiguration.Master`** for release.
- **`WebGL.debugSymbolMode = Off`** (no `.symbols.json` in production) — keeps function names out
  of the shipped build.
- **Exception support:** `WebGLExceptionSupport.None` (or `ExplicitlyThrownExceptionsOnly`) in
  release — `FullWithStacktrace` ships symbol-rich frames.
- **Compression:** Brotli, decompression fallback off (server sends `Content-Encoding: br` — the
  nginx config in `uberstrike-webgl-photon`); smaller surface, and the host serves it correctly.

## global-metadata.dat

IL2CPP ships type/method metadata in `Build/*.data` (`global-metadata.dat`), which
`Il2CppDumper` parses to reconstruct the managed surface. Options, cheapest → strongest:
1. **Default** — already non-trivial to read but dumpable.
2. **XOR/obfuscate the header magic** + de-obfuscate in a tiny native/JS pre-pass (slows naive
   `Il2CppDumper` runs; widely-known bypass exists, so this is a speed bump).
3. **Encrypt the metadata blob**, decrypt at startup into memory. Strongest of the three, most
   work, and still defeatable by a memory dump — diminishing returns, so do (1)/(2) and lean on
   server authority.

## Integrity signal (soft)

`IntegrityRegistry` (server-side, this commit): the host registers the deploy-time hash of the
shipped build; the client reports the hash of what it's running on connect; a mismatch is a
**soft** signal (telemetry + a small anomaly bump) and the hook to force-update outdated clients.
Never a hard ban — forgeable, but cheaply catches stale builds and naive tampering.

## Transport

- **WSS/HTTPS only** in production (browsers block mixed content from an https page anyway — the
  page being https *requires* `wss://` realtime, which dovetails with the Photon-WS listener +
  cert ask). The `WebSocketServerLink` binds loopback for our own testing; the production listener
  terminates TLS (or sits behind the nginx/Photon TLS the runbook configures).
- Per-connection rate limits (Phase 7) cap input/fire/byte floods at the transport edge.

## What hardening does NOT do

It does not make the client trustworthy. Live WASM memory is editable via DevTools with no
jailbreak; the metadata ships in the build. Every authoritative outcome — position, health, ammo,
hit, damage, score, currency, visibility — is owned by the server and re-derived from intent, so
a memory editor only changes the cheater's cosmetic view. Hardening just raises the cost of the
first step; the wall behind it is server authority.
