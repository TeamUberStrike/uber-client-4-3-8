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
                // Clear fingers AND the stale Aim on every enable/disable transition. A finger held through
                // death -> respawn never re-binds as the primary (its Began already passed while the Shooter
                // was disabled), so a leftover non-zero Aim would keep feeding WishLook and rotate the POV
                // with no input — the "POV rotates by itself after respawn" regression. Zeroing Aim stops it.
                _primaryFinger = new TouchFinger();
                _secondaryFinger = new TouchFinger();
                Aim = Vector2.zero;
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
                if (TouchInput.LogFireEvents)
                    Debug.Log("[TouchFireLog] Shooter 2nd-finger FIRE at " + touch.position + " (multiTouch=" + TouchInput.UseMultiTouch + ")");
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

    public override void FinalUpdate()
    {
        // iOS does not always deliver TouchPhase.Ended (the same dropped-Ended behaviour the D-pad hit on
        // device in v29). If it's dropped for the AIM finger, _primaryFinger stays bound to a finger that is
        // gone — and the guard in UpdateTouches then makes the NEXT finger bind as the SECONDARY (fire)
        // finger instead of the primary. Aim is driven only by the primary, so the POV stops moving while
        // you drag ("two fingers on the right side, POV sometimes won't move"). Prune any finger that has
        // left the live touch set so a fresh finger can re-bind as primary.
        if (_primaryFinger.FingerId != -1 && !IsFingerLive(_primaryFinger.FingerId))
        {
            _primaryFinger.Reset();
            Aim = Vector2.zero;
        }
        if (_secondaryFinger.FingerId != -1 && !IsFingerLive(_secondaryFinger.FingerId))
        {
            _secondaryFinger.Reset();
            if (OnFireEnd != null) OnFireEnd();   // also clears a stuck-fire if the fire finger's Ended was lost
        }
    }

    private static bool IsFingerLive(int fingerId)
    {
        foreach (Touch t in Input.touches)
        {
            if (t.fingerId == fingerId) return true;
        }
        return false;
    }

    public void IgnoreRect(Rect r)
    {
        if (!_ignoreTouches.Contains(r))
            _ignoreTouches.Add(r);
    }

    // The movement joystick zone is customizable (it can be moved/scaled in the layout editor), so
    // its ignore rect must be replaceable rather than added once — otherwise look-drag would keep
    // firing over the old zone. Removes the previously registered joystick rect and adds the new one.
    public void SetJoystickIgnore(Rect r)
    {
        _ignoreTouches.Remove(_joystickIgnore);
        _joystickIgnore = r;
        if (!_ignoreTouches.Contains(r))
            _ignoreTouches.Add(r);
    }

    private Rect _joystickIgnore = new Rect(-1, -1, 0, 0);

    // Replaceable set of action-button zones (fire/jump/crouch/secondary). A touch that starts on a
    // button must NOT also bind as the look/aim or 2nd-finger-fire finger, so the look-drag and the
    // buttons don't fight. Refreshed from TouchInput.ApplyLayout so it tracks the customizable layout.
    private readonly ArrayList _buttonIgnores = new ArrayList();
    public void SetButtonIgnores(Rect[] rects)
    {
        foreach (Rect r in _buttonIgnores) _ignoreTouches.Remove(r);
        _buttonIgnores.Clear();
        if (rects == null) return;
        foreach (Rect r in rects)
        {
            _buttonIgnores.Add(r);
            if (!_ignoreTouches.Contains(r)) _ignoreTouches.Add(r);
        }
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

