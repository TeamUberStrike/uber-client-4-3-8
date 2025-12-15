using System;
using System.Text;
using Cmune.Realtime.Common;
using UberStrike.Realtime.Common;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class RemoteCharacterState : ICharacterState
{
    private int _counter = 0;
    private int _lastInterpolation = 0;
    private BezierSplines _interpolator;
    private UberStrike.Realtime.Common.CharacterInfo _currentState;
    private event Action<SyncObject> _updateRecievedEvent;

    public UberStrike.Realtime.Common.CharacterInfo Info
    {
        get { return _currentState; }
    }

    public Vector3 LastPosition
    {
        get { return _interpolator.LatestPosition(); }
    }


    public BezierSplines GetPositionInterpolator()
    {
        return _interpolator;
    }


    public RemoteCharacterState(UberStrike.Realtime.Common.CharacterInfo info)
    {
        _currentState = info;

        _interpolator = new BezierSplines();

        SetHardPosition(GameConnectionManager.Client.PeerListener.ServerTimeTicks, info.Position);
    }

    public void RecieveDeltaUpdate(SyncObject delta)
    {
        //update current state
        _currentState.ReadSyncData(delta);

        if (_updateRecievedEvent != null)
            _updateRecievedEvent(delta);
    }

    public void SubscribeToEvents(CharacterConfig config)
    {
        _updateRecievedEvent = null;
        _updateRecievedEvent += config.OnCharacterStateUpdated;
    }

    public void UnSubscribeAll()
    {
        _updateRecievedEvent = null;
    }

    public void UpdatePositionSmooth(PlayerPosition p)
    {
        _counter++;
        _interpolator.AddSample(p.Time, p.Position);
    }

    public void SetHardPosition(int time, Vector3 pos)
    {
        _interpolator.Packets.Clear();
        _interpolator.LastPacket.Time = 0;
        _interpolator.AddSample(time, pos);
        _interpolator.PreviousPacket = _interpolator.LastPacket;
        _currentState.Position = pos;
    }

    public void Interpolate(int time)
    {
        if (_currentState.IsAlive)
        {
            Vector3 pos;

            _lastInterpolation = _interpolator.ReadPosition(time, out pos);

            if (_lastInterpolation > 0)
            {
                _currentState.Position = pos;
                _currentState.Distance = Vector3.Distance(_interpolator.LastPacket.Pos, pos);
            }
        }
    }

    internal void UpdatePosition()
    {
        Vector3 direction = (_interpolator.LastPacket.Pos - _interpolator.PreviousPacket.Pos);// Mathf.Min(_interpolator.LastPacket.GameTime - _interpolator.PreviousPacket.GameTime, 1);
        Vector3 destination = _interpolator.PreviousPacket.Pos + (direction * Time.deltaTime);
        _currentState.Position = Vector3.Lerp(_currentState.Position, destination, Time.deltaTime * GameState.InterpolationFactor);
        _currentState.Position.y = Mathf.Lerp(_currentState.Position.y, _interpolator.LastPacket.Pos.y, Time.deltaTime * GameState.InterpolationFactor);
        _currentState.Velocity = direction.magnitude;
        _currentState.Distance = Vector3.Distance(_interpolator.LastPacket.Pos, _currentState.Position);
    }

    public string DebugAll()
    {
        StringBuilder b = new StringBuilder();
        b.AppendFormat("time: {0}/{1} ", _lastInterpolation, _interpolator.Packets.Count);
        b.AppendFormat("cntr: {0}\n", _counter);
        return b.ToString();
    }
}
