using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Client-side anti-cheat orchestrator. Periodically sweeps the local machine for
/// known cheat-tool processes / modules / window titles and ships the raw enumeration to
/// the lobby server over Photon op-29. Server decides what counts as a hit
/// (<c>UberStrok.Realtime.Server.Comm.UberBeat.Initialize</c>).
///
/// HWID is built on first connect and sent once via op-28. UBZ's
/// <c>OnUberBeatAuthenticate</c> stores it for hardware-ban enforcement.
///
/// Wired in from <see cref="CheatDetection"/> alongside the existing
/// <c>SecureMemoryMonitor</c> + speedhack coroutines so the anti-cheat surface stays in
/// one file. Standalone-Windows only (Editor + Player) — every code path that
/// touches Win32 is behind a UNITY_STANDALONE_WIN / UNITY_EDITOR_WIN guard.
/// </summary>
public class UberBeat : MonoBehaviour
{
    public const float SweepIntervalSeconds = 5f;

    private static UberBeat s_instance;

    private string _pendingReport;
    private string _pendingDetection;
    private bool _hwidSent;
    private readonly object _lock = new object();
    private Thread _sweepThread;
    private volatile bool _stop;

    public static void EnsureRunning()
    {
        if (s_instance != null) return;
        var go = new GameObject("UberBeat");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<UberBeat>();
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(this);
            return;
        }
        s_instance = this;
    }

    private void OnEnable()
    {
        _stop = false;
        _sweepThread = new Thread(SweepLoop) { IsBackground = true, Name = "UberBeat" };
        _sweepThread.Start();
    }

    private void OnDisable()
    {
        _stop = true;
        _sweepThread = null;
    }

    private void Update()
    {
        // HWID dispatch — once per connected session.
        if (!_hwidSent)
        {
            string hwid;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                hwid = UberBeatScanner.BuildHwid() + "|UNITY:" + SystemInfo.deviceUniqueIdentifier;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UberBeat] HWID build failed: " + ex.Message);
                hwid = "BIOS:UNKNOWN|MOTHERBOARD:UNKNOWN|HDD:UNKNOWN|MAC:0.0.0.0|UNITY:" + SystemInfo.deviceUniqueIdentifier;
            }
#else
            hwid = "UNITY:" + SystemInfo.deviceUniqueIdentifier;
#endif
            if (UberBeatTransport.TrySendString(UberBeatTransport.OpUberBeatAuthenticate, hwid))
            {
                _hwidSent = true;
            }
        }

        // Periodic REPORT.
        string report = null, detection = null;
        lock (_lock)
        {
            if (_pendingReport != null) { report = _pendingReport; _pendingReport = null; }
            if (_pendingDetection != null) { detection = _pendingDetection; _pendingDetection = null; }
        }
        if (detection != null)
        {
            UberBeatTransport.TrySendString(UberBeatTransport.OpUberBeatReport, "DETECTED:" + detection);
        }
        if (report != null)
        {
            UberBeatTransport.TrySendString(UberBeatTransport.OpUberBeatReport, "REPORT:" + report);
        }
    }

    private void SweepLoop()
    {
        // Photon needs a moment to be useful — let the connection settle.
        Thread.Sleep(3000);
        while (!_stop)
        {
            try
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                string report = UberBeatScanner.BuildReport();
                lock (_lock) { _pendingReport = report; }
#endif
            }
            catch (Exception ex) { Debug.LogWarning("[UberBeat] sweep failed: " + ex); }

            int slept = 0;
            while (!_stop && slept < SweepIntervalSeconds * 1000)
            {
                Thread.Sleep(100);
                slept += 100;
            }
        }
    }

    /// <summary>
    /// Server pushed back the list of modules it didn't recognise; we now resolve each via
    /// Authenticode and ship a <c>TRUSTED:</c> reply with the ones that came back signed.
    /// Hook this up from wherever the server's <c>SendModulesRequest</c> event is delivered
    /// to the client.
    /// </summary>
    public static void OnServerRequestedModuleSignatures(string pipeSeparatedPaths)
    {
        if (string.IsNullOrEmpty(pipeSeparatedPaths)) return;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        var trusted = new List<string>();
        foreach (var path in pipeSeparatedPaths.Split('|'))
        {
            if (string.IsNullOrEmpty(path)) continue;
            if (UberBeatScanner.IsSigned(path)) trusted.Add(path);
        }
        if (trusted.Count > 0)
        {
            UberBeatTransport.TrySendString(UberBeatTransport.OpUberBeatReport,
                "TRUSTED:" + string.Join("|", trusted));
        }
#endif
    }

    /// <summary>
    /// Self-detection escape hatch — surface a hit from any other client subsystem (e.g. the
    /// existing <c>SecureMemoryMonitor</c> integrity check) and the report ships on next Update.
    /// </summary>
    public static void RaiseLocalDetection(string reason)
    {
        if (s_instance == null) return;
        lock (s_instance._lock) { s_instance._pendingDetection = reason; }
    }
}
