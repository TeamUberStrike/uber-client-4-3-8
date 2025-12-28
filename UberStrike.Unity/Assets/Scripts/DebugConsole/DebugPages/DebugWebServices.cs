using System.Text;
using UberStrike.WebService.Unity;
using UnityEngine;

public class DebugWebServices : IDebugPage
{
    private StringBuilder _requestLog;
    private string _currentLog;
    private Vector2 scroller;

    public DebugWebServices()
    {
        _requestLog = new StringBuilder();
        UberStrike.WebService.Unity.Configuration.RequestLogger += AddRequestLog;
    }

    private void AddRequestLog(string log)
    {
        _requestLog.AppendLine(log);
        _currentLog = _requestLog.ToString();
    }

    public string Title
    {
        get { return "WebServices"; }
    }

    public void Draw()
    {
        scroller = GUILayout.BeginScrollView(scroller);
        {
            GUILayout.Label("IN (" + WebServiceStatistics.TotalBytesIn + ") -  OUT (" + WebServiceStatistics.TotalBytesOut + ")");
            foreach (var log in WebServiceStatistics.Data)
            {
                GUILayout.Label(log.Key + ": " + log.Value);
            }
            GUILayout.TextArea(_currentLog);
        }
        GUILayout.EndScrollView();
    }
}