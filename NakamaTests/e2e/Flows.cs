using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cmune.Realtime.Photon.Client.Transport;
using UberStrike.Realtime.NakamaAdapter;

namespace ClientE2E
{
    // Real S2/S3 flows on the live Go module: comm chat, lobby list, deathmatch, heartbeat, moderation kick.
    static partial class Program
    {
        // CommRPC / LobbyRPC / GameRPC ids (Realtime.Photon.Client/Common/Defines/RPC.cs)
        const byte CJoin = 1, CFullList = 4, CChatToAll = 25;
        const byte GJoin = 1, GLeave = 2, GBegin = 21, GHit = 68, GSpawnPoints = 70, GMatchStart = 76, GPlayerEvent = 78, GSplat = 80, GPosition = 83, GKick = 101;
        const short ClassComm = 4, ClassLobby = 3, ClassDM = 101;
        const string CommActorDefault = "2e7f0600000000000006000000000c000001002d0107000000000000000000000500000c000001000c000001000600000000";

        static async Task RealFlows(NakamaLink la, NakamaLink lb, TestPeer commA, TestPeer lobby, int lobbyActor, int actorA, int cmidA, int cmidB, string roomIdComm)
        {
            // comm: CommRPC Join both, A ChatToAll -> B
            Check("comm A: CommRPC Join -> FullList", await CommJoin(commA, actorA, cmidA, "e2e A", Hex(roomIdComm)), "");

            var commB = new TestPeer("B comm", lb);
            Check("comm B: connect", commB.Connect("127.0.0.1:20088", cmidB) && await commB.WaitStatus(StatusCode.Connect, 5000), commB.LastStatus);
            OperationResponse jcb = await commB.Join(Hex(RoomComm));
            int actorB = jcb != null && jcb.ReturnCode == 0 ? (int)jcb.Parameters[9] : -1;
            Check("comm B: joined comm 88", actorB > 0 && actorB != actorA, Describe(jcb));
            await Register(commB, actorB, ClassComm);
            Check("comm B: CommRPC Join -> FullList", await CommJoin(commB, actorB, cmidB, "e2e B", (byte[])jcb.Parameters[4]), "");

            commA.Send(82, ClassComm, CChatToAll, Args(actorA, "hello from A"));
            EventData chat = await commB.WaitEvent(e => Ev(e, ClassComm, CChatToAll), 5000);
            object[] ca = chat == null ? null : Decode(chat);
            Check("comm chat: B gets A's ChatToAll (cmid, actor, text)", ca != null && ca.Length >= 4 && Equals(ca[0], cmidA) && Equals(ca[1], actorA) && Equals(ca[3], "hello from A"), ca == null ? "no event" : string.Join(",", ca));

            await Wait(600);
            commA.Send(82, ClassComm, CChatToAll, Args(actorB, "spoofed"));
            EventData spoof = await commB.WaitEvent(e => Ev(e, ClassComm, CChatToAll), 1500);
            Check("comm chat: spoofed actor id dropped", spoof == null, "");

            // lobby: register class 3 + LobbyRPC 1 -> op 3 with permanent rooms
            lobby.Clear();
            await Register(lobby, lobbyActor, ClassLobby);
            lobby.Send(82, ClassLobby, 1, Hex(CommActorDefault));
            EventData init = await lobby.WaitEvent(e => e.Code == 3, 3000);
            byte[] list = GameList(init);
            Check("lobby op 3: permanent rooms listed", list != null && Contains(list, Utf16("Veterans Battleground")) && Contains(list, Utf16("Pro Stadium")), list == null ? "no op 3" : list.Length + " B");

            // deathmatch: C creates, D joins by number
            int cmidC = cmidA + 200000, cmidD = cmidA + 300000;
            NakamaLink lc = NewLink(cmidC, 0, "e2e C");
            NakamaLink ld = NewLink(cmidD, 0, "e2e D");
            var gc = new TestPeer("C dm", lc);
            var gd = new TestPeer("D dm", ld);
            Check("dm C: connect", gc.Connect("127.0.0.1:20000", cmidC) && await gc.WaitStatus(StatusCode.Connect, 15000), gc.LastStatus);
            Check("dm D: connect", gd.Connect("127.0.0.1:20000", cmidD) && await gd.WaitStatus(StatusCode.Connect, 15000), gd.LastStatus);

            string name = "e2e dm " + DateTime.UtcNow.Ticks % 1000000;
            OperationResponse jc = await gc.Join(GameMeta(name, 0));
            int number = jc != null && jc.ReturnCode == 0 ? NakamaFraming.ReadInt32((byte[])jc.Parameters[4], 2) : -1;
            int actorC = jc != null && jc.ReturnCode == 0 ? (int)jc.Parameters[9] : -1;
            byte[] dmRoomId = jc != null && jc.ReturnCode == 0 ? (byte[])jc.Parameters[4] : null;
            Check("dm: C creates DM room", number >= 101 && actorC > 0, Describe(jc));

            EventData upd = await lobby.WaitEvent(e => e.Code == 4 && Contains(GameList(e), Utf16(name)), 10000);
            Check("lobby op 4: created room appears", upd != null, "");

            OperationResponse jd = await gd.Join(GameMeta("", number));
            int actorD = jd != null && jd.ReturnCode == 0 ? (int)jd.Parameters[9] : -1;
            Check("dm: D joins by number as new actor", actorD > 0 && actorD != actorC && NakamaFraming.ReadInt32((byte[])jd.Parameters[4], 2) == number, Describe(jd));

            Check("dm C: register 101 + Join -> Begin", await Play(gc, actorC, cmidC, "e2e C"), "");
            Check("dm D: register 101 + Join -> Begin", await Play(gd, actorD, cmidD, "e2e D"), "");
            Check("dm D: MatchStart", await gd.WaitEvent(e => Ev(e, ClassDM, GMatchStart), 5000) != null, "");
            Check("dm C: sees D join", await gc.WaitEvent(e => Ev(e, ClassDM, GJoin), 5000) != null, "");

            // positions: D at 20 Hz -> C gets aggregated op 83
            gc.Clear();
            for (short i = 1; i <= 10; i++)
            {
                gd.Send(82, ClassDM, GPosition, Args(Position(actorD, i)));
                await Wait(50);
            }
            EventData pos = await gc.WaitEvent(e => Ev(e, ClassDM, GPosition) && PosHasX(e), 3000);
            Check("dm: PositionUpdate D -> C position sync", pos != null, "");

            // hits: 60 -> damage event on D; 150 -> splat on C
            gc.Send(82, ClassDM, GHit, Hit(actorC, actorD, 60, 1));
            EventData dmg = await gd.WaitEvent(e => Ev(e, ClassDM, GPlayerEvent), 3000);
            Check("dm: PlayerHit 60 -> D gets damage event (78)", dmg != null, "");

            gc.Send(82, ClassDM, GHit, Hit(actorC, actorD, 150, 2));
            EventData splat = await gc.WaitEvent(e => Ev(e, ClassDM, GSplat), 3000);
            object[] sa = splat == null ? null : Decode(splat);
            Check("dm: lethal PlayerHit -> splat (80) shooter C, victim D", sa != null && sa.Length >= 2 && Equals(sa[0], actorC) && Equals(sa[1], actorD), sa == null ? "no event" : string.Join(",", sa));

            // heartbeat: idle 13 s in a game room, still connected
            await Wait(13000);
            bool cOk = !gc.Statuses.Any(Dropped) && gc.Peer.PeerState == PeerStateValue.Connected;
            bool dOk = !gd.Statuses.Any(Dropped) && gd.Peer.PeerState == PeerStateValue.Connected;
            Check("heartbeat: idle 13 s in game room, C + D still connected", cOk && dOk, gc.LastStatus + " / " + gd.LastStatus);
            gc.Clear();
            gd.Send(82, ClassDM, GPosition, Args(Position(actorD, 77)));
            Check("heartbeat: room still live after idle (position sync)", await gc.WaitEvent(e => Ev(e, ClassDM, GPosition) && PosHasX(e), 3000) != null, "");

            // moderation: non-mod refused, mod kick -> op 89 -> DisconnectByServerLogic on D only
            var probeC = new TestPeer("C probe", lc);
            Check("probe C: connect", probeC.Connect("127.0.0.1:20000", cmidC) && await probeC.WaitStatus(StatusCode.Connect, 3000), probeC.LastStatus);
            OperationResponse deny = await probeC.Op66(22, Args(cmidD, number, 0));
            Check("op66/22 kick by non-moderator -> rc 1", deny != null && deny.ReturnCode == 1, Describe(deny));

            int cmidM = cmidA + 400000;
            NakamaLink lm = NewLink(cmidM, 4, "e2e mod");
            var mod = new TestPeer("mod", lm);
            Check("mod: connect", mod.Connect("127.0.0.1:20000", cmidM) && await mod.WaitStatus(StatusCode.Connect, 15000), mod.LastStatus);
            gc.Clear();
            OperationResponse kick = await mod.Op66(22, Args(cmidD, number, 0));
            Check("op66/22 moderator kick -> rc 0", kick != null && kick.ReturnCode == 0, Describe(kick));
            Check("kick: D gets KickFromGame (101)", await gd.WaitEvent(e => Ev(e, ClassDM, GKick), 3000) != null, "");
            Check("kick: op 89 notice -> D DisconnectByServerLogic (RC-1)", await gd.WaitStatus(StatusCode.DisconnectByServerLogic, 5000), gd.LastStatus);
            Check("kick: C sees D leave", await gc.WaitEvent(e => Ev(e, ClassDM, GLeave) && Equals(First(e), actorD), 3000) != null, "");
            Check("kick: C not disconnected", !gc.Statuses.Any(Dropped) && gc.Peer.PeerState == PeerStateValue.Connected, gc.LastStatus);

            // C leaves -> room closes -> lobby op 5 with its room id
            OperationResponse lv = await gc.Leave();
            Check("dm C: leave -> 89", lv != null, "");
            EventData rem = await lobby.WaitEvent(e => e.Code == 5 && Contains(GameList(e), dmRoomId), 12000);
            Check("lobby op 5: closed room removed", rem != null, "");

            commB.Peer.Disconnect();
            lc.Shutdown();
            ld.Shutdown();
            lm.Shutdown();
        }

