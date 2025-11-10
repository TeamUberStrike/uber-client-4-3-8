using Cmune.Realtime.Photon.Client;
using Cmune.Util;
using UnityEngine;

public class DebugServerState : IDebugPage
{
    #region IPPDebugPage Members

    public string Title
    {
        get { return "Servers"; }
    }

    public void Draw()
    {
        GUILayout.Label("ALL SERVERS");
        foreach (var server in GameServerManager.Instance.PhotonServerList)
        {
            GUILayout.Label("  " + server.ConnectionString + " " + server.Latency);
        }

        if (CmuneNetworkManager.CurrentGameServer != null)
        {
            GUILayout.Space(10);
            GUILayout.Label(string.Format("GAMESERVER: {0}, isValid: {1}", CmuneNetworkManager.CurrentGameServer.ConnectionString, CmuneNetworkManager.CurrentGameServer.IsValid));
            GUILayout.Label("  Room ID: " + GameConnectionManager.CurrentRoomID);
            GUILayout.Label("  Player ID: " + GameConnectionManager.CurrentPlayerID);
            GUILayout.Label("  Network Time: " + GameConnectionManager.Client.PeerListener.ServerTimeTicks);
            GUILayout.Label("  KBytes IN: " + ConvertBytes.ToKiloBytes(GameConnectionManager.Client.PeerListener.IncomingBytes).ToString("f2"));
            GUILayout.Label("  KBytes OUT: " + ConvertBytes.ToKiloBytes(GameConnectionManager.Client.PeerListener.OutgoingBytes).ToString("f2"));
        }

        if (CmuneNetworkManager.CurrentLobbyServer != null)
        {
            GUILayout.Space(10);
            GUILayout.Label(string.Format("LOBBYSERVER: {0}, isValid: {1}", CmuneNetworkManager.CurrentLobbyServer.ConnectionString, CmuneNetworkManager.CurrentLobbyServer.IsValid));
            GUILayout.Label("  Player ID: " + LobbyConnectionManager.CurrentPlayerID);
            GUILayout.Label("  Network Time: " + LobbyConnectionManager.Client.PeerListener.ServerTimeTicks);
            GUILayout.Label("  KBytes IN: " + ConvertBytes.ToKiloBytes(LobbyConnectionManager.Client.PeerListener.IncomingBytes).ToString("f2"));
            GUILayout.Label("  KBytes OUT: " + ConvertBytes.ToKiloBytes(LobbyConnectionManager.Client.PeerListener.OutgoingBytes).ToString("f2"));
        }

        if (CmuneNetworkManager.CurrentCommServer != null)
        {
            GUILayout.Space(10);
            GUILayout.Label(string.Format("COMMSERVER: {0}, isValid: {1}", CmuneNetworkManager.CurrentCommServer.ConnectionString, CmuneNetworkManager.CurrentCommServer.IsValid));
            GUILayout.Label("  Player ID: " + CommConnectionManager.CurrentPlayerID);
            GUILayout.Label("  Network Time: " + CommConnectionManager.Client.PeerListener.ServerTimeTicks);
            GUILayout.Label("  KBytes IN: " + ConvertBytes.ToKiloBytes(CommConnectionManager.Client.PeerListener.IncomingBytes).ToString("f2"));
            GUILayout.Label("  KBytes OUT: " + ConvertBytes.ToKiloBytes(CommConnectionManager.Client.PeerListener.OutgoingBytes).ToString("f2"));
        }
    }

    #endregion
}