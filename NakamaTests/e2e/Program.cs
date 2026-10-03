using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cmune.Realtime.Photon.Client.Transport;
using Nakama;
using UberStrike.Realtime.NakamaAdapter;

namespace ClientE2E
{
    // Live run: NakamaPeer + NakamaLink (the code the game ships) over nakama-dotnet's stdlib socket
    // against Nakama 3.41.0 + the uber Go module (UBER_DEV_AUTH=true). Single thread, like Unity's main thread.
    static class Program
    {
        // golden.json (real SDK serializer): RoomMetaData comm 88 / lobby 66, GameMetaData new room, peer spec args
        const string RoomComm = "280e540068006500200043006f006d006d00530065007200760065007200000000000107580000007f000001bf13";
        const string RoomLobby = "280954006800650020004c006f006200620079000270007700640302010742000000c0a801142314";
        const string GameCreate = "67084e0065007700200052006f006f006d00067300650063007200650074000800000107000000000000000000000d002c0100000000000064000000000000ff00000000";
        const string PeerSpec = "01010c030031002e003700";

        static string _url = "http://127.0.0.1:7350";
        static string _key = "defaultkey";
        static string _report;
        static readonly StringBuilder Report = new StringBuilder();
        static int _fails;
        static int _checks;

        static int Main(string[] args)
        {
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                if (args[i] == "--url") _url = args[i + 1];
                else if (args[i] == "--key") _key = args[i + 1];
                else if (args[i] == "--report") _report = args[i + 1];
            }

            var ctx = new Pump();
            SynchronizationContext.SetSynchronizationContext(ctx);
            Task run = Run();
            ctx.RunUntil(run, TimeSpan.FromSeconds(240));

            if (!run.IsCompleted)
                Fail("scenario", "timed out");
            else if (run.IsFaulted)
                Fail("scenario", run.Exception.InnerException.ToString());

            Line("");
            Line(string.Format("{0} checks, {1} failed", _checks, _fails));
            if (_report != null)
                File.WriteAllText(_report, Report.ToString());
            return _fails == 0 ? 0 : 1;
        }

