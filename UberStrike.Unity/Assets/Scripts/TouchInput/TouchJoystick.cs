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

            _joystickBoundary = new Rect(touch.position.x - JoystickTexture.width / 2, touch.position.y - JoystickTexture.height / 2, JoystickTexture.width, JoystickTexture.height);

        }
        else if (_finger.FingerId == touch.fingerId)
        {
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                _joystickPos.x = Mathf.Clamp(touch.position.x, _joystickBoundary.x, _joystickBoundary.x + _joystickBoundary.width);
                _joystickPos.y = Mathf.Clamp(touch.position.y, _joystickBoundary.y, _joystickBoundary.y + _joystickBoundary.height);

                _finger.LastPos = touch.position;

                // get relative direction from -1 to 1 on x and y
                Vector2 dir = Vector2.zero;
                dir.x = (_joystickPos.x - _finger.StartPos.x) * 2 / _joystickBoundary.width;
                dir.y = (_joystickPos.y - _finger.StartPos.y) * 2 / _joystickBoundary.height;
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
            }
        }
    }

    public override void Draw()
    {
        if (_finger.FingerId != -1)
        {
            GUI.Label(new Rect(_joystickPos.x - JoystickTexture.width / 2, (Screen.height - _joystickPos.y) - JoystickTexture.height / 2, JoystickTexture.width, JoystickTexture.height), JoystickTexture);

            GUI.color = new Color(1, 1, 1, Mathf.Clamp(TouchController.Instance.GUIAlpha, MinGUIAlpha, 1.0f));
            GUI.Label(new Rect(_finger.StartPos.x - BackgroundTexture.width / 2, (Screen.height - _finger.StartPos.y) - BackgroundTexture.height / 2, BackgroundTexture.width, BackgroundTexture.height), BackgroundTexture);
            GUI.color = Color.white;
        }
    }

    #region Fields

    private TouchFinger _finger;
    private Rect _joystickBoundary;
    private Vector2 _joystickPos;

    #endregion
}

