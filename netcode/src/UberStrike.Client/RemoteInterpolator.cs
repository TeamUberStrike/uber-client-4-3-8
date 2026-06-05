using System.Numerics;

namespace UberStrike.Client;

/// <summary>
/// Buffers a remote player's authoritative snapshots and samples them in the past
/// (renderTime = ServerNow - InterpDelay), interpolating between the two bracketing samples.
/// This is the timeline the server's lag-comp rewind reconstructs.
/// </summary>
public sealed class RemoteInterpolator
{
    private const int Max = 32;
    private readonly List<(double t, Vector3 pos, float yaw, float pitch)> _buf = new();

    public void Ingest(double serverTime, Vector3 pos, float yaw, float pitch)
    {
        _buf.Add((serverTime, pos, yaw, pitch));
        if (_buf.Count > Max) _buf.RemoveAt(0);
    }

    public bool Sample(double renderTime, out Vector3 pos, out float yaw, out float pitch)
    {
        pos = default; yaw = 0f; pitch = 0f;
        if (_buf.Count == 0) return false;

        if (renderTime <= _buf[0].t)        { (_, pos, yaw, pitch) = _buf[0];        return true; }
        if (renderTime >= _buf[^1].t)       { (_, pos, yaw, pitch) = _buf[^1];       return true; }

        for (int i = 0; i < _buf.Count - 1; i++)
        {
            var a = _buf[i];
            var b = _buf[i + 1];
            if (renderTime >= a.t && renderTime <= b.t)
            {
                float span = (float)(b.t - a.t);
                float k = span > 1e-6f ? (float)((renderTime - a.t) / span) : 0f;
                pos = Vector3.Lerp(a.pos, b.pos, k);
                yaw = a.yaw + (b.yaw - a.yaw) * k;
                pitch = a.pitch + (b.pitch - a.pitch) * k;
                return true;
            }
        }
        return false;
    }
}
