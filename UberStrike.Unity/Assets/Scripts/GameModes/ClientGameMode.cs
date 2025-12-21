using System;
using System.Collections.Generic;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public abstract class ClientGameMode : ClientNetworkClass, IGameMode
{
    protected ClientGameMode(RemoteMethodInterface rmi, GameMetaData gameData)
        : base(rmi)
    {
        _gameData = gameData;
        _players = new Dictionary<int, UberStrike.Realtime.Common.CharacterInfo>();
    }

    public UberStrike.Realtime.Common.CharacterInfo GetPlayerWithID(int actorId)
    {
        UberStrike.Realtime.Common.CharacterInfo info;

        if (actorId == GameState.LocalCharacter.ActorId)
            return GameState.LocalCharacter;

        if (!Players.TryGetValue(actorId, out info))
        {
            //CmuneDebug.LogWarning("ClientGameMode:GetPlayerWithID({0}) returned NULL", playerID);
        }

        return info;
    }

    [NetworkMethod(GameRPC.Join)]
    protected virtual void OnPlayerJoined(SyncObject data, Vector3 position)
    {
        //CmuneDebug.LogError("ClientGameMode: OnPlayerJoined - Id = " + data.Id);
        if (data.IsEmpty)
        {
            CmuneDebug.LogError("ClientGameMode: OnPlayerJoined - SyncObject is empty!");
        }
        else
        {
            //ATTENTION
            //here we link MY CharacterInfo to the Server Syncronized instance!
            if (data.Id == GameState.LocalCharacter.ActorId)
            {
                GameState.LocalCharacter.ReadSyncData(data);

                //add CharacterInfo to the List (don't add if we are only spectator)
                if (!GameState.LocalCharacter.IsSpectator)
                {
                    Players[data.Id] = GameState.LocalCharacter;

                    HasJoinedGame = true;
                }
            }
            else
            {
                //add CharacterInfo to the List
                Players[data.Id] = new UberStrike.Realtime.Common.CharacterInfo(data);
                Players[data.Id].Position = position;
            }
        }
    }

    [NetworkMethod(GameRPC.Leave)]
    protected virtual void OnPlayerLeft(int actorId)
    {
        //remove the client from the list
        if (Players.Remove(actorId))
        {
            //if (CmuneDebug.IsDebugEnabled)
            //    CmuneDebug.Log("Player {0} left game", actorId);
        }
        //else
        //{
        //    CmuneDebug.LogError("onPlayerLeft {0}: Couldn't remove player because not existing", actorId);
        //}
        if (actorId == MyActorId)
        {
            HasJoinedGame = false;
        }
    }

    public bool HasJoinedGame
    {
        get;
        protected set;
    }

    [NetworkMethod(GameRPC.FullPlayerListUpdate)]
    protected virtual void OnFullPlayerListUpdate(List<SyncObject> data, List<Vector3> positions)
    {
        for (int i = 0; i < data.Count && i < positions.Count; i++)
        {
            OnPlayerJoined(data[i], positions[i]);
        }
    }

    [NetworkMethod(GameRPC.DeltaPlayerListUpdate)]
    protected virtual void OnGameFrameUpdate(List<SyncObject> data)
    {
        foreach (var d in data)
        {
            UberStrike.Realtime.Common.CharacterInfo player;
            if (!d.IsEmpty && Players.TryGetValue(d.Id, out player))
            {
                player.ReadSyncData(d);
            }
        }
    }

    [NetworkMethod(GameRPC.PlayerUpdate)]
    protected virtual void OnPlayerUpdate(SyncObject data)
    {
        UberStrike.Realtime.Common.CharacterInfo player;
        if (!data.IsEmpty && Players.TryGetValue(data.Id, out player))
        {
            player.ReadSyncData(data);
        }
    }

    [NetworkMethod(GameRPC.Begin)]
    protected virtual void OnStartGame() { IsGameStarted = true; }

    [NetworkMethod(GameRPC.End)]
    protected virtual void OnStopGame() { IsGameStarted = false; }

    #region PROPERTIES

    public bool IsMatchRunning { get; protected set; }

    public bool IsGameStarted { get; private set; }

    public GameMetaData GameData
    {
        get { return _gameData; }
    }

    public Dictionary<int, UberStrike.Realtime.Common.CharacterInfo> Players
    {
        get { return _players; }
    }

    public int MyActorId
    {
        get { return _rmi.Messenger.PeerListener.ActorId; }
    }

    #endregion

    #region FIELDS

    private Dictionary<int, UberStrike.Realtime.Common.CharacterInfo> _players;

    private GameMetaData _gameData;

    #endregion
}