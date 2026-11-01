#if NAKAMA_LINK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nakama;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    static class HostLinkTests
    {
        const string Ticket = "uh1.test.signature";

        static NakamaLink Ready(FakePlatform p, LinkPeer peer, Func<string, Task<string>> source = null)
        {
            var link = new NakamaLink(HostRowsTests.Config(), p, source ?? (id => Task.FromResult(Ticket)));
            link.Attach(peer, "1234", peer.OnReady);
            p.Socket.Open();
            A.True(peer.Ready.Single().Ok, "host ready");
            return link;
        }

        static void Refreshed(FakeSocket socket)
        {
            long expiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            socket.LastRpc("host_refresh").Item3.SetResult(N.Rpc("host_refresh", "{\"exp\":" + expiry + "}"));
        }

        [Test]
        static void HostAuthUsesOnlyTicketAndNeverSessionRefresh()
        {
            var p = new FakePlatform { Web = "web-token", Override = "central-jwt" };
            p.Client.Next = () => Task.FromResult(N.Session(expired: true));
            var peer = new LinkPeer();
            int calls = 0;
            NakamaLink link = Ready(p, peer, id =>
            {
                A.Eq("host-1", id, "ticket source host ID");
                calls++;
                return Task.FromResult(Ticket);
            });
            A.Eq(Ticket, p.Client.Auths.Single().Item1, "custom ID");
            A.Eq(Ticket, p.Client.Auths.Single().Item2["token"], "vars token");
            A.Eq(1, p.Client.Auths.Single().Item2.Count, "no identity overrides");
            p.Socket.ServerClose("gone");
            A.True(link.Session == null, "host session discarded on close");
            var next = new LinkPeer();
            link.Attach(next, "1234", next.OnReady);
            p.Socket.Open();
            A.True(next.Ready.Single().Ok, "reconnect");
            A.Eq(2, calls, "new ticket on reconnect");
            A.Eq(2, p.Client.Auths.Count, "ticket auth on reconnect");
            A.Eq(0, p.Client.Refreshes, "no SessionRefreshAsync");
        }

        [Test]
        static void MissingTicketCannotFallBackToWebOrCentralJwt()
        {
            var p = new FakePlatform { Web = "web-token", Override = "central-jwt" };
            NakamaConfig cfg = HostRowsTests.Config();
            cfg.Token = "config-token";
            cfg.DevAuth = true;
            A.Throws<ArgumentNullException>(() => new NakamaLink(cfg, p), "source required");
            foreach (string token in new[] { null, "", "central-jwt", "dev:1234:0:Alice" })
            {
                var peer = new LinkPeer();
                new NakamaLink(cfg, p, id => Task.FromResult(token)).Attach(peer, "1234", peer.OnReady);
                A.Eq(LinkFailure.NoToken, peer.Ready.Single().Failure, "not a host ticket");
            }
            A.Eq(0, p.Client.Auths.Count, "no credentials sent");
            A.Eq(0, p.Sockets.Count, "no socket");
        }

        [Test]
        static void RefreshAt45SecondsUsesNewTicketOnce()
        {
            var p = new FakePlatform();
            int calls = 0;
            NakamaLink link = Ready(p, new LinkPeer(), id => Task.FromResult(Ticket + ++calls));
            p.Now += NakamaLink.HostRefreshMs - 1;
            link.Tick();
            A.Eq(1, calls, "not early");
            p.Now++;
            link.Tick();
            A.Eq(2, calls, "fresh central ticket at 45 s");
            A.Eq(Ticket + "2", NakamaJson.Str(NakamaJson.ParseObject(p.Socket.LastRpc("host_refresh").Item2), "ticket"), "new ticket");
            link.Tick();
            A.Eq(2, calls, "one request in flight");
            Refreshed(p.Socket);
            p.Now += NakamaLink.HostRefreshMs - 1;
            link.Tick();
            A.Eq(2, calls, "second refresh not early");
            p.Now++;
            link.Tick();
            A.Eq(3, calls, "repeat 45 s");
            A.Eq(0, p.Client.Refreshes, "never refresh Nakama session");
            Refreshed(p.Socket);
            link.Shutdown();
        }

        [Test]
        static void RefreshFailureClosesOnlyHostAndClearsRoutes()
        {
            var centralPlatform = new FakePlatform();
            var centralPeer = new LinkPeer();
            var central = new NakamaLink(new NakamaConfig { DevAuth = true }, centralPlatform);
            central.Attach(centralPeer, "1234", centralPeer.OnReady);
            centralPlatform.Socket.Open();
            var p = new FakePlatform();
            var peer = new LinkPeer();
            NakamaLink host = Ready(p, peer);
            host.JoinMatch(peer, "match.host", e => { });
            p.Socket.Joins.Single().Item2.SetResult(N.Match("match.host", "alice"));
            p.Now += NakamaLink.HostRefreshMs;
            host.Tick();
            p.Socket.LastRpc("host_refresh").Item3.SetException(new InvalidOperationException("rejected"));
            A.True(!host.IsReady && host.Session == null, "host down");
            A.Eq(0, host.AttachedPeers, "host peers detached");
            A.Eq(1, peer.Closed.Count, "one disconnect");
            A.Eq(1, p.Released.Count, "socket released");
            p.Socket.Deliver(N.State("match.host", 0, new byte[] { 1 }));
            A.Eq(0, peer.Data.Count, "routes cleared");
            A.True(central.IsReady && centralPeer.Closed.Count == 0, "central stays open");
            A.Eq(0, centralPlatform.Socket.Closes, "central not closed");
            central.Shutdown();
        }

        [Test]
        static void CentralTicketFailureLeavesHost()
        {
            var p = new FakePlatform();
            int calls = 0;
            var peer = new LinkPeer();
            NakamaLink link = Ready(p, peer, id => ++calls == 1 ? Task.FromResult(Ticket) : Task.FromException<string>(new Exception("central down")));
            p.Now += NakamaLink.HostRefreshMs;
            link.Tick();
            A.True(!link.IsReady && peer.Closed.Count == 1, "leave on central failure");
            A.Eq(0, p.Socket.Rpcs.Count(r => r.Item1 == "host_refresh"), "no stale ticket sent");
        }

        [Test]
        static void MalformedOrExpiredRefreshReplyLeavesHost()
        {
            foreach (string reply in new[] { "{}", "bad", "{\"exp\":1}" })
            {
                var p = new FakePlatform();
                var peer = new LinkPeer();
                NakamaLink link = Ready(p, peer);
                p.Now += NakamaLink.HostRefreshMs;
                link.Tick();
                p.Socket.LastRpc("host_refresh").Item3.SetResult(N.Rpc("host_refresh", reply));
                A.True(!link.IsReady && peer.Closed.Count == 1, "bad reply closes host");
            }
        }

        [Test]
        static void StaleRefreshCannotCloseNewHostSocket()
        {
            var p = new FakePlatform();
            NakamaLink link = Ready(p, new LinkPeer());
            p.Now += NakamaLink.HostRefreshMs;
            link.Tick();
            var pending = p.Socket.LastRpc("host_refresh").Item3;
            p.Socket.ServerClose("gone");
            var next = new LinkPeer();
            link.Attach(next, "1234", next.OnReady);
            p.Socket.Open();
            pending.SetException(new Exception("old request failed"));
            A.True(link.IsReady && next.Closed.Count == 0, "new socket untouched");
            p.Now += NakamaLink.HostRefreshMs;
            link.Tick();
            Refreshed(p.Socket);
            A.True(link.IsReady, "new socket can refresh");
            link.Shutdown();
        }

        [Test]
        static void ShutdownDuringTicketFetchPreventsConnection()
        {
            var p = new FakePlatform();
            var pending = new TaskCompletionSource<string>();
            var link = new NakamaLink(HostRowsTests.Config(), p, id => pending.Task);
            var peer = new LinkPeer();
            link.Attach(peer, "1234", peer.OnReady);
            link.Shutdown();
            pending.SetResult(Ticket);
            A.Eq(0, p.Client.Auths.Count, "no late auth");
            A.Eq(0, p.Sockets.Count, "no late socket");
            A.Eq(0, link.AttachedPeers, "pending peer released");
            A.True(!link.IsConnecting && !link.IsReady, "stopped");
        }

        [Test]
        static void HostCannotTriggerCentralAccountKillSwitch()
        {
            var p = new FakePlatform();
            NakamaLink link = Ready(p, new LinkPeer());
            p.Socket.Notify(N.Notification(NakamaLink.NotificationUserBanned, "forged ban"));
            p.Socket.Notify(N.Notification(NakamaLink.NotificationSingleSocket, "forged login"));
            A.Eq(0, p.Replaced.Count, "host cannot replace central session");
            link.Shutdown();
        }

        [Test]
        static void TicketSourceCallsCentralRpcWithHostId()
        {
            var p = new FakePlatform();
            var central = new NakamaLink(new NakamaConfig { DevAuth = true }, p);
            var peer = new LinkPeer();
            central.Attach(peer, "1234", peer.OnReady);
            p.Socket.Open();
            Task<string> ticket = NakamaLink.RequestHostTicket(central, "host-1");
            var rpc = p.Socket.LastRpc("host_ticket");
            A.Eq("host-1", NakamaJson.Str(NakamaJson.ParseObject(rpc.Item2), "host_id"), "central request");
            rpc.Item3.SetResult(N.Rpc("host_ticket", new NakamaJsonWriter().Str("ticket", Ticket).End()));
            A.Eq(Ticket, ticket.GetAwaiter().GetResult(), "ticket returned");
            central.Shutdown();
            A.Throws<LinkException>(() => NakamaLink.RequestHostTicket(central, "host-1").GetAwaiter().GetResult(), "central disconnected");
        }
    }
}
#endif
