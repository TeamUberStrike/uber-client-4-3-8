using System.Numerics;
using System.Text;

namespace UberStrike.Shared;

/// <summary>Thrown by the strict reader on ANY malformed/short/over-cap buffer. Never escapes
/// the Wire.TryDecode* boundary — callers see a bool and treat false as a schema violation.</summary>
public sealed class WireException : Exception
{
    public WireException(string msg) : base(msg) { }
}

public enum MsgType : byte
{
    Input = 1, Fire = 2, Switch = 3, Snapshot = 4, Hit = 5,
    Ping = 6, Pong = 7, Hello = 8, Welcome = 9,
}

/// <summary>
/// Phase 2 — binary wire format. Layout: [version byte][type byte][payload].
///
/// Quantization policy (the part that interacts with determinism):
///   - <see cref="Snapshot.Local"/> travels as RAW float bits. Reconciliation rebuilds
///     MoveState from it and replays inputs through the bit-deterministic movement step —
///     quantizing it would make every reconcile land ~1e-4 off and permanently trip the
///     desync detector. Own-state is one player per packet; the bytes are cheap.
///   - <see cref="Snapshot.Others"/> are quantized (pos 1/1024 u, vel 1/64 u/s, angles
///     u16 turns, health 0.1) — remotes are only ever interpolated visuals, never replayed.
///   - InputCmd travels as raw float bits: the server simulates FROM these exact values, and
///     the client predicted with them — both sides must read identical bits.
///
/// Snapshot delta compression: per-recipient, against the LAST SNAPSHOT SENT on that
/// connection (SnapshotEncoder/SnapshotDecoder pair). Sound because the production transport
/// is WebSocket = TCP: ordered + reliable, no gaps. The per-entity field mask only writes
/// changed groups; an entity unknown to the cache (first sight, or first send after a
/// reconnect's fresh encoder) writes all groups. Fog-of-war add/remove works because every
/// message lists ALL currently-visible entity ids — only field DATA is delta'd.
///
/// Strict reading: every read is bounds-checked, strings/arrays are length-capped, and
/// trailing bytes after a payload are rejected. TryDecode* never throws past the boundary.
/// </summary>
public static class Wire
{
    public const byte ProtocolVersion = 1;
    public const int  MaxTokenBytes   = 64;
    public const int  MaxOthers       = 32;   // > any UberStrike room size

    // --- quantization scales -----------------------------------------------------------
    public const float PosScale    = 1024f;     // 1/1024 unit ≈ 1 mm
    public const float VelScale    = 64f;       // ±512 u/s in i16 (MaxVerticalSpeed is 150)
    public const float AngleScale  = 65536f / (2f * MathF.PI); // u16 turns
    public const float HealthScale = 10f;

    public static bool PeekType(byte[] data, out MsgType type)
    {
        type = default;
        if (data.Length < 2 || data[0] != ProtocolVersion) return false;
        byte t = data[1];
        if (t < (byte)MsgType.Input || t > (byte)MsgType.Welcome) return false;
        type = (MsgType)t;
        return true;
    }

    // --- InputPacket ---------------------------------------------------------------------
    public static byte[] EncodeInput(in InputPacket p)
    {
        var w = new WireWriter(64);
        w.Header(MsgType.Input);
        w.I32(p.EntityId); w.Str(p.SessionToken);
        w.U32(p.Cmd.Seq); w.U32(p.Cmd.ClientTick);
        w.F32(p.Cmd.MoveDir.X); w.F32(p.Cmd.MoveDir.Z);   // Y is structurally zero on the wire
        w.U8((byte)((p.Cmd.Jump ? 1 : 0) | (p.Cmd.Crouch ? 2 : 0)));
        w.F32(p.Cmd.Yaw); w.F32(p.Cmd.Pitch);
        return w.ToArray();
    }

