namespace UberStrike.Shared;

/// <summary>Client's view of the link.</summary>
public interface IClientLink
{
    void SendInput(in InputPacket p);
    void SendFire(in FireIntent f);
    event Action<Snapshot> SnapshotReceived;
    event Action<HitEvent> HitReceived;
}

/// <summary>Server's view of the link.</summary>
public interface IServerLink
{
    event Action<InputPacket> InputReceived;
    event Action<FireIntent>  FireReceived;
    void SendSnapshot(int entityId, in Snapshot s);
    void Broadcast(in HitEvent h);
}

/// <summary>
/// Deterministic in-process link with configurable one-way latency, used by the sandbox
/// and tests to drive a real predict/reconcile loop without sockets. Call
/// <see cref="Advance"/> with the current wall/sim time to deliver due messages.
///
/// Production: implement IClientLink over a browser WebSocket (WASM client) and IServerLink
/// over a WebSocket/Photon server. See IMPLEMENTATION_PLAN.md, Phase 7.
/// </summary>
public sealed class InProcessLink : IClientLink, IServerLink
{
    public double LatencySeconds = 0.05; // one-way; round trip ~= 2x
    private double _now;

    private readonly struct Timed<T> { public readonly double At; public readonly T Msg; public Timed(double at, T msg){ At = at; Msg = msg; } }

    private readonly Queue<Timed<InputPacket>> _toServerInput = new();
    private readonly Queue<Timed<FireIntent>>  _toServerFire  = new();
    private readonly Queue<Timed<Snapshot>>    _toClientSnap  = new();
    private readonly Queue<Timed<HitEvent>>    _toClientHit   = new();

    public event Action<InputPacket>? InputReceived;
    public event Action<FireIntent>?  FireReceived;
    public event Action<Snapshot>?    SnapshotReceived;
    public event Action<HitEvent>?    HitReceived;

    public void SendInput(in InputPacket p) => _toServerInput.Enqueue(new(_now + LatencySeconds, p));
    public void SendFire(in FireIntent f)   => _toServerFire.Enqueue(new(_now + LatencySeconds, f));
    public void SendSnapshot(int entityId, in Snapshot s) => _toClientSnap.Enqueue(new(_now + LatencySeconds, s));
    public void Broadcast(in HitEvent h)    => _toClientHit.Enqueue(new(_now + LatencySeconds, h));

    public void Advance(double now)
    {
        _now = now;
        // Dequeue BEFORE invoking: 'event?.Invoke(queue.Dequeue())' short-circuits the whole
        // expression when the event has no subscriber, so Dequeue() never runs and the queue
        // never drains -> infinite loop. Pull the message out first, then raise the event.
        while (_toServerInput.Count > 0 && _toServerInput.Peek().At <= _now) { InputPacket m = _toServerInput.Dequeue().Msg; InputReceived?.Invoke(m); }
        while (_toServerFire.Count  > 0 && _toServerFire.Peek().At  <= _now) { FireIntent  m = _toServerFire.Dequeue().Msg;  FireReceived?.Invoke(m); }
        while (_toClientSnap.Count  > 0 && _toClientSnap.Peek().At  <= _now) { Snapshot    m = _toClientSnap.Dequeue().Msg;  SnapshotReceived?.Invoke(m); }
        while (_toClientHit.Count   > 0 && _toClientHit.Peek().At   <= _now) { HitEvent    m = _toClientHit.Dequeue().Msg;   HitReceived?.Invoke(m); }
    }
}
