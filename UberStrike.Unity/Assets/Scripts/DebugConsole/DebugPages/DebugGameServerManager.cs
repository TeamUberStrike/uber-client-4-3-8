using UnityEngine;

public class DebugGameServerManager : IDebugPage
{
    public string Title
    {
        get { return "Requests"; }
    }

    // Use this for initialization
    public void Draw()
    {
        foreach (var r in GameServerManager.Instance.ServerRequests)
        {
            GUILayout.Label(r.Server.Name + " " + r.Server.ConnectionString + ", Latency: " + r.Server.Latency + " - " + r.Server.IsValid);
            GUILayout.Label("States: " + r.RequestState + " " + r.Server.Data.State + ", Connection: " + r.ConnectionState + " with Peer: " + r.PeerState);
            GUILayout.Space(10);
        }
    }
}