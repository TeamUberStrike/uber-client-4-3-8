using Cmune.Util;
using UberStrike.Core.Types;
using UnityEngine;

public class ArmorBuffQuickItem : BaseQuickItem
{
    [SerializeField]
    private ArmorBuffConfiguration _config;

    private StateMachine machine = new StateMachine();

    public override QuickItemConfiguration Configuration
    {
        get { return _config; }
        set { _config = (ArmorBuffConfiguration)value; }
    }

    protected override void OnActivated()
    {
        if (!machine.ContainsState(1))
            machine.RegisterState(1, new ActivatedState(this));

        QuickItemSfxController.Instance.ShowThirdPersonEffect(GameState.LocalPlayer.Character,
            QuickItemLogic.ArmorPack, _config.RobotLifeTimeMilliSeconds, _config.ScrapsLifeTimeMilliSeconds, _config.IsInstant);
        GameState.CurrentGame.ActivateQuickItem(QuickItemLogic.ArmorPack, 
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
                "Charging Armor", BlueStonez.label_interparkbold_16pt);
            GUITools.DrawWarmupBar(new Rect((Screen.width - width) * 0.5f, Screen.height / 2 + 50, width, height),
                Behaviour.FocusTimeTotal - Behaviour.FocusTimeRemaining, Behaviour.FocusTimeTotal);
        }
    }

    private class ActivatedState : IState
    {
        private ArmorBuffQuickItem _item;

        private float _nextHealthIncrease;
        private float _increaseCounter;

        public ActivatedState(ArmorBuffQuickItem configuration)
        {
            this._item = configuration;
        }

        public void OnEnter()
        {
            //increase health over time
            if (_item._config.IncreaseTimes > 0)
            {
                _increaseCounter = _item._config.IncreaseTimes;
                _nextHealthIncrease = 0;
            }
            else
            {
                SendHealthIncrease();
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
                SendHealthIncrease();

                if (_increaseCounter <= 0)
                {
                    _item.machine.PopState();
                }
            }
        }

        public void OnGUI() { }

        private void SendHealthIncrease()
        {
            int points = 0;
            switch (_item._config.ArmorIncrease)
            {
                case IncreaseStyle.Absolute:
                    points = _item._config.PointsGain;
                    break;
                case IncreaseStyle.PercentFromMax:
                    points = Mathf.RoundToInt(200 * Mathf.Clamp01(_item._config.PointsGain / 100f));
                    break;
                case IncreaseStyle.PercentFromStart:
                    points = Mathf.RoundToInt(100 * Mathf.Clamp01(_item._config.PointsGain / 100f));
                    break;
                default:
                    throw new System.NotImplementedException("SendArmorIncrease for type: " + _item._config.ArmorIncrease);
            }

            CmuneEventHandler.Route(new ArmorIncreaseEvent() { Armor = points });
        }
    }
}