        static NakamaLink NewLink(int cmid, int access, string name)
        {
            var p = new Platform(new NakamaIdentity { Cmid = cmid, Access = access, Name = name });
            var l = new NakamaLink(Config(), p);
            p.Link = l;
            return l;
        }

        static bool Dropped(StatusCode s)
        {
            return s == StatusCode.Disconnect || s == StatusCode.DisconnectByServer || s == StatusCode.DisconnectByServerLogic
                || s == StatusCode.DisconnectByServerUserLimit || s == StatusCode.TimeoutDisconnect;
        }

        static async Task<bool> Register(TestPeer p, int actor, short cls)
        {
            EventData e = await p.SendAndWaitEvent(82, 2, 3, Args(actor, (int)cls, cls), x => Ev(x, 1, 1), 5000);
            Check(p.Name + ": register class " + cls + " -> RecieveNetworkID", e != null, "");
            return e != null;
        }

        static async Task<bool> CommJoin(TestPeer p, int actor, int cmid, string name, byte[] room)
        {
            p.Send(82, ClassComm, CJoin, Args(Sync(0x2e, actor, new object[] { 0, cmid, 1, name, 3, new RoomId(room) })));
            return await p.WaitEvent(e => Ev(e, ClassComm, CFullList), 5000) != null;
        }

