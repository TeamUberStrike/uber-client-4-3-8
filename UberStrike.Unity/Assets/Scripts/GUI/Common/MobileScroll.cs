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
///
/// Why this implementation (after two that were inert on device):
///  - v12 used raw <c>Input.GetTouch</c> + <c>GUIToScreenPoint</c> to build a screen-space hit rect. Under
///    the nested <c>GUI.BeginGroup</c>s + the <see cref="MobileMenuScale"/> matrix that coordinate mapping
///    didn't line up, so the hit-test never passed.
///  - v13 used <c>Event.current.delta</c> on <c>MouseDrag</c>. IMGUI only delivers MouseDrag reliably when a
///    control owns the mouse (hotControl), which generic OnGUI code doesn't grab — so the delta was always 0.
///  - This version hit-tests on <c>MouseDown</c> with <c>Event.current.mousePosition</c>, which IMGUI ALREADY
///    reports in the current group + matrix space (the exact space as the viewport rect — no manual mapping),
///    then reads the per-frame movement from raw <c>Input.GetTouch().deltaPosition</c> (frame-global, always
///    valid) on the Repaint pass, converting screen pixels to GUI-logical units via <c>1 / Scale</c>.
///
/// No-op on desktop (gated through <see cref="MobileMenuScale.Active"/>). "Content follows finger".
/// </summary>
public static class MobileScroll
{
    // Flip on to draw a small live readout (touch count / active view / scrollPos / delta) for diagnosing
    // on device. Leave off in normal builds.
    public static bool DebugOverlay = true;

    private static readonly Dictionary<int, bool> _dragging = new Dictionary<int, bool>();
    private static int _activeView = -1;     // debug: which view currently owns the drag
    private static float _lastScroll, _lastDelta; // debug

    public static Vector2 Drag(int viewId, Rect localViewRect, Vector2 scrollPos)
    {
        if (!MobileMenuScale.Active || Event.current == null) return scrollPos;

        bool active;
        _dragging.TryGetValue(viewId, out active);

        switch (Event.current.type)
        {
            case EventType.MouseDown:
                // mousePosition is in the CURRENT group + matrix space — same as localViewRect.
                active = localViewRect.Contains(Event.current.mousePosition);
                _dragging[viewId] = active;
                if (active) _activeView = viewId;
                break;

            case EventType.MouseUp:
                _dragging[viewId] = false;
                if (_activeView == viewId) _activeView = -1;
                break;

            case EventType.Repaint:
                // Apply the frame's touch movement once (Repaint runs once per frame). Use raw touch delta
                // (reliable) rather than Event.current.delta (needs hotControl).
                if (active && Input.touchCount > 0)
                {
                    Touch t = Input.GetTouch(0);
                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    {
                        float s = Mathf.Max(0.0001f, MobileMenuScale.Scale);
                        // deltaPosition is y-up screen px; finger up (positive) reveals lower items =>
                        // scrollPos.y (GUI y-down) increases. Convert screen px -> GUI-logical via /Scale.
                        float dy = t.deltaPosition.y / s;
                        scrollPos.y = Mathf.Max(0f, scrollPos.y + dy);
                        _lastDelta = dy;
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        _dragging[viewId] = false;
                        if (_activeView == viewId) _activeView = -1;
                    }
                }
                break;
        }

        _lastScroll = scrollPos.y;
        return scrollPos;
    }

    /// <summary>Call once from a menu OnGUI (already inside MobileMenuScale) to show the debug readout.</summary>
    public static void DrawDebug()
    {
        if (!DebugOverlay || !MobileMenuScale.Active || Event.current == null || Event.current.type != EventType.Repaint)
            return;
        string msg = string.Format("[scroll] touches={0} view={1} pos={2:N0} d={3:N1}",
            Input.touchCount, _activeView, _lastScroll, _lastDelta);
        Color prev = GUI.color;
        GUI.color = new Color(0f, 1f, 0.4f, 0.9f);
        GUI.Label(new Rect(6, MobileMenuScale.VirtualHeight - 18, MobileMenuScale.VirtualWidth - 12, 18), msg);
        GUI.color = prev;
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
    public const int TrainingMaps = 6;
}
