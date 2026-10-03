using Cmune.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;

namespace UberStrike.Realtime.NakamaAdapter
{
    // Play page rows for the Nakama node. Addresses are labels (CmuneRoomID needs IPv4:port); peers dial NakamaConfig.
    public static class NakamaServerList
    {
        public const int GameServerId = 1;
        public const int CommServerId = 2;

        static NakamaConfig Cfg
        {
            get { return NakamaConfig.Current ?? (NakamaConfig.Current = NakamaBootstrap.LoadConfig()); }
        }

        public static bool Enabled { get { return Cfg.ReplaceServerList; } }
        public static string GameAddress { get { return Cfg.GameServerAddress; } }
        public static string CommAddress { get { return Cfg.CommServerAddress; } }

        public static PhotonView GameServer()
        {
            return new PhotonView
            {
                PhotonId = GameServerId,
                IP = Cfg.RoomHost,
                Port = Cfg.RoomPortBase,
                Name = "Nakama " + Cfg.Host + ":" + Cfg.Port,
                UsageType = PhotonUsageType.All,
            };
        }

        public static PhotonView CommServer()
        {
            return new PhotonView
            {
                PhotonId = CommServerId,
                IP = Cfg.RoomHost,
                Port = Cfg.RoomPortBase + 88,
                Name = "Nakama Comm " + Cfg.Host + ":" + Cfg.Port,
                UsageType = PhotonUsageType.CommServer,
            };
        }

        public static string KindOf(string server)
        {
            return server == Cfg.CommServerAddress ? NakamaPeer.KindComm : NakamaPeer.KindGame;
        }
    }
}
