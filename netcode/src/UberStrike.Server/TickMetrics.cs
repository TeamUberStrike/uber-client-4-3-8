using UberStrike.Shared;

namespace UberStrike.Server;

/// <summary>
/// Phase 10 — rolling tick-loop metrics. Records per-tick wall time (and optional snapshot
/// bandwidth) in a fixed window and answers the operational questions: are we inside the tick
/// budget, what's the worst case, how big are snapshots. No I/O — the host reads/exports these.
/// </summary>
public sealed class TickMetrics
{
    private readonly double[] _tickMs;
    private int _idx, _count;
    public long TotalTicks { get; private set; }
    public long OverBudgetTicks { get; private set; }
    public long TotalSnapshotBytes { get; private set; }
    public long SnapshotCount { get; private set; }

    /// <summary>Budget per server tick. The room loop must finish within this to hold tick rate.</summary>
    public double BudgetMs { get; }

    public TickMetrics(int window = 1024, double budgetMs = 1000.0 / GameConstants.TickRate)
    {
        _tickMs = new double[window];
        BudgetMs = budgetMs;
    }

    public void RecordTick(double ms)
    {
        _tickMs[_idx] = ms;
        _idx = (_idx + 1) % _tickMs.Length;
        if (_count < _tickMs.Length) _count++;
        TotalTicks++;
        if (ms > BudgetMs) OverBudgetTicks++;
    }

    public void RecordSnapshotBytes(long bytes) { TotalSnapshotBytes += bytes; SnapshotCount++; }

    public double AvgMs
    {
        get { if (_count == 0) return 0; double s = 0; for (int i = 0; i < _count; i++) s += _tickMs[i]; return s / _count; }
    }

    public double MaxMs
    {
        get { double m = 0; for (int i = 0; i < _count; i++) if (_tickMs[i] > m) m = _tickMs[i]; return m; }
    }

    /// <summary>Percentile (0..1) over the current window — e.g. 0.99 for p99 tick time.</summary>
    public double PercentileMs(double p)
    {
        if (_count == 0) return 0;
        var sorted = new double[_count];
        Array.Copy(_tickMs, sorted, _count);
        Array.Sort(sorted);
        int i = (int)Math.Clamp(MathF.Ceiling((float)(p * _count)) - 1, 0, _count - 1);
        return sorted[i];
    }

    public double AvgSnapshotBytes => SnapshotCount > 0 ? (double)TotalSnapshotBytes / SnapshotCount : 0;
    public bool WithinBudget => MaxMs <= BudgetMs;
}