    public static bool TryDecodeInput(byte[] data, out InputPacket p)
    {
        p = default;
        try
        {
            var r = new WireReader(data); r.Header(MsgType.Input);
            p.EntityId = r.I32(); p.SessionToken = r.Str();
            p.Cmd.Seq = r.U32(); p.Cmd.ClientTick = r.U32();
            p.Cmd.MoveDir = new Vector3(r.F32(), 0f, r.F32());
            byte b = r.U8(); p.Cmd.Jump = (b & 1) != 0; p.Cmd.Crouch = (b & 2) != 0;
            p.Cmd.Yaw = r.F32(); p.Cmd.Pitch = r.F32();
            r.End();
            return true;
        }
        catch (WireException) { return false; }
    }

    // --- FireIntent ------------------------------------------------------------------------
    public static byte[] EncodeFire(in FireIntent f)
    {
        var w = new WireWriter(32);
        w.Header(MsgType.Fire);
        w.I32(f.EntityId); w.Str(f.SessionToken); w.U8((byte)f.Slot); w.U32(f.ClientTick);
        return w.ToArray();
    }

    public static bool TryDecodeFire(byte[] data, out FireIntent f)
    {
        f = default;
        try
        {
            var r = new WireReader(data); r.Header(MsgType.Fire);
            f.EntityId = r.I32(); f.SessionToken = r.Str(); f.Slot = r.U8(); f.ClientTick = r.U32();
            r.End();
            return true;
        }
        catch (WireException) { return false; }
    }

    // --- SwitchIntent ------------------------------------------------------------------------
    public static byte[] EncodeSwitch(in SwitchIntent s)
    {
        var w = new WireWriter(32);
        w.Header(MsgType.Switch);
        w.I32(s.EntityId); w.Str(s.SessionToken); w.U8((byte)s.Slot);
        return w.ToArray();
    }

    public static bool TryDecodeSwitch(byte[] data, out SwitchIntent s)
    {
        s = default;
        try
        {
            var r = new WireReader(data); r.Header(MsgType.Switch);
            s.EntityId = r.I32(); s.SessionToken = r.Str(); s.Slot = r.U8();
            r.End();
            return true;
        }
        catch (WireException) { return false; }
    }

    // --- HitEvent ------------------------------------------------------------------------
    public static byte[] EncodeHit(in HitEvent h)
    {
        var w = new WireWriter(48);
        w.Header(MsgType.Hit);
        w.I32(h.Shooter); w.I32(h.Target); w.F32(h.Damage);
        w.U8((byte)((h.Headshot ? 1 : 0) | (h.Killed ? 2 : 0)));
        w.QPos(h.Point);
        return w.ToArray();
    }

    public static bool TryDecodeHit(byte[] data, out HitEvent h)
    {
        h = default;
        try
        {
            var r = new WireReader(data); r.Header(MsgType.Hit);
            h.Shooter = r.I32(); h.Target = r.I32(); h.Damage = r.F32();
            byte b = r.U8(); h.Headshot = (b & 1) != 0; h.Killed = (b & 2) != 0;
            h.Point = r.QPos();
            r.End();
            return true;
        }
        catch (WireException) { return false; }
    }

    // --- Ping / Pong (clock sync + server-observed RTT) -----------------------------------
    public static byte[] EncodePing(double clientSendTime)
    {
        var w = new WireWriter(16); w.Header(MsgType.Ping); w.F64(clientSendTime); return w.ToArray();
    }

    public static bool TryDecodePing(byte[] data, out double clientSendTime)
    {
        clientSendTime = 0;
        try { var r = new WireReader(data); r.Header(MsgType.Ping); clientSendTime = r.F64(); r.End(); return true; }
        catch (WireException) { return false; }
    }

    public static byte[] EncodePong(double clientSendTime, double serverTime)
    {
        var w = new WireWriter(24); w.Header(MsgType.Pong); w.F64(clientSendTime); w.F64(serverTime); return w.ToArray();
    }

