using System;
using System.Collections.Generic;
using System.Globalization;
using Cmune.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;

namespace UberStrike.Realtime.NakamaAdapter
{
    public sealed class NakamaHostRow
    {
        public string HostId { get; private set; }
        public string Endpoint { get; private set; }
        public string MatchId { get; private set; }
        public string Meta { get; private set; }
        public string Name { get; private set; }
        public string Region { get; private set; }
        public int Players { get; private set; }
        public int MaxPlayers { get; private set; }
        public bool Unranked { get { return true; } }
        public string Key { get { return Endpoint + "/" + MatchId; } }

        internal static NakamaHostRow Parse(Dictionary<string, object> o)
        {
            if (o == null || !NakamaJson.Bool(o, "unranked"))
                throw new FormatException("host row must be unranked");
            var r = new NakamaHostRow
            {
                HostId = NakamaJson.Str(o, "host_id"),
                Endpoint = NakamaJson.Str(o, "endpoint"),
                MatchId = NakamaJson.Str(o, "match_id"),
                Meta = NakamaJson.Str(o, "meta"),
                Name = NakamaJson.Str(o, "name"),
                Region = NakamaJson.Str(o, "region"),
            };
            if (string.IsNullOrWhiteSpace(r.HostId) || r.HostId.Length > 128 ||
                string.IsNullOrWhiteSpace(r.MatchId) || r.MatchId.Length > 256 ||
                string.IsNullOrEmpty(r.Meta) || r.Meta.Length > 8192)
                throw new FormatException("invalid host identity or metadata");
            if (Convert.FromBase64String(r.Meta).Length == 0)
                throw new FormatException("empty host metadata");
            long players = NakamaJson.Long(o, "players", 0);
            long max = NakamaJson.Long(o, "max_players", 0);
            if (players < 0 || max < 0 || max > 16 || players > max)
                throw new FormatException("invalid host player count");
            r.Players = (int)players;
            r.MaxPlayers = (int)max;
            r.Endpoint = NakamaConfig.FromHost(r).Endpoint;
            return r;
        }
    }

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

        public readonly List<NakamaHostRow> Hosts = new List<NakamaHostRow>();

        public readonly List<string> Warnings = new List<string>();
        public string Error;

        public string Address { get { return Node == null ? null : Key(Node); } }

        public void ApplyHosts(string json)
        {
            Dictionary<string, object> o = NakamaJson.ParseObject(json);
            object value;
            var hosts = o.TryGetValue("hosts", out value) ? value as List<object> : null;
            if (hosts == null || hosts.Count > 1000)
                throw new FormatException("host_list must contain at most 1000 hosts");
            var rows = new List<NakamaHostRow>();
            var keys = new HashSet<string>();
            foreach (object host in hosts)
            {
                NakamaHostRow row = NakamaHostRow.Parse(host as Dictionary<string, object>);
                if (!keys.Add(row.Key))
                    throw new FormatException("duplicate host room");
                rows.Add(row);
            }
            Hosts.Clear();
            Hosts.AddRange(rows);
        }

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
