using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchDPad : TouchBaseControl
{
    public Vector2 TopLeftPosition
    {
        set { _topLeft = value; Rebuild(); }
    }

    // Cluster scale (1 = original size). Re-derives every hit-rect; the visual image is drawn into the
    // scaled _dpadRect by Draw(). Customizable via the layout editor's size slider.
    public float Scale
    {
        get { return _scale; }
        set { _scale = (value <= 0f) ? 1f : value; Rebuild(); }
    }

    // Recompute every hit-rect + the cluster image rect from the current top-left and scale, then refresh
    // the Jump/Crouch rotation pivots about the (possibly moved/scaled) centre.
    private void Rebuild()
    {
        Vector2 tl = _topLeft;
        float s = _scale;
        float iw = (_dpad != null && _dpad.image != null) ? _dpad.image.width : 399f;
        float ih = (_dpad != null && _dpad.image != null) ? _dpad.image.height : 209f;

        _dpadRect = new Rect(tl.x, tl.y, iw * s, ih * s);
        _leftRect = new Rect(tl.x, tl.y, 104f * s, 209f * s);
        _forwardRect = new Rect(tl.x + 104f * s, tl.y, 104f * s, 104f * s);
        _backwardRect = new Rect(tl.x + 104f * s, tl.y + 104f * s, 104f * s, 106f * s);
        _rightRect = new Rect(tl.x + 207f * s, tl.y + 103f * s, 104f * s, 106f * s);
        _centerPosition = new Vector2(tl.x + 155f * s, tl.y + 103f * s);

        if (CrouchButton != null)
        {
            CrouchButton.Boundary = new Rect(tl.x + 311f * s, tl.y + 103f * s, 88f * s, 106f * s);
            CrouchButton.SetRotation(_rotation, _centerPosition);
        }
        if (JumpButton != null)
        {
            JumpButton.Boundary = new Rect(tl.x + 207f * s, tl.y, 192f * s, 104f * s);
            JumpButton.SetRotation(_rotation, _centerPosition);
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
            if (JumpButton != null) JumpButton.SetRotation(value, _centerPosition);
            if (CrouchButton != null) CrouchButton.SetRotation(value, _centerPosition);
        }
    }

    public TouchButton JumpButton;
    public TouchButton CrouchButton;

    private GUIContent _dpad;

    private Vector2 _centerPosition;

    public Vector2 Direction { get; private set; }
    public bool Moving { get; private set; }

    // Editor/layout accessors: the whole D-pad cluster's GUI rect + the source image size, so the layout
    // editor can show it as a draggable handle and ApplyLayout can convert a saved CENTER back to the
    // top-left this control is positioned by.
    public Rect Bounds { get { return _dpadRect; } }
    public float ImageWidth { get { return (_dpad != null && _dpad.image != null) ? _dpad.image.width : 399f; } }
    public float ImageHeight { get { return (_dpad != null && _dpad.image != null) ? _dpad.image.height : 209f; } }

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
        // Prune fingers that are no longer touching the screen. On device the Ended/Canceled phase is
        // NOT reliably delivered to this control (it owns the finger via _fingers, but the dispatcher may
        // not route the lift), which left a stale finger asserting movement forever after the player
        // lifted off the D-pad (#65 v28 "player doesn't stop when finger released"). Reconcile _fingers
        // against the live touch list every frame so a released finger always clears.
        if (_fingers.Count > 0)
        {
            _staleIds.Clear();
            foreach (int id in _fingers.Keys)
            {
                bool live = false;
                foreach (Touch t in Input.touches)
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

        // Angle/vector-based input (replaces the old discrete twisted-rect hit-test, which felt "random"
        // when sliding a finger between the four 15°-rotated zones — #65 device feedback v27). We take the
        // registered finger's vector FROM the cluster centre and snap it to one of 8 directions, so the
        // player just points where they want to go. The finger's LastPos was already un-twisted in
        // UpdateTouches (rotated by _rotation about the centre), so this vector is in the axis-aligned
        // D-pad frame — the visual 15° twist is kept in Draw(), only the input model changed.

        // Centre in screen space (y-up), matching the rotation-corrected finger LastPos frame.
        Vector2 pivotScreen = new Vector2(_centerPosition.x, Screen.height - _centerPosition.y);

        // Deadzone scales with the cluster so it feels identical at any editor size (~quarter of a button).
        float deadzone = 26f * _scale;

        Vector2 dir = Vector2.zero;
        bool moving = false;

        foreach (TouchFinger finger in _fingers.Values)
        {
            Vector2 delta = finger.LastPos - pivotScreen; // +x = right, +y = forward (screen y-up)
            if (delta.sqrMagnitude < deadzone * deadzone)
                continue;

            // Snap the continuous angle to the nearest 45° step → cardinals + diagonals. Predictable,
            // unlike the old approach where the twisted zone boundaries fought a sliding finger.
            float octant = Mathf.Atan2(delta.y, delta.x) / (Mathf.PI / 4f); // 0 = right, 2 = forward
            switch (((Mathf.RoundToInt(octant) % 8) + 8) % 8)
            {
                case 0: dir = new Vector2(1, 0); break;   // right
                case 1: dir = new Vector2(1, 1); break;   // forward-right
                case 2: dir = new Vector2(0, 1); break;   // forward
                case 3: dir = new Vector2(-1, 1); break;  // forward-left
                case 4: dir = new Vector2(-1, 0); break;  // left
                case 5: dir = new Vector2(-1, -1); break; // backward-left
                case 6: dir = new Vector2(0, -1); break;  // backward
                case 7: dir = new Vector2(1, -1); break;  // backward-right
            }
            moving = true;
            break; // a single movement finger drives the D-pad
        }

        Moving = moving;

        // No active steering finger (released, or finger resting inside the deadzone) -> STOP immediately.
        // The inertia roll-off below was originally kept "for tap jumping", but jump is now a separate
        // right-side button (movement/firing were decoupled in the v28 fix), so that roll-off served only
        // to make the player keep gliding ~0.3s after lifting off -> the #65 v29 device bug "D-pad doesn't
        // stop on release / player slides". Hard-zero the direction so release is instant. The roll-off is
        // retained ONLY to smooth axis changes WHILE actively moving (e.g. diagonal -> cardinal).
        if (!moving)
        {
            _lastDirection = Vector2.zero;
            Direction = Vector2.zero;
            return;
        }

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
    private readonly List<int> _staleIds = new List<int>(); // scratch list for pruning released fingers

    private Vector2 _topLeft;
    private float _scale = 1f;

    private Vector2 _lastDirection;

    #endregion
}

