namespace UberStrike.Server;

/// <summary>
/// Phase 7 — token-bucket rate limiter, one per connection per channel (inputs, fires, bytes).
/// The gateway ASSUMES this layer exists: it validates shape/auth/replay but not flood volume.
/// Refills continuously at <see cref="_ratePerSec"/>, bursts up to <see cref="_burst"/>.
/// Pure/time-injected so it's deterministic in tests (no wall clock inside).
/// </summary>
public sealed class RateLimiter
{
    private readonly double _ratePerSec;
    private readonly double _burst;
    private double _tokens;
    private double _last = double.NegativeInfinity;

    public RateLimiter(double ratePerSec, double burst)
    {
        _ratePerSec = ratePerSec;
        _burst = burst;
        _tokens = burst;
    }

    /// <summary>Charge <paramref name="cost"/> tokens at time <paramref name="now"/>; false = over limit.</summary>
    public bool TryConsume(double now, double cost = 1d)
    {
        if (_last == double.NegativeInfinity) _last = now;
        if (now > _last)
        {
            _tokens = Math.Min(_burst, _tokens + (now - _last) * _ratePerSec);
            _last = now;
        }
        if (_tokens < cost) return false;
        _tokens -= cost;
        return true;
    }
}
