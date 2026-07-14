using System;
using UnityEngine;
using System.Collections;

public class TouchButtonCircle : TouchButton
{
    // Layout scale multiplier (1 = native icon size). Driven by the customizable layout editor.
    public float LayoutScale = 1f;

    private Vector2 _centerPosition;
    public Vector2 CenterPosition
    {
        get { return _centerPosition; }
        set {
            _centerPosition = value;
            RecomputeBoundary();
        }
    }

    private void RecomputeBoundary()
    {
        if (Content != null && Content.image != null)
        {
            float w = Content.image.width * LayoutScale;
            float h = Content.image.height * LayoutScale;

            Boundary = new Rect(_centerPosition.x - w / 2f, _centerPosition.y - h / 2f, w, h);

            float r = w / 2f + 5; // add padding
            sqrRadius = r * r;
        }
    }

    // Sets position (GUI-space center) and scale together; used by the layout editor.
    public void SetLayout(Vector2 center, float scale)
    {
        LayoutScale = Mathf.Max(0.25f, scale);
        _centerPosition = center;
        RecomputeBoundary();
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

        // DrawTexture (not GUI.Label) so the icon scales with the layout-adjustable Boundary.
        if (Content != null && Content.image != null)
        {
            GUI.DrawTexture(Boundary, Content.image, ScaleMode.StretchToFill, true);
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
