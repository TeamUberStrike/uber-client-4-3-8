using System;
using UnityEngine;
using System.Collections;

public class TouchButtonCircle : TouchButton
{
    private Vector2 _centerPosition;
    public Vector2 CenterPosition
    {
        get { return _centerPosition; }
        set {
            if (value != _centerPosition)
            {
                if (Content != null)
                {
                    float halfWidth = Content.image.width / 2;

                    Boundary = new Rect(value.x - halfWidth, value.y - halfWidth, Content.image.width, Content.image.height);

                    halfWidth += 5; // add padding

                    sqrRadius = halfWidth * halfWidth;
                }

                _centerPosition = value;
            }
        }
    }

    public override bool Enabled
    {
        get
        {
            return base.Enabled;
        }
        set
        {
            base.Enabled = value;
            if (!base.Enabled && _quad != null)
            {
                _quad.FreeObject();
                _quad = null;
            }
        }
    }

    public bool ShowEffect = true;

    public float EffectTime = 0.25f;

    private float sqrRadius = 0;
    private float initialScale;

    public TouchButtonCircle(Texture texture)
        : base()
    {
        Content = new GUIContent(texture);
        initialScale = (float)texture.width / (float)ConsumableHudTextures.CircleWhite.width;
    }

    public override void FinalUpdate()
    {
        base.FinalUpdate();

        if (ShowEffect)
        {
            // if we've got a touch and no effect, start one
            if (_quad == null && finger.FingerId != -1)
            {
                _quad = new MeshGUIQuad(ConsumableHudTextures.CircleWhite, TextAnchor.MiddleCenter);
                _quad.Position = CenterPosition - new Vector2(3, 0); // compensate for difference in coordinate systems
                _quad.Scale = new Vector2(initialScale, initialScale);
                _timer = 0;
            }

            // if we've got the effect, update it
            if (_quad != null)
            {
                _quad.Scale = new Vector2((_timer / EffectTime + 1) * initialScale, (_timer / EffectTime + 1 ) * initialScale);
                _quad.Alpha = 1 - _timer / EffectTime;

                _timer += Time.deltaTime;
            }

            // if the effect has played, repeat
            if (_timer > EffectTime)
            {
                _timer = 0;

                // if we don't have a touch, reset but don't show the effect any longer
                if (finger.FingerId == -1)
                {
                    _quad.FreeObject();
                    _quad = null;
                }
            }
        }
    }


    public override void Draw()
    {
        GUI.color = new Color(1, 1, 1, Mathf.Clamp(TouchController.Instance.GUIAlpha, MinGUIAlpha, 1.0f));

        if (Content != null && Boundary != null)
        {
            GUI.Label(Boundary, Content);
        }
        else
        {
            Debug.LogWarning("You need to set a CenterPosition and Texture for the TouchButtonCircle to draw!");
        }

        GUI.color = Color.white;
    }

    protected override bool TouchInside(Vector2 position)
    {
        Vector2 center = new Vector2(Boundary.x + Boundary.width / 2, Boundary.y + Boundary.height / 2);

        // adjust for inverted-y coordinate system
        center.y = Screen.height - center.y;

        return (center - position).sqrMagnitude < sqrRadius;
    }

    private MeshGUIQuad _quad;
    private float _timer = 0;
}
