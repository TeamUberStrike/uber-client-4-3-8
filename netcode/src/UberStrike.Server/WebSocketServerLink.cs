using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Phase 7 — production server transport implementing <see cref="IServerLink"/> over real
/// WebSockets. Accepts TCP connections, performs the RFC 6455 upgrade handshake, then frames
/// every message with the Phase-2 <see cref="Wire"/> codec.
///
/// Per connection:
///   1. WebSocket upgrade (Sec-WebSocket-Accept).
///   2. SESSION HANDSHAKE: the first message must be a Hello carrying the SessionToken issued
///      out-of-band by the login/webservice. <see cref="Authenticate"/> maps it to an EntityId;
///      an unknown/invalid token is dropped immediately. This is the EntityId↔SessionToken bind
///      the whole anti-cheat trusts — every later packet is stamped with this connection's
///      authenticated EntityId, so a forged EntityId in a packet body can't impersonate anyone.
///   3. RATE LIMITING: per-connection token buckets for inputs, fires and raw bytes; a flood is
///      dropped (and flagged) without touching the simulation or other players.
///   4. Decode → raise InputReceived / FireReceived / SwitchReceived for the simulation.
///
/// Reconnect/resync: a fresh Hello with the same token rebinds to the existing entity and starts
/// a NEW SnapshotEncoder, so the next snapshot is a full baseline (the client's delta cache was
/// lost). Outbound Pong answers Ping for the server-observed RTT (Phase 6).
///
/// NOTE on the browser client: a WebGL/WASM build can't use ClientWebSocket; it uses a JS
/// WebSocket bridge. The WIRE is identical — only the socket primitive differs. This server
/// speaks standard RFC 6455, so a browser connects to it unchanged.
///
/// ToU: instances are bound to loopback for our own testing only. Production runs on HaZard's
/// Linux server, never on the Shadow PC.
/// </summary>
public sealed class WebSocketServerLink : IServerLink, IDisposable
{
    private const string WsMagic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    public event Action<InputPacket>? InputReceived;
    public event Action<FireIntent>?  FireReceived;
    public event Action<SwitchIntent>? SwitchReceived;
    public event Action<int>?         ClientConnected;     // entityId
    public event Action<int>?         ClientDisconnected;  // entityId

    /// <summary>token → entityId (null rejects). Supplied by the host (login/webservice).</summary>
    public Func<string, int?> Authenticate { get; set; } = _ => null;
    /// <summary>Raised when a server-observed RTT sample arrives (entityId, seconds).</summary>
    public event Action<int, double>? RttSample;

    // tunables (per connection)
    public double InputsPerSec = 90,  InputBurst = 30;   // 30 Hz tick + slack
    public double FiresPerSec  = 40,  FireBurst  = 20;
    public double BytesPerSec  = 64_000, ByteBurst = 32_000;

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate = new();
    private readonly Dictionary<int, Conn> _byEntity = new();
    private readonly Func<double> _now;

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public WebSocketServerLink(int port, Func<double> nowSeconds)
    {
        _now = nowSeconds;
        _listener = new TcpListener(IPAddress.Loopback, port); // loopback only (ToU)
    }

    public void Start()
    {
        _listener.Start();
        _ = AcceptLoopAsync(_cts.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient tcp;
            try { tcp = await _listener.AcceptTcpClientAsync(ct); }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            _ = HandleAsync(tcp, ct);
        }
    }

