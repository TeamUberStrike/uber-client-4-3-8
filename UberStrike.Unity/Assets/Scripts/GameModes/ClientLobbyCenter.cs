using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;

/// <summary>
/// 
/// </summary>
[NetworkClass(NetworkClassID.LobbyCenter)]
public class ClientLobbyCenter : ClientNetworkClass
{
    public ClientLobbyCenter(RemoteMethodInterface rmi)
        : base(rmi)
    {
        _myInfo = new CommActorInfo(string.Empty, 0, Cmune.DataCenter.Common.Entities.ChannelType.WebPortal);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        MyInfo.Cache.Clear();
        MyInfo.ActorId = _rmi.Messenger.PeerListener.ActorIdSecure;
        MyInfo.PlayerName = string.Empty;

        SendMethodToServer(LobbyRPC.Join, MyInfo);
    }

    #region PROPERTIES

    public ActorInfo MyInfo
    {
        get { return _myInfo; }
    }

    #endregion

    #region FIELDS

    private ActorInfo _myInfo;

    #endregion
}
