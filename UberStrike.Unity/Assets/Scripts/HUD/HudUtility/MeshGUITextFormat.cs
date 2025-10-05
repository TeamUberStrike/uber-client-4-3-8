using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGUITextFormat : Animatable2DGroup
{
    public delegate void SetStyle(MeshGUIText meshText);

    public MeshGUITextFormat(string formatText, BitmapFont bitmapFont, 
        TextAlignment textAlignment = TextAlignment.Left, SetStyle setTyleFunc = null)
    {
        _bitmapFont = bitmapFont;
        _setStyleFunc = setTyleFunc;
        SetTextAlignment(textAlignment);
        _scaleAnim = new Vector2Anim(OnScaleChange);
        _scaleAnim.Vec2 = Vector2.one;
        _setStyleFunc = setTyleFunc;
        FormatText = formatText;
    }

    public string FormatText
    {
        get { return _formatText; }
        set
        {
            _formatText = value;
            UpdateMeshTextGroup();
        }
    }

    public Vector2 Scale
    {
        get { return _scaleAnim.Vec2; }
        set
        {
            _scaleAnim.Vec2 = value;
        }
    }

    public float LineGap
    {
        get { return _verticalGapBetweenLines; }
        set
        {
            _verticalGapBetweenLines = value;
            ResetTransform();
        }
    }

    public float LineHeight
    {
        get
        {
            if (Group.Count > 0)
            {
                return (Group[0] as MeshGUIText).Size.y;
            }
            return 0.0f;
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

    public SetStyle SetStyleFunc
    {
        set { _setStyleFunc = value; }
    }

    public void UpdateStyle()
    {
        for (int i = 0; i < Group.Count; i++)
        {
            MeshGUIText meshGuiText = Group[i] as MeshGUIText;
            if (meshGuiText != null && _setStyleFunc != null)
            {
                _setStyleFunc(meshGuiText);
            }
        }
    }

    #region Private
    private void SetTextAlignment(TextAlignment textAlignment)
    {
        switch (textAlignment)
        {
            case TextAlignment.Left:
                _textAnchor = TextAnchor.UpperLeft;
                break;
            case TextAlignment.Center:
                _textAnchor = TextAnchor.UpperCenter;
                break;
            case TextAlignment.Right:
                _textAnchor = TextAnchor.UpperRight;
                break;
        }
    }

    private void UpdateMeshTextGroup()
    {
        ClearAndFree();

        List<string> stringArray = GetStringArray(_formatText);

        foreach (string str in stringArray)
        {
            MeshGUIText meshGUIText = new MeshGUIText(str, _bitmapFont, _textAnchor);
            if (_setStyleFunc != null)
            {
                _setStyleFunc(meshGUIText);
            }
            Group.Add(meshGUIText);
        }

        ResetTransform();
    }

    private List<string> GetStringArray(string formatStr)
    {
        List<string> stringArray = new List<string>();
        string tempStr = formatStr;
        while (true)
        {
            int index = tempStr.IndexOf('\n');
            if (index == -1)
            {
                if (!string.IsNullOrEmpty(tempStr))
                {
                    stringArray.Add(tempStr);
                }
                break;
            }
            stringArray.Add(tempStr.Substring(0, index));
            tempStr = tempStr.Substring(index + 1);
        }
        return stringArray;
    }

    private void OnShadowColorChange(Color oldColor, Color newColor)
    {
        foreach (IAnimatable2D animatable in Group)
        {
            MeshGUIText meshGuiText = animatable as MeshGUIText;
            meshGuiText.ShadowColorAnim.Color = newColor;
        }
    }

    private void OnScaleChange(Vector2 oldScale, Vector2 newScale)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        Vector2 posOffset = Vector2.zero;
        for (int i = 0; i < Group.Count; i++)
        {
            MeshGUIText meshGuiText = Group[i] as MeshGUIText;
            if (meshGuiText != null)
            {
                meshGuiText.Scale = _scaleAnim.Vec2;
                meshGuiText.Position = posOffset;
                posOffset += new Vector2(0.0f, meshGuiText.Size.y + _verticalGapBetweenLines);
            }
        }
    }

    private string _formatText;
    private BitmapFont _bitmapFont;
    private TextAnchor _textAnchor;
    private Vector2Anim _scaleAnim;
    private ColorAnim _shadowColorAnim;
    private float _verticalGapBetweenLines;
    private SetStyle _setStyleFunc;
    #endregion
}
