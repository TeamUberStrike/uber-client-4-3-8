#if NAKAMA_LINK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using Cmune.Realtime.Photon.Client.Transport;
using Nakama;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // The shipped NakamaLink (session engine) against proxied nakama-dotnet interfaces.
    static class LinkTests
    {
        static NakamaConfig Cfg(bool dev = true)
        {
            NakamaConfig c = NakamaConfig.Load(null, dev ? new[] { "-nakamadev" } : new string[0]);
            return c;
        }

        static NakamaLink Ready(FakePlatform p, LinkPeer peer, NakamaConfig cfg = null)
        {
            var link = new NakamaLink(cfg ?? Cfg(), p);
            link.Attach(peer, "1234", peer.OnReady);
            p.Socket.Open();
            A.True(peer.Ready.Count == 1 && peer.Ready[0].Ok, "ready: " + (peer.Ready.Count > 0 ? peer.Ready[0].Message : "none"));
            return link;
        }

        [Test]
        static void FirstAttachAuthConnectSyncThenShared()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            var link = new NakamaLink(Cfg(), p);
            link.Attach(a, "1234", a.OnReady);
            A.Eq(1, p.Client.Auths.Count, "one auth");
            A.Eq("dev:1234:2:Hazard Test", p.Client.Auths[0].Item1, "custom id = dev token");
            A.Eq("dev:1234:2:Hazard Test", p.Client.Auths[0].Item2["token"], "vars.token");
            A.Eq(1, p.Sockets.Count, "socket created");
            A.Eq(0, a.Ready.Count, "not ready before socket connect");
            A.True(link.IsConnecting, "connecting");

            p.Socket.Open();
            A.True(a.Ready.Single().Ok, "ready after connect + first uber_time");
            A.Eq(1, p.Socket.TimeCalls, "first sync");
            A.True(link.Clock.Synced, "clock synced before Connect status");
            A.True(link.IsReady, "ready");
            A.Eq(1234, link.AuthenticatedCmid, "cmid");

            var b = new LinkPeer();
            link.Attach(b, "1234", b.OnReady);
            A.True(b.Ready.Single().Ok, "second peer shares socket at once");
            var probe = new LinkPeer();
            link.Attach(probe, "0", probe.OnReady);
            A.True(probe.Ready.Single().Ok, "probe (cmid 0) shares socket");
            A.Eq(1, p.Client.Auths.Count, "no re-auth");
            A.Eq(1, p.Sockets.Count, "one socket");
            A.Eq(3, link.AttachedPeers, "3 attached");
        }

        [Test]
        static void TokenSources()
        {
            var p = new FakePlatform { Web = "web-tok" };
            var x = new LinkPeer();
            new NakamaLink(Cfg(false), p).Attach(x, "1234", x.OnReady);
            A.Eq("web-tok", p.Client.Auths[0].Item1, "web token without dev auth");

            p = new FakePlatform { Override = "ovr", Web = "web-tok" };
            new NakamaLink(Cfg(true), p).Attach(x, "1234", x.OnReady);
            A.Eq("ovr", p.Client.Auths[0].Item1, "override beats dev and web");

            p = new FakePlatform();
            NakamaConfig c = NakamaConfig.Load(null, new[] { "-nakamatoken", "cli-tok" });
            new NakamaLink(c, p).Attach(x, "1234", x.OnReady);
            A.Eq("cli-tok", p.Client.Auths[0].Item1, "-nakamatoken");

            p = new FakePlatform();
            var y = new LinkPeer();
            new NakamaLink(Cfg(false), p).Attach(y, "1234", y.OnReady);
            A.Eq(LinkFailure.NoToken, y.Ready.Single().Failure, "no token");
            A.Eq(0, p.Client.Auths.Count, "no auth call without token");

            p = new FakePlatform { Id = new NakamaIdentity() };
            var z = new LinkPeer();
            new NakamaLink(Cfg(true), p).Attach(z, "0", z.OnReady);
            A.Eq(LinkFailure.NoToken, z.Ready.Single().Failure, "dev auth needs a cmid (not logged in)");

            A.Eq("dev:7:10:Player7", NakamaLink.DevToken(7, 99, "  "), "clamp + default name");
            A.Eq(64, NakamaLink.DevToken(7, 1, new string('n', 80)).Length - "dev:7:1:".Length, "name max 64");
        }

        [Test]
        static void AuthRejectedBacksOff()
        {
            var p = new FakePlatform();
            p.Client.Next = () => Task.FromException<ISession>(new ApiResponseException(401, "{\"message\":\"login rejected\"}", 16));
            var link = new NakamaLink(Cfg(), p);
            var a = new LinkPeer();
            link.Attach(a, "1234", a.OnReady);
            A.Eq(LinkFailure.AuthRejected, a.Ready.Single().Failure, "401 -> AuthRejected");
            A.Eq(0, link.AttachedPeers, "failed peer dropped");

            var b = new LinkPeer();
            link.Attach(b, "1234", b.OnReady);
            A.Eq(LinkFailure.AuthRejected, b.Ready.Single().Failure, "backoff");
            A.Eq(1, p.Client.Auths.Count, "no hammering during backoff");

            p.Now += 3000;
            p.Client.Next = () => Task.FromResult(N.Session());
            var c = new LinkPeer();
            link.Attach(c, "1234", c.OnReady);
            p.Socket.Open();
            A.True(c.Ready.Single().Ok, "retry after backoff");
        }

        [Test]
        static void FailureClassification()
        {
            A.Eq(LinkFailure.AuthRejected, NakamaLink.Classify(new ApiResponseException(403, "banned", 7)).Failure, "403");
            A.Eq(LinkFailure.Unreachable, NakamaLink.Classify(new ApiResponseException("conn refused", new Exception("x"))).Failure, "transport");
            A.Eq(LinkFailure.Unreachable, NakamaLink.Classify(new ApiResponseException(500, "boom", 13)).Failure, "500");
            A.Eq(LinkFailure.Tls, NakamaLink.Classify(new WebSocketException("handshake", new AuthenticationException("cert"))).Failure, "tls");
            A.Eq(LinkFailure.Timeout, NakamaLink.Classify(new TaskCanceledException()).Failure, "timeout");
            A.Eq(LinkFailure.Timeout, NakamaLink.Classify(new AggregateException(new TimeoutException())).Failure, "aggregate timeout");
            A.Eq(LinkFailure.NoToken, NakamaLink.Classify(new LinkException(LinkFailure.NoToken, "x")).Failure, "link exception");
        }

        [Test]
        static void SocketConnectFailure()
        {
            var p = new FakePlatform();
            var link = new NakamaLink(Cfg(), p);
            var a = new LinkPeer();
            link.Attach(a, "1234", a.OnReady);
            p.Socket.ConnectTcs.SetException(new WebSocketException("connection refused"));
            A.Eq(LinkFailure.Unreachable, a.Ready.Single().Failure, "unreachable");
            A.Eq(1, p.Socket.Closes, "failed socket closed");
            A.True(!link.IsReady && !link.IsConnecting, "idle");
        }

        [Test]
        static void RoutingAndKick()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            var b = new LinkPeer();
            NakamaLink link = Ready(p, a);
            link.Attach(b, "1234", b.OnReady);
            string errA = "x", errB = "x";
            link.JoinMatch(a, "m1", e => errA = e);
            link.JoinMatch(b, "m2", e => errB = e);
            p.Socket.Joins[0].Item2.SetResult(N.Match("m1", "sess-1"));
            p.Socket.Joins[1].Item2.SetResult(N.Match("m2", "sess-1"));
            A.True(errA == null && errB == null, "joined");

            p.Socket.Deliver(N.State("m1", 0, new byte[] { 1 }));
            p.Socket.Deliver(N.State("m2", 3, new byte[] { 2 }));
            p.Socket.Deliver(N.State("zz", 0, new byte[] { 3 }));
            A.True(a.Data.Single().Item1 == "m1" && b.Data.Single().Item2 == 3, "routed by match id");

            p.Socket.Presence(N.Leave("m1", "other-session"));
            A.Eq(0, a.Left.Count, "someone else leaving is not a kick");
            p.Socket.Presence(N.Leave("m1", "sess-1"));
            A.Eq("m1", a.Left.Single(), "own presence left -> kick");
            A.Eq(0, b.Left.Count, "b untouched");
            p.Socket.Deliver(N.State("m1", 0, new byte[] { 4 }));
            A.Eq(1, a.Data.Count, "route gone after kick");

            A.True(link.Send("m2", 82, new byte[] { 9 }), "send");
            A.Eq(82L, p.Socket.Sent.Single().Item2, "SendMatchStateAsync op");
        }

        [Test]
        static void JoinRejectionAndLeave()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            NakamaLink link = Ready(p, a);
            string err = null;
            link.JoinMatch(a, "g1", e => err = e);
            p.Socket.Joins[0].Item2.SetException(new WebSocketException(WebSocketError.InvalidState, "rc=2"));
            A.True(err != null && NakamaFraming.ParseRejectCode(err) == 2, "reject reason survives: " + err);
            p.Socket.Deliver(N.State("g1", 0, new byte[] { 1 }));
            A.Eq(0, a.Data.Count, "route removed on reject");

            string leaveErr = "x";
            link.JoinMatch(a, "g2", e => { });
            link.LeaveMatch(a, "g2", e => leaveErr = e);
            A.True(leaveErr == null && p.Socket.Leaves.Single() == "g2", "LeaveMatchAsync");
        }

        [Test]
        static void ServerCloseDropsEveryPeerThenReconnects()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            var b = new LinkPeer();
            NakamaLink link = Ready(p, a);
            link.Attach(b, "1234", b.OnReady);
            link.JoinMatch(a, "m1", e => { });
            p.Socket.ServerClose("session disconnected");
            A.True(a.Closed.Single().Item1 && b.Closed.Single().Item1, "both by server");
            A.True(!link.IsReady && link.AttachedPeers == 0, "down, peers dropped");

            string rpcErr = null;
            link.Rpc("uber_op66", "{}", (r, e) => rpcErr = e);
            A.Eq("not connected", rpcErr, "rpc refused synchronously");
            A.True(!link.Send("m1", 82, new byte[1]), "send refused");

            var c = new LinkPeer();
            link.Attach(c, "1234", c.OnReady);
            A.Eq(2, p.Sockets.Count, "new socket");
            A.Eq(1, p.Client.Auths.Count, "session token reused");
            p.Socket.Open();
            A.True(c.Ready.Single().Ok, "reconnected");
        }

        [Test]
        static void IdleSocketClosesAfterGrace()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            NakamaLink link = Ready(p, a);
            link.Detach(a);
            p.Now += link.Config.CloseGraceMs - 1;
            link.Tick();
            A.Eq(0, p.Socket.Closes, "inside grace");

            var b = new LinkPeer();
            link.Attach(b, "1234", b.OnReady);
            p.Now += 60000;
            link.Tick();
            A.Eq(0, p.Socket.Closes, "re-attached: stays open");

            link.Detach(b);
            p.Now += link.Config.CloseGraceMs;
            link.Tick();
            A.Eq(1, p.Socket.Closes, "closed after grace");
            A.True(!link.IsReady, "down");
        }

        [Test]
        static void IdentityChangeReconnects()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            NakamaLink link = Ready(p, a);
            var b = new LinkPeer();
            link.Attach(b, "999", b.OnReady);
            A.True(a.Closed.Single().Item1 == false, "old identity peer dropped (by client)");
            A.Eq(2, p.Client.Auths.Count, "re-auth");
            A.True(p.Client.Auths[1].Item1.StartsWith("dev:999:"), "new cmid from appName");
            p.Socket.Open();
            A.True(b.Ready.Single().Ok, "new identity ready");
            A.Eq(999, link.AuthenticatedCmid, "cmid 999");
        }

        [Test]
        static void ClockPingLoop()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            NakamaLink link = Ready(p, a);
            p.Socket.AutoTime = false;
            link.Tick();
            A.Eq(1, p.Socket.TimeCalls, "no ping before interval");
            p.Now += link.Config.TimePingMs;
            link.Tick();
            link.Tick();
            A.Eq(2, p.Socket.TimeCalls, "one ping in flight");
            var t = p.Socket.LastRpc("uber_time");
            long c = (long)NakamaJson.ParseObject(t.Item2)["c"];
            p.Now += 30;
            t.Item3.SetResult(N.Rpc("uber_time", "{\"c\":" + c + ",\"s\":90000}"));
            A.Eq(2, link.Clock.Samples, "sample added");
            A.Eq(90000, link.ServerTimeMs - 15, "server ms = s + rtt/2 at receive");

            link.SyncClock();
            link.Tick();
            A.Eq(3, p.Socket.TimeCalls, "SyncClock pings now");
        }

        [Test]
        static void NotificationsHitKillSwitch()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            Ready(p, a);
            p.Socket.Notify(N.Notification(-6, "friend"));
            A.Eq(0, p.Replaced.Count, "other codes ignored");
            p.Socket.Notify(N.Notification(NakamaLink.NotificationSingleSocket, "single_socket"));
            p.Socket.Notify(N.Notification(NakamaLink.NotificationUserBanned, "banned"));
            A.Eq(2, p.Replaced.Count, "single_socket + banned -> OnDisconnectAndDisablePhoton path");
        }

        [Test]
        static void SocketClosedDuringFirstSync()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            var link = new NakamaLink(Cfg(), p);
            link.Attach(a, "1234", a.OnReady);
            p.Socket.AutoTime = false;
            p.Socket.Open();
            A.Eq(0, a.Ready.Count, "waiting for first uber_time");
            p.Socket.ServerClose("gone");
            p.Socket.LastRpc("uber_time").Item3.SetCanceled();
            A.Eq(LinkFailure.Closed, a.Ready.Single().Failure, "fails, not a fake success");
        }

        [Test]
        static void ShutdownClosesByClient()
        {
            var p = new FakePlatform();
            var a = new LinkPeer();
            NakamaLink link = Ready(p, a);
            link.Shutdown();
            A.True(a.Closed.Single().Item1 == false, "by client");
            A.Eq(1, p.Socket.Closes, "CloseAsync");
        }

        [Test]
        static void PeerOverLinkEndToEnd()
        {
            var p = new FakePlatform();
            var link = new NakamaLink(Cfg(), p);
            var peer = new NakamaPeer(link);
            var l = new RecordingListener { Peer = peer };
            peer.Listener = l;

            A.True(peer.Connect("127.0.0.1:20088", "1234"), "Connect");
            p.Socket.Open();
            l.Pump(peer);
            A.Eq(StatusCode.Connect, l.Statuses.Single(), "Connect status after link ready");

            peer.SendOperation(66, new Dictionary<byte, object> { { 100, (byte)1 }, { 61, (short)2 }, { 103, new byte[] { 1 } } }, true);
            var op66 = p.Socket.LastRpc("uber_op66");
            op66.Item3.SetResult(N.Rpc("uber_op66", "{\"rc\":0,\"i\":2}"));
            l.Pump(peer);
            A.Eq((byte)66, l.Responses.Single().OperationCode, "response 66");

            peer.SendOperation(88, new Dictionary<byte, object> { { 103, new byte[] { 0x28 } }, { 206, 1234 }, { 205, 0 } }, true);
            p.Socket.LastRpc("uber_room_join").Item3.SetResult(N.Rpc("uber_room_join", "{\"rc\":0,\"match\":\"comm.n\",\"number\":88}"));
            l.Pump(peer);
            p.Socket.Joins.Single().Item2.SetResult(N.Match("comm.n", "sess-9"));
            byte[] ack = { 1, 0, 0, 0, 1, 0, 0, 0, 5, 0, 0, 0, 0, 1, 7, 88, 0, 0, 0, 127, 0, 0, 1, 0x78, 0x4e };
            p.Socket.Deliver(N.State("comm.n", 88, ack));
            l.Pump(peer);
            A.Eq((short)0, l.Responses.Last().ReturnCode, "joined");

            peer.SendOperation(82, new Dictionary<byte, object> { { 101, (short)2 }, { 100, (byte)3 }, { 103, new byte[] { 6, 1, 0, 0, 0 } } }, true);
            A.Bytes(new byte[] { 2, 0, 3, 6, 1, 0, 0, 0 }, p.Socket.Sent.Single().Item3, "op 82 on the wire");
            p.Socket.Deliver(N.State("comm.n", 0, new byte[] { 1, 0, 1, 6, 1, 0, 0, 0 }));
            l.Pump(peer);
            A.Eq((short)1, (short)l.Events.Single().Parameters[101], "event 0 back");

            p.Socket.Presence(N.Leave("comm.n", "sess-9"));
            l.Pump(peer);
            A.Eq(StatusCode.DisconnectByServerLogic, l.Statuses.Last(), "MatchKick -> DisconnectByServerLogic");
            A.Eq(PeerStateValue.Disconnected, peer.PeerState, "down");
            A.Eq(0, link.AttachedPeers, "detached");
        }
    }
}
#endif
