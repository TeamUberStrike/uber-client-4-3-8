using UnityEngine;
using System;

public class Sprite2DButton : IAnimatable2D
{
    public GUIStyle Style
    {
        get
        {
            return _style;
        }
        set
        {
            _style = value;
        }
    }

    public GUIContent Content
    {
        get
        {
            return _content;
        }
        set
        {
            _content = value;
        }
    }

    public bool IsUsingGuiContentBounds { get; set; }

    public Action OnClick { get; set; }

    public Sprite2DButton(GUIContent content, GUIStyle style)
    {
        IsUsingGuiContentBounds = true;
        _content = content;
        _style = style;
        _positionAnim = new Vector2Anim();
        _scaleAnim = new Vector2Anim(OnScaleChange);
        _colorAnim = new ColorAnim();
        _colorAnim.Color = Color.white;
        _flickerAnim = new FlickerAnim();
        _size = GUIBounds;
        _rect = new Rect();
        _visible = true;
    }

    public void Draw(float offsetX = 0.0f, float offsetY = 0.0f)
    {
        bool isDrawn = _flickerAnim.IsAnimating ? _visible && _flickerAnim.IsFlickerVisible : _visible;
        if (isDrawn)
        {
            GUITools.BeginGUIColor(_colorAnim.Color);
            Rect spriteRect = Rect;
            spriteRect.x += offsetX;
            spriteRect.y += offsetY;
            if (GUI.Button(spriteRect, _content, _style) && OnClick != null)
            {
                OnClick();
            }
            GUITools.EndGUIColor();
        }

        _flickerAnim.Update();
        _colorAnim.Update();
        _positionAnim.Update();
        _scaleAnim.Update();
    }

    public Vector2 GetPosition()
    {
        return Position;
    }

    public Vector2 GetCenter()
    {
        return Center;
    }

    public Rect GetRect()
    {
        return Rect;
    }

    public void Show()
    {
        Visible = true;
    }

    public void Hide()
    {
        Visible = false;
    }

    public void FreeObject() { }

    public void StopFading()
    {
        _colorAnim.StopFading();
    }

    public void StopMoving()
    {
        _positionAnim.StopAnim();
    }

    public void StopScaling()
    {
        _scaleAnim.StopAnim();
    }

    public void StopFlickering()
    {
        _flickerAnim.StopAnim();
    }

    public void FadeColorTo(Color destColor, float time = 0.0f, EaseType easeType = 0)
    {
        _colorAnim.FadeColorTo(destColor, time, easeType);
    }

    public void FadeColor(Color deltaColor, float time = 0.0f, EaseType easeType = 0)
    {
        _colorAnim.FadeColor(deltaColor, time, easeType);
    }

    public void FadeAlphaTo(float destAlpha, float time = 0.0f, EaseType easeType = 0)
    {
        _colorAnim.FadeAlphaTo(destAlpha, time, easeType);
    }

    public void FadeAlpha(float deltaAlpha, float time = 0.0f, EaseType easeType = 0)
    {
        _colorAnim.FadeAlpha(deltaAlpha, time, easeType);
    }

    public void MoveTo(Vector2 destPosition, float time = 0.0f, EaseType easeType = 0, float startDelay = 0)
    {
        _positionAnim.AnimTo(destPosition, time, easeType, startDelay);
    }

    public void Move(Vector2 deltaPosition, float time = 0.0f, EaseType easeType = 0)
    {
        _positionAnim.AnimBy(deltaPosition, time, easeType);
    }

    public void ScaleTo(Vector2 destScale, float time = 0.0f, EaseType easeType = 0)
    {
        _scaleAnim.AnimTo(destScale, time, easeType);
    }

    public void ScaleDelta(Vector2 scaleFactor, float time = 0.0f, EaseType easeType = 0)
    {
        _scaleAnim.AnimBy(scaleFactor, time, easeType);
    }

    public void ScaleToAroundPivot(Vector2 destScale, Vector2 pivot, float time = 0.0f, EaseType easeType = 0)
    {
        ScaleTo(destScale, time, easeType);
        _isScaleAnimAroundPivot = true;
        _scaleAnimPivot = pivot;
    }

    public void ScaleAroundPivot(Vector2 scaleFactor, Vector2 pivot, float time = 0.0f, EaseType easeType = 0)
    {
        Vector2 destScale;
        destScale.x = Scale.x * scaleFactor.x;
        destScale.y = Scale.y * scaleFactor.y;
        ScaleToAroundPivot(destScale, pivot, time, easeType);
    }

    public void Flicker(float time, float flickerInterval = 0.02f)
    {
        _flickerAnim.Flicker(time, flickerInterval);
    }

    public Color Color
    {
        get { return _colorAnim.Color; }
        set { _colorAnim.Color = value; }
    }

    public float Alpha
    {
        get { return _colorAnim.Alpha; }
        set { _colorAnim.Alpha = value; }
    }

    public Vector2 Position
    {
        get { return _positionAnim.Vec2; }
        set
        {
            _positionAnim.Vec2 = value;
        }
    }

    public Vector2 Scale
    {
        get { return _scaleAnim.Vec2; }
        set { _scaleAnim.Vec2 = value; }
    }

    public Vector2 GUIBounds
    {
        get
        {
            if (IsUsingGuiContentBounds)
            {
                return _style.CalcSize(_content);
            }
            else
            {
                return _guiBounds;
            }
        }
        set { _guiBounds = value; }
    }

    public Vector2 Size
    {
        get
        {
            _size.x = Scale.x * GUIBounds.x;
            _size.y = Scale.y * GUIBounds.y;
            return _size;
        }
    }

    public Vector2 Center
    {
        get
        {
            _center = Position + Size / 2;
            return _center;
        }
    }

    public Rect Rect
    {
        get
        {
            UpdateRect();
            return _rect;
        }
    }

    public bool Visible
    {
        get
        {
            return _visible;
        }
        set
        {
            _visible = value;
        }
    }

    #region Private
    private void UpdateRect()
    {
        _rect.x = Position.x - Screen.width * (1 - CameraRectController.Instance.Width) / 2;
        _rect.y = Position.y;
        _rect.width = Size.x;
        _rect.height = Size.y;
    }

    private void OnScaleChange(Vector2 oldScale, Vector2 newScale)
    {
        if (_scaleAnim.IsAnimating && _isScaleAnimAroundPivot)
        {
            Vector2 newPos;
            if (oldScale.x > 0.0f)
            {
                newPos.x = (Position.x - _scaleAnimPivot.x) * newScale.x / oldScale.x + _scaleAnimPivot.x;
            }
            else
            {
                newPos.x = Position.x - Size.x / 2;
            }
            if (oldScale.y > 0.0f)
            {
                newPos.y = (Position.y - _scaleAnimPivot.y) * newScale.y / oldScale.y + _scaleAnimPivot.y;
            }
            else
            {
                newPos.y = Position.y - Size.y / 2;
            }
            Position = newPos;
        }
    }

    private ColorAnim _colorAnim;
    private Vector2Anim _positionAnim;
    private FlickerAnim _flickerAnim;
    private Vector2Anim _scaleAnim;

    private GUIContent _content;
    private GUIStyle _style;
    private Vector2 _size;
    private Vector2 _center;
    private Vector2 _guiBounds;
    private Rect _rect;
    private bool _visible;

    private bool _isScaleAnimAroundPivot;
    private Vector2 _scaleAnimPivot;
    #endregion
}