        static async Task Run()
        {
            Line("client e2e against " + _url + " (Nakama.dll " + typeof(Client).Assembly.GetName().Version + ")");
            int cmidA = 700000 + (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond % 90000);
            int cmidB = cmidA + 100000;

            var pa = new Platform(new NakamaIdentity { Cmid = cmidA, Access = 0, Name = "e2e A" });
            var pb = new Platform(new NakamaIdentity { Cmid = cmidB, Access = 0, Name = "e2e B" });
            var la = new NakamaLink(Config(), pa);
            var lb = new NakamaLink(Config(), pb);
            pa.Link = la;
            pb.Link = lb;

            // S1: connect, peer spec, server load, clock
            var comm = new TestPeer("A comm", la);
            var sw = Stopwatch.StartNew();
            Check("connect: Connect status", comm.Connect("127.0.0.1:20088", cmidA) && await comm.WaitStatus(StatusCode.Connect, 15000), comm.LastStatus);
            Line("  connect (auth + socket + first uber_time): " + sw.ElapsedMilliseconds + " ms");
            Check("connect: clock synced", la.Clock.Synced, "samples " + la.Clock.Samples);

            var probe = new TestPeer("A probe", la);
            sw.Restart();
            Check("probe: shares socket", probe.Connect("127.0.0.1:20000", 0) && await probe.WaitStatus(StatusCode.Connect, 3000), probe.LastStatus);
            Line("  second peer on shared socket: " + sw.ElapsedMilliseconds + " ms");

            OperationResponse spec = await probe.Op66(1, Hex(PeerSpec));
            Check("op66/1 PeerSpecification '1.7' rc 0", spec != null && spec.ReturnCode == 0, Describe(spec));

            OperationResponse load = await probe.Op66(2, new byte[0]);
            byte[] loadData = load != null && load.Parameters.ContainsKey(42) ? (byte[])load.Parameters[42] : null;
            Check("op66/2 QueryServerLoad -> tagged ServerLoadData", load != null && load.ReturnCode == 0 && loadData != null && loadData.Length > 0 && loadData[0] == 0x2f, Describe(load) + " " + Hex(loadData));

            OperationResponse geo = await probe.Op66(4, new byte[0]);
            Check("op66/4 GeoLocalization -> rc 1 not supported", geo != null && geo.ReturnCode == 1, Describe(geo));

            // S2 plumbing: comm room, class registration, lobby
            OperationResponse jc = await comm.Join(Hex(RoomComm));
            int actorA = jc != null && jc.ReturnCode == 0 ? (int)jc.Parameters[9] : -1;
            Check("op88 comm 88: joined, actor > 0", actorA > 0, Describe(jc));
            byte[] roomId = jc != null && jc.Parameters.ContainsKey(4) ? (byte[])jc.Parameters[4] : null;
            Check("op88 comm: CmuneRoomID 12 B, number 88", roomId != null && roomId.Length == 12 && NakamaFraming.ReadInt32(roomId, 2) == 88, Hex(roomId));
            Check("op88 comm: 201 long ticks on server clock", jc != null && jc.Parameters[201] is long, "");

            byte[] reg = Tagged(actorA, 7, (short)4);
            EventData recv = await comm.SendAndWaitEvent(82, 2, 3, reg, e => (short)e.Parameters[101] == 1 && (byte)e.Parameters[100] == 1, 5000);
            Check("op82 RegisterStaticNetworkClass(comm 4) -> event 0 RecieveNetworkID", recv != null && Hex((byte[])recv.Parameters[103]) == "0607000000040400", recv == null ? "no event" : Hex((byte[])recv.Parameters[103]));

            byte[] spoof = Tagged(actorA + 50, 8, (short)4);
            EventData none = await comm.SendAndWaitEvent(82, 2, 3, spoof, e => (short)e.Parameters[101] == 1, 1500);
            Check("op82 register with spoofed actor id -> ignored (Q5)", none == null, "");

            var lobby = new TestPeer("A lobby", la);
            Check("lobby: connect", lobby.Connect("127.0.0.1:20000", cmidA) && await lobby.WaitStatus(StatusCode.Connect, 3000), lobby.LastStatus);
            OperationResponse jl = await lobby.Join(Hex(RoomLobby));
            Check("op88 lobby 66: joined", jl != null && jl.ReturnCode == 0 && lobby.Peer.Kind == NakamaPeer.KindLobby, Describe(jl));

            // S3 plumbing: create a game, second user finds it via RoomRequest and joins, relays
            var gameA = new TestPeer("A game", la);
            Check("game A: connect", gameA.Connect("127.0.0.1:20000", cmidA) && await gameA.WaitStatus(StatusCode.Connect, 3000), gameA.LastStatus);
            OperationResponse jg = await gameA.Join(Hex(GameCreate));
            int number = jg != null && jg.ReturnCode == 0 ? NakamaFraming.ReadInt32((byte[])jg.Parameters[4], 2) : -1;
            int gameActorA = jg != null && jg.ReturnCode == 0 ? (int)jg.Parameters[9] : -1;
            Check("op88 room 0 creates a game (number >= 101)", number >= 101, Describe(jg) + " number " + number);

            var gameB = new TestPeer("B game", lb);
            Check("game B: second user connects", gameB.Connect("127.0.0.1:20000", cmidB) && await gameB.WaitStatus(StatusCode.Connect, 15000), gameB.LastStatus);
            OperationResponse rr = await gameB.Op66(21, Tagged(number));
            byte[] meta = rr != null && rr.Parameters.ContainsKey(42) ? (byte[])rr.Parameters[42] : null;
            Check("op66/21 RoomRequest -> tagged GameMetaData", rr != null && rr.ReturnCode == 0 && meta != null && meta[0] == 0x67, Describe(rr));

            OperationResponse jb = await gameB.Join(meta ?? new byte[] { 0 });
            int actorB = jb != null && jb.ReturnCode == 0 ? (int)jb.Parameters[9] : -1;
            Check("B joins A's game as a new actor", actorB > 0 && actorB != gameActorA, Describe(jb) + " A=" + gameActorA + " B=" + actorB);
            Check("B sees same room number", jb != null && jb.ReturnCode == 0 && NakamaFraming.ReadInt32((byte[])jb.Parameters[4], 2) == number, "");

            var other = new TestPeer("A game2", la);
            Check("game2: connect", other.Connect("127.0.0.1:20000", cmidA) && await other.WaitStatus(StatusCode.Connect, 3000), other.LastStatus);
            OperationResponse j2 = await other.Join(Hex(GameCreate));
            Check("join a 2nd game while in one -> rc 4 (MatchJoinAttempt reason rc=4)", j2 != null && j2.ReturnCode == 4, Describe(j2));
            other.Peer.Disconnect();

            // relays (op 83 bullet, op 80 knockback) + one-way-ish latency A -> server -> B
            var relay = new List<double>();
            for (int i = 0; i < 30; i++)
            {
                byte[] a = Tagged(i);
                long t0 = Stopwatch.GetTimestamp();
                gameA.Send(83, 100, 89, a);
                EventData got = await gameB.WaitEvent(e => (short)e.Parameters[101] == 100 && (byte)e.Parameters[100] == 89 && Hex((byte[])e.Parameters[103]) == Hex(a), 3000);
                if (got == null)
                    break;
                relay.Add(Ms(t0));
            }
            Check("op83 SingleBulletFire relayed A -> B (30/30)", relay.Count == 30, relay.Count + "/30");

            gameA.Send(80, 100, 68, Tagged(1), actorB);
            EventData kb = await gameB.WaitEvent(e => (byte)e.Parameters[100] == 68, 3000);
            Check("op80 knockback to B's actor", kb != null, "");

            gameA.Send(83, 100, 68, Tagged(1));
            EventData blocked = await gameB.WaitEvent(e => (byte)e.Parameters[100] == 68, 1500);
            Check("op83 PlayerHit relay blocked by whitelist (Q5)", blocked == null, "");

            // latency (Q7)
            var rpc = new List<double>();
            for (int i = 0; i < 30; i++)
            {
                long t0 = Stopwatch.GetTimestamp();
                OperationResponse r = await probe.Op66(2, new byte[0]);
                if (r == null || r.ReturnCode != 0)
                    break;
                rpc.Add(Ms(t0));
            }
            Check("op66 round trips 30/30", rpc.Count == 30, rpc.Count + "/30");
            Line("  op66 rtt ms " + Stats(rpc));
            Line("  relay A->srv->B ms " + Stats(relay));
            Line("  uber_time rtt " + la.RoundTripTime + " ms, variance " + la.RoundTripTimeVariance + " ms");

            await Wait(2500);
            int skew = Math.Abs(la.ServerTimeMs - lb.ServerTimeMs);
            Check("two sockets agree on ServerTimeTicks within 20 ms", skew <= 20, skew + " ms");

            // leave + disconnect
            OperationResponse lv = await gameB.Leave();
            Check("op89 leave -> response 89", lv != null, "");
            gameB.Peer.Disconnect();
            Check("Disconnect -> StatusCode.Disconnect", await gameB.WaitStatus(StatusCode.Disconnect, 3000) && gameB.Peer.PeerState == PeerStateValue.Disconnected, gameB.LastStatus);

            // bad server key -> auth rejected -> ExceptionOnConnect
            var pbad = new Platform(new NakamaIdentity { Cmid = cmidA + 7, Access = 0, Name = "bad" });
            var cbad = Config();
            cbad.ServerKey = "wrong-key";
            var lbad = new NakamaLink(cbad, pbad);
            pbad.Link = lbad;
            var bad = new TestPeer("bad key", lbad);
            Check("wrong server key -> ExceptionOnConnect", bad.Connect("127.0.0.1:20000", cmidA + 7) && await bad.WaitStatus(StatusCode.ExceptionOnConnect, 15000), bad.LastStatus);

            la.Shutdown();
            lb.Shutdown();
            await Wait(200);
            Check("shutdown -> attached peers see Disconnect", comm.Statuses.Contains(StatusCode.Disconnect) || comm.Peer.PeerState == PeerStateValue.Disconnected, comm.LastStatus);
        }

