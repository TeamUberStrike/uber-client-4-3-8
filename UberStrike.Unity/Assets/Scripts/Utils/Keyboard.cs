using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Keyboard
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int UberGetDeviceType();

    [DllImport("__Internal")]
    private static extern void UberOpenTextKeyboard(string text, string gameObjectName, string methodName, bool masked, int maxLength);

    [DllImport("__Internal")]
    private static extern void UberCloseKeyboard();

    [DllImport("__Internal")]
    private static extern void UberAddKeyboardLayout(string id, string label, string rowsJson);

    [DllImport("__Internal")]
    private static extern void UberLoadKeyboardLayout(string url, string id, string label);
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    private static bool? _isMobileCache;
#endif

    public static bool IsMobile
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!_isMobileCache.HasValue)
                _isMobileCache = UberGetDeviceType() != 0;

            return _isMobileCache.Value;
#else
            return false;
#endif
        }
    }

    public static void Open(string text, GameObject receiver, string methodName, bool masked, int maxLength = -1)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (receiver != null && IsMobile)
            UberOpenTextKeyboard(text ?? string.Empty, receiver.name, methodName, masked, maxLength);
#endif
    }

    public static void Close()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UberCloseKeyboard();
#endif
#if UNITY_WEBGL
        if (_router != null)
        {
            _router.ActiveFieldId = 0;
            _router.HasResult = false;
            _router.Result = null;
            _router.OpenedValue = null;
        }
#endif
    }

#if UNITY_WEBGL
    private static WebGLKeyboardRouter _router;

    public static WebGLKeyboardRouter Router
    {
        get
        {
            if (_router == null)
            {
                var go = new GameObject("WebGLKeyboardRouter");
                go.hideFlags = HideFlags.HideInHierarchy;
                Object.DontDestroyOnLoad(go);
                _router = go.AddComponent<WebGLKeyboardRouter>();
            }

            return _router;
        }
    }
#endif

    // Drop-in replacement for GUI.TextField/GUI.PasswordField on mobile WebGL.
    // fieldId must be unique for the field (any other field with the same id
    // currently on screen would steal the result, so use distinct ids).
    public static string TextField(Rect rect, string value, GUIStyle style, int fieldId, bool password = false, int maxLength = -1)
    {
#if UNITY_WEBGL
        if (IsMobile)
        {
            WebGLKeyboardRouter router = Router;

            if (router.ActiveFieldId == fieldId && router.HasResult)
            {
                if (router.OpenedValue == null || router.OpenedValue == value)
                {
                    string result = router.Result ?? string.Empty;
                    if (maxLength > 0 && result.Length > maxLength)
                        result = result.Substring(0, maxLength);
                    value = result;
                }

                router.HasResult = false;
                router.ActiveFieldId = 0;
                router.OpenedValue = null;
            }

            string shown = password && !string.IsNullOrEmpty(value) ? "".PadLeft(value.Length, '*') : value;

            if (GUI.Button(rect, new GUIContent(shown), style ?? GUIStyle.none))
            {
                router.ActiveFieldId = fieldId;
                router.HasResult = false;
                router.OpenedValue = value;
                Open(value, router.gameObject, "OnWebGLFieldInput", password, maxLength);
            }

            return value;
        }
#endif
        if (password)
        {
            if (maxLength > 0)
                return GUI.PasswordField(rect, value, '*', maxLength, style ?? GUIStyle.none);

            return GUI.PasswordField(rect, value, '*', style ?? GUIStyle.none);
        }

        if (maxLength > 0)
            return GUI.TextField(rect, value, maxLength, style ?? GUIStyle.none);

        return GUI.TextField(rect, value, style ?? GUIStyle.none);
    }

    // rowsJson is a JSON array of rows, each an array of key strings
    // ("{shift}" and "{bksp}" are the special keys).
    public static void AddLayout(string id, string label, string rowsJson)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UberAddKeyboardLayout(id, label, rowsJson);
#endif
    }

    // Downloads a keyboard layout as JSON (same format as AddLayout) from url.
    // The server must allow CORS requests from the game origin.
    public static void LoadLayout(string url, string id, string label)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UberLoadKeyboardLayout(url, id, label);
#endif
    }
}

#if UNITY_WEBGL
public class WebGLKeyboardRouter : MonoBehaviour
{
    public int ActiveFieldId;
    public bool HasResult;
    public string Result;
    public string OpenedValue;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ActiveFieldId = 0;
        HasResult = false;
        Result = null;
        OpenedValue = null;
    }

    public void OnWebGLFieldInput(string text)
    {
        Result = text ?? string.Empty;
        HasResult = true;
    }
}
#endif
