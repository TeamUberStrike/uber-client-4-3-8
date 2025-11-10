using UnityEngine;

public abstract class MeshGUIBase : IAnimatable2D
{
    abstract public void FreeObject();
    abstract public Vector2 GetOriginalBounds();
    abstract protected GameObject AllocObject(GameObject parentObject);
    abstract protected CustomMesh GetCustomMesh();
    abstract protected Vector2 GetAdjustScale();
    abstract protected void UpdateRect();

    public MeshGUIBase(GameObject parentObject)
    {
        if (MeshGUIManager.Exists == false)
        {
            return;
        }
        _meshObject = AllocObject(parentObject);
        _customeMesh = GetCustomMesh();
        _positionAnim = new Vector2Anim(OnPositionChange);
        _scaleAnim = new Vector2Anim(OnScaleChange);
        _scaleAnim.Vec2 = Vector2.one;
        _colorAnim = new ColorAnim(OnColorChange);
        _flickerAnim = new FlickerAnim(UpdateVisible);

        ResetGUI();
    }

    public virtual void Draw(float offsetX = 0.0f, float offsetY = 0.0f)
    {
        if (_parentPosition.x != offsetX || _parentPosition.y != offsetY)
        {
            ParentPosition = new Vector2(offsetX, offsetY);
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
        if (IsEnabled)
        {
            IsVisible = true;

            if (_customeMesh)
            {
                _customeMesh.IsVisible = true;
            }
        }
    }

    public void Hide()
    {
        IsVisible = false;

        if (_customeMesh)
        {
            _customeMesh.IsVisible = false;
        }
    }

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

    public bool IsFlickering()
    {
        return _flickerAnim.IsAnimating;
    }

    protected void ResetGUI()
    {
        _parentPosition = Vector2.zero;
        _depth = 0.0f;
        Color = Color.white;
        Position = Vector2.zero;
        Scale = Vector2.one;
        Show();
        UpdateRect();
    }

    #region Properties
    public string Name
    {
        get { return _customeMesh.name; }
        set { _customeMesh.name = value; }
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

    public Vector2 ParentPosition
    {
        get { return _parentPosition; }
        set
        {
            _parentPosition = value;
            UpdatePosition();
        }
    }

    public Vector2 Position
    {
        get { return _positionAnim.Vec2; }
        set
        {
            _positionAnim.Vec2 = value;
        }
    }

    public float Depth
    {
        get { return _depth; }
        set
        {
            _depth = value;
            UpdatePosition();
        }
    }

    public Vector2 Scale
    {
        get { return _scaleAnim.Vec2; }
        set { _scaleAnim.Vec2 = value; }
    }

    public Vector2 Size
    {
        get
        {
            Vector2 bounds = GetOriginalBounds();
            _size.x = Scale.x * bounds.x;
            _size.y = Scale.y * bounds.y;
            return _size;
        }
    }

    public Vector2 Center
    {
        get
        {
            UpdateRect();
            _center.x = _rect.x + Size.x / 2;
            _center.y = _rect.y + Size.y / 2;
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

    public Vector2 Rotation
    {
        get;
        set;
    }

    //public bool Visible
    //{
    //    get
    //    {
    //        return _isVisible;
    //    }
    //    set
    //    {
    //        _isVisible = value;
    //        if (_customeMesh)
    //        {
    //            _customeMesh.Visible = value;
    //        }
    //    }
    //}


    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            _isEnabled = value;

            if (_isEnabled) Show();
            else Hide();
        }
    }

    public bool IsVisible { get; private set; }

    #endregion

    private void OnColorChange(Color oldColor, Color newColor)
    {
        _customeMesh.Color = _colorAnim.Color;
        _customeMesh.Alpha = _colorAnim.Alpha;
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
        Vector2 adjustScale = GetAdjustScale();
        _meshObject.transform.localScale = new Vector3(newScale.x * adjustScale.x,
            newScale.y * adjustScale.y, 1.0f);
        UpdateRect();
    }

    private void OnPositionChange(Vector2 oldPosition, Vector2 newPosition)
    {
        UpdatePosition();
    }

    private void UpdateVisible(FlickerAnim animation)
    {
        if (_customeMesh == null) return;

        if (animation.IsAnimating)
        {
            _customeMesh.IsVisible = IsVisible && animation.IsFlickerVisible;
        }
        else
        {
            _customeMesh.IsVisible = IsVisible;
        }
    }

    private void UpdatePosition()
    {
        Vector3 position = MeshGUIManager.Instance.
            TransformPosFromScreenToWorld(_parentPosition + _positionAnim.Vec2);
        position.z = _depth;
        _meshObject.transform.position = position;
        UpdateRect();
    }

    protected GameObject _meshObject;
    protected CustomMesh _customeMesh;
    protected ColorAnim _colorAnim;
    protected Vector2Anim _positionAnim;
    protected FlickerAnim _flickerAnim;
    protected Vector2Anim _scaleAnim;
    protected Rect _rect;

    private float _depth;
    private Vector2 _size;
    private Vector2 _center;
    private Vector2 _parentPosition;
    private Vector2 _scaleAnimPivot;

    private bool _isEnabled = true;
    private bool _isScaleAnimAroundPivot;
}