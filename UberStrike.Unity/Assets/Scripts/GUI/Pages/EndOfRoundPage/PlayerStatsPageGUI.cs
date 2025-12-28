using UnityEngine;
using Cmune.Util;
using UberStrike.Realtime.Common;

class PlayerStatsPageGUI : PageGUI
{
    public override void DrawGUI(Rect rect)
    {
        GUI.BeginGroup(rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            DrawStats(new Rect(2.0f, 2.0f, rect.width - 4.0f, 168));
            DrawRewards(new Rect(2.0f, 170, rect.width - 4.0f, rect.height * 0.2f));
        }
        GUI.EndGroup();
    }

    private void DrawStats(Rect rect)
    {
        GUI.Button(new Rect(rect.x, rect.y, rect.width, 40), string.Empty, BlueStonez.box_grey50);
        GUI.Label(new Rect(rect.x + 10, rect.y + 2, rect.width, 40), "MY STATUS", BlueStonez.label_interparkbold_18pt_left);

        float width = rect.width;
        float height = rect.height - 40;
        float xpHeight = 32;

        GUI.BeginGroup(new Rect(rect.x, rect.y + 40.0f, rect.width, rect.height - 40.0f), GUIContent.none, BlueStonez.window);
        {
            GUI.BeginGroup(new Rect(0, 0, (width / 2) + 1, height), BlueStonez.group_grey81);
            {
                // kills
                GUI.Label(new Rect(5, xpHeight * 0, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.KillXP, UberstrikeIcons.StatsKills), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 0, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.KillXP, BlueStonez.label_interparkbold_18pt_right);
                // death
                GUI.Label(new Rect(5, xpHeight * 1, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.DeathsCaps, UberstrikeIcons.StatsDeaths), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 1, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.Deaths, BlueStonez.label_interparkbold_18pt_right);
                // KDR
                GUI.Label(new Rect(5, xpHeight * 2, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.KDR, UberstrikeIcons.StatsKDR), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 2, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.KDR, BlueStonez.label_interparkbold_18pt_right);
                // suicides
                GUI.Label(new Rect(5, xpHeight * 3, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.SuicideXP, UberstrikeIcons.StatsSuicides), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 3, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.Suicides, BlueStonez.label_interparkbold_18pt_right);
            }
            GUI.EndGroup();

            GUI.BeginGroup(new Rect(width / 2, 0, width / 2, height), BlueStonez.group_grey81);
            {
                // headshot
                GUI.Label(new Rect(5, xpHeight * 0, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.HeadshotXP, UberstrikeIcons.StatsHeadshots), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 0, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.HeadshotXP, BlueStonez.label_interparkbold_18pt_right);
                // nutshot
                GUI.Label(new Rect(5, xpHeight * 1, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.NutshotXP, UberstrikeIcons.StatsNutshots), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 1, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.NutshotXP, BlueStonez.label_interparkbold_18pt_right);
                // smackdown
                GUI.Label(new Rect(5, xpHeight * 2, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.SmackdownXP, UberstrikeIcons.StatsSmackDown), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 2, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.SmackdownXP, BlueStonez.label_interparkbold_18pt_right);
                // damage
                GUI.Label(new Rect(5, xpHeight * 3, (width / 2) + 1, xpHeight), new GUIContent(LocalizedStrings.DamageXP, UberstrikeIcons.StatsDamage), BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, xpHeight * 3, (width / 2) - 5, xpHeight), EndOfMatchStats.Instance.DamageXP, BlueStonez.label_interparkbold_18pt_right);
            }
            GUI.EndGroup();
        }
        GUI.EndGroup();
    }

    private void DrawRewards(Rect rect)
    {
        GUI.Button(new Rect(rect.x, rect.y, rect.width, 40.0f), string.Empty, BlueStonez.box_grey50);
        GUI.Label(new Rect(rect.x + 10, rect.y + 2, rect.width, 40.0f), "MY REWARDS", BlueStonez.label_interparkbold_18pt_left);

        float height = rect.height - 40;
        GUI.BeginGroup(new Rect(rect.x, rect.y + 40.0f, rect.width, height), GUIContent.none, BlueStonez.window);
        {
            GUI.BeginGroup(new Rect(0, 0, rect.width / 2, height), GUIContent.none, BlueStonez.group_grey81);
            {
                GUI.DrawTexture(new Rect(5, (height - 20) / 2, 20, 20), UberstrikeIcons.XPIcon20x20);
                GUI.Label(new Rect(30, (height - 20) / 2, 200, 20), "XP EARNED", BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, (height - 20) / 2, rect.width / 2 - 5, 20), EndOfMatchStats.Instance.XPEarned, BlueStonez.label_interparkbold_18pt_right);
            }
            GUI.EndGroup();

            GUI.BeginGroup(new Rect(rect.width / 2 - 1, 0, rect.width / 2, height), GUIContent.none, BlueStonez.group_grey81);
            {
                GUI.DrawTexture(new Rect(5, (height - 20) / 2, 20, 20), UberstrikeTextures.IconPoints20x20);
                GUI.Label(new Rect(30, (height - 20) / 2, 200, 20), "POINTS EARNED", BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(0, (height - 20) / 2, rect.width / 2 - 5, 20), EndOfMatchStats.Instance.PointsEarned, BlueStonez.label_interparkbold_18pt_right);
            }
            GUI.EndGroup();
        }
        GUI.EndGroup();
    }
}