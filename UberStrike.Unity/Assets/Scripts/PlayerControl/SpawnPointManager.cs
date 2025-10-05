using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;

/// <summary>
/// Handles storing and retrieval of spawn points
/// </summary>
public class SpawnPointManager : Singleton<SpawnPointManager>
{
    private static readonly Vector3 DefaultSpawnPoint = new Vector3(0, 6, 0);

    private IDictionary<GameMode, IDictionary<TeamID, IList<SpawnPoint>>> _spawnPointsDictionary;

    private SpawnPointManager()
    {
        _spawnPointsDictionary = new Dictionary<GameMode, IDictionary<TeamID, IList<SpawnPoint>>>();

        // Set up spawnpoint dictionary
        foreach (GameMode gameMode in System.Enum.GetValues(typeof(GameMode)))
        {
            _spawnPointsDictionary[gameMode] = new Dictionary<TeamID, IList<SpawnPoint>>
            {
                { TeamID.BLUE, new List<SpawnPoint>() },
                { TeamID.RED, new List<SpawnPoint>() },
                { TeamID.NONE, new List<SpawnPoint>() }
            };
        }
    }

    private void Clear()
    {
        // Set up spawnpoint dictionary
        foreach (GameMode gameMode in System.Enum.GetValues(typeof(GameMode)))
        {
            _spawnPointsDictionary[gameMode][TeamID.NONE].Clear();
            _spawnPointsDictionary[gameMode][TeamID.BLUE].Clear();
            _spawnPointsDictionary[gameMode][TeamID.RED].Clear();
        }
    }

    #region Private Methods

    private bool TryGetSpawnPointAt(int index, GameMode gameMode, TeamID teamID, out SpawnPoint point)
    {
        point = index < GetSpawnPointList(gameMode, teamID).Count ? GetSpawnPointList(gameMode, teamID)[index] : null;
        return point != null;
    }

    private bool TryGetRandomSpawnPoint(GameMode gameMode, TeamID teamID, out SpawnPoint point)
    {
        IList<SpawnPoint> list = GetSpawnPointList(gameMode, teamID);

        point = list.Count > 0 ? list[UnityEngine.Random.Range(0, list.Count)] : null;
        return point != null;
    }

    private IList<SpawnPoint> GetSpawnPointList(GameMode gameMode, TeamID team)
    {
        if (gameMode == GameMode.Training)
            return _spawnPointsDictionary[GameMode.DeathMatch][TeamID.NONE];
        else
            return _spawnPointsDictionary[gameMode][team];
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Populates the dictionary of spawn points
    /// </summary>
    /// <param name="points"></param>
    public void ConfigureSpawnPoints(SpawnPoint[] points)
    {
        Clear();

        // Populate dictionary
        foreach (var p in points)
        {
            _spawnPointsDictionary[p.GameMode][p.TeamId].Add(p);
        }
    }

    /// <summary>
    /// Gets the total number of spawn points per game mode and team
    /// </summary>
    /// <param name="gameMode"></param>
    /// <param name="team"></param>
    /// <returns></returns>
    public int GetSpawnPointCount(GameMode gameMode, TeamID team)
    {
        return GetSpawnPointList(gameMode, team).Count;
    }

    /// <summary>
    /// Gets a spawn point at a specified index 
    /// </summary>
    /// <param name="index">Index sent by server to avoid player spawn collision</param>
    /// <param name="team">Spawn point belonging to team</param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    public void GetSpawnPointAt(int index, GameMode gameMode, TeamID team, out Vector3 position, out Quaternion rotation)
    {
        SpawnPoint p;
        if (TryGetSpawnPointAt(index, gameMode, team, out p))
        {
            position = p.transform.position;
            rotation = p.transform.rotation;
        }
        else
        {
            Debug.LogError("No spawnpoints found at " + index + " int list of length " + GetSpawnPointCount(gameMode, team));
            position = DefaultSpawnPoint;
            rotation = Quaternion.identity;
        }
    }

    public void GetRandomSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        IList<SpawnPoint> points = _spawnPointsDictionary[GameMode.DeathMatch][TeamID.NONE];

        SpawnPoint p = points[Random.Range(0, points.Count)];

        position = p.transform.position;
        rotation = p.transform.rotation;
    }

    public List<SpawnPoint> GetAllSpawnPoints()
    {
        List<SpawnPoint> result = new List<SpawnPoint>();

        foreach (IDictionary<TeamID, IList<SpawnPoint>> dic in _spawnPointsDictionary.Values)
        {
            foreach (IList<SpawnPoint> list in dic.Values)
            {
                result.AddRange(list);
            }
        }

        return result;
    }

    #endregion
}