using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using Nakama;

namespace UberStrike.Realtime.NakamaAdapter
{
    public struct NakamaIdentity
    {
        public int Cmid;
        public int Access;
        public string Name;
    }

    // What NakamaLink needs from the host (Unity: NakamaSession; tests/CI: plain .NET).
    public interface INakamaPlatform
    {
        IClient NewClient(NakamaConfig cfg);
        ISocket NewSocket(IClient client);

        // runs socket events on the thread that owns the link (Unity: already main thread)
        void Post(Action action);

        long NowMs();
        NakamaIdentity Identity();

        // non-empty overrides every other token source
        string TokenOverride(int cmid);
        string WebToken();

        void SessionReplaced(string message);
        void Log(string message);
        void Warn(string message);
    }

    // One IClient/ISession/ISocket for the whole game; NakamaPeers (Comm, Lobby, Game, probes) attach to it.
    // Single-threaded: call from the owner thread; async continuations and socket events come back via Post.
    public sealed class NakamaLink : INakamaLink
    {
        public const int NotificationSingleSocket = -7;
        public const int NotificationUserBanned = -8;
        public const int FirstSyncTimeoutMs = 2000;

        readonly NakamaConfig _cfg;
        readonly INakamaPlatform _platform;
        readonly NakamaClock _clock = new NakamaClock();
        readonly HashSet<ILinkPeer> _peers = new HashSet<ILinkPeer>();
        readonly Dictionary<ILinkPeer, Action<LinkResult>> _waiting = new Dictionary<ILinkPeer, Action<LinkResult>>();
        readonly Dictionary<string, ILinkPeer> _routes = new Dictionary<string, ILinkPeer>();

        IClient _client;
        ISession _session;
        ISocket _socket;
        string _selfSession;
        int _authCmid;
        int _epoch;
        bool _connecting;
        bool _pingInFlight;
        long _nextPing;
        long _emptySince = -1;
        long _authRetryAt;
        int _authFailures;

        public NakamaLink(NakamaConfig cfg, INakamaPlatform platform)
        {
            if (platform == null)
                throw new ArgumentNullException("platform");
            _cfg = cfg ?? new NakamaConfig();
            _platform = platform;
        }

        public NakamaConfig Config { get { return _cfg; } }
        public bool IsReady { get { return _socket != null && _socket.IsConnected && !_connecting; } }
        public bool IsConnecting { get { return _connecting; } }
        public int AttachedPeers { get { return _peers.Count; } }
        public int AuthenticatedCmid { get { return _authCmid; } }
        public NakamaClock Clock { get { return _clock; } }
        public ISession Session { get { return _session; } }

        long Now { get { return _platform.NowMs(); } }

        // INakamaLink

        public int ServerTimeMs { get { return _clock.Synced ? _clock.ServerTime(Now) : (int)(Now & int.MaxValue); } }
        public int RoundTripTime { get { return _clock.RoundTripTime; } }
        public int RoundTripTimeVariance { get { return _clock.RoundTripTimeVariance; } }

        public void SyncClock()
        {
            _nextPing = 0;
        }

        public void Attach(ILinkPeer peer, string appName, Action<LinkResult> ready)
        {
            _peers.Add(peer);
            _emptySince = -1;

            int cmid = ParseCmid(appName);
            bool sameIdentity = cmid <= 0 || cmid == _authCmid;
            if (IsReady && sameIdentity)
            {
                ready(LinkResult.Success);
                return;
            }

            _waiting[peer] = ready;
            if (IsReady)
            {
                _platform.Log("[nakama] identity changed " + _authCmid + " -> " + cmid + ", reconnecting");
                _session = null;
                CloseSocket("identity changed");
            }
            if (!_connecting)
                Connect(cmid);
        }

        public void Detach(ILinkPeer peer)
        {
            _peers.Remove(peer);
            _waiting.Remove(peer);

            List<string> drop = null;
            foreach (KeyValuePair<string, ILinkPeer> r in _routes)
                if (r.Value == peer)
                    (drop ?? (drop = new List<string>())).Add(r.Key);
            if (drop != null)
                foreach (string m in drop)
                    _routes.Remove(m);

            if (_peers.Count == 0)
                _emptySince = Now;
        }

        public void Rpc(string id, string payload, Action<string, string> done)
        {
            ISocket s = _socket;
            if (s == null || !s.IsConnected)
            {
                done(null, "not connected");
                return;
            }
            RunRpc(s, id, payload, done);
        }

