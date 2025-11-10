using UnityEngine;

public class Vector2Anim
{
    public delegate void OnValueChange(Vector2 oldValue, Vector2 newValue);

    public Vector2Anim(OnValueChange onVec2Change = null)
    {
        _isAnimating = false;
        if (onVec2Change != null)
        {
            _onVec2Change = onVec2Change;
        }
    }

    public Vector2 Vec2
    {
        get { return _vec2; }
        set
        {
            Vector2 oldValue = _vec2;
            _vec2 = value;
            if (_onVec2Change != null)
            {
                _onVec2Change(oldValue, _vec2);
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
                Vec2 = Vector2.Lerp(_animSrc, _animDest, Mathfx.Ease(t, _animEaseType));
            }
            else
            {
                Vec2 = _animDest;
                _isAnimating = false;
            }
        }
    }

    public void AnimTo(Vector2 destPosition, float time = 0.0f, EaseType easeType = 0, float startDelay = 0)
    {
        if (time <= 0.0f)
        {
            Vec2 = destPosition;
            return;
        }
        _isAnimating = true;
        _animSrc = Vec2;
        _animDest = destPosition;
        _animTime = time;
        _animEaseType = easeType;
        _animStartTime = Time.time + startDelay;
    }

    public void AnimBy(Vector2 deltaPosition, float time = 0.0f, EaseType easeType = 0)
    {
        Vector2 destPosition = Vec2 + deltaPosition;
        AnimTo(destPosition, time, easeType);
    }

    public void StopAnim()
    {
        _isAnimating = false;
    }

    private Vector2 _vec2;
    private OnValueChange _onVec2Change;
    private bool _isAnimating;
    private Vector2 _animSrc;
    private Vector2 _animDest;
    private float _animTime;
    private float _animStartTime;
    private EaseType _animEaseType;
}
