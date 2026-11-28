using UnityEngine;

public static class GuiManager
{
    public static void DrawTooltip()
    {
        // Not during a finger scroll (stale pointer).
        if (!string.IsNullOrEmpty(GUI.tooltip) && !GUITools.IsHoverSuppressedByTouchScroll)
        {
            Matrix4x4 currentMatrix = GUI.matrix;
            // reset scaling, so GUI tooltip stay same size

            GUI.matrix = Matrix4x4.identity;
            GUIStyle style = TooltipStyle;
            float k = _tooltipScale;
            Vector2 tipSize = style.CalcSize(new GUIContent(GUI.tooltip));
            Rect position = new Rect(Mathf.Clamp(Event.current.mousePosition.x, 14 * k, Screen.width - (tipSize.x + 14 * k)), Event.current.mousePosition.y + 24 * k, tipSize.x, tipSize.y + 16 * k);

            //check bounds
            if (position.yMax > Screen.height)
            {
                position.x += 30;
                position.y += (Screen.height - position.yMax);
            }
            if (position.xMax > Screen.width)
            {
                position.x += (Screen.width - position.xMax);
            }
            GUI.Label(position, GUI.tooltip, style);
            GUI.matrix = currentMatrix;
        }
    }

    // Tooltips used the 11 px font at every resolution and were hard to read. On desktop the text
    // follows the screen height: the closest of the fixed-size bold fonts (drawn 1:1, so sharp) to
    // 15 px * (height / 1080), i.e. 15 px at 1080p, 17 px at 1440p, 28 px at 4K. The bubble's edge and
    // spacing grow with it.
    private static GUIStyle _tooltipStyle;
    private static int _tooltipStyleHeight;
    private static float _tooltipScale = 1f;

    private static GUIStyle TooltipStyle
    {
        get
        {
            if (ApplicationDataManager.IsMobile || BlueStonez.tooltip.font == null) { _tooltipScale = 1f; return BlueStonez.tooltip; }
            if (_tooltipStyle != null && _tooltipStyleHeight == Screen.height) return _tooltipStyle;

            _tooltipStyleHeight = Screen.height;
            float target = 15f * Mathf.Max(1f, Screen.height / 1080f);
            Font font = BlueStonez.tooltip.font;
            float bestDiff = float.MaxValue;
            foreach (GUIStyle s in new[] { BlueStonez.label_interparkbold_13pt, BlueStonez.label_interparkbold_16pt, BlueStonez.label_interparkbold_18pt, BlueStonez.label_interparkbold_32pt })
            {
                if (s == null || s.font == null) continue;
                float diff = Mathf.Abs(s.font.fontSize - target);
                if (diff < bestDiff) { bestDiff = diff; font = s.font; }
            }

            _tooltipScale = (float)font.fontSize / Mathf.Max(1, BlueStonez.tooltip.font.fontSize);
            float k = _tooltipScale;
            _tooltipStyle = new GUIStyle(BlueStonez.tooltip);
            _tooltipStyle.font = font;
            RectOffset o = BlueStonez.tooltip.overflow;
            _tooltipStyle.overflow = new RectOffset(Mathf.RoundToInt(o.left * k), Mathf.RoundToInt(o.right * k), Mathf.RoundToInt(o.top * k), Mathf.RoundToInt(o.bottom * k));
            return _tooltipStyle;
        }
    }
}