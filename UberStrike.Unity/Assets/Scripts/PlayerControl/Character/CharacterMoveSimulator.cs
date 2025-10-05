using UnityEngine;
using UberStrike.Realtime.Common;

public class CharacterMoveSimulator
{
    public CharacterMoveSimulator(Transform transform)
    {
        _transform = transform;
    }

    public void Update(CharacterInfo state)
    {
        if (state != null)
        {
            _transform.localPosition = state.Position;
            _transform.localRotation = Quaternion.Lerp(_transform.rotation, state.HorizontalRotation, Time.deltaTime * 5);

            if (_positionObserver != null)
                _positionObserver.Notify();
        }
    }

    /// <summary>
    /// Add an observer which has interest in the position
    /// </summary>
    /// <param name="observer"></param>
    public void AddPositionObserver(IObserver observer)
    {
        _positionObserver = observer;
    }

    /// <summary>
    /// 
    /// </summary>
    public void RemovePositionObserver()
    {
        _positionObserver = null;
    }

    #region Fields
    private Transform _transform;
    private IObserver _positionObserver;
    #endregion
}