using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class GUITools
{
    #region Screen Size

    private static int _screenX = 1;
    private static int _screenY = 1;
    private static int _screenHalfX = 1;
    private static int _screenHalfY = 1;

    private static float _aspectRatio = 1;
    public static float AspectRatio
    {
        get { return _aspectRatio; }
    }

    public static float SinusPulse
    {
        get { return (Mathf.Sin(Time.time * 2) + 1.3f) * 0.5f; }
    }

    public static float FastSinusPulse
    {
        get { return (Mathf.Sin(Time.time * 5) + 1.3f) * 0.5f; }
    }

    public static IEnumerator StartScreenSizeListener(float s)
    {
        UpdateScreenSize();

        yield return new WaitForEndOfFrame();

        while (true)
        {
            UpdateScreenSize();

            yield return new WaitForSeconds(s);
        }
    }

    public static void UpdateScreenSize()
    {
        if (ScreenWidth != Screen.width)
        {
            ScreenWidth = Screen.width;
            _aspectRatio = ScreenWidth / ScreenHeight;
        }

        if (ScreenHeight != Screen.height)
        {
            ScreenHeight = Screen.height;
            _aspectRatio = ScreenWidth / ScreenHeight;
        }
    }

    public static int ScreenHalfWidth
    {
        get { return _screenHalfX; }
    }

    public static int ScreenHalfHeight
    {
        get { return _screenHalfY; }
    }

    public static int ScreenWidth
    {
        get { return _screenX; }
        private set
        {
            _screenX = Mathf.Max(value, 1);
            _screenHalfX = _screenX >> 1;
        }
    }

    public static int ScreenHeight
    {
        get { return _screenY; }
        private set
        {
            _screenY = Mathf.Max(value, 1);
            _screenHalfY = _screenY >> 1;
        }
    }

    public static bool IsScreenResolutionChanged()
    {
        if (GUITools.ScreenWidth != Screen.width ||
            GUITools.ScreenHeight != Screen.height)
        {
            return true;
        }
        return false;
    }

    #endregion

    #region Save Click

    private static float _lastClick = 0;
    private static float _lastRepeatClick = 0;
    private static float _repeatButtonPressed = 0;

    public static bool RepeatClick(float vel)
    {
        if (Mathf.Abs(Time.time - _lastRepeatClick - Time.deltaTime) < 0.0001f)
        {
            //CmuneDebug.Log(string.Format("## Second click: {0} + {1} = {2} ({3} | {4})", _lastRepeatClick, Time.deltaTime, Time.time, (_lastRepeatClick + Time.deltaTime), (Time.time - _lastRepeatClick - Time.deltaTime)));
            _repeatButtonPressed += Time.deltaTime;
        }
        else
        {
            //CmuneDebug.Log(string.Format("First click: {0} + {1} = {2} ({3} | {4})", _lastRepeatClick, Time.deltaTime, Time.time, (_lastRepeatClick + Time.deltaTime), (Time.time - _lastRepeatClick - Time.deltaTime)));
            _repeatButtonPressed = 0;
        }

        _lastRepeatClick = Time.time;

        return (_repeatButtonPressed == 0) ? true : (_repeatButtonPressed > 0.5f ? SaveClickIn(vel) : false);
    }

    public static float LastClick
    {
        get { return _lastClick; }
    }

    public static bool SaveClick
    {
        get { return (_lastClick + 0.5F) < Time.time; }
    }

    public static bool SaveClickIn(float t)
    {
        return (_lastClick + t) < Time.time;
    }

    public static void ClickAndUse()
    {
        if (Event.current != null) Event.current.Use();
        _lastClick = Time.time;
    }

    public static void Clicked()
    {
        //if (Event.current != null) Event.current.Use();
        _lastClick = Time.time;
    }
    #endregion

    #region GUI enable state

    private static Stack<bool> _stateStack = new Stack<bool>();

    public static int CheckGUIState()
    {
        return _stateStack.Count;
    }

    public static void PushGUIState()
    {
        if (_stateStack.Count < 100)
            _stateStack.Push(GUI.enabled);
        else
            Debug.LogError("Check your calls of PushGUIState");
    }

    public static void PopGUIState()
    {
        GUI.enabled = _stateStack.Pop();
    }

    #endregion

    #region GUI color state

    private static Color _lastGuiColor;

    public static void BeginGUIColor(Color color)
    {
        _lastGuiColor = GUI.color;
        GUI.color = color;
    }

    public static void EndGUIColor()
    {
        GUI.color = _lastGuiColor;
    }

    #endregion

    public static void LabelShadow(Rect rect, string text, GUIStyle style, Color color)
    {
        GUI.color = new Color(0, 0, 0, color.a * 0.5f);
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
        GUI.color = color;
        GUI.Label(rect, text, style);
        GUI.color = Color.white;
    }

    public static bool Button(Rect rect, GUIContent content, GUIStyle style)
    {
        return Button(rect, content, style, SoundEffectType.UIButtonClick);
    }

    public static bool Button(Rect rect, GUIContent content, GUIStyle style, SoundEffectType soundEffect)
    {
        if (GUI.Button(rect, content, style))
        {
            SfxManager.Play2dAudioClip(soundEffect);
            return true;
        }

        return false;
    }

    #region ScrollView

    // Finger scrolling (mobile only; PC keeps the scrollbar and mouse wheel).
    // Driven by IMGUI pointer events, so the menu scale, safe-area shift and enclosing groups need
    // no conversion, and lists under a popup (GuiLockController) do not move. A press stays with the
    // control under it until it moves TouchScrollSlopMm vertically; then the list takes over (the
    // control is released without a click), follows the finger and glides on release.

    private static readonly int _touchScrollHash = "GUIToolsTouchScroll".GetHashCode();

    // Vertical finger travel (mm) before a press becomes a scroll.
    private const float TouchScrollSlopMm = 2.5f;
    // Share of the glide speed left after one second.
    private const float TouchScrollGlideKeptPerSecond = 0.02f;
    // Glide start speed, as a share of the finger's speed.
    private const float TouchScrollGlideStart = 0.8f;
    // Glide speed limits, GUI units per second.
    private const float TouchScrollMinGlideSpeed = 20f;
    private const float TouchScrollMaxGlideSpeed = 2500f;
    // A tap on a list gliding faster than this only stops it.
    private const float TouchScrollTapStopSpeed = 80f;
    // No glide if the finger rested this long (s) before lifting.
    private const float TouchScrollRestTime = 0.08f;

    private static int _touchScrollId;              // control id of the list that owns the press or glide; 0 = none
    private static int _touchScrollSeenFrame;       // last frame that list was drawn
    private static bool _touchScrollPressed;        // the pointer is down on the list
    private static bool _touchScrollDragging;       // the press has become a scroll
    private static Vector2 _touchScrollStartMouse;  // pointer position the scroll is measured from
    private static float _touchScrollStartY;        // scroll position at that point
    private static float _touchScrollLastY;
    private static float _touchScrollLastTime;
    private static float _touchScrollLastMoveTime;
    private static float _touchScrollVelocity;      // GUI units per second, positive = further down the list
    private static int _touchScrollFingerId = -1;   // finger that drags the list when it is not the pointer; -1 = none

    // See IsHoverSuppressedByTouchScroll.
    private static bool _touchScrollHoverStale;
    private static Vector3 _touchScrollStalePointer;

    /// <summary>
    /// Set around a BeginScrollView call to let a second finger drag that list (the scoreboard, shown
    /// only while its button is held). Menu lists leave it off.
    /// </summary>
    public static bool AllowSecondFingerScroll;

    /// <summary>Ends any finger scroll, e.g. when a row in the list takes the press (item drag).</summary>
    public static void CancelTouchScroll()
    {
        if (_touchScrollDragging)
            MarkTouchScrollHoverStale();

        _touchScrollId = 0;
        _touchScrollPressed = false;
        _touchScrollDragging = false;
        _touchScrollVelocity = 0f;
        _touchScrollFingerId = -1;
    }

    /// <summary>
    /// True during a finger scroll or glide and until the next press: the touch pointer is stale and
    /// rows slide under it, so hover effects should stay off. Always false on PC.
    /// </summary>
    public static bool IsHoverSuppressedByTouchScroll
    {
        get
        {
            ExpireTouchScroll();

            if (_touchScrollDragging || (_touchScrollId != 0 && !_touchScrollPressed && _touchScrollVelocity != 0f))
                return true;

            // A new press or pointer move ends it.
            if (_touchScrollHoverStale && (Input.GetMouseButton(0) || Input.mousePosition != _touchScrollStalePointer))
                _touchScrollHoverStale = false;

            return _touchScrollHoverStale;
        }
    }

    private static void MarkTouchScrollHoverStale()
    {
        _touchScrollHoverStale = true;
        _touchScrollStalePointer = Input.mousePosition;
    }

    // Drops the gesture when its list is no longer drawn.
    private static void ExpireTouchScroll()
    {
        if (_touchScrollId != 0 && Time.frameCount > _touchScrollSeenFrame + 2)
            CancelTouchScroll();
    }

    // Ends the press; glides on if the list was being dragged.
    private static void ReleaseTouchScroll(float now)
    {
        _touchScrollPressed = false;

        if (!_touchScrollDragging)
        {
            // Plain tap: the controls handled it.
            CancelTouchScroll();
            return;
        }

        MarkTouchScrollHoverStale();
        _touchScrollDragging = false;

        if (now - _touchScrollLastMoveTime > TouchScrollRestTime)
            _touchScrollVelocity = 0f;

        _touchScrollVelocity = Mathf.Clamp(_touchScrollVelocity * TouchScrollGlideStart, -TouchScrollMaxGlideSpeed, TouchScrollMaxGlideSpeed);
        _touchScrollLastTime = now;

        if (Mathf.Abs(_touchScrollVelocity) < TouchScrollMinGlideSpeed)
            CancelTouchScroll();
    }

    // Records the drag position and the speed for the glide.
    private static void TrackTouchScroll(float y, float now)
    {
        if (y != _touchScrollLastY)
            _touchScrollLastMoveTime = now;

        // Near-simultaneous events are measured together with the next one.
        float dt = now - _touchScrollLastTime;
        if (dt > 0.0001f)
        {
            _touchScrollVelocity = Mathf.Lerp(_touchScrollVelocity, (y - _touchScrollLastY) / dt, Mathf.Clamp01(dt * 15f));
            _touchScrollLastTime = now;
            _touchScrollLastY = y;
        }
    }

    // A raw touch position (screen pixels, origin bottom left) in top-level GUI units.
    private static Vector2 TouchToTopLevel(Vector2 touchPosition)
    {
        Vector3 pixels = new Vector3(touchPosition.x, Screen.height - touchPosition.y, 0f);
        Matrix4x4 matrix = GUI.matrix;
        return matrix.isIdentity ? pixels : matrix.inverse.MultiplyPoint3x4(pixels);
    }

    // Second-finger drag for lists with AllowSecondFingerScroll, read from raw touches (it gets no
    // pointer events). Runs on Repaint; returns true while that finger owns the list.
    private static bool ScrollWithSecondFinger(int id, bool owner, Rect position, ref Vector2 scrollPosition, float max, float now)
    {
        Touch[] touches = TouchSource.Touches;

        if (_touchScrollFingerId >= 0)
        {
            if (!owner)
                return false;

            for (int i = 0; i < touches.Length; i++)
            {
                Touch touch = touches[i];
                if (touch.fingerId != _touchScrollFingerId)
                    continue;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    break;

                float y = Mathf.Clamp(_touchScrollStartY - (TouchToTopLevel(touch.position).y - _touchScrollStartMouse.y), 0f, max);
                TrackTouchScroll(y, now);
                scrollPosition.y = y;
                return true;
            }

            // Finger lifted: glide.
            _touchScrollFingerId = -1;
            ReleaseTouchScroll(now);
            return true;
        }

        if (!AllowSecondFingerScroll || touches.Length < 2 || max <= 0f || !GUI.enabled || DragAndDrop.Instance.IsDragging)
            return false;

        for (int i = 0; i < touches.Length; i++)
        {
            Touch touch = touches[i];
            if (touch.phase != TouchPhase.Began)
                continue;

            // Touch position in this list's space.
            Vector2 topLevel = TouchToTopLevel(touch.position);
            Rect origin = ToTopLevel(new Rect(0f, 0f, 0f, 0f));
            if (!position.Contains(new Vector2(topLevel.x - origin.x, topLevel.y - origin.y)))
                continue;

            _touchScrollId = id;
            _touchScrollSeenFrame = Time.frameCount;
            _touchScrollFingerId = touch.fingerId;
            _touchScrollPressed = true;
            _touchScrollDragging = true;
            _touchScrollStartMouse = topLevel;
            _touchScrollStartY = Mathf.Clamp(scrollPosition.y, 0f, max);
            _touchScrollLastY = _touchScrollStartY;
            _touchScrollLastTime = now;
            _touchScrollLastMoveTime = now;
            _touchScrollVelocity = 0f;
            return true;
        }

        return false;
    }

    private static Vector2 HandleTouchScroll(int id, Rect position, Vector2 scrollPosition, Rect contentRect)
    {
        ExpireTouchScroll();

        bool owner = _touchScrollId == id;
        if (owner)
            _touchScrollSeenFrame = Time.frameCount;

        // Vertical only.
        float max = Mathf.Max(0f, contentRect.height - position.height);
        float now = Time.realtimeSinceStartup;
        Event e = Event.current;

        // While a second finger drags, the pointer events belong to the first one.
        if (_touchScrollFingerId >= 0 && e.type != EventType.Repaint)
            return scrollPosition;

        switch (e.type)
        {
            case EventType.MouseDown:
            {
                // Leave the scrollbar strip alone.
                GUIStyle bar = GUI.skin.verticalScrollbar;
                Rect area = position;
                area.width -= bar.fixedWidth + bar.margin.left;

                if (e.button == 0 && GUI.enabled && max > 0f && area.Contains(e.mousePosition) && !DragAndDrop.Instance.IsDragging)
                {
                    bool stopGlide = owner && !_touchScrollPressed && Mathf.Abs(_touchScrollVelocity) > TouchScrollTapStopSpeed;

                    _touchScrollId = id;
                    _touchScrollSeenFrame = Time.frameCount;
                    _touchScrollPressed = true;
                    _touchScrollDragging = stopGlide;
                    _touchScrollStartMouse = e.mousePosition;
                    _touchScrollStartY = Mathf.Clamp(scrollPosition.y, 0f, max);
                    _touchScrollLastY = _touchScrollStartY;
                    _touchScrollLastTime = now;
                    _touchScrollLastMoveTime = now;
                    _touchScrollVelocity = 0f;

                    // Tapping a gliding list stops it without pressing the row.
                    if (stopGlide)
                        e.Use();
                }
                else if (owner)
                {
                    // A press anywhere else ends the glide.
                    CancelTouchScroll();
                }
                break;
            }

            case EventType.MouseDrag:
            {
                if (!owner || !_touchScrollPressed)
                    break;

                // Second finger (the pointer jumps) or an item drag: give up.
                if (Input.touchCount > 1 || DragAndDrop.Instance.IsDragging)
                {
                    CancelTouchScroll();
                    break;
                }

                Vector2 moved = e.mousePosition - _touchScrollStartMouse;

                if (!_touchScrollDragging)
                {
                    float slop = TouchScrollSlopMm * MobileMenuScale.PixelsPerMm / MobileMenuScale.Scale;

                    if (Mathf.Abs(moved.y) > slop && Mathf.Abs(moved.y) >= Mathf.Abs(moved.x))
                    {
                        // Take over: release the pressed control without a click; re-measure so the list doesn't jump.
                        _touchScrollDragging = true;
                        GUIUtility.hotControl = 0;
                        _touchScrollStartMouse = e.mousePosition;
                        _touchScrollStartY = Mathf.Clamp(scrollPosition.y, 0f, max);
                        _touchScrollLastY = _touchScrollStartY;
                        _touchScrollLastTime = now;
                        _touchScrollLastMoveTime = now;
                        moved = Vector2.zero;
                    }
                    else
                    {
                        // Sideways: leave it to the control (slider, item drag).
                        if (Mathf.Abs(moved.x) > slop)
                            CancelTouchScroll();
                        break;
                    }
                }

                float y = Mathf.Clamp(_touchScrollStartY - moved.y, 0f, max);
                TrackTouchScroll(y, now);
                scrollPosition.y = y;
                e.Use();
                break;
            }

            case EventType.MouseUp:
            {
                if (!owner || !_touchScrollPressed)
                    break;

                // The release after a scroll is not a click.
                if (_touchScrollDragging)
                    e.Use();

                ReleaseTouchScroll(now);
                break;
            }

            case EventType.Repaint:
            {
                if (ScrollWithSecondFinger(id, owner, position, ref scrollPosition, max, now))
                    break;

                if (!owner)
                    break;

                if (_touchScrollPressed)
                {
                    // The release was lost (finger lifted elsewhere).
                    if (!Input.GetMouseButton(0) && Input.touchCount == 0)
                        ReleaseTouchScroll(now);
                    break;
                }

                if (_touchScrollVelocity == 0f)
                    break;

                // Glide and slow down; stop at either end.
                float dt = Mathf.Min(now - _touchScrollLastTime, 0.1f);
                _touchScrollLastTime = now;

                float y = Mathf.Clamp(scrollPosition.y, 0f, max) + _touchScrollVelocity * dt;
                if (y <= 0f || y >= max)
                {
                    y = Mathf.Clamp(y, 0f, max);
                    CancelTouchScroll();
                }
                else
                {
                    _touchScrollVelocity *= Mathf.Pow(TouchScrollGlideKeptPerSecond, dt);
                    if (Mathf.Abs(_touchScrollVelocity) < TouchScrollMinGlideSpeed)
                        CancelTouchScroll();
                }

                scrollPosition.y = y;
                break;
            }
        }

        return scrollPosition;
    }

    public static Rect ToGlobal(Rect rect)
    {
        Vector2 p = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
        return new Rect(p.x, p.y, rect.width, rect.height);
    }

    /// <summary>
    /// A rect inside groups / scroll views, moved to the top level of the current OnGUI, in the same
    /// GUI units: where to draw something at the top level so that it lines up with the rect.
    ///
    /// ToGlobal is not that once GUI.matrix scales the menu (mobile): GUIToScreenPoint transforms
    /// only the local point and then adds the offset of the enclosing groups unscaled, so its
    /// result is neither screen pixels nor GUI units. That offset is recovered here by taking the
    /// transformed local point away again. Without a matrix (PC) the result equals ToGlobal.
    /// </summary>
    public static Rect ToTopLevel(Rect rect)
    {
        Vector2 local = new Vector2(rect.x, rect.y);
        Vector2 p = GUIUtility.GUIToScreenPoint(local);

        Matrix4x4 matrix = GUI.matrix;
        if (!matrix.isIdentity)
        {
            Vector2 transformed = matrix.MultiplyPoint3x4(new Vector3(local.x, local.y, 0f));
            p = local + (p - transformed);
        }

        return new Rect(p.x, p.y, rect.width, rect.height);
    }

    public static bool Label(Rect rect, Texture2D image, GUIStyle style)
    {
        GUI.Label(rect, image, style);
        return rect.Contains(Event.current.mousePosition);
    }

    public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect contentRect, bool showHorizontalScrollbar, bool showVerticalScrollbar, GUIStyle hStyle, GUIStyle vStyle, bool allowDrag=true)
    {
        // Finger scrolling, mobile only.
        if (allowDrag && MobileMenuScale.Active)
        {
            int id = GUIUtility.GetControlID(_touchScrollHash, FocusType.Passive);
            scrollPosition = HandleTouchScroll(id, position, scrollPosition, contentRect);
        }

        return GUI.BeginScrollView(position, scrollPosition, contentRect);
    }

    public static Vector2 BeginScrollView(
        Rect position,
        Vector2 scrollPosition,
        Rect contentRect,
        bool useHorizontal = false,
        bool useVertical = false,
        bool allowDrag = true)
    {
        return BeginScrollView(position, scrollPosition, contentRect, useHorizontal, useVertical, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, allowDrag);
    }

    public static Vector2 BeginScrollView(
    Rect position,
    Vector2 scrollPosition,
    Rect contentRect,
    GUIStyle hStyle, 
    GUIStyle vStyle)
    {
        return BeginScrollView(position, scrollPosition, contentRect, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);
    }

    public static void EndScrollView()
    {
        GUI.EndScrollView();
    }

    #endregion

    #region PopupList

    // determines if a list should be started, or if it has been clicked off
    public static bool BeginList(ref bool showList, Rect listRect, Rect buttonRect)
    {
        // if false, ignore
        if (!showList) return false;

        // if we receive a click outside the box, close the window
        if (Input.GetMouseButtonUp(0) && !listRect.ContainsTouch(Input.mousePosition) && !buttonRect.ContainsTouch(Input.mousePosition))
        {
            showList = false;
        }

        if (TouchSource.TouchCount > 0)
        {
            foreach (Touch touch in TouchSource.Touches)
            {
                if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) 
                    && !listRect.ContainsTouch(touch.position)
                    && !buttonRect.ContainsTouch(touch.position))
                {
                    showList = false;
                }
            }
        }

        return showList;
    }

    #endregion
}