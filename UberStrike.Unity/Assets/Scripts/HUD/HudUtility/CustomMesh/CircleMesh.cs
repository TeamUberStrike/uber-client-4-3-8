using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CircleMesh : CustomMesh
{
    private static Vector3 Normal = new Vector3(0, 0, -1);
    private static Vector3 TexShift = new Vector3(0.5f, 0.5f, 0.5f);

    private Vector3 _center;
    private float _radius;
    private float _startAngle;
    private float _fillAngle;

    public float Radius
    {
        get { return _radius; }
    }

    public float StartAngle
    {
        get { return _startAngle; }
        set { _startAngle = value; Reset(); }
    }

    public float FillAngle
    {
        get { return _fillAngle; }
        set { _fillAngle = value; Reset(); }
    }

    protected override Mesh GenerateMesh()
    {
        Quaternion rot = Quaternion.Euler(0, 0, -_startAngle);
        Vector3 from = rot * Vector3.up;
        int resolution = (int)Mathf.Clamp(Mathf.Abs(_fillAngle) * 0.1f, 5, 30);
        float fraction = 1f / (resolution - 1);

        Quaternion quaternion = Quaternion.AngleAxis(_fillAngle * fraction, Normal);
        Vector3 vector = (from * _radius);

        float d = 1 / (2 * _radius);
        Vector3 diameter = new Vector3(d, d, d);

        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        for (int i = 0; i < resolution - 1; i++)
        {
            Vector3 oldvector = vector;
            vector = (quaternion * vector);

            uvs.Add(TexShift);
            vertices.Add(_center);
            normals.Add(Normal);
            triangles.Add(i * 3);

            uvs.Add(TexShift + Vector3.Scale(oldvector, diameter));
            vertices.Add(_center + oldvector);
            normals.Add(Normal);
            triangles.Add(i * 3 + 1);

            uvs.Add(TexShift + Vector3.Scale(vector, diameter));
            vertices.Add(_center + vector);
            normals.Add(Normal);
            triangles.Add(i * 3 + 2);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.subMeshCount = 1;
        mesh.SetTriangles(triangles.ToArray(), 0);
        return mesh;
    }

    private void Awake()
    {
        _radius = 0.5f;
        _center = Vector3.zero;
        _startAngle = 0.0f;
        _fillAngle = 360.0f;
        Reset();
    }
}