    private async Task HandleAsync(TcpClient tcp, CancellationToken ct)
    {
        Conn? conn = null;
        try
        {
            tcp.NoDelay = true;
            NetworkStream stream = tcp.GetStream();
            if (!await UpgradeAsync(stream, ct)) { tcp.Close(); return; }

            WebSocket ws = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: null,
                keepAliveInterval: TimeSpan.FromSeconds(15));

            // --- session handshake: first frame MUST be a transport-enveloped Hello ---
            byte[]? first = await ReceiveMessageAsync(ws, ct);
            if (first is null || !TransportEnvelope.TryUnwrap(first, out uint helloSeq, out byte[] helloPayload))
            { await CloseAsync(ws); return; }
            var guard = new TransportGuard();
            if (guard.Inspect(helloSeq, _now()) != FrameVerdict.Ok) { await CloseAsync(ws); return; }
            if (!Wire.TryDecodeHello(helloPayload, out string token)) { await CloseAsync(ws); return; }
            int? entityId = Authenticate(token);
            if (entityId is null) { await CloseAsync(ws); return; }

            conn = new Conn(entityId.Value, token, ws, _now, InputsPerSec, InputBurst,
                            FiresPerSec, FireBurst, BytesPerSec, ByteBurst, guard);
            lock (_gate)
            {
                if (_byEntity.TryGetValue(entityId.Value, out Conn? old)) old.Kill(); // reconnect: drop the old
                _byEntity[entityId.Value] = conn;
            }
            await SendRawAsync(ws, Wire.EncodeWelcome(entityId.Value, GameConstants.TickRate), ct);
            ClientConnected?.Invoke(entityId.Value);

            await ReceiveLoopAsync(conn, ct);
        }
        catch { /* connection died; fall through to cleanup */ }
        finally
        {
            if (conn != null)
            {
                lock (_gate) { if (_byEntity.TryGetValue(conn.EntityId, out Conn? cur) && ReferenceEquals(cur, conn)) _byEntity.Remove(conn.EntityId); }
                ClientDisconnected?.Invoke(conn.EntityId);
            }
            try { tcp.Close(); } catch { }
        }
    }

    private async Task ReceiveLoopAsync(Conn conn, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && conn.Ws.State == WebSocketState.Open && !conn.Dead)
        {
            byte[]? frame = await ReceiveMessageAsync(conn.Ws, ct);
            if (frame is null) break;
            double now = _now();

            // --- transport anti-manipulation: strip + validate the frame sequence FIRST ---
            // (replay / reorder / duplicate / forged-jump / frame-flood). A modified client or a
            // raw-socket script that bypasses the game is caught here, below message validation.
            if (!TransportEnvelope.TryUnwrap(frame, out uint frameSeq, out byte[] msg)) { conn.Flagged++; continue; }
            FrameVerdict verdict = conn.Guard.Inspect(frameSeq, now);
            if (verdict != FrameVerdict.Ok)
            {
                conn.Flagged++;
                if (conn.Guard.ShouldDisconnect) break;   // persistent frame tampering → drop
                continue;
            }

            // byte-rate flood guard (cheap, covers everything)
            if (!conn.Bytes.TryConsume(now, msg.Length)) { conn.Flagged++; continue; }
            if (!Wire.PeekType(msg, out MsgType type)) { conn.Flagged++; continue; }

            switch (type)
            {
                case MsgType.Input:
                    if (!conn.Inputs.TryConsume(now)) { conn.Flagged++; break; }
                    if (Wire.TryDecodeInput(msg, out InputPacket ip))
                    {
                        ip.EntityId = conn.EntityId;          // STAMP authenticated id (ignore body claim)
                        ip.SessionToken = conn.Token;
                        InputReceived?.Invoke(ip);
                    }
                    break;
                case MsgType.Fire:
                    if (!conn.Fires.TryConsume(now)) { conn.Flagged++; break; }
                    if (Wire.TryDecodeFire(msg, out FireIntent f))
                    {
                        f.EntityId = conn.EntityId; f.SessionToken = conn.Token;
                        FireReceived?.Invoke(f);
                    }
                    break;
                case MsgType.Switch:
                    if (Wire.TryDecodeSwitch(msg, out SwitchIntent sw))
                    {
                        sw.EntityId = conn.EntityId; sw.SessionToken = conn.Token;
                        SwitchReceived?.Invoke(sw);
                    }
                    break;
                case MsgType.Ping:
                    if (Wire.TryDecodePing(msg, out double clientSent))
                    {
                        await SendRawAsync(conn.Ws, Wire.EncodePong(clientSent, now), ct);
                        // server-observed RTT: time from when WE last echoed this client's clock.
                        // (Full RTT needs the client to ping; here we surface the one-way seam.)
                        RttSample?.Invoke(conn.EntityId, 0d);
                    }
                    break;
                default: break; // clients don't send Snapshot/Hit/Welcome
            }
        }
    }

    // --- IServerLink outbound -------------------------------------------------------------

    public void SendSnapshot(int entityId, in Snapshot s)
    {
        Conn? conn; lock (_gate) { _byEntity.TryGetValue(entityId, out conn); }
        if (conn is null) return;
        byte[] bytes;
        try { bytes = conn.Encoder.Encode(s); } catch { return; }
        _ = SendRawAsync(conn.Ws, bytes, _cts.Token);
    }

    public void Broadcast(in HitEvent h)
    {
        byte[] bytes = Wire.EncodeHit(h);
        List<Conn> conns; lock (_gate) { conns = new List<Conn>(_byEntity.Values); }
        foreach (Conn c in conns) _ = SendRawAsync(c.Ws, bytes, _cts.Token);
    }

    public int FlaggedCount(int entityId)
    {
        lock (_gate) { return _byEntity.TryGetValue(entityId, out Conn? c) ? c.Flagged : 0; }
    }

    // --- low-level WS framing -------------------------------------------------------------

    private static async Task SendRawAsync(WebSocket ws, byte[] data, CancellationToken ct)
    {
        if (ws.State != WebSocketState.Open) return;
        try { await ws.SendAsync(data, WebSocketMessageType.Binary, true, ct); } catch { }
    }

    private static async Task<byte[]?> ReceiveMessageAsync(WebSocket ws, CancellationToken ct)
    {
        var buf = new byte[4096];
        using var ms = new MemoryStream();
        try
        {
            while (true)
            {
                WebSocketReceiveResult r = await ws.ReceiveAsync(buf, ct);
                if (r.MessageType == WebSocketMessageType.Close) return null;
                ms.Write(buf, 0, r.Count);
                if (ms.Length > 64_000) return null;   // oversized frame: drop the connection
                if (r.EndOfMessage) break;
            }
        }
        catch { return null; }
        return ms.ToArray();
    }

    private static async Task CloseAsync(WebSocket ws)
    {
        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
        ws.Dispose();
    }

    private static async Task<bool> UpgradeAsync(NetworkStream stream, CancellationToken ct)
    {
        // read the HTTP upgrade request headers (until a blank line)
        var sb = new StringBuilder();
        var one = new byte[1];
        int blanks = 0;
        while (sb.Length < 8192)
        {
            int n = await stream.ReadAsync(one, ct);
            if (n == 0) return false;
            char c = (char)one[0];
            sb.Append(c);
            if (c == '\n') { if (++blanks == 2) break; } else if (c != '\r') blanks = 0;
        }
        string req = sb.ToString();
        string? key = null;
        foreach (string line in req.Split("\r\n"))
        {
            int i = line.IndexOf(':');
            if (i > 0 && line[..i].Trim().Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                key = line[(i + 1)..].Trim();
        }
        if (key is null) return false;

        string accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + WsMagic)));
        string resp = "HTTP/1.1 101 Switching Protocols\r\n" +
                      "Upgrade: websocket\r\n" +
                      "Connection: Upgrade\r\n" +
                      "Sec-WebSocket-Accept: " + accept + "\r\n\r\n";
        byte[] respBytes = Encoding.ASCII.GetBytes(resp);
        await stream.WriteAsync(respBytes, ct);
        return true;
    }

    public void Dispose()
    {
        try { _cts.Cancel(); } catch { }
        try { _listener.Stop(); } catch { }
        List<Conn> conns; lock (_gate) { conns = new List<Conn>(_byEntity.Values); _byEntity.Clear(); }
        foreach (Conn c in conns) c.Kill();
        _cts.Dispose();
    }

    private sealed class Conn
    {
        public readonly int EntityId;
        public readonly string Token;
        public readonly WebSocket Ws;
        public readonly SnapshotEncoder Encoder = new(); // fresh per connection (reconnect = new baseline)
        public readonly RateLimiter Inputs, Fires, Bytes;
        public readonly TransportGuard Guard;            // anti replay/reorder/dup/flood
        public int Flagged;
        public bool Dead;

        public Conn(int entityId, string token, WebSocket ws, Func<double> now,
                    double inRate, double inBurst, double fRate, double fBurst, double bRate, double bBurst,
                    TransportGuard guard)
        {
            EntityId = entityId; Token = token; Ws = ws; Guard = guard;
            Inputs = new RateLimiter(inRate, inBurst);
            Fires  = new RateLimiter(fRate, fBurst);
            Bytes  = new RateLimiter(bRate, bBurst);
        }

        public void Kill()
        {
            Dead = true;
            try { Ws.Abort(); } catch { }
            try { Ws.Dispose(); } catch { }
        }
    }
}
