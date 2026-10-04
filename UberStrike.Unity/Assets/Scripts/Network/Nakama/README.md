# Nakama transport

Photon is gone. `UberStrike.UnitySdk.dll` (uber-server-4-3-8 `nakama-sdk`) drives `INetworkPeer`;
`NakamaPeer` implements it on nakama-unity 3.22.1. Game code above the SDK is unchanged.

| File | What |
|---|---|
| `NakamaBootstrap` | sets `PeerFactory.Create` before any manager `Awake`; config; kill switch |
| `NakamaSession` | Unity side: game-wide `NakamaLink`, `UnityWebRequestAdapter`, `NewSocket(useMainThread: true)`, token hooks |
| `NakamaLink` | one `IClient`/`ISession`/`ISocket`: auth, shared socket, `uber_time` clock, routing by match id, idle close (no UnityEngine) |
| `NakamaSessionHost` | `Update` -> `NakamaLink.Tick`, quit -> close |
| `NakamaPeer` | one per `PhotonClient` (Comm, Lobby, Game, probes); Photon op/status/event semantics |
| `NakamaFraming` | byte + JSON framing, BCL only (tests: `/NakamaTests`) |
| `NakamaClock` | min-RTT offset, slew, int31 server ms |
| `NakamaConfig` | endpoint, key, dev auth; `ApplyNode` = web row -> dial target |
| `NakamaNodeRows` | web rows -> node + Play page rows (no UnityEngine, tests) |
| `NakamaServerList` | `Apply(AuthenticateApplicationView)`: endpoint, Play page, comm row |

## Endpoint

Truth = web DB `PhotonServers` CommServer row (UsageType 6) in the group of the client's `ApplicationVersions` row.
`AuthenticateApplication` -> `NakamaServerList.Apply`:

- dial `IP:Port` of the row; port 443 = https/wss to `tlsHost`, else the web URL host name, else the row IP
- Play page = game rows at the same `IP:Port`, else one clone of the row; rows at other addresses skipped (warning)
- every `CmuneRoomID` = row `IP:Port`; rooms told apart by number. Peer kind = game until join (88 comm, 66 lobby)
- no valid row -> error log, config endpoint used

Change host/port: fix the row (admin Deployment > Photons or `setNakamaEndpoint.sql`), recycle the web app pool.
Node side + full knob list: photon-migration `nakama/CONFIG.md`.

## Dev overrides (not for release)

Defaults: `http://127.0.0.1:7350`, key `defaultkey`, no dev auth.

- command line: `-nakama https://host:7350` (pins the dial target, DB row ignored), `-nakamakey <key>`, dev login `-nakamadev`, web token `-nakamatoken <t>`
- or `StreamingAssets/nakama.json`: `{"endpoint":"http://host:7350","serverKey":"...","devAuth":false,"tlsHost":"nk.example","replaceServerList":false}`
- `endpoint`/`host`/`port` pin the dial target; `scheme` alone only fixes the scheme; `tlsHost` = TLS name for a 443 row
- `replaceServerList: true` = Play page from config, no DB row needed
- `roomHost`/`roomPortBase` gone (ignored)

Login (Q2): custom id = token, `vars.token` = token. Token source order: `NakamaSession.TokenProvider`,
dev token `dev:<cmid>:<access>:<name>` (`-nakamadev`, node `UBER_DEV_AUTH=true`), `NakamaSession.WebToken`, `-nakamatoken`.
4.3.8 web login has no token yet: the login flow must set `NakamaSession.WebToken` once the web mints one.

## Wire

| Photon | Nakama |
|---|---|
| Connect | `AuthenticateCustomAsync` + socket (shared), first `uber_time`, then `StatusCode.Connect` |
| op 66 | RPC `uber_op66` `{m,i,a,p}` -> `{rc,i,d,msg,dc}` -> response 66 `{61, 42?}` |
| op 88 | RPC `uber_room_join` `{meta}` -> `JoinMatchAsync` -> match op 88 ack -> response 88 `{9,11,4,200,201}` |
| op 89 | `LeaveMatchAsync` -> response 89 |
| op 80/81/82/83 | match data, `[i16 netId][u8 method][i32 target, 80 only][args]` |
| event 0 / 3,4,5 | match op 0 -> `{101,100,103}`; op 3/4/5 -> `{42:{122\|123: bytes}}` |
| server time / RTT | RPC `uber_time` every 1 s |
| liveness in a game room | op 82 `[2][4]` (ServerSyncCenter InitializeRoom no-op) every 3 s; Go kicks after 10 s silence |
| server kick | match op 89 + UTF-8 reason -> `DisconnectByServerLogic` |

## Errors

| Nakama | Listener sees |
|---|---|
| auth rejected (401/403), no token, unreachable, timeout | `ExceptionOnConnect` (auth: backoff 2..30 s) |
| TLS failure | `SecurityExceptionOnConnect` then `Disconnect` |
| socket closed by server (`SessionDisconnect`, ban, network) | `DisconnectByServer` on every attached peer |
| match op 89 kick notice, payload = reason (`NakamaFraming.MatchOpKicked` = Go `game.OpKicked`, sent before `MatchKick`) | `DisconnectByServerLogic` on that peer only |
| own presence in a match leave (fallback only: Nakama 3.41 never sends a kicked session its own leave) | `DisconnectByServerLogic` on that peer only |
| join reject `rc=<n>` | response 88 rc n (1 gone, 2 full, 3 banned, 4 in game) |
| RPC/transport error | response 66 rc 1 / response 88 rc 5, `DebugMessage` = error |
| notification `single_socket` (-7) / banned (-8) | `ClientCommCenter.OnDisconnectAndDisablePhoton` (CommRPC 36 path) |

One socket for all peers: a socket drop stops Comm, Lobby and Game together (Comm reconnects every 5 s as before).
Every socket is released after close (`INakamaPlatform.ReleaseSocket`: Unity destroys its `[Nakama Socket]` GameObject).
Reconnect reuses the session; expired token -> `SessionRefreshAsync`, then fresh login. Identity = `cmid` from the session vars (Go hook).
Kill switch unchanged: CommRPC 36 / `CheatDetection` -> `OnDisconnectAndDisablePhoton` -> `IsPhotonEnabled = false` -> no reconnect; socket closes when idle.
Relays (ops 80/83) are handled in the room's `MatchLoop`: up to one tick (50 ms at 20 Hz) added vs Photon's immediate relay.
