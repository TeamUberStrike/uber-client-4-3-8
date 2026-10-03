using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    static class ClockTests
    {
        [Test]
        static void FirstSampleSetsOffset()
        {
            var c = new NakamaClock();
            A.True(!c.Synced, "unsynced");
            // sent 1000, back 1040, server said 50020 at mid 1020 -> offset 49000
            A.True(c.AddSample(1000, 1040, 50020), "sample");
            A.True(c.Synced, "synced");
            A.Eq(49000L, c.Offset, "offset");
            A.Eq(40, c.RoundTripTime, "rtt");
            A.Eq(20, c.RoundTripTimeVariance, "var = rtt/2 first");
            A.Eq(50100, c.ServerTime(1100), "server time");
        }

        [Test]
        static void RejectsNonsense()
        {
            var c = new NakamaClock();
            A.True(!c.AddSample(100, 90, 5), "negative rtt");
            A.True(!c.AddSample(100, 110, -1), "negative server ms");
            A.True(!c.Synced, "still unsynced");
        }

        [Test]
        static void MinRttSampleWinsAndSlews()
        {
            var c = new NakamaClock();
            c.AddSample(0, 10, 10005);              // offset 10000, rtt 10
            c.AddSample(1000, 1200, 11150);         // queueing: offset 10050 but rtt 200 -> ignored (min-rtt)
            A.Eq(10000L, c.Offset, "noisy sample ignored");
            c.AddSample(2000, 2008, 12034);         // offset 10030, rtt 8 -> new best, slew +10
            A.Eq(10010L, c.Offset, "slew 10");
            c.AddSample(3000, 3010, 13035);         // best still 10030 -> +10
            A.Eq(10020L, c.Offset, "slew 20");
            c.AddSample(4000, 4010, 14035);
            A.Eq(10030L, c.Offset, "converged");
            c.AddSample(5000, 5010, 15035);
            A.Eq(10030L, c.Offset, "stable");
        }

        [Test]
        static void RttEwma()
        {
            var c = new NakamaClock();
            c.AddSample(0, 100, 50);
            for (int i = 1; i <= 50; i++)
                c.AddSample(i * 1000, i * 1000 + 20, i * 1000 + 10);
            A.True(c.RoundTripTime >= 20 && c.RoundTripTime <= 21, "ewma converges to 20, got " + c.RoundTripTime);
            A.True(c.RoundTripTimeVariance <= 1, "variance shrinks, got " + c.RoundTripTimeVariance);
        }

        [Test]
        static void ServerRestartJumps()
        {
            var c = new NakamaClock();
            for (int i = 0; i < 8; i++)
                c.AddSample(i * 1000, i * 1000 + 10, 900000 + i * 1000 + 5);
            A.Eq(900000L, c.Offset, "before");
            c.AddSample(9000, 9030, 400);           // server restarted: offset -8615
            A.Eq(-8615L, c.Offset, "jump to new clock despite old low-rtt samples");
            c.AddSample(10000, 10010, 1390);        // offset -8615 again (mid 10005)
            A.Eq(-8615L, c.Offset, "old samples flushed");
        }

        [Test]
        static void Int31Wrap()
        {
            var c = new NakamaClock();
            c.AddSample(0, 10, int.MaxValue - 4);   // offset = 2^31-1-4-5
            int before = c.ServerTime(5);
            A.Eq(int.MaxValue - 4, before, "near max");
            int after = c.ServerTime(15);
            A.Eq(5, after, "wraps like the Go clock (& 0x7FFFFFFF)");
            c.AddSample(1000, 1010, 995);           // server wrapped too: same offset mod 2^31
            A.Eq(995, c.ServerTime(1005), "consistent after wrap sample");
        }
    }

    static class ConfigTests
    {
        [Test]
        static void DefaultsAreQ3()
        {
            NakamaConfig c = NakamaConfig.Load(null, new string[0]);
            A.Eq("http://127.0.0.1:7350", c.Endpoint, "endpoint");
            A.Eq("defaultkey", c.ServerKey, "key");
            A.Eq(false, c.DevAuth, "dev auth off by default");
            A.Eq("127.0.0.1:20000", c.GameServerAddress, "game row = Go UBER_ROOM_PORT_BASE");
            A.Eq("127.0.0.1:20088", c.CommServerAddress, "comm row = base + 88 (Go Address.For(88))");
            A.Eq(true, c.ReplaceServerList, "replace list");
            A.Eq("default", c.Source, "source");
        }

        [Test]
        static void JsonThenArgs()
        {
            string json = "{\"scheme\":\"https\",\"host\":\"uber.example\",\"port\":443,\"serverKey\":\"k1\",\"devAuth\":true,\"roomHost\":\"10.0.0.5\",\"roomPortBase\":30000,\"replaceServerList\":false}";
            NakamaConfig c = NakamaConfig.Load(json, new string[0]);
            A.Eq("https://uber.example:443", c.Endpoint, "json endpoint");
            A.Eq("k1", c.ServerKey, "json key");
            A.Eq(true, c.DevAuth, "json dev");
            A.Eq("10.0.0.5:30088", c.CommServerAddress, "json rooms");
            A.Eq(false, c.ReplaceServerList, "json replace");

            c = NakamaConfig.Load(json, new[] { "UberStrike.exe", "-nakama", "http://192.168.1.9:7350", "-NakamaKey", "k2", "-nakamatoken", "tok" });
            A.Eq("http://192.168.1.9:7350", c.Endpoint, "args win");
            A.Eq("k2", c.ServerKey, "args key");
            A.Eq("tok", c.Token, "token");
            A.Eq("nakama.json + command line", c.Source, "source");
        }

        [Test]
        static void EndpointParsing()
        {
            var c = new NakamaConfig();
            A.True(c.TrySetEndpoint("https://nk.example.org"), "https default port");
            A.Eq("https://nk.example.org:443", c.Endpoint, "443");
            A.True(c.TrySetEndpoint("http://localhost:7351/"), "trailing slash");
            A.Eq("http://localhost:7351", c.Endpoint, "7351");
            A.True(!c.TrySetEndpoint("ws://x:1"), "ws scheme rejected");
            A.True(!c.TrySetEndpoint("http://x:99999"), "bad port");
            A.True(!c.TrySetEndpoint("127.0.0.1:7350"), "no scheme");
            A.True(!c.TrySetEndpoint("http://h/path"), "path");
            A.Eq("http://localhost:7351", c.Endpoint, "unchanged after bad input");
        }

        [Test]
        static void DevFlagAndBadValues()
        {
            NakamaConfig c = NakamaConfig.Load("{\"port\":0,\"timePingMs\":5,\"roomPortBase\":70000}", new[] { "-nakamadev", "-nakama" });
            A.Eq(true, c.DevAuth, "-nakamadev");
            A.Eq(7350, c.Port, "port fixed");
            A.Eq(100, c.TimePingMs, "ping floor");
            A.Eq(20000, c.RoomPortBase, "room base fixed");
        }
    }
}
