using Cmune.Util;
using UnityEngine;

public class DebugApplication : IDebugPage
{
    public string Title
    {
        get { return "Application"; }
    }

    public void Draw()
    {
        GUILayout.Label("Channel: " + ApplicationDataManager.Channel);
        GUILayout.Label("BuildType: " + ApplicationDataManager.BuildType);
        GUILayout.Label("VersionShort: " + ApplicationDataManager.VersionShort);
        GUILayout.Label("VersionLong: " + ApplicationDataManager.VersionLong);
        GUILayout.Label("Url: " + Application.absoluteURL);
        GUILayout.Label("Debug Level: " + CmuneDebug.DebugLevel);

        if (PlayerDataManager.AccessLevel > 0)
        {
            GUILayout.Label("Time: " + CheatDetection.GameTime + " " + CheatDetection.RealTime + " (Dif: " + (CheatDetection.GameTime - CheatDetection.RealTime) + ")");
            GUILayout.Label("Member Name: " + PlayerDataManager.Name);
            GUILayout.Label("Member Cmid: " + PlayerDataManager.Cmid);
            GUILayout.Label("Member Access: " + PlayerDataManager.AccessLevel);
            GUILayout.Label("Member Tag: " + PlayerDataManager.GroupTag);
        }
    }
}