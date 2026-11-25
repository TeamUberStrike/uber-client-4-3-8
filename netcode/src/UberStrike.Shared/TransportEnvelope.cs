namespace UberStrike.Shared;

/// <summary>
/// Phase 7.5 — the 4-byte little-endian monotonic frame-sequence prefix on every
/// client→server WebSocket frame. Kept OUT of the <see cref="Wire"/> message body: it's a
/// transport concern (anti-replay / anti-reorder), not part of the game protocol. The client
/// stamps an increasing sequence on each outbound frame; the server's TransportGuard rejects
/// any frame whose sequence replays, reorders, jumps, or floods. Server→client frames don't
/// carry it (the client isn't the threat to itself).
/// </summary>
public static class TransportEnvelope
{
    public const int PrefixBytes = 4;

    public static byte[] Wrap(uint seq, byte[] payload)
    {
        var outBuf = new byte[PrefixBytes + payload.Length];
        outBuf[0] = (byte)seq;
        outBuf[1] = (byte)(seq >> 8);
        outBuf[2] = (byte)(seq >> 16);
        outBuf[3] = (byte)(seq >> 24);
        System.Buffer.BlockCopy(payload, 0, outBuf, PrefixBytes, payload.Length);
        return outBuf;
    }

    /// <summary>Strip the prefix. Returns false if the frame is too short to carry one.</summary>
    public static bool TryUnwrap(byte[] frame, out uint seq, out byte[] payload)
    {
        seq = 0; payload = System.Array.Empty<byte>();
        if (frame == null || frame.Length < PrefixBytes) return false;
        seq = (uint)(frame[0] | (frame[1] << 8) | (frame[2] << 16) | (frame[3] << 24));
        payload = new byte[frame.Length - PrefixBytes];
        System.Buffer.BlockCopy(frame, PrefixBytes, payload, 0, payload.Length);
        return true;
    }
}
