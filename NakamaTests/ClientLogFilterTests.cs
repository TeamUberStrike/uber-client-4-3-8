using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // webgl ?devlog=1: what may leave the device
    static class ClientLogFilterTests
    {
        const string Page = "192.168.1.51";

        static void No(string scrubbed, string secret, string what)
        {
            A.True(scrubbed.IndexOf(secret, System.StringComparison.Ordinal) < 0, what + ": " + scrubbed);
        }

        [Test]
        static void OptInOnly()
        {
            A.True(ClientLogFilter.Enabled("http://192.168.1.51:8000/?devlog=1"), "?devlog=1");
            A.True(ClientLogFilter.Enabled("http://192.168.1.51:8000/index.html?a=b&devlog=1#x"), "&devlog=1");
            A.True(!ClientLogFilter.Enabled("http://192.168.1.51:8000/"), "default off");
            A.True(!ClientLogFilter.Enabled("http://192.168.1.51:8000/?devlog=10"), "devlog=10");
            A.True(!ClientLogFilter.Enabled("http://192.168.1.51:8000/?xdevlog=1"), "xdevlog");
            A.True(!ClientLogFilter.Enabled(null), "null");
        }

        [Test]
        static void OnlyNakamaLinesAndErrors()
        {
            A.True(ClientLogFilter.Wanted("[nakama] connected", false), "nakama line");
            A.True(!ClientLogFilter.Wanted("Loading shop for player Bob", false), "other log");
            A.True(ClientLogFilter.Wanted("NullReferenceException", true), "error");
            A.True(!ClientLogFilter.Wanted("", true), "empty");
        }

        [Test]
        static void KeepsDebugFacts()
        {
            string s = ClientLogFilter.Scrub("[nakama] connected http://192.168.1.51:7350 as cmid 1234, rtt 12 ms (login 80 ms, socket 40 ms, first sync 30 ms)", Page);
            A.Eq("[nakama] connected http://192.168.1.51:7350 as cmid <n>, rtt 12 ms (login 80 ms, socket 40 ms, first sync 30 ms)", s, "timings + LAN endpoint kept, cmid gone");
            s = ClientLogFilter.Scrub("[nakama] http://127.0.0.1:7350 (default) devAuth=False replaceServerList=False config=default", Page);
            A.Eq("[nakama] http://127.0.0.1:7350 (default) devAuth=False replaceServerList=False config=default", s, "config line unchanged");
            s = ClientLogFilter.Scrub("[nakama] connect to http://203.0.113.7:7350 failed after 10012 ms (Timeout): x", "203.0.113.7");
            A.Eq("[nakama] connect to http://203.0.113.7:7350 failed after 10012 ms (Timeout): x", s, "page host kept");
        }

        [Test]
        static void ScrubsSecretsAndIds()
        {
            No(ClientLogFilter.Scrub("from 81.10.20.30 via 203.0.113.7", Page), "81.10.20.30", "public ip");
            No(ClientLogFilter.Scrub("session eyJhbGciOiJIUzI1NiJ9.eyJ1aWQiOiIxIn0.c2lnbmF0dXJl", Page), "eyJ", "jwt");
            No(ClientLogFilter.Scrub("token=abc123 next", Page), "abc123", "token=");
            No(ClientLogFilter.Scrub("{\"serverKey\":\"defaultkey\"}", Page), "defaultkey", "json key");
            No(ClientLogFilter.Scrub("password: hunter2", Page), "hunter2", "password");
            No(ClientLogFilter.Scrub("Authorization: Bearer abcdef.ghi", Page), "abcdef", "bearer");
            No(ClientLogFilter.Scrub("custom id dev:1234:2:Hazard Test", Page), "Hazard Test", "dev token");
            No(ClientLogFilter.Scrub("user me@example.com failed", Page), "me@example.com", "email");
            No(ClientLogFilter.Scrub("GET http://h.example/a?ticket=99&x=2 failed", Page), "ticket=99", "query");
            No(ClientLogFilter.Scrub("steam 76561198000000001", Page), "76561198000000001", "steam id");
            No(ClientLogFilter.Scrub("[nakama] identity changed 1234 -> 5678, reconnecting", Page), "5678", "identity cmids");
            No(ClientLogFilter.Scrub("blob 0123456789abcdef0123456789abcdef01234567", Page), "0123456789abcdef", "long blob");
            A.True(ClientLogFilter.Scrub(new string('x', 5000), Page).Length <= ClientLogFilter.MaxLine + 3, "length cap");
        }

        [Test]
        static void JsonEscapes()
        {
            A.Eq(@"{""t"":1.500,""type"":""Log"",""msg"":""a\""b\\c\u000ad""}", ClientLogFilter.Json(1.5f, "Log", "a\"b\\c\nd"), "escaped");
        }
    }
}
