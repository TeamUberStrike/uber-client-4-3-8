using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds finger drag-to-scroll to IMGUI <c>GUI.BeginScrollView</c> lists on mobile (the built-in scroll
/// view only scrolls via its thin scrollbar, which is very hard to grab with a thumb).
///
/// Usage: right before a scroll view, pipe its scroll position through <see cref="Drag"/> with the SAME
/// viewport rect you pass to BeginScrollView and a unique id per scroll view, e.g.:
/// <code>
///   _scroll = MobileScroll.Drag(ScrollId.ServerList, viewRect, _scroll);
///   _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);
/// </code>
/// No-op on desktop (gated through <see cref="MobileMenuScale.Active"/>). Drag direction is "content
/// follows finger" (swipe up reveals lower items).
/// </summary>
public static class MobileScroll
{
    private class DragState { public int fingerId = -1; }
    private static readonly Dictionary<int, DragState> _states = new Dictionary<int, DragState>();

    public static Vector2 Drag(int viewId, Rect localViewRect, Vector2 scrollPos)
    {
        if (!MobileMenuScale.Active) return scrollPos;
        // Apply once per frame (OnGUI runs Layout+Repaint); Repaint sees the final transform stack.
        if (Event.current == null || Event.current.type != EventType.Repaint) return scrollPos;

        DragState st;
        if (!_states.TryGetValue(viewId, out st)) { st = new DragState(); _states[viewId] = st; }

        if (Input.touchCount == 0) { st.fingerId = -1; return scrollPos; }

        // Map the viewport (local GUI space, inside the current group + GUI.matrix) to screen pixels.
        float s = MobileMenuScale.Scale;
        Vector2 tl = GUIUtility.GUIToScreenPoint(new Vector2(localViewRect.x, localViewRect.y));
        Rect screenRect = new Rect(tl.x, tl.y, localViewRect.width * s, localViewRect.height * s);

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            // GUIToScreenPoint is y-down from the top-left; touch.position is y-up from the bottom-left.
            Vector2 sp = new Vector2(t.position.x, Screen.height - t.position.y);

            if (t.phase == TouchPhase.Began)
            {
                if (st.fingerId == -1 && screenRect.Contains(sp)) st.fingerId = t.fingerId;
            }
            else if (t.fingerId == st.fingerId)
            {
                if (t.phase == TouchPhase.Moved)
                {
                    // Finger up (deltaPosition.y > 0 in y-up) reveals lower items => scrollPos.y increases.
                    scrollPos.y = Mathf.Max(0f, scrollPos.y + t.deltaPosition.y / s);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    st.fingerId = -1;
                }
            }
        }

        return scrollPos;
    }
}

/// <summary>Stable ids for each drag-scrollable list (avoids magic numbers at call sites).</summary>
public static class ScrollId
{
    public const int ServerList = 1;
    public const int ServerHelp = 2;
    public const int ShopLab = 3;
    public const int LoadoutWeapons = 4;
    public const int LoadoutGear = 5;
}
