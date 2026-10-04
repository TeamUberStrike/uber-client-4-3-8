using System;
using System.Collections.Generic;
using System.Globalization;

namespace UberStrike.Realtime.NakamaAdapter
{
    // Endpoint truth = web DB CommServer row (ApplyNode at AuthenticateApplication).
    // Release: nakama.json tlsHost (443 row, Nakama not under the web host name).
    // Dev overrides: defaults < StreamingAssets/nakama.json < command line (-nakama, -nakamakey, -nakamadev, -nakamatoken).
    public sealed class NakamaConfig
    {
        public const string OriginDefault = "default";
        public const string OriginJson = "nakama.json";
        public const string OriginArgs = "command line";

        public string Scheme = "http";
        public string Host = "127.0.0.1";
        public int Port = 7350;
        public string ServerKey = "defaultkey";

        // dev token dev:<cmid>:<access>:<name>; node must run UBER_DEV_AUTH=true
        public bool DevAuth;

        // web token handed in on the command line (testing the UBER_TOKEN_CHECK_URL path)
        public string Token;

        // json endpoint/host/port or -nakama: dial target fixed, DB row ignored
        public bool Pinned;

        // json scheme: kept by ApplyNode
        public bool SchemePinned;

        // RELEASE: TLS name when the row port is 443 (row holds an IPv4 only). Unset = web URL host name.
        public string TlsHost;

        // where the https host came from: "tlsHost", "web URL host", "row IP"; null = http
        public string TlsFrom;

        // dev: Play page from this config, no DB row needed
        public bool ReplaceServerList;

        public int TimePingMs = 1000;
        public int ConnectTimeoutSec = 10;
        public int CloseGraceMs = 10000;

        // where the config came from / where the dial target came from
        public string Source = OriginDefault;
        public string Origin = OriginDefault;

        // node row ip:port (CmuneRoomID address); null = not seen yet
        string _node;

        public static NakamaConfig Current;

        public string Endpoint { get { return Scheme + "://" + Host + ":" + Port.ToString(CultureInfo.InvariantCulture); } }

        // IPv4:port for CmuneRoomID / Play rows: node row, else Host:Port (127.0.0.1 when Host is a name)
        public string Label
        {
            get
            {
                if (_node != null)
                    return _node;
                return (IsIPv4(Host) ? Host : "127.0.0.1") + ":" + Port.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static NakamaConfig Load(string json, string[] args)
        {
            var c = new NakamaConfig();
            if (!string.IsNullOrEmpty(json))
            {
                if (c.ApplyJson(json))
                    c.Origin = OriginJson;
                c.Source = OriginJson;
            }
            if (args != null && c.ApplyArgs(args))
                c.Source = c.Source == OriginDefault ? OriginArgs : c.Source + " + " + OriginArgs;
            c.Validate();
            return c;
        }

        // Web row -> dial target. Pinned: label only. 443 = https (TlsHost, else web host name, else ip). False = bad row.
        public bool ApplyNode(string ip, int port, string webHost, string origin)
        {
            if (!IsIPv4(ip) || port <= 0 || port > 65535)
                return false;

            _node = ip + ":" + port.ToString(CultureInfo.InvariantCulture);
            if (Pinned)
                return true;

            bool tls = SchemePinned ? Scheme == "https" : port == 443;
            string host = ip;
            TlsFrom = null;
            if (tls)
            {
                string web = HostName(webHost);
                host = NonEmpty(TlsHost) ?? web ?? ip;
                TlsFrom = NonEmpty(TlsHost) != null ? "tlsHost" : web != null ? "web URL host" : "row IP";
            }
            Scheme = tls ? "https" : "http";
            Host = host;
            Port = port;
            Origin = (origin ?? "web row") + (TlsFrom != null ? ", TLS name from " + TlsFrom : "");
            return true;
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

        // dotted IPv4, first octet > 0 (SDK ConnectionAddress.IsValid)
        public static bool IsIPv4(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            string[] p = s.Split('.');
            if (p.Length != 4)
                return false;
            for (int i = 0; i < 4; i++)
            {
                byte b;
                if (p[i].Length == 0 || p[i].Length > 3 || !byte.TryParse(p[i], NumberStyles.None, CultureInfo.InvariantCulture, out b))
                    return false;
                if (i == 0 && b == 0)
                    return false;
            }
            return true;
        }

        // host part of a URL ("http://web.example:5000/x" -> "web.example")
        public static string UrlHost(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
            int s = url.IndexOf("://", StringComparison.Ordinal);
            string rest = s >= 0 ? url.Substring(s + 3) : url;
            int end = rest.IndexOfAny(new[] { '/', '?', '#' });
            if (end >= 0)
                rest = rest.Substring(0, end);
            int at = rest.LastIndexOf('@');
            if (at >= 0)
                rest = rest.Substring(at + 1);
            int colon = rest.LastIndexOf(':');
            if (colon >= 0 && rest.IndexOf(']') < colon)
                rest = rest.Substring(0, colon);
            return rest.Length > 0 ? rest : null;
        }

        static string HostName(string host)
        {
            host = NonEmpty(host);
            return host == null || IsIPv4(host) || host.IndexOf(':') >= 0 ? null : host;
        }

        static string NonEmpty(string s)
        {
            return string.IsNullOrEmpty(s) ? null : s.Trim();
        }

        // true = dial target pinned
        bool ApplyJson(string json)
        {
            Dictionary<string, object> o = NakamaJson.ParseObject(json);
            string v;
            if ((v = NakamaJson.Str(o, "scheme")) != null) { Scheme = v.ToLowerInvariant(); SchemePinned = true; }
            if ((v = NakamaJson.Str(o, "host")) != null) { Host = v; Pinned = true; }
            if ((v = NakamaJson.Str(o, "serverKey")) != null) ServerKey = v;
            if ((v = NakamaJson.Str(o, "tlsHost")) != null) TlsHost = NonEmpty(v);
            if ((v = NakamaJson.Str(o, "endpoint")) != null && TrySetEndpoint(v)) { Pinned = true; SchemePinned = true; }
            if (o.ContainsKey("port")) { Port = (int)NakamaJson.Long(o, "port", Port); Pinned = true; }
            TimePingMs = (int)NakamaJson.Long(o, "timePingMs", TimePingMs);
            ConnectTimeoutSec = (int)NakamaJson.Long(o, "connectTimeoutSec", ConnectTimeoutSec);
            CloseGraceMs = (int)NakamaJson.Long(o, "closeGraceMs", CloseGraceMs);
            if (o.ContainsKey("devAuth")) DevAuth = NakamaJson.Bool(o, "devAuth");
            if (o.ContainsKey("replaceServerList")) ReplaceServerList = NakamaJson.Bool(o, "replaceServerList");
            return Pinned;
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
                        if (next != null && TrySetEndpoint(next))
                        {
                            Pinned = true;
                            SchemePinned = true;
                            Origin = OriginArgs;
                            any = true;
                            i++;
                        }
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
            if (TimePingMs < 100) TimePingMs = 100;
            if (ConnectTimeoutSec < 1) ConnectTimeoutSec = 10;
            if (CloseGraceMs < 0) CloseGraceMs = 0;
            if (string.IsNullOrEmpty(Host)) Host = "127.0.0.1";
        }

        public override string ToString()
        {
            return Endpoint + " (" + Origin + (Pinned ? ", pinned" : "") + ") devAuth=" + DevAuth
                + " replaceServerList=" + ReplaceServerList + " config=" + Source;
        }
    }
}
