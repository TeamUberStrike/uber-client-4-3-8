using UnityEngine;

public class AbstractAnim : IAnim
{
    public bool IsAnimating { get; set; }

    public float Duration { get; set; }

    public float StartTime { get; set; }

    public void Start()
    {
        IsAnimating = true;
        StartTime = Time.time;
        OnStart();
    }

    public void Stop()
    {
        OnStop();
        IsAnimating = false;
    }

    public void Update()
    {
        if (IsAnimating)
        {
            if (Time.time > StartTime + Duration)
            {
                Stop();
            }
            else
            {
                OnUpdate();
            }
        }
    }

    protected virtual void OnUpdate() { }
    protected virtual void OnStart() { }
    protected virtual void OnStop() { }
}
