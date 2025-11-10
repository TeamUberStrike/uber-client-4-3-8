using UnityEngine;

public class KillComboCounter
{
    public void OnKillEnemy()
    {
        if (Time.time < _lastKillTime + MultiKillInterval)
        {
            _killCounter++;
            _lastKillTime = Time.time;
            PopupHud.Instance.PopupMultiKill(_killCounter); // TODO: later dispatch event here
        }
        else
        {
            _killCounter = 1;
            _lastKillTime = Time.time;
        }
    }

    public void ResetCounter()
    {
        _killCounter = 0;
    }

    #region Private fields

    private const float MultiKillInterval = 10;
    private float _lastKillTime;
    private int _killCounter;

    #endregion
}

