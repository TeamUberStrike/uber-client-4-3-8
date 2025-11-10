using UnityEngine;
using System.Collections;
using UberStrike.Realtime.Common;
using Cmune.DataCenter.Common.Entities;

public class JoinTeamGameGUI : BaseJoinGameGUI
{
    private TeamDeathMatchGameMode _gameMode;

    public JoinTeamGameGUI(TeamDeathMatchGameMode gameMode)
    {
        _gameMode = gameMode;
    }

    public override void Draw(Rect rect)
    {
        float width = Mathf.Min(400, rect.width);
        Vector2 margin = Vector2.zero;

        margin.x = (rect.width - width) / 2;
        margin.y = margin.x + width;

        float marginLeft2 = margin.y - 130;
        int maxPlayersInTeam = Mathf.CeilToInt(_gameMode.GameData.MaxPlayers / 2f);

        DrawPlayers(new Rect(margin.x, 64 + 130, 130, 24), _gameMode.BlueTeamPlayerCount, maxPlayersInTeam, StormFront.DotBlue);

        GUITools.PushGUIState();
        GUI.enabled = _gameMode.IsInitialized && PlayerDataManager.AccessLevel >= MemberAccessLevel.SeniorModerator || (_gameMode.CanJoinBlueTeam && _gameMode.GameData.ConnectedPlayers < _gameMode.GameData.MaxPlayers);

        if (GUITools.Button(new Rect(margin.x, 64, 130, 130), GUIContent.none, StormFront.ButtonJoinBlue))
        {
            GameState.LocalDecorator.HideWeapons();
            GameState.LocalDecorator.MeshRenderer.enabled = false;
            GamePageManager.Instance.UnloadCurrentPage();
            _gameMode.InitializeMode(TeamID.BLUE);
        }

        GUITools.PopGUIState();
        GUITools.PushGUIState();
        GUI.enabled = _gameMode.IsInitialized && _gameMode.CanJoinRedTeam;

        DrawPlayers(new Rect(marginLeft2, 64 + 130, 130, 24), _gameMode.RedTeamPlayerCount, maxPlayersInTeam, StormFront.DotRed);

        if (GUITools.Button(new Rect(marginLeft2, 64, 130, 130), GUIContent.none, StormFront.ButtonJoinRed))
        {
            GameState.LocalDecorator.HideWeapons();
            GameState.LocalDecorator.MeshRenderer.enabled = false;
            GamePageManager.Instance.UnloadCurrentPage();
            _gameMode.InitializeMode(TeamID.RED);
        }

        GUITools.PopGUIState();

        DrawSpectateButton(new Rect((rect.width - 33), (rect.height - 60), 33, 33));
    }
}