        static NakamaConfig Config()
        {
            NakamaConfig c = NakamaConfig.Load(null, new[] { "-nakama", _url, "-nakamakey", _key, "-nakamadev" });
            return c;
        }

        // peer + recording listener, pumped by Wait
        sealed class TestPeer : INetworkPeerListener
        {
            public readonly string Name;
            public readonly NakamaPeer Peer_;
            public readonly NakamaLink Link;
            public readonly List<StatusCode> Statuses = new List<StatusCode>();
            readonly List<OperationResponse> _responses = new List<OperationResponse>();
            readonly List<EventData> _events = new List<EventData>();
            short _invoc = 100;

            public TestPeer(string name, NakamaLink link)
            {
                Name = name;
                Link = link;
                Peer_ = new NakamaPeer(link);
                Peer_.Listener = this;
                All.Add(this);
            }

            public NakamaPeer Peer { get { return Peer_; } }
            public string LastStatus { get { return Statuses.Count == 0 ? "no status" : string.Join(",", Statuses); } }

            public bool Connect(string server, int cmid) { return Peer_.Connect(server, cmid.ToString()); }

            public async Task<bool> WaitStatus(StatusCode code, int ms)
            {
                return await Until(() => Statuses.Contains(code), ms);
            }

            public async Task<OperationResponse> Op66(byte method, byte[] args)
            {
                short i = ++_invoc;
                if (!Peer_.SendOperation(66, new Dictionary<byte, object> { { 100, method }, { 61, i }, { 103, args } }, true))
                    return null;
                OperationResponse r = null;
                await Until(() => (r = _responses.FirstOrDefault(x => x.OperationCode == 66 && x.Parameters.ContainsKey(61) && (short)x.Parameters[61] == i)) != null, 5000);
                return r;
            }

