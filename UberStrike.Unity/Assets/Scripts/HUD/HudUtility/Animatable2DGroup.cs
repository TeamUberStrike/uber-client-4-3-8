using System.Collections.Generic;
using UnityEngine;

public class Animatable2DGroup : IAnimatable2D
{
    public Animatable2DGroup()
    {
        _group = new List<IAnimatable2D>();
    }

    public void Draw(float offsetX = 0.0f, float offsetY = 0.0f)
    {
        AnimPosition();

        foreach (IAnimatable2D sprite in _group)
        {
            sprite.Draw(_position.x + offsetX, _position.y + offsetY);
        }
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
            foreach (IAnimatable2D animatable in _group)
            {
                animatable.Show();
            }

            IsVisible = true;
        }
    }

    public void Hide()
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.Hide();
        }

        IsVisible = false;
    }

    public void FadeColorTo(Color destColor, float time = 0.0f, EaseType easeType = 0)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.FadeColorTo(destColor, time, easeType);
        }
    }

    public void FadeColor(Color deltaColor, float time = 0.0f, EaseType easeType = 0)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.FadeColorTo(deltaColor, time, easeType);
        }
    }

    public void FadeAlphaTo(float destAlpha, float time = 0.0f, EaseType easeType = 0)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.FadeAlphaTo(destAlpha, time, easeType);
        }
    }

    public void FadeAlpha(float deltaAlpha, float time = 0.0f, EaseType easeType = 0)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.FadeAlpha(deltaAlpha, time, easeType);
        }
    }

    public void MoveTo(Vector2 destPosition, float time = 0, EaseType easeType = 0, float startDelay = 0)
    {
        if (time <= 0.0f)
        {
            _position = destPosition;
            UpdateRect();
            return;
        }
        _isAnimatingPosition = true;
        _positionAnimSrc = _position;
        _positionAnimDest = destPosition;
        _positionAnimTime = time;
        _positionAnimEaseType = easeType;
        _positionAnimStartTime = Time.time + startDelay;
    }

    public void Move(Vector2 deltaPosition, float time = 0.0f, EaseType easeType = 0)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.Move(deltaPosition, time, easeType);
        }
    }

    public void ScaleTo(Vector2 destScale, float time = 0.0f, EaseType easeType = 0)
    {
        // treat current group's scale as 1.0f
        ScaleDelta(destScale, time, easeType);
    }

    public void ScaleDelta(Vector2 scaleFactor, float time = 0.0f, EaseType easeType = 0)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.ScaleDelta(scaleFactor, time, easeType);
        }
    }

    public void ScaleToAroundPivot(Vector2 destScale, Vector2 pivot, float time = 0.0f, EaseType easeType = 0)
    {
        // treat current group's scale as 1.0f
        ScaleAroundPivot(destScale, pivot, time, easeType);
    }

    public void ScaleAroundPivot(Vector2 scaleFactor, Vector2 pivot, float time = 0.0f, EaseType easeType = 0)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            if (animatable is Animatable2DGroup)
            {
                animatable.ScaleAroundPivot(scaleFactor, pivot - animatable.GetPosition(), time, easeType);
            }
            else
            {
                animatable.ScaleAroundPivot(scaleFactor, pivot, time, easeType);
            }
        }
    }

    public void Flicker(float time, float flickerInterval = 0.02f)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.Flicker(time, flickerInterval);
        }
    }

    public void StopFading()
    {
        foreach (IAnimatable2D animatable in Group)
        {
            animatable.StopFading();
        }
    }

    public void StopMoving()
    {
        _isAnimatingPosition = false;
        foreach (IAnimatable2D animatable in Group)
        {
            animatable.StopMoving();
        }
    }

    public void StopScaling()
    {
        foreach (IAnimatable2D animatable in Group)
        {
            animatable.StopScaling();
        }
    }

    public void StopFlickering()
    {
        foreach (IAnimatable2D animatable in Group)
        {
            animatable.StopFlickering();
        }
    }

    public void RemoveAndFree(int index)
    {
        if (index >= 0 && index < _group.Count)
        {
            IAnimatable2D animatable = _group[index];
            animatable.FreeObject();
            _group.RemoveAt(index);
        }
    }

    public void RemoveAndFree(IAnimatable2D animatable)
    {
        if (_group.Contains(animatable))
        {
            animatable.FreeObject();
            _group.Remove(animatable);
        }
    }

    public void ClearAndFree()
    {
        FreeObject();
        _group.Clear();
    }

    // this function is called to immediately update meshGUI's parentPosition, 
    // instead of updating until Draw() function
    public void UpdateMeshGUIPosition(float offsetX = 0.0f, float offsetY = 0.0f)
    {
        foreach (IAnimatable2D animatable in _group)
        {
            if (animatable is MeshGUIBase)
            {
                (animatable as MeshGUIBase).ParentPosition = _position + new Vector2(offsetX, offsetY);
            }
            else if (animatable is Animatable2DGroup)
            {
                (animatable as Animatable2DGroup).UpdateMeshGUIPosition(_position.x + offsetX,
                    _position.y + offsetY);
            }
        }
    }

    public void FreeObject()
    {
        foreach (IAnimatable2D animatable in _group)
        {
            animatable.FreeObject();
        }
    }

    #region Private functions

    private void AnimPosition()
    {
        if (_isAnimatingPosition == true)
        {
            float elapsedTime = Time.time - _positionAnimStartTime;
            if (elapsedTime <= _positionAnimTime)
            {
                float t = Mathf.Clamp01(elapsedTime * (1.0f / _positionAnimTime));
                _position = Vector2.Lerp(_positionAnimSrc, _positionAnimDest,
                        Mathfx.Ease(t, _positionAnimEaseType));
                UpdateRect();
            }
            else
            {
                _isAnimatingPosition = false;
                _position = _positionAnimDest;
            }
        }
    }

    private void UpdateCenter()
    {
        if (_group.Count > 0)
        {
            _center = Vector2.zero;
            foreach (IAnimatable2D animatable in _group)
            {
                _center += animatable.GetCenter();
            }
            _center /= _group.Count;
        }
    }

    private void UpdateRect()
    {
        if (_group.Count > 0)
        {
            Vector2 boundMin = Vector2.zero;
            Vector2 boundMax = Vector2.zero;
            foreach (IAnimatable2D animatable in _group)
            {
                Rect rect = animatable.GetRect();
                Vector2 rightBottomOfRect = new Vector2(rect.x + rect.width, rect.y + rect.height);
                boundMin.x = boundMin.x < rect.x ? boundMin.x : rect.x;
                boundMin.y = boundMin.y < rect.y ? boundMin.y : rect.y;
                boundMax.x = boundMax.x > rightBottomOfRect.x ? boundMax.x : rightBottomOfRect.x;
                boundMax.y = boundMax.y > rightBottomOfRect.y ? boundMax.y : rightBottomOfRect.y;
            }
            _rect.x = boundMin.x;
            _rect.y = boundMin.y;
            _rect.width = boundMax.x - boundMin.x;
            _rect.height = boundMax.y - boundMin.y;
            _rect.x += Position.x;
            _rect.y += Position.y;
        }
        else
        {
            _rect.x = _position.x;
            _rect.y = _position.y;
            _rect.width = _rect.height = 0.0f;
        }
    }

    #endregion

    #region Properties

    public List<IAnimatable2D> Group
    {
        get { return _group; }
        set { _group = value; }
    }

    public Vector2 Center
    {
        get
        {
            UpdateCenter();
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

    public Vector2 Position
    {
        get { return _position; }
        set { _position = value; }
    }

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

    #region Fields

    private List<IAnimatable2D> _group;
    private Vector2 _center;
    private Rect _rect;
    private Vector2 _position;

    private bool _isEnabled = true;
    private bool _isAnimatingPosition;
    private Vector2 _positionAnimSrc;
    private Vector2 _positionAnimDest;
    private float _positionAnimTime;
    private float _positionAnimStartTime;
    private EaseType _positionAnimEaseType;

    #endregion
}
