using UnityEngine;

public static class TouchSource
{
    public static Touch[] Touches
    {
        get
        {
#if UNITY_EDITOR
            if (EditorTouchSimulator.Active)
                return EditorTouchSimulator.Touches;
#endif
            return Input.touches;
        }
    }

    public static int TouchCount
    {
        get { return Touches.Length; }
    }
}
