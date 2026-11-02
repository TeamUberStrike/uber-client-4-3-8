using System;
using System.Collections.Generic;
using Cmune.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    static class HostRowsTests
    {
        public static string Row(string endpoint = "http://127.0.0.1:7355", string match = "match.host")
        {
            return new NakamaJsonWriter().Str("host_id", "host-1").Str("endpoint", endpoint)
                .Str("match_id", match).Str("meta", "Zw==").Bool("unranked", true)
                .Str("name", "Host match").Str("region", "eu").Num("players", 1).Num("max_players", 8).End();
        }

        public static NakamaConfig Config()
        {
            var rows = new NakamaNodeRows();
            rows.ApplyHosts("{\"hosts\":[" + Row() + "]}");
            return NakamaConfig.FromHost(rows.Hosts[0]);
        }

        [Test]
        static void HostRowsStaySeparateFromCentralRows()
        {
            var comm = new PhotonView { IP = "203.0.113.7", Port = 443, UsageType = PhotonUsageType.CommServer };
            var other = new PhotonView { IP = "198.51.100.1", Port = 7355, UsageType = PhotonUsageType.All };
            NakamaNodeRows rows = NakamaNodeRows.Pick(comm, new[] { other }, false);
            rows.ApplyHosts("{\"hosts\":[" + Row() + "," + Row("http://127.0.0.2:7355") + "]}");
            A.Eq("203.0.113.7:443", rows.Address, "central unchanged");
            A.Eq(1, rows.Game.Count, "central game row");
            A.Eq(1, rows.Warnings.Count, "normal foreign row still skipped");
            A.Eq(2, rows.Hosts.Count, "host rows separate");
            A.True(rows.Hosts[0].Key != rows.Hosts[1].Key, "endpoint separates identical match IDs");
            A.Eq("host-1", rows.Hosts[0].HostId, "host ID");
            A.Eq("Zw==", rows.Hosts[0].Meta, "wire metadata");
            A.True(rows.Hosts[0].Unranked, "unranked");
            rows.ApplyHosts("{\"hosts\":[]}");
            A.Eq(0, rows.Hosts.Count, "replace stale hosts");
            A.Eq(1, rows.Game.Count, "central kept after empty list");
        }

        [Test]
        static void InvalidHostListDoesNotPartiallyReplaceRows()
        {
            var rows = new NakamaNodeRows();
            rows.ApplyHosts("{\"hosts\":[" + Row() + "]}");
            foreach (string bad in new[]
            {
                "{}", "{\"hosts\":null}", "{\"hosts\":[null]}",
                "{\"hosts\":[" + Row() + "," + Row() + "]}",
                "{\"hosts\":[" + Row().Replace("true", "false") + "]}",
                "{\"hosts\":[" + Row().Replace("Zw==", "bad base64") + "]}",
                "{\"hosts\":[" + Row().Replace("host-1", "") + "]}",
                "{\"hosts\":[" + Row().Replace("\"players\":1", "\"players\":99") + "]}",
            })
            {
                A.Throws<FormatException>(() => rows.ApplyHosts(bad), "invalid list rejected");
                A.Eq(1, rows.Hosts.Count, "old list retained");
            }
        }

        [Test]
        static void HostConfigDoesNotInheritCentralCredentials()
        {
            NakamaConfig saved = NakamaConfig.Current;
            try
            {
                NakamaConfig.Current = new NakamaConfig { Token = "central-jwt", ServerKey = "central-key", DevAuth = true };
                NakamaConfig cfg = Config();
                A.True(cfg.IsHost && cfg.Pinned && cfg.SchemePinned, "host config pinned");
                A.Eq("host-1", cfg.HostId, "host ID");
                A.Eq("match.host", cfg.MatchId, "match ID");
                A.True(cfg.Token == null && !cfg.DevAuth && cfg.ServerKey != "central-key", "no central credentials");
                A.Eq("http://127.0.0.1:7355", cfg.Endpoint, "plain host endpoint");
                A.Eq(0, cfg.CloseGraceMs, "host closes when detached");
                A.True(!NakamaConfig.Current.IsHost, "central not retargeted");
            }
            finally { NakamaConfig.Current = saved; }
        }

        [Test]
        static void WebSocketEndpointOptInOnlyForHostRows()
        {
            var rows = new NakamaNodeRows();
            foreach (string scheme in new[] { "http", "https", "ws", "wss" })
            {
                rows.ApplyHosts("{\"hosts\":[" + Row(scheme + "://127.0.0.1:7355") + "]}");
                NakamaConfig cfg = NakamaConfig.FromHost(rows.Hosts[0]);
                A.Eq(scheme == "https" || scheme == "wss" ? "https" : "http", cfg.Scheme, "SDK transport scheme");
            }
            A.True(!new NakamaConfig().TrySetEndpoint("ws://127.0.0.1:7355"), "normal endpoint parser unchanged");
            foreach (string endpoint in new[] { "ftp://127.0.0.1:7355", "http://user@127.0.0.1:7355", "http://127.0.0.1:7355/x", "http://127.0.0.1:7355/?token=x", "http://127.0.0.1:7355/#x", "http://host.test:7355", "http://127.0.0.1:0" })
                A.Throws<FormatException>(() => rows.ApplyHosts("{\"hosts\":[" + Row(endpoint) + "]}"), "invalid endpoint");
        }
    }
}
