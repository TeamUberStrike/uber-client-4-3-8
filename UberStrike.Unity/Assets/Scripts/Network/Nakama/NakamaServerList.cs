using Cmune.Core.Models.Views;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

namespace UberStrike.Realtime.NakamaAdapter
{
    // AuthenticateApplication -> endpoint + Play page + comm row. Truth = web DB CommServer row.
    public static class NakamaServerList
    {
        static NakamaConfig Cfg
        {
            get { return NakamaConfig.Current ?? (NakamaConfig.Current = NakamaBootstrap.LoadConfig()); }
        }

        // placeholder CmuneRoomID address until the web row arrives
        public static string Label { get { return Cfg.Label; } }

        public static void Apply(AuthenticateApplicationView ev)
        {
            NakamaConfig cfg = Cfg;
#if UNITY_ANDROID || UNITY_IPHONE
            const bool mobile = true;
#else
            const bool mobile = false;
#endif
            NakamaNodeRows rows = null;
            if (!cfg.ReplaceServerList)
            {
                rows = NakamaNodeRows.Pick(ev != null ? ev.CommServer : null, ev != null ? ev.GameServers : null, mobile);
                foreach (string w in rows.Warnings)
                    Debug.LogWarning("[nakama] " + w);
                if (rows.Node == null)
                {
                    Debug.LogError("[nakama] " + rows.Error + ". Fix the PhotonServers row. Using " + cfg.Endpoint + " (" + cfg.Origin + ")");
                    rows = null;
                }
                else if (!cfg.ApplyNode(rows.Node.IP, rows.Node.Port, NakamaConfig.UrlHost(global::UberStrike.WebService.Unity.Configuration.WebserviceBaseUrl), "web row #" + rows.Node.PhotonId))
                {
                    Debug.LogError("[nakama] bad node row " + rows.Address);
                    rows = null;
                }
                else if (cfg.TlsFrom != null)
                {
                    Debug.Log("[nakama] row " + rows.Address + " -> " + cfg.Endpoint + " (TLS name from " + cfg.TlsFrom
                        + "). Nakama not under the web host name: set tlsHost in StreamingAssets/nakama.json");
                }
            }
            if (rows == null)
                rows = NakamaNodeRows.FromConfig(cfg);

            NakamaSession.Instance.Retarget();

            foreach (PhotonView v in rows.Game)
                GameServerManager.Instance.AddGameServer(v);
            CmuneNetworkManager.CurrentCommServer = new GameServerView(rows.Node);
        }
    }
}
