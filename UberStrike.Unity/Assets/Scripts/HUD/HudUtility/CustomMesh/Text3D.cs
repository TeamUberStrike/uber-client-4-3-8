using UnityEngine;

public class Text3D : BitmapMeshText
{
    [SerializeField]
    private string _textContent = "Cmune";
    [SerializeField]
    private BitmapFont _font;
    [SerializeField]
    private Color _innerColor = Color.black;
    [SerializeField]
    private Color _borderColor = Color.white;
    [SerializeField]
    private StyleType _textStyle = StyleType.Default;

    public enum StyleType
    {
        Default,
        Custom,
        NoBorder,
    }

    void Awake()
    {
        Font = _font;
        Anchor = UnityEngine.TextAnchor.MiddleCenter;
        Text = _textContent;

        switch (_textStyle)
        {
            case StyleType.Default: SetDefaultStyle(); break;
            case StyleType.NoBorder: SetNoShadowStyle(); break;
            case StyleType.Custom: SetCustomStyle(); break;
            default: break;
        }
    }

    private void SetCustomStyle()
    {
        Color = _innerColor;
        ShadowColor = _borderColor;
        AlphaMin = 0.45f;
        AlphaMax = 0.62f;
        ShadowAlphaMin = 0.20f;
        ShadowAlphaMax = 0.45f;
        ShadowOffsetU = 0.0f;
        ShadowOffsetV = 0.0f;
    }

    private void SetDefaultStyle()
    {
        Color = Color.white;
        ShadowColor = HudStyleUtility.DEFAULT_BLUE_COLOR;
        AlphaMin = 0.45f;
        AlphaMax = 0.62f;
        ShadowAlphaMin = 0.20f;
        ShadowAlphaMax = 0.45f;
        ShadowOffsetU = 0.0f;
        ShadowOffsetV = 0.0f;
    }

    private void SetNoShadowStyle()
    {
        Color = _innerColor;
        ShadowColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        AlphaMin = 0.18f;
        AlphaMax = 0.62f;
        ShadowAlphaMin = 0.18f;
        ShadowAlphaMax = 0.39f;
        ShadowOffsetU = 0.0f;
        ShadowOffsetV = 0.0f;
    }
}