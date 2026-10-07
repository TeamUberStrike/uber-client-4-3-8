using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;

public abstract class TouchBaseControl
{
    public virtual bool Enabled { get; set; }

    // Independent "player removed this control in the layout editor" gate. Kept SEPARATE from Enabled
    // because the touch state machine toggles Enabled every frame (UpdateWalkingEnabled, OnEnter, etc.)
    // and would otherwise undo a removal. TouchController skips Removed controls for BOTH input and draw;
    // TouchInput.ApplyLayout sets it from the saved layout. The movement joystick/D-pad are never removable.
    public bool Removed { get; set; }
    public virtual Rect Boundary { get; set; }
    public virtual void FirstUpdate() { }
    public virtual void UpdateTouches(Touch touch) { }
    public virtual void FinalUpdate() { }
    public virtual void Draw() { }

    public TouchBaseControl()
    {
        TouchController.Instance.AddControl(this);
    }

    ~TouchBaseControl()
    {
        TouchController.Instance.RemoveControl(this);
    }

    public class TouchFinger
    {
        public Vector2 StartPos;
        public Vector2 LastPos;
        public float StartTouchTime;
        public int FingerId;

        public TouchFinger()
        {
            Reset();
        }

        public void Reset()
        {
            StartPos = Vector2.zero;
            LastPos = Vector2.zero;
            StartTouchTime = 0;
            FingerId = -1;
        }
    }
}

