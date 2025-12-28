using UnityEngine;

public static class ColorScheme
{
    public static readonly Color ProgressBar = new Color(0, 0.607f, 0.662f);//ColorConverter.RgbToColor(109, 165, 0);

    public static readonly Color HudTeamRed = ColorConverter.RgbToColor(206, 87, 87);
    public static readonly Color HudTeamBlue = ColorConverter.RgbToColor(76, 127, 216);

    public static readonly Color GuiTeamRed = new Color(0.929f, 0.270f, 0.270f);
    public static readonly Color GuiTeamBlue = new Color(0.176f, 0.643f, 0.792f);

    public static readonly Color ChatNameCurrentUser = new Color(0, 0.85f, 1);
    public static readonly Color ChatNameFriendsUser = new Color(0, 0.8f, 0);
    public static readonly Color ChatNameAdminUser = new Color(0.8f, 0, 0);
    public static readonly Color ChatNameModeratorUser = new Color(0.9f, 0.7f, 0);
    public static readonly Color ChatNameOtherUser = new Color(0.6f, 0.6f, 0.6f);

    public static readonly Color UberStrikeYellow = new Color(0.87f, 0.64f, 0.035f);
    public static readonly Color UberStrikeBlue = new Color(0.176f, 0.643f, 0.792f);
    public static readonly Color UberStrikeRed = new Color(0.929f, 0.270f, 0.270f);

    public static readonly Color CheatWarningRed = ColorConverter.RgbToColor(183, 48, 48);

    public static readonly Color XPColor = ColorConverter.RgbToColor(255, 127, 0);
    public static readonly Color TeamOutline = Color.white;//ColorConverter.RgbToColor(0, 128, 0);

}