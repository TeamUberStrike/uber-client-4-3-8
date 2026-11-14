#if UNITY_EDITOR
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class EditorTouchSimulator : MonoBehaviour
{
    private static EditorTouchSimulator _instance;
    private static readonly Touch[] Empty = new Touch[0];
    private static Touch[] _touches = Empty;

    public static bool Active
    {
        get { return _instance != null && _instance._active; }
    }

    public static Touch[] Touches
    {
        get { return _touches; }
    }

    private bool _active;
    private bool _primaryDown;
    private bool _secondaryDown;
    private bool _primaryEnded;
    private bool _secondaryEnded;
    private bool _primaryFrozen;
    private Vector2 _primaryPos;
    private Vector2 _primaryLastPos;
    private Vector2 _secondaryPos;
    private TouchPhase _primaryPhase;
    private TouchPhase _secondaryPhase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Application.isEditor || _instance != null) return;

        GameObject go = new GameObject("EditorTouchSimulator");
        go.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<EditorTouchSimulator>();
    }

    private void Update()
    {
        bool shouldRun = TouchInput.IsSupported && !MobileControlLayout.EditMode;

        if (!shouldRun || Input.touchCount > 0)
        {
            ResetState();
            return;
        }

        _active = true;

        bool left = Input.GetMouseButton(0);
        bool right = Input.GetMouseButton(1);
        Vector2 mouse = Input.mousePosition;

        if (right && !_secondaryDown)
        {
            _secondaryDown = true;
            _secondaryPos = mouse;
            _secondaryPhase = TouchPhase.Began;

            if (_primaryDown)
                _primaryFrozen = true;
        }
        else if (right)
        {
            _secondaryPhase = TouchPhase.Stationary;
        }
        else if (_secondaryDown)
        {
            _secondaryDown = false;
            _secondaryEnded = true;
            _secondaryPhase = TouchPhase.Ended;
        }

        if (left)
        {
            if (!_primaryDown)
            {
                _primaryDown = true;
                _primaryPos = mouse;
                _primaryLastPos = mouse;
                _primaryPhase = TouchPhase.Began;
            }
            else if (_primaryFrozen)
            {
                _primaryPhase = TouchPhase.Stationary;
            }
            else
            {
                _primaryLastPos = _primaryPos;
                _primaryPos = mouse;
                _primaryPhase = _primaryLastPos == _primaryPos ? TouchPhase.Stationary : TouchPhase.Moved;
            }
        }
        else if (_primaryDown)
        {
            _primaryDown = false;
            _primaryFrozen = false;
            _primaryEnded = true;
            _primaryPhase = TouchPhase.Ended;
        }

        bool primaryVisible = _primaryDown || _primaryEnded;
        bool secondaryVisible = _secondaryDown || _secondaryEnded;

        if (!primaryVisible && !secondaryVisible)
        {
            _touches = Empty;
            return;
        }

        Touch[] touches = new Touch[primaryVisible && secondaryVisible ? 2 : 1];
        int index = 0;

        if (primaryVisible)
        {
            Vector2 delta = _primaryPhase == TouchPhase.Moved ? _primaryPos - _primaryLastPos : Vector2.zero;
            touches[index++] = MakeTouch(0, _primaryPos, delta, _primaryPhase);
        }

        if (secondaryVisible)
        {
            touches[index] = MakeTouch(1, _secondaryPos, Vector2.zero, _secondaryPhase);
        }

        _touches = touches;
        _primaryEnded = false;
        _secondaryEnded = false;
    }

    private void ResetState()
    {
        _active = false;
        _primaryDown = false;
        _secondaryDown = false;
        _primaryEnded = false;
        _secondaryEnded = false;
        _primaryFrozen = false;
        _touches = Empty;
    }

    private static Touch MakeTouch(int fingerId, Vector2 position, Vector2 delta, TouchPhase phase)
    {
        Touch touch = new Touch();
        touch.fingerId = fingerId;
        touch.position = position;
        touch.rawPosition = position;
        touch.deltaPosition = delta;
        touch.deltaTime = Time.unscaledDeltaTime;
        touch.tapCount = 1;
        touch.phase = phase;
        touch.type = TouchType.Direct;
        touch.pressure = 1f;
        touch.maximumPossiblePressure = 1f;
        touch.radius = 1f;
        touch.radiusVariance = 0f;
        touch.altitudeAngle = 0f;
        touch.azimuthAngle = 0f;
        return touch;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        ResetState();
    }
}
#endif
