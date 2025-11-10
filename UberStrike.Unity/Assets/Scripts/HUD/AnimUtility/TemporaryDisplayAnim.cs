using UnityEngine;
using System.Collections;

public class TemporaryDisplayAnim : AbstractAnim
{
    public TemporaryDisplayAnim(IAnimatable2D animatable2D,
        float displayTime, float fadeOutAnimTime)
    {
        _animatable = animatable2D;
        _displayTime = displayTime;
        _fadeOutAnimTime = fadeOutAnimTime;
        Duration = _displayTime + _fadeOutAnimTime;
    }

    protected override void OnStart()
    {
        _animatable.FadeAlphaTo(1.0f, 0.0f, EaseType.None);
    }

    protected override void OnStop()
    {
        _animatable.FadeAlphaTo(0.0f, 0.0f, EaseType.None);
        _animatable.StopFading();
        _isFading = false;
    }

    protected override void OnUpdate()
    {
        if (IsAnimating && Time.time > StartTime + _displayTime && !_isFading)
        {
            DoFadeoutAnim();
        }
    }

    private void DoFadeoutAnim(params object[] args)
    {
        if (IsAnimating)
        {
            _isFading = true;
            _animatable.FadeAlphaTo(0.0f, _fadeOutAnimTime, EaseType.Out);
        }
    }

    private IAnimatable2D _animatable;
    private float _displayTime;
    private float _fadeOutAnimTime;

    private bool _isFading = false;
}
