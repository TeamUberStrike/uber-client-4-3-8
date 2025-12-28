using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public delegate void EventDelegate(params object[] args);

public class EventFunction
{
    public EventFunction(EventDelegate eventDelegate, params object[] args)
    {
        _eventDelegate = eventDelegate;
        _args = args;
    }

    public void Execute()
    {
        _eventDelegate(_args);
    }

    private void DefaultEventDelegate(params object[] args) { }

    private EventDelegate _eventDelegate;
    private object[] _args;
}

public class CallUtil : Singleton<CallUtil>, IUpdatable
{
    public string SetTimeout(float time, EventDelegate func, params object[] args)
    {
        string timeoutId = Guid.NewGuid().ToString();
        ScheduledFunction funcData = new ScheduledFunction(new EventFunction(func, args), time);
        _timeoutGroup.Add(timeoutId, funcData);
        return timeoutId;
    }

    public string SetInterval(EventDelegate func, float time, params object[] args)
    {
        string intervalId = Guid.NewGuid().ToString();
        ScheduledFunction funcData = new ScheduledFunction(new EventFunction(func, args), time);
        _intervalGroup.Add(intervalId, funcData);
        return intervalId;
    }

    public void ClearTimeout(string timeoutId)
    {
        if (_timeoutGroup.ContainsKey(timeoutId))
        {
            _timeoutGroup.Remove(timeoutId);
        }
        else
        {
            throw new Exception("ClearTimeout - timeout id [" + timeoutId + "] doesn't exist.");
        }
    }

    public void ClearInterval(string intervalId)
    {
        if (_intervalGroup.ContainsKey(intervalId))
        {
            _intervalGroup.Remove(intervalId);
        }
        else
        {
            throw new Exception("ClearInterval - interval id [" + intervalId + "] doesn't exist.");
        }
    }

    public void Update()
    {
        UpdateTimeoutGroup();
        UpdateIntervalGroup();
    }

    #region Private
    private void UpdateTimeoutGroup()
    {
        List<string> toBeRemovedTimeout = new List<string>();
        foreach (KeyValuePair<string, ScheduledFunction> timeoutPair in _timeoutGroup)
        {
            ScheduledFunction timeout = timeoutPair.Value;
            if (Time.time > timeout.CallTime)
            {
                timeout.Function.Execute();
                toBeRemovedTimeout.Add(timeoutPair.Key);
            }
        }
        foreach (string timeoutId in toBeRemovedTimeout)
        {
            _timeoutGroup.Remove(timeoutId);
        }
    }

    private void UpdateIntervalGroup()
    {
        foreach (ScheduledFunction interval in _intervalGroup.Values)
        {
            if (Time.time > interval.CallTime)
            {
                interval.Function.Execute();
                interval.CallTime = Time.time + interval.DelayedTime;
            }
        }
    }

    private CallUtil()
    {
        _timeoutGroup = new Dictionary<string, ScheduledFunction>();
        _intervalGroup = new Dictionary<string, ScheduledFunction>();
    }

    private class ScheduledFunction
    {
        public EventFunction Function { get; private set; }
        public float DelayedTime { get; private set; }
        public float CallTime { get; set; }

        public ScheduledFunction(EventFunction function, float time)
        {
            Function = function;
            DelayedTime = time;
            CallTime = Time.time + time;
        }
    }

    private Dictionary<string, ScheduledFunction> _timeoutGroup;
    private Dictionary<string, ScheduledFunction> _intervalGroup;
    #endregion
}
