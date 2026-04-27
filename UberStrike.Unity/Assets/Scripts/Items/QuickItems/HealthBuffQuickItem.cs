
using Cmune.Util;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;
using UberStrike.Core.Types;

public class HealthBuffQuickItem : BaseQuickItem
{
    [SerializeField]
    private HealthBuffConfiguration _config;

    private StateMachine machine = new StateMachine();

    public override QuickItemConfiguration Configuration
    {
        get { return _config; }
        set { _config = (HealthBuffConfiguration)value; }
    }

    public override Texture2D GetCustomIcon(QuickItemConfiguration customConfig)
    {
        HealthBuffConfiguration customHealthBuffConfig = customConfig as HealthBuffConfiguration;
        if (customHealthBuffConfig != null)
        {
            if (customHealthBuffConfig.IsHealNeedCharge)
            {
                return ConsumableHudTextures.HealthBuffCharge;
            }
            else if (customHealthBuffConfig.IsHealOverTime)
            {
                return ConsumableHudTextures.HealthBuffOvertime;
            }
            else
            {
                return ConsumableHudTextures.HealthBuffInstant;
            }
        }
        return Icon;
    }

    protected override void OnActivated()
    {
        if (!machine.ContainsState(1))
            machine.RegisterState(1, new ActivatedState(this));

        QuickItemSfxController.Instance.ShowThirdPersonEffect(GameState.LocalPlayer.Character,
            QuickItemLogic.HealthPack, _config.RobotLifeTimeMilliSeconds, _config.ScrapsLifeTimeMilliSeconds, _config.IsHealInstant);
        GameState.CurrentGame.ActivateQuickItem(QuickItemLogic.HealthPack,
            _config.RobotLifeTimeMilliSeconds, _config.ScrapsLifeTimeMilliSeconds, _config.IsHealInstant);

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
                "Charging Health", BlueStonez.label_interparkbold_16pt);
            GUITools.DrawWarmupBar(new Rect((Screen.width - width) * 0.5f, Screen.height / 2 + 50, width, height),
                Behaviour.FocusTimeTotal - Behaviour.FocusTimeRemaining, Behaviour.FocusTimeTotal);
        }
    }

    private class ActivatedState : IState
    {
        private HealthBuffQuickItem _item;

        private float _nextHealthIncrease;
        private float _increaseCounter;

        public ActivatedState(HealthBuffQuickItem configuration)
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
            // Dev server returns PointsGain=1 for the charged HealthBot, so the
            // original config-driven path added a single HP. Description says the
            // bot refills you to 100 if you're below — do NOT overheal past 100
            // (max HP is 200 but that buffer is for armor/pickups, not this bot).
            int currentHp = GameState.LocalCharacter != null ? GameState.LocalCharacter.Health : 100;
            int delta = Mathf.Max(0, 100 - currentHp);
            if (delta > 0)
            {
                CmuneEventHandler.Route(new HealthIncreaseEvent() { Health = delta });
            }
        }
    }
}