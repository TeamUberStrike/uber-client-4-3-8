using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Mobile only: Android/iOS apps and phone/tablet browsers. Everything else stops before login.
public class DeviceLock : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int UberGetDeviceType();
#endif

    public static bool IsAllowed { get; private set; } = true;

    private static DeviceLock _instance;
    private GUIStyle _title;
    private GUIStyle _body;
    private Texture2D _logo;

    public static bool CheckAllowed()
    {
#if UNITY_EDITOR
        return true;
#elif UNITY_ANDROID || UNITY_IOS
        return Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
#elif UNITY_WEBGL
        return UberGetDeviceType() != 0;
#else
        return false;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        IsAllowed = CheckAllowed();
        if (!IsAllowed) Show();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartCheck()
    {
        DeviceCheck.Install();
    }

    public static void Lock()
    {
        IsAllowed = false;
        if (_instance != null) return;
        Show();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) Freeze(scene);
        }
    }

    private static void Show()
    {
        ApplicationDataManager.AutoLogin = false;
        AudioListener.volume = 0f;

        var go = new GameObject("DeviceLock");
        DontDestroyOnLoad(go);
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.09f);
        cam.cullingMask = 0;
        cam.depth = 1000f;
        _instance = go.AddComponent<DeviceLock>();
        SceneManager.sceneLoaded += (scene, mode) => Freeze(scene);
    }

    private static void Freeze(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (_instance == null || root != _instance.gameObject) root.SetActive(false);
        }
    }

    private void Start()
    {
        _logo = Resources.Load<Texture2D>("DeviceLockLogo");
        for (int i = 0; i < SceneManager.sceneCount; i++) Freeze(SceneManager.GetSceneAt(i));
    }

    private void OnGUI()
    {
        GUI.depth = -1000;
        if (_title == null)
        {
            _title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            _body = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _title.normal.textColor = Color.white;
            _body.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        }
        float unit = Mathf.Max(12f, Mathf.Min(Screen.width, Screen.height) / 24f);
        _title.fontSize = Mathf.RoundToInt(unit * 1.1f);
        _body.fontSize = Mathf.RoundToInt(unit * 0.8f);

        float w = Screen.width * 0.8f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.22f;
        if (_logo != null)
        {
            float lw = Mathf.Min(Screen.width * 0.5f, unit * 18f);
            float lh = lw * _logo.height / _logo.width;
            GUI.DrawTexture(new Rect((Screen.width - lw) / 2f, y, lw, lh), _logo, ScaleMode.ScaleToFit, true);
            y += lh + unit;
        }
        GUI.Label(new Rect(x, y, w, unit * 2f), "Mobile only", _title);
        GUI.Label(new Rect(x, y + unit * 2.2f, w, unit * 4f),
            "This version only runs on Android and iOS phones and tablets.\nOpen it on your phone or tablet to play.", _body);
    }
}
