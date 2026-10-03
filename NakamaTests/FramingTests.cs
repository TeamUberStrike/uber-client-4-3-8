using System;
using System.Collections.Generic;
using UberStrike.Realtime.NakamaAdapter;

namespace NakamaTests
{
    // Byte vectors come from the Go module (TeamUberStrike/photon-migration@nakama a4e69ea):
    // internal/rmi/rmi_test.go, internal/roomcore/core_test.go, wire/testdata/golden.json (real SDK output).
    static class FramingTests
    {
        static Dictionary<byte, object> Room(short netId, byte method, byte[] args)
        {
            return new Dictionary<byte, object> { { 101, netId }, { 100, method }, { 103, args } };
        }

        [Test]
        static void Op82HeaderMatchesGo()
        {
            byte[] args = { 6, 5, 0, 0, 0 };
            byte[] b = NakamaFraming.EncodeEnvelope(82, Room(4, 25, args));
            A.Bytes(new byte[] { 4, 0, 25, 6, 5, 0, 0, 0 }, b, "op 82 [netId][method][args]");
        }

        [Test]
        static void Op83And81SameAs82()
        {
            byte[] args = { 1, 2 };
            byte[] b82 = NakamaFraming.EncodeEnvelope(82, Room(101, 86, args));
            A.Bytes(b82, NakamaFraming.EncodeEnvelope(83, Room(101, 86, args)), "op 83");
            A.Bytes(b82, NakamaFraming.EncodeEnvelope(81, Room(101, 86, args)), "op 81");
            A.Bytes(new byte[] { 101, 0, 86, 1, 2 }, b82, "netId 101 LE");
        }

        [Test]
        static void Op80HeaderMatchesGo()
        {
            var p = Room(4, 25, new byte[0]);
            p[102] = 7;
            A.Bytes(new byte[] { 4, 0, 25, 7, 0, 0, 0 }, NakamaFraming.EncodeEnvelope(80, p), "op 80 target int32 LE");
        }

        [Test]
        static void NegativeNetworkId()
        {
            byte[] b = NakamaFraming.EncodeEnvelope(82, Room(-1, 1, null));
            A.Bytes(new byte[] { 0xff, 0xff, 1 }, b, "netId -1");
            A.Eq((short)-1, (short)NakamaFraming.DecodeEvent(b)[101], "decode -1");
        }

        [Test]
        static void EnvelopeTypeChecks()
        {
            var p = new Dictionary<byte, object> { { 101, 4 }, { 100, (byte)1 }, { 103, new byte[0] } };
            A.Throws<FramingException>(() => NakamaFraming.EncodeEnvelope(82, p), "int netId rejected");
            A.Throws<FramingException>(() => NakamaFraming.EncodeEnvelope(80, Room(4, 1, null)), "op 80 needs 102");
            A.Throws<FramingException>(() => NakamaFraming.EncodeEnvelope(66, Room(4, 1, null)), "66 not a room op");
            A.Throws<FramingException>(() => NakamaFraming.EncodeEnvelope(82, null), "null params");
        }

        [Test]
        static void Event0FromGoEvent()
        {
            // Go rmi.Event(ClassClientSync=1, 1, int32 3, int16 4)
            byte[] go = A.FromHex("010001" + "0603000000" + "040400");
            Dictionary<byte, object> p = NakamaFraming.DecodeEvent(go);
            A.Eq((short)1, (short)p[101], "101 short");
            A.Eq((byte)1, (byte)p[100], "100 byte");
            A.Bytes(A.FromHex("0603000000040400"), (byte[])p[103], "103 args");
            A.Eq(3, p.Count, "3 keys");
            A.Bytes(new byte[0], (byte[])NakamaFraming.DecodeEvent(new byte[] { 2, 0, 5 })[103], "no args");
            A.Throws<FramingException>(() => NakamaFraming.DecodeEvent(new byte[] { 1, 0 }), "short event");
        }

