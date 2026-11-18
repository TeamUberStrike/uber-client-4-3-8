using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Sends device diagnostics to the web server; login carries the returned token, server decides.
public class DeviceCheck : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string UberDiagCollect(string phase);
#endif

    private const string DefaultBaseUrl = "http://localhost:9000/";
    private const float PlayInterval = 60f;

    public static string Token { get; private set; }
    public static string Verdict { get; private set; }

    private static DeviceCheck _instance;
    private static float _start;
    private bool _ok;
    private int _touchBegan, _mouseDown, _wasd, _keys, _frames;
    private float _dtSum;

    public static void Install()
    {
        if (_instance != null) return;
        var go = new GameObject("DeviceCheck");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<DeviceCheck>();
        _start = Time.realtimeSinceStartup;
    }

    public static string MachineId(string id)
    {
        return string.IsNullOrEmpty(Token) ? id : id + "|dl:" + Token;
    }

    public static IEnumerator Run(string phase)
    {
        Install();
        for (int i = 0; i < 2; i++)
        {
            yield return _instance.Send(phase);
            if (_instance._ok) yield break;
        }
    }

    private IEnumerator Start()
    {
        float wait = 0f;
        while (BaseUrl() == null && wait < 60f)
        {
            yield return new WaitForSecondsRealtime(1f);
            wait += 1f;
        }
        if (BaseUrl() != null && string.IsNullOrEmpty(Token)) yield return Send("init");
        while (true)
        {
            yield return new WaitForSecondsRealtime(PlayInterval);
            if (!string.IsNullOrEmpty(Token) && PlayerDataManager.IsPlayerLoggedIn) yield return Send("play");
        }
    }

    private void Update()
    {
        _frames++;
        _dtSum += Time.unscaledDeltaTime;
#if !UNITY_WEBGL || UNITY_EDITOR
        for (int i = 0; i < Input.touchCount; i++)
            if (Input.GetTouch(i).phase == TouchPhase.Began) _touchBegan++;
        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0)) _mouseDown++;
        if (Input.anyKeyDown) _keys++;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)) _wasd++;
#endif
    }

    private static string BaseUrl()
    {
        var url = UberStrike.WebService.Unity.Configuration.WebserviceBaseUrl;
        return string.IsNullOrEmpty(url) || url == DefaultBaseUrl ? null : url;
    }

    private IEnumerator Send(string phase)
    {
        _ok = false;
        var url = BaseUrl();
        if (url == null) yield break;
        var body = Encoding.UTF8.GetBytes(Build(phase));
        using (var req = new UnityWebRequest(url + "DeviceCheck.ashx", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "text/plain");
            req.timeout = 10;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) yield break;
            Response r = null;
            try { r = JsonUtility.FromJson<Response>(req.downloadHandler.text); } catch (Exception) { }
            if (r == null) yield break;
            _ok = true;
            if (!string.IsNullOrEmpty(r.token)) Token = r.token;
            Verdict = r.verdict;
            if ((r.kick || r.verdict == "block") && !Application.isEditor) DeviceLock.Lock();
        }
    }

    private string Build(string phase)
    {
        string web = null;
#if UNITY_WEBGL && !UNITY_EDITOR
        web = UberDiagCollect(phase);
#endif
        var u = new StringBuilder();
        u.Append("{");
        Field(u, "plat", Application.platform.ToString()); u.Append(',');
        Field(u, "gdt", SystemInfo.graphicsDeviceType.ToString()); u.Append(',');
        Field(u, "gdn", SystemInfo.graphicsDeviceName); u.Append(',');
        Field(u, "gdv", SystemInfo.graphicsDeviceVendor); u.Append(',');
        Num(u, "gmem", SystemInfo.graphicsMemorySize); u.Append(',');
        Num(u, "smem", SystemInfo.systemMemorySize); u.Append(',');
        Num(u, "cores", SystemInfo.processorCount); u.Append(',');
        Field(u, "cpu", SystemInfo.processorType); u.Append(',');
        Field(u, "os", SystemInfo.operatingSystem); u.Append(',');
        Field(u, "model", SystemInfo.deviceModel); u.Append(',');
        Num(u, "sw", Screen.width); u.Append(',');
        Num(u, "sh", Screen.height); u.Append(',');
        Num(u, "crw", Screen.currentResolution.width); u.Append(',');
        Num(u, "crh", Screen.currentResolution.height); u.Append(',');
        Num(u, "crr", Screen.currentResolution.refreshRateRatio.value); u.Append(',');
        u.Append("\"touch\":").Append(Input.touchSupported ? "true" : "false").Append(',');
        Num(u, "tc", Input.touchCount); u.Append(',');
        Num(u, "udt", Time.unscaledDeltaTime); u.Append(',');
        Num(u, "fps", _dtSum > 0 ? _frames / _dtSum : 0);
        u.Append("}");

        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(web) && web.EndsWith("}"))
        {
            sb.Append(web, 0, web.Length - 1).Append(',');
        }
        else
        {
            sb.Append("{\"v\":1,");
            Field(sb, "kind", Application.isEditor ? "editor" : (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) ? "native" : "desktop");
            sb.Append(',');
            Field(sb, "phase", phase); sb.Append(',');
            sb.Append("\"in\":{");
            Num(sb, "ts", _touchBegan); sb.Append(',');
            Num(sb, "md", _mouseDown); sb.Append(',');
            Num(sb, "kd", _keys); sb.Append(',');
            Num(sb, "wasd", _wasd);
            sb.Append("},\"rt\":{");
            Num(sb, "sec", Time.realtimeSinceStartup - _start);
            sb.Append("},");
        }
        sb.Append("\"unity\":").Append(u);
        if (!string.IsNullOrEmpty(Token)) { sb.Append(','); Field(sb, "token", Token); }
        sb.Append('}');
        return sb.ToString();
    }

    private static void Field(StringBuilder sb, string k, string v)
    {
        sb.Append('"').Append(k).Append("\":\"");
        foreach (char c in v ?? "")
        {
            if (c == '"' || c == '\\') sb.Append('\\').Append(c);
            else if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
            else sb.Append(c);
        }
        sb.Append('"');
    }

    private static void Num(StringBuilder sb, string k, double v)
    {
        sb.Append('"').Append(k).Append("\":").Append((double.IsNaN(v) || double.IsInfinity(v) ? 0 : Math.Round(v, 4)).ToString(CultureInfo.InvariantCulture));
    }

    [Serializable]
    private class Response
    {
        public string verdict;
        public string token;
        public bool kick;
    }
}
