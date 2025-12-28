using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuadMesh : CustomMesh
{
    #region Quad Parameters

    private Vector3[] quadVerts = 
    {
        new Vector3(0, -1),
        new Vector3(0, 0),
        new Vector3(1, 0),
        new Vector3(1, -1)
    };

    private Vector2[] quadUvs = 
    {
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(1, 0)
    };

    private int[] quadTriangles = 
    { 
        0, 1, 2,
        2, 3, 0
    };

    #endregion

    public TextAnchor Anchor
    {
        get
        {
            return _anchor;
        }
        set
        {
            _anchor = value;
            Reset();
        }
    }

    public Vector2 OffsetToUpperLeft { get; private set; }

    protected override Mesh GenerateMesh()
    {
        _bounds = Vector2.one;
        CalculateOffset();

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        for (int i = 0; i < quadVerts.Length; i++)
        {
            Vector3 vert = new Vector3();
            vert.x = quadVerts[i].x * _bounds.x - _offset.x;
            vert.y = quadVerts[i].y * _bounds.y + (1 - _offset.y);
            vertices.Add(vert);
        }
        mesh.vertices = vertices.ToArray();
        mesh.uv = quadUvs;
        mesh.subMeshCount = 1;
        mesh.SetTriangles(quadTriangles, 0);
        return mesh;
    }

    private void Awake()
    {
        Reset();
    }

    private void CalculateOffset()
    {
        _offset = Vector2.zero;
        if (Anchor == TextAnchor.UpperCenter || Anchor == TextAnchor.UpperLeft || Anchor == TextAnchor.UpperRight)
        {
            _offset.y = _bounds.y;
        }
        if (Anchor == TextAnchor.MiddleCenter || Anchor == TextAnchor.MiddleLeft || Anchor == TextAnchor.MiddleRight)
        {
            _offset.y = _bounds.y / 2;
        }
        if (Anchor == TextAnchor.UpperRight || Anchor == TextAnchor.MiddleRight || Anchor == TextAnchor.LowerRight)
        {
            _offset.x = _bounds.x;
        }
        if (Anchor == TextAnchor.UpperCenter || Anchor == TextAnchor.MiddleCenter || Anchor == TextAnchor.LowerCenter)
        {
            _offset.x = _bounds.x / 2;
        }
        OffsetToUpperLeft = new Vector2(_offset.x, _bounds.y - _offset.y);
    }

    private TextAnchor _anchor;
    private Vector2 _offset;
}
