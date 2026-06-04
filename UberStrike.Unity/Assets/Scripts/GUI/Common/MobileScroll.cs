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
    // Per-view: was the drag that's currently in progress started inside this view's viewport?
    // Tracked so a drag that begins on the list keeps scrolling it even after the finger leaves the
    // viewport bounds, and so a drag starting elsewhere never scrolls this list.
    private static readonly Dictionary<int, bool> _dragging = new Dictionary<int, bool>();

    public static Vector2 Drag(int viewId, Rect localViewRect, Vector2 scrollPos)
    {
        if (!MobileMenuScale.Active || Event.current == null) return scrollPos;

        // IMGUI synthesizes mouse events from touch, so a one-finger drag arrives as MouseDown ->
        // MouseDrag* -> MouseUp. Event.current.mousePosition and .delta are already in the CURRENT GUI
        // coordinate space (inside the active group + GUI.matrix scale) — the SAME space as localViewRect —
        // so no GUIToScreenPoint / manual scale conversion is needed (that was why the old version was inert).
        // This runs BEFORE BeginScrollView at each call site, so it reads the event before any item button
        // can consume it.
        switch (Event.current.type)
        {
            case EventType.MouseDown:
                _dragging[viewId] = localViewRect.Contains(Event.current.mousePosition);
                break;

            case EventType.MouseDrag:
                bool active;
                if (!_dragging.TryGetValue(viewId, out active))
                {
                    // No MouseDown was seen (e.g. drag entered mid-gesture) — fall back to "is the finger
                    // over the list right now?".
                    active = localViewRect.Contains(Event.current.mousePosition);
                    _dragging[viewId] = active;
                }
                if (active)
                {
                    // GUI space is y-down: dragging the finger UP gives a negative delta.y and should reveal
                    // lower items (scrollPos.y increases) => subtract the delta. Content follows the finger.
                    scrollPos.y = Mathf.Max(0f, scrollPos.y - Event.current.delta.y);
                }
                break;

            case EventType.MouseUp:
                _dragging[viewId] = false;
                break;
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
