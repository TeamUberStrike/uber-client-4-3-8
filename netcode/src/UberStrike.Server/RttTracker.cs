namespace UberStrike.Server;

/// <summary>
/// Phase 6 — server-observed RTT feeding the lag-comp rewind clamp.
///
/// The rewind window must come from what the SERVER measured (ping → pong wall time), never
/// from a client-claimed latency. Even then, a cheater can try to inflate the measurement by
/// delaying pong replies ("fake lag" → wider backtack window). Two defenses:
///   1. CombatSystem.RewindTime already hard-clamps rewind to MaxRewindSeconds — the ceiling.
///   2. This tracker rate-limits GROWTH (≤ RttMaxGrowSeconds per observation) while letting
///      the estimate fall freely — an abuser must hold inflated latency for many seconds to
///      gain window (and actually play at that latency), while a legit player's estimate
///      recovers instantly when their connection improves. Oscillation gains nothing.
/// </summary>
public sealed class RttTracker
{
    /// <summary>Per-observation growth cap (seconds). At ~2 pings/s, growing the window by
    /// 100 ms of extra rewind takes ~2.5 s of sustained real latency.</summary>
    public const double MaxGrowPerObservation = 0.020;
    /// <summary>First-observation cap: beyond 2× MaxRewind the value can't matter (rewind clamps).</summary>
    public const double InitCap = 0.5;
    /// <summary>Raw sample sanity ceiling.</summary>
    public const double SampleCap = 2.0;

    public double Seconds { get; private set; }

    public void Observe(double sample)
    {
        if (double.IsNaN(sample) || sample < 0d) return;
        sample = Math.Min(sample, SampleCap);

        if (Seconds <= 0d)
        {
            Seconds = Math.Min(sample, InitCap);
            return;
        }

        double ema = Seconds + 0.1 * (sample - Seconds);
        // growth is rate-limited; shrink applies in full
        Seconds = ema > Seconds ? Math.Min(ema, Seconds + MaxGrowPerObservation) : ema;
    }
}
