using UnityEngine;

public static partial class GUITools
{
    static int HoverButtonHash = "Button".GetHashCode();

    public static EventType HoverButton(Rect position, GUIContent content, GUIStyle style)
    {
        int controlID = GUIUtility.GetControlID(HoverButtonHash, FocusType.Passive);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (position.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                    return EventType.MouseDown;
                }
                break;
            case EventType.MouseUp:
                //if we are the hot control, then we grab the mouse up too
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0; //The hot control resets the GUI.hotcontrol regardless of where the mouseup is located
                    if (position.Contains(Event.current.mousePosition))
                    {
                        Event.current.Use();
                        return EventType.MouseUp;
                    }
                } //If we are not the hot control, check if somethings in the drag cache and see if we can accept it
                else if (position.Contains(Event.current.mousePosition))
                {
                    //The mouse was released over another hoverbutton, lets trigger the Drop action (the hotcontrol is cleared by the owner)
                    return EventType.DragExited;
                }
                return EventType.Ignore;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    Event.current.Use();
                    return EventType.MouseDrag;
                }
                else
                    return EventType.Ignore;
            case EventType.Repaint:
                style.Draw(position, content, controlID);
                if (position.Contains(Event.current.mousePosition))
                    return EventType.MouseMove;
                else
                    return EventType.Repaint;
        }
        if (position.Contains(Event.current.mousePosition))
            return EventType.MouseMove;
        else
            return EventType.Ignore;
    }

    public static string PasswordField(Rect mPosition, string mPassword)
    {
        string strPasswordMask;
        if (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown)
        {
            strPasswordMask = "";
            for (int i = 0; i < mPassword.Length; i++)
            {
                strPasswordMask += "*";
            }
        }
        else
        {
            strPasswordMask = mPassword;
        }
        GUI.changed = false;
        strPasswordMask = GUI.TextField(mPosition, strPasswordMask, 20);
        if (GUI.changed)
        {
            mPassword = strPasswordMask;
        }
        return mPassword;
    }

    public static string PasswordField(string mPassword)
    {
        string strPasswordMask;
        if (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown)
        {
            strPasswordMask = "";
            for (int i = 0; i < mPassword.Length; i++)
            {
                strPasswordMask += "*";
            }
        }
        else
        {
            strPasswordMask = mPassword;
        }
        GUI.changed = false;
        strPasswordMask = GUILayout.TextField(strPasswordMask, 24, GUILayout.Height(30));
        if (GUI.changed)
        {
            mPassword = strPasswordMask;
        }
        return mPassword;
    }

    public static Vector2 DoScrollArea(Rect position, GUIContent[] buttons, int buttonHeight, Vector2 listScroller)
    {
        float height = 0; int index = 0;
        if (buttons.Length > 0)
            height = ((buttons.Length - 1) * buttonHeight);
        listScroller = GUI.BeginScrollView(position, listScroller, new Rect(0, 0, position.width - 20, height + buttonHeight));
        for (index = 0; index < buttons.Length; index++)
            if (((index + 1) * buttonHeight) > listScroller.y) break;
        for (; index < buttons.Length && (index * buttonHeight) < listScroller.y + position.height; index++)
            GUI.Button(new Rect(0, index * buttonHeight, position.width - 16, buttonHeight), buttons[index]);
        GUI.EndScrollView();
        return listScroller;
    }

    public static void OutlineLabel(Rect position, string text)
    {
        OutlineLabel(position, text, "SuperBigTitle", 1);
    }

    public static void OutlineLabel(Rect position, string text, GUIStyle style)
    {
        OutlineLabel(position, text, style, 1);
    }

    public static void OutlineLabel(Rect position, string text, GUIStyle style, Color c)
    {
        OutlineLabel(position, text, style, 1, Color.white, c);
    }

    public static void OutlineLabel(Rect position, string text, GUIStyle style, int Offset)
    {
        OutlineLabel(position, text, style, Offset, Color.white, Color.black);
    }

    public static void OutlineLabel(Rect position, string text, GUIStyle style, int Offset, Color textColor, Color outlineColor)
    {
        Color tcolor = style.normal.textColor;
        style.normal.textColor = outlineColor;

        if (Offset > 0)
        {
            GUI.Label(new Rect(position.x, position.y + Offset, position.width, position.height), text, style);
            GUI.Label(new Rect(position.x - Offset, position.y, position.width, position.height), text, style);
            GUI.Label(new Rect(position.x + Offset, position.y, position.width, position.height), text, style);
            GUI.Label(new Rect(position.x, position.y - Offset, position.width, position.height), text, style);
            if (Offset > 1)
            {
                GUI.Label(new Rect(position.x - Offset, position.y - Offset, position.width, position.height), text, style);
                GUI.Label(new Rect(position.x - Offset, position.y + Offset, position.width, position.height), text, style);
                GUI.Label(new Rect(position.x + Offset, position.y + Offset, position.width, position.height), text, style);
                GUI.Label(new Rect(position.x + Offset, position.y - Offset, position.width, position.height), text, style);
            }
        }
        else
            GUI.Label(new Rect(position.x, position.y + 1, position.width, position.height), text, style);
        style.normal.textColor = tcolor;
        GUI.color = textColor;
        GUI.Label(position, text, style);
        GUI.color = Color.white;
    }

    public static void ProgressBar(Rect position, string text, float percentage, Color barColor, int barWidth, string value)
    {
        GUI.BeginGroup(position);
        {
            GUI.Label(new Rect(0, 0, position.width - (barWidth + 4) - 2 - 30, 14), text, BlueStonez.label_interparkbold_11pt_right);
            GUI.Label(new Rect(position.width - barWidth - 30, 1, barWidth, 12), GUIContent.none, BlueStonez.progressbar_background);
            GUI.color = barColor;
            GUI.Label(new Rect((position.width - barWidth - 30) + 2, 3, (float)(barWidth - 4) * Mathf.Clamp01(percentage), 8), string.Empty, BlueStonez.progressbar_thumb);
            GUI.color = Color.white;
            if (!string.IsNullOrEmpty(value))
                GUI.Label(new Rect(position.width - 25, 0, 30, 14), value, BlueStonez.label_interparkmed_10pt_left);
        }
        GUI.EndGroup();
    }

    public static void DrawWarmupBar(Rect position, float value, float maxValue)
    {
        GUI.BeginGroup(position);
        {
            GUI.Box(new Rect(0, 0, position.width, position.height), GUIContent.none, StormFront.ProgressBackground);

            float width = (position.width - 8) * value / maxValue;
            GUI.Box(new Rect(4, 4, width, position.height - 8), GUIContent.none, StormFront.ProgressForeground);

            float thumbWidth = position.height * 0.5f;
            GUI.Box(new Rect(4 + width - thumbWidth * 0.5f, 2, thumbWidth, position.height - 4), GUIContent.none, StormFront.ProgressThumb);
        }
        GUI.EndGroup();
    }
}