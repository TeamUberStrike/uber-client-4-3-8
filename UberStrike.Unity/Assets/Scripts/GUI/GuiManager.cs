using UnityEngine;

public static class GuiManager
{
    public static void DrawTooltip()
    {
        if (!string.IsNullOrEmpty(GUI.tooltip))
        {
            Matrix4x4 currentMatrix = GUI.matrix;
            // reset scaling, so GUI tooltip stay same size

            GUI.matrix = Matrix4x4.identity;
            Vector2 tipSize = BlueStonez.tooltip.CalcSize(new GUIContent(GUI.tooltip));
            Rect position = new Rect(Mathf.Clamp(Event.current.mousePosition.x, 14, Screen.width - (tipSize.x + 14)), Event.current.mousePosition.y + 24, tipSize.x, tipSize.y + 16);

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
            GUI.Label(position, GUI.tooltip, BlueStonez.tooltip);
            GUI.matrix = currentMatrix;
        }
    }
}