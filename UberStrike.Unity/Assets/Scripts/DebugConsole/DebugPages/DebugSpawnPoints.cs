using UnityEngine;
using UberStrike.Realtime.Common;

public class DebugSpawnPoints : IDebugPage
{
    public string Title
    {
        get { return "Spawn"; }
    }

    private Vector2 scroll1;
    private Vector2 scroll2;
    private Vector2 scroll3;
    private GameMode gameMode;

    // Use this for initialization
    public void Draw()
    {
        GUILayout.BeginHorizontal();
        foreach (GameMode mode in System.Enum.GetValues(typeof(GameMode)))
        {
            if (GUILayout.Button(mode.ToString()))
                gameMode = mode;
        }
        GUILayout.EndHorizontal();
        {
            if (GameState.HasCurrentGame)
                GUILayout.Label("CurrentSpawnPoint " + GameState.CurrentGame.GameMode + " - " + GameState.CurrentGame.CurrentSpawnPoint);
            else
                GUILayout.Label("CurrentSpawnPoint - no game mode running");

            GUILayout.BeginHorizontal();
            {
                Vector3 pos;
                Quaternion rot;

                //NONE
                scroll1 = GUILayout.BeginScrollView(scroll1);
                GUILayout.Label(TeamID.NONE.ToString());
                for (int i = 0; i < SpawnPointManager.Instance.GetSpawnPointCount(gameMode, TeamID.NONE); i++)
                {
                    SpawnPointManager.Instance.GetSpawnPointAt(i, gameMode, TeamID.NONE, out pos, out rot);
                    GUILayout.Label(i + ": " + pos);
                }
                GUILayout.EndScrollView();

                //BLUE
                scroll2 = GUILayout.BeginScrollView(scroll2);
                GUILayout.Label(TeamID.BLUE.ToString());
                for (int i = 0; i < SpawnPointManager.Instance.GetSpawnPointCount(gameMode, TeamID.BLUE); i++)
                {
                    SpawnPointManager.Instance.GetSpawnPointAt(i, gameMode, TeamID.BLUE, out pos, out rot);
                    GUILayout.Label(i + ": " + pos);
                }
                GUILayout.EndScrollView();

                //RED
                scroll3 = GUILayout.BeginScrollView(scroll3);
                GUILayout.Label(TeamID.RED.ToString());
                for (int i = 0; i < SpawnPointManager.Instance.GetSpawnPointCount(gameMode, TeamID.RED); i++)
                {
                    SpawnPointManager.Instance.GetSpawnPointAt(i, gameMode, TeamID.RED, out pos, out rot);
                    GUILayout.Label(i + ": " + pos);
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
        }
    }
}