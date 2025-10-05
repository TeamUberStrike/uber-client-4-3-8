
using System;
using System.Collections.Generic;
using Cmune.Realtime.Common;
using UberStrike.Realtime.Common;
using UnityEngine;

public class GameStateInterpolator
{
    public GameStateInterpolator()
    {
        _remoteStateByID = new Dictionary<int, RemoteCharacterState>(20);
        _remoteStateByNumber = new Dictionary<byte, RemoteCharacterState>(20);

        _internalState = INTERPOLATION_STATE.PAUSED;

        //GameState.DrawGUI += OnGUI;
    }

    //private void OnGUI()
    //{
    //    GUILayout.Label(debug1);
    //    GUILayout.Label(debug2);
    //    GUILayout.Label(debug3);
    //}

    public void Interpolate()
    {
        if (_internalState == INTERPOLATION_STATE.RUNNING)
        {
            foreach (RemoteCharacterState p in _remoteStateByID.Values)
            {
                //if (GameState.Instance.test)
                //p.UpdatePosition();
                //else
                p.Interpolate(GameConnectionManager.Client.PeerListener.ServerTimeTicks - GameState.PredictionTimeMax);
            }
        }
    }

    public void Run()
    {
        _internalState = INTERPOLATION_STATE.RUNNING;
    }

    public void Pause()
    {
        _internalState = INTERPOLATION_STATE.PAUSED;
    }

    public void UpdateCharacterInfo(SyncObject update)
    {
        RemoteCharacterState state;

        if (_remoteStateByID.TryGetValue(update.Id, out state))
        {
            //sync the update using the RemoteCharacterState interpolation
            state.RecieveDeltaUpdate(update);
        }
        else
        {
            Debug.LogWarning("UpdateCharacterInfo but state not found for actor " + update.Id);
        }
    }

    public void UpdatePositionSmooth(List<PlayerPosition> all)
    {
        RemoteCharacterState state;
        foreach (PlayerPosition p in all)
        {
            if (_remoteStateByNumber.TryGetValue(p.Player, out state))
            {
                state.UpdatePositionSmooth(p);
            }
        }
    }

    public void UpdatePositionHard(byte playerNumber, Vector3 pos)
    {
        RemoteCharacterState state;
        if (_remoteStateByNumber.TryGetValue(playerNumber, out state))
            state.SetHardPosition(GameConnectionManager.Client.PeerListener.ServerTimeTicks, pos);
        else
            Debug.LogWarning("UpdatePositionSmooth failed for " + playerNumber + " " + pos);
    }

    public void AddCharacterInfo(CharacterInfo user)
    {
        _remoteStateByID[user.ActorId] = new RemoteCharacterState(user);
        _remoteStateByNumber[user.PlayerNumber] = _remoteStateByID[user.ActorId];
    }

    public void RemoveCharacterInfo(int playerID)
    {
        RemoteCharacterState state;
        if (_remoteStateByID.TryGetValue(playerID, out state))
        {
            _remoteStateByNumber.Remove(state.Info.PlayerNumber);
        }
    }

    public RemoteCharacterState GetState(int playerID)
    {
        RemoteCharacterState state;

        if (_remoteStateByID.TryGetValue(playerID, out state))
        {
            return state;
        }
        else
        {
            throw new Exception(string.Format("GameStateInterpolator:GetPlayerState({0}) failed because CharacterState was not inserted", playerID));
        }
    }

    #region PROPERTIES

    public float SimulationFrequency
    {
        get { return _timeDelta; }
        set { _timeDelta = value; }
    }

    #endregion

    #region FIELDS
    private Dictionary<int, RemoteCharacterState> _remoteStateByID;
    private Dictionary<byte, RemoteCharacterState> _remoteStateByNumber;

    private float _timeDelta = 0.01f;
    //private float _interpolationFrequency = 0.03f;
    //private float _lastInterpolation = 0;

    private INTERPOLATION_STATE _internalState;
    #endregion

    public enum INTERPOLATION_STATE
    {
        RUNNING,
        PAUSED,
    }
}
