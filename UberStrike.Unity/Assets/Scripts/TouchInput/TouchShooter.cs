using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchShooter : TouchBaseControl
{
    public Vector2 Aim { get; private set; }

    public float SecondaryFireTapDelay = 0.4f;
    public float SecondaryFireTapMaxDistanceSqr = 10000.0f;

    public event Action<Vector2> OnDoubleTap;
    public event Action OnFireStart;
    public event Action OnFireEnd;

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
                    _primaryFinger = new TouchFinger();
                    _secondaryFinger = new TouchFinger();
                }
            }
        }
    }

    public TouchShooter()
        : base()
    {
        _primaryFinger = new TouchFinger();
        _secondaryFinger = new TouchFinger();

        _ignoreTouches = new ArrayList();
    }


    public override void UpdateTouches(Touch touch)
    {
        if (touch.phase == TouchPhase.Began && Boundary.ContainsTouch(touch.position) && ValidArea(touch.position))
        {
            if (_primaryFinger.FingerId == -1)
            {
                _primaryFinger = new TouchFinger()
                {
                    StartPos = touch.position,
                    StartTouchTime = Time.time,
                    LastPos = touch.position,
                    FingerId = touch.fingerId,
                };
                // if finger was tapped within time and close enough
                if (_lastFireTouch + SecondaryFireTapDelay > Time.time && (_lastFirePosition - touch.position).sqrMagnitude < SecondaryFireTapMaxDistanceSqr)
                {
                    if (OnDoubleTap != null) OnDoubleTap(touch.position);
                }
                else
                {
                    _lastFireTouch = Time.time;
                    _lastFirePosition = touch.position;
                }
            }
            else if (_primaryFinger.FingerId != touch.fingerId && _secondaryFinger.FingerId == -1)
            {
                _secondaryFinger = new TouchFinger()
                {
                    StartPos = touch.position,
                    StartTouchTime = Time.time,
                    LastPos = touch.position,
                    FingerId = touch.fingerId,
                };
                if (OnFireStart != null) OnFireStart();
            }
        }
        else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            if (_primaryFinger.FingerId == touch.fingerId)
            {
                // record finger movement scaling for screen size
                Aim = touch.deltaPosition * 500 / Screen.width;
            }
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            if (_primaryFinger.FingerId == touch.fingerId)
            {
                _primaryFinger.Reset();
                Aim = Vector2.zero;
            }
            else if (_secondaryFinger.FingerId == touch.fingerId)
            {
                if (OnFireEnd != null) OnFireEnd();
                _secondaryFinger.Reset();
            }
        }
    }

    public void IgnoreRect(Rect r)
    {
        if (!_ignoreTouches.Contains(r))
            _ignoreTouches.Add(r);
    }

    private bool ValidArea(Vector2 pos)
    {
        if (_ignoreTouches.Count == 0) return true;

        foreach (Rect r in _ignoreTouches)
        {
            if (r.ContainsTouch(pos)) return false;
        }
        return true;
    }

    #region Fields

    private TouchFinger _primaryFinger;
    private TouchFinger _secondaryFinger;

    private float _lastFireTouch = 0;
    private Vector2 _lastFirePosition = Vector2.zero;

    private ArrayList _ignoreTouches;

    #endregion
}

