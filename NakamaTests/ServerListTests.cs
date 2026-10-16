using System.Collections.Generic;
using Cmune.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // web AuthenticateApplication rows -> node + Play page (NakamaNodeRows.Pick)
    static class ServerListTests
    {
        static PhotonView Row(int id, string ip, int port, PhotonUsageType usage, string name = "HaZard's Server [DE]")
        {
            return new PhotonView { PhotonId = id, IP = ip, Port = port, UsageType = usage, Name = name, Region = RegionType.EuWest, MinLatency = 0 };
        }

        [Test]
        static void CommOnlyClonesOneGameRow()
        {
            NakamaNodeRows r = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 7350, PhotonUsageType.CommServer), new List<PhotonView>(), false);
            A.Eq(null, r.Error, "no error");
            A.Eq("203.0.113.7:7350", r.Address, "node = comm row");
            A.Eq(PhotonUsageType.CommServer, r.Node.UsageType, "comm usage");
            A.Eq(1, r.Game.Count, "one Play row");
            A.Eq(7, r.Game[0].PhotonId, "clone keeps id");
            A.Eq(PhotonUsageType.All, r.Game[0].UsageType, "clone = All");
            A.Eq("HaZard's Server [DE]", r.Game[0].Name, "name kept (flag)");
            A.Eq(RegionType.EuWest, r.Game[0].Region, "region kept");

            r = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 7350, PhotonUsageType.CommServer), null, false);
            A.Eq(1, r.Game.Count, "null game list ok");
        }

        [Test]
        static void PortChangeInDbMovesEverything()
        {
            NakamaNodeRows a = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 7350, PhotonUsageType.CommServer), null, false);
            NakamaNodeRows b = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 7450, PhotonUsageType.CommServer), null, false);
            A.Eq("203.0.113.7:7350", a.Address, "before");
            A.Eq("203.0.113.7:7450", b.Address, "after");
            A.Eq(7450, b.Game[0].Port, "Play row follows");

            NakamaConfig c = NakamaConfig.Load(null, null);
            c.ApplyNode(a.Node.IP, a.Node.Port, "web.example", "web row #7");
            A.Eq("http://203.0.113.7:7350", c.Endpoint, "dial before");
            c.ApplyNode(b.Node.IP, b.Node.Port, "web.example", "web row #7");
            A.Eq("http://203.0.113.7:7450", c.Endpoint, "dial after");
            A.Eq("203.0.113.7:7450", c.Label, "room label after");
        }

        [Test]
        static void SameAddressGameRowsWinOthersSkipped()
        {
            var games = new List<PhotonView>
            {
                Row(3, "203.0.113.7", 7350, PhotonUsageType.All, "Node EU [DE]"),
                Row(4, "203.0.113.7", 5055, PhotonUsageType.All, "old photon"),
                Row(5, "198.51.100.1", 7350, PhotonUsageType.All, "other host"),
                Row(6, "203.0.113.7", 7350, PhotonUsageType.Mobile, "mobile"),
            };
            NakamaNodeRows r = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 7350, PhotonUsageType.CommServer), games, false);
            A.Eq(1, r.Game.Count, "only the same-address non-mobile row");
            A.Eq(3, r.Game[0].PhotonId, "row 3");
            A.Eq("Node EU [DE]", r.Game[0].Name, "its own name");
            A.Eq(2, r.Warnings.Count, "two skipped");
            A.True(r.Warnings[0].Contains("#4") && r.Warnings[0].Contains("5055"), "old port named: " + r.Warnings[0]);
            A.True(r.Warnings[1].Contains("#5"), "other host named");

            r = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 7350, PhotonUsageType.CommServer), games, true);
            A.Eq(2, r.Game.Count, "mobile build keeps mobile row");
        }

        [Test]
        static void NoCommRowFallsBackToFirstGameRow()
        {
            var games = new List<PhotonView>
            {
                Row(6, "203.0.113.9", 7350, PhotonUsageType.Mobile),
                Row(3, "203.0.113.7", 7450, PhotonUsageType.All),
            };
            NakamaNodeRows r = NakamaNodeRows.Pick(null, games, false);
            A.Eq("203.0.113.7:7450", r.Address, "first non-mobile row");
            A.Eq(PhotonUsageType.CommServer, r.Node.UsageType, "used as comm");
            A.Eq(1, r.Game.Count, "itself");
        }

        [Test]
        static void NothingOrBadRowIsConfigMode()
        {
            NakamaNodeRows r = NakamaNodeRows.Pick(null, new List<PhotonView>(), false);
            A.True(r.Node == null && r.Error != null, "no rows -> error");

            r = NakamaNodeRows.Pick(Row(7, "nk.example", 7350, PhotonUsageType.CommServer), null, false);
            A.True(r.Node == null && r.Error.Contains("#7"), "hostname row -> error: " + r.Error);
            r = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 0, PhotonUsageType.CommServer), null, false);
            A.True(r.Node == null, "port 0 -> error");

            NakamaConfig c = NakamaConfig.Load(null, null);
            r = NakamaNodeRows.FromConfig(c);
            A.Eq("127.0.0.1:7350", r.Address, "config mode label");
            A.Eq(PhotonUsageType.CommServer, r.Node.UsageType, "comm");
            A.Eq(PhotonUsageType.All, r.Game[0].UsageType, "game");
            A.True(r.Node.PhotonId != r.Game[0].PhotonId, "distinct ids");

            c = NakamaConfig.Load("{\"endpoint\":\"https://nk.example:443\",\"replaceServerList\":true}", null);
            r = NakamaNodeRows.FromConfig(c);
            A.Eq("127.0.0.1:443", r.Address, "hostname endpoint -> loopback label");
            A.Eq("Nakama nk.example:443", r.Game[0].Name, "name shows endpoint");
        }

        [Test]
        static void NullNameGetsDefault()
        {
            NakamaNodeRows r = NakamaNodeRows.Pick(Row(7, "203.0.113.7", 7350, PhotonUsageType.CommServer, null), null, false);
            A.Eq("Nakama", r.Node.Name, "comm name (GameServerView NREs on null)");
            A.Eq("Nakama", r.Game[0].Name, "game name");
        }
    }
}
