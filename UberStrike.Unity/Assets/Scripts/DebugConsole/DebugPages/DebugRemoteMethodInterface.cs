using System.Collections.Generic;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;
using UnityEngine;

public class DebugRemoteMethodInterface : IDebugPage
{
    private Vector2[] _scrollers = new Vector2[3];

    public string Title
    {
        get { return "Rmi"; }
    }

    public void Draw()
    {
        DrawRemoteMethodInterface(GameConnectionManager.Rmi, 0);
    }

    public void DrawRemoteMethodInterface(RemoteMethodInterface rmi, int i)
    {
        GUILayout.BeginHorizontal();
        {
            _scrollers[(i * 3) + 0] = GUILayout.BeginScrollView(_scrollers[(i * 3) + 0], GUILayout.Width(200));
            {
                GUILayout.Label("WAIT REG");
                foreach (RemoteMethodInterface.RegistrationJob j in rmi.RegistrationJobs)
                    GUILayout.Label(string.Format("{0}", j));
            }
            GUILayout.EndScrollView();

            _scrollers[(i * 3) + 1] = GUILayout.BeginScrollView(_scrollers[(i * 3) + 1], GUILayout.Width(400));
            {
                GUILayout.Label("REG CLASS");
                foreach (INetworkClass kvp in rmi.RegisteredClasses)
                    GUILayout.Label(string.Format("{0}", kvp));
            }
            GUILayout.EndScrollView();

            _scrollers[(i * 3) + 2] = GUILayout.BeginScrollView(_scrollers[(i * 3) + 2], GUILayout.Width(100));
            {
                GUILayout.Label("NET CLASS");
                foreach (short kvp in rmi.NetworkInstantiatedObjects)
                    GUILayout.Label(string.Format("Nid {0}", kvp));
            }
            GUILayout.EndScrollView();
        }
        GUILayout.EndHorizontal();
    }
}