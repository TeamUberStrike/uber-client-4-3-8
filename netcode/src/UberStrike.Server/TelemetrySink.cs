using System.Text;

namespace UberStrike.Server;

public readonly record struct TelemetryEvent(double Time, int EntityId, string Kind, string Detail, float Value);

/// <summary>
/// Phase 8 — telemetry pipeline seam. Keeps a bounded in-memory ring for the review
/// dashboard / tests, and raises <see cref="Emitted"/> for the host to stream wherever it
/// wants (JSONL file, HTTP collector, …). The netcode library itself does NO file or
/// network I/O — the host owns that.
/// </summary>
public sealed class TelemetrySink
{
    public const int Capacity = 1024;

    private readonly TelemetryEvent[] _ring = new TelemetryEvent[Capacity];
    private int _head = -1, _count;

    public event Action<TelemetryEvent>? Emitted;

    public void Emit(double time, int entityId, string kind, string detail, float value = 0f)
    {
        var e = new TelemetryEvent(time, entityId, kind, detail, value);
        _head = (_head + 1) % Capacity;
        _ring[_head] = e;
        if (_count < Capacity) _count++;
        Emitted?.Invoke(e);
    }

    /// <summary>Most-recent-first snapshot of the buffered events.</summary>
    public List<TelemetryEvent> Recent(int max = Capacity)
    {
        int n = Math.Min(max, _count);
        var list = new List<TelemetryEvent>(n);
        for (int i = 0; i < n; i++)
            list.Add(_ring[(_head - i + Capacity) % Capacity]);
        return list;
    }

    /// <summary>One JSONL line per event — hand this to a file/stream in the host.</summary>
    public static string ToJsonl(in TelemetryEvent e)
    {
        var sb = new StringBuilder(128);
        sb.Append("{\"t\":").Append(e.Time.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))
          .Append(",\"entity\":").Append(e.EntityId)
          .Append(",\"kind\":\"").Append(Escape(e.Kind))
          .Append("\",\"detail\":\"").Append(Escape(e.Detail))
          .Append("\",\"value\":").Append(e.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))
          .Append('}');
        return sb.ToString();
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
