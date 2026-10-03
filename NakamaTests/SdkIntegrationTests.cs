#if UNITY_MANAGED
using System;
using System.Collections.Generic;
using System.Linq;
using Cmune.Realtime.Common;
using Cmune.Realtime.Common.IO;
using Cmune.Realtime.Photon.Client;
using Cmune.Realtime.Photon.Client.Transport;
using Cmune.Util;
using UberStrike.Realtime.Common.IO;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // Real rebuilt UberStrike.UnitySdk PhotonPeerListener on top of NakamaPeer + scripted link.
    // Update() is not called (Time.time is a Unity ECall); the pump is the same loop it runs.
    static class SdkIntegrationTests
    {
        sealed class LogChannel : ICmuneDebug
        {
            public readonly List<string> Lines = new List<string>();
            public void Log(int level, string s) { Lines.Add(s); }
        }

        static readonly LogChannel Logs = new LogChannel();

        // golden.json (photon-migration@nakama wire/testdata, produced by the real SDK serializer)
        const string RoomList = "6f0300280e540068006500200043006f006d006d00530065007200760065007200000000000107580000007f000001bf13670f440065006100740068006d006100740063006800200052006f006f006d00001002010107650000007f000001b61c04005802000014000000650003000000015002000000280954006800650020004c006f006200620079000270007700640302010742000000c0a801142314";
        const string RoomIds = "3302000107650000007f000001b61c0107660000007f000001b61c";

        static PhotonPeerListener NewListener(FakeLink link, out NakamaPeer peer)
        {
            RealtimeSerialization.Converter = new UberStrikeByteConverter();
            CmuneDebug.AddDebugChannel(Logs);
            NakamaPeer made = null;
            PeerFactory.Create = () => made = new NakamaPeer(link);
            var l = new PhotonPeerListener();
            peer = made;
            return l;
        }

        static void Pump(NakamaPeer p)
        {
            while (p.DispatchIncomingCommands()) { }
        }

        static PhotonPeerListener Connected(FakeLink link, out NakamaPeer peer, string server = "127.0.0.1:20088")
        {
            PhotonPeerListener l = NewListener(link, out peer);
            A.True(l.Connect(server, 1234), "listener Connect");
            link.Complete(peer, LinkResult.Success);
            Pump(peer);
            return l;
        }

        static void Join(PhotonPeerListener l, FakeLink link, NakamaPeer peer, RoomMetaData room, string match, byte[] ack)
        {
            l.JoinRoom(room, 1234, 0);
            FakeLink.RpcCall rj = link.LastRpc("uber_room_join");
            byte[] meta = Convert.FromBase64String((string)NakamaJson.ParseObject(rj.Payload)["meta"]);
            object sent = RealtimeSerialization.ToObjects(meta)[0];
            A.True(sent is RoomMetaData && ((RoomMetaData)sent).RoomID.Number == room.RoomID.Number, "op 88 key 103 = tagged RoomMetaData");
            rj.Done("{\"rc\":0,\"match\":\"" + match + "\",\"number\":" + room.RoomID.Number + "}", null);
            Pump(peer);
            link.Deliver(match, 88, ack);
            link.Joins[link.Joins.Count - 1].Value(null);
            Pump(peer);
        }

        static byte[] Ack(int actor, int number, string server)
        {
            byte[] b = new byte[25];
            NakamaFraming.WriteInt32(b, 0, actor);
            NakamaFraming.WriteInt32(b, 4, 1);
            NakamaFraming.WriteInt32(b, 8, 777);
            byte[] id = new CmuneRoomID(number, server).GetBytes();
            A.Eq(12, id.Length, "SDK CmuneRoomID.GetBytes is 12 B");
            Buffer.BlockCopy(id, 0, b, 13, 12);
            return b;
        }

        [Test]
        static void PeerFactoryWiresListener()
        {
            NakamaPeer peer;
            PhotonPeerListener l = NewListener(new FakeLink(), out peer);
            A.True(peer != null && peer.Listener == l, "PhotonPeerListener took the factory peer and set Listener");
            PeerFactory.Create = null;
            A.Throws<InvalidOperationException>(() => new PhotonPeerListener(), "unset factory throws (no Photon fallback)");
        }

        [Test]
        static void ConnectSendsPeerSpecification()
        {
            var link = new FakeLink();
            NakamaPeer peer;
            PhotonPeerListener l = NewListener(link, out peer);
            A.True(l.Connect("127.0.0.1:20088", 1234), "Connect");
            A.True(l.IsConnecting, "STATE_CONNECTING while link works");
            A.Eq("1234", link.AppNames[0], "cmid as appName");
            link.Complete(peer, LinkResult.Success);
            Pump(peer);
            A.True(l.IsConnectedToServer, "IsConnectedToServer");

            Dictionary<string, object> o = NakamaJson.ParseObject(link.LastRpc("uber_op66").Payload);
            A.Eq(1L, (long)o["m"], "ApplicationRPC.PeerSpecification");
            object[] args = RealtimeSerialization.ToObjects(Convert.FromBase64String((string)o["a"]));
            A.Eq((byte)1, (byte)args[0], "PeerType.GamePeer");
            A.Eq("1.7", (string)args[1], "Protocol.Version");
        }

        [Test]
        static void Op66CallbackGetsDecodedResult()
        {
            var link = new FakeLink();
            NakamaPeer peer;
            PhotonPeerListener l = Connected(link, out peer);
            link.Rpcs.Clear();

            // SendOperationToServerApplication is internal; drive it the way NetworkMessenger does via reflection
            int got = -1;
            object[] result = null;
            var m = typeof(PhotonPeerListener).GetMethod("SendOperationToServerApplication", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            short invoc = (short)m.Invoke(l, new object[] { new Action<int, object[]>((rc, r) => { got = rc; result = r; }), (byte)2, new object[0] });

            FakeLink.RpcCall c = link.LastRpc("uber_op66");
            byte[] data = RealtimeSerialization.ToBytes(42, "srv").ToArray();
            c.Done("{\"rc\":0,\"i\":" + invoc + ",\"d\":\"" + Convert.ToBase64String(data) + "\"}", null);
            Pump(peer);
            A.Eq(0, got, "callback rc");
            A.Eq(42, (int)result[0], "decoded int");
            A.Eq("srv", (string)result[1], "decoded string");
        }

        [Test]
        static void JoinCommRoomThenRmiAndLeave()
        {
            var link = new FakeLink();
            NakamaPeer peer;
            PhotonPeerListener l = Connected(link, out peer);

            short gotNet = 0;
            byte gotMethod = 0;
            object[] gotArgs = null;
            l.SetMessageCallback((n, mth, a) => { gotNet = n; gotMethod = mth; gotArgs = a; });

            Join(l, link, peer, new RoomMetaData(88, "The CommServer", "127.0.0.1:20088"), "comm.n1", Ack(3, 88, "127.0.0.1:20088"));
            A.True(l.HasJoinedRoom, "HasJoinedRoom");
            A.Eq(3, l.ActorId, "ActorId from ack");
            A.Eq(88, l.CurrentRoom.Number, "CurrentRoom number");
            A.Eq("127.0.0.1:20088", l.CurrentRoom.Server, "CurrentRoom label address");

            byte[] args = RealtimeSerialization.ToBytes(5, "hi").ToArray();
            byte[] ev = new byte[3 + args.Length];
            ev[0] = 4; ev[2] = 25;
            Buffer.BlockCopy(args, 0, ev, 3, args.Length);
            link.Deliver("comm.n1", 0, ev);
            Pump(peer);
            A.Eq((short)4, gotNet, "RMI netId");
            A.Eq((byte)25, gotMethod, "RMI method");
            A.Eq(5, (int)gotArgs[0], "RMI arg 0");
            A.Eq("hi", (string)gotArgs[1], "RMI arg 1");

            // the SDK's own op 82 dictionary -> Go envelope
            Dictionary<byte, object> p82 = OperationFactory.Create(82, (short)2, (byte)3, RealtimeSerialization.ToBytes(3, 0, (short)4).ToArray());
            A.True(peer.SendOperation(82, p82, true), "op 82 from OperationFactory");
            byte[] sent = link.Sent[0].Data;
            A.Bytes(new byte[] { 2, 0, 3 }, sent.Take(3).ToArray(), "envelope header");
            object[] back = RealtimeSerialization.ToObjects(sent.Skip(3).ToArray());
            A.Eq(3, (int)back[0], "args survive");

            Dictionary<byte, object> p80 = OperationFactory.Create(80, 9, (short)4, (byte)30, new byte[0]);
            A.True(peer.SendOperation(80, p80, true), "op 80 from OperationFactory");
            A.Bytes(new byte[] { 4, 0, 30, 9, 0, 0, 0 }, link.Sent[1].Data, "op 80 envelope");

            l.LeaveCurrentRoom();
            A.True(l.IsLeaving, "STATE_LEAVING");
            A.Eq("comm.n1", link.Leaves[0].Key, "LeaveMatchAsync");
            link.Leaves[0].Value(null);
            Pump(peer);
            A.True(!l.HasJoinedRoom, "left");
            A.Eq(0, l.ActorId, "actor cleared");
        }

        [Test]
        static void LobbyListFromGoBytes()
        {
            var link = new FakeLink();
            NakamaPeer peer;
            PhotonPeerListener l = Connected(link, out peer, "127.0.0.1:20000");
            Join(l, link, peer, new RoomMetaData(66, "The Lobby", "127.0.0.1:20000"), "lobby.n1", Ack(1, 66, "127.0.0.1:20066"));
            A.True(l.HasJoinedRoom, "in lobby");

            link.Deliver("lobby.n1", 3, A.FromHex(RoomList));
            Pump(peer);
            List<RoomMetaData> rooms = CmuneNetworkState.AllRooms.ToList();
            A.Eq(3, rooms.Count, "event 3 -> 3 rooms (SDK parsed Go/golden tag 111 bytes)");
            A.True(rooms.Any(r => r.RoomID.Number == 101), "game 101 listed");

            link.Deliver("lobby.n1", 5, A.FromHex(RoomIds));
            Pump(peer);
            List<RoomMetaData> after = CmuneNetworkState.AllRooms.ToList();
            A.True(after.All(r => r.RoomID.Number != 101), "event 5 removed 101 (tag 51 ids)");
            A.True(!Logs.Lines.Any(s => s.Contains("LobbyList")), "no LobbyList parse errors: " + string.Join(" | ", Logs.Lines.Where(s => s.Contains("LobbyList"))));
        }

        [Test]
        static void JoinRejectedBecomesJoinFailedAndDisconnect()
        {
            var link = new FakeLink();
            NakamaPeer peer;
            PhotonPeerListener l = Connected(link, out peer, "127.0.0.1:20101");
            var events = new List<PhotonPeerListener.ConnectionEvent>();
            l.SubscribeToEvents(e => events.Add(e));

            l.JoinRoom(new RoomMetaData(101, "x", "127.0.0.1:20101"), 1234, 0);
            link.LastRpc("uber_room_join").Done("{\"rc\":0,\"match\":\"g.n1\",\"number\":101}", null);
            Pump(peer);
            link.Joins[0].Value("rc=2");
            Pump(peer);
            A.True(!l.HasJoinedRoom, "not joined");
            A.True(l.DoDisconnect, "listener schedules disconnect (legacy)");

            var q = typeof(PhotonPeerListener).GetField("_connectionEvents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(l) as Queue<PhotonPeerListener.ConnectionEvent>;
            PhotonPeerListener.ConnectionEvent failed = q.First(e => e.Type == PhotonPeerListener.ConnectionEventType.JoinFailed);
            A.Eq(2, failed.ErrorCode, "JoinFailed carries rc 2 (GameConnectionManager: 'Game Full')");

            peer.Disconnect();
            Pump(peer);
            A.Eq(PeerStateValue.Disconnected, l.PeerState, "PeerState 0 ends PhotonClient spin");
            A.Eq(NetworkState.STATE_DISCONNECTED, l.ConnectionState, "STATE_DISCONNECTED");
        }

        [Test]
        static void ServerClockAndLatencyFromLink()
        {
            var link = new FakeLink { ServerTimeMs = int.MaxValue, RoundTripTime = 41 };
            NakamaPeer peer;
            PhotonPeerListener l = NewListener(link, out peer);
            A.Eq(int.MaxValue, l.ServerTimeTicks, "ServerTimeTicks = server ms & int.MaxValue");
            A.Eq(20, l.Latency, "Latency = round(RTT/2)");
            l.FetchServerTime();
            l.UpdateServerTime();
            A.Eq(2, link.SyncRequests, "FetchServerTimestamp -> resync");
        }

        [Test]
        static void SocketDropDisconnectsListener()
        {
            var link = new FakeLink();
            NakamaPeer peer;
            PhotonPeerListener l = Connected(link, out peer);
            peer.OnLinkClosed(true, "server");
            Pump(peer);
            A.Eq(NetworkState.STATE_DISCONNECTED, l.ConnectionState, "STATE_DISCONNECTED");
            A.Eq(PeerStateValue.Disconnected, l.PeerState, "PeerState 0");
        }
    }
}
#endif
