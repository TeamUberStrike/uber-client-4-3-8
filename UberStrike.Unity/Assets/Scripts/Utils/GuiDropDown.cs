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

    public void SetRect(Rect rect)
    {
        _rect = GUITools.ToGlobal(rect);
    }

    public void Draw()
    {
        _isDown = GUI.Toggle(_rect, _isDown, Caption, BlueStonez.buttondark_medium);

        if (_isDown)
        {
            float popupScale = ApplicationDataManager.IsMobile ? 2f : 1f;
            MouseOrbit.Disable = true;

            if (!new Rect(_rect.xMax - ButtonWidth * popupScale, _rect.yMax, ButtonWidth * popupScale, ButtonHeight * popupScale * _data.Count).Contains(Event.current.mousePosition)
                && !_rect.Contains(Event.current.mousePosition) 
                && (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used))
            {
                _isDown = false;
                MouseOrbit.Disable = false;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            if (popupScale != 1f)
                GUIUtility.ScaleAroundPivot(new Vector2(popupScale, popupScale), new Vector2(_rect.xMax, _rect.yMax));

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