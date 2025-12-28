using UnityEngine;
using System.Collections;

public class ObjectiveTick
{
    private bool _completed;

    private Texture _bk;
    private Texture _tip;

    public ObjectiveTick(Texture bk, Texture tip)
    {
        _bk = bk;
        _tip = tip;
    }

    public void Draw(Vector2 position, float scale)
    {
        int size = 78;

        Vector2 pivot = new Vector2(position.x, position.y + size * scale / 2);

        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);

        GUI.BeginGroup(new Rect(position.x, position.y, size, size));
        {
            GUI.Label(new Rect(0, 0, 78, 78), _bk);

            if (_completed)
                GUI.DrawTexture(new Rect(4, 16, 62, 49), _tip);
        }
        GUI.EndGroup();

        GUI.matrix = Matrix4x4.identity;
    }

    public void Complete()
    {
        _completed = true;
    }
}
