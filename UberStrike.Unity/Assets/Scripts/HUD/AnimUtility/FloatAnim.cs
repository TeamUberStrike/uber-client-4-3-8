using UnityEngine;

public class FloatAnim
{
    public delegate void OnValueChange(float oldValue, float newValue);

    public FloatAnim(OnValueChange onValueChange = null, float value = 0.0f)
    {
        _isAnimating = false;
        _value = value;
        if (onValueChange != null)
        {
            _onValueChange = onValueChange;
        }
    }

    public float Value
    {
        get { return _value; }
        set
        {
            float oldValue = _value;
            _value = value;
            if (_onValueChange != null)
            {
                _onValueChange(oldValue, _value);
            }
        }
    }

    public bool IsAnimating
    {
        get { return _isAnimating; }
    }

    public void Update()
    {
        if (_isAnimating == true)
        {
            float elapsedTime = Time.time - _animStartTime;
            if (elapsedTime <= _animTime)
            {
                float t = Mathf.Clamp01(elapsedTime * (1.0f / _animTime));
                Value = Mathf.Lerp(_animSrc, _animDest,
                        Mathfx.Ease(t, _animEaseType));
            }
            else
            {
                Value = _animDest;
                _isAnimating = false;
            }
        }
    }

    public void AnimTo(float destValue, float time = 0.0f, EaseType easeType = 0)
    {
        if (time <= 0.0f)
        {
            Value = destValue;
            return;
        }
        _isAnimating = true;
        _animSrc = Value;
        _animDest = destValue;
        _animTime = time;
        _animEaseType = easeType;
        _animStartTime = Time.time;
    }

    public void AnimBy(float deltaValue, float time = 0.0f, EaseType easeType = 0)
    {
        float destValue = Value + deltaValue;
        AnimTo(destValue, time, easeType);
    }

    public void StopAnim()
    {
        _isAnimating = false;
    }

    private float _value;
    private OnValueChange _onValueChange;
    private bool _isAnimating;
    private float _animSrc;
    private float _animDest;
    private float _animTime;
    private float _animStartTime;
    private EaseType _animEaseType;
}
