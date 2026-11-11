using System.Text;
using System.Text.RegularExpressions;

namespace UberStrike.Realtime.NakamaAdapter
{
    // webgl dev log (?devlog=1): only [nakama] lines + errors leave the device, secrets and ids scrubbed first
    public static class ClientLogFilter
    {
        public const int MaxLine = 500;

        static readonly Regex On = new Regex(@"[?&]devlog=1(?:[&#]|$)");
        static readonly Regex Ip = new Regex(@"\b(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\b");
        static readonly Regex[] Rules =
        {
            new Regex(@"eyJ[\w-]+\.[\w-]+\.[\w-]*"),
            new Regex(@"(?i)\bbearer\s+\S+"),
            new Regex(@"(?i)(""?\b\w*(?:token|passw\w*|pwd|secret|key|(?<!dev)auth\w*|ticket|session\w*|cookie|credential\w*)""?\s*[:=]\s*)(""[^""]*""|[^\s,;&}]+)"),
            new Regex(@"dev:\d+:\d+:[^""\r\n,)]*"),
            new Regex(@"[\w.+-]+@[\w-]+(?:\.[\w-]+)+"),
            new Regex(@"(https?://[^\s?""#]+)\?[^\s""#]*"),
            new Regex(@"\b7656119\d{10}\b"),
            new Regex(@"(?i)(\bcmid\s*[:=#]?\s*)\d+"),
            new Regex(@"(?i)(identity changed )\d+ -> \d+"),
            new Regex(@"[A-Za-z0-9+/_-]{32,}={0,2}"),
        };
        static readonly string[] With =
        {
            "<jwt>", "Bearer <redacted>", "$1<redacted>", "<devtoken>", "<email>", "$1?<redacted>", "<steamid>", "$1<n>",
            "$1<n> -> <n>", "<redacted>",
        };

        public static bool Enabled(string pageUrl)
        {
            return !string.IsNullOrEmpty(pageUrl) && On.IsMatch(pageUrl);
        }

        public static bool Wanted(string msg, bool error)
        {
            return !string.IsNullOrEmpty(msg) && (error || msg.StartsWith("[nakama]"));
        }

        // pageHost: the server that served the page, kept (it already knows itself)
        public static string Scrub(string msg, string pageHost)
        {
            if (string.IsNullOrEmpty(msg))
                return "";
            string s = msg.Length > MaxLine * 2 ? msg.Substring(0, MaxLine * 2) : msg;
            for (int i = 0; i < Rules.Length; i++)
                s = Rules[i].Replace(s, With[i]);
            s = Ip.Replace(s, m => m.Value == pageHost || PrivateIp(m) ? m.Value : "<ip>");
            return s.Length > MaxLine ? s.Substring(0, MaxLine) + "..." : s;
        }

        static bool PrivateIp(Match m)
        {
            int a = int.Parse(m.Groups[1].Value), b = int.Parse(m.Groups[2].Value);
            return a == 10 || a == 127 || (a == 172 && b >= 16 && b <= 31) || (a == 192 && b == 168) || (a == 169 && b == 254)
                || m.Value == "0.0.0.0";
        }

        public static string Json(float t, string type, string msg)
        {
            var sb = new StringBuilder(msg.Length + 48);
            sb.Append("{\"t\":").Append(t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(",\"type\":\"").Append(type).Append("\",\"msg\":\"");
            foreach (char c in msg)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            return sb.Append("\"}").ToString();
        }
    }
}
