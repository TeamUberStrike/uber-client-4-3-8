
using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class GameConnectionManager : AutoMonoBehaviour<GameConnectionManager>
{
    private void Awake()
    {
        _client = new PhotonClient(this, true);
        _client.PeerListener.SubscribeToEvents(OnEventCallback);
    }

    private void Update()
    {
        if (_client != null && _syncTime <= Time.realtimeSinceStartup)
        {
            _syncTime = Time.realtimeSinceStartup + 0.02f;
            _client.Update();
        }
    }

    private void OnGUI()
    {
        //if (Client != null && Client.PeerListener != null)
        //{
        //    GUI.Label(new Rect(100, 120, 200, 20), Client.PeerListener.ServerTimeTicks + " || " + TimeSpan.FromMilliseconds(Client.PeerListener.ServerTimeTicks));
        //    GUI.Label(new Rect(100, 140, 200, 20), SystemTime.Running + " || " + TimeSpan.FromMilliseconds(SystemTime.Running));
        //    if (GUI.Button(new Rect(100, 100, 100, 20), "Check"))
        //    {
        //        Client.PeerListener.FetchServerTime();
        //    }
        //}

        if (GameState.HasCurrentGame && GameState.CurrentGame.NetworkID != -1 &&
            Client != null && Client.ConnectionState != PhotonClient.ConnectionStatus.RUNNING)
        {
            Rect rect = new Rect((GUITools.ScreenWidth - 320) * 0.5f, (GUITools.ScreenHeight - 240 - 56) * 0.5f, 320, 240);

            GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
            GUI.Label(new Rect(0, 0, 320, 56), LocalizedStrings.PleaseWait, BlueStonez.tab_strip);

            if (Client.ConnectionState == PhotonClient.ConnectionStatus.STARTING)
            {
                GUI.Button(new Rect(17, 55, 286, 140), LocalizedStrings.ConnectingToServer, BlueStonez.label_interparkbold_11pt);
            }
            else if (Client.ConnectionState == PhotonClient.ConnectionStatus.STOPPED)
            {
                GUI.Button(new Rect(17, 55, 286, 140), LocalizedStrings.ServerError, BlueStonez.label_interparkbold_11pt);

                if (GUITools.Button(new Rect(100, 200, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button))
                {
                    GameStateController.Instance.UnloadGameMode();
                }
            }
            GUI.EndGroup();
        }
    }

    private void Reconnect()
    {
        StartCoroutine(StartReconnectionInSeconds(1));
    }

    private void CloseGame()
    {
        GameStateController.Instance.UnloadLevelAndLoadPage(PageType.Play);
    }

    private void CloseGameAndBackToServerSelection()
    {
        GameServerController.Instance.SelectedServer = null;

        GameStateController.Instance.UnloadLevelAndLoadPage(PageType.Play);
    }

    private void OnRequestRoomMetaDataCallback(int result, object[] table)
    {
        _reponseArrived = true;
        if (table.Length > 0)
        {
            _requestedGameData = (GameMetaData)table[0];
        }
    }

    private void OnEventCallback(PhotonPeerListener.ConnectionEvent ev)
    {
        switch (ev.Type)
        {
            case PhotonPeerListener.ConnectionEventType.JoinFailed:
                {
                    _joinFullHackTime = Time.time + 2;

                    switch (ev.ErrorCode)
                    {
                        case 4:
                            PopupSystem.ShowMessage("Server Full", "The server is currently full!\nDo you want to try again?", PopupSystem.AlertType.OKCancel, Reconnect, CloseGame);
                            break;
                        default:
                            PopupSystem.ShowMessage("Game Full", "The game is currently full!\nDo you want to try again?", PopupSystem.AlertType.OKCancel, Reconnect, CloseGame);
                            break;
                    }
                    break;
                }
            case PhotonPeerListener.ConnectionEventType.Disconnected:
                {
                    if (_isConnectionStarted && _joinFullHackTime < Time.time)
                    {
                        PopupSystem.ShowMessage("Connection Error", "You lost the connection to our server!\nDo you want to reconnect?", PopupSystem.AlertType.OKCancel, Reconnect, CloseGameAndBackToServerSelection);
                        Screen.lockCursor = false;
                    }

                    break;
                }
            case PhotonPeerListener.ConnectionEventType.JoinedRoom:
                {
                    if (CmuneDebug.IsDebugEnabled)
                        CmuneDebug.Log("JoinedRoom: " + ev.Room);

                    if (_gameRoom != null)
                        _gameRoom.RoomID = ev.Room;
                    break;
                }
        }
    }

    protected void OnApplicationQuit()
    {
        if (_client != null)
            _client.ShutDown();
    }

    private IEnumerator StartReconnectionInSeconds(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (_gameRoom != null)
        {
            _client.ConnectToRoom(_gameRoom, PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);
        }
        else
        {
            CmuneDebug.LogError("Failed to reconnect because GameRoom is null!");
        }
    }

    private IEnumerator StartRequestRoomMetaData(CmuneRoomID room, Action<int, GameMetaData> action)
    {
        _gameRoom = null;
        _isConnectionStarted = false;
        if (GameState.HasCurrentGame)
            GameState.CurrentGame.Leave();
        yield return Client.Disconnect();

        yield return new WaitForEndOfFrame();

        _isConnectionStarted = true;
        yield return Client.ConnectToServer(room.Server, PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);

        if (Client.IsConnected)
        {
            //send a request to the server to to return the gameMetaDatat of a certain room
            Client.Rmi.Messenger.SendOperationToServerApplication(OnRequestRoomMetaDataCallback, GameApplicationRPC.RoomRequest, room.Number);

            //here we are waiting for the return (call to OnRequestRoomMetaData)
            float timeout = 5;
            _reponseArrived = false;
            while (!_reponseArrived && timeout > 0)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }

            //check if we just got a timeout or an actual return
            if (_reponseArrived && !_requestedGameData.RoomID.IsEmpty)
            {
                action(0, _requestedGameData);
            }
            else
            {
                action(1, GameMetaData.Empty);
            }
        }
        else
        {
            action(2, GameMetaData.Empty);
        }
    }

    public static void Start(GameMetaData game)
    {
        Instance._isConnectionStarted = true;
        Instance._gameRoom = game;
        Instance._client.ConnectToRoom(game, PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);
    }

    public static void Stop()
    {
        if (GameState.HasCurrentGame)
            GameState.CurrentGame.Leave();

        Instance._gameRoom = null;

        Instance._isConnectionStarted = false;

        Instance._client.Disconnect();
    }

    public static void RequestRoomMetaData(CmuneRoomID room, Action<int, GameMetaData> action)
    {
        Instance.StartCoroutine(Instance.StartRequestRoomMetaData(room, action));
    }

    public bool IsConnectedToServer(string server)
    {
        return _client.IsConnected && _client.CurrentConnection == server;
    }

    #region PROPERTIES

    public static bool IsConnected
    {
        get
        {
            return Instance._client.ConnectionState == PhotonClient.ConnectionStatus.RUNNING;
        }
    }

    public static RemoteMethodInterface Rmi
    {
        get { return Instance._client.Rmi; }
    }

    public static PhotonClient Client
    {
        get { return Instance._client; }
    }

    public static string CurrentRoomID
    {
        get
        {
            return Instance._client.PeerListener.CurrentRoom.ID;
        }
    }

    public static int CurrentPlayerID
    {
        get
        {
            return Instance._client.PeerListener.ActorId;
        }
    }

    #endregion

    #region FIELDS

    private float _syncTime = 0;
    private float _joinFullHackTime = 0;
    private bool _reponseArrived = false;
    private bool _isConnectionStarted = false;

    private PhotonClient _client;
    private GameMetaData _gameRoom;
    private GameMetaData _requestedGameData = GameMetaData.Empty;

    #endregion
}