            public async Task<OperationResponse> Join(byte[] meta)
            {
                int n = _responses.Count;
                if (!Peer_.SendOperation(88, new Dictionary<byte, object> { { 103, meta }, { 206, 0 }, { 205, 0 } }, true))
                    return null;
                OperationResponse r = null;
                await Until(() => (r = _responses.Skip(n).FirstOrDefault(x => x.OperationCode == 88)) != null, 10000);
                return r;
            }

            public async Task<OperationResponse> Leave()
            {
                int n = _responses.Count;
                if (!Peer_.SendOperation(89, new Dictionary<byte, object> { { 120, new byte[12] } }, true))
                    return null;
                OperationResponse r = null;
                await Until(() => (r = _responses.Skip(n).FirstOrDefault(x => x.OperationCode == 89)) != null, 5000);
                return r;
            }

            public bool Send(byte op, short netId, byte method, byte[] args, int target = 0)
            {
                var p = new Dictionary<byte, object> { { 101, netId }, { 100, method }, { 103, args } };
                if (op == 80)
                    p[102] = target;
                return Peer_.SendOperation(op, p, op != 83);
            }

            public async Task<EventData> SendAndWaitEvent(byte op, short netId, byte method, byte[] args, Func<EventData, bool> match, int ms)
            {
                int n = _events.Count;
                Send(op, netId, method, args);
                EventData e = null;
                await Until(() => (e = _events.Skip(n).FirstOrDefault(match)) != null, ms);
                return e;
            }

            public async Task<EventData> WaitEvent(Func<EventData, bool> match, int ms)
            {
                EventData e = null;
                await Until(() => { e = _events.FirstOrDefault(match); if (e != null) _events.Remove(e); return e != null; }, ms);
                return e;
            }

            public void Pump()
            {
                if (Peer_.PeerState > 0)
                    while (Peer_.DispatchIncomingCommands()) { }
            }

            public void DebugReturn(DebugLevel level, string message) { Line("  [" + Name + "] " + level + " " + message); }
            public void OnOperationResponse(OperationResponse r) { _responses.Add(r); }
            public void OnStatusChanged(StatusCode s) { Statuses.Add(s); }
            public void OnEvent(EventData e) { _events.Add(e); }
        }

        static readonly List<TestPeer> All = new List<TestPeer>();