        static async Task<bool> Play(TestPeer p, int actor, int cmid, string name)
        {
            if (!await Register(p, actor, ClassDM))
                return false;
            p.Send(82, ClassDM, GSpawnPoints, Args((byte)4, (byte)4, (byte)4));
            // CharacterInfo: Cmid, PlayerName, Level, TeamID, Health
            p.Send(82, ClassDM, GJoin, Args(Sync(101, actor, new object[] { 0, cmid, 1, name, 17, (byte)10, 18, (byte)0, 21, (short)100 })));
            return await p.WaitEvent(e => Ev(e, ClassDM, GBegin), 5000) != null;
        }

        static bool Ev(EventData e, short net, byte method)
        {
            return e.Code == 0 && e.Parameters.ContainsKey(101) && (short)e.Parameters[101] == net && (byte)e.Parameters[100] == method;
        }

        static object First(EventData e)
        {
            object[] a = Decode(e);
            return a.Length > 0 ? a[0] : null;
        }

        static object[] Decode(EventData e)
        {
            return DecodeArgs((byte[])e.Parameters[103]);
        }

        static byte[] GameList(EventData e)
        {
            if (e == null || !e.Parameters.ContainsKey(42))
                return null;
            var d = e.Parameters[42] as Dictionary<byte, object>;
            if (d == null)
                return null;
            object v;
            if (d.TryGetValue(122, out v) || d.TryGetValue(123, out v))
                return v as byte[];
            return null;
        }

