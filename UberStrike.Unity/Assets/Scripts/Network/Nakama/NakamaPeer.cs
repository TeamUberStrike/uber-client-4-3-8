using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace UberStrike.Realtime.NakamaAdapter
{
    using Cmune.Realtime.Photon.Client.Transport;
    using TDebugLevel = Cmune.Realtime.Photon.Client.Transport.DebugLevel;

    // INetworkPeer on Nakama. One per PhotonClient; all share one socket (INakamaLink).
    // Listener callbacks only from DispatchIncomingCommands, one per call (Photon3Unity3D contract).
    public sealed class NakamaPeer : INetworkPeer, ILinkPeer
    {
        public const string KindGame = "game";
        public const string KindComm = "comm";
        public const string KindLobby = "lobby";

        sealed class Item
        {
            public int Gen;
            public Func<bool> Run;
        }

        readonly INakamaLink _link;
        readonly ConcurrentQueue<Item> _queue = new ConcurrentQueue<Item>();

        volatile int _gen;
        PeerStateValue _state = PeerStateValue.Disconnected;
        string _server;
        string _kind = KindGame;
        string _matchId;
        int _room;
        bool _joining;
        bool _stopped;
        long _bytesIn;
        long _bytesOut;
        int _inFlight;

        // every room shares the node address; kind = game until the join reply (88 comm, 66 lobby)
        public NakamaPeer(INakamaLink link)
        {
            if (link == null)
                throw new ArgumentNullException("link");
            _link = link;
        }

        public INetworkPeerListener Listener { get; set; }
        public PeerStateValue PeerState { get { return _state; } }
        // >= 1 while connected: GetBestServer skips latency 0
        public int RoundTripTime { get { return _state == PeerStateValue.Connected ? Math.Max(1, _link.RoundTripTime) : _link.RoundTripTime; } }
        public int RoundTripTimeVariance { get { return _link.RoundTripTimeVariance; } }
        public long BytesIn { get { return Interlocked.Read(ref _bytesIn); } }
        public long BytesOut { get { return Interlocked.Read(ref _bytesOut); } }
        public int QueuedIncomingCommands { get { return _queue.Count; } }
        public int QueuedOutgoingCommands { get { return Volatile.Read(ref _inFlight); } }
        public int ServerTimeInMilliSeconds { get { return _link.ServerTimeMs; } }

        public string MatchId { get { return _matchId; } }
        public string Kind { get { return _kind; } }

        public bool Connect(string serverAddress, string applicationName)
        {
            if (_state != PeerStateValue.Disconnected || _stopped)
                return false;

            NewGeneration();
            _state = PeerStateValue.Connecting;
            _server = serverAddress;
            _kind = KindGame;
            _matchId = null;
            _room = 0;
            _joining = false;

            int gen = _gen;
            try
            {
                _link.Attach(this, applicationName, r => Post(gen, () => OnAttached(r)));
            }
            catch (Exception)
            {
                _state = PeerStateValue.Disconnected;
                _link.Detach(this);
                return false;
            }
            return true;
        }

        public void Disconnect()
        {
            if (_state == PeerStateValue.Disconnected || _state == PeerStateValue.Disconnecting)
                return;

            _state = PeerStateValue.Disconnecting;
            DropMatch();
            _link.Detach(this);

            NewGeneration();
            Post(_gen, () => Status(StatusCode.Disconnect));
        }

        public void StopThread()
        {
            _stopped = true;
            if (_matchId != null)
                DropMatch();
            _link.Detach(this);
        }

        public void FetchServerTimestamp()
        {
            _link.SyncClock();
        }

        public bool SendOperation(byte operationCode, Dictionary<byte, object> parameters, bool sendReliable)
        {
            if (_state != PeerStateValue.Connected || parameters == null)
                return false;

            try
            {
                switch (operationCode)
                {
                    case NakamaFraming.OpToApplication:
                        return SendOp66(parameters);
                    case NakamaFraming.OpJoin:
                        return SendJoin(parameters);
                    case NakamaFraming.OpLeave:
                        return SendLeave();
                    default:
                        if (!NakamaFraming.IsRoomOp(operationCode) || _matchId == null || _joining)
                            return false;
                        byte[] data = NakamaFraming.EncodeEnvelope(operationCode, parameters);
                        if (!_link.Send(_matchId, operationCode, data))
                            return false;
                        Interlocked.Add(ref _bytesOut, data.Length);
                        return true;
                }
            }
            catch (FramingException e)
            {
                string msg = "op " + operationCode + ": " + e.Message;
                Post(_gen, () => Debug(TDebugLevel.ERROR, msg, true));
                return false;
            }
        }

        public bool DispatchIncomingCommands()
        {
            Item it;
            while (_queue.TryDequeue(out it))
            {
                if (it.Gen != _gen)
                    continue;
                if (it.Run())
                    return true;
            }
            return false;
        }

        public bool SendOutgoingCommands()
        {
            return false;
        }

        // ILinkPeer (any thread)

        public void OnMatchData(string matchId, long opCode, byte[] data)
        {
            if (data != null)
                Interlocked.Add(ref _bytesIn, data.Length);
            Post(_gen, () => HandleMatchData(matchId, opCode, data));
        }

        public void OnMatchLeft(string matchId)
        {
            Post(_gen, () =>
            {
                if (matchId != _matchId || _state != PeerStateValue.Connected)
                    return false;
                _matchId = null;
                _joining = false;
                _link.Detach(this);
                return Status(StatusCode.DisconnectByServerLogic);
            });
        }

        public bool InGameRoom(string matchId)
        {
            return _state == PeerStateValue.Connected && !_joining && matchId != null && matchId == _matchId
                && _room > 0 && _room != NakamaFraming.RoomLobby && _room != NakamaFraming.RoomComm;
        }

        public void OnLinkClosed(bool byServer, string reason)
        {
            Post(_gen, () =>
            {
                _matchId = null;
                _joining = false;
                if (_state == PeerStateValue.Connecting)
                    return Status(StatusCode.ExceptionOnConnect);
                if (_state == PeerStateValue.Connected)
                {
                    Debug(TDebugLevel.WARNING, "socket closed: " + reason, false);
                    return Status(byServer ? StatusCode.DisconnectByServer : StatusCode.Disconnect);
                }
                return false;
            });
        }

        // internals (main thread, inside Dispatch)

        void NewGeneration()
        {
            Interlocked.Increment(ref _gen);
            Item drop;
            while (_queue.TryDequeue(out drop)) { }
        }

        void Post(int gen, Func<bool> run)
        {
            _queue.Enqueue(new Item { Gen = gen, Run = run });
        }

        bool OnAttached(LinkResult r)
        {
            if (_state != PeerStateValue.Connecting)
                return false;

            if (r.Ok)
                return Status(StatusCode.Connect);

            _link.Detach(this);
            Debug(TDebugLevel.ERROR, "connect failed (" + r.Failure + "): " + r.Message, false);
            if (r.Failure == LinkFailure.Tls)
            {
                Status(StatusCode.SecurityExceptionOnConnect);
                Post(_gen, () => Status(StatusCode.Disconnect));
                return true;
            }
            return Status(StatusCode.ExceptionOnConnect);
        }

        bool SendOp66(Dictionary<byte, object> p)
        {
            short invoc = NakamaFraming.Get<short>(p, NakamaFraming.KeyInvocationId);
            string json = NakamaFraming.Op66Request(p, _kind);
            Interlocked.Add(ref _bytesOut, json.Length);
            Interlocked.Increment(ref _inFlight);

            int gen = _gen;
            _link.Rpc(NakamaFraming.RpcOp66, json, (reply, err) =>
            {
                Interlocked.Decrement(ref _inFlight);
                if (reply != null)
                    Interlocked.Add(ref _bytesIn, reply.Length);
                Post(gen, () => OnOp66Reply(invoc, reply, err));
            });
            return true;
        }

        bool OnOp66Reply(short invoc, string reply, string err)
        {
            if (err != null)
                return Response(NakamaFraming.OpToApplication, 1, "nakama: " + err, NakamaFraming.Op66Params(invoc, null));

            NakamaFraming.Op66Reply r;
            try
            {
                r = NakamaFraming.ParseOp66Reply(reply);
            }
            catch (FormatException e)
            {
                return Response(NakamaFraming.OpToApplication, 1, "nakama: bad uber_op66 reply: " + e.Message, NakamaFraming.Op66Params(invoc, null));
            }

            if (r.Disconnect)
                Debug(TDebugLevel.WARNING, "server will disconnect: " + r.Message, false);
            return Response(NakamaFraming.OpToApplication, r.ReturnCode, r.Message, NakamaFraming.Op66Params(invoc, r.Data));
        }

        bool SendJoin(Dictionary<byte, object> p)
        {
            if (_matchId != null || _joining)
            {
                Post(_gen, () => Response(NakamaFraming.OpJoin, NakamaFraming.RcAlreadyInRoom, "already in a room", null));
                return true;
            }

            string json = NakamaFraming.RoomJoinRequest(p);
            _joining = true;
            Interlocked.Add(ref _bytesOut, json.Length);
            Interlocked.Increment(ref _inFlight);

            int gen = _gen;
            _link.Rpc(NakamaFraming.RpcRoomJoin, json, (reply, err) =>
            {
                Interlocked.Decrement(ref _inFlight);
                Post(gen, () => OnRoomJoinReply(reply, err));
            });
            return true;
        }

        bool OnRoomJoinReply(string reply, string err)
        {
            if (!_joining)
                return false;

            NakamaFraming.RoomJoinReply r;
            if (err != null)
            {
                r = new NakamaFraming.RoomJoinReply { ReturnCode = NakamaFraming.RcTransport, Message = "nakama: " + err };
            }
            else
            {
                try { r = NakamaFraming.ParseRoomJoinReply(reply); }
                catch (FormatException e) { r = new NakamaFraming.RoomJoinReply { ReturnCode = NakamaFraming.RcTransport, Message = "nakama: bad uber_room_join reply: " + e.Message }; }
            }

            if (r.ReturnCode != 0)
            {
                _joining = false;
                return Response(NakamaFraming.OpJoin, r.ReturnCode, r.Message, null);
            }

            if (r.Number == NakamaFraming.RoomComm)
                _kind = KindComm;
            else if (r.Number == NakamaFraming.RoomLobby)
                _kind = KindLobby;
            _room = r.Number;

            string match = r.MatchId;
            _matchId = match;
            int gen = _gen;
            Interlocked.Increment(ref _inFlight);
            _link.JoinMatch(this, match, jerr =>
            {
                Interlocked.Decrement(ref _inFlight);
                Post(gen, () => OnJoinMatchDone(match, jerr));
            });
            return false;
        }

        bool OnJoinMatchDone(string match, string err)
        {
            if (err == null || match != _matchId)
                return false;

            bool waiting = _joining;
            _matchId = null;
            _joining = false;
            if (!waiting)
            {
                Debug(TDebugLevel.ERROR, "match " + match + " join failed after ack: " + err, false);
                return Status(StatusCode.DisconnectByServerLogic);
            }

            int rc = NakamaFraming.ParseRejectCode(err);
            return Response(NakamaFraming.OpJoin, rc > 0 ? rc : NakamaFraming.RcTransport, err, null);
        }

        bool SendLeave()
        {
            string match = _matchId;
            _matchId = null;
            _joining = false;

            int gen = _gen;
            if (match == null)
            {
                Post(gen, () => Response(NakamaFraming.OpLeave, 0, null, null));
                return true;
            }

            Interlocked.Increment(ref _inFlight);
            _link.LeaveMatch(this, match, err =>
            {
                Interlocked.Decrement(ref _inFlight);
                Post(gen, () => Response(NakamaFraming.OpLeave, 0, err, null));
            });
            return true;
        }

        void DropMatch()
        {
            string match = _matchId;
            _matchId = null;
            _joining = false;
            if (match != null)
                _link.LeaveMatch(this, match, err => { });
        }

        bool HandleMatchData(string matchId, long op, byte[] data)
        {
            if (matchId == null || matchId != _matchId)
                return false;

            if (op == NakamaFraming.OpJoin)
            {
                if (!_joining)
                    return false;
                _joining = false;

                NakamaFraming.JoinAck ack;
                if (!NakamaFraming.TryParseJoinAck(data, out ack))
                    return Response(NakamaFraming.OpJoin, NakamaFraming.RcTransport, "nakama: bad join ack", null);
                return Response(NakamaFraming.OpJoin, 0, null, NakamaFraming.JoinParams(ack));
            }

            if (op == NakamaFraming.MatchOpKicked)
            {
                // Nakama never sends the kicked session its own leave: this notice is the kick
                _matchId = null;
                _joining = false;
                _link.Detach(this);
                Debug(TDebugLevel.WARNING, "kicked: " + NakamaFraming.Utf8(data), false);
                return Status(StatusCode.DisconnectByServerLogic);
            }

            if (_joining)
                return false;

            if (op == NakamaFraming.EvStandard)
            {
                Dictionary<byte, object> p;
                try { p = NakamaFraming.DecodeEvent(data); }
                catch (FramingException e) { return Debug(TDebugLevel.WARNING, "event 0: " + e.Message, true); }
                return Event(NakamaFraming.EvStandard, p);
            }

            if (NakamaFraming.IsGameList(op))
                return Event((byte)op, NakamaFraming.WrapGameList((byte)op, data));

            return Debug(TDebugLevel.WARNING, "match op " + op + " ignored", true);
        }

        bool Status(StatusCode code)
        {
            bool terminal = IsTerminal(code);
            if (code == StatusCode.Connect)
                _state = PeerStateValue.Connected;

            try
            {
                if (Listener != null)
                    Listener.OnStatusChanged(code);
            }
            finally
            {
                if (terminal)
                {
                    _state = PeerStateValue.Disconnected;
                    _matchId = null;
                    _joining = false;
                    NewGeneration();
                }
            }
            return true;
        }

        bool Response(byte op, int rc, string message, Dictionary<byte, object> p)
        {
            if (Listener != null)
            {
                Listener.OnOperationResponse(new OperationResponse
                {
                    OperationCode = op,
                    ReturnCode = (short)rc,
                    DebugMessage = message,
                    Parameters = p ?? new Dictionary<byte, object>(),
                });
            }
            return true;
        }

        bool Event(byte code, Dictionary<byte, object> p)
        {
            if (Listener != null)
                Listener.OnEvent(new EventData { Code = code, Parameters = p });
            return true;
        }

        bool Debug(TDebugLevel level, string message, bool asItem)
        {
            if (Listener != null)
                Listener.DebugReturn(level, "[nakama " + _kind + "] " + message);
            return asItem;
        }

        static bool IsTerminal(StatusCode c)
        {
            switch (c)
            {
                case StatusCode.Disconnect:
                case StatusCode.DisconnectByServer:
                case StatusCode.DisconnectByServerLogic:
                case StatusCode.DisconnectByServerUserLimit:
                case StatusCode.TimeoutDisconnect:
                case StatusCode.Exception:
                case StatusCode.ExceptionOnConnect:
                case StatusCode.InternalReceiveException:
                    return true;
            }
            return false;
        }
    }
}
