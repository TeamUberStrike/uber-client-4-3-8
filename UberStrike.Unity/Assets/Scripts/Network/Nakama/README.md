# Nakama transport

Photon is gone. `UberStrike.UnitySdk.dll` (uber-server-4-3-8 `nakama-sdk`) drives `INetworkPeer`;
`NakamaPeer` implements it on nakama-unity 3.22.1. Game code above the SDK is unchanged.

| File | What |
|---|---|
| `NakamaBootstrap` | sets `PeerFactory.Create` before any manager `Awake`; config; kill switch |
| `NakamaSession` | one `IClient`/`ISession`/`ISocket` (`NewSocket(useMainThread: true)`), auth, `uber_time` clock, routing by match id |
| `NakamaPeer` | one per `PhotonClient` (Comm, Lobby, Game, probes); Photon op/status/event semantics |
| `NakamaFraming` | byte + JSON framing, no Unity/Nakama (tests: `/NakamaTests`) |
| `NakamaClock` | min-RTT offset, slew, int31 server ms |
| `NakamaConfig` | endpoint, key, dev auth, room address labels |
| `NakamaServerList` | Play page rows for the node |

## Run against a node

Defaults: `http://127.0.0.1:7350`, key `defaultkey`, no dev auth.

- command line: `-nakama https://host:7350 -nakamakey <key>`; dev login `-nakamadev`; web token `-nakamatoken <t>`
- or `StreamingAssets/nakama.json`: `{"endpoint":"http://host:7350","serverKey":"...","devAuth":false,"roomHost":"127.0.0.1","roomPortBase":20000}`
- `roomHost`/`roomPortBase` = Go `UBER_ROOM_HOST`/`UBER_ROOM_PORT_BASE` (IPv4 labels in `CmuneRoomID`, never dialed)

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

## Errors

| Nakama | Listener sees |
|---|---|
| auth rejected (401/403), no token, unreachable, timeout | `ExceptionOnConnect` (auth: backoff 2..30 s) |
| TLS failure | `SecurityExceptionOnConnect` then `Disconnect` |
| socket closed by server (`SessionDisconnect`, ban, network) | `DisconnectByServer` on every attached peer |
| own presence leaves match (`MatchKick`, match end) | `DisconnectByServerLogic` on that peer only |
| join reject `rc=<n>` | response 88 rc n (1 gone, 2 full, 3 banned, 4 in game) |
| RPC/transport error | response 66 rc 1 / response 88 rc 5, `DebugMessage` = error |
| notification `single_socket` (-7) / banned (-8) | `ClientCommCenter.OnDisconnectAndDisablePhoton` (CommRPC 36 path) |

One socket for all peers: a socket drop stops Comm, Lobby and Game together (Comm reconnects every 5 s as before).
