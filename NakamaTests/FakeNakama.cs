#if NAKAMA_LINK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nakama;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // Nakama interfaces faked with DispatchProxy: only what NakamaLink touches; anything else throws.
    public class Proxy<T> : DispatchProxy
    {
        public Func<MethodInfo, object[], object> Handler;

        protected override object Invoke(MethodInfo m, object[] a)
        {
            return Handler(m, a);
        }

        public static T Make(Func<MethodInfo, object[], object> h)
        {
            T p = Create<T, Proxy<T>>();
            ((Proxy<T>)(object)p).Handler = h;
            return p;
        }

        public static T Props(Dictionary<string, object> values)
        {
            return Make((m, a) =>
            {
                object v;
                if (m.Name.StartsWith("get_") && values.TryGetValue(m.Name.Substring(4), out v))
                    return v;
                throw new NotImplementedException(typeof(T).Name + "." + m.Name);
            });
        }
    }

    static class N
    {
        public static IUserPresence Presence(string session)
        {
            return Proxy<IUserPresence>.Props(new Dictionary<string, object> { { "SessionId", session }, { "UserId", "u-" + session } });
        }

        public static IApiRpc Rpc(string id, string payload)
        {
            return Proxy<IApiRpc>.Props(new Dictionary<string, object> { { "Id", id }, { "Payload", payload } });
        }

        public static IMatch Match(string id, string selfSession)
        {
            return Proxy<IMatch>.Props(new Dictionary<string, object> { { "Id", id }, { "Self", Presence(selfSession) } });
        }

        public static IMatchState State(string match, long op, byte[] data)
        {
            return Proxy<IMatchState>.Props(new Dictionary<string, object> { { "MatchId", match }, { "OpCode", op }, { "State", data } });
        }

        public static IMatchPresenceEvent Leave(string match, params string[] sessions)
        {
            return Proxy<IMatchPresenceEvent>.Props(new Dictionary<string, object>
            {
                { "MatchId", match },
                { "Leaves", sessions.Select(Presence).ToList() },
                { "Joins", new List<IUserPresence>() },
            });
        }

        public static IApiNotification Notification(int code, string subject)
        {
            return Proxy<IApiNotification>.Props(new Dictionary<string, object> { { "Code", code }, { "Subject", subject } });
        }

        public static ISession Session(bool expired = false, bool refreshExpired = false, int varsCmid = 0)
        {
            IDictionary<string, string> vars = varsCmid > 0 ? new Dictionary<string, string> { { "cmid", varsCmid.ToString() }, { "access", "0" } } : null;
            return Proxy<ISession>.Make((m, a) =>
            {
                switch (m.Name)
                {
                    case "HasExpired": return expired;
                    case "HasRefreshExpired": return refreshExpired;
                    case "get_Vars": return vars;
                    case "get_AuthToken": return "jwt";
                    case "get_UserId": return "user-1";
                    default: throw new NotImplementedException("ISession." + m.Name);
                }
            });
        }
    }

    sealed class FakeSocket
    {
        public readonly ISocket Socket;
        public bool Connected;
        public bool AutoTime = true;
        public int ServerMs = 50000;
        public readonly TaskCompletionSource<bool> ConnectTcs = new TaskCompletionSource<bool>();
        public readonly List<Tuple<string, string, TaskCompletionSource<IApiRpc>>> Rpcs = new List<Tuple<string, string, TaskCompletionSource<IApiRpc>>>();
        public readonly List<Tuple<string, TaskCompletionSource<IMatch>>> Joins = new List<Tuple<string, TaskCompletionSource<IMatch>>>();
        public readonly List<string> Leaves = new List<string>();
        public readonly List<Tuple<string, long, byte[]>> Sent = new List<Tuple<string, long, byte[]>>();
        public int Closes;
        public int TimeCalls;

        Action<string> _closed;
        Action<IMatchState> _state;
        Action<IMatchPresenceEvent> _presence;
        Action<IApiNotification> _notification;
        Action<Exception> _error;

        public FakeSocket()
        {
            Socket = Proxy<ISocket>.Make(Handle);
        }

        object Handle(MethodInfo m, object[] a)
        {
            switch (m.Name)
            {
                case "add_Closed": _closed += (Action<string>)a[0]; return null;
                case "add_ReceivedMatchState": _state += (Action<IMatchState>)a[0]; return null;
                case "add_ReceivedMatchPresence": _presence += (Action<IMatchPresenceEvent>)a[0]; return null;
                case "add_ReceivedNotification": _notification += (Action<IApiNotification>)a[0]; return null;
                case "add_ReceivedError": _error += (Action<Exception>)a[0]; return null;
                case "get_IsConnected": return Connected;
                case "get_IsConnecting": return false;
                case "ConnectAsync": return ConnectTcs.Task;
                case "CloseAsync": Closes++; Connected = false; return Task.CompletedTask;
                case "RpcAsync":
                    {
                        string id = (string)a[0];
                        string payload = (string)a[1];
                        if (id == NakamaFraming.RpcTime)
                            TimeCalls++;
                        if (id == NakamaFraming.RpcTime && AutoTime)
                        {
                            long c;
                            int s;
                            NakamaFraming.TryParseTimeReply(payload.Replace("}", ",\"s\":0}"), out c, out s);
                            return Task.FromResult(N.Rpc(id, "{\"c\":" + c + ",\"s\":" + ServerMs + "}"));
                        }
                        var t = new TaskCompletionSource<IApiRpc>();
                        Rpcs.Add(Tuple.Create(id, payload, t));
                        return t.Task;
                    }
                case "JoinMatchAsync":
                    {
                        if (!(a[0] is string))
                            throw new NotImplementedException("JoinMatchAsync(matched)");
                        var t = new TaskCompletionSource<IMatch>();
                        Joins.Add(Tuple.Create((string)a[0], t));
                        return t.Task;
                    }
                case "LeaveMatchAsync":
                    Leaves.Add(a[0] as string);
                    return Task.CompletedTask;
                case "SendMatchStateAsync":
                    Sent.Add(Tuple.Create((string)a[0], (long)a[1], (byte[])a[2]));
                    return Task.CompletedTask;
                default:
                    throw new NotImplementedException("ISocket." + m.Name);
            }
        }

        public void Open()
        {
            Connected = true;
            ConnectTcs.SetResult(true);
        }

        public void ServerClose(string reason)
        {
            Connected = false;
            _closed?.Invoke(reason);
        }

        public void Deliver(IMatchState s) { _state?.Invoke(s); }
        public void Presence(IMatchPresenceEvent e) { _presence?.Invoke(e); }
        public void Notify(IApiNotification n) { _notification?.Invoke(n); }
        public void Error(Exception e) { _error?.Invoke(e); }

        public Tuple<string, string, TaskCompletionSource<IApiRpc>> LastRpc(string id)
        {
            return Rpcs.Last(r => r.Item1 == id);
        }
    }

    sealed class FakeClient
    {
        public readonly IClient Client;
        public readonly List<Tuple<string, Dictionary<string, string>>> Auths = new List<Tuple<string, Dictionary<string, string>>>();
        public Func<Task<ISession>> Next = () => Task.FromResult(N.Session());
        public Func<Task<ISession>> Refresh = () => Task.FromResult(N.Session());
        public int Refreshes;

        public FakeClient()
        {
            Client = Proxy<IClient>.Make((m, a) =>
            {
                switch (m.Name)
                {
                    case "AuthenticateCustomAsync":
                        Auths.Add(Tuple.Create((string)a[0], (Dictionary<string, string>)a[3]));
                        return Next();
                    case "SessionRefreshAsync":
                        Refreshes++;
                        return Refresh();
                    case "set_Timeout":
                        return null;
                    default:
                        throw new NotImplementedException("IClient." + m.Name);
                }
            });
        }
    }

    sealed class FakePlatform : INakamaPlatform
    {
        public readonly FakeClient Client = new FakeClient();
        public readonly List<FakeSocket> Sockets = new List<FakeSocket>();
        public long Now = 1000;
        public NakamaIdentity Id = new NakamaIdentity { Cmid = 1234, Access = 2, Name = "Hazard Test" };
        public string Override;
        public string Web;
        public readonly List<string> Replaced = new List<string>();
        public readonly List<string> Logs = new List<string>();
        public readonly List<ISocket> Released = new List<ISocket>();
        public int Clients;

        public FakeSocket Socket { get { return Sockets[Sockets.Count - 1]; } }

        public readonly List<string> Endpoints = new List<string>();

        public IClient NewClient(NakamaConfig cfg) { Clients++; Endpoints.Add(cfg.Endpoint); return Client.Client; }

        public ISocket NewSocket(IClient client)
        {
            var s = new FakeSocket();
            Sockets.Add(s);
            return s.Socket;
        }

        public void ReleaseSocket(ISocket socket) { Released.Add(socket); }
        public void Post(Action action) { action(); }
        public long NowMs() { return Now; }
        public NakamaIdentity Identity() { return Id; }
        public string TokenOverride(int cmid) { return Override; }
        public string WebToken() { return Web; }
        public void SessionReplaced(string message) { Replaced.Add(message); }
        public void Log(string message) { Logs.Add(message); }
        public void Warn(string message) { Logs.Add("W " + message); }
    }

    sealed class LinkPeer : ILinkPeer
    {
        public readonly List<LinkResult> Ready = new List<LinkResult>();
        public readonly List<Tuple<string, long, byte[]>> Data = new List<Tuple<string, long, byte[]>>();
        public readonly List<string> Left = new List<string>();
        public readonly List<Tuple<bool, string>> Closed = new List<Tuple<bool, string>>();
        public readonly HashSet<string> GameRooms = new HashSet<string>();

        public Action<LinkResult> OnReady { get { return r => Ready.Add(r); } }
        public void OnMatchData(string matchId, long opCode, byte[] data) { Data.Add(Tuple.Create(matchId, opCode, data)); }
        public void OnMatchLeft(string matchId) { Left.Add(matchId); }
        public bool InGameRoom(string matchId) { return GameRooms.Contains(matchId); }
        public void OnLinkClosed(bool byServer, string reason) { Closed.Add(Tuple.Create(byServer, reason)); }
    }
}
#endif