        [Test]
        static void JoinAckMatchesGoLayout()
        {
            // roomcore.TestJoinAckLayout: actor 3, count 2, serverMs 0x01020304, init false, room 88 @ 127.0.0.1:5055
            byte[] go = { 3, 0, 0, 0, 2, 0, 0, 0, 4, 3, 2, 1, 0, 1, 7, 88, 0, 0, 0, 127, 0, 0, 1, 0xbf, 0x13 };
            NakamaFraming.JoinAck a;
            A.True(NakamaFraming.TryParseJoinAck(go, out a), "parse");
            A.Eq(3, a.ActorId, "actor");
            A.Eq(2, a.Count, "count");
            A.Eq(0x01020304, a.ServerMs, "serverMs");
            A.Eq(false, a.InitRoom, "initRoom");
            // golden roomid_comm = 2d + these 12 bytes (SDK CmuneRoomID.GetBytes)
            A.Bytes(A.FromHex("0107580000007f000001bf13"), a.RoomId, "CmuneRoomID 12 B");

            Dictionary<byte, object> p = NakamaFraming.JoinParams(a);
            A.True(p[9] is int && (int)p[9] == 3, "9 int actor");
            A.True(p[4] is byte[], "4 byte[]");
            A.True(p[200] is bool && !(bool)p[200], "200 bool");
            A.True(p[201] is long && (long)p[201] == 0x01020304, "201 long ticks");
            A.True(p[11] is int && (int)p[11] == 2, "11 int count");

            go[12] = 1;
            A.True(NakamaFraming.TryParseJoinAck(go, out a) && a.InitRoom, "initRoom true");
            A.True(!NakamaFraming.TryParseJoinAck(new byte[24], out a), "24 bytes rejected");
            A.True(!NakamaFraming.TryParseJoinAck(null, out a), "null rejected");
        }

        [Test]
        static void GameListWrap()
        {
            byte[] list = A.FromHex("6f0000");
            Dictionary<byte, object> p3 = NakamaFraming.WrapGameList(3, list);
            var inner = (Dictionary<byte, object>)p3[42];
            A.True(inner[122] == (object)list, "3 -> 42/122");
            A.True(((Dictionary<byte, object>)NakamaFraming.WrapGameList(4, list)[42]).ContainsKey(122), "4 -> 122");
            A.True(((Dictionary<byte, object>)NakamaFraming.WrapGameList(5, list)[42]).ContainsKey(123), "5 -> 123");
            A.True(((System.Collections.IDictionary)p3[42])[(byte)122] is byte[], "IDictionary lookup with boxed byte");
            A.True(NakamaFraming.IsGameList(3) && NakamaFraming.IsGameList(5) && !NakamaFraming.IsGameList(0) && !NakamaFraming.IsGameList(88), "IsGameList");
        }

        [Test]
        static void Op66RequestShape()
        {
            var p = new Dictionary<byte, object> { { 100, (byte)2 }, { 61, (short)-5 }, { 103, new byte[] { 1, 2, 255 } } };
            string json = NakamaFraming.Op66Request(p, "game");
            Dictionary<string, object> o = NakamaJson.ParseObject(json);
            A.Eq(2L, (long)o["m"], "m");
            A.Eq(-5L, (long)o["i"], "i");
            A.Eq("AQL/", (string)o["a"], "a b64");
            A.Eq("game", (string)o["p"], "p");
            A.True(!NakamaJson.ParseObject(NakamaFraming.Op66Request(p, null)).ContainsKey("p"), "p omitted");
            p.Remove(103);
            A.Eq("", (string)NakamaJson.ParseObject(NakamaFraming.Op66Request(p, null))["a"], "no args -> empty");
        }

        [Test]
        static void Op66ReplyVariants()
        {
            NakamaFraming.Op66Reply r = NakamaFraming.ParseOp66Reply("{\"rc\":0,\"i\":7,\"d\":\"AQI=\"}");
            A.Eq(0, r.ReturnCode, "rc");
            A.Eq((short)7, r.InvocationId, "i");
            A.Bytes(new byte[] { 1, 2 }, r.Data, "d");
            A.True(!r.Disconnect, "dc");

            r = NakamaFraming.ParseOp66Reply("{\"rc\":1,\"i\":3,\"msg\":\"not allowed\"}");
            A.Eq(1, r.ReturnCode, "rc 1");
            A.Eq("not allowed", r.Message, "msg");
            A.True(r.Data == null, "no d");

            r = NakamaFraming.ParseOp66Reply("{\"rc\":1,\"i\":2,\"msg\":\"The current version '1.6' of your client is outdated\",\"dc\":true}");
            A.True(r.Disconnect, "dc true");

            Dictionary<byte, object> p = NakamaFraming.Op66Params(7, null);
            A.True(p[61] is short && !p.ContainsKey(42), "no 42 without data (legacy 'no data attached')");
            p = NakamaFraming.Op66Params(7, new byte[] { 9 });
            A.True(p[42] is byte[], "42 byte[]");
            A.Throws<FormatException>(() => NakamaFraming.ParseOp66Reply("{\"rc\":0,\"i\":1,\"d\":\"***\"}"), "bad b64");
        }

