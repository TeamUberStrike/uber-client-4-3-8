using System.Collections.Generic;
using Cmune.Realtime.Common;
using Cmune.Realtime.Photon.Client;
using Cmune.Util;
using UberStrike.Realtime.Common;

public static class GameListManager
{
    private static Dictionary<CmuneRoomID, GameMetaData> _gameList = new Dictionary<CmuneRoomID, GameMetaData>();

    public static int PlayersCount { get; private set; }

    public static IEnumerable<GameMetaData> GameList { get { return GameListManager._gameList.Values; } }

    public static int GamesCount { get { return GameListManager._gameList.Count; } }

    static GameListManager()
    {
        CmuneEventHandler.AddListener<RoomListUpdatedEvent>(OnGameListUpdated);
    }

    /// <summary>
    /// Call this Initialization once, to listen properly to RoomListUpdatedEvent & ServerLoadQueryEvent
    /// </summary>
    public static void Init()
    {
        //only important to run the static constructor and listen to the game list update events
    }

    private static void OnGameListUpdated(RoomListUpdatedEvent ev)
    {
        PlayersCount = 0;
        _gameList.Clear();

        foreach (RoomMetaData room in ev.Rooms)
        {
            if (room.RoomID.IsVersionCompatible)
            {
                GameMetaData game = room as GameMetaData;
                if (game != null)
                {
                    PlayersCount += game.ConnectedPlayers;
                    game.Latency = GameServerManager.Instance.GetServerLatency(game.ServerConnection);

                    _gameList[game.RoomID] = game;
                }
            }
        }

        if (ev.IsInitialList && PlayPageGUI.Exists && MenuPageManager.IsCurrentPage(PageType.Play))
        {
            PlayPageGUI.Instance.RefreshGameList();
        }
    }

    public static void UpdateServerLatency(string serverConnection)
    {
        //update game list
        foreach (GameMetaData room in _gameList.Values)
        {
            if (room.ServerConnection == serverConnection)
                room.Latency = GameServerManager.Instance.GetServerLatency(serverConnection);
        }
    }

    public static void ClearGameList()
    {
        GameListManager._gameList.Clear();
    }

    public static bool TryGetGame(CmuneRoomID id, out GameMetaData game)
    {
        return _gameList.TryGetValue(id, out game);
    }
}