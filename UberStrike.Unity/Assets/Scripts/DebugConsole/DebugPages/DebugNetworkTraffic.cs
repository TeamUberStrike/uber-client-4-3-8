using Cmune.Realtime.Photon.Client.Network.Utils;
using UnityEngine;

public class DebugNetworkTraffic : IDebugPage
{
    private Vector2 scroller;

    public DebugNetworkTraffic()
    {
    }

    public string Title
    {
        get { return "Network"; }
    }

    public void Draw()
    {
        scroller = GUILayout.BeginScrollView(scroller);
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                GUILayout.Label("IN (" + NetworkStatistics.TotalBytesIn + ")");
                foreach (var log in NetworkStatistics.Incoming)
                {
                    GUILayout.Label(log.Key + ": " + log.Value);
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                GUILayout.Label("OUT (" + NetworkStatistics.TotalBytesOut + ")");
                foreach (var log in NetworkStatistics.Outgoing)
                {
                    GUILayout.Label(log.Key + ": " + log.Value);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }
}