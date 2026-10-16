using System;
using System.Collections.Generic;
using Cmune.Realtime.Photon.Client.Transport;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // Scripted INakamaLink: every async op waits until the test completes it.
    sealed class FakeLink : INakamaLink
    {
        public sealed class RpcCall
        {
            public string Id;
            public string Payload;
            public Action<string, string> Done;
        }

        public sealed class SendCall
        {
            public string MatchId;
            public long Op;
            public byte[] Data;
        }

        public readonly Dictionary<ILinkPeer, Action<LinkResult>> Waiting = new Dictionary<ILinkPeer, Action<LinkResult>>();
        public readonly HashSet<ILinkPeer> Attached = new HashSet<ILinkPeer>();
        public readonly List<string> AppNames = new List<string>();
        public readonly List<RpcCall> Rpcs = new List<RpcCall>();
        public readonly List<KeyValuePair<string, Action<string>>> Joins = new List<KeyValuePair<string, Action<string>>>();
        public readonly List<KeyValuePair<string, Action<string>>> Leaves = new List<KeyValuePair<string, Action<string>>>();
        public readonly List<SendCall> Sent = new List<SendCall>();
        public readonly Dictionary<string, ILinkPeer> Routes = new Dictionary<string, ILinkPeer>();
        public bool Connected = true;
        public bool ReadyImmediately;
        public int SyncRequests;
        public int ServerTimeMs { get; set; }
        public int RoundTripTime { get; set; }
        public int RoundTripTimeVariance { get; set; }

        public void Attach(ILinkPeer peer, string appName, Action<LinkResult> ready)
        {
            Attached.Add(peer);
            AppNames.Add(appName);
            if (ReadyImmediately)
                ready(LinkResult.Success);
            else
                Waiting[peer] = ready;
        }

        public void Detach(ILinkPeer peer)
        {
            Attached.Remove(peer);
            Waiting.Remove(peer);
        }

        public void Complete(ILinkPeer peer, LinkResult r)
        {
            Action<LinkResult> a = Waiting[peer];
            Waiting.Remove(peer);
            a(r);
        }

        public void Rpc(string id, string payload, Action<string, string> done)
        {
            Rpcs.Add(new RpcCall { Id = id, Payload = payload, Done = done });
        }

        public RpcCall LastRpc(string id)
        {
            for (int i = Rpcs.Count - 1; i >= 0; i--)
                if (Rpcs[i].Id == id)
                    return Rpcs[i];
            throw new AssertException("no rpc " + id);
        }

        public void JoinMatch(ILinkPeer peer, string matchId, Action<string> done)
        {
            Routes[matchId] = peer;
            Joins.Add(new KeyValuePair<string, Action<string>>(matchId, done));
        }

        public void LeaveMatch(ILinkPeer peer, string matchId, Action<string> done)
        {
            Routes.Remove(matchId);
            Leaves.Add(new KeyValuePair<string, Action<string>>(matchId, done));
        }

        public bool Send(string matchId, long opCode, byte[] data)
        {
            if (!Connected)
                return false;
            Sent.Add(new SendCall { MatchId = matchId, Op = opCode, Data = data });
            return true;
        }

        public void SyncClock()
        {
            SyncRequests++;
        }

        public void Deliver(string matchId, long op, byte[] data)
        {
            Routes[matchId].OnMatchData(matchId, op, data);
        }
    }

    // Records callbacks; fails if one arrives while the peer is inside Connect/Disconnect/SendOperation.
    sealed class RecordingListener : INetworkPeerListener
    {
        public readonly List<StatusCode> Statuses = new List<StatusCode>();
        public readonly List<OperationResponse> Responses = new List<OperationResponse>();
        public readonly List<EventData> Events = new List<EventData>();
        public readonly List<string> Debug = new List<string>();
        public readonly List<PeerStateValue> StateAtStatus = new List<PeerStateValue>();
        public INetworkPeer Peer;
        public bool InCall;
        public Action<StatusCode> OnStatus;

        public void DebugReturn(DebugLevel level, string message)
        {
            Debug.Add(level + " " + message);
        }

        public void OnOperationResponse(OperationResponse r)
        {
            A.True(!InCall, "callback inside a peer call");
            Responses.Add(r);
        }

        public void OnStatusChanged(StatusCode s)
        {
            A.True(!InCall, "status callback inside a peer call");
            Statuses.Add(s);
            if (Peer != null)
                StateAtStatus.Add(Peer.PeerState);
            if (OnStatus != null)
                OnStatus(s);
        }

        public void OnEvent(EventData e)
        {
            A.True(!InCall, "event callback inside a peer call");
            Events.Add(e);
        }

        public int Pump(INetworkPeer p)
        {
            int n = 0;
            while (p.DispatchIncomingCommands())
                n++;
            return n;
        }

        public T Call<T>(Func<T> f)
        {
            InCall = true;
            try { return f(); }
            finally { InCall = false; }
        }

        public void Call(Action f)
        {
            InCall = true;
            try { f(); }
            finally { InCall = false; }
        }
    }
}
