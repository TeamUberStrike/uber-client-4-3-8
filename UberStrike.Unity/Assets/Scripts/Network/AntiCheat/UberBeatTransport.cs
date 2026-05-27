using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cmune.Realtime.Photon.Client;
using UberStrike.Core.Serialization;
using UnityEngine;

/// <summary>
/// Reaches the private <c>_peer</c> field on <see cref="PhotonPeerListener"/> and sends raw
/// Photon ops 27/28/29 directly. The 4.3.8 client SDK's typed RPC layer
/// (<c>SendMethodToServer</c> → CommRPC byte → Photon op 66 wrapper) does not expose UberBeat
/// operations because UBZ added them server-side after Cmune froze the 4.3.8 SDK. UBZ's
/// <c>BaseLobbyRoomOperationHandler.OnOperationRequest</c> reads <c>Parameters.Keys.First()</c>
/// as the handler ID (0 for the lobby handler) and <c>Parameters[handlerId]</c> as a byte[]
/// payload — which is exactly how the patched 4.7.1 client wires it.
/// </summary>
internal static class UberBeatTransport
{
    public const byte OpRequestModules = 27;
    public const byte OpUberBeatAuthenticate = 28;
    public const byte OpUberBeatReport = 29;

    private const byte LobbyHandlerId = 0;

    private static FieldInfo s_peerField;
    private static MethodInfo s_opCustom;

    public static bool TrySendString(byte opCode, string payload)
    {
        PhotonClient client;
        try { client = CommConnectionManager.Client; }
        catch { return false; }
        var listener = client?.PeerListener;
        if (listener == null || !listener.IsConnectedToServer) return false;

        object peer = ResolvePeer(listener);
        if (peer == null) return false;

        byte[] data;
        using (var ms = new MemoryStream())
        {
            StringProxy.Serialize(ms, payload ?? string.Empty);
            data = ms.ToArray();
        }

        var parameters = new Dictionary<byte, object> { { LobbyHandlerId, data } };
        return InvokeOpCustom(peer, opCode, parameters, sendReliable: true);
    }

    private static object ResolvePeer(PhotonPeerListener listener)
    {
        if (s_peerField == null)
        {
            s_peerField = typeof(PhotonPeerListener)
                .GetField("_peer", BindingFlags.Instance | BindingFlags.NonPublic);
            if (s_peerField == null)
            {
                Debug.LogWarning("[UberBeat] PhotonPeerListener._peer field not found via reflection.");
                return null;
            }
        }
        return s_peerField.GetValue(listener);
    }

    private static bool InvokeOpCustom(object peer, byte opCode, Dictionary<byte, object> parameters, bool sendReliable)
    {
        if (s_opCustom == null)
        {
            var t = peer.GetType();
            s_opCustom = t.GetMethod("OpCustom",
                new[] { typeof(byte), typeof(Dictionary<byte, object>), typeof(bool) });
            if (s_opCustom == null)
            {
                s_opCustom = t.GetMethod("OpCustom",
                    new[] { typeof(byte), typeof(Dictionary<byte, object>), typeof(bool), typeof(byte) });
            }
            if (s_opCustom == null)
            {
                Debug.LogWarning("[UberBeat] PhotonPeer.OpCustom not found via reflection.");
                return false;
            }
        }

        try
        {
            object[] args = s_opCustom.GetParameters().Length == 4
                ? new object[] { opCode, parameters, sendReliable, (byte)0 }
                : new object[] { opCode, parameters, sendReliable };
            object result = s_opCustom.Invoke(peer, args);
            return result is bool b ? b : true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UberBeat] OpCustom invoke failed: " + ex.Message);
            return false;
        }
    }
}
