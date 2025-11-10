//#pragma warning disable 0169

using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;
using Cmune.Util;
using UnityEngine;

public class LobbyConnectionManager : AutoMonoBehaviour<LobbyConnectionManager>
{
    private void Awake()
    {
        _client = new PhotonClient(this, true);
        //_client.PeerListener.EnableNetworkSimulation(true, 500, 500);

        _lobbyCenter = new ClientLobbyCenter(_client.Rmi);

        _client.PeerListener.SubscribeToEvents(OnEventCallback);
    }

    private void OnEventCallback(PhotonPeerListener.ConnectionEvent ev)
    {
        switch (ev.Type)
        {
            case PhotonPeerListener.ConnectionEventType.Disconnected:
                {
                    GameListManager.ClearGameList();
                    break;
                }
        }
    }

    public static void StartConnection()
    {
        if (!Instance._client.IsConnected)
        {
            if (CmuneNetworkManager.CurrentLobbyServer.IsValid)
            {
                RoomMetaData lobby = new RoomMetaData(StaticRoomID.LobbyCenter, "The Lobby", CmuneNetworkManager.CurrentLobbyServer.ConnectionString);

                Instance._client.ConnectToRoom(lobby, PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);
            }
        }
        //else
        //    CmuneDebug.LogWarning("Lobby is not initialized!\nIsConnected = {0}", Instance._client.IsConnected);
    }

    public static void Stop()
    {
        if (Instance._client.IsConnected)
        {
            Instance._lobbyCenter.Leave();
            Instance._client.Disconnect();
        }
    }

    private float _syncTime = 0;
    private void Update()
    {
        if (_client != null && _syncTime <= Time.time)
        {
            _syncTime = Time.time + 0.02f;
            _client.Update();
        }
    }

    protected void OnApplicationQuit()
    {
        if (_client != null)
            _client.ShutDown();
    }

    #region PROPERTIES

    public static RemoteMethodInterface Rmi
    {
        get { return Instance._client.Rmi; }
    }

    public static PhotonClient Client
    {
        get { return Instance._client; }
    }

    public static int CurrentPlayerID
    {
        get
        {
            return Instance._client.PeerListener.ActorId;
        }
    }

    public static bool IsConnected
    {
        get { return Instance._client.IsConnected; }
    }

    public static bool IsConnecting
    {
        get { return Instance._client.ConnectionState == PhotonClient.ConnectionStatus.STARTING; }
    }


    public static bool IsInLobby
    {
        get { return Instance._client.PeerListener.HasJoinedRoom; }
    }

    #endregion

    #region FIELDS
    private PhotonClient _client;

    /// <summary>
    /// don't remove this field  - otherwise the lobby will not work anymore
    /// </summary>
#pragma warning disable 0414
    private ClientLobbyCenter _lobbyCenter;
#pragma warning restore

    #endregion
}