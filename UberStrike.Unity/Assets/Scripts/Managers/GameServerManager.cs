using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.Core.Models.Views;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using UnityEngine;

public class GameServerManager : Singleton<GameServerManager>
{
    private Dictionary<int, GameServerView> _gameServers = new Dictionary<int, GameServerView>();

    // sorting
    private List<GameServerView> _sortedServers = new List<GameServerView>();
    private IComparer<GameServerView> _comparer;
    private bool _reverseSorting = false;

    // latency check
    private Dictionary<int, ServerLoadRequest> _loadRequests = new Dictionary<int, ServerLoadRequest>();
    private const int ServerUpdateCycle = 30;

    public int PhotonServerCount { get { return _gameServers.Count; } }
    public int AllPlayersCount { get; private set; }
    public int AllGamesCount { get; private set; }
    public IEnumerable<GameServerView> PhotonServerList { get { return _sortedServers; } }
    public IEnumerable<ServerLoadRequest> ServerRequests { get { return _loadRequests.Values; } }

    private GameServerManager() { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reverse"></param>
    public void SortServers()
    {
        if (_comparer != null)
        {
            _sortedServers.Sort(_comparer);

            if (_reverseSorting)
                _sortedServers.Reverse();
        }
    }

    public GameServerView GetBestServer()
    {
        List<GameServerView> servers = new List<GameServerView>(_gameServers.Values);
        servers.Sort((s, t) => s.Latency - t.Latency);

        GameServerView server = null;
        for (int i = 0; i < servers.Count; i++)
        {
            GameServerView s = servers[i];

            if (s.Latency == 0) continue;
#if UNITY_ANDROID || UNITY_IPHONE
            if (s.UsageType != PhotonUsageType.Mobile) continue;
#endif

            if (server == null)
            {
                server = s;
            }
            else if (s.Latency < 200 && server.Data.PlayersConnected < s.Data.PlayersConnected)
            {
                server = s;
            }
        }
        return server;

        //float latency = float.MaxValue;
        //foreach (var gs in _gameServers.Values)
        //{
        //    if (gs.Latency > 0 && gs.Latency < latency)
        //    {
        //        latency = gs.Latency;
        //        server = gs;
        //    }
        //}
        //return server;
    }

    internal string GetServerName(string connection)
    {
        string server = string.Empty;
        foreach (var gs in _gameServers.Values)
        {
            if (gs.ConnectionString == connection)
            {
                server = gs.Name;
                break;
            }
        }
        return server;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="comparer"></param>
    /// <param name="reverse"></param>
    public void SortServers(IComparer<GameServerView> comparer, bool reverse = false)
    {
        _comparer = comparer;
        _reverseSorting = reverse;

        lock (_sortedServers)
        {
            _sortedServers.Clear();
            _sortedServers.AddRange(_gameServers.Values);
        }

        SortServers();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="view"></param>
    public void AddGameServer(PhotonView view)
    {
        _gameServers.Add(view.PhotonId, new GameServerView(view));

        //_sortedServers = new List<GameServerView>(_gameServers.Values);

        SortServers();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public int GetServerLatency(string connection)
    {
        foreach (GameServerView info in _gameServers.Values)
        {
            if (info.ConnectionString == connection)
                return info.Latency;
        }

        return 0;
    }

    public IEnumerator StartUpdatingServerLoads()
    {
        foreach (var server in _gameServers.Values)
        {
            ServerLoadRequest request;
            if (!_loadRequests.TryGetValue(server.Id, out request))
            {
                request = ServerLoadRequest.Run(MonoRoutine.Instance, server,
                    () =>
                    {
                        UpdateGamesAndPlayerCount();
                        GameListManager.UpdateServerLatency(server.ConnectionString);
                    });
                _loadRequests.Add(server.Id, request);
            }

            //don't run multiple requests
            if (request.RequestState != ServerLoadRequest.RequestStateType.Waiting)
            {
                request.RunAgain();
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    public IEnumerator StartUpdatingLatency(Action<float> progressCallback)
    {
        //UpdateLatency();
        yield return MonoRoutine.Start(StartUpdatingServerLoads());

        int count = 0;
        while (count != _loadRequests.Count)
        {
            yield return new WaitForSeconds(1);

            count = 0;
            foreach (var r in _loadRequests.Values)
            {
                if (r.RequestState != ServerLoadRequest.RequestStateType.Waiting)
                    count++;
            }

            progressCallback(count / _loadRequests.Count);
        }
    }

    /// <summary>
    /// server data refresh event (stats for server selection screen)
    /// </summary>
    /// <param name="ev"></param>
    private void UpdateGamesAndPlayerCount()
    {
        AllPlayersCount = 0;
        AllGamesCount = 0;

        foreach (GameServerView server in _gameServers.Values)
        {
            AllPlayersCount += server.Data.PlayersConnected;
            AllGamesCount += server.Data.RoomsCreated;
        }

        SortServers();
    }
}

public static class CmuneNetworkManager
{
    public static bool UseLocalCommServer = true;

    public static int RoomCreationMethod = StaticRoomID.Auto;

    public static GameServerView CurrentGameServer = GameServerView.Empty;
    public static GameServerView CurrentLobbyServer = GameServerView.Empty;
    public static GameServerView CurrentCommServer = GameServerView.Empty;
}