        static bool Contains(byte[] hay, byte[] needle)
        {
            if (hay == null || needle == null || needle.Length == 0)
                return false;
            for (int i = 0; i + needle.Length <= hay.Length; i++)
            {
                int j = 0;
                while (j < needle.Length && hay[i + j] == needle[j])
                    j++;
                if (j == needle.Length)
                    return true;
            }
            return false;
        }

        // small string as the SDK writes it: unit count + UTF-16LE
        static byte[] Utf16(string s)
        {
            var b = new List<byte> { (byte)s.Length };
            b.AddRange(Encoding.Unicode.GetBytes(s));
            return b.ToArray();
        }

        // tag 103 GameMetaData (Go wire.GameMetaData field order); number 0 creates
        static byte[] GameMeta(string name, int number)
        {
            var b = new List<byte> { 103 };
            b.AddRange(Utf16(name));
            b.Add(0);
            b.Add(8);
            b.Add(0);
            b.Add(0);
            b.Add(1);
            b.Add(7);
            b.AddRange(BitConverter.GetBytes(number));
            b.AddRange(new byte[6]);
            b.AddRange(BitConverter.GetBytes((short)5));
            b.AddRange(BitConverter.GetBytes(300));
            b.AddRange(BitConverter.GetBytes(20));
            b.AddRange(BitConverter.GetBytes(ClassDM));
            b.AddRange(BitConverter.GetBytes(0));
            b.Add(0);
            b.Add(255);
            b.AddRange(BitConverter.GetBytes(0));
            return b.ToArray();
        }

        // position bytes: int32 actor, ShortVector3, int32 time
        static byte[] Position(int actor, short x)
        {
            var b = new List<byte>();
            b.AddRange(BitConverter.GetBytes(actor));
            b.AddRange(BitConverter.GetBytes(x));
            b.AddRange(BitConverter.GetBytes((short)1));
            b.AddRange(BitConverter.GetBytes((short)2));
            b.AddRange(BitConverter.GetBytes(Environment.TickCount & 0x7fffffff));
            return b.ToArray();
        }

        // op 83 sync: bytes [count][num, int32, short x, y, z]...; any entry with x != 0
        static bool PosHasX(EventData e)
        {
            object[] a = Decode(e);
            byte[] raw = a.Length > 0 ? a[0] as byte[] : null;
            if (raw == null || raw.Length < 1)
                return false;
            for (int i = 0, o = 1; i < raw[0] && o + 11 <= raw.Length; i++, o += 11)
                if (BitConverter.ToInt16(raw, o + 5) != 0)
                    return true;
            return false;
        }

        static byte[] Hit(int shooter, int target, short dmg, int shot)
        {
            return Args(shooter, target, dmg, (byte)1, shot, (byte)5, 1003, (byte)3, 0, 0f);
        }

        sealed class RoomId
        {
            public readonly byte[] B;
            public RoomId(byte[] b) { B = b; }
        }

