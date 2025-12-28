using System;
using UberStrike.Realtime.Common;
using UnityEngine;
using System.Text;

public class GameState : MonoSingleton<GameState>
{
    public static LocalPlayer LocalPlayer { get { return Instance._player; } }
    public static CharacterInfo LocalCharacter { get { return _localCharacter; } }
    public static AvatarDecorator LocalDecorator { get; set; }
    public static bool IsShuttingDown { get; private set; }
    public Vector2 Offset;

    public static bool IsRagdollShootable = false;

    public Vector2 TouchLookSensitivity = new Vector2(1.0f, 0.5f);

    public static bool UsePlayerPing
    {
        get { return true; }
    }

    public static event Action DrawGizmos;
    public const float InterpolationFactor = 10;

    #region Fields

    [SerializeField]
    LocalPlayer _player;

    private static FpsGameMode _currentGameMode;
    private static MapConfiguration _currentSpace;
    private static CharacterInfo _localCharacter = new CharacterInfo();
    #endregion

    private void FixedUpdate()
    {
        if (CurrentGame != null)
        {
            CurrentGame.FixedUpdate();
        }
    }

    private void Update()
    {
        if (CurrentGame != null)
        {
            CurrentGame.Update();
        }

        GameStateController.Instance.StateMachine.Update();
    }

    void OnGUI()
    {
        GameStateController.Instance.StateMachine.OnGUI();

    }

    private void OnDrawGizmos()
    {
        if (DrawGizmos != null)
        {
            DrawGizmos();
        }
    }

    private void OnApplicationQuit()
    {
        IsShuttingDown = true;
    }

    public static void SetCurrentSpace(MapConfiguration space)
    {
        if (_currentSpace)
        {
            _currentSpace.SetEnabled(false);
        }

        _currentSpace = space;
        _currentSpace.SetEnabled(true);
    }

    public static bool IsReadyForNextGame { get; set; }

    public static Transform WeaponCameraTransform
    {
        get { return LocalPlayer.WeaponCamera.transform; }
    }

    public static int CurrentPlayerID
    {
        get { return LocalCharacter != null ? LocalCharacter.ActorId : 0; }
    }

    public static bool HasCurrentPlayer
    {
        get { return Exists && Instance._player != null; }
    }

    public static GameMode CurrentGameMode
    {
        get { return HasCurrentGame ? CurrentGame.GameMode : GameMode.None; }
    }

    public static bool IsSinglePlayer
    {
        get { return !GameStateController.IsMultiplayerGameMode((int)CurrentGameMode); }
    }

    public static FpsGameMode CurrentGame
    {
        get { return _currentGameMode; }
        set
        {
            if (_currentGameMode != null)
                _currentGameMode.Dispose();

            _currentGameMode = value;
        }
    }

    public static bool HasCurrentGame
    {
        get
        {
            return _currentGameMode != null;
        }
    }

    public static MapConfiguration MenuSpace
    {
        get { return LevelManager.Instance.GetMapWithId(0).Space; }
    }

    public static MapConfiguration CurrentSpace
    {
        get { return _currentSpace; }
    }

    public static bool HasCurrentSpace
    {
        get { return _currentSpace != null; }
    }
}