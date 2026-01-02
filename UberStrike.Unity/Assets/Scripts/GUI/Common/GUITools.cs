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

#if UNITY_ANDROID || UNITY_IPHONE || UNITY_EDITOR
    private static int touchFingerId = -1;
    private static Rect selectedRect;
    private static Vector2 scrollVelocity = Vector2.zero;
    private static float inertiaDuration = 1.0f; // how long should inertia last
    private static float timeTouchPhaseEnded; // when did the last touch end
    private static Queue<Vector2> lastTouchesPos = new Queue<Vector2>(); // stores a list of previous touch locations
    private static Queue<float> lastTouchesTime = new Queue<float>(); // stores a list of previous touch times
#endif
    public static Rect ToGlobal(Rect rect)
    {
        Vector2 p = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
        return new Rect(p.x, p.y, rect.width, rect.height);
    }

    public static bool Label(Rect rect, Texture2D image, GUIStyle style)
    {
        GUI.Label(rect, image, style);
        return rect.Contains(Event.current.mousePosition);
    }

    public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect contentRect, bool showHorizontalScrollbar, bool showVerticalScrollbar, GUIStyle hStyle, GUIStyle vStyle, bool allowDrag=true)
    {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_EDITOR
        if (Input.touchCount > 0 && allowDrag && !DragAndDrop.Instance.IsDragging)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began && touchFingerId == -1)
                {
                    // convert from touch to GUI coordinates (flip y)
                    Vector2 invertedTouchPos = new Vector2(touch.position.x, Screen.height - touch.position.y);
                    // did we hit this GUI ScrollView?
                    if (position.Contains(GUIUtility.ScreenToGUIPoint(invertedTouchPos)))
                    {
                        touchFingerId = touch.fingerId;
                        scrollVelocity = Vector2.zero;
                        selectedRect = contentRect; // workaround to identify selected item
                        break;
                    }
                }

                // only respond to movement on the right finger and content
                if (touch.fingerId == touchFingerId && selectedRect == contentRect)
                {
                    if (touch.phase == TouchPhase.Moved)
                    {
                        // respond to movement
                        scrollPosition += touch.deltaPosition;

                        // only store a specific amount of touches
                        if (lastTouchesPos.Count > 15)
                        {
                            lastTouchesPos.Dequeue();
                            lastTouchesTime.Dequeue();
                        }

                        // push touch onto list
                        lastTouchesPos.Enqueue(touch.deltaPosition);
                        lastTouchesTime.Enqueue(touch.deltaTime);
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        touchFingerId = -1;
                        timeTouchPhaseEnded = Time.time;

                        // total the movements and time for averaging
                        Vector2 totalVel = Vector2.zero;
                        float totalTime = 0.0f;
                        while (lastTouchesPos.Count > 0)
                        {
                            totalVel += lastTouchesPos.Dequeue();
                            totalTime += lastTouchesTime.Dequeue();
                        }

                        // calculate the final average velocity and clamp to reasonable speeds
                        if (totalVel != Vector2.zero && totalTime > 0)
                        {
                            scrollVelocity = totalVel / totalTime;
                            scrollVelocity.x = Mathf.Clamp(scrollVelocity.x, -200, 200);
                            scrollVelocity.y = Mathf.Clamp(scrollVelocity.y, -200, 200);
                        }
                    }
                }
            }
        }
        else
        {
            touchFingerId = -1;
        }

        if (touchFingerId == -1 && scrollVelocity != Vector2.zero && selectedRect == contentRect)
        {
                // slow down over time
                float t = (Time.time - timeTouchPhaseEnded) / inertiaDuration;
                Vector2 frameVelocity = Vector2.Lerp(scrollVelocity, Vector2.zero, t);
                
                scrollPosition += frameVelocity * Time.deltaTime;

                // after N seconds, we've stopped
                if (t >= inertiaDuration) scrollVelocity = Vector2.zero;
        }
#endif

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

        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
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