using UnityEngine;
using System.Collections;

public class MeshGUIQuad : MeshGUIBase
{
    public MeshGUIQuad(Texture tex, TextAnchor anchor = TextAnchor.UpperLeft, GameObject parentObject = null)
        : base(parentObject)
    {
        Texture = tex;
        QuadMesh quadMesh = QuadMesh;
        if (quadMesh != null)
        {
            quadMesh.Anchor = anchor;
        }
    }

    public override void FreeObject()
    {
        MeshGUIManager.Instance.FreeQuadMesh(_meshObject);
    }

    public override Vector2 GetOriginalBounds()
    {
        if (Texture != null)
        {
            return new Vector2(Texture.width, Texture.height);
        }
        return Vector2.zero;
    }

    protected override Vector2 GetAdjustScale()
    {
        if (Texture != null)
        {
            return MeshGUIManager.Instance.TransformSizeFromScreenToWorld(
                        new Vector2(Texture.width, Texture.height));
        }
        return Vector2.zero;
    }

    protected override CustomMesh GetCustomMesh()
    {
        if (_meshObject != null)
        {
            return _meshObject.GetComponent<QuadMesh>();
        }
        return null;
    }

    protected override GameObject AllocObject(GameObject parentObject)
    {
        return MeshGUIManager.Instance.CreateQuadMesh(parentObject);
    }

    protected override void UpdateRect()
    {
        Vector2 offset = MeshGUIManager.Instance.TransformSizeFromWorldToScreen(QuadMesh.OffsetToUpperLeft);
        _rect.x = Position.x - offset.x * Scale.x;
        _rect.y = Position.y - offset.y * Scale.y;
        _rect.width = Size.x;
        _rect.height = Size.y;
    }

    public Texture Texture
    {
        get { return QuadMesh.Texture; }
        set { QuadMesh.Texture = value; }
    }

    public QuadMesh QuadMesh
    {
        get { return _customeMesh as QuadMesh; }
    }
}