        public void JoinMatch(ILinkPeer peer, string matchId, Action<string> done)
        {
            ISocket s = _socket;
            if (s == null || !s.IsConnected)
            {
                done("not connected");
                return;
            }
            _routes[matchId] = peer;
            RunJoin(s, peer, matchId, done);
        }

        public void LeaveMatch(ILinkPeer peer, string matchId, Action<string> done)
        {
            ILinkPeer owner;
            if (_routes.TryGetValue(matchId, out owner) && owner == peer)
                _routes.Remove(matchId);

            ISocket s = _socket;
            if (s == null || !s.IsConnected)
            {
                done(null);
                return;
            }
            RunLeave(s, matchId, done);
        }

        public bool Send(string matchId, long opCode, byte[] data)
        {
            ISocket s = _socket;
            if (s == null || !s.IsConnected)
                return false;

            Observe(s.SendMatchStateAsync(matchId, opCode, data), "send op " + opCode);
            return true;
        }

        // host

        public void Tick()
        {
            if (!IsReady)
                return;

            long now = Now;
            if (!_pingInFlight && now >= _nextPing)
                Ping();

            if (_peers.Count == 0 && _emptySince >= 0 && now - _emptySince >= _cfg.CloseGraceMs)
            {
                _emptySince = -1;
                CloseSocket("idle");
            }
        }

        public void Shutdown()
        {
            _waiting.Clear();
            CloseSocket("quit");
        }

        // connect

