using UberStrike.Realtime.Client;
using UberStrike.Core.Models;

public class CommPeer : BaseCommPeer
{
    LobbyRoom lobby;

    public CommPeer()
        : base(100)
    {
        lobby = new LobbyRoom();

        AddRoomLogic(lobby);
    }

    protected override void OnLoadData(UberStrike.Core.ViewModel.ServerConnectionView data)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnLobbyEntered()
    {
        lobby.SendContactList();

        if (GameConnectionManager.Client.PeerListener.HasJoinedRoom)
        {
            var r = GameConnectionManager.Client.PeerListener.CurrentRoom;
            //when we joined the CommServer AND are currently in a game => update room information
            lobby.UpdatePlayerRoom(new CmuneRoomID() { RoomNumber = r.Number, Server = new ConnectionAddress() { Ipv4 = ConnectionAddress.ToInteger(r.Server) } });
        }
    }
}