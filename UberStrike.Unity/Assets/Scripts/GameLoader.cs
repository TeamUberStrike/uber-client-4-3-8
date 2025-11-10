using UberStrike.Realtime.Common;
using System.Collections;
using UnityEngine;
using Cmune.Util;
using UberStrike.Core.Types;

/// <summary>
/// <copyright>(c) Cmune Ltd. 2011</copyright>
/// <author>Thomas Franken</author>
/// ...
/// </summary>
public class GameLoader : Singleton<GameLoader>
{
    private GameMetaData data;
    private UberstrikeMap map;

    private GameLoader() { }

    public void CreateGame(UberstrikeMap map, string name = " ", string password = "", int timeLimit = 0, int killLimit = 1, int playerLimit = 1, GameModeType mode = GameModeType.None, GameFlags.GAME_FLAGS flags = 0)
    {
        GameMetaData gameData = new GameMetaData(CmuneNetworkManager.RoomCreationMethod,
                                name,
                                CmuneNetworkManager.CurrentGameServer.ConnectionString,
                                map.Id,
                                password,
                                timeLimit,
                                playerLimit,
                                mode.GetGameModeID());

        gameData.GameModifierFlags = (int)flags;
        gameData.SplatLimit = killLimit;
        gameData.Password = password;

        JoinGame(gameData);
    }

    public void JoinGame(GameMetaData data)
    {
        if (data != null)
        {
            this.data = data;
            this.map = LevelManager.Instance.GetMapWithId(data.MapID);

            if (map != null)
            {
                if (!map.IsLoaded)
                {
                    CoroutineManager.StartCoroutine(StartLoadingMap, true);
                }
                else
                {
                    LaunchGame();
                }
            }
        }
        else
        {
            CmuneDebug.LogError("JoinGame failed because GameMetaDAta is null");
        }
    }

    private IEnumerator StartLoadingMap()
    {
        int id = CoroutineManager.Begin(StartLoadingMap);

        LevelManager.Instance.LoadMap(map.Id);

        float timeout = Time.time + 240;
        string mapName = map.Name;
        var progress = PopupSystem.ShowProgress("Loading Map", "Loading " + mapName + "...", () => LevelManager.Instance.CurrentProgress);

        progress.ShowCancelButton(() =>
        {
            //only enable cancel before the file is fully donwloaded
            if (progress.ManualProgress < 1)
            {
                LevelManager.Instance.CancelLoadMap(map.Id);
                CoroutineManager.End(StartLoadingMap, id);
            }
            else
            {
                //set 10 seconds grace time after the level is fully loaded but nothing is happening
                timeout = Time.time + 10;
            }
        });

        while (!map.IsLoaded && CoroutineManager.IsCurrent(StartLoadingMap, id) && Time.time < timeout)
        {
            yield return new WaitForEndOfFrame();
        }

        PopupSystem.HideMessage(progress);

        if (CoroutineManager.IsCurrent(StartLoadingMap, id) && map.IsLoaded)
        {
            LaunchGame();
        }
        else
        {
            LevelManager.Instance.CancelLoadMap(map.Id);
            Debug.LogError("StartLoadingMap canceled or couldn't be loaded, timeout: " + (Time.time < timeout));
        }

        CoroutineManager.End(StartLoadingMap, id);
    }

    private void LaunchGame()
    {
        if (map.IsLoaded)
            GameStateController.Instance.CreateGame(data);
    }
}