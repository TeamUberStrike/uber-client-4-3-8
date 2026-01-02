using System;
using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;

class ValuablePlayerListGUI
{
    private const float HEADER_HEIGHT = 20;

    private readonly float[] _columnWidthPercent = new float[] { 0.7f, 0.1f, 0.1f, 0.1f, };
    private readonly string[] _headingArray = new string[] { "NAME", "KILL", "DEATHS", "LEVEL", };
    private Vector2 _scroll;
    private int _curSelectedPlayerIndex = -1;
    private float _playerListViewHeight = 0;

    public bool Enabled { get; set; }

    public Action<StatsSummary> OnSelectionChange { get; set; }

    public float Height
    {
        get { return HEADER_HEIGHT + _playerListViewHeight + 2; }
    }

    public void ClearSelection()
    {
        _curSelectedPlayerIndex = -1;
    }

    public void Draw(Rect rect)
    {
        GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window);
        {
            Rect headerRect = new Rect(0.0f, 0.0f, rect.width, HEADER_HEIGHT);
            Rect contentRect = new Rect(0.0f, 20.0f, rect.width, rect.height - HEADER_HEIGHT - 1);

            // here it's better to use a class to take care of all the 

            DrawRankingListHeader(headerRect, _columnWidthPercent);
            DrawRankingListContent(contentRect, _columnWidthPercent);
        }
        GUI.EndGroup();
    }

    public void SetSelection(int index)
    {
        _curSelectedPlayerIndex = index;
        OnSelectionChange(EndOfMatchStats.Instance.Data.MostValuablePlayers[_curSelectedPlayerIndex]);
    }

    private void DrawRankingListHeader(Rect rect, float[] columnWidthPercent)
    {
        GUI.BeginGroup(rect);
        {
            float xOffset = 0.0f;
            for (int i = 0; i < _headingArray.Length; i++)
            {
                Rect buttonRect = new Rect(xOffset, 0, rect.width * columnWidthPercent[i], rect.height);
                GUI.Button(buttonRect, string.Empty, BlueStonez.box_grey50);
                GUI.Label(buttonRect, new GUIContent(_headingArray[i]), BlueStonez.label_interparkmed_11pt);
                xOffset += rect.width * columnWidthPercent[i];
            }
        }
        GUI.EndGroup();
    }

    private void DrawRankingListContent(Rect rect, float[] columnWidthPercent)
    {
        float viewWidth = rect.width;
        _playerListViewHeight = EndOfMatchStats.Instance.Data.MostValuablePlayers.Count * 32;

        if (_playerListViewHeight > rect.height)
        {
            viewWidth -= 20;
        }

        _scroll = GUITools.BeginScrollView(rect, _scroll, new Rect(0, 0, viewWidth, _playerListViewHeight));
        {
            float yOffset = 0.0f;

            for (int i = 0; EndOfMatchStats.Instance.Data != null && i < EndOfMatchStats.Instance.Data.MostValuablePlayers.Count; i++)
            {
                DrawStatsSummary(new Rect(0, yOffset, viewWidth, 32), i, columnWidthPercent);

                yOffset += 32f;
            }
        }
        GUITools.EndScrollView();
    }

    private void DrawStatsSummary(Rect rect, int rank, float[] columnWidthPercent)
    {
        StatsSummary stats = EndOfMatchStats.Instance.Data.MostValuablePlayers[rank];

        Color color = Color.white;

        if (stats.Cmid != PlayerDataManager.Cmid)
        {
            if (stats.Team == TeamID.BLUE)
            {
                color = ColorScheme.UberStrikeBlue;
            }
            else if (stats.Team == TeamID.RED)
            {
                color = ColorScheme.UberStrikeRed;
            }
        }

        if (_curSelectedPlayerIndex == rank)
        {
            GUI.Label(rect, GUIContent.none, StormFront.GrayPanelBox);
        }
        else
        {
            GUI.color = new Color(1, 1, 1, 0.5f);
        }

        GUI.BeginGroup(rect);
        {
            float xOffset = 0;
            Vector2 nameSize = BlueStonez.label_interparkbold_18pt_left.CalcSize(new GUIContent(stats.Name));

            GUI.contentColor = color;
            DrawAchivements(new Rect(xOffset + 16 + nameSize.x, 0, rect.width * columnWidthPercent[0] - xOffset - 16 - nameSize.x, 32), stats.Achievements);

            GUI.Label(new Rect(xOffset + 10, 0, rect.width * columnWidthPercent[0], 32), stats.Name, BlueStonez.label_interparkbold_18pt_left);
            xOffset += rect.width * columnWidthPercent[0];
            GUI.Label(new Rect(xOffset, 0, rect.width * columnWidthPercent[1], 32), stats.Kills.ToString(), BlueStonez.label_interparkbold_18pt);
            xOffset += rect.width * columnWidthPercent[1];
            GUI.Label(new Rect(xOffset, 0, rect.width * columnWidthPercent[2], 32), stats.Deaths.ToString(), BlueStonez.label_interparkbold_18pt);
            xOffset += rect.width * columnWidthPercent[2];
            GUI.Label(new Rect(xOffset, 0, rect.width * columnWidthPercent[3], 32), stats.Level.ToString(), BlueStonez.label_interparkbold_18pt);

            GUI.color = Color.white;
            GUI.contentColor = Color.white;
        }
        GUI.EndGroup();

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            if (_curSelectedPlayerIndex != rank)
            {
                SetSelection(rank);
            }
        }
    }

    private void DrawAchivements(Rect rect, Dictionary<byte, ushort> achievements)
    {
        GUI.BeginGroup(rect);
        {
            float xOffset = 0;
            foreach (var a in achievements)
            //foreach (AchievementType a in System.Enum.GetValues(typeof(AchievementType)))
            {
                AchievementType achievement = (AchievementType)a.Key;
                GUI.DrawTexture(new Rect(xOffset, 0, rect.height, rect.height), UberstrikeIcons.GetAchievement(achievement));
                xOffset += rect.height;
            }
        }
        GUI.EndGroup();
    }
}