using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace UberStrike.Realtime.NakamaAdapter
{
    // Game-wide NakamaLink on Unity: UnityWebRequestAdapter, main-thread UnitySocket (destroyed on release), Debug log.
    public static class NakamaSession
    {
        // Q2: web login token. 4.3.8 login has none yet; the login flow sets this once the web mints one.
        public static string WebToken;

        // overrides every other token source when it returns non-empty
        public static Func<int, string> TokenProvider;

        // logged-in player (dev token + cmid fallback), set by NakamaBootstrap
        public static Func<NakamaIdentity> IdentitySource;

        // single_socket / banned notification -> legacy kill path (CommRPC 36 behaviour)
        public static Action<string> SessionReplaced;

        static NakamaLink _instance;

        public static NakamaLink Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NakamaLink(NakamaConfig.Current ?? NakamaBootstrap.LoadConfig(), new UnityPlatform());
                    NakamaSessionHost.Ensure(_instance);
                }
                return _instance;
            }
        }

        public static void ResetStatics()
        {
            _instance = null;
            WebToken = null;
            TokenProvider = null;
            IdentitySource = null;
            SessionReplaced = null;
        }

        sealed class UnityPlatform : INakamaPlatform
        {
            readonly Stopwatch _watch = Stopwatch.StartNew();
            readonly Dictionary<ISocket, UnitySocket> _adapters = new Dictionary<ISocket, UnitySocket>();

            public IClient NewClient(NakamaConfig cfg)
            {
                var c = new Client(cfg.Scheme, cfg.Host, cfg.Port, cfg.ServerKey, UnityWebRequestAdapter.Instance);
                c.Timeout = cfg.ConnectTimeoutSec;
                return c;
            }

            // = client.NewSocket(true), keeping the UnitySocket so ReleaseSocket can destroy its GameObject
            public ISocket NewSocket(IClient client)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                ISocketAdapter inner = new JsWebSocketAdapter();
#else
                ISocketAdapter inner = new WebSocketStdlibAdapter();
#endif
                UnitySocket adapter = UnitySocket.Create(inner);
                ISocket socket = Socket.From(client, adapter);
                _adapters[socket] = adapter;
                return socket;
            }

            public void ReleaseSocket(ISocket socket)
            {
                UnitySocket adapter;
                if (socket == null || !_adapters.TryGetValue(socket, out adapter))
                    return;
                _adapters.Remove(socket);
                if (adapter != null)
                    UnityEngine.Object.Destroy(adapter.gameObject);
            }

            public void Post(Action action)
            {
                action();
            }

            public long NowMs()
            {
                return _watch.ElapsedMilliseconds;
            }

            public NakamaIdentity Identity()
            {
                return IdentitySource != null ? IdentitySource() : new NakamaIdentity();
            }

            public string TokenOverride(int cmid)
            {
                return TokenProvider != null ? TokenProvider(cmid) : null;
            }

            public string WebToken()
            {
                return NakamaSession.WebToken;
            }

            public void SessionReplaced(string message)
            {
                if (NakamaSession.SessionReplaced != null)
                    NakamaSession.SessionReplaced(message);
            }

            public void Log(string message)
            {
                Debug.Log(message);
            }

            public void Warn(string message)
            {
                Debug.LogWarning(message);
            }
        }
    }
}
