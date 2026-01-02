using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchDPad : TouchBaseControl
{
    public Vector2 TopLeftPosition
    {
        set
        {
            _dpadRect = new Rect(value.x, value.y, _dpad.image.width, _dpad.image.height);

            _leftRect = new Rect(value.x, value.y, 104, 209);

            _forwardRect = new Rect(value.x + 104, value.y, 104, 104);
            _backwardRect = new Rect(value.x + 104, value.y + 104, 104, 106);

            _rightRect = new Rect(value.x + 207, value.y + 103, 104, 106);

            _centerPosition = new Vector2(value.x + 155, value.y + 103);

            if (CrouchButton != null)
            {
                Rect crouchRect = new Rect(value.x + 311, value.y + 103, 88, 106);
                CrouchButton.Boundary = crouchRect;
            }
            if (JumpButton != null)
            {
                Rect jumpRect = new Rect(value.x + 207, value.y, 192, 104);
                JumpButton.Boundary = jumpRect;
            }
        }
    }

    private bool enabled = false;
    public override bool Enabled
    {
        get { return enabled; }
        set
        {
            if (value != enabled)
            {
                enabled = value;

                if (JumpButton != null)
                    JumpButton.Enabled = value;
                if (CrouchButton != null)
                    CrouchButton.Enabled = value;

                _lastDirection = Vector2.zero;
                Direction = Vector2.zero;
                if (!enabled)
                {
                    _fingers.Clear();
                    Moving = false;
                }
            }
        }
    }

    public Vector2 TapDelay = new Vector2(0.2f, 0.2f);
    public Vector2 MoveInteriaRolloff = new Vector2(12.0f, 10.0f);

    private float _rotation = 0;
    public float Rotation
    {
        get
        {
            return _rotation;
        }
        set
        {
            _rotation = value;
            JumpButton.SetRotation(value, _centerPosition);
            CrouchButton.SetRotation(value, _centerPosition);
        }
    }

    public TouchButton JumpButton;
    public TouchButton CrouchButton;

    private GUIContent _dpad;

    private Vector2 _centerPosition;

    public Vector2 Direction { get; private set; }
    public bool Moving { get; private set; }

    public float MinGUIAlpha = 0.3f;

    public TouchDPad()
        : base()
    {
        _fingers = new Dictionary<int, TouchFinger>();
        _lastDirection = Vector2.zero;
        Moving = false;
    }

    public TouchDPad(Texture dpad)
        : this()
    {
        _dpad = new GUIContent(dpad);

        JumpButton = new TouchButton();
        CrouchButton = new TouchButton();
    }

    // does the touch fall within any of the four buttons
    public bool InsideBoundary(Vector2 position)
    {
        return _forwardRect.ContainsTouch(position)
            || _leftRect.ContainsTouch(position)
            || _rightRect.ContainsTouch(position)
            || _backwardRect.ContainsTouch(position);
    }

    // check if a double tap on a button was recorded
    /*public void CheckDoubleTap(Vector2 position)
    {
        if (_leftRect.ContainsTouch(position))
        {
            if ((Time.time - _lastLeftTouch) < TapDelay.x)
                if (OnDoubleTap != null) OnDoubleTap();
            _lastLeftTouch = Time.time;
        }
        else if (_rightRect.ContainsTouch(position))
        {
            if ((Time.time - _lastRightTouch) < TapDelay.x)
                if (OnDoubleTap != null) OnDoubleTap();
            _lastRightTouch = Time.time;
        }
        else if (_forwardRect.ContainsTouch(position))
        {
            if ((Time.time - _lastForwardTouch) < TapDelay.y)
                if (OnDoubleTap != null) OnDoubleTap();
            _lastForwardTouch = Time.time;
        }
        else if (_backwardRect.ContainsTouch(position))
        {
            if ((Time.time - _lastBackwardTouch) < TapDelay.y)
                if (OnDoubleTap != null) OnDoubleTap();
            _lastBackwardTouch = Time.time;
        }
    }*/



    public override void UpdateTouches(Touch touch)
    {
        Vector2 pos = Mathfx.RotateVector2AboutPoint(touch.position, new Vector2(_centerPosition.x, Screen.height -_centerPosition.y), _rotation);
        if (touch.phase == TouchPhase.Began && InsideBoundary(pos))
        {
            _fingers.Remove(touch.fingerId); // remove in case end phase wasn't sent
            _fingers.Add(touch.fingerId, new TouchFinger()
            {
                StartPos = pos,
                StartTouchTime = Time.time,
                LastPos = pos,
                FingerId = touch.fingerId,
            });
            //CheckDoubleTap(pos);
        }
        else if (touch.phase == TouchPhase.Moved)
        {
            if (_fingers.ContainsKey(touch.fingerId))
            {
                _fingers[touch.fingerId].LastPos = pos;
            }
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            _fingers.Remove(touch.fingerId);
        }
    }

    public override void FinalUpdate()
    {
        bool leftTouched = false;
        bool rightTouched = false;
        bool forwardTouched = false;
        bool backwardTouched = false;

        foreach (TouchFinger finger in _fingers.Values)
        {
            if (_leftRect.ContainsTouch(finger.LastPos))
                leftTouched = true;
            else if (_rightRect.ContainsTouch(finger.LastPos))
                rightTouched = true;
            else if (_forwardRect.ContainsTouch(finger.LastPos))
                forwardTouched = true;
            else if (_backwardRect.ContainsTouch(finger.LastPos))
                backwardTouched = true;
        }

        Vector2 dir = Vector2.zero;
        if (leftTouched) dir += new Vector2(-1, 0);
        if (rightTouched) dir += new Vector2(1, 0);
        if (forwardTouched) dir += new Vector2(0, 1);
        if (backwardTouched) dir += new Vector2(0, -1);

        if (leftTouched || rightTouched || forwardTouched || backwardTouched) Moving = true;
        else Moving = false;

        // Don't stop the player immediately, keep a bit of intertia for tap jumping
        if (dir.y == 0)
            dir.y = Mathf.Lerp(_lastDirection.y, dir.y, Time.deltaTime * MoveInteriaRolloff.y);

        if (dir.x == 0)
            dir.x = Mathf.Lerp(_lastDirection.x, dir.x, Time.deltaTime * MoveInteriaRolloff.x);

        _lastDirection = Direction;
        Direction = dir;
    }

    public override void Draw()
    {
        GUI.color = new Color(1, 1, 1, Mathf.Clamp(TouchController.Instance.GUIAlpha, MinGUIAlpha, 1.0f));

        GUIUtility.RotateAroundPivot(_rotation, _centerPosition);

        GUI.Label(_dpadRect, _dpad);

        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
    }

    #region Fields

    private Rect _leftRect;
    private Rect _rightRect;
    private Rect _forwardRect;
    private Rect _backwardRect;
    private Rect _dpadRect;
    private Dictionary<int, TouchFinger> _fingers;

    private Vector2 _lastDirection;

    #endregion
}