    public static bool TryDecodePong(byte[] data, out double clientSendTime, out double serverTime)
    {
        clientSendTime = 0; serverTime = 0;
        try
        {
            var r = new WireReader(data); r.Header(MsgType.Pong);
            clientSendTime = r.F64(); serverTime = r.F64(); r.End();
            return true;
        }
        catch (WireException) { return false; }
    }

    // --- Hello / Welcome (Phase 7 session handshake) ---------------------------------------
    public static byte[] EncodeHello(string sessionToken)
    {
        var w = new WireWriter(80); w.Header(MsgType.Hello); w.Str(sessionToken); return w.ToArray();
    }

    public static bool TryDecodeHello(byte[] data, out string sessionToken)
    {
        sessionToken = "";
        try { var r = new WireReader(data); r.Header(MsgType.Hello); sessionToken = r.Str(); r.End(); return true; }
        catch (WireException) { return false; }
    }

    public static byte[] EncodeWelcome(int entityId, float tickRate)
    {
        var w = new WireWriter(16); w.Header(MsgType.Welcome); w.I32(entityId); w.F32(tickRate); return w.ToArray();
    }

    public static bool TryDecodeWelcome(byte[] data, out int entityId, out float tickRate)
    {
        entityId = 0; tickRate = 0;
        try
        {
            var r = new WireReader(data); r.Header(MsgType.Welcome);
            entityId = r.I32(); tickRate = r.F32(); r.End();
            return true;
        }
        catch (WireException) { return false; }
    }
}

// ---- snapshot delta codec (stateful per connection) -------------------------------------

/// <summary>Quantized remote-player fields, grouped for the delta mask.</summary>
internal struct QuantOther
{
    public int   Px, Py, Pz;          // bit0
    public short Vx, Vy, Vz;          // bit1
    public ushort QYaw, QPitch;       // bit2
    public byte  Flags, Ungrounded;   // bit3
    public ushort QHealth;            // bit4
    public byte  Slot;                // bit5
    public byte  QSpeedScale;         // bit6

    public static QuantOther From(in PlayerSnap s) => new()
    {
        Px = Quant.PosQ(s.Position.X), Py = Quant.PosQ(s.Position.Y), Pz = Quant.PosQ(s.Position.Z),
        Vx = Quant.VelQ(s.Velocity.X), Vy = Quant.VelQ(s.Velocity.Y), Vz = Quant.VelQ(s.Velocity.Z),
        QYaw = Quant.AngleQ(s.Yaw), QPitch = Quant.AngleQ(s.Pitch),
        Flags = (byte)((s.Grounded ? 1 : 0) | (s.Jumping ? 2 : 0) | (s.Ducked ? 4 : 0) | (s.JumpArmed ? 8 : 0)),
        Ungrounded = s.UngroundedTicks,
        QHealth = (ushort)Math.Clamp((int)MathF.Round(MathF.Max(0f, s.Health) * Wire.HealthScale), 0, ushort.MaxValue),
        Slot = (byte)Math.Clamp(s.ActiveSlot, 0, 255),
        QSpeedScale = (byte)Math.Clamp((int)MathF.Round(MathF.Max(0f, s.SpeedScale) * 64f), 0, 255),
    };

    public PlayerSnap ToSnap(int entityId) => new()
    {
        EntityId = entityId,
        Position = new Vector3(Px / Wire.PosScale, Py / Wire.PosScale, Pz / Wire.PosScale),
        Velocity = new Vector3(Vx / Wire.VelScale, Vy / Wire.VelScale, Vz / Wire.VelScale),
        Yaw = QYaw / Wire.AngleScale, Pitch = Quant.UnwrapPitch(QPitch),
        Grounded = (Flags & 1) != 0, Jumping = (Flags & 2) != 0,
        Ducked = (Flags & 4) != 0, JumpArmed = (Flags & 8) != 0,
        UngroundedTicks = Ungrounded,
        Health = QHealth / Wire.HealthScale,
        ActiveSlot = Slot,
        SpeedScale = QSpeedScale / 64f,
    };
}

