using System;
using System.Collections;
using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;

class ValuablePlayerDetailGUI
{
    public ValuablePlayerDetailGUI()
    {
        _types = new List<AchievementType>();
    }

    public void SetValuablePlayer(StatsSummary playerStats)
    {
        _curPlayerStats = playerStats;

        _curBadgeTitle = string.Empty;
        _curBadgeText = string.Empty;

        _types.Clear();
        if (playerStats != null)
        {
            foreach (var a in _curPlayerStats.Achievements)
            {
                _types.Add((AchievementType)a.Key);
            }
        }

        MonoRoutine.Start(StartBadgeShow());
    }

    public void StopBadgeShow()
    {
        PreemptiveCoroutineManager.Instance.IncrementId(StartBadgeShow);
    }

    public void Draw(Rect rect)
    {
        GUI.BeginGroup(new Rect(rect.x, rect.y, rect.width, rect.height - 2), GUIContent.none, StormFront.GrayPanelBox);
        {
            DrawPlayerBadge(new Rect((rect.width - 180) / 2, 10, 180, 125));
            DrawStatsDetail(new Rect(0.0f, 140.0f, rect.width, rect.height - 140));
        }
        GUI.EndGroup();
    }

    #region Private

    private void DrawPlayerBadge(Rect rect)
    {
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (_curBadge != null)
        {
            GUI.DrawTexture(rect, _curBadge);
        }
#endif
    }

    private void DrawStatsDetail(Rect rect)
    {
        if (_curPlayerStats != null)
        {
            GUI.BeginGroup(rect);
            {
                GUI.contentColor = ColorScheme.UberStrikeYellow;
                GUI.Label(new Rect(0, 5, rect.width, 20), _curBadgeTitle, BlueStonez.label_interparkbold_16pt);
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(0, 30, rect.width, 20), _curBadgeText, BlueStonez.label_interparkbold_16pt);
                GUI.Label(new Rect(0, 60, rect.width, 20), _curPlayerStats.Name, BlueStonez.label_interparkbold_18pt);
            }
            GUI.EndGroup();
        }
    }

    private IEnumerator StartBadgeShow()
    {
        int coroutineId = PreemptiveCoroutineManager.Instance.IncrementId(StartBadgeShow);
        if (_types.Count > 0 && _curPlayerStats != null && _curPlayerStats.Achievements.Count == _types.Count)
        {
            _curAchievementIndex = 0;
            while (PreemptiveCoroutineManager.Instance.IsCurrent(StartBadgeShow, coroutineId))
            {
                var type = _types[_curAchievementIndex];

                SetCurrentAchievementBadge(type, _curPlayerStats.Achievements[(byte)type]);

                yield return new WaitForSeconds(2.0f);

                if (_types.Count > 0)
                    _curAchievementIndex = ++_curAchievementIndex % _types.Count;
            }
        }
        else
        {
            if (_curPlayerStats != null)
                SetCurrentAchievementBadge(AchievementType.None, Mathf.RoundToInt(Math.Max(_curPlayerStats.Kills, 0) / Math.Max(_curPlayerStats.Deaths, 1f) * 10));
            yield break;
        }
    }

    private void SetCurrentAchievementBadge(AchievementType type, int value)
    {
#if !UNITY_ANDROID && !UNITY_IPHONE
        if (_curBadge != null)
        {
            _curBadge.Stop();
        }

        _curBadge = UberstrikeIcons.GetAchievementBadge(type);
        _curBadge.Play();
#endif

        _curBadgeTitle = UberstrikeIcons.GetAchievementTitle(type);

        switch (type)
        {
            case AchievementType.MostValuable:
                _curBadgeText = string.Format("Best KDR: {0:N1}", value / 10f);
                break;
            case AchievementType.MostAggressive:
                _curBadgeText = string.Format("Total Kills: {0:N0}", value);
                break;
            case AchievementType.SharpestShooter:
                _curBadgeText = string.Format("Critial Strikes: {0:N0}", value);
                break;
            case AchievementType.TriggerHappy:
                _curBadgeText = string.Format("Kills in a row: {0:N0}", value);
                break;
            case AchievementType.HardestHitter:
                _curBadgeText = string.Format("Damage Dealt: {0:N0}", value);
                break;
            case AchievementType.CostEffective:
                _curBadgeText = string.Format("Accuracy: {0:N1}%", value / 10f);
                break;
            default:
                _curBadgeText = string.Format("KDR: {0:N1}", value / 10f);
                break;
        }
    }

    private StatsSummary _curPlayerStats;
    private List<AchievementType> _types;
#if !UNITY_ANDROID && !UNITY_IPHONE
    private MovieTexture _curBadge;
#endif
    private string _curBadgeTitle;
    private string _curBadgeText;
    private int _curAchievementIndex = -1;
    #endregion
}
