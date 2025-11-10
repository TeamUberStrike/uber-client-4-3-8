using System.Collections.Generic;
using System;

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
        if (ContainsState(stateId))
        {
            PopAllStates();
            _curStates.Push(stateId);
            GetState(stateId).OnEnter();
        }
        else
        {
            throw new Exception("ChangeState failed - cannot found state [" + stateId +
                "] from state machine");
        }
    }

    public void PushState(int stateId)
    {
        if (ContainsState(stateId))
        {
            _curStates.Push(stateId);
            GetState(stateId).OnEnter();
        }
        else
        {
            throw new Exception("PushState failed - cannot found state[" + stateId +
                "] from state machine");
        }
    }

    public void PopState()
    {
        if (_curStates.Count != 0)
        {
            CurrentState.OnExit();
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
            CurrentState.OnUpdate();
        }
    }

    public void OnGUI()
    {
        if (_curStates.Count > 0)
        {
            CurrentState.OnGUI();
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

    private IState CurrentState
    {
        get { return GetState(CurrentStateId); }
    }

    private Dictionary<int, IState> _statesGroup;
    private Stack<int> _curStates;
}