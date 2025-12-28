using Cmune.Util;
using UberStrike.Core.Types;
using UnityEngine;

public class AmmoBuffQuickItem : BaseQuickItem
{
    [SerializeField]
    private AmmoBuffConfiguration _config;

    private StateMachine machine = new StateMachine();

    public override QuickItemConfiguration Configuration
    {
        get { return _config; }
        set { _config = (AmmoBuffConfiguration)value; }
    }

    protected override void OnActivated()
    {
        if (!machine.ContainsState(1))
            machine.RegisterState(1, new ActivatedState(this));

        QuickItemSfxController.Instance.ShowThirdPersonEffect(GameState.LocalPlayer.Character,
            QuickItemLogic.AmmoPack, _config.RobotLifeTimeMilliSeconds, _config.ScrapsLifeTimeMilliSeconds, _config.IsInstant);
        GameState.CurrentGame.ActivateQuickItem(QuickItemLogic.AmmoPack,
            _config.RobotLifeTimeMilliSeconds, _config.ScrapsLifeTimeMilliSeconds, _config.IsInstant);

        machine.SetState(1);
    }

    private void Update()
    {
        machine.Update();
    }

    private void OnGUI()
    {
        if (Behaviour.IsBusy && Behaviour.FocusTimeRemaining > 0)
        {
            float height = Mathf.Clamp(Screen.height * 0.03f, 10, 40);
            float width = height * 10; //keep a certain aspect ratio

            GUI.Label(new Rect((Screen.width - width) * 0.5f, Screen.height / 2 + 20, width, height),
                "Charging Ammo", BlueStonez.label_interparkbold_16pt);
            GUITools.DrawWarmupBar(new Rect((Screen.width - width) * 0.5f, Screen.height / 2 + 50, width, height),
                Behaviour.FocusTimeTotal - Behaviour.FocusTimeRemaining, Behaviour.FocusTimeTotal);
        }
    }

    private class ActivatedState : IState
    {
        private AmmoBuffQuickItem _item;

        private float _nextHealthIncrease;
        private float _increaseCounter;

        public ActivatedState(AmmoBuffQuickItem configuration)
        {
            this._item = configuration;
        }

        public void OnEnter()
        {
            //increase ammo over time
            if (_item._config.IncreaseTimes > 0)
            {
                _increaseCounter = _item._config.IncreaseTimes;
                _nextHealthIncrease = 0;
            }
            else
            {
                SendAmmoIncrease();
                _item.machine.PopState();
            }
        }

        public void OnExit() { }

        public void OnUpdate()
        {
            //increase helath over time
            if (_nextHealthIncrease < Time.time)
            {
                _increaseCounter--;
                _nextHealthIncrease = Time.time + (_item._config.IncreaseFrequency / 1000f);
                SendAmmoIncrease();

                if (_increaseCounter <= 0)
                {
                    _item.machine.PopState();
                }
            }
        }

        public void OnGUI() { }

        private void SendAmmoIncrease()
        {
            switch (_item._config.AmmoIncrease)
            {
                case IncreaseStyle.Absolute:
                    CmuneEventHandler.Route(new AddAmmoIncreaseEvent() { Amount = _item._config.PointsGain });
                    break;
                case IncreaseStyle.PercentFromMax:
                    CmuneEventHandler.Route(new AmmoAddMaxEvent() { Percent = _item._config.PointsGain });
                    break;
                case IncreaseStyle.PercentFromStart:
                    CmuneEventHandler.Route(new AmmoAddStartEvent() { Percent = _item._config.PointsGain });
                    break;
                default:
                    throw new System.NotImplementedException("SendAmmoIncrease for type: " + _item._config.AmmoIncrease);
            }
        }
    }
}