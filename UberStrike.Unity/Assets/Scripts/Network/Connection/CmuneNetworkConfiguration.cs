using Cmune.DataCenter.Common.Entities;
using UberStrike.Realtime.Common.IO;
using Cmune.Realtime.Common;
using Cmune.Realtime.Common.IO;
using Cmune.Realtime.Photon.Client;
using UnityEngine;

public class CmuneNetworkConfiguration : MonoSingleton<CmuneNetworkConfiguration>
{
    #region Fields

    [SerializeField]
    private LocalRealtimeServer _localGameServer = new LocalRealtimeServer()
    {
        Ip = "127.0.0.1",
        Port = 5155,
        IsEnabled = true
    };

    [SerializeField]
    private LocalRealtimeServer _localCommServer = new LocalRealtimeServer()
    {
        Ip = "127.0.0.1",
        Port = 5055,
        IsEnabled = true
    };

    [SerializeField]
    private bool _enableDebugMessages = false;

    [SerializeField]
    private RoomCreation _roomCreation;

    #endregion

    public LocalRealtimeServer CustomGameServer { get { return _localGameServer; } }
    public LocalRealtimeServer CustomCommServer { get { return _localCommServer; } }

    #region Enums

    public enum RoomCreation
    {
        Auto = 0,
        Test = 1,
    }

    [System.Serializable]
    public class LocalRealtimeServer
    {
        public string Ip = string.Empty;
        public int Port = 0;
        public string Address { get { return Ip + ":" + Port; } }
        public bool IsEnabled = false;
    }
    #endregion

    private void Awake()
    {
        RealtimeSerialization.Converter = new UberStrikeByteConverter();
        CmuneNetworkState.DebugMessaging = _enableDebugMessages && Application.isEditor;

#if UNITY_EDITOR
        switch (_roomCreation)
        {
            case RoomCreation.Auto: CmuneNetworkManager.RoomCreationMethod = StaticRoomID.Auto; break;
            case RoomCreation.Test: CmuneNetworkManager.RoomCreationMethod = StaticRoomID.Test; break;
        }
#endif

        CmuneNetworkManager.CurrentGameServer = new GameServerView(_localGameServer.Address, PhotonUsageType.All);
        CmuneNetworkManager.CurrentCommServer = new GameServerView(_localCommServer.Address, PhotonUsageType.CommServer);
        CmuneNetworkManager.UseLocalCommServer = _localCommServer.IsEnabled;
    }
}