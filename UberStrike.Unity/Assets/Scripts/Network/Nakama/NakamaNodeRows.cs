using System.Collections.Generic;
using System.Globalization;
using Cmune.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;

namespace UberStrike.Realtime.NakamaAdapter
{
    // Web AuthenticateApplication rows -> the Nakama node + Play page rows. No UnityEngine (tests).
    public sealed class NakamaNodeRows
    {
        public const int ConfigGameId = 1;
        public const int ConfigCommId = 2;
        public const string DefaultName = "Nakama";

        // node row (UsageType CommServer); null = config mode
        public PhotonView Node;

        // Play page rows (UsageType All)
        public readonly List<PhotonView> Game = new List<PhotonView>();

        public readonly List<string> Warnings = new List<string>();
        public string Error;

        public string Address { get { return Node == null ? null : Key(Node); } }

        // node = comm row, else first non-Mobile game row. Play rows = game rows at the node address, else a node clone.
        public static NakamaNodeRows Pick(PhotonView comm, IList<PhotonView> games, bool mobile)
        {
            var r = new NakamaNodeRows();
            PhotonView node = comm;
            if (node == null && games != null)
                foreach (PhotonView g in games)
                    if (g != null && (mobile || g.UsageType != PhotonUsageType.Mobile))
                    {
                        node = g;
                        break;
                    }

            if (node == null)
            {
                r.Error = "web sent no CommServer row (UsageType 6) and no game row";
                return r;
            }
            if (!Valid(node))
            {
                r.Error = "web node row #" + node.PhotonId + " '" + node.IP + ":" + node.Port + "' is not IPv4:port";
                return r;
            }

            r.Node = Clone(node, PhotonUsageType.CommServer);
            string at = Key(node);
            if (games != null)
            {
                foreach (PhotonView g in games)
                {
                    if (g == null || (!mobile && g.UsageType == PhotonUsageType.Mobile))
                        continue;
                    if (Valid(g) && Key(g) == at)
                        r.Game.Add(Clone(g, PhotonUsageType.All));
                    else
                        r.Warnings.Add("skipped row #" + g.PhotonId + " " + g.IP + ":" + g.Port + ": not the Nakama node " + at);
                }
            }
            if (r.Game.Count == 0)
                r.Game.Add(Clone(node, PhotonUsageType.All));
            return r;
        }

        // dev / no DB row: one game + one comm row at the config label
        public static NakamaNodeRows FromConfig(NakamaConfig cfg)
        {
            var r = new NakamaNodeRows();
            string label = cfg.Label;
            int colon = label.LastIndexOf(':');
            string ip = label.Substring(0, colon);
            int port = int.Parse(label.Substring(colon + 1), CultureInfo.InvariantCulture);
            string name = DefaultName + " " + cfg.Host + ":" + cfg.Port.ToString(CultureInfo.InvariantCulture);
            r.Node = new PhotonView { PhotonId = ConfigCommId, IP = ip, Port = port, Name = name, UsageType = PhotonUsageType.CommServer };
            r.Game.Add(new PhotonView { PhotonId = ConfigGameId, IP = ip, Port = port, Name = name, UsageType = PhotonUsageType.All });
            return r;
        }

        static bool Valid(PhotonView v)
        {
            return NakamaConfig.IsIPv4(v.IP) && v.Port > 0 && v.Port <= 65535;
        }

        static string Key(PhotonView v)
        {
            return v.IP + ":" + v.Port.ToString(CultureInfo.InvariantCulture);
        }

        static PhotonView Clone(PhotonView v, PhotonUsageType usage)
        {
            return new PhotonView
            {
                PhotonId = v.PhotonId,
                IP = v.IP,
                Port = v.Port,
                Name = string.IsNullOrEmpty(v.Name) ? DefaultName : v.Name,
                Region = v.Region,
                UsageType = usage,
                MinLatency = v.MinLatency,
            };
        }
    }
}
