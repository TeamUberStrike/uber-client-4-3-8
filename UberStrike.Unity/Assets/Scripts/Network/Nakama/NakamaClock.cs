using System;

namespace UberStrike.Realtime.NakamaAdapter
{
    // uber_time samples -> server ms (int31, Photon ServerTimeInMilliSeconds) + RTT/variance.
    public sealed class NakamaClock
    {
        public const int Window = 8;
        public const long JumpMs = 250;
        public const long SlewMs = 10;
        const long Mod = 1L << 31;

        readonly long[] _rtt = new long[Window];
        readonly long[] _off = new long[Window];
        int _count;
        int _next;
        double _srtt;
        double _rttVar;

        public bool Synced { get; private set; }
        public long Offset { get; private set; }
        public int RoundTripTime { get; private set; }
        public int RoundTripTimeVariance { get; private set; }
        public int Samples { get; private set; }

        // sent/received = local ms of the same monotonic clock; serverMs = Go clock.ServerMs()
        public bool AddSample(long sentLocal, long receivedLocal, int serverMs)
        {
            long rtt = receivedLocal - sentLocal;
            if (rtt < 0 || serverMs < 0)
                return false;

            long sample = serverMs - (sentLocal + rtt / 2);

            if (Samples == 0)
            {
                _srtt = rtt;
                _rttVar = rtt / 2.0;
            }
            else
            {
                double err = rtt - _srtt;
                _srtt += err / 8.0;
                _rttVar += (Math.Abs(err) - _rttVar) / 4.0;
            }
            Samples++;
            RoundTripTime = (int)Math.Round(_srtt);
            RoundTripTimeVariance = (int)Math.Round(_rttVar);

            // further off than its own error bound: server clock restarted, drop old samples
            if (Synced && Math.Abs(Wrap(sample - Offset)) > JumpMs + rtt)
            {
                _count = 0;
                _next = 0;
            }

            _rtt[_next] = rtt;
            _off[_next] = sample;
            _next = (_next + 1) % Window;
            if (_count < Window)
                _count++;

            long target = BestOffset();
            if (!Synced)
            {
                Offset = target;
                Synced = true;
                return true;
            }

            long diff = Wrap(target - Offset);
            if (Math.Abs(diff) > JumpMs)
                Offset += diff;
            else
                Offset += Math.Max(-SlewMs, Math.Min(SlewMs, diff));
            return true;
        }

        public int ServerTime(long localNow)
        {
            return (int)((localNow + Offset) & int.MaxValue);
        }

        public void Reset()
        {
            _count = 0;
            _next = 0;
            Samples = 0;
            Synced = false;
            Offset = 0;
            RoundTripTime = 0;
            RoundTripTimeVariance = 0;
        }

        // offset of the lowest-RTT sample in the window (least queueing noise)
        long BestOffset()
        {
            int best = -1;
            for (int k = 0; k < _count; k++)
                if (best < 0 || _rtt[k] < _rtt[best])
                    best = k;
            return _off[best];
        }

        // server clock is int31; keep diffs in [-2^30, 2^30)
        static long Wrap(long d)
        {
            d %= Mod;
            if (d >= Mod / 2) d -= Mod;
            if (d < -Mod / 2) d += Mod;
            return d;
        }
    }
}
