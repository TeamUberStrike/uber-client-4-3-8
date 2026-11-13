namespace UberStrike.Client;

/// <summary>
/// Estimates server time + smoothed RTT from ping/pong. ServerNow drives remote interpolation
/// and must roughly agree with the server's own RTT estimate used for lag-comp rewind.
/// </summary>
public sealed class ClockSync
{
    public double SmoothedRtt { get; private set; }
    private double _offset;

    public double ServerNow(double localNow) => localNow + _offset;

    public void OnPong(double clientSent, double serverTime, double now)
    {
        double rtt = now - clientSent;
        SmoothedRtt = SmoothedRtt <= 0d ? rtt : SmoothedRtt + 0.1 * (rtt - SmoothedRtt);
        _offset = serverTime + rtt * 0.5 - now;
    }
}