internal static class Quant
{
    public static int    PosQ(float v)   => (int)MathF.Round(v * Wire.PosScale);
    public static short  VelQ(float v)   => (short)Math.Clamp((int)MathF.Round(v * Wire.VelScale), short.MinValue, short.MaxValue);
    public static ushort AngleQ(float r) => (ushort)((int)MathF.Round(r * Wire.AngleScale) & 0xFFFF); // wraps mod 2π
    // pitch is physically in (-π/2, π/2); the u16 stores it mod 2π — values in the upper half
    // of the circle are the negative pitches.
    public static float UnwrapPitch(ushort q)
    {
        float a = q / Wire.AngleScale;
        return a > MathF.PI ? a - 2f * MathF.PI : a;
    }
}

/// <summary>
/// Per-connection snapshot encoder. Keeps the last quantized values SENT on this connection
/// and emits only changed field groups per entity. Create a FRESH instance per connection
/// (including reconnects) — the paired decoder must start from the same empty baseline.
/// </summary>
public sealed class SnapshotEncoder
{
    private readonly Dictionary<int, QuantOther> _lastSent = new();

    public byte[] Encode(in Snapshot s)
    {
        if (s.Others.Length > Wire.MaxOthers)
            throw new ArgumentException($"snapshot carries {s.Others.Length} others; cap is {Wire.MaxOthers}");

        var w = new WireWriter(64 + s.Others.Length * 32);
        w.Header(MsgType.Snapshot);
        w.F64(s.ServerTime);
        w.U32(s.LastProcessedInput);

        // Local: raw float bits — reconciliation replays from these exact values.
        PlayerSnap l = s.Local;
        w.I32(l.EntityId);
        w.F32(l.Position.X); w.F32(l.Position.Y); w.F32(l.Position.Z);
        w.F32(l.Velocity.X); w.F32(l.Velocity.Y); w.F32(l.Velocity.Z);
        w.F32(l.Yaw); w.F32(l.Pitch);
        w.U8((byte)((l.Grounded ? 1 : 0) | (l.Jumping ? 2 : 0) | (l.Ducked ? 4 : 0) | (l.JumpArmed ? 8 : 0)));
        w.U8(l.UngroundedTicks);
        w.F32(l.SpeedScale);
        w.F32(l.Health);
        w.U8((byte)Math.Clamp(l.ActiveSlot, 0, 255));
        w.U16((ushort)Math.Clamp(l.ActiveAmmo, 0, ushort.MaxValue));

        // Others: quantized + per-field-group delta vs the last values sent on this connection.
        w.U8((byte)s.Others.Length);
        foreach (PlayerSnap o in s.Others)
        {
            QuantOther q = QuantOther.From(o);
            bool known = _lastSent.TryGetValue(o.EntityId, out QuantOther prev);

            byte mask = 0;
            if (!known || q.Px != prev.Px || q.Py != prev.Py || q.Pz != prev.Pz)  mask |= 1 << 0;
            if (!known || q.Vx != prev.Vx || q.Vy != prev.Vy || q.Vz != prev.Vz)  mask |= 1 << 1;
            if (!known || q.QYaw != prev.QYaw || q.QPitch != prev.QPitch)         mask |= 1 << 2;
            if (!known || q.Flags != prev.Flags || q.Ungrounded != prev.Ungrounded) mask |= 1 << 3;
            if (!known || q.QHealth != prev.QHealth)                              mask |= 1 << 4;
            if (!known || q.Slot != prev.Slot)                                    mask |= 1 << 5;
            if (!known || q.QSpeedScale != prev.QSpeedScale)                      mask |= 1 << 6;

            w.I32(o.EntityId);
            w.U8(mask);
            if ((mask & (1 << 0)) != 0) { w.I32(q.Px); w.I32(q.Py); w.I32(q.Pz); }
            if ((mask & (1 << 1)) != 0) { w.I16(q.Vx); w.I16(q.Vy); w.I16(q.Vz); }
            if ((mask & (1 << 2)) != 0) { w.U16(q.QYaw); w.U16(q.QPitch); }
            if ((mask & (1 << 3)) != 0) { w.U8(q.Flags); w.U8(q.Ungrounded); }
            if ((mask & (1 << 4)) != 0) { w.U16(q.QHealth); }
            if ((mask & (1 << 5)) != 0) { w.U8(q.Slot); }
            if ((mask & (1 << 6)) != 0) { w.U8(q.QSpeedScale); }

            _lastSent[o.EntityId] = q;
        }
        return w.ToArray();
    }
}

