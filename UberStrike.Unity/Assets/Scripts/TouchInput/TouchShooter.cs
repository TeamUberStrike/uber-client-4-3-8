using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchShooter : TouchBaseControl
{
    public Vector2 Aim { get; private set; }

    /// <summary>
    /// True while the tap-to-fire (secondary) finger is still live. Used by TouchInput's fire
    /// safety net so D-pad-mode fire (owned by the Shooter) isn't released because the on-screen
    /// fire button is disabled.
    /// </summary>
    public bool IsFireFingerLive { get { return _secondaryFinger.FingerId != -1; } }

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
                // Clear fingers AND the stale Aim on every enable/disable transition. A finger held
                // through death -> respawn never re-binds as the primary (its Began already passed
                // while the Shooter was disabled), so a leftover non-zero Aim would keep feeding
                // WishLook and rotate the POV with no input.
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
        if (touch.phase == TouchPhase.Began && Boundary.ContainsTouch(touch.position))
        {
            // Fingers that start on the D-pad, joystick, fire/jump buttons or any other on-screen
            // control belong to that control and must NOT count toward the look/shoot fingers.
            // Only fingers that begin in free space compete for the two slots, so the first free
            // finger is always the aim finger and the second is the fire finger, no matter how
            // many other fingers are resting on buttons.
            if (!ValidArea(touch.position))
                return;

            if (_primaryFinger.FingerId == -1)
            {
                _primaryFinger = new TouchFinger()
                {
                    StartPos = touch.position,
                    StartTouchTime = Time.time,
                    LastPos = touch.position,
                    FingerId = touch.fingerId,
                };
                Debug.Log("[ShooterDiag] primary bind finger=" + touch.fingerId + " pos=" + touch.position);

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
                Debug.Log("[ShooterDiag] secondary bind -> FIRE finger=" + touch.fingerId + " pos=" + touch.position);
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
        // iOS does not always deliver TouchPhase.Ended. If it is dropped for the AIM finger,
        // _primaryFinger stays bound to a finger that is gone, and the next finger binds as the
        // SECONDARY (fire) finger instead of the primary, so the POV stops moving. Prune any
        // finger that has left the live touch set so a fresh finger can re-bind as primary.
        if (_primaryFinger.FingerId != -1 && !IsFingerLive(_primaryFinger.FingerId))
        {
            _primaryFinger.Reset();
            Aim = Vector2.zero;
        }
        if (_secondaryFinger.FingerId != -1 && !IsFingerLive(_secondaryFinger.FingerId))
        {
            _secondaryFinger.Reset();
            if (OnFireEnd != null) OnFireEnd();
        }
    }

    private static bool IsFingerLive(int fingerId)
    {
        foreach (Touch t in TouchSource.Touches)
        {
            if (t.fingerId == fingerId) return true;
        }
        return false;
    }

    // Replaces the full set of zones that do not participate in the look/shoot finger counting
    // (D-pad, joystick, fire/jump/... buttons, quick items, weapon changer, scope swipe).
    // TouchInput rebuilds this every frame from the controls that are currently enabled, so it
    // always matches the current state, mode and custom layout.
    public void SetIgnoreRects(IEnumerable<Rect> rects)
    {
        _ignoreTouches.Clear();
        if (rects == null) return;
        foreach (Rect r in rects)
        {
            if (!_ignoreTouches.Contains(r))
                _ignoreTouches.Add(r);
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

