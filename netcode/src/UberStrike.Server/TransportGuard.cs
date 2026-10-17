namespace UberStrike.Server;

/// <summary>
/// Verdict for an inbound transport frame.</summary>
public enum FrameVerdict { Ok, Replay, OutOfOrder, Gap, Flood }

/// <summary>
/// Phase 7.5 — defense against raw WebSocket FRAME manipulation, the layer below message
/// validation. WSS/TLS protects the wire from third parties, but in this threat model the
/// CLIENT itself is the attacker: a modified WebGL client (or a script driving the socket
/// directly, bypassing the game) can capture and REPLAY a valid frame (e.g. a fire), REORDER
/// frames, inject DUPLICATES, or flood. Server authority already means a replayed "I moved"
/// can't teleport you — but a replayed fire/switch/quick-item frame, or a burst of duplicated
/// frames, can still cause double-actions or load. This guard closes the transport seam.
///
/// Every client→server frame carries a monotonic per-connection sequence (the
/// <see cref="TransportEnvelope"/> 4-byte prefix). The guard enforces, per connection:
///   - strictly increasing sequence (a replay or reorder reuses/decreases it → rejected),
///   - a dedup window over recent accepted sequences (belt-and-suspenders vs replay),
///   - a bounded forward gap (a forged far-future seq can't slip the dedup window),
///   - a per-second frame-rate ceiling (flood of fresh-seq frames).
/// Each rejection is a STRIKE; past <see cref="StrikeLimit"/> the connection should be dropped
/// (a legitimate client never trips these — its own envelope is monotonic and paced).
///
/// Pure + time-injected (no wall clock inside) so it is deterministic in tests.
/// </summary>
public sealed class TransportGuard
{
    public const int    DedupWindow  = 256;   // remember this many recent seqs
    public const uint   MaxForwardGap = 1024; // a jump larger than this is a forged seq
    public const double MaxFramesPerSec = 120;// hard transport ceiling (well above 30Hz play)
    public const int    StrikeLimit   = 10;   // TAMPERING strikes (replay/reorder/gap) before drop

    private long _lastSeq = -1;
    private readonly HashSet<uint> _recent = new();
    private readonly Queue<uint>   _recentOrder = new();

    // frame-rate window
    private double _windowStart = double.NegativeInfinity;
    private int    _framesThisWindow;

    public int  Strikes { get; private set; }
    public bool ShouldDisconnect => Strikes >= StrikeLimit;

    public FrameVerdict Inspect(uint seq, double now)
    {
        // 1) frame-rate flood (counts every arrival, accepted or not). A single-second burst is
        //    DROPPED but does NOT strike — a legit catch-up/reconnect can briefly burst, and the
        //    dropped frames never reach gameplay. Only TAMPERING below counts toward disconnect.
        if (_windowStart == double.NegativeInfinity || now - _windowStart >= 1.0)
        {
            _windowStart = now;
            _framesThisWindow = 0;
        }
        if (++_framesThisWindow > MaxFramesPerSec) return FrameVerdict.Flood;

        // 2) replay / reorder: sequence must strictly increase
        if (_lastSeq >= 0 && seq <= _lastSeq)
        {
            // distinguish exact replay (still in dedup window) from a stale/reordered frame
            return Strike(_recent.Contains(seq) ? FrameVerdict.Replay : FrameVerdict.OutOfOrder);
        }

        // 3) forged far-future jump that would let an attacker skip past the dedup window
        if (_lastSeq >= 0 && seq > _lastSeq + MaxForwardGap) return Strike(FrameVerdict.Gap);

        // accept
        _lastSeq = seq;
        _recent.Add(seq);
        _recentOrder.Enqueue(seq);
        if (_recentOrder.Count > DedupWindow) _recent.Remove(_recentOrder.Dequeue());
        return FrameVerdict.Ok;
    }

    private FrameVerdict Strike(FrameVerdict v) { Strikes++; return v; }
}
