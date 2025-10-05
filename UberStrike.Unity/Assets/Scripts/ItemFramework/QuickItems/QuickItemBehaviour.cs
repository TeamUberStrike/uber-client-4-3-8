using System;
using Cmune.Util;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class QuickItemBehaviour
{
    private StateMachine _machine;
    private CoolingDownState _coolDownState;
    private FocusedState _focusedState;

    private BaseQuickItem _item;
    private float _chargeTimeOut;

    public Action OnActivated;

    public float CoolDownTimeRemaining
    {
        get { return Mathf.Max(_coolDownState.TimeOut - Time.time, 0); }
    }

    public float CoolDownTimeTotal
    {
        get { return _item.Configuration.CoolDownTime / 1000f; }
    }

    public float FocusTimeRemaining
    {
        get { return Mathf.Max(_focusedState.TimeOut - Time.time, 0); }
    }

    public float FocusTimeTotal
    {
        get { return _item.Configuration.WarmUpTime / 1000f; }
    }

    public float ChargingTimeRemaining
    {
        get { return Mathf.Max(_chargeTimeOut - Time.time, 0); }
    }

    public float ChargingTimeTotal
    {
        get { return _item.Configuration.RechargeTime / 1000f; }
    }

    public int CurrentAmount { get; set; }

    public GameInputKey FocusKey
    {
        get;
        set;
    }

    public bool IsBusy
    {
        get
        {
            return _machine.IsRunning;
        }
    }

    public QuickItemBehaviour(BaseQuickItem item, System.Action onActivated)
    {
        _item = item;
        OnActivated = onActivated;

        _machine = new StateMachine();
        _coolDownState = new CoolingDownState(this);
        _focusedState = new FocusedState(this);

        _machine.RegisterState((int)States.CoolingDown, _coolDownState);
        _machine.RegisterState((int)States.Focused, _focusedState);
    }

    private void Activate()
    {
        //set the inital recharge timeout on activation
        if (CurrentAmount == _item.Configuration.AmountRemaining)
        {
            _chargeTimeOut = Time.time + (_item.Configuration.RechargeTime / 1000f);
        }

        //start cooling down
        if (_item.Configuration.CoolDownTime > 0)
            _machine.PushState((int)States.CoolingDown);

        CurrentAmount--;
        CmuneEventHandler.Route(new QuickItemAmountChangedEvent());

        if (OnActivated != null)
            OnActivated();
    }

    public bool Run()
    {
        if (CurrentAmount > 0 && !_machine.IsRunning)
        {
            MonoRoutine.Instance.OnUpdateEvent += Update;

            //apply effect over time
            if (_item.Configuration.WarmUpTime > 0)
            {
                _machine.PushState((int)States.Focused);
            }
            //apply effect instantly
            else
            {
                Activate();
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private void Update()
    {
        _machine.Update();

        //recharge
        if (_item.Configuration.RechargeTime > 0)
        {
            if (_chargeTimeOut < Time.time && CurrentAmount < _item.Configuration.AmountRemaining)
            {
                CurrentAmount = Mathf.Min(CurrentAmount + 1, _item.Configuration.AmountRemaining);
                CmuneEventHandler.Route(new QuickItemAmountChangedEvent());

                if (CurrentAmount < _item.Configuration.AmountRemaining)
                    _chargeTimeOut = Time.time + (_item.Configuration.RechargeTime / 1000f);
            }
        }

        if (!_machine.IsRunning && CurrentAmount == _item.Configuration.AmountRemaining)
        {
            MonoRoutine.Instance.OnUpdateEvent -= Update;
        }
    }

    public override string ToString()
    {
        var debug = new System.Text.StringBuilder();

        debug.AppendLine("Name: " + _item.Configuration.Name);
        debug.AppendLine("IsBusy: " + IsBusy);
        debug.AppendLine("State: " + _machine.CurrentStateId);
        debug.AppendLine("Amount Current: " + CurrentAmount);
        debug.AppendLine("Amount Total: " + _item.Configuration.AmountRemaining);
        debug.AppendLine("Time: " + CoolDownTimeRemaining.ToString("F2") + " || " + ChargingTimeRemaining.ToString("F2"));

        return debug.ToString();
    }

    public enum States
    {
        CoolingDown = 1,
        Focused = 2,
    }

    private class CoolingDownState : IState
    {
        QuickItemBehaviour behaviour;

        public float TimeOut { get; private set; }

        public CoolingDownState(QuickItemBehaviour behaviour)
        {
            this.behaviour = behaviour;
        }

        public void OnEnter()
        {
            TimeOut = Time.time + (behaviour._item.Configuration.CoolDownTime / 1000f);
        }

        public void OnExit() { }

        public void OnGUI() { }

        public void OnUpdate()
        {
            if (TimeOut < Time.time)
            {
                behaviour._machine.PopState();
            }
        }
    }

    private class FocusedState : IState
    {
        public float TimeOut { get; private set; }

        public FocusedState(QuickItemBehaviour behaviour)
        {
            this.behaviour = behaviour;
        }

        public void OnEnter()
        {
            TimeOut = Time.time + (behaviour._item.Configuration.WarmUpTime / 1000f);

            WeaponController.Instance.IsEnabled = false;
            QuickItemController.Instance.IsCharging = true;
            WeaponController.Instance.PutdownCurrentWeapon();
            HudDrawFlagGroup.Instance.BaseDrawFlag &= ~(HudDrawFlags.Reticle);
            GameState.LocalPlayer.MoveController.IsJumpDisabled = true;
            _originalStopSpeed = LevelEnviroment.Instance.Settings.StopSpeed;
            LevelEnviroment.Instance.Settings.StopSpeed = _originalStopSpeed * behaviour._item.Configuration.SlowdownOnCharge;
        }

        public void OnExit()
        {
            TimeOut = 0;
            WeaponController.Instance.IsEnabled = true;
            QuickItemController.Instance.IsCharging = false;
            WeaponController.Instance.PickupCurrentWeapon();
            HudDrawFlagGroup.Instance.BaseDrawFlag |= HudDrawFlags.Reticle;
            GameState.LocalPlayer.MoveController.IsJumpDisabled = false;
            LevelEnviroment.Instance.Settings.StopSpeed = _originalStopSpeed;
        }

        public void OnUpdate()
        {
            if (TimeOut < Time.time)
            {
                behaviour._machine.PopState();
                behaviour.Activate();
            }
            else if (!(InputManager.Instance.IsDown(behaviour.FocusKey) || InputManager.Instance.IsDown(GameInputKey.UseQuickItem)))
            {
                behaviour._machine.PopState();
            }
        }

        public void OnGUI() { }

        private QuickItemBehaviour behaviour;
        private float _originalStopSpeed;
    }
}