        async void Connect(int cmidHint)
        {
            _connecting = true;
            int epoch = ++_epoch;
            ISocket socket = null;
            LinkResult result;

            try
            {
                if (Now < _authRetryAt)
                    throw new LinkException(LinkFailure.AuthRejected, "login rejected, retry in " + ((_authRetryAt - Now) / 1000 + 1) + " s");

                NakamaIdentity id = Identity();
                int cmid = cmidHint > 0 ? cmidHint : id.Cmid;
                string token = ResolveToken(cmid, id);

                if (_client == null)
                    _client = _platform.NewClient(_cfg);

                if (_session == null || _authCmid != cmid || _session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
                {
                    _session = null;
                    var vars = new Dictionary<string, string> { { "token", token } };
                    ISession session = await _client.AuthenticateCustomAsync(token, null, true, vars, new RetryConfiguration(250, 1));
                    if (epoch != _epoch)
                        return;
                    _session = session;
                    _authCmid = cmid;
                    _authFailures = 0;
                }

                socket = _platform.NewSocket(_client);
                Wire(socket);
                await socket.ConnectAsync(_session, false, _cfg.ConnectTimeoutSec);
                if (epoch != _epoch)
                {
                    Observe(socket.CloseAsync(), "close stale socket");
                    return;
                }

                _socket = socket;
                _clock.Reset();
                await FirstSync(socket);
                if (_socket != socket || !socket.IsConnected)
                    throw new LinkException(LinkFailure.Closed, "socket closed during first sync");
                result = LinkResult.Success;
                _platform.Log("[nakama] connected " + _cfg.Endpoint + " as cmid " + _authCmid + ", rtt " + _clock.RoundTripTime + " ms");
            }
            catch (Exception e)
            {
                result = Classify(e);
                if (result.Failure == LinkFailure.AuthRejected && !(e is LinkException))
                {
                    _session = null;
                    _authFailures++;
                    _authRetryAt = Now + Math.Min(30000, 1000L << Math.Min(_authFailures, 5));
                }
                if (socket != null && _socket == socket)
                    _socket = null;
                if (socket != null)
                    Observe(socket.CloseAsync(), "close failed socket");
                _platform.Warn("[nakama] connect to " + _cfg.Endpoint + " failed (" + result.Failure + "): " + result.Message);
            }

            if (epoch != _epoch)
                return;
            _connecting = false;

            var waiting = new List<KeyValuePair<ILinkPeer, Action<LinkResult>>>(_waiting);
            _waiting.Clear();
            foreach (KeyValuePair<ILinkPeer, Action<LinkResult>> w in waiting)
            {
                if (!result.Ok)
                    _peers.Remove(w.Key);
                w.Value(result);
            }
            if (_peers.Count == 0)
                _emptySince = Now;
        }

        async Task FirstSync(ISocket s)
        {
            long c = Now;
            Task<IApiRpc> rpc = s.RpcAsync(NakamaFraming.RpcTime, NakamaFraming.TimeRequest(c));
            if (await Task.WhenAny(rpc, Task.Delay(FirstSyncTimeoutMs)) != rpc)
            {
                Observe(rpc, "first uber_time");
                _platform.Warn("[nakama] first uber_time timed out");
                return;
            }
            try
            {
                AddTimeSample(c, rpc.Result.Payload);
            }
            catch (Exception e)
            {
                _platform.Warn("[nakama] first uber_time failed: " + Message(e));
            }
            _nextPing = Now + _cfg.TimePingMs;
        }

        string ResolveToken(int cmid, NakamaIdentity id)
        {
            string t = _platform.TokenOverride(cmid);
            if (!string.IsNullOrEmpty(t))
                return t;

            if (_cfg.DevAuth)
            {
                if (cmid <= 0)
                    throw new LinkException(LinkFailure.NoToken, "dev auth needs a logged-in cmid");
                return DevToken(cmid, id.Access, id.Name);
            }

            t = _platform.WebToken();
            if (!string.IsNullOrEmpty(t))
                return t;
            if (!string.IsNullOrEmpty(_cfg.Token))
                return _cfg.Token;
            throw new LinkException(LinkFailure.NoToken, "no login token: set NakamaSession.WebToken after web login (Q2), -nakamatoken, or -nakamadev against a UBER_DEV_AUTH=true node");
        }

        // dev:<cmid>:<access>:<name>, Go auth.ParseDevToken (access 0..10, name 1..64)
        public static string DevToken(int cmid, int access, string name)
        {
            name = string.IsNullOrEmpty(name) ? "" : name.Trim();
            if (name.Length == 0)
                name = "Player" + cmid;
            if (name.Length > 64)
                name = name.Substring(0, 64).Trim();
            return "dev:" + cmid + ":" + Math.Max(0, Math.Min(10, access)) + ":" + name;
        }

        NakamaIdentity Identity()
        {
            try { return _platform.Identity(); }
            catch (Exception) { return new NakamaIdentity(); }
        }

        static int ParseCmid(string appName)
        {
            int cmid;
            return int.TryParse(appName, out cmid) ? cmid : 0;
        }

        // socket events

        void Wire(ISocket s)
        {
            s.Closed += reason => _platform.Post(() => OnClosed(s, reason));
            s.ReceivedMatchState += st => _platform.Post(() => OnMatchState(s, st));
            s.ReceivedMatchPresence += ev => _platform.Post(() => OnMatchPresence(s, ev));
            s.ReceivedNotification += n => _platform.Post(() => OnNotification(s, n));
            s.ReceivedError += e => _platform.Post(() => { if (s == _socket) _platform.Warn("[nakama] socket error: " + e.Message); });
        }

        void OnClosed(ISocket s, string reason)
        {
            if (s != _socket)
                return;

            _socket = null;
            _platform.Log("[nakama] socket closed by server: " + reason);
            DropPeers(true, reason);

            if (_waiting.Count > 0 && !_connecting)
                Connect(0);
        }

        // socket already detached: tell every attached (not waiting) peer, forget routes
        void DropPeers(bool byServer, string reason)
        {
            _routes.Clear();
            _selfSession = null;
            _pingInFlight = false;

            var peers = new List<ILinkPeer>();
            foreach (ILinkPeer p in _peers)
                if (!_waiting.ContainsKey(p))
                    peers.Add(p);
            foreach (ILinkPeer p in peers)
                _peers.Remove(p);
            foreach (ILinkPeer p in peers)
                p.OnLinkClosed(byServer, reason);
        }

        void OnMatchState(ISocket s, IMatchState st)
        {
            ILinkPeer peer;
            if (s == _socket && st != null && st.MatchId != null && _routes.TryGetValue(st.MatchId, out peer))
                peer.OnMatchData(st.MatchId, st.OpCode, st.State);
        }

        void OnMatchPresence(ISocket s, IMatchPresenceEvent ev)
        {
            if (s != _socket || ev == null || ev.MatchId == null || _selfSession == null)
                return;

            foreach (IUserPresence p in ev.Leaves)
            {
                if (p == null || p.SessionId != _selfSession)
                    continue;
                ILinkPeer peer;
                if (_routes.TryGetValue(ev.MatchId, out peer))
                {
                    _routes.Remove(ev.MatchId);
                    peer.OnMatchLeft(ev.MatchId);
                }
                return;
            }
        }

        void OnNotification(ISocket s, IApiNotification n)
        {
            if (s != _socket || n == null)
                return;

            string msg = null;
            if (n.Code == NotificationSingleSocket)
                msg = "You have been disconnected because your account logged in somewhere else.";
            else if (n.Code == NotificationUserBanned)
                msg = "You have been disconnected. Your account has been banned.";
            if (msg == null)
                return;

            _platform.Warn("[nakama] notification " + n.Code + " " + n.Subject);
            _platform.SessionReplaced(msg);
        }

        // async helpers: result first, callback outside the try

        async void RunRpc(ISocket s, string id, string payload, Action<string, string> done)
        {
            string reply = null, err = null;
            try
            {
                IApiRpc r = await s.RpcAsync(id, payload);
                reply = r != null && r.Payload != null ? r.Payload : "";
            }
            catch (Exception e)
            {
                err = Message(e);
            }
            done(reply, err);
        }

        async void RunJoin(ISocket s, ILinkPeer peer, string matchId, Action<string> done)
        {
            string err = null;
            try
            {
                IMatch m = await s.JoinMatchAsync(matchId);
                if (m != null && m.Self != null)
                    _selfSession = m.Self.SessionId;
            }
            catch (Exception e)
            {
                err = Message(e);
                ILinkPeer owner;
                if (_routes.TryGetValue(matchId, out owner) && owner == peer)
                    _routes.Remove(matchId);
            }
            done(err);
        }

        async void RunLeave(ISocket s, string matchId, Action<string> done)
        {
            string err = null;
            try
            {
                await s.LeaveMatchAsync(matchId);
            }
            catch (Exception e)
            {
                err = Message(e);
            }
            done(err);
        }

        void Ping()
        {
            ISocket s = _socket;
            long c = Now;
            _pingInFlight = true;
            RunRpc(s, NakamaFraming.RpcTime, NakamaFraming.TimeRequest(c), (reply, err) =>
            {
                if (s != _socket)
                    return;
                _pingInFlight = false;
                _nextPing = Now + _cfg.TimePingMs;
                if (err == null)
                    AddTimeSample(c, reply);
            });
        }

        void AddTimeSample(long sent, string reply)
        {
            long echo;
            int serverMs;
            if (NakamaFraming.TryParseTimeReply(reply, out echo, out serverMs) && echo == sent)
                _clock.AddSample(sent, Now, serverMs);
        }

        void CloseSocket(string why)
        {
            ISocket s = _socket;
            if (s == null)
                return;

            _socket = null;
            ++_epoch;
            _connecting = false;
            _platform.Log("[nakama] closing socket: " + why);
            Observe(s.CloseAsync(), "close");
            DropPeers(false, why);
        }

        void Observe(Task t, string what)
        {
            INakamaPlatform p = _platform;
            t.ContinueWith(x => p.Warn("[nakama] " + what + " failed: " + Message(x.Exception)), TaskContinuationOptions.OnlyOnFaulted);
        }

        public static LinkResult Classify(Exception e)
        {
            Exception root = e;
            var agg = e as AggregateException;
            if (agg != null && agg.InnerException != null)
                root = agg.InnerException;

            var link = root as LinkException;
            if (link != null)
                return LinkResult.Fail(link.Failure, link.Message);

            var api = root as ApiResponseException;
            if (api != null)
            {
                if (api.StatusCode == 401 || api.StatusCode == 403 || api.GrpcStatusCode == 16 || api.GrpcStatusCode == 7)
                    return LinkResult.Fail(LinkFailure.AuthRejected, api.Message);
                return LinkResult.Fail(LinkFailure.Unreachable, api.Message);
            }

            for (Exception x = root; x != null; x = x.InnerException)
                if (x is AuthenticationException)
                    return LinkResult.Fail(LinkFailure.Tls, x.Message);

            if (root is TaskCanceledException || root is OperationCanceledException || root is TimeoutException)
                return LinkResult.Fail(LinkFailure.Timeout, Message(root));

            return LinkResult.Fail(LinkFailure.Unreachable, Message(root));
        }

        static string Message(Exception e)
        {
            var agg = e as AggregateException;
            if (agg != null && agg.InnerException != null)
                e = agg.InnerException;
            return e == null ? "unknown" : e.GetType().Name + ": " + e.Message;
        }
    }

    public sealed class LinkException : Exception
    {
        public readonly LinkFailure Failure;

        public LinkException(LinkFailure failure, string message) : base(message)
        {
            Failure = failure;
        }
    }
}
