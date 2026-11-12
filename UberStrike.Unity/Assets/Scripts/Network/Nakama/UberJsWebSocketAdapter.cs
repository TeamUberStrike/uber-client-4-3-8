// Vendored + patched copy of Nakama's JsWebSocketAdapter (com.heroiclabs.nakama-unity).
// Fixes two WebGL connect bugs that made iOS Safari take minutes to connect:
//   1. The original close handler never completed the connect TaskCompletionSource,
//      so a socket closed by the server DURING connecting hung `ConnectAsync` forever
//      (no timeout exists on the DOM WebSocket — the package's own TODO).
//   2. No connect timeout: a socket that neither opens, errors nor closes now fails
//      after the caller's timeout seconds.
// The bridge JS is vendored too (UberWebGLSocketBridge.jslib) and closes every socket
// on page unload so a reload doesn't leave a stale socket on the node (which the node's
// single-socket policy then rejects for minutes).

#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Nakama
{
    /// <summary>
    /// An adapter which uses the WebSocket protocol with Nakama server on a JavaScript bridge with Unity engine.
    /// </summary>
    public class UberJsWebSocketAdapter : ISocketAdapter
    {
        /// <inheritdoc cref="ISocketAdapter.Connected"/>
        public event Action Connected;

        /// <inheritdoc cref="ISocketAdapter.Closed"/>
        public event Action<string> Closed;

        /// <inheritdoc cref="ISocketAdapter.ReceivedError"/>
        public event Action<Exception> ReceivedError;

        /// <inheritdoc cref="ISocketAdapter.Received"/>
        public event Action<ArraySegment<byte>> Received;

        /// <summary>
        /// If the WebSocket is connected.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// If the WebSocket is connecting.
        /// </summary>
        public bool IsConnecting { get; private set; }

        private int Ref { get; set; } // unique jslib socket ref
        private Uri _uri;

        public UberJsWebSocketAdapter()
        {
            Ref = -1;
        }

        /// <inheritdoc cref="ISocketAdapter.CloseAsync"/>
        public Task CloseAsync()
        {
            UberWebGLSocketBridge.Instance.Close(Ref);
            Ref = -1;
            IsConnecting = false;
            IsConnected = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ISocketAdapter.ConnectAsync"/>
        public async Task ConnectAsync(Uri uri, int timeout)
        {
            if (Ref > -1)
            {
                return;
            }

            _uri = uri;
            IsConnecting = true;
            var tcs = new TaskCompletionSource<bool>();

            Action open = () =>
            {
                IsConnected = true;
                IsConnecting = false;
                tcs.TrySetResult(true);
                Connected?.Invoke();
            };
            Action<int, string> close = (code, reason) =>
            {
                IsConnected = false;
                IsConnecting = false;
                Ref = -1;
                // Vendor patch: a close during connect must fail the connect task,
                // otherwise ConnectAsync would await forever.
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetException(new Exception("websocket closed during connect (code " + code + "): " + reason));
                Closed?.Invoke(reason);
            };
            Action<string> error = reason =>
            {
                IsConnected = false;
                Ref = -1;
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetException(new Exception(reason));
                }
                else
                {
                    ReceivedError?.Invoke(new Exception(reason));
                }
            };
            Action<string> handler = message =>
            {
                Received?.Invoke(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)));
            };
            Ref = UberWebGLSocketBridge.Instance.CreateSocket(uri.AbsoluteUri, open, close, error, handler);

            // Vendor patch: bound the connect. The DOM WebSocket has no timeout of its
            // own; a socket that never fires open/error/close would hang forever.
            int delayMs = Math.Max(1, timeout) * 1000;
            Task completed = await Task.WhenAny(tcs.Task, Task.Delay(delayMs));
            if (completed != tcs.Task)
            {
                if (Ref != -1)
                {
                    UberWebGLSocketBridge.Instance.Close(Ref);
                    Ref = -1;
                }
                IsConnecting = false;
                throw new TimeoutException("websocket connect timeout after " + timeout + " s");
            }
            await tcs.Task;
        }

        /// <inheritdoc cref="ISocketAdapter.SendAsync"/>
        public Task SendAsync(ArraySegment<byte> buffer, bool reliable = true, CancellationToken canceller = default)
        {
            if (Ref == -1)
            {
                throw new SocketException((int)SocketError.NotConnected);
            }

            var payload = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            UberWebGLSocketBridge.Instance.Send(Ref, payload);
            return Task.CompletedTask;
        }

        public override string ToString() =>
            $"UberJsWebSocketAdapter(IsConnected={IsConnected}, IsConnecting={IsConnecting}, Uri='{_uri}')";
    }

    // ReSharper disable once InconsistentNaming
    public class UberWebGLSocketBridge : MonoBehaviour
    {
        private static readonly IDictionary<int, string> CloseErrorMessages = new Dictionary<int, string>
        {
            { 1000, "Normal" },
            { 1001, "Away" },
            { 1002, "ProtocolError" },
            { 1003, "UnsupportedData" },
            { 1004, "Undefined" },
            { 1005, "NoStatus" },
            { 1006, "Abnormal" },
            { 1007, "InvalidData" },
            { 1008, "PolicyViolation" },
            { 1009, "TooBig" },
            { 1010, "MandatoryExtension" },
            { 1011, "ServerError" },
            { 1015, "TlsHandshakeFailure" }
        };

        private static int _globalSocketRef;

        private readonly Dictionary<int, UnityWebGLSocketBridgeHandler> _handlers =
            new Dictionary<int, UnityWebGLSocketBridgeHandler>();

        private static readonly object Lock = new object();
        private static UberWebGLSocketBridge _instance;

        public static UberWebGLSocketBridge Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance != null) return _instance;

                    var go = GameObject.Find("/[Nakama]");
                    if (go == null)
                    {
                        go = new GameObject("[Nakama]");
                    }

                    if (go.GetComponent<UberWebGLSocketBridge>() == null)
                    {
                        go.AddComponent<UberWebGLSocketBridge>();
                    }

                    DontDestroyOnLoad(go);
                    _instance = go.GetComponent<UberWebGLSocketBridge>();
                    return _instance;
                }
            }
        }

        private UberWebGLSocketBridge()
        {
        }

        public int CreateSocket(string address, Action socketOpenHandle, Action<int, string> socketCloseHandle,
            Action<string> socketErrorHandle, Action<string> socketMessageHandle)
        {
            var handler = new UnityWebGLSocketBridgeHandler
            {
                OnOpen = socketOpenHandle,
                OnClose = socketCloseHandle,
                OnMessage = socketMessageHandle,
                OnError = socketErrorHandle
            };

            var socketRef = _globalSocketRef++;
            _handlers.Add(socketRef, handler);
            UberNKCreateSocket(socketRef, address);
            return socketRef;
        }

        public void Close(int socketRef)
        {
            UberNKCloseSocket(socketRef);
        }

        public void Send(int socketRef, string payload)
        {
            UberNKSendData(socketRef, payload);
        }

        private UnityWebGLSocketBridgeHandler GetHandler(int socketRef)
        {
            UnityWebGLSocketBridgeHandler handler;
            _handlers.TryGetValue(socketRef, out handler);
            return handler;
        }

        [DllImport("__Internal")]
        private static extern void UberNKCreateSocket(int socketRef, string address);

        [DllImport("__Internal")]
        private static extern void UberNKCloseSocket(int socketRef);

        [DllImport("__Internal")]
        private static extern void UberNKSendData(int socketRef, string data);

        [DllImport("__Internal")]
        private static extern int UberNKSocketState();

        // called by jslib
        private void NKBridgeOnOpen(string bridgeMsg)
        {
            var index = bridgeMsg.IndexOf('_');
            if (index < 0)
            {
                return;
            }

            var socketRef = Convert.ToInt32(bridgeMsg.Substring(0, index));
            GetHandler(socketRef)?.OnOpen?.Invoke();
        }

        // called by jslib
        private void NKBridgeOnMessage(string bridgeMsg)
        {
            var index = bridgeMsg.IndexOf('_');
            if (index < 0 || index + 1 >= bridgeMsg.Length)
            {
                return;
            }

            var socketRef = Convert.ToInt32(bridgeMsg.Substring(0, index));
            var msg = bridgeMsg.Substring(index + 1);
            GetHandler(socketRef)?.OnMessage?.Invoke(msg);
        }

        // called by jslib
        private void NKBridgeOnClose(string bridgeMsg)
        {
            var tokens = bridgeMsg.Split('_', 3);
            if (tokens.Length < 3)
            {
                return;
            }

            var socketRef = Convert.ToInt32(tokens[0]);
            var code = Convert.ToInt32(tokens[1]);
            var reason = tokens[2];
            if (reason.Length > 0)
            {
                 reason = CloseErrorMessages[code]+": "+reason;
            } else {
                 reason = CloseErrorMessages[code];
            }
            GetHandler(socketRef)?.OnClose?.Invoke(code, reason);
            _handlers.Remove(socketRef);
        }

        // called by jslib
        private void NKBridgeOnError(string bridgeMsg)
        {
            var index = bridgeMsg.IndexOf('_');
            if (index >= 0 && index + 1 < bridgeMsg.Length)
            {
                var socketRef = Convert.ToInt32(bridgeMsg.Substring(0, index));
                var msg = bridgeMsg.Substring(index + 1);

                GetHandler(socketRef)?.OnError?.Invoke(msg);
                _handlers.Remove(socketRef);
            }
        }

        // ReSharper disable once InconsistentNaming
        private class UnityWebGLSocketBridgeHandler
        {
            public Action OnOpen;
            public Action<int, string> OnClose;
            public Action<string> OnError;
            public Action<string> OnMessage;
        }
    }
}

#endif
