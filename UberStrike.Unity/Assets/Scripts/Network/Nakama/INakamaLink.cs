using System;

namespace UberStrike.Realtime.NakamaAdapter
{
    // What NakamaPeer needs from the shared Nakama session. Callbacks may come on any thread.
    public interface INakamaLink
    {
        // auth + socket on first peer; ready fires once (sync or later)
        void Attach(ILinkPeer peer, string appName, Action<LinkResult> ready);

        // idempotent; socket closes after a grace once no peer is attached
        void Detach(ILinkPeer peer);

        // done(payload, null) or done(null, error)
        void Rpc(string id, string payload, Action<string, string> done);

        // routes match data of matchId to peer before the join is sent; done(null) or done(error)
        void JoinMatch(ILinkPeer peer, string matchId, Action<string> done);

        void LeaveMatch(ILinkPeer peer, string matchId, Action<string> done);

        bool Send(string matchId, long opCode, byte[] data);

        int ServerTimeMs { get; }
        int RoundTripTime { get; }
        int RoundTripTimeVariance { get; }

        void SyncClock();
    }

    public interface ILinkPeer
    {
        void OnMatchData(string matchId, long opCode, byte[] data);

        // own presence left the match without us asking (MatchKick, match ended)
        void OnMatchLeft(string matchId);

        // joined game room (not lobby 66 / comm 88): link sends the op 82 [2][4] heartbeat
        bool InGameRoom(string matchId);

        // socket gone; byServer = not closed by us
        void OnLinkClosed(bool byServer, string reason);
    }

    public enum LinkFailure
    {
        None = 0,
        AuthRejected = 1,
        NoToken = 2,
        Unreachable = 3,
        Timeout = 4,
        Tls = 5,
        Closed = 6,
    }

    public struct LinkResult
    {
        public LinkFailure Failure;
        public string Message;

        public bool Ok { get { return Failure == LinkFailure.None; } }

        public static LinkResult Success { get { return new LinkResult(); } }

        public static LinkResult Fail(LinkFailure f, string message)
        {
            return new LinkResult { Failure = f, Message = message };
        }
    }
}
