using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UnityEngine;

public class DebugConsoleManager : MonoBehaviour
{
    private Vector2 _scrollDebug;
    private static bool _isExceptionSent = false;
    private List<string> _exceptions = new List<string>(10);

    private static IDebugPage[] _debugPages = new IDebugPage[0];
    private static string[] _debugPageDescriptors = new string[0];

    private static int _currentPageSelectedIdx = 0;
    private static IDebugPage _currentPageSelected;

    [SerializeField]
    private bool _isDebugConsoleEnabled = false;

    public bool IsDebugConsoleEnabled { get { return _isDebugConsoleEnabled; } set { _isDebugConsoleEnabled = value; } }

    private void Awake()
    {
        if (Application.isEditor)
        {
            UpdatePages(MemberAccessLevel.Admin);
        }
        else
        {
            CmuneEventHandler.AddListener<LoginEvent>((ev) => UpdatePages(ev.AccessLevel));
        }
    }

    private void Start()
    {
        Application.RegisterLogCallback(OnUnityDebugCallback);
    }

    private void Update()
    {
        if (KeyInput.AltPressed && KeyInput.CtrlPressed && KeyInput.GetKeyDown(KeyCode.D))
        {
            _isDebugConsoleEnabled = !_isDebugConsoleEnabled;
        }
    }

    private void OnGUI()
    {
        if (ApplicationDataManager.BuildType != BuildType.Prod)
        {
            for (int i = 0; i < _exceptions.Count; i++)
            {
                GUI.Label(new Rect(0, GUITools.ScreenHeight - 40 - i * 25, GUITools.ScreenWidth, 20), _exceptions[i]);
            }
        }

        if (_isDebugConsoleEnabled)
        {
            GUI.skin = BlueStonez.Skin;

            if (_debugPageDescriptors.Length > 0)
            {
                GUILayout.BeginArea(new Rect(20, Screen.height * 0.5f, Screen.width - 40, Screen.height * 0.5f - 20), "", "window");
                {
                    int sel = GUILayout.SelectionGrid(_currentPageSelectedIdx, _debugPageDescriptors, _debugPageDescriptors.Length);
                    if (sel != _currentPageSelectedIdx)
                    {
                        sel = Mathf.Clamp(sel, 0, _debugPages.Length - 1);
                        _currentPageSelectedIdx = sel;
                        _currentPageSelected = _debugPages[sel];
                    }

                    GUILayout.Space(5);

                    _scrollDebug = GUILayout.BeginScrollView(_scrollDebug);
                    {
                        if (_currentPageSelected != null)
                        {
                            _currentPageSelected.Draw();
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndArea();
            }
        }
    }

    private void UpdatePages(MemberAccessLevel level)
    {
        if (level == MemberAccessLevel.Admin)
        {
            _debugPages = new IDebugPage[]
            {
                new DebugLogMessages(),
                new DebugApplication(),
                new DebugWebServices(),
                new DebugNetworkTraffic(),
                new DebugGraphics(),
                new DebugMaps(),

                new DebugProjectiles(),
                new DebugConnection(),
                new DebugServerState(),
                new DebugGameState(),
                new DebugPlayerManager(),
                //new DebugAnimation(),
                //new DebugShop(),
                //new DebugAudio(audio),
                //new DebugSpawnPoints(),
            };
        }
        else if (level == MemberAccessLevel.SeniorModerator || level == MemberAccessLevel.JuniorModerator)
        {
            _debugPages = new IDebugPage[]
            {
                new DebugLogMessages(),
                new DebugApplication(),
                new DebugWebServices(),
                new DebugNetworkTraffic(),
                new DebugGraphics(),
                new DebugGameState(),
                new DebugProjectiles(),
            };
        }
        else
        {
            _debugPages = new IDebugPage[]
            {
                new DebugLogMessages(),
                new DebugApplication(),
                //new DebugProjectiles(),
                //new DebugWebServices(),
                //new DebugNetworkTraffic(),
                //new DebugGraphics(),

                //new DebugConnection(),
                //new DebugServerState(),
                //new DebugRemoteMethodInterface(),
                //new DebugGameState(),
                //new DebugNetworkMessaging(),
                //new DebugPlayerManager(),
                //new DebugAnimation(),
                //new DebugAudio(audio),
            };
        }
        _debugPageDescriptors = new string[_debugPages.Length];

        for (int i = 0; i < _debugPages.Length; i++)
        {
            _debugPageDescriptors[i] = _debugPages[i].Title;
        }

        _currentPageSelectedIdx = 0;
        _currentPageSelected = _debugPages[0];
    }

    private void OnUnityDebugCallback(string logString, string stackTrace, LogType logType)
    {
        switch (logType)
        {
            case LogType.Log:
                if (CmuneDebug.IsDebugEnabled) DebugLogMessages.Log(0, logString);
                break;
            case LogType.Warning:
                if (CmuneDebug.IsWarningEnabled) DebugLogMessages.Log(1, logString);
                break;
            case LogType.Error:
                if (CmuneDebug.IsErrorEnabled) DebugLogMessages.Log(2, logString);
                break;
            case LogType.Assert:
                DebugLogMessages.Log(2, logString);
                break;
            case LogType.Exception:
                // If the WWW Service returned "Could not resolve host" then we likely cant connect to the WS box, so don't bother trying to Send Exception Report
                if (logString.Contains("Could not resolve host") || logString.Contains("Failed downloading http://"))
                {
                    DebugLogMessages.Console.Log(2, logString + "\n Info: It is likely you have lost connection to the internet, or the url is unreachable.");
                }
                else
                {
                    DebugLogMessages.Console.Log(2, logString);
                    SendExceptionReport(logString, stackTrace);
                    if (_exceptions.Count < 10)
                        _exceptions.Add(logString + " " + stackTrace);
                }
                break;
        }
    }

    public static void SendExceptionReport(string logString, string stackTrace, string popupMessage = null)
    {
        CmuneDebug.LogError("Exception: {0}\n{1}", logString, stackTrace);

        if (ApplicationDataManager.Exists && ApplicationDataManager.IsOnline)
        {
            // Send exception to webservice unless its already sent or we are in Unity Editor
            if (!_isExceptionSent)
            {
                _isExceptionSent = true;

                UberStrike.WebService.Unity.ApplicationWebServiceClient.RecordException(
                        PlayerDataManager.CmidSecure,
                        ApplicationDataManager.BuildType,
                        ApplicationDataManager.Channel,
                        ApplicationDataManager.VersionLong,
                        logString,
                        stackTrace,
                        DebugLogMessages.Console.ToHTML() + ApplicationDataManager.Instance.LocalSystemInfo.ToHTML(),
                        () => Debug.Log("SendExceptionReport Called."), null);
            }

            if (!string.IsNullOrEmpty(popupMessage))
            {
                PopupSystem.ShowMessage("Sorry", popupMessage, PopupSystem.AlertType.OK);
            }
        }
        else
        {
            Debug.LogWarning("SendExceptionReport: You can't send an exception report before the application is authenticated.");
        }
    }
}