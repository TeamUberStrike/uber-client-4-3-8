using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchDPad : TouchBaseControl
{
    // Uniform scale for the whole D-pad cluster (layout editor). 1 = authored pixel size.
    public float Scale = 1f;

    public Vector2 TopLeftPosition
    {
        set
        {
            float s = Scale;

            _dpadRect = new Rect(value.x, value.y, _dpad.image.width * s, _dpad.image.height * s);

            _leftRect = new Rect(value.x, value.y, 104 * s, 209 * s);

            _forwardRect = new Rect(value.x + 104 * s, value.y, 104 * s, 104 * s);
            _backwardRect = new Rect(value.x + 104 * s, value.y + 104 * s, 104 * s, 106 * s);

            _rightRect = new Rect(value.x + 207 * s, value.y + 103 * s, 104 * s, 106 * s);

            _centerPosition = new Vector2(value.x + 155 * s, value.y + 103 * s);

            if (CrouchButton != null)
            {
                Rect crouchRect = new Rect(value.x + 311 * s, value.y + 103 * s, 88 * s, 106 * s);
                CrouchButton.Boundary = crouchRect;
            }
            if (JumpButton != null)
            {
                Rect jumpRect = new Rect(value.x + 207 * s, value.y, 192 * s, 104 * s);
                JumpButton.Boundary = jumpRect;
            }

            // Refresh the rotation pivot after a move/resize so Jump/Crouch rotate about the new centre.
            if (_rotation != 0 && JumpButton != null && CrouchButton != null)
            {
                JumpButton.SetRotation(_rotation, _centerPosition);
                CrouchButton.SetRotation(_rotation, _centerPosition);
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

    public Rect DpadBoundary
    {
        get { return _dpadRect; }
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
        // Device input does not reliably deliver TouchPhase.Ended. Reconcile _fingers against
        // the live touch list so a released finger cannot keep asserting movement forever.
        if (_fingers.Count > 0)
        {
            _staleIds.Clear();
            foreach (int id in _fingers.Keys)
            {
                bool live = false;
                foreach (Touch t in TouchSource.Touches)
                {
                    if (t.fingerId == id && t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled)
                    {
                        live = true;
                        break;
                    }
                }
                if (!live) _staleIds.Add(id);
            }
            for (int i = 0; i < _staleIds.Count; i++)
                _fingers.Remove(_staleIds[i]);
        }

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

        if (!Moving)
        {
            _lastDirection = Vector2.zero;
            Direction = Vector2.zero;
            return;
        }

        // Keep a bit of inertia only while actively steering (smooth axis changes).
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

        // Draw scaled to _dpadRect (GUI.Label renders the image at its natural size, ignoring the
        // rect, so the editor's Scale never showed in-game — the size mismatch with the editor).
        GUI.DrawTexture(_dpadRect, _dpad.image, ScaleMode.StretchToFill, true);

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
    private readonly List<int> _staleIds = new List<int>();

    private Vector2 _lastDirection;

    #endregion
}