/// <summary>
/// Per-connection snapshot decoder — the exact mirror of <see cref="SnapshotEncoder"/>.
/// Requires an ordered, reliable transport (WebSocket/TCP): a dropped or reordered message
/// would desync the delta baseline.
/// </summary>
public sealed class SnapshotDecoder
{
    private readonly Dictionary<int, QuantOther> _lastSeen = new();

    public bool TryDecode(byte[] data, out Snapshot s)
    {
        s = default;
        try
        {
            var r = new WireReader(data); r.Header(MsgType.Snapshot);
            s.ServerTime = r.F64();
            s.LastProcessedInput = r.U32();

            PlayerSnap l = default;
            l.EntityId = r.I32();
            l.Position = new Vector3(r.F32(), r.F32(), r.F32());
            l.Velocity = new Vector3(r.F32(), r.F32(), r.F32());
            l.Yaw = r.F32(); l.Pitch = r.F32();
            byte fl = r.U8();
            l.Grounded = (fl & 1) != 0; l.Jumping = (fl & 2) != 0;
            l.Ducked = (fl & 4) != 0; l.JumpArmed = (fl & 8) != 0;
            l.UngroundedTicks = r.U8();
            l.SpeedScale = r.F32();
            l.Health = r.F32();
            l.ActiveSlot = r.U8();
            l.ActiveAmmo = r.U16();
            s.Local = l;

            int count = r.U8();
            if (count > Wire.MaxOthers) throw new WireException("others over cap");
            var others = new PlayerSnap[count];
            for (int i = 0; i < count; i++)
            {
                int id = r.I32();
                byte mask = r.U8();
                _lastSeen.TryGetValue(id, out QuantOther q); // unknown id with partial mask
                bool known = _lastSeen.ContainsKey(id);      // would mean a codec bug/forgery
                if (!known && mask != 0x7F) throw new WireException("delta against unknown baseline");

                if ((mask & (1 << 0)) != 0) { q.Px = r.I32(); q.Py = r.I32(); q.Pz = r.I32(); }
                if ((mask & (1 << 1)) != 0) { q.Vx = r.I16(); q.Vy = r.I16(); q.Vz = r.I16(); }
                if ((mask & (1 << 2)) != 0) { q.QYaw = r.U16(); q.QPitch = r.U16(); }
                if ((mask & (1 << 3)) != 0) { q.Flags = r.U8(); q.Ungrounded = r.U8(); }
                if ((mask & (1 << 4)) != 0) { q.QHealth = r.U16(); }
                if ((mask & (1 << 5)) != 0) { q.Slot = r.U8(); }
                if ((mask & (1 << 6)) != 0) { q.QSpeedScale = r.U8(); }

                _lastSeen[id] = q;
                others[i] = q.ToSnap(id);
            }
            s.Others = others;
            r.End();
            return true;
        }
        catch (WireException) { s = default; return false; }
    }
}

// ---- low-level writer / strict reader ----------------------------------------------------

public sealed class WireWriter
{
    private byte[] _buf;
    private int _pos;

    public WireWriter(int capacity = 64) => _buf = new byte[capacity];

    public void Header(MsgType t) { U8(Wire.ProtocolVersion); U8((byte)t); }

