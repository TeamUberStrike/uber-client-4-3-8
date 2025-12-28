using UnityEngine;

/// <summary>
/// 
/// </summary>
public static partial class GuiCircle
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="angle"></param>
    /// <param name="radius"></param>
    /// <param name="material"></param>
    /// <param name="dir"></param>
    public static void DrawArcLine(Vector2 position, float startAngle, float fillAngle, float radius, float width, Material material, Direction dir)
    {
        if (Event.current.type == EventType.Repaint)
        {
            material.SetPass(0);
            DrawSolidArc(new Vector3(position.x, position.y, 0), fillAngle, radius, width, Quaternion.Euler(0, 0, startAngle) * Vector3.down, dir);
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
    private static void DrawSolidArc(Vector3 center, float angle, float radius, float width, Vector3 from, Direction dir)
    {
        if (radius > 0)
        {
            //dynamic resolution based on angle
            int resolution = (int)Mathf.Clamp(angle * 0.1f, 5, 30);
            float fraction = 1f / (resolution - 1);
            float w = 1 - Mathf.Clamp(width / radius, 0.001f, 1);

            Quaternion quaternion = Quaternion.AngleAxis(angle * fraction, dir == Direction.Clockwise ? Normal : -Normal);
            Vector3 vector = (from * radius);

            GL.Begin(GL.QUADS);
            for (int i = 0; i < resolution - 1; i++)
            {
                Vector3 oldvector = vector;
                vector = (quaternion * vector);

                if (dir == Direction.Clockwise)
                {
                    GL.Vertex(center + oldvector);
                    GL.Vertex(center + vector);
                    GL.Vertex(center + vector * w);
                    GL.Vertex(center + oldvector * w);
                }
                else
                {
                    GL.Vertex(center + vector);
                    GL.Vertex(center + oldvector);
                    GL.Vertex(center + oldvector * w);
                    GL.Vertex(center + vector * w);
                }
            }
            GL.End();
        }
    }
}
