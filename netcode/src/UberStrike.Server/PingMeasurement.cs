namespace UberStrike.Server;

/// <summary>
/// Phase 6 — server-side bookkeeping for server-measured RTT. The server issues an opaque nonce
/// per heartbeat (<see cref="Issue"/>), stamping it with the server's own clock; when the client
/// echoes that nonce back the server resolves it (<see cref="Resolve"/>) into a round-trip time
/// measured entirely on the server clock. This is what makes the lag-comp rewind window trustworthy
/// — the RTT never comes from a client-claimed latency field.
///
/// Three abuse vectors, all closed here (then further by <see cref="RttTracker"/> + the MaxRewind clamp):
///   1. Replay — echoing the same nonce twice to manufacture extra samples: a nonce is CONSUMED on
///      first resolve, so a second echo of it returns false.
///   2. Stale-hoarding — replying to an OLD nonce to inflate the apparent RTT: the outstanding set
///      is capped (<see cref="MaxOutstanding"/>); when full, the OLDEST unanswered nonce is evicted,
///      so a client can only ever echo a recent nonce (bounded inflation), and one that never echoes
///      can't grow server memory.
///   3. Forgery — a nonce the server never issued: not in the set → rejected.
///
/// Single-threaded contract: callers serialize Issue/Resolve under the connection's lock.
/// </summary>
public sealed class PingMeasurement
{
    /// <summary>Max simultaneously-outstanding (unanswered) pings before the oldest is evicted.
    /// At ~2 heartbeats/s this bounds how stale an echoable nonce can be to ~4 s — and the RTT
    /// sample is clamped again by <see cref="RttTracker.SampleCap"/>.</summary>
    public const int MaxOutstanding = 8;

    private readonly Dictionary<uint, double> _outstanding = new();
    private uint _nextNonce;

    /// <summary>Number of pings sent but not yet echoed (test/diagnostic visibility).</summary>
    public int OutstandingCount => _outstanding.Count;

    /// <summary>Issue a fresh nonce stamped at <paramref name="now"/> (server clock).</summary>
    public uint Issue(double now)
    {
        if (_outstanding.Count >= MaxOutstanding)
        {
            // evict the oldest unanswered ping (smallest send time)
            uint oldest = 0; double best = double.MaxValue;
            foreach (KeyValuePair<uint, double> kv in _outstanding)
                if (kv.Value < best) { best = kv.Value; oldest = kv.Key; }
            _outstanding.Remove(oldest);
        }
        uint nonce = ++_nextNonce;
        _outstanding[nonce] = now;
        return nonce;
    }

    /// <summary>Resolve an echoed nonce into a server-measured RTT. Returns false (and does not
    /// emit a sample) for an unknown, already-consumed, or forged nonce.</summary>
    public bool Resolve(uint nonce, double now, out double rtt)
    {
        rtt = 0d;
        if (!_outstanding.Remove(nonce, out double sent)) return false; // unknown / replayed / stale
        rtt = now - sent;
        if (rtt < 0d) rtt = 0d;   // monotonic-clock guard; never negative
        return true;
    }
}
