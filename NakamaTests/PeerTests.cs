using System;
using System.Collections.Generic;
using Cmune.Realtime.Photon.Client.Transport;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    static class PeerTests
    {
        // node row = every room's address (comm, lobby, games)
        const string GameAddr = "127.0.0.1:7350";
        const string CommAddr = GameAddr;

        static readonly byte[] Ack88 = { 3, 0, 0, 0, 2, 0, 0, 0, 4, 3, 2, 1, 0, 1, 7, 88, 0, 0, 0, 127, 0, 0, 1, 0x78, 0x4e };

        sealed class Rig
        {
            public FakeLink Link = new FakeLink();
            public RecordingListener L = new RecordingListener();
            public NakamaPeer Peer;

            public Rig()
            {
                Peer = new NakamaPeer(Link);
                Peer.Listener = L;
                L.Peer = Peer;
            }

            public int Pump() { return L.Pump(Peer); }

            public void Connected(string addr = GameAddr)
            {
                A.True(L.Call(() => Peer.Connect(addr, "1234")), "connect");
                Link.Complete(Peer, LinkResult.Success);
                Pump();
                A.Eq(PeerStateValue.Connected, Peer.PeerState, "connected");
            }

            public bool Send(byte op, Dictionary<byte, object> p)
            {
                return L.Call(() => Peer.SendOperation(op, p, true));
            }

            public void Joined(string match = "m1", int number = 101)
            {
                A.True(Send(88, JoinParams()), "send 88");
                Link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"" + match + "\",\"number\":" + number + "}", null);
                Pump();
                Link.Deliver(match, 88, Ack88);
                Pump();
                OperationResponse r = L.Responses[L.Responses.Count - 1];
                A.Eq((byte)88, r.OperationCode, "response 88");
                A.Eq((short)0, r.ReturnCode, "rc 0");
            }
        }

        static Dictionary<byte, object> JoinParams()
        {
            return new Dictionary<byte, object> { { 103, new byte[] { 0x67, 1, 2 } }, { 206, 1234 }, { 205, 0 } };
        }

        static Dictionary<byte, object> Room(short netId, byte method, byte[] args)
        {
            return new Dictionary<byte, object> { { 101, netId }, { 100, method }, { 103, args } };
        }

        [Test]
        static void ConnectIsAsyncThenConnect()
        {
            var r = new Rig();
            A.True(r.L.Call(() => r.Peer.Connect(GameAddr, "1234")), "connect true");
            A.Eq(PeerStateValue.Connecting, r.Peer.PeerState, "connecting");
            A.Eq("1234", r.Link.AppNames[0], "appName = cmid");
            A.Eq(0, r.Pump(), "nothing before link ready");
            r.Link.Complete(r.Peer, LinkResult.Success);
            A.Eq(1, r.Pump(), "one status");
            A.Eq(StatusCode.Connect, r.L.Statuses[0], "Connect");
            A.Eq(PeerStateValue.Connected, r.L.StateAtStatus[0], "Connected before callback (listener sends 66 inside)");
            A.True(!r.L.Call(() => r.Peer.Connect(GameAddr, "1234")), "second Connect refused");
        }

        [Test]
        static void SynchronousLinkStillNoCallbackInsideConnect()
        {
            var r = new Rig();
            r.Link.ReadyImmediately = true;
            A.True(r.L.Call(() => r.Peer.Connect(GameAddr, "1")), "connect");
            A.Eq(0, r.L.Statuses.Count, "no status yet");
            A.Eq(1, r.Pump(), "Connect on pump");
        }

        [Test]
        static void ConnectFailures()
        {
            foreach (LinkFailure f in new[] { LinkFailure.AuthRejected, LinkFailure.NoToken, LinkFailure.Unreachable, LinkFailure.Timeout, LinkFailure.Closed })
            {
                var r = new Rig();
                r.L.Call(() => r.Peer.Connect(GameAddr, "1"));
                r.Link.Complete(r.Peer, LinkResult.Fail(f, "x"));
                r.Pump();
                A.Eq(StatusCode.ExceptionOnConnect, r.L.Statuses[0], f + " -> ExceptionOnConnect");
                A.True(r.L.StateAtStatus[0] > 0, "pump still alive during callback");
                A.Eq(PeerStateValue.Disconnected, r.Peer.PeerState, "then Disconnected");
                A.True(!r.Link.Attached.Contains(r.Peer), "detached");
            }
        }

        [Test]
        static void TlsFailureIsSecurityThenDisconnect()
        {
            var r = new Rig();
            r.L.Call(() => r.Peer.Connect(GameAddr, "1"));
            r.Link.Complete(r.Peer, LinkResult.Fail(LinkFailure.Tls, "cert"));
            A.True(r.Peer.DispatchIncomingCommands(), "first item");
            A.Eq(1, r.L.Statuses.Count, "one per dispatch");
            A.True(r.Peer.DispatchIncomingCommands(), "second item");
            A.True(!r.Peer.DispatchIncomingCommands(), "empty");
            A.Eq(StatusCode.SecurityExceptionOnConnect, r.L.Statuses[0], "security");
            A.Eq(StatusCode.Disconnect, r.L.Statuses[1], "disconnect");
            A.Eq(PeerStateValue.Disconnected, r.Peer.PeerState, "disconnected");
        }

        [Test]
        static void SendRefusedUnlessConnected()
        {
            var r = new Rig();
            var p66 = new Dictionary<byte, object> { { 100, (byte)1 }, { 61, (short)1 }, { 103, new byte[0] } };
            A.True(!r.Send(66, p66), "disconnected");
            r.L.Call(() => r.Peer.Connect(GameAddr, "1"));
            A.True(!r.Send(66, p66), "connecting");
            r.Link.Complete(r.Peer, LinkResult.Success);
            r.Pump();
            A.True(r.Send(66, p66), "connected");
            A.True(!r.Send(66, null), "null params");
            A.True(!r.Send(82, Room(4, 1, null)), "82 outside a room");
            A.True(!r.Send(42, Room(4, 1, null)), "unknown op");
        }

        [Test]
        static void Op66RoundTrip()
        {
            var r = new Rig();
            r.Connected();
            A.True(r.Send(66, new Dictionary<byte, object> { { 100, (byte)2 }, { 61, (short)9 }, { 103, new byte[] { 7 } } }), "send");
            FakeLink.RpcCall c = r.Link.LastRpc("uber_op66");
            Dictionary<string, object> req = NakamaJson.ParseObject(c.Payload);
            A.Eq(2L, (long)req["m"], "m");
            A.Eq(9L, (long)req["i"], "i");
            A.Eq("Bw==", (string)req["a"], "a");
            A.Eq("game", (string)req["p"], "p");
            A.Eq(1, r.Peer.QueuedOutgoingCommands, "in flight");

            c.Done("{\"rc\":0,\"i\":9,\"d\":\"AQI=\"}", null);
            A.Eq(0, r.Peer.QueuedOutgoingCommands, "done");
            r.Pump();
            OperationResponse resp = r.L.Responses[0];
            A.Eq((byte)66, resp.OperationCode, "op");
            A.Eq((short)0, resp.ReturnCode, "rc");
            A.Eq((short)9, (short)resp.Parameters[61], "61 short");
            A.Bytes(new byte[] { 1, 2 }, (byte[])resp.Parameters[42], "42 bytes");
        }

        [Test]
        static void Op66Failures()
        {
            var r = new Rig();
            r.Connected();
            r.Send(66, new Dictionary<byte, object> { { 100, (byte)22 }, { 61, (short)3 }, { 103, new byte[0] } });
            r.Link.LastRpc("uber_op66").Done("{\"rc\":1,\"i\":3,\"msg\":\"not allowed\"}", null);
            r.Send(66, new Dictionary<byte, object> { { 100, (byte)2 }, { 61, (short)4 }, { 103, new byte[0] } });
            r.Link.LastRpc("uber_op66").Done(null, "WebSocketException: session required");
            r.Send(66, new Dictionary<byte, object> { { 100, (byte)2 }, { 61, (short)5 }, { 103, new byte[0] } });
            r.Link.LastRpc("uber_op66").Done("<html>", null);
            r.Pump();
            A.Eq(3, r.L.Responses.Count, "3 responses");
            A.Eq((short)1, r.L.Responses[0].ReturnCode, "server rc 1");
            A.Eq("not allowed", r.L.Responses[0].DebugMessage, "server msg");
            A.True(!r.L.Responses[0].Parameters.ContainsKey(42), "no data");
            A.Eq((short)1, r.L.Responses[1].ReturnCode, "transport rc 1");
            A.True(r.L.Responses[1].DebugMessage.Contains("session required"), "transport msg");
            A.Eq((short)4, (short)r.L.Responses[1].Parameters[61], "invoc kept");
            A.Eq((short)1, r.L.Responses[2].ReturnCode, "bad json rc 1");
        }

        [Test]
        static void JoinFlowAndRoomTraffic()
        {
            var r = new Rig();
            r.Connected();
            A.True(r.Send(88, JoinParams()), "send 88");
            A.Eq("{\"meta\":\"ZwEC\"}", r.Link.LastRpc("uber_room_join").Payload, "meta b64");
            A.True(!r.Send(82, Room(2, 3, null)), "no room traffic while joining");
            r.Link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"m1\",\"number\":88}", null);
            A.Eq(0, r.Pump(), "no callback until ack");
            A.Eq("m1", r.Link.Joins[0].Key, "JoinMatchAsync m1");
            A.Eq(NakamaPeer.KindComm, r.Peer.Kind, "room 88 -> comm");

            r.Link.Deliver("m1", 0, new byte[] { 4, 0, 1 });
            r.Link.Deliver("m1", 88, Ack88);
            r.Link.Joins[0].Value(null);
            r.Pump();
            A.Eq(0, r.L.Events.Count, "events before ack dropped");
            OperationResponse resp = r.L.Responses[0];
            A.Eq((byte)88, resp.OperationCode, "88");
            A.Eq((short)0, resp.ReturnCode, "rc 0");
            A.Eq(3, (int)resp.Parameters[9], "actor 3");
            A.Eq(12, ((byte[])resp.Parameters[4]).Length, "room id");
            A.Eq(0x01020304L, (long)resp.Parameters[201], "ticks");

            A.True(r.Send(82, Room(2, 3, new byte[] { 6, 1, 0, 0, 0 })), "82");
            A.True(r.Send(83, Room(101, 86, new byte[] { 9 })), "83");
            var p80 = Room(4, 30, new byte[0]);
            p80[102] = 5;
            A.True(r.Send(80, p80), "80");
            A.Eq(3, r.Link.Sent.Count, "3 sends");
            A.Eq("m1", r.Link.Sent[0].MatchId, "match");
            A.Eq(82L, r.Link.Sent[0].Op, "op 82");
            A.Bytes(new byte[] { 2, 0, 3, 6, 1, 0, 0, 0 }, r.Link.Sent[0].Data, "82 bytes");
            A.Eq(83L, r.Link.Sent[1].Op, "op 83");
            A.Bytes(new byte[] { 4, 0, 30, 5, 0, 0, 0 }, r.Link.Sent[2].Data, "80 bytes");
            A.True(r.Peer.BytesOut > 0, "bytes out counted");
        }

        [Test]
        static void AckBeforeJoinTaskCompletes()
        {
            var r = new Rig();
            r.Connected();
            r.Send(88, JoinParams());
            r.Link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"m1\",\"number\":101}", null);
            r.Pump();
            r.Link.Deliver("m1", 88, Ack88);
            r.Pump();
            A.Eq(1, r.L.Responses.Count, "joined on ack");
            r.Link.Joins[0].Value(null);
            A.Eq(0, r.Pump(), "late success is silent");
            A.Eq("m1", r.Peer.MatchId, "still in m1");
        }

        [Test]
        static void JoinRejections()
        {
            var cases = new[] { "rc=1", "rc=2", "rc=3", "rc=4", "match not found" };
            var want = new short[] { 1, 2, 3, 4, 5 };
            for (int i = 0; i < cases.Length; i++)
            {
                var r = new Rig();
                r.Connected();
                r.Send(88, JoinParams());
                r.Link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"m1\",\"number\":101}", null);
                r.Pump();
                r.Link.Joins[0].Value(cases[i]);
                r.Pump();
                A.Eq(want[i], r.L.Responses[0].ReturnCode, cases[i]);
                A.True(r.Peer.MatchId == null, "no match after reject");
                A.True(!r.Send(82, Room(2, 3, null)), "no room traffic");
            }
        }

        [Test]
        static void JoinRpcFailures()
        {
            var r = new Rig();
            r.Connected();
            r.Send(88, JoinParams());
            r.Link.LastRpc("uber_room_join").Done("{\"rc\":1,\"number\":105,\"msg\":\"Game doesn't exist anymore!\"}", null);
            r.Pump();
            r.Send(88, JoinParams());
            r.Link.LastRpc("uber_room_join").Done(null, "WebSocketException: bad meta: x");
            r.Pump();
            A.Eq((short)1, r.L.Responses[0].ReturnCode, "room gone rc 1");
            A.Eq("Game doesn't exist anymore!", r.L.Responses[0].DebugMessage, "msg");
            A.Eq((short)5, r.L.Responses[1].ReturnCode, "transport rc 5");
            A.Eq(0, r.Link.Joins.Count, "no JoinMatchAsync");
        }

        [Test]
        static void SecondJoinRefusedWithRc4()
        {
            var r = new Rig();
            r.Connected();
            r.Joined();
            A.True(r.Send(88, JoinParams()), "accepted for response");
            r.Pump();
            A.Eq((short)4, r.L.Responses[1].ReturnCode, "already in room");
        }

        [Test]
        static void BadAckIsRc5()
        {
            var r = new Rig();
            r.Connected();
            r.Send(88, JoinParams());
            r.Link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"m1\",\"number\":101}", null);
            r.Pump();
            r.Link.Deliver("m1", 88, new byte[24]);
            r.Pump();
            A.Eq((short)5, r.L.Responses[0].ReturnCode, "bad ack");
        }

        [Test]
        static void EventsAfterJoin()
        {
            var r = new Rig();
            r.Connected();
            r.Joined();
            r.Link.Deliver("m1", 0, new byte[] { 1, 0, 1, 6, 2, 0, 0, 0 });
            r.Link.Deliver("m1", 3, new byte[] { 0x6f, 0, 0 });
            r.Link.Deliver("m1", 4, new byte[] { 0x6f, 0, 0 });
            r.Link.Deliver("m1", 5, new byte[] { 0x33, 0, 0 });
            r.Link.Deliver("m1", 77, new byte[] { 1 });
            r.Link.Deliver("m1", 0, new byte[] { 1 });
            r.Peer.OnMatchData("other", 0, new byte[] { 1, 0, 1 });
            A.Eq(6, r.Pump(), "6 items (other match ignored)");
            A.Eq(4, r.L.Events.Count, "4 events");
            EventData e0 = r.L.Events[0];
            A.Eq((byte)0, e0.Code, "ev 0");
            A.Eq((short)1, (short)e0.Parameters[101], "netId");
            A.Eq((byte)1, (byte)e0.Parameters[100], "method");
            A.Bytes(new byte[] { 6, 2, 0, 0, 0 }, (byte[])e0.Parameters[103], "args");
            A.Eq((byte)3, r.L.Events[1].Code, "ev 3");
            A.True(((Dictionary<byte, object>)r.L.Events[1].Parameters[42]).ContainsKey(122), "3 -> 122");
            A.True(((Dictionary<byte, object>)r.L.Events[3].Parameters[42]).ContainsKey(123), "5 -> 123");
            A.Eq(2, r.L.Debug.Count, "unknown op + short event logged");
            A.True(r.Peer.BytesIn >= 8, "bytes in");
        }

        [Test]
        static void LeaveFlows()
        {
            var r = new Rig();
            r.Connected();
            A.True(r.Send(89, new Dictionary<byte, object> { { 120, new byte[12] } }), "leave without room");
            A.Eq(1, r.Pump(), "response 89 posted");
            A.Eq((byte)89, r.L.Responses[0].OperationCode, "89");

            r.Joined();
            A.True(r.Send(89, new Dictionary<byte, object> { { 120, new byte[12] } }), "leave");
            A.Eq("m1", r.Link.Leaves[0].Key, "LeaveMatchAsync");
            A.True(r.Peer.MatchId == null, "match dropped at once");
            A.True(!r.Send(82, Room(2, 3, null)), "no traffic after leave");
            A.Eq(0, r.Pump(), "wait for leave");
            r.Link.Leaves[0].Value("WebSocketException: x");
            r.Pump();
            OperationResponse last = r.L.Responses[r.L.Responses.Count - 1];
            A.Eq((byte)89, last.OperationCode, "89 even on error");
        }

        [Test]
        static void DisconnectIsDeferredAndLeavesMatch()
        {
            var r = new Rig();
            r.Connected();
            r.Joined();
            r.Link.Deliver("m1", 0, new byte[] { 1, 0, 1 });
            FakeLink.RpcCall pending = null;
            r.Send(66, new Dictionary<byte, object> { { 100, (byte)2 }, { 61, (short)1 }, { 103, new byte[0] } });
            pending = r.Link.LastRpc("uber_op66");

            r.L.Call(() => r.Peer.Disconnect());
            A.Eq(PeerStateValue.Disconnecting, r.Peer.PeerState, "Disconnecting (> 0) until status");
            A.Eq("m1", r.Link.Leaves[0].Key, "left the match (shared socket stays)");
            A.True(!r.Link.Attached.Contains(r.Peer), "detached");
            pending.Done("{\"rc\":0,\"i\":1}", null);

            int statuses = r.L.Statuses.Count;
            A.Eq(1, r.Pump(), "only the status");
            A.Eq(StatusCode.Disconnect, r.L.Statuses[statuses], "Disconnect");
            A.Eq(PeerStateValue.Disconnecting, r.L.StateAtStatus[statuses], "state during callback");
            A.Eq(PeerStateValue.Disconnected, r.Peer.PeerState, "after");
            A.Eq(0, r.L.Events.Count, "stale event dropped");
            r.L.Call(() => r.Peer.Disconnect());
            A.Eq(0, r.Pump(), "second Disconnect no-op");
        }

        [Test]
        static void DisconnectWhileConnecting()
        {
            var r = new Rig();
            r.L.Call(() => r.Peer.Connect(GameAddr, "1"));
            Action<LinkResult> late = r.Link.Waiting[r.Peer];
            r.L.Call(() => r.Peer.Disconnect());
            late(LinkResult.Success);
            r.Pump();
            A.Eq(1, r.L.Statuses.Count, "one status");
            A.Eq(StatusCode.Disconnect, r.L.Statuses[0], "Disconnect, late Connect ignored");
            A.Eq(PeerStateValue.Disconnected, r.Peer.PeerState, "disconnected");
        }

        [Test]
        static void ReconnectAfterDisconnect()
        {
            var r = new Rig();
            r.Connected();
            r.Peer.Disconnect();
            r.Pump();
            r.Connected();
            A.Eq(2, r.Link.AppNames.Count, "attached twice");
            A.Eq(StatusCode.Connect, r.L.Statuses[r.L.Statuses.Count - 1], "connected again");
        }

        [Test]
        static void KickOnlyHitsThatPeer()
        {
            var a = new Rig();
            var b = new Rig();
            b.Link = a.Link;
            b.Peer = new NakamaPeer(a.Link);
            b.Peer.Listener = b.L;
            b.L.Peer = b.Peer;
            a.Connected();
            b.Connected();
            a.Joined("m1");
            b.Joined("m2");

            a.Peer.OnMatchLeft("m1");
            a.Pump();
            b.Pump();
            A.Eq(StatusCode.DisconnectByServerLogic, a.L.Statuses[a.L.Statuses.Count - 1], "kicked");
            A.Eq(PeerStateValue.Disconnected, a.Peer.PeerState, "a down");
            A.Eq(PeerStateValue.Connected, b.Peer.PeerState, "b untouched");
            A.True(!a.Link.Attached.Contains(a.Peer) && a.Link.Attached.Contains(b.Peer), "only a detached");

            b.Link.Deliver("m2", 0, new byte[] { 1, 0, 1 });
            A.Eq(1, b.Pump(), "b still receives");
            b.Peer.OnMatchLeft("m1");
            A.Eq(0, b.Pump(), "leave of another match ignored");
        }

        static Rig Twin(Rig a)
        {
            var b = new Rig();
            b.Link = a.Link;
            b.Peer = new NakamaPeer(a.Link);
            b.Peer.Listener = b.L;
            b.L.Peer = b.Peer;
            return b;
        }

        [Test]
        static void KickNoticeOp89OnlyHitsThatPeer()
        {
            var a = new Rig();
            var b = Twin(a);
            a.Connected();
            b.Connected();
            a.Joined("m1");
            b.Joined("m2");

            a.Link.Deliver("m1", 89, System.Text.Encoding.UTF8.GetBytes("no data for 10 s"));
            a.Pump();
            b.Pump();
            A.Eq(StatusCode.DisconnectByServerLogic, a.L.Statuses[a.L.Statuses.Count - 1], "op 89 -> DisconnectByServerLogic");
            A.Eq(PeerStateValue.Disconnected, a.Peer.PeerState, "a down");
            A.True(a.L.Debug.Exists(d => d.Contains("kicked: no data for 10 s")), "reason logged");
            A.True(!a.Link.Attached.Contains(a.Peer), "a detached");
            A.Eq(PeerStateValue.Connected, b.Peer.PeerState, "b untouched");
            A.True(a.Link.Attached.Contains(b.Peer), "b attached");
            A.True(!b.L.Statuses.Contains(StatusCode.DisconnectByServerLogic), "b no status");

            b.Peer.OnMatchData("m1", 89, new byte[0]);
            A.Eq(0, b.Pump(), "op 89 of another match ignored");
            A.Eq(PeerStateValue.Connected, b.Peer.PeerState, "b still up");

            a.Peer.OnMatchLeft("m1");
            A.Eq(0, a.Pump(), "later MatchLeave is a no-op");

            b.Link.Deliver("m2", 89, null);
            b.Pump();
            A.Eq(StatusCode.DisconnectByServerLogic, b.L.Statuses[b.L.Statuses.Count - 1], "empty reason still kicks");
        }

        [Test]
        static void KickNoticeWhileJoining()
        {
            var r = new Rig();
            r.Connected();
            r.Send(88, JoinParams());
            r.Link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"m1\",\"number\":101}", null);
            r.Pump();
            r.Link.Deliver("m1", 89, new byte[] { 0x78 });
            r.Pump();
            A.Eq(StatusCode.DisconnectByServerLogic, r.L.Statuses[r.L.Statuses.Count - 1], "kick before ack");
            A.Eq(PeerStateValue.Disconnected, r.Peer.PeerState, "down");
        }

        [Test]
        static void InGameRoomOnlyForJoinedGameRooms()
        {
            var r = new Rig();
            A.True(!r.Peer.InGameRoom("m1"), "not connected");
            r.Connected();
            r.Send(88, JoinParams());
            r.Link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"m1\",\"number\":101}", null);
            r.Pump();
            A.True(!r.Peer.InGameRoom("m1"), "not while joining");
            r.Link.Deliver("m1", 88, Ack88);
            r.Pump();
            A.True(r.Peer.InGameRoom("m1"), "game room 101");
            A.True(!r.Peer.InGameRoom("m2") && !r.Peer.InGameRoom(null), "other match");
            r.Send(89, new Dictionary<byte, object>());
            A.True(!r.Peer.InGameRoom("m1"), "after leave");

            var lobby = new Rig();
            lobby.Connected();
            lobby.Joined("lob", 66);
            A.True(!lobby.Peer.InGameRoom("lob"), "lobby 66");

            var comm = new Rig();
            comm.Connected(CommAddr);
            comm.Joined("comm", 88);
            A.True(!comm.Peer.InGameRoom("comm"), "comm 88");
        }

        [Test]
        static void KindByJoinNotAddress()
        {
            var c = new Rig();
            c.Connected(CommAddr);
            A.Eq(NakamaPeer.KindGame, c.Peer.Kind, "pre-join = game (comm shares the node address)");
            c.Send(66, new Dictionary<byte, object> { { 100, (byte)1 }, { 61, (short)1 }, { 103, new byte[0] } });
            A.Eq("game", (string)NakamaJson.ParseObject(c.Link.LastRpc("uber_op66").Payload)["p"], "pre-join probe op 66 = game");
            c.Joined("comm", 88);
            A.Eq(NakamaPeer.KindComm, c.Peer.Kind, "join 88 -> comm");
            c.Send(66, new Dictionary<byte, object> { { 100, (byte)21 }, { 61, (short)2 }, { 103, new byte[0] } });
            A.Eq("comm", (string)NakamaJson.ParseObject(c.Link.LastRpc("uber_op66").Payload)["p"], "comm op 66 after join");

            var l = new Rig();
            l.Connected("10.20.30.40:7450");
            A.Eq(NakamaPeer.KindGame, l.Peer.Kind, "any address: game");
            l.Joined("lob", 66);
            A.Eq(NakamaPeer.KindLobby, l.Peer.Kind, "join 66 -> lobby");

            var g = new Rig();
            g.Connected();
            g.Joined("m9", 101);
            A.Eq(NakamaPeer.KindGame, g.Peer.Kind, "join 101 -> game");
        }

        [Test]
        static void RoundTripTimeClamp()
        {
            var r = new Rig();
            r.Link.RoundTripTime = 0;
            A.Eq(0, r.Peer.RoundTripTime, "disconnected: raw 0");
            r.Connected();
            A.Eq(1, r.Peer.RoundTripTime, "connected: >= 1 (GetBestServer skips 0)");
            r.Link.RoundTripTime = 42;
            A.Eq(42, r.Peer.RoundTripTime, "real rtt");
        }

        [Test]
        static void SocketCloseMapping()
        {
            var up = new Rig();
            up.Connected();
            up.Peer.OnLinkClosed(true, "server");
            up.Pump();
            A.Eq(StatusCode.DisconnectByServer, up.L.Statuses[1], "by server");

            var mine = new Rig();
            mine.Connected();
            mine.Peer.OnLinkClosed(false, "quit");
            mine.Pump();
            A.Eq(StatusCode.Disconnect, mine.L.Statuses[1], "by client");

            var connecting = new Rig();
            connecting.L.Call(() => connecting.Peer.Connect(GameAddr, "1"));
            connecting.Peer.OnLinkClosed(true, "x");
            connecting.Pump();
            A.Eq(StatusCode.ExceptionOnConnect, connecting.L.Statuses[0], "while connecting");
        }

        [Test]
        static void TerminalStatusDropsLeftovers()
        {
            var r = new Rig();
            r.Connected();
            r.Joined();
            r.Peer.OnLinkClosed(true, "x");
            r.Link.Deliver("m1", 0, new byte[] { 1, 0, 1 });
            A.True(r.Peer.DispatchIncomingCommands(), "status");
            A.True(!r.Peer.DispatchIncomingCommands(), "event after terminal dropped");
            A.Eq(0, r.L.Events.Count, "no event");
        }

        [Test]
        static void ListenerExceptionPropagatesStateStillDown()
        {
            var r = new Rig();
            r.Connected();
            r.L.OnStatus = s => { throw new InvalidOperationException("boom"); };
            r.Peer.OnLinkClosed(true, "x");
            A.Throws<InvalidOperationException>(() => r.Peer.DispatchIncomingCommands(), "propagates (P3)");
            A.Eq(PeerStateValue.Disconnected, r.Peer.PeerState, "still Disconnected");
        }

        [Test]
        static void FramingErrorsNeverCallBackInsideSend()
        {
            var r = new Rig();
            r.Connected();
            r.Joined();
            var bad = new Dictionary<byte, object> { { 101, 4 }, { 100, (byte)1 }, { 103, new byte[0] } };
            A.True(!r.Send(82, bad), "bad netId type refused");
            int before = r.L.Debug.Count;
            A.Eq(1, r.Pump(), "debug item on pump");
            A.True(r.L.Debug.Count == before + 1 && r.L.Debug[before].Contains("op 82"), "logged later");
            A.True(!r.Send(66, new Dictionary<byte, object> { { 100, (byte)1 } }), "66 without 61");
        }

        [Test]
        static void ClockAndCountersFromLink()
        {
            var r = new Rig();
            r.Link.ServerTimeMs = 123456;
            r.Link.RoundTripTime = 40;
            r.Link.RoundTripTimeVariance = 3;
            A.Eq(123456, r.Peer.ServerTimeInMilliSeconds, "server ms");
            A.Eq(40, r.Peer.RoundTripTime, "rtt");
            A.Eq(3, r.Peer.RoundTripTimeVariance, "var");
            r.Peer.FetchServerTimestamp();
            A.Eq(1, r.Link.SyncRequests, "sync now");
            A.True(!r.Peer.SendOutgoingCommands(), "no-op flush");
        }

        [Test]
        static void StopThreadIsFinal()
        {
            var r = new Rig();
            r.Connected();
            r.Joined();
            r.Peer.Disconnect();
            r.Peer.StopThread();
            A.True(!r.Link.Attached.Contains(r.Peer), "detached");
            r.Pump();
            A.True(!r.Peer.Connect(GameAddr, "1"), "no reconnect after StopThread");
        }

        [Test]
        static void OneCallbackPerDispatch()
        {
            var r = new Rig();
            r.Connected();
            r.Joined();
            for (int i = 0; i < 3; i++)
                r.Link.Deliver("m1", 0, new byte[] { 1, 0, (byte)i });
            A.Eq(3, r.Peer.QueuedIncomingCommands, "queued");
            for (int i = 0; i < 3; i++)
            {
                A.True(r.Peer.DispatchIncomingCommands(), "item " + i);
                A.Eq(i + 1, r.L.Events.Count, "one event per call");
            }
            A.True(!r.Peer.DispatchIncomingCommands(), "empty -> false");
        }
    }
}
