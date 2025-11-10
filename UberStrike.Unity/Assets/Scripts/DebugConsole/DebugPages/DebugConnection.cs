using Cmune.Realtime.Common;
using UberStrike.Realtime.Common;
using UnityEngine;

public class DebugConnection : IDebugPage
{
    public string Title
    {
        get { return "Connection"; }
    }

    // Use this for initialization
    public void Draw()
    {
        if (GUI.Button(new Rect(20, 70, 150, 20), "Comm Disconnect"))
        {
            CommConnectionManager.Stop();//.Client.Disconnect();
        }
        if (GUI.Button(new Rect(180, 70, 150, 20), "Comm Connect"))
        {
            CommConnectionManager.Client.ConnectToRoom(new RoomMetaData(StaticRoomID.CommCenter, "", CmuneNetworkManager.CurrentCommServer.ConnectionString), PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);
        }
        GUI.Label(new Rect(360, 70, 600, 20), CommConnectionManager.Client.Debug);


        if (GUI.Button(new Rect(20, 100, 150, 20), "Lobby Disconnect"))
        {
            LobbyConnectionManager.Stop();
        }
        if (GUI.Button(new Rect(180, 100, 150, 20), "Lobby Connect"))
        {
            LobbyConnectionManager.Client.ConnectToRoom(new RoomMetaData(StaticRoomID.LobbyCenter, "", CmuneNetworkManager.CurrentLobbyServer.ConnectionString), PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);
        }
        GUI.Label(new Rect(360, 100, 600, 20), LobbyConnectionManager.Client.Debug);


        if (GUI.Button(new Rect(20, 130, 150, 20), "Game Disconnect"))
        {
            GameConnectionManager.Stop();
        }
        if (GUI.Button(new Rect(180, 130, 150, 20), "Game Connect"))
        {
            GameConnectionManager.Client.ConnectToRoom(new GameMetaData(StaticRoomID.Auto, "", CmuneNetworkManager.CurrentGameServer.ConnectionString), PlayerDataManager.CmidSecure, PlayerDataManager.AccessLevelSecure);
        }
        GUI.Label(new Rect(360, 130, 600, 20), GameConnectionManager.Client.Debug);
    }
}