        // SyncObject: tag, int32 mask, int32 id, tagged fields by bit, bit 0 repeated
        sealed class Raw
        {
            public readonly byte[] B;
            public Raw(byte[] b) { B = b; }
        }

        static Raw Sync(byte tag, int id, object[] bitsAndValues)
        {
            int mask = 0;
            var fields = new SortedDictionary<int, byte[]>();
            for (int i = 0; i + 1 < bitsAndValues.Length; i += 2)
            {
                int bit = (int)bitsAndValues[i];
                mask |= 1 << bit;
                fields[bit] = Args(bitsAndValues[i + 1]);
            }
            var b = new List<byte> { tag };
            b.AddRange(BitConverter.GetBytes(mask));
            b.AddRange(BitConverter.GetBytes(id));
            foreach (byte[] f in fields.Values)
                b.AddRange(f);
            if (fields.ContainsKey(0))
                b.AddRange(fields[0]);
            return new Raw(b.ToArray());
        }

        // tagged args: byte 01, bool 03, short 04, int 06, float 0a, string 0c, bytes 0f, room id 2d; pre-tagged blobs raw
        static byte[] Args(params object[] args)
        {
            var b = new List<byte>();
            foreach (object a in args)
            {
                if (a is byte)
                {
                    b.Add(1);
                    b.Add((byte)a);
                }
                else if (a is bool)
                {
                    b.Add(3);
                    b.Add((bool)a ? (byte)1 : (byte)0);
                }
                else if (a is short)
                {
                    b.Add(4);
                    b.AddRange(BitConverter.GetBytes((short)a));
                }
                else if (a is int)
                {
                    b.Add(6);
                    b.AddRange(BitConverter.GetBytes((int)a));
                }
                else if (a is float)
                {
                    b.Add(10);
                    b.AddRange(BitConverter.GetBytes((float)a));
                }
                else if (a is string)
                {
                    string s = (string)a;
                    b.Add(12);
                    b.AddRange(BitConverter.GetBytes((short)s.Length));
                    b.AddRange(Encoding.Unicode.GetBytes(s));
                }
                else if (a is RoomId)
                {
                    b.Add(45);
                    b.AddRange(((RoomId)a).B);
                }
                else if (a is Raw)
                    b.AddRange(((Raw)a).B);
                else if (a is byte[])
                {
                    byte[] x = (byte[])a;
                    b.Add(15);
                    b.AddRange(BitConverter.GetBytes(x.Length));
                    b.AddRange(x);
                }
                else
                    throw new ArgumentException(a.GetType().Name);
            }
            return b.ToArray();
        }

        // leading primitive args; stops at the first tag it does not know
        static object[] DecodeArgs(byte[] d)
        {
            var o = new List<object>();
            int i = 0;
            while (d != null && i < d.Length)
            {
                byte t = d[i++];
                if (t == 1 && i + 1 <= d.Length) { o.Add(d[i]); i += 1; }
                else if (t == 3 && i + 1 <= d.Length) { o.Add(d[i] != 0); i += 1; }
                else if (t == 4 && i + 2 <= d.Length) { o.Add(BitConverter.ToInt16(d, i)); i += 2; }
                else if (t == 6 && i + 4 <= d.Length) { o.Add(BitConverter.ToInt32(d, i)); i += 4; }
                else if (t == 10 && i + 4 <= d.Length) { o.Add(BitConverter.ToSingle(d, i)); i += 4; }
                else if (t == 12 && i + 2 <= d.Length)
                {
                    int n = BitConverter.ToInt16(d, i);
                    i += 2;
                    if (i + 2 * n > d.Length)
                        break;
                    o.Add(Encoding.Unicode.GetString(d, i, 2 * n));
                    i += 2 * n;
                }
                else if (t == 15 && i + 4 <= d.Length)
                {
                    int n = BitConverter.ToInt32(d, i);
                    i += 4;
                    if (n < 0 || i + n > d.Length)
                        break;
                    o.Add(d.Skip(i).Take(n).ToArray());
                    i += n;
                }
                else
                    break;
            }
            return o.ToArray();
        }
    }
}
