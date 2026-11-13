using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GuiDropDown
{
    private class Button
    {
        private GUIContent onContent;
        private GUIContent offContent;
        private Func<bool> isOn;

        public Action Action;
        public Func<bool> IsEnabled;

        public GUIContent Content
        {
            get { return (isOn == null || isOn()) ? onContent : offContent; }
        }

        public Button(GUIContent onContent)
            : this(onContent, onContent, () => true)
        { }

        public Button(GUIContent onContent, GUIContent offContent, Func<bool> isOn)
        {
            this.onContent = onContent;
            this.offContent = offContent;
            this.isOn = isOn;
            IsEnabled = () => true;
            Action = () => { };
        }
    }

    private List<Button> _data = new List<Button>();
    private bool _isDown = false;
    private Rect _rect;

    public GUIContent Caption { get; set; }
    public float ButtonWidth = 100;
    public float ButtonHeight = 20;

    /// <summary>
    /// Extra scale of the popup list relative to the caller's GUI space (1 = authored 100x20 rows).
    /// Callers drawing inside MobileMenuScale.Begin/End set this to a touch boost on mobile.
    /// </summary>
    public float PopupScale = 1f;

    /// <summary>
    /// Extra invisible hit area (in the caller's GUI space) added to the left/right of the caption
    /// button. The caption is still drawn at its normal rect. Horizontal only, so it never steals taps
    /// from controls above/below the button.
    /// </summary>
    public float HitPaddingLeft = 0f;
    public float HitPaddingRight = 0f;

    public bool IsOpen { get { return _isDown; } }

    public GuiDropDown() { }

    public void Add(GUIContent content, Action onClick)
    {
        _data.Add(new Button(content)
        {
            Action = onClick,
        });
    }

    public void Add(GUIContent onContent, GUIContent offContent, Func<bool> isOn, Action onClick)
    {
        _data.Add(new Button(onContent, offContent, isOn)
        {
            Action = onClick,
        });
    }

    /// <summary>Sets the caption rect, converting from the current GUI group to screen space.
    /// Only valid when GUI.matrix is identity.</summary>
    public void SetRect(Rect rect)
    {
        _rect = GUITools.ToGlobal(rect);
    }

    /// <summary>
    /// Sets the caption rect directly in the current GUI coordinate space (no group conversion).
    /// Use this when drawing at top level under a scaled GUI.matrix (MobileMenuScale), where
    /// GUITools.ToGlobal would apply the scale a second time.
    /// </summary>
    public void SetLocalRect(Rect rect)
    {
        _rect = rect;
    }

    /// <summary>Closes the popup and releases camera input (e.g. when the owner is hidden).</summary>
    public void Close()
    {
        if (_isDown)
        {
            _isDown = false;
            MouseOrbit.Disable = false;
        }
    }

    public void Draw()
    {
        if (HitPaddingLeft > 0f || HitPaddingRight > 0f)
        {
            Rect hitRect = new Rect(_rect.x - HitPaddingLeft, _rect.y, _rect.width + HitPaddingLeft + HitPaddingRight, _rect.height);
            if (Event.current.type == EventType.Repaint)
                BlueStonez.buttondark_medium.Draw(_rect, Caption, hitRect.Contains(Event.current.mousePosition), false, _isDown, false);
            _isDown = GUI.Toggle(hitRect, _isDown, GUIContent.none, GUIStyle.none);
        }
        else
        {
            _isDown = GUI.Toggle(_rect, _isDown, Caption, BlueStonez.buttondark_medium);
        }

        if (_isDown)
        {
            float popupScale = Mathf.Max(0.1f, PopupScale);
            MouseOrbit.Disable = true;

            if (!new Rect(_rect.xMax - ButtonWidth * popupScale, _rect.yMax, ButtonWidth * popupScale, ButtonHeight * popupScale * _data.Count).Contains(Event.current.mousePosition)
                && !new Rect(_rect.x - HitPaddingLeft, _rect.y, _rect.width + HitPaddingLeft + HitPaddingRight, _rect.height).Contains(Event.current.mousePosition) 
                && (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used))
            {
                _isDown = false;
                MouseOrbit.Disable = false;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            if (popupScale != 1f)
                MobileMenuScale.ScaleAroundLocalPivot(new Vector2(popupScale, popupScale), new Vector2(_rect.xMax, _rect.yMax));

            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i].IsEnabled())
                {
                    GUIStyle style = BlueStonez.dropdown;
                    if (GUI.Button(new Rect(_rect.xMax - ButtonWidth, _rect.yMax + (i * ButtonHeight), ButtonWidth, ButtonHeight), _data[i].Content, style))
                    {
                        _isDown = false;
                        MouseOrbit.Disable = false;
                        _data[i].Action();
                    }
                }
            }

            GUI.matrix = previousMatrix;
        }
    }
}