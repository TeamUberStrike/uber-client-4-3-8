#if NAKAMA_LINK
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cmune.Realtime.Photon.Client.Transport;
using Nakama;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    static class HostE2ETests
    {
        [Test]
        static void RealSdkHostGameplayRefreshAndNegatives()
        {
            using (var fixture = new HostFixture())
            {
                SynchronizationContext previous = SynchronizationContext.Current;
                var pump = new HostPump();
                SynchronizationContext.SetSynchronizationContext(pump);
                try
                {
                    Task run = Scenario(fixture);
                    var watch = Stopwatch.StartNew();
                    while (!run.IsCompleted && watch.ElapsedMilliseconds < 110000)
                        pump.Step();
                    A.True(run.IsCompleted, "host e2e timeout");
                    run.GetAwaiter().GetResult();
                }
                finally { SynchronizationContext.SetSynchronizationContext(previous); }
            }
        }

        static async Task Scenario(HostFixture fixture)
        {
            Dictionary<string, object> health = await fixture.Health();
            string hostId = NakamaJson.Str(health, "host_id");
            string matchId = NakamaJson.Str(health, "match_id");
            string meta = NakamaJson.Str(health, "meta");
            var rows = new NakamaNodeRows();
            string row = new NakamaJsonWriter().Str("host_id", hostId).Str("match_id", matchId)
                .Str("endpoint", fixture.Endpoint).Str("meta", meta).Bool("unranked", true)
                .Num("players", 0).Num("max_players", 8).End();
            rows.ApplyHosts("{\"hosts\":[" + row + "]}");
            NakamaConfig cfg = NakamaConfig.FromHost(rows.Hosts.Single());
            var platform = new HostPlatform();
            int ticketCalls = 0;
            bool ticketFailure = false;
            var link = new NakamaLink(cfg, platform, id =>
            {
                A.Eq(hostId, id, "real ticket host ID");
                ticketCalls++;
                if (ticketFailure)
                    throw new InvalidOperationException("central unavailable");
                return Task.FromResult(fixture.Sign(id, "Alice", 42, "60s"));
            });
            var peer = new NakamaPeer(link);
            var listener = new RecordingListener { Peer = peer };
            peer.Listener = listener;
            ISocket expirySocket = null;
            try
            {
                Console.WriteLine("E2E real Nakama.dll " + typeof(Client).Assembly.GetName().Version + "; uber-host PID " + fixture.Pid);
                A.True(peer.Connect(cfg.Label, "42"), "host peer connect started");
                await Until(() => listener.Statuses.Contains(StatusCode.Connect), link, peer, 12000);
                A.True(link.Clock.Synced, "real uber_time synced");
                A.Eq(42, link.AuthenticatedCmid, "identity from signed ticket");
                A.Eq("Alice", link.Session.UserId, "ticket user");
                A.True(string.IsNullOrEmpty(link.Session.RefreshToken), "host has no Nakama refresh token");
                long initialExpiry = link.Session.ExpireTime;
                Console.WriteLine("E2E ticket auth + socket + uber_time OK; cmid=42; refresh_token empty");

                A.True(peer.SendOperation(88, new Dictionary<byte, object>
                {
                    { 103, Convert.FromBase64String(meta) }, { 206, 42 }, { 205, 0 },
                }, true), "op 88 sent");
                await Until(() => listener.Responses.Any(r => r.OperationCode == 88), link, peer);
                OperationResponse joined = listener.Responses.Single(r => r.OperationCode == 88);
                A.Eq((short)0, joined.ReturnCode, "real room join");
                int actor = (int)joined.Parameters[9];
                A.True(actor > 0 && peer.InGameRoom(matchId), "real game actor");
                A.Eq(matchId, peer.MatchId, "host match route");
                Console.WriteLine("E2E uber_room_join + match_join + op 88 ack OK; actor=" + actor);

                byte[] registration;
                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)6); writer.Write(actor);
                    writer.Write((byte)6); writer.Write(7);
                    writer.Write((byte)4); writer.Write((short)101);
                    registration = stream.ToArray();
                }
                A.True(peer.SendOperation(82, new Dictionary<byte, object>
                {
                    { 101, (short)2 }, { 100, (byte)3 }, { 103, registration },
                }, true), "game registration op sent");
                await Until(() => listener.Events.Any(e => e.Code == 0 && (short)e.Parameters[101] == 1 && (byte)e.Parameters[100] == 1), link, peer);
                EventData registered = listener.Events.First(e => e.Code == 0 && (short)e.Parameters[101] == 1 && (byte)e.Parameters[100] == 1);
                A.Bytes(new byte[] { 6, 7, 0, 0, 0, 4, 101, 0 }, (byte[])registered.Parameters[103], "real class registration result");
                A.True(platform.States.Any(s => s.MatchId == matchId && s.OpCode == 0), "real SDK ReceivedMatchState");
                Console.WriteLine("E2E game op 82 class registration -> real SDK MatchState op 0 OK");

                var client = new Client(cfg.Scheme, cfg.Host, cfg.Port, cfg.ServerKey) { Timeout = 5 };
                string bad = fixture.Sign(hostId, "Bad", 99, "60s") + "x";
                bool authRejected = false;
                try
                {
                    await client.AuthenticateCustomAsync(bad, null, true, new Dictionary<string, string> { { "token", bad } });
                }
                catch (ApiResponseException e) { authRejected = e.StatusCode == 401; }
                A.True(authRejected, "tampered ticket rejected with 401");
                Console.WriteLine("E2E negative bad ticket -> HTTP 401 OK");

                string bob = fixture.Sign(hostId, "Bob", 43, "60s");
                bool refreshRejected = false;
                try
                {
                    await platform.Socket.RpcAsync("host_refresh", new NakamaJsonWriter().Str("ticket", bob).End());
                }
                catch (Exception e) when (e is ApiResponseException || e is WebSocketException)
                {
                    refreshRejected = e.Message.IndexOf("invalid host ticket", StringComparison.Ordinal) >= 0;
                }
                A.True(refreshRejected && link.IsReady, "Bob cannot refresh Alice");
                Console.WriteLine("E2E negative Bob ticket on Alice session -> refresh rejected OK");

                string shortTicket = fixture.Sign(hostId, "Expiry", 44, "3s");
                ISession shortSession = await client.AuthenticateCustomAsync(shortTicket, null, true, new Dictionary<string, string> { { "token", shortTicket } });
                expirySocket = Nakama.Socket.From(client, new WebSocketStdlibAdapter());
                var closed = new TaskCompletionSource<bool>();
                expirySocket.Closed += reason => closed.TrySetResult(true);
                await expirySocket.ConnectAsync(shortSession, false, 5);
                await expirySocket.JoinMatchAsync(matchId);
                await Until(() => closed.Task.IsCompleted, link, peer, 7000);
                A.True(DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= shortSession.ExpireTime, "not dropped before ticket expiry");
                await Until(async () => NakamaJson.Long(await fixture.Health(), "players", -1) == 1, link, peer);
                Console.WriteLine("E2E negative no refresh -> socket dropped at 3 s ticket expiry; player removed OK");

                await Until(() => DateTimeOffset.UtcNow.ToUnixTimeSeconds() > initialExpiry + 2, link, peer, 70000);
                A.Eq(2, ticketCalls, "one new ticket at 45 seconds");
                A.True(link.IsReady && peer.InGameRoom(matchId), "alive past initial ticket expiry");
                IApiRpc time = await platform.Socket.RpcAsync("uber_time", NakamaFraming.TimeRequest(123));
                long echo;
                int server;
                A.True(NakamaFraming.TryParseTimeReply(time.Payload, out echo, out server) && echo == 123, "host responds after original expiry");
                A.Eq(1L, NakamaJson.Long(await fixture.Health(), "players", -1), "heartbeat preserved game member");
                Console.WriteLine("E2E host_refresh at 45 s OK; socket + game member alive past initial 60 s expiry; 3 s heartbeat OK");

                ticketFailure = true;
                platform.AdvanceMs = NakamaLink.HostRefreshMs;
                link.Tick();
                await Until(() => peer.PeerState == PeerStateValue.Disconnected, link, peer);
                await Until(async () => NakamaJson.Long(await fixture.Health(), "players", -1) == 0, link, peer);
                A.True(!link.IsReady && link.Session == null, "failed central refresh clears host");
                Console.WriteLine("E2E central ticket source failure -> clean host disconnect + player removed OK");
            }
            finally
            {
                peer.StopThread();
                link.Shutdown();
                if (expirySocket != null)
                    await expirySocket.CloseAsync();
            }
        }

        static Task Until(Func<bool> ready, NakamaLink link, NakamaPeer peer, int timeout = 5000)
        {
            return Until(() => Task.FromResult(ready()), link, peer, timeout);
        }

        static async Task Until(Func<Task<bool>> ready, NakamaLink link, NakamaPeer peer, int timeout = 5000)
        {
            var watch = Stopwatch.StartNew();
            while (!await ready())
            {
                A.True(watch.ElapsedMilliseconds < timeout, "host condition timed out after " + timeout + " ms");
                link.Tick();
                while (peer.DispatchIncomingCommands()) { }
                await Task.Delay(10);
            }
        }

        sealed class HostPlatform : INakamaPlatform
        {
            readonly SynchronizationContext _context = SynchronizationContext.Current;
            readonly Stopwatch _watch = Stopwatch.StartNew();
            public readonly List<IMatchState> States = new List<IMatchState>();
            public ISocket Socket;
            public long AdvanceMs;
            public IClient NewClient(NakamaConfig cfg) { return new Client(cfg.Scheme, cfg.Host, cfg.Port, cfg.ServerKey) { Timeout = cfg.ConnectTimeoutSec }; }
            public ISocket NewSocket(IClient client)
            {
                Socket = Nakama.Socket.From(client, new WebSocketStdlibAdapter());
                Socket.ReceivedMatchState += state => Post(() => States.Add(state));
                return Socket;
            }
            public void ReleaseSocket(ISocket socket) { }
            public void Post(Action action) { _context.Post(_ => action(), null); }
            public long NowMs() { return _watch.ElapsedMilliseconds + AdvanceMs; }
            public NakamaIdentity Identity() { return new NakamaIdentity { Cmid = 42, Name = "Alice" }; }
            public string TokenOverride(int cmid) { throw new AssertException("host read central token override"); }
            public string WebToken() { throw new AssertException("host read web token"); }
            public void SessionReplaced(string message) { throw new AssertException("host replaced central session"); }
            public void Log(string message) { Console.WriteLine("E2E " + message); }
            public void Warn(string message) { Console.WriteLine("E2E " + message); }
        }

        sealed class HostPump : SynchronizationContext
        {
            readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
            public override void Post(SendOrPostCallback callback, object state) { _queue.Add(() => callback(state)); }
            public void Step()
            {
                Action action;
                if (_queue.TryTake(out action, 20))
                    action();
            }
        }

        sealed class HostFixture : IDisposable
        {
            readonly string _root = Environment.GetEnvironmentVariable("HOSTGAME_ROOT") ?? "C:/nk/src/uber-host-game";
            readonly string _temp = Path.Combine(Path.GetTempPath(), "hostclient-" + Guid.NewGuid().ToString("N"));
            readonly HttpClient _http = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(2) };
            readonly ConcurrentQueue<string> _output = new ConcurrentQueue<string>();
            Process _host;
            bool _started;
            string _generator;
            string _seed;
            public string Endpoint { get; private set; }
            public int Pid { get { return _host.Id; } }

            public HostFixture()
            {
                try
                {
                    Directory.CreateDirectory(_temp);
                    _generator = Path.Combine(_temp, "ticketgen.exe");
                    _seed = Path.Combine(_temp, "seed.bin");
                    string cachedGo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "go", "pkg", "mod", "golang.org", "toolchain@v0.0.1-go1.27.1.windows-amd64", "bin", "go.exe");
                    string go = Environment.GetEnvironmentVariable("HOSTGAME_GO") ?? (File.Exists(cachedGo) ? cachedGo : "go");
                    Run(go, "build", "-o", _generator, "./tools/ticketgen");
                    string publicKey = Run(_generator, "-seed", _seed, "-generate");
                    int port;
                    var listener = new TcpListener(IPAddress.Loopback, 0);
                    listener.Start();
                    port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    listener.Stop();
                    Endpoint = "http://127.0.0.1:" + port;
                    string config = new NakamaJsonWriter().Str("name", "SDK host e2e").Str("region", "eu")
                        .Num("port", port).Num("max_players", 8).Str("central_url", "http://127.0.0.1:1")
                        .Str("central_public_key", publicKey).Str("public_address", "127.0.0.1:" + port)
                        .Num("map_id", 5).Num("mode", 101).Num("round_time", 300).Num("splat_limit", 20).End();
                    string path = Path.Combine(_temp, "host.json");
                    File.WriteAllText(path, config);
                    _host = new Process { StartInfo = StartInfo(Path.Combine(_root, "dist", "uber-host.exe"), "-config", path) };
                    _host.OutputDataReceived += (_, e) => { if (e.Data != null) _output.Enqueue(e.Data); };
                    _host.ErrorDataReceived += (_, e) => { if (e.Data != null) _output.Enqueue(e.Data); };
                    _host.Start();
                    _started = true;
                    _host.BeginOutputReadLine();
                    _host.BeginErrorReadLine();
                }
                catch { Dispose(); throw; }
            }

            public async Task<Dictionary<string, object>> Health()
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    A.True(!_host.HasExited, "host exited: " + string.Join(" | ", _output));
                    try { return NakamaJson.ParseObject(await _http.GetStringAsync(Endpoint + "/health")); }
                    catch (HttpRequestException) { await Task.Delay(100); }
                }
                throw new AssertException("host health unavailable: " + string.Join(" | ", _output));
            }

            public string Sign(string hostId, string user, int cmid, string ttl)
            {
                return Run(_generator, "-seed", _seed, "-host", hostId, "-user", user, "-name", user, "-cmid", cmid.ToString(), "-ttl", ttl);
            }

            ProcessStartInfo StartInfo(string file, params string[] args)
            {
                var info = new ProcessStartInfo(file)
                {
                    WorkingDirectory = _root, UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                };
                info.Environment["GOPROXY"] = "off";
                info.Environment["GOSUMDB"] = "off";
                info.Environment["GOTOOLCHAIN"] = "local";
                foreach (string arg in args)
                    info.ArgumentList.Add(arg);
                return info;
            }

            string Run(string file, params string[] args)
            {
                using (Process process = Process.Start(StartInfo(file, args)))
                {
                    Task<string> output = process.StandardOutput.ReadToEndAsync();
                    Task<string> error = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(30000))
                    {
                        process.Kill();
                        process.WaitForExit();
                        throw new AssertException(Path.GetFileName(file) + " timed out");
                    }
                    A.Eq(0, process.ExitCode, Path.GetFileName(file) + ": " + error.GetAwaiter().GetResult());
                    return output.GetAwaiter().GetResult().Trim();
                }
            }

            public void Dispose()
            {
                if (_host != null)
                {
                    if (_started && !_host.HasExited)
                    {
                        _host.Kill();
                        _host.WaitForExit();
                    }
                    if (_started)
                        Console.WriteLine("E2E stopped owned uber-host PID " + _host.Id);
                    _host.Dispose();
                }
                _http.Dispose();
                if (Directory.Exists(_temp))
                    Directory.Delete(_temp, true);
            }
        }
    }
}
#endif
