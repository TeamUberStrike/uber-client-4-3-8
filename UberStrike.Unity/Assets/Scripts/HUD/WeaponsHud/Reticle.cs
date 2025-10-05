using System;
using UnityEngine;

public class Reticle
{
    public Reticle()
    {
        _texRotate = null;
        _texScale1 = null;
        _texScale2 = null;
        _texTranslate = null;

        //_rotateAngle = 0;
        _innerScaleRatio = 0;
        _outterScaleRatio = 0;
        _translateDistance = 0;
    }

    public void SetRotate(Texture image, float angle)
    {
        _texRotate = image;
        //_rotateAngle = angle;
    }

    public void SetInnerScale(Texture image, float ratio)
    {
        _texScale1 = image;
        _innerScaleRatio = ratio;
    }

    public void SetOutterScale(Texture image, float ratio)
    {
        _texScale2 = image;
        _outterScaleRatio = ratio;
    }

    public void SetTranslate(Texture image, float distance)
    {
        _texTranslate = image;
        _translateDistance = distance;
    }

    public void Update()
    {
        switch (_currentState)
        {
            case STATE_NORMAL:
                {
                    _timer = Mathf.Lerp(_timer, 0, Time.deltaTime);
                    _currentDistance = Mathf.Lerp(_currentDistance, 0, Time.deltaTime);
                    _currentInnerRatio = Mathf.Lerp(_currentInnerRatio, 1, Time.deltaTime);
                    _currentOutterRatio = Mathf.Lerp(_currentOutterRatio, 1, Time.deltaTime);
                }
                break;

            case STATE_BIG:
                if (_timer < DURATION)
                {
                    _timer += Time.deltaTime * 5;
                    _currentAngle = Mathf.Lerp(0, 60, _timer / DURATION);
                    _currentDistance = _translateDistance * _timer / DURATION;
                    _currentInnerRatio = Mathf.Lerp(1, _innerScaleRatio, _timer / DURATION);
                    _currentOutterRatio = Mathf.Lerp(1, _outterScaleRatio, _timer / DURATION);
                }
                else
                {
                    _currentState = STATE_SMALL;
                }
                break;

            case STATE_SMALL:
                if (_timer > 0)
                {
                    _timer -= Time.deltaTime * 5;
                    _currentDistance = _translateDistance * _timer / DURATION;
                    _currentInnerRatio = Mathf.Lerp(1, _innerScaleRatio, _timer / DURATION);
                    _currentOutterRatio = Mathf.Lerp(1, _outterScaleRatio, _timer / DURATION);
                }
                else
                {
                    _currentState = STATE_NORMAL;
                }
                break;
        }
    }

    public void Trigger()
    {
        _currentState = STATE_BIG;
    }

    public void Draw(Rect position)
    {
        Vector2 center = new Vector2(position.x + position.width * 0.5f, position.y + position.height * 0.5f);

        // rotate
        if (_texRotate)
        {
            GUIUtility.RotateAroundPivot(_currentAngle, center);
            GUI.DrawTexture(position, _texRotate);
            //GUIUtility.RotateAroundPivot(-_currentAngle, center);
            GUI.matrix = Matrix4x4.identity;
        }

        // scale
        if (_texScale1)
        {
            GUIUtility.ScaleAroundPivot(new Vector2(_currentInnerRatio, _currentInnerRatio), center);
            GUI.DrawTexture(position, _texScale1);
            //GUIUtility.ScaleAroundPivot(new Vector2(1 / _currentInnerRatio, 1 / _currentInnerRatio), center);
            GUI.matrix = Matrix4x4.identity;
        }

        if (_texScale2)
        {
            GUIUtility.ScaleAroundPivot(new Vector2(_currentOutterRatio, _currentOutterRatio), center);
            GUI.DrawTexture(position, _texScale2);
            //GUIUtility.ScaleAroundPivot(new Vector2(1 / _currentOutterRatio, 1 / _currentOutterRatio), center);
            GUI.matrix = Matrix4x4.identity;
        }

        // translate
        if (_texTranslate)
        {
            position.x += _currentDistance;

            GUI.DrawTexture(position, _texTranslate);
            GUIUtility.RotateAroundPivot(-90, center);

            GUI.DrawTexture(position, _texTranslate);
            GUIUtility.RotateAroundPivot(-90, center);

            GUI.DrawTexture(position, _texTranslate);
            GUIUtility.RotateAroundPivot(-90, center);

            GUI.DrawTexture(position, _texTranslate);
            GUIUtility.RotateAroundPivot(-90, center);

            GUI.matrix = Matrix4x4.identity;
        }
    }

    private Texture _texRotate;
    private Texture _texScale1;
    private Texture _texScale2;
    private Texture _texTranslate;

    private float _innerScaleRatio;
    private float _outterScaleRatio;
    private float _translateDistance;

    private float _currentAngle;
    private float _currentDistance;
    private float _currentInnerRatio;
    private float _currentOutterRatio;

    private const int STATE_NORMAL = 0;
    private const int STATE_BIG = 1;
    private const int STATE_SMALL = 2;

    private int _currentState;
    private float _timer;
    private const float DURATION = 1;
}
