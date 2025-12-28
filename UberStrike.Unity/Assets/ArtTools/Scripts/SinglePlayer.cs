using System.Collections;
using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;

public class SinglePlayer : MonoBehaviour
{
    private AvatarDecorator _decorator;
    private GameMode _gameMode = GameMode.DeathMatch;
    private TeamID _team = TeamID.NONE;
    private int _spawnPoint = 0;

    [SerializeField]
    private Transform _firstPersonWeapons;
    [SerializeField]
    private Transform _thirdPersonWeapons;

    public Transform FirstPersonWeapons { get { return _firstPersonWeapons; } }
    public Transform ThirdPersonWeapons { get { return _thirdPersonWeapons; } }

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        var map = new List<UberstrikeMap>(LevelManager.Instance.AllMaps).Find(m => m.Id > 0);

        if (map != null)
        {
            GameState.SetCurrentSpace(map.Space);
            LevelCamera.Instance.SetLevelCamera(GameState.CurrentSpace.Camera, GameState.CurrentSpace.DefaultViewPoint.position, GameState.CurrentSpace.DefaultViewPoint.rotation);
            GameState.LocalPlayer.InitializePlayer();
            GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.FirstPerson);
            GameState.LocalPlayer.SetWeaponControlState(PlayerHudState.Playing);
            SpawnPointManager.Instance.ConfigureSpawnPoints(GameState.CurrentSpace.SpawnPoints.GetComponentsInChildren<SpawnPoint>(true));
            GameState.LocalPlayer.Pause();

            _decorator = GetComponentInChildren<AvatarDecorator>();
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 150, 30), "Game Modes:");
        if (GUI.Toggle(new Rect(160, 10, 60, 30), _gameMode == GameMode.DeathMatch, "DM") && GUI.changed) _gameMode = GameMode.DeathMatch;
        if (GUI.Toggle(new Rect(220, 10, 60, 30), _gameMode == GameMode.TeamDeathMatch, "TDM") && GUI.changed) _gameMode = GameMode.TeamDeathMatch;
        if (GUI.Toggle(new Rect(280, 10, 60, 30), _gameMode == GameMode.TeamElimination, "TE") && GUI.changed) _gameMode = GameMode.TeamElimination;

        GUI.Label(new Rect(10, 40, 150, 30), "Teams:");
        if (GUI.Toggle(new Rect(160, 40, 60, 30), _team == TeamID.NONE, "NONE") && GUI.changed) _team = TeamID.NONE;
        if (GUI.Toggle(new Rect(220, 40, 60, 30), _team == TeamID.RED, "RED") && GUI.changed) _team = TeamID.RED;
        if (GUI.Toggle(new Rect(280, 40, 60, 30), _team == TeamID.BLUE, "BLUE") && GUI.changed) _team = TeamID.BLUE;

        GUI.Label(new Rect(10, 70, 150, 30), "Points:");
        for (int i = 0; i < SpawnPointManager.Instance.GetSpawnPointCount(_gameMode, _team); i++)
        {
            if (GUI.Toggle(new Rect(160 + (30 * i), 70, 30, 20), _spawnPoint == i, "" + (i + 1)) && GUI.changed)
            {
                _spawnPoint = i;
                Respawn();
            }
        }
        if (SpawnPointManager.Instance.GetSpawnPointCount(_gameMode, _team) == 0)
        {
            GUI.Label(new Rect(160, 70, 200, 20), "No points found!");
        }

        if (!Screen.lockCursor)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 30), "CONTINUE"))
            {
                GameState.LocalPlayer.UnPausePlayer();
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            _spawnPoint = (_spawnPoint + 1) % SpawnPointManager.Instance.GetSpawnPointCount(_gameMode, _team);
            Respawn();
        }

        if (_decorator)
        {
            _decorator.transform.position = GameState.LocalCharacter.Position;
            _decorator.transform.rotation = GameState.LocalCharacter.HorizontalRotation;
        }
    }

    private void Respawn()
    {

        Vector3 p;
        Quaternion r;
        SpawnPointManager.Instance.GetSpawnPointAt(_spawnPoint, _gameMode, _team, out p, out r);
        GameState.LocalPlayer.SpawnPlayerAt(p, r);
    }
}