    public void U8(byte v)  { Ensure(1); _buf[_pos++] = v; }
    public void I16(short v) { Ensure(2); _buf[_pos++] = (byte)v; _buf[_pos++] = (byte)(v >> 8); }
    public void U16(ushort v) { Ensure(2); _buf[_pos++] = (byte)v; _buf[_pos++] = (byte)(v >> 8); }
    public void I32(int v) { Ensure(4); for (int i = 0; i < 4; i++) _buf[_pos++] = (byte)(v >> (8 * i)); }
    public void U32(uint v) { Ensure(4); for (int i = 0; i < 4; i++) _buf[_pos++] = (byte)(v >> (8 * i)); }
    public void F32(float v) => I32(BitConverter.SingleToInt32Bits(v));
    public void F64(double v) { long b = BitConverter.DoubleToInt64Bits(v); Ensure(8); for (int i = 0; i < 8; i++) _buf[_pos++] = (byte)(b >> (8 * i)); }

    public void Str(string s)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(s ?? "");
        if (utf8.Length > Wire.MaxTokenBytes) throw new ArgumentException("string exceeds wire cap");
        U8((byte)utf8.Length);
        Ensure(utf8.Length);
        utf8.CopyTo(_buf, _pos);
        _pos += utf8.Length;
    }

    public void QPos(Vector3 p) { I32(Quant.PosQ(p.X)); I32(Quant.PosQ(p.Y)); I32(Quant.PosQ(p.Z)); }

    private void Ensure(int n)
    {
        if (_pos + n <= _buf.Length) return;
        int cap = _buf.Length * 2;
        while (cap < _pos + n) cap *= 2;
        Array.Resize(ref _buf, cap);
    }

    public byte[] ToArray()
    {
        var outBuf = new byte[_pos];
        Array.Copy(_buf, outBuf, _pos);
        return outBuf;
    }
}

/// <summary>Strict bounds-checked reader. Throws <see cref="WireException"/> on any overrun,
/// bad header, over-cap length, or trailing garbage — and nothing else.</summary>
public sealed class WireReader
{
    private readonly byte[] _buf;
    private int _pos;

    public WireReader(byte[] buf) => _buf = buf ?? throw new WireException("null buffer");

    public void Header(MsgType expected)
    {
        if (U8() != Wire.ProtocolVersion) throw new WireException("bad protocol version");
        if (U8() != (byte)expected) throw new WireException("unexpected message type");
    }

    public byte U8()
    {
        if (_pos + 1 > _buf.Length) throw new WireException("short buffer");
        return _buf[_pos++];
    }

    public short I16() { return (short)(U8() | (U8() << 8)); }
    public ushort U16() { return (ushort)(U8() | (U8() << 8)); }

    public int I32()
    {
        if (_pos + 4 > _buf.Length) throw new WireException("short buffer");
        int v = _buf[_pos] | (_buf[_pos + 1] << 8) | (_buf[_pos + 2] << 16) | (_buf[_pos + 3] << 24);
        _pos += 4;
        return v;
    }

    public uint U32() => unchecked((uint)I32());
    public float F32() => BitConverter.Int32BitsToSingle(I32());

    public double F64()
    {
        if (_pos + 8 > _buf.Length) throw new WireException("short buffer");
        long b = 0;
        for (int i = 7; i >= 0; i--) b = (b << 8) | _buf[_pos + i];
        _pos += 8;
        return BitConverter.Int64BitsToDouble(b);
    }

    public string Str()
    {
        int len = U8();
        if (len > Wire.MaxTokenBytes) throw new WireException("string over cap");
        if (_pos + len > _buf.Length) throw new WireException("short buffer");
        string s = Encoding.UTF8.GetString(_buf, _pos, len);
        _pos += len;
        return s;
    }

    public Vector3 QPos() => new(I32() / Wire.PosScale, I32() / Wire.PosScale, I32() / Wire.PosScale);

    /// <summary>Call after the last field: trailing bytes mean a malformed/forged packet.</summary>
    public void End()
    {
        if (_pos != _buf.Length) throw new WireException("trailing bytes");
    }
}
