using UnityEngine;

public class CommServerConnection : MonoBehaviour
{
    CommPeer peer;

    // Use this for initialization
    void Start()
    {
        peer = new CommPeer();
       
        UnityRuntime.Instance.OnUpdate += peer.Update;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 200, 20), peer.IsConnected ? "Disconnect" : "Connect"))
        {
            if (peer.IsConnected)
                peer.Disconnect();
            else
                peer.Connect("172.16.130.171:5055", 2, Cmune.DataCenter.Common.Entities.ChannelType.Android);
        }
        GUI.Label(new Rect(100, 120, 200, 20), "Status: " + peer.Peer.PeerState);

        if (peer.IsConnected)
        {
            if (GUI.Button(new Rect(100, 150, 200, 20), "Enter"))
            {
                peer.Operations.SendEnterLobby();
            }
            if (GUI.Button(new Rect(100, 170, 200, 20), "Leave"))
            {
                peer.Operations.SendLeaveLobby();
            }
        }
    }
}