using System;
using UnityEngine;

public class FlickerAnim
{
    public FlickerAnim(Action<FlickerAnim> onFlickerVisibleChange = null)
    {
        _isAnimating = false;
        _onFlickerVisibleChange = onFlickerVisibleChange;
    }

    public bool IsAnimating
    {
        get { return _isAnimating; }
    }

    public bool IsFlickerVisible
    {
        get { return _isFlickerVisible; }
        set
        {
            _isFlickerVisible = value;

            if (_onFlickerVisibleChange != null)
                _onFlickerVisibleChange(this);
        }
    }

    public void Update()
    {
        if (_isAnimating == true)
        {
            float curTime = Time.time;
            if (curTime > _flickerEndTime)
            {
                _isAnimating = false;
                IsFlickerVisible = true;
            }
            else
            {
                if (curTime > _lastFlickerTime + _flickerInterval)
                {
                    IsFlickerVisible = !_isFlickerVisible;
                    _lastFlickerTime = curTime;
                }
            }
        }
    }

    public void Flicker(float time, float flickerInterval = 0.02f)
    {
        if (time <= 0.0f || flickerInterval >= time)
        {
            return;
        }
        _isAnimating = true;
        _flickerInterval = 0.02f;
        _flickerStartTime = Time.time;
        _flickerEndTime = _flickerStartTime + time;
        _lastFlickerTime = _flickerStartTime;
    }

    public void StopAnim()
    {
        _isAnimating = false;
    }

    private bool _isAnimating;
    private Action<FlickerAnim> _onFlickerVisibleChange;
    private float _flickerInterval;
    private float _flickerStartTime;
    private float _flickerEndTime;
    private float _lastFlickerTime;
    private bool _isFlickerVisible;
}