        [Test]
        static void RoomJoinShapes()
        {
            var p = new Dictionary<byte, object> { { 103, new byte[] { 0x28, 1 } }, { 206, 5 }, { 205, 0 } };
            A.Eq("{\"meta\":\"KAE=\"}", NakamaFraming.RoomJoinRequest(p), "request");

            NakamaFraming.RoomJoinReply r = NakamaFraming.ParseRoomJoinReply("{\"rc\":0,\"match\":\"abc.nakama1\",\"number\":101}");
            A.Eq(0, r.ReturnCode, "rc");
            A.Eq("abc.nakama1", r.MatchId, "match");
            A.Eq(101, r.Number, "number");

            r = NakamaFraming.ParseRoomJoinReply("{\"rc\":1,\"number\":105,\"msg\":\"Game doesn't exist anymore!\"}");
            A.Eq(1, r.ReturnCode, "rc 1");
            A.Eq("Game doesn't exist anymore!", r.Message, "msg");

            r = NakamaFraming.ParseRoomJoinReply("{\"rc\":0}");
            A.Eq(NakamaFraming.RcTransport, r.ReturnCode, "rc 0 without match = failure");
            r = NakamaFraming.ParseRoomJoinReply("{}");
            A.Eq(NakamaFraming.RcTransport, r.ReturnCode, "missing rc = failure");
        }

        [Test]
        static void RejectCodes()
        {
            A.Eq(2, NakamaFraming.ParseRejectCode("rc=2"), "rc=2");
            A.Eq(4, NakamaFraming.ParseRejectCode("Match join rejected: rc=4"), "embedded");
            A.Eq(13, NakamaFraming.ParseRejectCode("rc=13 extra"), "2 digits");
            A.Eq(-1, NakamaFraming.ParseRejectCode("rc="), "no digits");
            A.Eq(-1, NakamaFraming.ParseRejectCode("match not found"), "none");
            A.Eq(-1, NakamaFraming.ParseRejectCode(null), "null");
        }

        [Test]
        static void TimeShapes()
        {
            A.Eq("{\"c\":123456789012}", NakamaFraming.TimeRequest(123456789012), "request");
            long c;
            int s;
            A.True(NakamaFraming.TryParseTimeReply("{\"c\":42,\"s\":2147483647}", out c, out s), "parse");
            A.Eq(42L, c, "c");
            A.Eq(int.MaxValue, s, "s int31");
            A.True(!NakamaFraming.TryParseTimeReply("{\"c\":42}", out c, out s), "missing s");
            A.True(!NakamaFraming.TryParseTimeReply("nope", out c, out s), "garbage");
        }

        [Test]
        static void JsonReader()
        {
            Dictionary<string, object> o = NakamaJson.ParseObject(" { \"a\" : \"x\\\"y\\\\z\\n\\u00e9\" , \"n\":-12, \"f\":1.5e2, \"t\":true, \"z\":null, \"o\":{\"k\":[1,\"2\",{}]}, \"e\":[] } ");
            A.Eq("x\"y\\z\né", (string)o["a"], "escapes");
            A.Eq(-12L, (long)o["n"], "long");
            A.Eq(150.0, (double)o["f"], "double");
            A.Eq(true, (bool)o["t"], "bool");
            A.True(o["z"] == null, "null");
            A.True(o["o"] is Dictionary<string, object>, "nested");
            A.Eq(150L, NakamaJson.Long(o, "f", 0), "double as long");
            A.Throws<FormatException>(() => NakamaJson.ParseObject("{\"a\":1"), "unterminated");
            A.Throws<FormatException>(() => NakamaJson.ParseObject("[1]"), "not object");
            A.Throws<FormatException>(() => NakamaJson.ParseObject("{\"a\":1} x"), "trailing");
            A.Throws<FormatException>(() => NakamaJson.ParseObject(null), "null");
        }

        [Test]
        static void JsonWriterEscapes()
        {
            string s = new NakamaJsonWriter().Str("k", "a\"b\\c\u0001\n").Num("n", -3).Bool("b", true).End();
            A.Eq("{\"k\":\"a\\\"b\\\\c\\u0001\\n\",\"n\":-3,\"b\":true}", s, "writer");
            Dictionary<string, object> o = NakamaJson.ParseObject(s);
            A.Eq("a\"b\\c\u0001\n", (string)o["k"], "round trip");
        }
    }
}
