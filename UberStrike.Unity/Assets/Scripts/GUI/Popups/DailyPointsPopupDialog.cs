using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

class DailyPointsPopupDialog : BaseEventPopup
{
    private DailyPointsView _points;

    public DailyPointsPopupDialog(DailyPointsView dailypoints)
    {
        if (dailypoints != null)
        {
            _points = dailypoints;
        }
        else
        {
            _points = new DailyPointsView()
            {
                Current = 700,
                PointsTomorrow = 800,
                PointsMax = 1000,
            };
        }

        Width = 500;
        Height = 330;
    }

    protected override void DrawGUI(UnityEngine.Rect rect)
    {
        //title
        GUI.color = ColorScheme.HudTeamBlue;
        GUI.DrawTexture(new Rect(-50, -20, rect.width + 100, 100), HudTextures.WhiteBlur128);
        GUI.color = Color.white;
        GUITools.OutlineLabel(new Rect(0, 10, rect.width, 50), "Daily Reward", BlueStonez.label_interparkbold_32pt, 1, Color.white, ColorScheme.GuiTeamBlue);

        //image
        int size = 230;
        GUI.DrawTexture(new Rect((rect.width - size) / 2, rect.height - size - 25, size, size), UberstrikeTextures.Points48x48);

        //front line 
        GUI.color = ColorScheme.HudTeamBlue;
        GUI.DrawTexture(new Rect(-50, 25, rect.width + 100, 120), HudTextures.WhiteBlur128);
        GUI.color = Color.white;
        GUITools.OutlineLabel(new Rect(0, 35, rect.width, 100), _points.Current + " POINTS", BlueStonez.label_interparkbold_48pt, 1, Color.white, ColorScheme.GuiTeamBlue);

        //description
        GUITools.OutlineLabel(new Rect(0, rect.height - 50, rect.width, 50), string.Format("Come back tomorrow for {0} points!", GetPointsForTomorrow()), BlueStonez.label_interparkbold_13pt, 1, new Color(0.9f, 0.9f, 0.9f), ColorScheme.GuiTeamBlue.SetAlpha(0.5f));
    }

    int GetPointsForTomorrow()
    {
        return _points.PointsTomorrow;
    }
}