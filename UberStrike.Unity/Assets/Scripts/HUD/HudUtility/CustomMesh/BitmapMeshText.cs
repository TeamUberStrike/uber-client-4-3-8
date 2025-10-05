using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BitmapMeshText : CustomMesh
{
    private string _text;
    private Vector3 _offset;
    private Color _mainColor;
    private Color _shadowColor;
    private float _alphaMin;
    private float _alphaMax;
    private float _shadowAlphaMin;
    private float _shadowAlphaMax;
    private float _shadowOffsetU;
    private float _shadowOffsetV;

    public BitmapFont Font { get; set; }
    public string Text
    {
        get { return _text; }
        set { _text = value; Reset(); }
    }
    public TextAnchor Anchor { get; set; }

    #region Quad Parameters

    private Vector3[] quadVerts = 
    {
        new Vector3(0, 0),
        new Vector3(0, 1),
        new Vector3(1, 1),
        new Vector3(1, 0)
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

    public override Color Color
    {
        get
        {
            return MainColor;
        }
        set
        {
            MainColor = value;
        }
    }

    public override float Alpha
    {
        get
        {
            return MainColor.a;
        }
        set
        {
            Color color = MainColor;
            color.a = value;
            MainColor = color;
            color = ShadowColor;
            color.a = value;
            ShadowColor = color;
        }
    }

    public Vector2 OffsetToUpperLeft { get; private set; }

    public Color MainColor
    {
        get
        {
            return _mainColor;
        }
        set
        {
            _mainColor = value;
            if (_meshRenderer != null)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetColor("_Color", value);
                }
            }
        }
    }

    public Color ShadowColor
    {
        get
        {
            return _shadowColor;
        }
        set
        {
            _shadowColor = value;
            if (_meshRenderer != null)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetColor("_ShadowColor", value);
                }
            }
        }
    }

    public float AlphaMin
    {
        get
        {
            return _alphaMin;
        }
        set
        {
            _alphaMin = value;
            if (_meshRenderer != null)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetFloat("_AlphaMin", value);
                }
            }
        }
    }

    public float AlphaMax
    {
        get
        {
            return _alphaMax;
        }
        set
        {
            _alphaMax = value;
            if (_meshRenderer != null)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetFloat("_AlphaMax", value);
                }
            }
        }
    }

    public float ShadowAlphaMin
    {
        get
        {
            return _shadowAlphaMin;
        }
        set
        {
            _shadowAlphaMin = value;

            if (_meshRenderer)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetFloat("_ShadowAlphaMin", value);
                }
            }
        }
    }

    public float ShadowAlphaMax
    {
        get
        {
            return _shadowAlphaMax;
        }
        set
        {
            _shadowAlphaMax = value;

            if (_meshRenderer)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetFloat("_ShadowAlphaMax", value);
                }
            }
        }
    }

    public float ShadowOffsetU
    {
        get
        {
            return _shadowOffsetU;
        }
        set
        {
            _shadowOffsetU = value;

            if (_meshRenderer)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetFloat("_ShadowOffsetU", value);
                }
            }
        }
    }

    public float ShadowOffsetV
    {
        get
        {
            return _shadowOffsetV;
        }
        set
        {
            _shadowOffsetV = value;

            if (_meshRenderer)
            {
                foreach (Material mat in _meshRenderer.materials)
                {
                    mat.SetFloat("_ShadowOffsetV", value);
                }
            }
        }
    }

    protected override void Reset()
    {
        if (Font == null)
        {
            return;
        }

        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (_meshFilter == null)
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        Vector3 renderSize = new Vector3(1, 1, 1);
        Vector2 renderSize2 = new Vector2(renderSize.x, renderSize.y);

        //Calculate bounding box of rendered text
        _bounds = Font.CalculateSize(Text, renderSize2);

        _offset = new Vector3(0, 0);
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

        //Replace mesh
        Mesh mesh = GenerateMesh();
        if (_meshFilter.sharedMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_meshFilter.sharedMesh);
            }
            else
            {
                DestroyImmediate(_meshFilter.sharedMesh);
            }
        }
        _meshFilter.mesh = mesh;

        //Get materials for each texture page
        Material[] mats = new Material[mesh.subMeshCount];
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            mats[i] = Font.GetPageMaterial(i, _shader);
        }

        foreach (var m in _meshRenderer.materials)
            Destroy(m);

        _meshRenderer.materials = mats;
        ResetMaterial();
    }

    protected override Mesh GenerateMesh()
    {
        Vector3 renderSize = new Vector3(1, 1, 1);
        string str = Text;
        Vector3 position = new Vector3(0, 0, 0) - _offset;

        //Set up mesh structures
        List<int> Triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        //Keep track of position
        Vector3 curPos = position;
        Vector3 scale = renderSize / Font.Size;

        for (int idx = 0; !string.IsNullOrEmpty(str) && idx < str.Length; idx++)
        {
            char c = str[idx];
            BitmapChar charInfo = Font.GetBitmapChar((int)c);
            int vertIndex = vertices.Count;

            //Set up uvs
            Rect uvRect = Font.GetUVRect(charInfo);
            Vector2 uvScale = new Vector2(uvRect.width, uvRect.height);
            Vector2 uvOffset = new Vector2(uvRect.x, uvRect.y);
            for (int i = 0; i < quadUvs.Length; i++)
            {
                uvs.Add(Vector2.Scale(quadUvs[i], uvScale) + uvOffset);
            }

            //Set up verts
            Vector3 vertSize = Vector2.Scale(charInfo.Size, scale);
            Vector3 vertOffset = Vector2.Scale(charInfo.Offset, scale);
            vertOffset.y = renderSize.y - (vertOffset.y + vertSize.y);  // change offset from top to bottom
            for (int i = 0; i < quadVerts.Length; i++)
            {
                Vector3 vert = Vector3.Scale(quadVerts[i], vertSize) + curPos + vertOffset;
                vertices.Add(vert);
            }

            //Set up triangles
            for (int i = 0; i < quadTriangles.Length; i++)
            {
                Triangles.Add(quadTriangles[i] + vertIndex);
            }

            //Advance cursor
            float krn = 0;
            if (idx < Text.Length - 1)
            {
                krn = Font.GetKerning(c, Text[idx + 1]);
            }
            curPos.x += (charInfo.XAdvance + krn) * scale.x;
        }

        //Assign verts, uvs, tris and materials to mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.subMeshCount = 1;
        mesh.SetTriangles(Triangles.ToArray(), 0);

        return mesh;
    }

    private void ResetMaterial()
    {
        if (_meshRenderer != null)
        {
            foreach (Material mat in _meshRenderer.materials)
            {
                mat.SetColor("_Color", _mainColor);
                mat.SetColor("_ShadowColor", _shadowColor);
                mat.SetFloat("_AlphaMin", _alphaMin);
                mat.SetFloat("_AlphaMax", _alphaMax);
                mat.SetFloat("_ShadowAlphaMin", _shadowAlphaMin);
                mat.SetFloat("_ShadowAlphaMax", _shadowAlphaMax);
                mat.SetFloat("_ShadowOffsetU", _shadowOffsetU);
                mat.SetFloat("_ShadowOffsetV", _shadowOffsetV);
            }
        }
    }
}
