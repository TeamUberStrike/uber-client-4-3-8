
using UnityEngine;
using System.Collections.Generic;

public class MouseInput : Singleton<MouseInput>
{
    private Click Current;
    private Click Previous;

    public const float DoubleClickInterval = 0.3f;

    private MouseInput() { }

    public bool DoubleClick(Rect rect)
    {
        return (Time.time - Previous.Time < DoubleClickInterval) && GUITools.ToGlobal(rect).Contains(Current.Point);
    }

    private struct Click
    {
        public float Time;
        public Vector2 Point;
    }

    public void OnGUI()
    {
        if (Event.current.type == EventType.MouseDown)
        {
            Previous = Current;
            Current.Time = Time.time;
            Current.Point = Event.current.mousePosition;
        }
    }
}