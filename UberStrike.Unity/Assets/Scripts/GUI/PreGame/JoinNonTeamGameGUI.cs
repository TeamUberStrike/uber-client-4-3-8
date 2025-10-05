using UnityEngine;
using System.Collections;
using Cmune.DataCenter.Common.Entities;

public class JoinNonTeamGameGUI : BaseJoinGameGUI
{
    private DeathMatchGameMode _gameMode;

    public JoinNonTeamGameGUI(DeathMatchGameMode gameMode)
    {
        _gameMode = gameMode;
    }

    public override void Draw(Rect rect)
    {
        int playerCount = _gameMode.GameData.ConnectedPlayers;
        int maxPlayersInColumn1 = Mathf.Min(8, _gameMode.GameData.MaxPlayers);
        int maxPlayersInColumn2 = _gameMode.GameData.MaxPlayers - maxPlayersInColumn1;

        DrawPlayers(new Rect((rect.width - 130) / 2, 64 + 130, 130, 24), Mathf.Min(maxPlayersInColumn1, playerCount), maxPlayersInColumn1, StormFront.DotBlue);

        if (maxPlayersInColumn2 > 0)
        {
            DrawPlayers(new Rect((rect.width - 130) / 2, 64 + 130 + 18, 130, 24), Mathf.Max(0, playerCount - maxPlayersInColumn1), maxPlayersInColumn2, StormFront.DotBlue);
        }

        GUITools.PushGUIState();
        GUI.enabled = _gameMode.IsInitialized && PlayerDataManager.AccessLevel >= MemberAccessLevel.SeniorModerator || (playerCount < _gameMode.GameData.MaxPlayers);

        if (GUITools.Button(new Rect((rect.width - 130) / 2, 64, 130, 130), GUIContent.none, StormFront.ButtonJoinGray))
        {
            GameState.LocalDecorator.HideWeapons();
            GameState.LocalDecorator.MeshRenderer.enabled = false;
            GamePageManager.Instance.UnloadCurrentPage();
            _gameMode.InitializeMode();
        }

        GUITools.PopGUIState();

        DrawSpectateButton(new Rect((rect.width - 33), (rect.height - 60), 33, 33));
    }
}