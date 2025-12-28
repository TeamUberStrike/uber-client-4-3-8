using UnityEngine;
using System.Collections;
using Cmune.DataCenter.Common.Entities;
using System;

public abstract class BaseJoinGameGUI
{
    public abstract void Draw(Rect rect);

    protected bool DrawJoinButton(Rect rect, string content, GUIStyle style)
    {
        bool result;

        GUI.BeginGroup(rect);
        {
            result = GUI.Button(new Rect(0, 0, rect.width, rect.height), GUIContent.none, style);

            GUI.Label(new Rect(0, rect.height / 2 - 35, rect.width, 30), "JOIN", BlueStonez.label_interparkbold_32pt);
            GUI.Label(new Rect(0, rect.height / 2 + 5, rect.width, 30), content, BlueStonez.label_interparkbold_32pt);
        }
        GUI.EndGroup();

        return result;
    }

    protected void DrawPlayers(Rect rect, int playerCount, int maxPlayerCount, GUIStyle style)
    {
        GUI.BeginGroup(new Rect(rect.x - 1, rect.y, rect.width, rect.height));

        float x = 24;
        float step = -8.857f;// maxPlayerCount <= 1 ? x : (rect.width - x * maxPlayerCount) / (maxPlayerCount - 1);

        for (int i = 0; i < maxPlayerCount; i++)
        {
            GUIStyle playerStyle = (i < playerCount) ? style : StormFront.DotGray;

            GUI.Label(new Rect(i * (x + step), 0, x, x), GUIContent.none, playerStyle);
        }

        GUI.EndGroup();
    }

    protected void DrawSpectateButton(Rect position)
    {
        bool canSpectate = PlayerDataManager.AccessLevel >= MemberAccessLevel.SeniorModerator;

#if UNITY_EDITOR
        canSpectate = true;
#endif

        if (canSpectate && GUITools.Button(position, GUIContent.none, StormFront.ButtonCam))
        {
            GamePageManager.Instance.UnloadCurrentPage();
            GameStateController.Instance.SpectateCurrentGame();
            GameState.LocalDecorator.MeshRenderer.enabled = false;
        }
    }
}