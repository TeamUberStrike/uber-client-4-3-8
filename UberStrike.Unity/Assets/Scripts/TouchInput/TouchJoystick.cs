using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using System;

public class TouchJoystick : TouchBaseControl
{
    public Texture JoystickTexture;
    public Texture BackgroundTexture;
    public Vector2 MoveInteriaRolloff = new Vector2(6.0f, 5.0f);
    public float MinGUIAlpha = 0.4f;

    public event Action<Vector2> OnJoystickMoved;
    public event Action OnJoystickStopped;

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
                    if (_finger.FingerId != -1 && OnJoystickStopped != null)
                    {
                        OnJoystickStopped();
                    }
                    _finger.Reset();
                    _touchOrigin = Vector2.zero;
                }
            }
        }
    }

    public TouchJoystick()
        : base()
    {
        _finger = new TouchFinger();
    }

    public TouchJoystick(Texture joystick, Texture background) : this()
    {
        JoystickTexture = joystick;
        BackgroundTexture = background;
    }

    public override void UpdateTouches(Touch touch)
    {
        if (touch.phase == TouchPhase.Began && _finger.FingerId == -1 && Boundary.ContainsTouch(touch.position))
        {
            _finger = new TouchFinger()
            {
                StartPos = touch.position,
                StartTouchTime = Time.time,
                LastPos = touch.position,
                FingerId = touch.fingerId,
            };

            // Anchor the wheel at the lower corner of the zone instead of wherever the finger
            // lands, so the movement wheel always appears in the lower corner. Movement is
            // measured from the actual touch-down point so touching anywhere in the activation
            // zone does not immediately create a direction.
            const float edgeInset = 128f;
            Vector2 anchor = new Vector2(Boundary.xMin + JoystickTexture.width / 2 + edgeInset,
                                         Screen.height - Boundary.yMax + JoystickTexture.height / 2 + edgeInset);
            _touchOrigin = touch.position;
            _finger.LastPos = touch.position;
            _joystickPos = anchor;
            _joystickBoundary = new Rect(anchor.x - JoystickTexture.width / 2, anchor.y - JoystickTexture.height / 2, JoystickTexture.width, JoystickTexture.height);
        }
        else if (_finger.FingerId == touch.fingerId)
        {
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 anchor = _joystickBoundary.center;
                Vector2 offset = touch.position - _touchOrigin;
                float radius = Mathf.Min(_joystickBoundary.width, _joystickBoundary.height) / 2;
                if (offset.magnitude > radius)
                    offset = offset.normalized * radius;

                _joystickPos = anchor + offset;

                _finger.LastPos = touch.position;

                // get relative direction from -1 to 1 on x and y
                Vector2 dir = offset / radius;
                dir *= ApplicationDataManager.ApplicationOptions.TouchMoveSensitivity;

                if (touch.phase == TouchPhase.Moved && OnJoystickMoved != null)
                {
                    OnJoystickMoved(dir);
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (OnJoystickStopped != null)
                {
                    OnJoystickStopped();
                }
                _finger.Reset();
                _touchOrigin = Vector2.zero;
            }
        }
    }

    public override void Draw()
    {
        if (_finger.FingerId != -1)
        {
            GUI.Label(new Rect(_joystickPos.x - JoystickTexture.width / 2, (Screen.height - _joystickPos.y) - JoystickTexture.height / 2, JoystickTexture.width, JoystickTexture.height), JoystickTexture);

            GUI.color = new Color(1, 1, 1, Mathf.Clamp(TouchController.Instance.GUIAlpha, MinGUIAlpha, 1.0f));
            Vector2 basePosition = _joystickBoundary.center;
            GUI.Label(new Rect(basePosition.x - BackgroundTexture.width / 2, (Screen.height - basePosition.y) - BackgroundTexture.height / 2, BackgroundTexture.width, BackgroundTexture.height), BackgroundTexture);
            GUI.color = Color.white;
        }
    }

    #region Fields

    private TouchFinger _finger;
    private Rect _joystickBoundary;
    private Vector2 _joystickPos;
    private Vector2 _touchOrigin;

    #endregion
}
