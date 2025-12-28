using UnityEngine;

class InGameCountdown
{
    public int EndTime { get; set; }

    public int RemainingSeconds 
    {
        get { return _remainingSeconds; }
        private set
        {
            if (_remainingSeconds != value)
            {
                _remainingSeconds = value;
                OnUpdateRemainingSeconds();
            }
        }
    }

    public void Stop()
    {
        RemainingSeconds = 0;
    }

    public void Update()
    {
        int fullSecond = Mathf.CeilToInt((EndTime - GameConnectionManager.Client.PeerListener.ServerTimeTicks) / 1000f);
        if (RemainingSeconds != fullSecond)
        {
            MatchStatusHud.Instance.RemainingSeconds = fullSecond;
            RemainingSeconds = fullSecond;
        }
    }

    private void OnUpdateRemainingSeconds()
    {
        MatchStatusHud.Instance.RemainingSeconds = _remainingSeconds;
    }

    private int _remainingSeconds;
}