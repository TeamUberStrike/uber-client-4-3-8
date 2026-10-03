using System;
using System.Collections.Generic;
using System.Globalization;

namespace UberStrike.Realtime.NakamaAdapter
{
    // Q3: defaults < StreamingAssets/nakama.json < command line (-nakama, -nakamakey, -nakamadev, -nakamatoken).
    public sealed class NakamaConfig
    {
        public string Scheme = "http";
        public string Host = "127.0.0.1";
        public int Port = 7350;
        public string ServerKey = "defaultkey";

        // dev token dev:<cmid>:<access>:<name>; node must run UBER_DEV_AUTH=true
        public bool DevAuth;

        // web token handed in on the command line (testing the UBER_TOKEN_CHECK_URL path)
        public string Token;

        // fake but stable IPv4:port labels (Go UBER_ROOM_HOST / UBER_ROOM_PORT_BASE), never dialed
        public string RoomHost = "127.0.0.1";
        public int RoomPortBase = 20000;

        public bool ReplaceServerList = true;
        public int TimePingMs = 1000;
        public int ConnectTimeoutSec = 10;
        public int CloseGraceMs = 10000;

        public string Source = "default";

        public static NakamaConfig Current;

        public string Endpoint { get { return Scheme + "://" + Host + ":" + Port.ToString(CultureInfo.InvariantCulture); } }
        public string GameServerAddress { get { return RoomHost + ":" + RoomPortBase.ToString(CultureInfo.InvariantCulture); } }
        public string CommServerAddress { get { return RoomHost + ":" + (RoomPortBase + 88).ToString(CultureInfo.InvariantCulture); } }

        // Comm label by port: the SDK re-renders the host (IPv4 int, hostname -> 0.0.0.0). Game ports skip +88 (Go rooms.Alloc).
        public bool IsCommAddress(string server)
        {
            if (string.IsNullOrEmpty(server))
                return false;
            if (server == CommServerAddress)
                return true;
            int colon = server.LastIndexOf(':');
            int port;
            return colon >= 0 && int.TryParse(server.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port)
                && port == RoomPortBase + 88;
        }

        public static NakamaConfig Load(string json, string[] args)
        {
            var c = new NakamaConfig();
            if (!string.IsNullOrEmpty(json))
            {
                c.ApplyJson(json);
                c.Source = "nakama.json";
            }
            if (args != null && c.ApplyArgs(args))
                c.Source = c.Source == "default" ? "command line" : c.Source + " + command line";
            c.Validate();
            return c;
        }

        // scheme://host:port
        public bool TrySetEndpoint(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            int s = url.IndexOf("://", StringComparison.Ordinal);
            if (s <= 0)
                return false;
            string scheme = url.Substring(0, s).ToLowerInvariant();
            if (scheme != "http" && scheme != "https")
                return false;

            string rest = url.Substring(s + 3).TrimEnd('/');
            int port = scheme == "https" ? 443 : 7350;
            string host = rest;
            int colon = rest.LastIndexOf(':');
            if (colon > 0 && rest.IndexOf(']') < colon)
            {
                if (!int.TryParse(rest.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) || port <= 0 || port > 65535)
                    return false;
                host = rest.Substring(0, colon);
            }
            if (host.Length == 0 || host.IndexOf('/') >= 0)
                return false;

            Scheme = scheme;
            Host = host;
            Port = port;
            return true;
        }

        void ApplyJson(string json)
        {
            Dictionary<string, object> o = NakamaJson.ParseObject(json);
            string v;
            if ((v = NakamaJson.Str(o, "scheme")) != null) Scheme = v.ToLowerInvariant();
            if ((v = NakamaJson.Str(o, "host")) != null) Host = v;
            if ((v = NakamaJson.Str(o, "serverKey")) != null) ServerKey = v;
            if ((v = NakamaJson.Str(o, "roomHost")) != null) RoomHost = v;
            if ((v = NakamaJson.Str(o, "endpoint")) != null) TrySetEndpoint(v);
            Port = (int)NakamaJson.Long(o, "port", Port);
            RoomPortBase = (int)NakamaJson.Long(o, "roomPortBase", RoomPortBase);
            TimePingMs = (int)NakamaJson.Long(o, "timePingMs", TimePingMs);
            ConnectTimeoutSec = (int)NakamaJson.Long(o, "connectTimeoutSec", ConnectTimeoutSec);
            CloseGraceMs = (int)NakamaJson.Long(o, "closeGraceMs", CloseGraceMs);
            if (o.ContainsKey("devAuth")) DevAuth = NakamaJson.Bool(o, "devAuth");
            if (o.ContainsKey("replaceServerList")) ReplaceServerList = NakamaJson.Bool(o, "replaceServerList");
        }

        bool ApplyArgs(string[] a)
        {
            bool any = false;
            for (int i = 0; i < a.Length; i++)
            {
                string k = a[i].ToLowerInvariant();
                string next = i + 1 < a.Length ? a[i + 1] : null;
                switch (k)
                {
                    case "-nakama":
                        if (next != null && TrySetEndpoint(next)) { any = true; i++; }
                        break;
                    case "-nakamakey":
                        if (next != null) { ServerKey = next; any = true; i++; }
                        break;
                    case "-nakamatoken":
                        if (next != null) { Token = next; any = true; i++; }
                        break;
                    case "-nakamadev":
                        DevAuth = true;
                        any = true;
                        break;
                }
            }
            return any;
        }

        void Validate()
        {
            if (Scheme != "http" && Scheme != "https") Scheme = "http";
            if (Port <= 0 || Port > 65535) Port = 7350;
            if (RoomPortBase <= 0 || RoomPortBase > 65535 - 88) RoomPortBase = 20000;
            if (TimePingMs < 100) TimePingMs = 100;
            if (ConnectTimeoutSec < 1) ConnectTimeoutSec = 10;
            if (CloseGraceMs < 0) CloseGraceMs = 0;
            if (string.IsNullOrEmpty(Host)) Host = "127.0.0.1";
            if (string.IsNullOrEmpty(RoomHost)) RoomHost = "127.0.0.1";
        }

        public override string ToString()
        {
            return Endpoint + " devAuth=" + DevAuth + " rooms=" + GameServerAddress + " (" + Source + ")";
        }
    }
}
