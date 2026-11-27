using System.Collections.Generic;
using System;
using UnityEngine;

public interface IState
{
    void OnEnter();
    void OnExit();
    void OnUpdate();
    void OnGUI();
}

public class StateMachine
{
    public StateMachine()
    {
        _statesGroup = new Dictionary<int, IState>();
        _curStates = new Stack<int>();
    }

    public void SetState(int stateId)
    {
        if (!ContainsState(stateId))
        {
            throw new Exception("ChangeState failed - cannot found state [" + stateId +
                "] from state machine");
        }

        while (_curStates.Count > 0)
        {
            try
            {
                CurrentState.OnExit();
            }
            catch (Exception e)
            {
                LogError("StateMachine::SetState - OnExit of state [" + CurrentStateId +
                    "] threw: " + e);
            }

            _curStates.Pop();
        }

        _curStates.Push(stateId);

        try
        {
            GetState(stateId).OnEnter();
        }
        catch (Exception e)
        {
            LogError("StateMachine::SetState - OnEnter of state [" + stateId +
                "] threw: " + e);
        }
    }

    public void PushState(int stateId)
    {
        if (!ContainsState(stateId))
        {
            throw new Exception("PushState failed - cannot found state[" + stateId +
                "] from state machine");
        }

        _curStates.Push(stateId);

        try
        {
            GetState(stateId).OnEnter();
        }
        catch (Exception e)
        {
            LogError("StateMachine::PushState - OnEnter of state [" + stateId +
                "] threw: " + e);

            if (_curStates.Count > 0)
                _curStates.Pop();
        }
    }

    public void PopState()
    {
        if (_curStates.Count != 0)
        {
            try
            {
                CurrentState.OnExit();
            }
            catch (Exception e)
            {
                LogError("StateMachine::PopState - OnExit of state [" + CurrentStateId +
                    "] threw: " + e);
            }

            _curStates.Pop();
        }
    }

    public void PopAllStates()
    {
        while (_curStates.Count > 0)
        {
            PopState();
        }
    }

    public void RegisterState(int stateId, IState state)
    {
        if (!_statesGroup.ContainsKey(stateId))
        {
            _statesGroup.Add(stateId, state);
        }
        else
        {
            throw new Exception("StateMachine::RegisterState - state [" + stateId +
                "] already exists in the current registry");
        }
    }

    public bool ContainsState(int stateId)
    {
        return _statesGroup.ContainsKey(stateId);
    }

    public void Update()
    {
        if (_curStates.Count > 0)
        {
            try
            {
                CurrentState.OnUpdate();
            }
            catch (Exception e)
            {
                LogError("StateMachine::Update - OnUpdate of state [" + CurrentStateId +
                    "] threw: " + e);
            }
        }
    }

    public void OnGUI()
    {
        if (_curStates.Count > 0)
        {
            try
            {
                CurrentState.OnGUI();
            }
            catch (Exception e)
            {
                LogError("StateMachine::OnGUI - OnGUI of state [" + CurrentStateId +
                    "] threw: " + e);
            }
        }
    }

    public int CurrentStateId
    {
        get { return _curStates.Count > 0 ? _curStates.Peek() : -1; }
    }

    public bool IsRunning { get { return _curStates.Count > 0; } }

    public IState GetState(int stateId)
    {
        IState state = null;
        _statesGroup.TryGetValue(stateId, out state);
        return state;
    }

    private void LogError(string message)
    {
        if (_lastError == message) return;
        _lastError = message;
        Debug.LogError(message);
    }

    private IState CurrentState
    {
        get { return GetState(CurrentStateId); }
    }

    private Dictionary<int, IState> _statesGroup;
    private Stack<int> _curStates;
    private string _lastError;
}
