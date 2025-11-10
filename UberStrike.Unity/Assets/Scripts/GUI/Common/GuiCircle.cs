using UnityEngine;

/// <summary>
/// 
/// </summary>
public static partial class GuiCircle
{
    private static Vector3 TexShift = new Vector3(0.5f, 0.5f, 0.5f);
    private static Vector3 Normal = new Vector3(0, 0, 1);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="angle"></param>
    /// <param name="radius"></param>
    /// <param name="material"></param>
    public static void DrawArc(Vector2 position, float angle, float radius, Material material)
    {
        DrawArc(position, 0, angle, radius, material, Direction.Clockwise);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="angle"></param>
    /// <param name="radius"></param>
    /// <param name="material"></param>
    /// <param name="dir"></param>
    public static void DrawArc(Vector2 position, float startAngle, float fillAngle, float radius, Material material, Direction dir)
    {
        if (Event.current.type == EventType.Repaint)
        {
            GL.PushMatrix();
            material.SetPass(0);
            DrawSolidArc(new Vector3(position.x, position.y, 0), fillAngle, radius, Quaternion.Euler(0, 0, startAngle), dir);
            GL.PopMatrix();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="center"></param>
    /// <param name="angle"></param>
    /// <param name="radius"></param>
    /// <param name="from"></param>
    /// <param name="dir"></param>
    private static void DrawSolidArc(Vector3 center, float angle, float radius, Quaternion rot, Direction dir)
    {
        Vector3 from = rot * Vector3.down;
        //dynamic resolution based on angle
        int resolution = (int)Mathf.Clamp(angle * 0.1f, 5, 30);
        float fraction = 1f / (resolution - 1);

        Quaternion quaternion = Quaternion.AngleAxis(angle * fraction, dir == Direction.Clockwise ? Normal : -Normal);
        Vector3 vector = (from * radius);

        float d = 1 / (2 * radius);
        Vector3 diameter = new Vector3(d, -d, d);

        GL.Begin(GL.TRIANGLES);
        for (int i = 0; i < resolution - 1; i++)
        {
            Vector3 oldvector = vector;
            vector = (quaternion * vector);

            GL.TexCoord(TexShift);
            GL.Vertex(center);
            if (dir == Direction.Clockwise)
            {
                GL.TexCoord(TexShift + rot * Vector3.Scale(oldvector, diameter));
                GL.Vertex(center + oldvector);
                GL.TexCoord(TexShift + rot * Vector3.Scale(vector, diameter));
                GL.Vertex(center + vector);
            }
            else
            {
                GL.TexCoord(TexShift + rot * Vector3.Scale(vector, diameter));
                GL.Vertex(center + vector);
                GL.TexCoord(TexShift + rot * Vector3.Scale(oldvector, diameter));
                GL.Vertex(center + oldvector);
            }
        }
        GL.End();
    }

    public enum Direction
    {
        Clockwise = 0,
        CounterClockwise = 1,
    }
}
