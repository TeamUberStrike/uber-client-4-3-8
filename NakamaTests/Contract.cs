using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // Cross-language check with the Go module (contract/go/client_contract_test.go):
    //   NakamaTests --emit cs.json      C# encodings for Go to parse
    //   go test (UBER_CS_VECTORS=cs.json UBER_GO_VECTORS=go.json)
    //   NakamaTests --verify go.json    Go encodings parsed by C#
    static class Contract
    {
        static readonly byte[] Args = { 6, 5, 0, 0, 0, 0x0c, 2, 0, 0x68, 0, 0x69, 0 };

        public static int Emit(string path)
        {
            var sb = new StringBuilder("{\n\"envelopes\":[\n");
            var env = new List<string>
            {
                Envelope(82, 4, 25, 0, Args),
                Envelope(83, 101, 86, 0, Args),
                Envelope(81, 100, 99, 0, new byte[0]),
                Envelope(80, 4, 30, 7, Args),
                Envelope(82, -1, 1, 0, new byte[] { 1 }),
                Envelope(80, 106, 68, -2, new byte[0]),
            };
            sb.Append(string.Join(",\n", env)).Append("\n],\n\"op66\":[\n");

            var op66 = new List<string>
            {
                Op66(1, 2, new byte[] { 1, 0x0c, 3, 0, 0x31, 0, 0x2e, 0, 0x37, 0 }, "game"),
                Op66(22, -32768, Args, "comm"),
                Op66(2, 32767, new byte[0], null),
            };
            sb.Append(string.Join(",\n", op66)).Append("\n],\n\"roomJoin\":[\n");

            byte[] meta = { 0x28, 1, 2, 3, 255 };
            string rj = NakamaFraming.RoomJoinRequest(new Dictionary<byte, object> { { 103, meta } });
            sb.Append("{\"json\":").Append(Quote(rj)).Append(",\"metaHex\":\"").Append(NakamaFraming.Hex(meta)).Append("\"}");
            sb.Append("\n],\n\"time\":[");
            sb.Append("{\"json\":").Append(Quote(NakamaFraming.TimeRequest(123456789012))).Append(",\"c\":123456789012}");
            sb.Append("],\n\"joinAck\":[\n");

            // layout the C# parser expects for ActorID 7, Count 3, ServerMs 99, InitRoom true, room 101 @ 127.0.0.1:20101
            byte[] ack = new byte[25];
            NakamaFraming.WriteInt32(ack, 0, 7);
            NakamaFraming.WriteInt32(ack, 4, 3);
            NakamaFraming.WriteInt32(ack, 8, 99);
            ack[12] = 1;
            byte[] room = { 1, 7, 101, 0, 0, 0, 127, 0, 0, 1, 0x85, 0x4e };
            Buffer.BlockCopy(room, 0, ack, 13, 12);
            NakamaFraming.JoinAck parsed;
            A.True(NakamaFraming.TryParseJoinAck(ack, out parsed) && parsed.ActorId == 7 && parsed.InitRoom, "self-check");
            sb.Append("{\"actor\":7,\"count\":3,\"serverMs\":99,\"init\":true,\"number\":101,\"server\":\"127.0.0.1:20101\",\"hex\":\"").Append(NakamaFraming.Hex(ack)).Append("\"}");
            sb.Append("\n]\n}\n");

            File.WriteAllText(path, sb.ToString());
            Console.WriteLine("emitted " + path);
            return 0;
        }

        public static int Verify(string path)
        {
            Dictionary<string, object> o = NakamaJson.ParseObject(File.ReadAllText(path));
            int n = 0;

            foreach (Dictionary<string, object> c in Items(o, "op66Reply"))
            {
                NakamaFraming.Op66Reply r = NakamaFraming.ParseOp66Reply((string)c["json"]);
                A.Eq((int)NakamaJson.Long(c, "rc", -9), r.ReturnCode, "op66 rc " + c["json"]);
                A.Eq((short)NakamaJson.Long(c, "i", -9), r.InvocationId, "op66 i");
                string d = NakamaJson.Str(c, "dHex");
                A.True(d == "" ? r.Data == null : NakamaFraming.Hex(r.Data) == d, "op66 d " + c["json"]);
                A.Eq(NakamaJson.Str(c, "msg") ?? "", r.Message ?? "", "op66 msg");
                A.Eq(NakamaJson.Bool(c, "dc"), r.Disconnect, "op66 dc");
                n++;
            }

            foreach (Dictionary<string, object> c in Items(o, "roomJoinReply"))
            {
                NakamaFraming.RoomJoinReply r = NakamaFraming.ParseRoomJoinReply((string)c["json"]);
                A.Eq((int)NakamaJson.Long(c, "rc", -9), r.ReturnCode, "room join rc " + c["json"]);
                A.Eq(NakamaJson.Str(c, "match") ?? "", r.MatchId ?? "", "room join match");
                A.Eq((int)NakamaJson.Long(c, "number", -9), r.Number, "room join number");
                n++;
            }

            foreach (Dictionary<string, object> c in Items(o, "timeReply"))
            {
                long cc;
                int s;
                A.True(NakamaFraming.TryParseTimeReply((string)c["json"], out cc, out s), "time parse");
                A.Eq(NakamaJson.Long(c, "c", -9), cc, "time c");
                A.Eq((int)NakamaJson.Long(c, "s", -9), s, "time s");
                n++;
            }

            foreach (Dictionary<string, object> c in Items(o, "joinAck"))
            {
                NakamaFraming.JoinAck a;
                A.True(NakamaFraming.TryParseJoinAck(A.FromHex((string)c["hex"]), out a), "go ack parses");
                A.Eq((int)NakamaJson.Long(c, "actor", -9), a.ActorId, "ack actor");
                A.Eq((int)NakamaJson.Long(c, "count", -9), a.Count, "ack count");
                A.Eq((int)NakamaJson.Long(c, "serverMs", -9), a.ServerMs, "ack ms");
                A.Eq(NakamaJson.Bool(c, "init"), a.InitRoom, "ack init");
                A.Eq((string)c["roomHex"], NakamaFraming.Hex(a.RoomId), "ack room id");
                n++;
            }

            foreach (Dictionary<string, object> c in Items(o, "events"))
            {
                Dictionary<byte, object> p = NakamaFraming.DecodeEvent(A.FromHex((string)c["hex"]));
                A.Eq((short)NakamaJson.Long(c, "netId", -9), (short)p[101], "event netId");
                A.Eq((byte)NakamaJson.Long(c, "method", -9), (byte)p[100], "event method");
                A.Eq((string)c["argsHex"], NakamaFraming.Hex((byte[])p[103]), "event args");
                n++;
            }

            foreach (Dictionary<string, object> c in Items(o, "reasons"))
            {
                A.Eq((int)NakamaJson.Long(c, "rc", -9), NakamaFraming.ParseRejectCode((string)c["reason"]), "reason " + c["reason"]);
                n++;
            }

            A.True(n >= 10, "go vectors present, got " + n);
            Console.WriteLine("verified " + n + " go vectors from " + path);
            return 0;
        }

        static IEnumerable<Dictionary<string, object>> Items(Dictionary<string, object> o, string key)
        {
            object v;
            if (!o.TryGetValue(key, out v) || !(v is List<object>))
                throw new AssertException("missing " + key);
            foreach (object x in (List<object>)v)
                yield return (Dictionary<string, object>)x;
        }

        static string Envelope(byte op, short netId, byte method, int target, byte[] args)
        {
            var p = new Dictionary<byte, object> { { 101, netId }, { 100, method }, { 103, args } };
            if (op == 80)
                p[102] = target;
            byte[] b = NakamaFraming.EncodeEnvelope(op, p);
            return "{\"op\":" + op + ",\"netId\":" + netId + ",\"method\":" + method + ",\"target\":" + target +
                   ",\"argsHex\":\"" + NakamaFraming.Hex(args) + "\",\"hex\":\"" + NakamaFraming.Hex(b) + "\"}";
        }

        static string Op66(byte m, short i, byte[] a, string kind)
        {
            string json = NakamaFraming.Op66Request(new Dictionary<byte, object> { { 100, m }, { 61, i }, { 103, a } }, kind);
            return "{\"json\":" + Quote(json) + ",\"m\":" + m + ",\"i\":" + i + ",\"aHex\":\"" + NakamaFraming.Hex(a) + "\",\"p\":" + Quote(kind ?? "") + "}";
        }

        static string Quote(string s)
        {
            var sb = new StringBuilder();
            NakamaJson.Quote(sb, s);
            return sb.ToString();
        }
    }
}
