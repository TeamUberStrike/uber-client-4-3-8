using System;
using System.Collections;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;
using Cmune.Realtime.Photon.Client.Network;
using UnityEngine;

public class CommConnectionManager : AutoMonoBehaviour<CommConnectionManager>
{
    private void Awake()
    {
        _client = new PhotonClient(this, true);

        _commCenter = new ClientCommCenter(_client.Rmi);
    }

    private void Start()
    {
        GameConnectionManager.Client.PeerListener.SubscribeToEvents(OnGameConnectionChange);

        StartCoroutine(StartCheckingCommServerConnection());
    }

    private void Update()
    {
        if (_client != null && _syncTime <= Time.time)
        {
            _syncTime = Time.time + 0.02f;
            _client.Update();
        }

        //POLL FRIENDS ONLINE STATUS EVERY 5 SECONDS
        if (_pollFriendsOnlineStatus < Time.time)
        {
            _pollFriendsOnlineStatus = Time.time + 30;

            if (MenuPageManager.IsCurrentPage(PageType.Chat) || MenuPageManager.IsCurrentPage(PageType.Inbox) || MenuPageManager.IsCurrentPage(PageType.Clans))
            {
                CommCenter.UpdateContacts();
            }
        }
    }

    public static bool TryGetActor(int cmid, out CommActorInfo actor)
    {
        if (cmid > 0 && CommCenter != null)
        {
            return CommCenter.TryGetActorWithCmid(cmid, out actor) && actor != null;
        }
        else
        {
            actor = null;
            return false;
        }
    }

    public static bool IsPlayerOnline(int cmid)
    {
        if (cmid > 0 && CommCenter != null)
        {
            return CommCenter.HasActorWithCmid(cmid);
        }
        else
        {
            return false;
        }
    }

    protected void OnApplicationQuit()
    {
        if (_client != null)
            _client.ShutDown();
    }

    private IEnumerator StartCheckingCommServerConnection()
    {
        while (true)
        {
            yield return new WaitForSeconds(5);

            if (_client.ConnectionState == PhotonClient.ConnectionStatus.STOPPED && CmuneNetworkManager.CurrentCommServer.IsValid && PlayerDataManager.IsPlayerLoggedIn)
            {
                _client.ConnectToRoom(new RoomMetaData(StaticRoomID.CommCenter, "The CommServer", CmuneNetworkManager.CurrentCommServer.ConnectionString), PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);
            }
        }
    }

    public static void Stop()
    {
        if (Instance._client != null)
        {
            Instance._commCenter.Leave();
            Instance._client.Disconnect();
        }
    }

    public static void Request(byte applicationMethod, Action<int, object[]> callback, params object[] args)
    {
        ServerRequest.Run(Instance, CmuneNetworkManager.CurrentCommServer.ConnectionString, callback, applicationMethod, args);
    }

    private void OnGameConnectionChange(PhotonPeerListener.ConnectionEvent ev)
    {
        if (_commCenter.IsInitialized)
        {
            switch (ev.Type)
            {
                case PhotonPeerListener.ConnectionEventType.JoinedRoom:
                    {
                        _commCenter.UpdatePlayerRoom(ev.Room);
                        break;
                    }
                case PhotonPeerListener.ConnectionEventType.LeftRoom:
                    {
                        _commCenter.ResetPlayerRoom();
                        break;
                    }
            }
        }
    }

    #region Properties

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

    public static CmuneRoomID CurrentRoomID
    {
        get
        {
            return Instance._client.PeerListener.CurrentRoom;
        }
    }

    public static ClientCommCenter CommCenter
    {
        get { return Instance._commCenter; }
    }

    public static bool IsConnected
    {
        get { return Instance._client.IsConnected; }
    }

    #endregion

    #region Fields
    private PhotonClient _client;
    private ClientCommCenter _commCenter;
    private float _syncTime = 0;
    private float _pollFriendsOnlineStatus;
    #endregion
}
