using System.Net.WebSockets;
using UberStrike.Shared;

namespace UberStrike.Client;

/// <summary>
/// Phase 7 — client transport implementing <see cref="IClientLink"/> over a real WebSocket,
/// the mirror of <see cref="UberStrike.Server.WebSocketServerLink"/>. Connects, sends Hello,
/// awaits Welcome, then frames Input/Fire/Switch with the Phase-2 <see cref="Wire"/> codec and
/// decodes Snapshot (via a per-connection <see cref="SnapshotDecoder"/>) + Hit.
///
/// BROWSER NOTE: a WebGL/WASM build cannot use <see cref="ClientWebSocket"/>. The production
/// WASM client swaps THIS class's socket primitive for a JS WebSocket bridge (the detached
/// spike's NativeWebSocket .jslib is the reference) — the Wire framing, handshake and decode
/// path are identical, so only Connect/Send/Receive change. This managed implementation is the
/// desktop/test client and the contract the WASM bridge must match.
/// </summary>
public sealed class WebSocketClientLink : IClientLink, IDisposable
{
    public event Action<Snapshot>? SnapshotReceived;
    public event Action<HitEvent>? HitReceived;
    public event Action<int>?      Welcomed;        // entityId assigned by the server
    public event Action<double, double>? Pong;      // (clientSendTime, serverTime) → ClockSync

    private readonly ClientWebSocket _ws = new();
    private readonly SnapshotDecoder _decoder = new();
    private readonly CancellationTokenSource _cts = new();
    // Transport anti-replay: a monotonic per-connection frame sequence on every outbound frame,
    // and a send lock so the seq assignment + SendAsync stay ordered (ClientWebSocket also
    // forbids overlapping sends). The server's TransportGuard rejects replayed/reordered frames.
    private readonly System.Threading.SemaphoreSlim _sendLock = new(1, 1);
    private uint _frameSeq;
    public int EntityId { get; private set; } = -1;

    /// <summary>Connect, complete the Hello→Welcome handshake, and start the receive loop.</summary>
    public async Task<bool> ConnectAsync(Uri uri, string sessionToken, CancellationToken ct = default)
    {
        await _ws.ConnectAsync(uri, ct);
        await SendRawAsync(Wire.EncodeHello(sessionToken), ct);

        byte[]? welcome = await ReceiveMessageAsync(ct);
        if (welcome is null || !Wire.TryDecodeWelcome(welcome, out int entityId, out _)) return false;
        EntityId = entityId;
        Welcomed?.Invoke(entityId);
        _ = ReceiveLoopAsync(_cts.Token);
        return true;
    }

    public void SendInput(in InputPacket p) => _ = SendRawAsync(Wire.EncodeInput(p), _cts.Token);
    public void SendFire(in FireIntent f)   => _ = SendRawAsync(Wire.EncodeFire(f), _cts.Token);
    public void SendSwitch(in SwitchIntent s) => _ = SendRawAsync(Wire.EncodeSwitch(s), _cts.Token);
    public void SendPing(double clientNow)  => _ = SendRawAsync(Wire.EncodePing(clientNow), _cts.Token);

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
        {
            byte[]? msg = await ReceiveMessageAsync(ct);
            if (msg is null) break;
            if (!Wire.PeekType(msg, out MsgType type)) continue;
            switch (type)
            {
                case MsgType.Snapshot:
                    if (_decoder.TryDecode(msg, out Snapshot s)) SnapshotReceived?.Invoke(s);
                    break;
                case MsgType.Hit:
                    if (Wire.TryDecodeHit(msg, out HitEvent h)) HitReceived?.Invoke(h);
                    break;
                case MsgType.Pong:
                    if (Wire.TryDecodePong(msg, out double cs, out double st)) Pong?.Invoke(cs, st);
                    break;
                default: break;
            }
        }
    }

    private async Task SendRawAsync(byte[] data, CancellationToken ct)
    {
        if (_ws.State != WebSocketState.Open) return;
        await _sendLock.WaitAsync(ct);
        try
        {
            byte[] framed = TransportEnvelope.Wrap(_frameSeq++, data);
            await _ws.SendAsync(framed, WebSocketMessageType.Binary, true, ct);
        }
        catch { }
        finally { _sendLock.Release(); }
    }

    private async Task<byte[]?> ReceiveMessageAsync(CancellationToken ct)
    {
        var buf = new byte[4096];
        using var ms = new MemoryStream();
        try
        {
            while (true)
            {
                WebSocketReceiveResult r = await _ws.ReceiveAsync(buf, ct);
                if (r.MessageType == WebSocketMessageType.Close) return null;
                ms.Write(buf, 0, r.Count);
                if (ms.Length > 64_000) return null;
                if (r.EndOfMessage) break;
            }
        }
        catch { return null; }
        return ms.ToArray();
    }

    public void Dispose()
    {
        try { _cts.Cancel(); } catch { }
        try { _ws.Abort(); } catch { }
        try { _ws.Dispose(); } catch { }
        _cts.Dispose();
    }
}
