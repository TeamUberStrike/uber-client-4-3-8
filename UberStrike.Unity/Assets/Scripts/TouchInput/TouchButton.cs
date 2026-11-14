using System;
using UnityEngine;

public class TouchButton : TouchControl
{
    public GUIContent Content;
    public GUIStyle Style;

    public event Action OnPushed;
    public event Action OnLongPress;

    public const float LongPressTime = 0.4f;
    public float MinGUIAlpha = 0.0f;

    public TouchButton()
        : base()
    {
        OnTouchBegan += OnTouchButtonBegan;
        OnTouchEnded += OnTouchButtonEnded;
    }

    public TouchButton(string title, GUIStyle style)
        : this()
    {
        Content = new GUIContent(title);
        Style = style;
    }

    public TouchButton(Texture texture) : this()
    {
        Content = new GUIContent(texture);
    }

    ~TouchButton()
    {
        OnTouchBegan -= OnTouchButtonBegan;
        OnTouchEnded -= OnTouchButtonEnded;
    }

    public override void UpdateTouches(Touch touch)
    {
        base.UpdateTouches(touch);

        if (_touchStarted && !_touchSent)
        {
            if (finger.StartTouchTime + LongPressTime < Time.time)
            {
                if (OnLongPress != null)
                {
                    OnLongPress();
                }
                else if (OnPushed != null)
                {
                    OnPushed();
                }
                _touchSent = true;
            }
        }
    }

    public override void Draw()
    {
        if (Content != null)
        {
            GUI.color = new Color(1, 1, 1, Mathf.Clamp(TouchController.Instance.GUIAlpha, MinGUIAlpha, 1.0f));

            if (_rotationAngle != 0)
            {
                GUIUtility.RotateAroundPivot(_rotationAngle, _rotationPoint);
            }
        
            if (Style != null)
                GUI.Label(Boundary, Content, Style);
            else
                GUI.Label(Boundary, Content);

            if (_rotationAngle != 0)
            {
                GUI.matrix = Matrix4x4.identity;
            }

            GUI.color = Color.white;
        }
    }

    void OnTouchButtonEnded(Vector2 pos)
    {
        if (!_touchSent)
        {
            if (OnPushed != null)
            {
                OnPushed();
            }
        }
    }

    void OnTouchButtonBegan(Vector2 pos)
    {
        _touchSent = false;
        _touchStarted = true;
    }

    protected override void ResetTouch()
    {
        base.ResetTouch();
        _touchStarted = false;
        _touchSent = false;
    }

    #region Fields

    private bool _touchStarted;
    private bool _touchSent;

    #endregion
}