        static async Task<bool> Until(Func<bool> cond, int ms)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                foreach (TestPeer p in All)
                    p.Pump();
                foreach (Platform pl in Platform.AllPlatforms)
                    pl.Link.Tick();
                if (cond())
                    return true;
                if (sw.ElapsedMilliseconds > ms)
                    return false;
                await Task.Delay(5);
            }
        }

        static Task Wait(int ms)
        {
            return Until(() => false, ms);
        }

        sealed class Platform : INakamaPlatform
        {
            public static readonly List<Platform> AllPlatforms = new List<Platform>();
            readonly NakamaIdentity _id;
            readonly Stopwatch _watch = Stopwatch.StartNew();
            readonly SynchronizationContext _ctx = SynchronizationContext.Current;
            public NakamaLink Link;

            public Platform(NakamaIdentity id)
            {
                _id = id;
                AllPlatforms.Add(this);
            }

            public IClient NewClient(NakamaConfig cfg)
            {
                return new Client(cfg.Scheme, cfg.Host, cfg.Port, cfg.ServerKey) { Timeout = cfg.ConnectTimeoutSec };
            }

            public ISocket NewSocket(IClient client) { return Socket.From(client, new WebSocketStdlibAdapter()); }
            public void ReleaseSocket(ISocket socket) { }
            public void Post(Action action) { _ctx.Post(_ => action(), null); }
            public long NowMs() { return _watch.ElapsedMilliseconds; }
            public NakamaIdentity Identity() { return _id; }
            public string TokenOverride(int cmid) { return null; }
            public string WebToken() { return null; }
            public void SessionReplaced(string message) { Line("  SessionReplaced: " + message); }
            public void Log(string message) { Line("  " + message); }
            public void Warn(string message) { Line("  " + message); }
        }

        // single-thread SynchronizationContext: Unity main thread stand-in
        sealed class Pump : SynchronizationContext
        {
            readonly BlockingCollection<Action> _q = new BlockingCollection<Action>();
            public override void Post(SendOrPostCallback d, object state) { _q.Add(() => d(state)); }
            public override void Send(SendOrPostCallback d, object state) { throw new NotSupportedException(); }

            public void RunUntil(Task t, TimeSpan max)
            {
                var sw = Stopwatch.StartNew();
                while (!t.IsCompleted && sw.Elapsed < max)
                {
                    Action a;
                    if (_q.TryTake(out a, 50))
                        a();
                }
            }
        }

        static void Check(string what, bool ok, string detail)
        {
            _checks++;
            if (!ok)
                _fails++;
            Line((ok ? "PASS " : "FAIL ") + what + (string.IsNullOrEmpty(detail) || ok ? "" : "  [" + detail + "]"));
        }

        static void Fail(string what, string detail)
        {
            Check(what, false, detail);
        }

        static void Line(string s)
        {
            lock (Report)
            {
                Console.WriteLine(s);
                Report.AppendLine(s);
            }
        }

        static string Describe(OperationResponse r)
        {
            return r == null ? "no response" : "op " + r.OperationCode + " rc " + r.ReturnCode + (r.DebugMessage != null ? " '" + r.DebugMessage + "'" : "");
        }

        static double Ms(long t0)
        {
            return (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
        }

        static string Stats(List<double> v)
        {
            if (v.Count == 0)
                return "n/a";
            var s = v.OrderBy(x => x).ToList();
            Func<double, double> q = f => s[Math.Min(s.Count - 1, (int)Math.Ceiling(f * s.Count) - 1)];
            return string.Format("n={0} p50={1:F1} p95={2:F1} max={3:F1}", s.Count, q(0.5), q(0.95), s.Last());
        }

        // tagged args: int -> 06 + LE, short -> 04 + LE
        static byte[] Tagged(params object[] args)
        {
            var b = new List<byte>();
            foreach (object a in args)
            {
                if (a is int)
                {
                    b.Add(6);
                    b.AddRange(BitConverter.GetBytes((int)a));
                }
                else if (a is short)
                {
                    b.Add(4);
                    b.AddRange(BitConverter.GetBytes((short)a));
                }
                else
                    throw new ArgumentException(a.GetType().Name);
            }
            return b.ToArray();
        }

        static byte[] Hex(string h)
        {
            var b = new byte[h.Length / 2];
            for (int i = 0; i < b.Length; i++)
                b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
            return b;
        }

        static string Hex(byte[] b)
        {
            return b == null ? "null" : BitConverter.ToString(b).Replace("-", "").ToLowerInvariant();
        }
    }
}
