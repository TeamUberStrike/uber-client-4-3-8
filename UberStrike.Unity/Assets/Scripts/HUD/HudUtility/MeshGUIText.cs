using UnityEngine;

public class MeshGUIText : MeshGUIBase
{
    public MeshGUIText(string text, BitmapFont font,
        TextAnchor textAnchor = TextAnchor.UpperLeft, GameObject parentObject = null)
        : base(parentObject)
    {
        BitmapMeshText bitmapMeshText = BitmapMeshText;
        if (bitmapMeshText != null)
        {
            bitmapMeshText.Font = font;
            bitmapMeshText.Anchor = textAnchor;
        }
        Text = text;
        ShadowColorAnim.Color = BitmapMeshText.ShadowColor;
    }

    public override void FreeObject()
    {
        MeshGUIManager.Instance.FreeMeshText(_meshObject);
    }

    public override Vector2 GetOriginalBounds()
    {
        return TextBounds;
    }

    protected override Vector2 GetAdjustScale()
    {
        return Vector2.one;
    }

    protected override CustomMesh GetCustomMesh()
    {
        if (_meshObject != null)
        {
            return _meshObject.GetComponent<BitmapMeshText>();
        }
        return null;
    }

    protected override GameObject AllocObject(GameObject parentObject)
    {
        return MeshGUIManager.Instance.CreateMeshText(parentObject);
    }

    protected override void UpdateRect()
    {
        Vector2 offset = MeshGUIManager.Instance.
                TransformSizeFromWorldToScreen(BitmapMeshText.OffsetToUpperLeft);
        _rect.x = Position.x - offset.x * Scale.x;
        _rect.y = Position.y - offset.y * Scale.y;
        _rect.width = Size.x;
        _rect.height = Size.y;
    }

    public string NamePrefix
    {
        get { return _namePrefix; }
        set
        {
            _namePrefix = value;
            UpdateName();
        }
    }

    public string Text
    {
        get { return BitmapMeshText.Text; }
        set
        {
            if (BitmapMeshText.Text != value)
            {
                BitmapMeshText.Text = value;
                UpdateName();
            }
        }
    }

    public ColorAnim ShadowColorAnim
    {
        get
        {
            if (_shadowColorAnim == null)
            {
                _shadowColorAnim = new ColorAnim(OnShadowColorChange);
            }
            return _shadowColorAnim;
        }
    }

    public BitmapMeshText BitmapMeshText
    {
        get { return _customeMesh as BitmapMeshText; }
    }

    public Vector2 TextBounds
    {
        get
        {
            return MeshGUIManager.Instance.
                TransformSizeFromWorldToScreen(BitmapMeshText.Bounds);
        }
    }

    private void OnShadowColorChange(Color oldColor, Color newColor)
    {
        BitmapMeshText.ShadowColor = newColor;
    }

    private void UpdateName()
    {
        if (string.IsNullOrEmpty(_namePrefix))
        {
            _meshObject.name = Text;
        }
        else
        {
            _meshObject.name = _namePrefix + "_" + Text;
        }
    }

    private string _namePrefix;
    private ColorAnim _shadowColorAnim;
}