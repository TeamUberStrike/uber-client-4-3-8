using System.Numerics;

namespace UberStrike.Server;

public struct HitboxSnapshot
{
    public double  Time;
    public Vector3 Position;
    public float   Yaw;
}

/// <summary>
/// Ring buffer of recent positions for lag compensation. The server rewinds a target's
/// hitbox to the time the shooter actually "saw" it before testing a hit.
/// </summary>
public sealed class HitboxHistory
{
    private readonly HitboxSnapshot[] _buf;
    private int _head = -1;
    private int _count;

    public HitboxHistory(int capacity = 64) => _buf = new HitboxSnapshot[capacity];

    public void Record(double time, Vector3 pos, float yaw)
    {
        _head = (_head + 1) % _buf.Length;
        _buf[_head] = new HitboxSnapshot { Time = time, Position = pos, Yaw = yaw };
        if (_count < _buf.Length) _count++;
    }

    /// <summary>Sample the position at <paramref name="targetTime"/>, interpolating between samples.</summary>
    public bool Rewind(double targetTime, out Vector3 pos, out float yaw)
    {
        pos = default; yaw = 0f;
        if (_count == 0) return false;

        // Walk newest -> oldest looking for the bracket [older, newer] containing targetTime.
        HitboxSnapshot newer = _buf[_head];
        if (targetTime >= newer.Time) { pos = newer.Position; yaw = newer.Yaw; return true; }

        for (int i = 1; i < _count; i++)
        {
            int idxNewer = (_head - (i - 1) + _buf.Length) % _buf.Length;
            int idxOlder = (_head - i + _buf.Length) % _buf.Length;
            HitboxSnapshot a = _buf[idxOlder];
            HitboxSnapshot b = _buf[idxNewer];
            if (targetTime >= a.Time && targetTime <= b.Time)
            {
                float span = (float)(b.Time - a.Time);
                float t = span > 1e-6f ? (float)((targetTime - a.Time) / span) : 0f;
                pos = Vector3.Lerp(a.Position, b.Position, t);
                yaw = a.Yaw + (b.Yaw - a.Yaw) * t;
                return true;
            }
        }

        // Older than the oldest sample we retained: clamp to oldest.
        int oldestIdx = (_head - (_count - 1) + _buf.Length) % _buf.Length;
        pos = _buf[oldestIdx].Position;
        yaw = _buf[oldestIdx].Yaw;
        return true;
    }
}
