
using UnityEngine;
using System.Collections.Generic;

public class ExplosionDebug : MonoSingleton<ExplosionDebug>
{
    public static Vector3 ImpactPoint;
    public static Vector3 TestPoint;
    public static float Radius;

    public static List<Vector3> Hits = new List<Vector3>();
    public static List<Line> Protections = new List<Line>();

    public static void Reset()
    {
        Hits.Clear();
        Protections.Clear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(ImpactPoint, Radius);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(TestPoint, 0.1f);

        for (int i = 0; i < Hits.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Hits[i], 0.1f);
        }

        for (int i = 0; i < Protections.Count; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Protections[i].Start, Protections[i].End);
        }
    }

    public struct Line
    {
        public Line(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }
        public Vector3 Start;
        public Vector3 End;
    }
}
