using UnityEngine;
using Cmune.Util;

class EndOfMatchCountdownEvent
{
    public int Countdown { get; set; }
}

class InGameEndOfMatchState : IState
{
    public void OnEnter()
    {
        QuickItemController.Instance.Restriction.RenewGameUses();
        QuickItemController.Instance.IsEnabled = false;

        GamePageManager.Instance.LoadPage(PageType.EndOfMatch);
        SfxManager.Play2dAudioClip(SoundEffectType.UIEndOfRound);
        PopupHud.Instance.PopupMatchOver();
        PlayerStateMsgHud.Instance.ButtonEnabled = false;
        Screen.lockCursor = false;
        HudDrawFlagGroup.Instance.BaseDrawFlag = HudDrawFlags.InGameChat | HudDrawFlags.XpPoints;

        XpPtsHud.Instance.DisplayPermanently();

        CmuneEventHandler.AddListener<OnSetEndOfMatchCountdownEvent>(OnStartCountdown);
    }

    public void OnExit()
    {
        _endOfMatchCountdown = 0;
        SfxManager.Play2dAudioClip(SoundEffectType.GameCountdownTonal2);
        SfxManager.Play2dAudioClip(SoundEffectType.GameFight, 0.5f);

        GamePageManager.Instance.UnloadCurrentPage();
        CmuneEventHandler.RemoveListener<OnSetEndOfMatchCountdownEvent>(OnStartCountdown);
    }

    public void OnUpdate()
    {
        if (_nextRoundStartTime >= Time.time)
        {
            int fullSecond = Mathf.CeilToInt(Mathf.Max(_nextRoundStartTime - Time.time, 0));

            if (_endOfMatchCountdown != fullSecond)
            {
                if (fullSecond <= 3 && fullSecond >= 1)
                {
                    SfxManager.Play2dAudioClip(SoundEffectType.GameCountdownTonal1);
                }
                GameState.CurrentGame.UpdatePlayerReadyForNextRound();
                _endOfMatchCountdown = fullSecond;
                CmuneEventHandler.Route(new EndOfMatchCountdownEvent()
                {
                    Countdown = _endOfMatchCountdown
                });
            }
        }
    }

    public void OnGUI() { }

    private void OnStartCountdown(OnSetEndOfMatchCountdownEvent ev)
    {
        _nextRoundStartTime = Time.time + ev.SecondsUntilNextMatch;
        _endOfMatchCountdown = 0;
    }

    private float _nextRoundStartTime = 0;
    private int _endOfMatchCountdown = 0;
}

