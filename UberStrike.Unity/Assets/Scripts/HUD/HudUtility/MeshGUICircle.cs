using UnityEngine;
using System.Collections;


public class MeshGUICircle : MeshGUIBase
{
    public MeshGUICircle(Texture tex, GameObject parentObject = null)
        : base(parentObject)
    {
        Texture = tex;
        _angleAnim = new FloatAnim(OnAngleChange);
    }

    public override void FreeObject()
    {
        MeshGUIManager.Instance.FreeCircleMesh(_meshObject);
    }

    public override void Draw(float offsetX = 0.0f, float offsetY = 0.0f)
    {
        base.Draw(offsetX, offsetY);
        _angleAnim.Update();
    }

    public override Vector2 GetOriginalBounds()
    {
        if (Texture != null)
        {
            return new Vector2(Texture.width, Texture.height);
        }
        return Vector2.zero;
    }

    public void AnimAngleTo(float destAngle, float time = 0.0f, EaseType easeType = 0)
    {
        _angleAnim.AnimTo(destAngle, time, easeType);
    }

    public void AnimAngleDelta(float deltaAngle, float time = 0.0f, EaseType easeType = 0)
    {
        _angleAnim.AnimBy(deltaAngle, time, easeType);
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
            return _meshObject.GetComponent<CircleMesh>();
        }
        return null;
    }

    protected override GameObject AllocObject(GameObject parentObject)
    {
        return MeshGUIManager.Instance.CreateCircleMesh(parentObject);
    }

    protected override void UpdateRect()
    {
        _rect.x = Position.x - Size.x / 2;
        _rect.y = Position.y - Size.x / 2;
        _rect.width = Size.x;
        _rect.height = Size.y;
    }

    public Texture Texture
    {
        get { return CircleMesh.Texture; }
        set { CircleMesh.Texture = value; }
    }

    public CircleMesh CircleMesh
    {
        get { return _customeMesh as CircleMesh; }
    }

    public float Angle
    {
        get { return CircleMesh.FillAngle; }
        set { CircleMesh.FillAngle = value; }
    }

    private void OnAngleChange(float oldAngle, float newAngle)
    {
        CircleMesh.FillAngle = newAngle;
    }

    private FloatAnim _angleAnim;
}
