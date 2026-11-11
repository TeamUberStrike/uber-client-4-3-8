#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UberStrike.Realtime.NakamaAdapter
{
    // ?devlog=1 only: scrubbed lines (ClientLogFilter) -> POST same-origin /__log every 2 s
    public static class WebGLLogForward
    {
        const int MaxBatch = 50;
        const int MaxTotal = 2000;
        static readonly List<string> Queue = new List<string>();
        static string _url;
        static string _host;
        static int _total;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Uri page;
            if (!ClientLogFilter.Enabled(Application.absoluteURL) || !Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out page))
                return;
            _host = page.Host;
            _url = page.GetLeftPart(UriPartial.Authority) + "/__log";
            Application.logMessageReceived += OnLog;
            MonoRoutine.Start(Pump());
            Debug.Log("[nakama] devlog on: [nakama] lines + errors, scrubbed, to " + _url);
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            bool error = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
            if (_total >= MaxTotal || !ClientLogFilter.Wanted(msg, error))
                return;
            _total++;
            Queue.Add(ClientLogFilter.Json(Time.realtimeSinceStartup, type.ToString(), ClientLogFilter.Scrub(msg, _host)));
        }

        static IEnumerator Pump()
        {
            int failures = 0;
            while (failures < 3)
            {
                yield return new WaitForSecondsRealtime(2f);
                if (Queue.Count == 0)
                    continue;
                int n = Math.Min(MaxBatch, Queue.Count);
                string body = "[" + string.Join(",", Queue.GetRange(0, n)) + "]";
                Queue.RemoveRange(0, n);
                using (var req = new UnityWebRequest(_url, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = 5;
                    yield return req.SendWebRequest();
                    failures = req.result == UnityWebRequest.Result.Success ? 0 : failures + 1;
                }
            }
            Application.logMessageReceived -= OnLog;
            Queue.Clear();
        }
    }
}
#endif
