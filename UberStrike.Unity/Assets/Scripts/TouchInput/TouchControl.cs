using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchControl : TouchBaseControl
{
    public TouchFinger finger;

    private bool enabled = false;
    public override bool Enabled
    {
        get { return enabled; }
        set
        {
            if (value != enabled)
            {
                enabled = value;
                if (!enabled)
                {
                    finger.Reset();
                    _inside = false;
                }
            }
        }
    }

    public event Action<Vector2> OnTouchBegan;
    public event Action<Vector2, Vector2> OnTouchLeftBoundary;
    public event Action<Vector2, Vector2> OnTouchMoved;
    public event Action<Vector2, Vector2> OnTouchEnteredBoundary;
    public event Action<Vector2> OnTouchEnded;

    public TouchControl() : base()
    {
        finger = new TouchFinger();
    }

    public bool IsActive
    {
        get
        {
            return finger.FingerId != -1;
        }
    }

    protected float _rotationAngle = 0;
    protected Vector2 _rotationPoint = Vector2.zero;

    public void SetRotation(float angle, Vector2 point)
    {
        _rotationAngle = angle;
        _rotationPoint = point;
    }

    public override void UpdateTouches(Touch touch)
    {
        if (finger.FingerId != -1 && touch.fingerId != finger.FingerId) return; // bound to another item
        if (finger.FingerId == -1 && touch.phase != TouchPhase.Began) return; // touch didn't start here

        Vector2 pos = touch.position;
        if (_rotationAngle != 0)
        {
            pos = Mathfx.RotateVector2AboutPoint(touch.position, new Vector2(_rotationPoint.x, Screen.height - _rotationPoint.y), _rotationAngle);
        }

        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (!TouchInside(pos)) // touch didn't start inside this item
                {
                    break;
                }

                finger.StartPos = pos;
                finger.LastPos = pos;
                finger.StartTouchTime = Time.time;
                finger.FingerId = touch.fingerId;
                _inside = true;
                if (OnTouchBegan != null)
                {
                    OnTouchBegan(pos);
                }
                break;
            case TouchPhase.Stationary:
            case TouchPhase.Moved:
                bool touchWithin = TouchInside(pos);

                if (_inside && !touchWithin) // moved outside our boundary
                {
                    _inside = false;
                    if (OnTouchLeftBoundary != null)
                    {
                        OnTouchLeftBoundary(pos, touch.deltaPosition);
                    }
                }
                else if (!_inside && touchWithin) // moved back inside our boundary
                {
                    _inside = true;
                    if (OnTouchEnteredBoundary != null)
                    {
                        OnTouchEnteredBoundary(pos, touch.deltaPosition);
                    }
                }

                if (OnTouchMoved != null)
                {
                    OnTouchMoved(pos, touch.deltaPosition);
                }

                finger.LastPos = pos;

                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (OnTouchEnded != null)
                {
                    OnTouchEnded(pos);
                }
                ResetTouch();
                break;
        }
    }

    protected virtual void ResetTouch()
    {
        finger.Reset();
        _inside = false;
    }

    protected virtual bool TouchInside(Vector2 position)
    {
        return Boundary.ContainsTouch(position);
    }

    private bool _inside = false;
}

