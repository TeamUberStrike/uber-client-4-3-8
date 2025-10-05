using System;
using System.Collections.Generic;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class PlayerSpectatorControl : Singleton<PlayerSpectatorControl>
{
    private PlayerSpectatorControl() { }

    public bool IsEnabled
    {
        get { return _isEnabled; }
        set
        {
            //ENABLE
            if (value)
            {
                EnterFreeMoveMode();

                if (_isEnabled == false)
                {
                    CmuneEventHandler.AddListener<InputChangeEvent>(OnInputChanged);
                    CmuneEventHandler.Route(new OnPlayerSpectatingEvent());
                }
            }
            //DISABLE
            else
            {
                ReleaseLastTarget();

                if (_isEnabled == true)
                {
                    if (GameState.LocalPlayer && GameState.LocalDecorator)
                        GameState.LocalPlayer.SetPlayerControlState(LocalPlayer.PlayerState.FirstPerson, GameState.LocalDecorator.Configuration);

                    CmuneEventHandler.RemoveListener<InputChangeEvent>(OnInputChanged);
                    CmuneEventHandler.Route(new OnPlayerUnspecatingEvent());
                }
            }

            _isEnabled = value;
        }
    }

    public void OnInputChanged(InputChangeEvent ev)
    {
        if (!InGameChatHud.Instance.CanInput && // IF PLAYER IS NOT CHATTING
            Screen.lockCursor)                  // IF PLAYER IS IN GAME
        {
            if (ev.Key == GameInputKey.PrimaryFire && ev.IsDown)
            {
                FollowPrevPlayer();
            }
            else if (ev.Key == GameInputKey.SecondaryFire && ev.IsDown)
            {
                FollowNextPlayer();
            }
            else if (ev.Key == GameInputKey.Jump && ev.IsDown)
            {
                EnterFreeMoveMode();
            }
        }
    }

    public void FollowNextPlayer()
    {
        try
        {
            if (GameState.HasCurrentGame && GameState.CurrentGame.Players.Count > 0)
            {
                CharacterInfo[] players = GameState.CurrentGame.Players.ValueArray();
                _currentFollowIndex = (_currentFollowIndex + 1) % players.Length;

                int nextIndex = _currentFollowIndex;

                //jump over the ignored actorid
                while (players[_currentFollowIndex].ActorId == GameState.CurrentPlayerID
                    || !players[_currentFollowIndex].IsAlive
                    || !GameState.CurrentGame.HasAvatarLoaded(players[_currentFollowIndex].ActorId))
                {
                    _currentFollowIndex = (_currentFollowIndex + 1) % players.Length;

                    if (_currentFollowIndex == nextIndex)
                    {
                        EnterFreeMoveMode();
                        return;
                    }
                }

                if (players[_currentFollowIndex] != null)
                {
                    ChangeTarget(players[_currentFollowIndex].ActorId);
                }
                else
                {
                    EnterFreeMoveMode();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to follow next player: " + ex.Message);
        }
    }

    public void FollowPrevPlayer()
    {
        try
        {
            if (GameState.HasCurrentGame && GameState.CurrentGame.Players.Count > 0)
            {
                List<CharacterInfo> players = new List<CharacterInfo>(GameState.CurrentGame.Players.Values);

                _currentFollowIndex = (_currentFollowIndex + players.Count - 1) % players.Count;

                int nextIndex = _currentFollowIndex;

                //jump over the ignored actorid
                while (players[_currentFollowIndex].ActorId == GameState.CurrentPlayerID
                    || !players[_currentFollowIndex].IsAlive
                    || !GameState.CurrentGame.HasAvatarLoaded(players[_currentFollowIndex].ActorId))
                {
                    _currentFollowIndex = (_currentFollowIndex + players.Count - 1) % players.Count;

                    if (_currentFollowIndex == nextIndex)
                    {
                        EnterFreeMoveMode();
                        return;
                    }
                }

                if (players[_currentFollowIndex] != null)
                {
                    ChangeTarget(players[_currentFollowIndex].ActorId);
                }
                else
                {
                    EnterFreeMoveMode();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to follow prev player: " + ex.Message);
        }
    }

    public void ReleaseLastTarget()
    {
        if (GameState.HasCurrentGame)
        {
            CharacterConfig cfg;
            if (_currentFollowActorId > 0 && GameState.CurrentGame.TryGetCharacter(_currentFollowActorId, out cfg))
            {
                cfg.RemoveFollowCamera();
            }
        }

        _currentFollowActorId = 0;
    }

    private void ChangeTarget(int actorId)
    {
        //Debug.Log("ChangeTarget " + actorId);

        if (GameState.HasCurrentGame && _currentFollowActorId != actorId)
        {
            ReleaseLastTarget();

            CharacterConfig cfg;
            if (GameState.CurrentGame.TryGetCharacter(actorId, out cfg) && cfg.Decorator)
            {
                _currentFollowActorId = actorId;

                LevelCamera.Instance.SetTarget(cfg.Decorator.transform);
                LevelCamera.Instance.SetMode(LevelCamera.CameraMode.SmoothFollow);

                if (!cfg.State.IsAlive)
                    LevelCamera.Instance.transform.position = cfg.State.Position;

                LevelCamera.Instance.SetLookAtHeight(1.3f);

                cfg.AddFollowCamera();
                cfg.Decorator.HudInformation.ForceShowInformation = true;
            }
        }
    }

    public void EnterFreeMoveMode()
    {
        ReleaseLastTarget();

        Screen.lockCursor = true;

        LevelCamera.Instance.SetLookAtHeight(0);
        LevelCamera.Instance.SetMode(LevelCamera.CameraMode.Spectator);
    }

    public int CurrentActorId
    {
        get { return _currentFollowActorId; }
    }

    public bool IsFollowingPlayer
    {
        get { return _currentFollowActorId > 0; }
    }

    #region Fields
    private int _currentFollowActorId;
    private int _currentFollowIndex = 0;
    private bool _isEnabled = false;
    #endregion
}