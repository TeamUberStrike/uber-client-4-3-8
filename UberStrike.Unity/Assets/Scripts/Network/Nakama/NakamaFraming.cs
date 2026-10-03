using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UberStrike.Realtime.NakamaAdapter
{
    // Photon op/param dictionaries <-> Nakama match data + RPC JSON. BCL only (unit tested in plain .NET).
    public static class NakamaFraming
    {
        public const byte OpToApplication = 66;
        public const byte OpToPlayer = 80;
        public const byte OpToAll = 81;
        public const byte OpToServer = 82;
        public const byte OpToOthers = 83;
        public const byte OpJoin = 88;
        public const byte OpLeave = 89;

        // server -> client match op: kick notice before MatchKick, payload = reason (Go game.OpKicked)
        public const long MatchOpKicked = 89;

        // ServerSyncCenter InitializeRoom no-op = game room liveness (Go roomcore.SyncInitRoom)
        public const int RoomLobby = 66;
        public const int RoomComm = 88;

        public const short ClassServerSync = 2;
        public const byte SyncInitRoom = 4;

        public const byte EvStandard = 0;
        public const byte EvGameListInit = 3;
        public const byte EvGameListUpdate = 4;
        public const byte EvGameListRemoval = 5;

        public const byte KeyGameId = 4;
        public const byte KeyActorNr = 9;
        public const byte KeyActors = 11;
        public const byte KeyData = 42;
        public const byte KeyInvocationId = 61;
        public const byte KeyMethodId = 100;
        public const byte KeyInstanceId = 101;
        public const byte KeyActorId = 102;
        public const byte KeyBytes = 103;
        public const byte KeyRoomId = 120;
        public const byte KeyLobbyRoomUpdate = 122;
        public const byte KeyLobbyRoomDelete = 123;
        public const byte KeyInitRoom = 200;
        public const byte KeyServerTicks = 201;
        public const byte KeyAccessLevel = 205;
        public const byte KeyCmid = 206;

        public const string RpcOp66 = "uber_op66";
        public const string RpcRoomJoin = "uber_room_join";
        public const string RpcTime = "uber_time";

        public const int JoinAckSize = 25;
        public const int RoomIdSize = 12;

        // rc the adapter uses when Nakama itself failed (Go RcInternal).
        public const int RcTransport = 5;
        public const int RcAlreadyInRoom = 4;

        public static bool IsRoomOp(byte op)
        {
            return op == OpToPlayer || op == OpToAll || op == OpToServer || op == OpToOthers;
        }

        // op 80: [int16 netId][u8 method][int32 target][args]; 81/82/83: no target.
        public static byte[] EncodeEnvelope(byte op, IDictionary<byte, object> p)
        {
            if (!IsRoomOp(op))
                throw new FramingException("op " + op + " is not a room op");

            short netId = Get<short>(p, KeyInstanceId);
            byte method = Get<byte>(p, KeyMethodId);
            byte[] args = GetBytesOrEmpty(p, KeyBytes);
            int head = op == OpToPlayer ? 7 : 3;

            byte[] b = new byte[head + args.Length];
            b[0] = (byte)netId;
            b[1] = (byte)(netId >> 8);
            b[2] = method;
            if (op == OpToPlayer)
                WriteInt32(b, 3, Get<int>(p, KeyActorId));
            Buffer.BlockCopy(args, 0, b, head, args.Length);
            return b;
        }

        public static string Utf8(byte[] b)
        {
            return b == null || b.Length == 0 ? "" : System.Text.Encoding.UTF8.GetString(b);
        }

        // op 82 [2][4], no args
        public static byte[] Heartbeat()
        {
            return new byte[] { (byte)ClassServerSync, (byte)(ClassServerSync >> 8), SyncInitRoom };
        }

        // match data op 0 -> event 0 {101 short, 100 byte, 103 byte[]}
        public static Dictionary<byte, object> DecodeEvent(byte[] data)
        {
            if (data == null || data.Length < 3)
                throw new FramingException("event 0 shorter than 3 bytes");

            byte[] args = new byte[data.Length - 3];
            Buffer.BlockCopy(data, 3, args, 0, args.Length);
            return new Dictionary<byte, object>
            {
                { KeyInstanceId, (short)(data[0] | (data[1] << 8)) },
                { KeyMethodId, data[2] },
                { KeyBytes, args },
            };
        }

        // match data op 3/4/5 -> {42: {122|123: bytes}}
        public static Dictionary<byte, object> WrapGameList(byte code, byte[] data)
        {
            byte key = code == EvGameListRemoval ? KeyLobbyRoomDelete : KeyLobbyRoomUpdate;
            return new Dictionary<byte, object>
            {
                { KeyData, new Dictionary<byte, object> { { key, data ?? new byte[0] } } },
            };
        }

        public static bool IsGameList(long op)
        {
            return op == EvGameListInit || op == EvGameListUpdate || op == EvGameListRemoval;
        }

        public struct JoinAck
        {
            public int ActorId;
            public int Count;
            public int ServerMs;
            public bool InitRoom;
            public byte[] RoomId;
        }

        // match data op 88: [int32 actor][int32 count][int32 serverMs][u8 initRoom][12 B CmuneRoomID]
        public static bool TryParseJoinAck(byte[] b, out JoinAck ack)
        {
            ack = new JoinAck();
            if (b == null || b.Length != JoinAckSize)
                return false;

            ack.ActorId = ReadInt32(b, 0);
            ack.Count = ReadInt32(b, 4);
            ack.ServerMs = ReadInt32(b, 8);
            ack.InitRoom = b[12] != 0;
            ack.RoomId = new byte[RoomIdSize];
            Buffer.BlockCopy(b, 13, ack.RoomId, 0, RoomIdSize);
            return true;
        }

        // response 88 rc 0 params the listener reads (9 int, 4 byte[], 200 bool, 201 long)
        public static Dictionary<byte, object> JoinParams(JoinAck a)
        {
            return new Dictionary<byte, object>
            {
                { KeyActorNr, a.ActorId },
                { KeyActors, a.Count },
                { KeyGameId, a.RoomId },
                { KeyInitRoom, a.InitRoom },
                { KeyServerTicks, (long)a.ServerMs },
            };
        }

        // Go reject reason "rc=<n>" -> n, else -1
        public static int ParseRejectCode(string message)
        {
            if (string.IsNullOrEmpty(message))
                return -1;

            int i = message.IndexOf("rc=", StringComparison.Ordinal);
            if (i < 0)
                return -1;

            int j = i + 3, n = 0, digits = 0;
            while (j < message.Length && message[j] >= '0' && message[j] <= '9' && digits < 6)
            {
                n = n * 10 + (message[j] - '0');
                j++;
                digits++;
            }
            return digits == 0 ? -1 : n;
        }

        // op 66 {100 byte, 61 short, 103 byte[]} -> {"m","i","a","p"}
        public static string Op66Request(IDictionary<byte, object> p, string peerKind)
        {
            byte method = Get<byte>(p, KeyMethodId);
            short invoc = Get<short>(p, KeyInvocationId);
            byte[] args = GetBytesOrEmpty(p, KeyBytes);

            var w = new NakamaJsonWriter();
            w.Num("m", method).Num("i", invoc).Str("a", Convert.ToBase64String(args));
            if (!string.IsNullOrEmpty(peerKind))
                w.Str("p", peerKind);
            return w.End();
        }

        public struct Op66Reply
        {
            public int ReturnCode;
            public short InvocationId;
            public byte[] Data;
            public string Message;
            public bool Disconnect;
        }

        public static Op66Reply ParseOp66Reply(string json)
        {
            Dictionary<string, object> o = NakamaJson.ParseObject(json);
            return new Op66Reply
            {
                ReturnCode = (int)NakamaJson.Long(o, "rc", 0),
                InvocationId = (short)NakamaJson.Long(o, "i", 0),
                Data = NakamaJson.Base64(o, "d"),
                Message = NakamaJson.Str(o, "msg"),
                Disconnect = NakamaJson.Bool(o, "dc"),
            };
        }

        // response 66: {61 short, 42 byte[] only when data came back}
        public static Dictionary<byte, object> Op66Params(short invocationId, byte[] data)
        {
            var p = new Dictionary<byte, object> { { KeyInvocationId, invocationId } };
            if (data != null)
                p[KeyData] = data;
            return p;
        }

        // op 88 {103 meta} -> {"meta": b64}
        public static string RoomJoinRequest(IDictionary<byte, object> p)
        {
            return new NakamaJsonWriter().Str("meta", Convert.ToBase64String(GetBytesOrEmpty(p, KeyBytes))).End();
        }

        public struct RoomJoinReply
        {
            public int ReturnCode;
            public string MatchId;
            public int Number;
            public string Message;
        }

        public static RoomJoinReply ParseRoomJoinReply(string json)
        {
            Dictionary<string, object> o = NakamaJson.ParseObject(json);
            var r = new RoomJoinReply
            {
                ReturnCode = (int)NakamaJson.Long(o, "rc", RcTransport),
                MatchId = NakamaJson.Str(o, "match"),
                Number = (int)NakamaJson.Long(o, "number", 0),
                Message = NakamaJson.Str(o, "msg"),
            };
            if (r.ReturnCode == 0 && string.IsNullOrEmpty(r.MatchId))
            {
                r.ReturnCode = RcTransport;
                r.Message = "uber_room_join: no match id";
            }
            return r;
        }

        public static string TimeRequest(long clientTick)
        {
            return new NakamaJsonWriter().Num("c", clientTick).End();
        }

        public static bool TryParseTimeReply(string json, out long clientTick, out int serverMs)
        {
            clientTick = 0;
            serverMs = 0;
            Dictionary<string, object> o;
            try { o = NakamaJson.ParseObject(json); }
            catch (FormatException) { return false; }

            if (!o.ContainsKey("c") || !o.ContainsKey("s"))
                return false;

            clientTick = NakamaJson.Long(o, "c", 0);
            serverMs = (int)NakamaJson.Long(o, "s", 0);
            return true;
        }

        public static T Get<T>(IDictionary<byte, object> p, byte key)
        {
            object v;
            if (p == null || !p.TryGetValue(key, out v))
                throw new FramingException("param " + key + " missing");
            if (!(v is T))
                throw new FramingException("param " + key + " is " + (v == null ? "null" : v.GetType().Name) + ", want " + typeof(T).Name);
            return (T)v;
        }

        static byte[] GetBytesOrEmpty(IDictionary<byte, object> p, byte key)
        {
            object v;
            if (p == null || !p.TryGetValue(key, out v) || v == null)
                return new byte[0];
            byte[] b = v as byte[];
            if (b == null)
                throw new FramingException("param " + key + " is " + v.GetType().Name + ", want Byte[]");
            return b;
        }

        public static int ReadInt32(byte[] b, int o)
        {
            return b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24);
        }

        public static void WriteInt32(byte[] b, int o, int v)
        {
            b[o] = (byte)v;
            b[o + 1] = (byte)(v >> 8);
            b[o + 2] = (byte)(v >> 16);
            b[o + 3] = (byte)(v >> 24);
        }

        public static string Hex(byte[] b)
        {
            var sb = new StringBuilder(b.Length * 2);
            foreach (byte x in b)
                sb.Append(x.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }

    public sealed class FramingException : Exception
    {
        public FramingException(string message) : base(message) { }
    }

    // flat JSON object writer (our requests only)
    public sealed class NakamaJsonWriter
    {
        readonly StringBuilder _sb = new StringBuilder("{");
        bool _first = true;

        public NakamaJsonWriter Num(string key, long value)
        {
            Key(key);
            _sb.Append(value.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        public NakamaJsonWriter Str(string key, string value)
        {
            Key(key);
            NakamaJson.Quote(_sb, value);
            return this;
        }

        public NakamaJsonWriter Bool(string key, bool value)
        {
            Key(key);
            _sb.Append(value ? "true" : "false");
            return this;
        }

        void Key(string key)
        {
            if (!_first)
                _sb.Append(',');
            _first = false;
            NakamaJson.Quote(_sb, key);
            _sb.Append(':');
        }

        public string End()
        {
            return _sb.ToString() + "}";
        }
    }

    // small JSON reader: objects -> Dictionary<string,object>, arrays -> List<object>, numbers -> long|double
    public static class NakamaJson
    {
        public static Dictionary<string, object> ParseObject(string s)
        {
            if (s == null)
                throw new FormatException("json: null");
            int i = 0;
            object v = Value(s, ref i);
            Ws(s, ref i);
            if (i != s.Length)
                throw new FormatException("json: trailing data at " + i);
            var o = v as Dictionary<string, object>;
            if (o == null)
                throw new FormatException("json: not an object");
            return o;
        }

        public static long Long(Dictionary<string, object> o, string k, long def)
        {
            object v;
            if (!o.TryGetValue(k, out v) || v == null)
                return def;
            if (v is long)
                return (long)v;
            if (v is double)
                return (long)(double)v;
            return def;
        }

        public static string Str(Dictionary<string, object> o, string k)
        {
            object v;
            return o.TryGetValue(k, out v) ? v as string : null;
        }

        public static bool Bool(Dictionary<string, object> o, string k)
        {
            object v;
            return o.TryGetValue(k, out v) && v is bool && (bool)v;
        }

        public static byte[] Base64(Dictionary<string, object> o, string k)
        {
            string s = Str(o, k);
            if (string.IsNullOrEmpty(s))
                return null;
            try { return Convert.FromBase64String(s); }
            catch (FormatException) { throw new FormatException("json: " + k + " is not base64"); }
        }

        public static void Quote(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (s != null)
            {
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        default:
                            if (c < 0x20)
                                sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r'))
                i++;
        }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length)
                throw new FormatException("json: unexpected end");

            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return StrVal(s, ref i);
            if (c == 't') { Lit(s, ref i, "true"); return true; }
            if (c == 'f') { Lit(s, ref i, "false"); return false; }
            if (c == 'n') { Lit(s, ref i, "null"); return null; }
            if (c == '-' || (c >= '0' && c <= '9')) return Num(s, ref i);
            throw new FormatException("json: bad char '" + c + "' at " + i);
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var o = new Dictionary<string, object>();
            i++;
            Ws(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return o; }
            while (true)
            {
                Ws(s, ref i);
                if (i >= s.Length || s[i] != '"')
                    throw new FormatException("json: key expected at " + i);
                string k = StrVal(s, ref i);
                Ws(s, ref i);
                if (i >= s.Length || s[i] != ':')
                    throw new FormatException("json: ':' expected at " + i);
                i++;
                o[k] = Value(s, ref i);
                Ws(s, ref i);
                if (i >= s.Length)
                    throw new FormatException("json: unexpected end in object");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return o; }
                throw new FormatException("json: ',' or '}' expected at " + i);
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var a = new List<object>();
            i++;
            Ws(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return a; }
            while (true)
            {
                a.Add(Value(s, ref i));
                Ws(s, ref i);
                if (i >= s.Length)
                    throw new FormatException("json: unexpected end in array");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return a; }
                throw new FormatException("json: ',' or ']' expected at " + i);
            }
        }

        static string StrVal(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"')
                    return sb.ToString();
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= s.Length)
                    break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length)
                            throw new FormatException("json: bad \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw new FormatException("json: bad escape \\" + e);
                }
            }
            throw new FormatException("json: unterminated string");
        }

        static object Num(string s, ref int i)
        {
            int start = i;
            if (s[i] == '-') i++;
            bool frac = false;
            while (i < s.Length)
            {
                char c = s[i];
                if (c >= '0' && c <= '9') { i++; continue; }
                if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { frac = true; i++; continue; }
                break;
            }
            string t = s.Substring(start, i - start);
            long l;
            if (!frac && long.TryParse(t, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out l))
                return l;
            double d;
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                return d;
            throw new FormatException("json: bad number '" + t + "'");
        }

        static void Lit(string s, ref int i, string lit)
        {
            if (string.CompareOrdinal(s, i, lit, 0, lit.Length) != 0)
                throw new FormatException("json: bad literal at " + i);
            i += lit.Length;
        }
    }
}
