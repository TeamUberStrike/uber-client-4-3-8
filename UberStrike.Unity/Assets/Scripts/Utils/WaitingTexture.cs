using UnityEngine;

public static class WaitingTexture
{
    public static void Draw(Vector2 position, int size = 0)
    {
        if (size <= 0)
            size = 32;
        else
            size = Mathf.Clamp(size, 1, 32);

        // Rotate about the texture's real screen position so it spins in place.
        Vector2 sp = GUIUtility.GUIToScreenPoint(position);
        Vector2 pivot = new Vector2(sp.x, Screen.height - sp.y);
        Matrix4x4 prev = GUI.matrix;
        GUIUtility.RotateAroundPivot(Angle, pivot);
        GUI.DrawTexture(new Rect(position.x - size * 0.5f, position.y - size * 0.5f, size, size), UberstrikeTextures.Waiting);
        GUI.matrix = prev;
    }

    public static int Angle
    {
        get { return Mathf.RoundToInt(Time.time * 10) * 30; }
    }
}