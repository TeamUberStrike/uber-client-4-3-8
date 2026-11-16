using System.Collections.Generic;
using UberStrike.Realtime.Common;
using UnityEngine;
using System;

public class TabScreenPanelGUI : MonoSingleton<TabScreenPanelGUI>
{
    private void Awake()
    {
        ForceShow = false;

        _rect = new Rect();

        _panelSize.x = 700;
        _panelSize.y = 400;

        _allPlayers = new List<UberStrike.Realtime.Common.CharacterInfo>(0);
        _redTeam = new List<UberStrike.Realtime.Common.CharacterInfo>(0);
        _blueTeam = new List<UberStrike.Realtime.Common.CharacterInfo>(0);
    }

    private void Update()
    {
        // Laid out in menu units (the scaled space DrawTabScreen runs in), so rows and text keep the
        // menu's size on phones. On PC menu units are screen pixels and the layout is as before.
        float virtualWidth = MobileMenuScale.VirtualWidth;
        float virtualHeight = MobileMenuScale.VirtualHeight;
        bool ribbon = GlobalUIRibbon.Instance.IsEnabled;

        if (MobileMenuScale.Active)
        {
            // Phones only get 540 x 960 units: use the full width so the team columns keep their
            // deaths / KDR cells, and all the height between the ribbon and the bottom edge so a
            // full team of eight fits without scrolling.
            float top = ribbon ? GlobalUIRibbon.Instance.GetHeight() + MobileMargin : MobileMargin;
            _panelSize.x = Mathf.Floor(virtualWidth - 2 * MobileMargin);
            _panelSize.y = Mathf.Floor(virtualHeight - top - MobileMargin);

            _rect.x = (virtualWidth - _panelSize.x) * 0.5f;
            _rect.y = top;
        }
        else
        {
            _panelSize.x = Mathf.Floor(virtualWidth * 7 / 8);
            _panelSize.y = Mathf.Floor(virtualHeight * 8 / 9);

            _rect.x = (virtualWidth - _panelSize.x) * 0.5f;
            _rect.y = (virtualHeight - _panelSize.y) * 0.5f;

            if (ribbon)
                _rect.y += 45;
        }

        _rect.width = _panelSize.x;
        _rect.height = _panelSize.y;
    }

    private void OnGUI()
    {
        if (!GameState.HasCurrentGame || !GameState.HasCurrentPlayer) return;

        bool showTabScreen =
            (InputManager.Instance.RawValue(GameInputKey.Tabscreen) > 0 || ForceShow) &&
            !InGameChatPageGUI.IsOpen &&
            !PanelManager.IsAnyPanelOpen &&
            GameState.CurrentGame.CanShowTabscreen;

        if (Enabled != showTabScreen)
        {
            Enabled = showTabScreen;
            if (showTabScreen && SortPlayersByRank != null)
            {
                SortPlayersByRank(GameState.CurrentGame.Players.Values);
            }
        }

        if (Enabled)
        {
            //remove 1 to display even on top of end of the match screen
            GUI.depth = (int)GuiDepth.Page - 1;

            // Same scale and safe-area shift as the menus; no-op on PC.
            Matrix4x4 scaleMatrix = MobileMenuScale.Begin();
            DrawTabScreen();
            MobileMenuScale.End(scaleMatrix);
        }
    }

    ////////////////////////////////////////////////////////

    public Action<IEnumerable<UberStrike.Realtime.Common.CharacterInfo>> SortPlayersByRank { get; set; }

    public void SetGameName(string name)
    {
        _gameName = name;
    }

    public void SetServerName(string name)
    {
        _serverName = name;
    }

    public void SetTeamSplats(int blueSplats, int redSplats)
    {
        _redTeamScore = redSplats;
        _blueTeamScore = blueSplats;
    }

    private void DrawTabScreen()
    {
        GUI.skin = BlueStonez.Skin;

        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.label_interparkmed_11pt);
        {
            DoTitle(new Rect(0, 0, _panelSize.x, 50));

            bool myStats = false;
            int offset = myStats ? 174 : 60;

            switch (GameState.CurrentGameMode)
            {
                case GameMode.TeamDeathMatch:
                    DoTeamStats(new Rect(0, 46, _panelSize.x, _panelSize.y - offset));
                    break;

                case GameMode.TeamElimination:
                    DoTeamStats(new Rect(0, 46, _panelSize.x, _panelSize.y - offset));
                    break;

                default:
                    _scrollPos = DoAllStats(new Rect(0, 46, _panelSize.x, _panelSize.y - offset), _scrollPos, _allPlayers);
                    break;
            }
        }
        GUI.EndGroup();
    }

    private void DoTitle(Rect position)
    {
        GUI.BeginGroup(position, GUIContent.none, BlueStonez.box_overlay);
        {
            // Name of current game
            GUI.Label(new Rect(10, 2, position.width - 230, 30), LocalizedStrings.Game + ": ", BlueStonez.label_interparkbold_18pt_left);
            GUI.contentColor = ColorScheme.UberStrikeYellow;
            GUI.Label(new Rect(65, 2, position.width - 280, 30), _gameName, BlueStonez.label_interparkbold_18pt_left);
            GUI.contentColor = Color.white;

            // Name of Server
            GUI.Label(new Rect(10, position.height - 32, position.width - 230, 30), "Server: " + _serverName, BlueStonez.label_interparkbold_18pt_left);

            // Name of current map
            GUI.Label(new Rect(position.width - 230, 2, 230, 30), MapName, BlueStonez.label_interparkbold_18pt_left);

            // Name of current mode
            GUI.Label(new Rect(position.width - 230, position.height - 32, 220, 30), ModeName, BlueStonez.label_interparkbold_18pt_left);
        }
        GUI.EndGroup();
    }

    private string MapName
    {
        get
        {
            return LocalizedStrings.Map + ": " + LevelManager.Instance.GetMapName(GameState.CurrentSpace.MapId);
        }
    }

    private string ModeName
    {
        get
        {
            return LocalizedStrings.Mode + ": " + GameModes.GetModeName(GameState.CurrentGameMode);
        }
    }

    private void DoTeamStats(Rect position)
    {
        Rect leftPos = new Rect(position.x, position.y, position.width * 0.5f, position.height);
        Rect rightPos = new Rect(position.x + leftPos.width, leftPos.y, leftPos.width, leftPos.height);

        _blueScrollPos = DoTeam(leftPos, TeamID.BLUE, _blueScrollPos, _blueTeam);
        _redScrollPos = DoTeam(rightPos, TeamID.RED, _redScrollPos, _redTeam);
    }

    private Vector2 DoTeam(Rect position, TeamID teamID, Vector2 scroll, List<UberStrike.Realtime.Common.CharacterInfo> players)
    {
        GUI.BeginGroup(position);
        {
            Color tmp = GUI.contentColor;

            GUI.BeginGroup(new Rect(0, 0, position.width, 60), GUIContent.none, BlueStonez.box_overlay);
            {
                GUI.color = (teamID == TeamID.BLUE) ? ColorScheme.HudTeamBlue : ColorScheme.HudTeamRed;

                if (teamID == TeamID.RED)
                {
                    GUI.Label(new Rect(10, 6, 200, 32), teamID.ToString(), BlueStonez.label_interparkbold_32pt_left);
                    GUI.Label(new Rect(10, 34, 200, 18), string.Format(LocalizedStrings.NPlayers, players.Count), BlueStonez.label_interparkbold_18pt_left);

                    if (GameState.CurrentGameMode == GameMode.TeamElimination)
                        GUI.Label(new Rect(position.width - 215, 8, 200, 48), string.Format("{0} / {1}", _redTeamScore, GameState.CurrentGame.GameData.SplatLimit), BlueStonez.label_interparkbold_48pt_right);
                    else
                        GUI.Label(new Rect(position.width - 215, 8, 200, 48), _redTeamScore.ToString(), BlueStonez.label_interparkbold_48pt_right);
                }
                else
                {
                    if (GameState.CurrentGameMode == GameMode.TeamElimination)
                        GUI.Label(new Rect(15, 8, 200, 48), string.Format("{0} / {1}", _blueTeamScore, GameState.CurrentGame.GameData.SplatLimit), BlueStonez.label_interparkbold_48pt_left);
                    else
                        GUI.Label(new Rect(15, 8, 200, 48), _blueTeamScore.ToString(), BlueStonez.label_interparkbold_48pt_left);

                    GUI.Label(new Rect(position.width - 210, 6, 200, 32), teamID.ToString(), BlueStonez.label_interparkbold_32pt_right);
                    GUI.Label(new Rect(position.width - 210, 34, 200, 18), string.Format(LocalizedStrings.NPlayers, players.Count), BlueStonez.label_interparkbold_18pt_right);
                }
            }
            GUI.EndGroup();

            GUI.color = tmp;

            scroll = DoAllStats(new Rect(0, 56, position.width, position.height - 56), scroll, players);
        }
        GUI.EndGroup();

        return scroll;
    }

    public void SetPlayerListAll(List<UberStrike.Realtime.Common.CharacterInfo> players)
    {
        _allPlayers = players;
    }

    private Vector2 DoAllStats(Rect position, Vector2 scroll, List<UberStrike.Realtime.Common.CharacterInfo> players)
    {
        // Touch devices get taller rows and larger text: at a phone's menu scale the 11pt rows of the
        // PC layout are too small to read. The PC values are the original ones.
        bool large = MobileMenuScale.Active;
        if (large) EnsureLargeStyles();

        GUIStyle smallHeaderStyle = large ? _largeSmallHeader : BlueStonez.label_interparkmed_11pt_left;
        GUIStyle nameStyle = large ? _largeName : BlueStonez.label_interparkbold_11pt_left_wrap;
        GUIStyle valueStyle = large ? _largeValue : BlueStonez.label_interparkbold_11pt_left;
        GUIStyle valueRightStyle = large ? _largeValueRight : BlueStonez.label_interparkbold_11pt_right;
        GUIStyle weaponNameStyle = large ? _largeWeaponName : BlueStonez.label_interparkbold_11pt_left;

        int rowHeight = large ? 44 : 36;
        int headerHeight = large ? 44 : 36;     // top of the list below the column titles
        int titleY = large ? 12 : 10;           // "Name" / "Ping"
        int smallTitleY = large ? 18 : 15;      // "Kills" / "Deaths" / ...
        int iconSize = large ? 20 : 16;
        int deadIconSize = large ? 28 : 25;
        int weaponIconSize = rowHeight - 4;

        int offset = 8;
        int iconWidth = large ? 28 : 25;
        int levelWidth = large ? 34 : 25;
        int splatsWidth = large ? 40 : 30;
        int weaponWidth = large ? 40 : 32;
        int descriptionWidth = position.width > (large ? 640 : 540) ? (large ? 180 : 150) : 0;
        int killedWidth = position.width > (large ? 440 : 420) ? (large ? 46 : 50) : 0;
        int kdrWidth = position.width > (large ? 440 : 450) ? (large ? 42 : 30) : 0;
        int pointsWidth = !large && position.width > 490 ? 40 : 0;
        int deadWidth = large ? 32 : 30;
        int pingWidth = large ? 56 : 50;
        int nameWidth = Mathf.Clamp(Mathf.RoundToInt(position.width - 30 - offset - iconWidth - levelWidth - splatsWidth - weaponWidth - descriptionWidth - deadWidth - pingWidth - killedWidth - kdrWidth - pointsWidth), 110, large ? 340 : 300);

        GUI.BeginGroup(position, GUIContent.none, BlueStonez.box_overlay);
        {
            int x = 10 + offset + iconWidth;

            GUI.Label(new Rect(x, titleY, nameWidth, 18), LocalizedStrings.Name, BlueStonez.label_interparkmed_18pt_left);
            x += nameWidth;

            GUI.Label(new Rect(x, smallTitleY, splatsWidth, 18), LocalizedStrings.Kills, smallHeaderStyle);
            x += splatsWidth;

            if (killedWidth > 0)
            {
                GUI.Label(new Rect(x, smallTitleY, killedWidth, 18), LocalizedStrings.Deaths, smallHeaderStyle);
                x += killedWidth;
            }

            if (kdrWidth > 0)
            {
                GUI.Label(new Rect(x, smallTitleY, kdrWidth, 18), LocalizedStrings.KDR, smallHeaderStyle);
                x += kdrWidth;
            }

            //if (pointsWidth > 0)
            //{
            //    GUI.Label(new Rect(x, 15, pointsWidth, 18), LocalizedStrings.Bounty, BlueStonez.label_interparkmed_11pt_left);
            //    x += pointsWidth;
            //}

            GUI.Label(new Rect(x, titleY, deadWidth, 18), GUIContent.none, BlueStonez.label_interparkbold_16pt_left);
            x += deadWidth;

            GUI.Label(new Rect(x, smallTitleY, levelWidth + 10, 18), LocalizedStrings.Level, smallHeaderStyle);
            x += levelWidth;

            GUI.Label(new Rect(x, titleY, weaponWidth + descriptionWidth, 18), GUIContent.none, BlueStonez.label_interparkbold_16pt_left);
            x += weaponWidth + descriptionWidth;

            GUI.Label(new Rect(position.width - pingWidth, titleY, pingWidth, 18), LocalizedStrings.Ping, BlueStonez.label_interparkmed_18pt_left);

            // Seperating line
            GUI.Label(new Rect(10, headerHeight - 4, position.width - 20, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);

            scroll = GUI.BeginScrollView(new Rect(10, headerHeight, position.width - 20, position.height - headerHeight - 9), scroll, new Rect(0, 0, position.width - 40, players.Count * rowHeight));
            {
                int i = 0;

                foreach (UberStrike.Realtime.Common.CharacterInfo ui in players)
                {
                    x = offset;
                    GUI.BeginGroup(new Rect(0, i * rowHeight, position.width, rowHeight));
                    {
                        if (ui.ActorId == GameState.CurrentPlayerID)
                        {
                            GUI.color = new Color(1, 1, 1, 0.3f);
                            GUI.Box(new Rect(0, 0, position.width - 21, rowHeight), GUIContent.none, BlueStonez.box_white_rounded);
                            GUI.color = Color.white;
                        }

                        // icon
                        GUI.DrawTexture(new Rect(x, (rowHeight - iconSize) / 2, iconSize, iconSize), UberstrikeIcons.GetIconForChannel(ui.Channel));
                        x += iconWidth;

                        // name
                        Color tmp = GUI.contentColor;
                        GUI.color = Color.white;
                        if (!GameState.CurrentGame.HasAvatarLoaded(ui.ActorId)) GUI.color = Color.gray; //currently only spectator
                        else if (ui.TeamID == TeamID.BLUE) GUI.color = ColorScheme.HudTeamBlue;
                        else if (ui.TeamID == TeamID.RED) GUI.color = ColorScheme.HudTeamRed;

                        GUI.Label(new Rect(x, 0, nameWidth, rowHeight), ui.PlayerName, nameStyle);
                        GUI.color = tmp;
                        x += nameWidth;

                        // kills
                        GUI.Label(new Rect(x, 0, splatsWidth, rowHeight), ui.Kills.ToString(), valueStyle);
                        x += splatsWidth;

                        if (killedWidth > 0)
                        {
                            GUI.Label(new Rect(x, 0, killedWidth, rowHeight), ui.Deaths.ToString("N0"), valueStyle);
                            x += killedWidth;
                        }
                        if (kdrWidth > 0)
                        {
                            GUI.Label(new Rect(x, 0, kdrWidth, rowHeight), ui.KDR.ToString("N1"), valueStyle);
                            x += kdrWidth;
                        }
                        //if (pointsWidth > 0)
                        //{
                        //    int points = ui.GetPointBonus();
                        //    for (int j = 0; j < points; j++)
                        //        GUI.Label(new Rect(x + j * 5, 10, pointsWidth, 16), UberstrikeIcons.PointsIcon20x20, BlueStonez.label_interparkbold_11pt_left);
                        //    x += pointsWidth;
                        //}

                        // death icon
                        if (!ui.IsAlive)
                            GUI.Label(new Rect(x, large ? 8 : 6, deadIconSize, deadIconSize), _iconSplatted, BlueStonez.label_interparkbold_11pt_right);
                        x += deadWidth;

                        // level
                        //if (!isMinimal)
                        GUI.Label(new Rect(x + 5, 0, levelWidth, rowHeight), ui.Level.ToString(), valueStyle);
                        x += levelWidth + 5;

                        //weapon
                        WeaponItem weapon = ItemManager.Instance.GetWeaponItemInShop(ui.CurrentWeaponID);
                        if (weapon != null)
                        {
                            // weapon icon
                            GUI.Label(new Rect(x, 2, weaponIconSize, weaponIconSize), weapon.Icon, BlueStonez.box_black);
                            x += weaponWidth;

                            // weapon name
                            if (descriptionWidth > 0)
                            {
                                GUI.Label(new Rect(x + 10, 0, descriptionWidth, rowHeight), weapon.Name, weaponNameStyle);
                                x += descriptionWidth;
                            }
                        }
                        else
                        {
                            x += weaponWidth;
                        }

                        // ping
                        GUI.Label(new Rect(position.width - 40 - pingWidth, 0, pingWidth, rowHeight), ui.Ping.ToString(), valueRightStyle);
                    }
                    GUI.EndGroup();
                    i++;
                }
            }
            GUI.EndScrollView();
        }
        GUI.EndGroup();

        return scroll;
    }

    // Row styles for touch devices: the 16pt / 13pt fonts with the colours and alignment of the 11pt
    // styles the PC rows use.
    private static void EnsureLargeStyles()
    {
        if (_largeValue != null) return;

        _largeValue = new GUIStyle(BlueStonez.label_interparkbold_16pt_left);
        _largeValue.normal.textColor = BlueStonez.label_interparkbold_11pt_left.normal.textColor;

        _largeValueRight = new GUIStyle(BlueStonez.label_interparkbold_16pt_right);
        _largeValueRight.normal.textColor = BlueStonez.label_interparkbold_11pt_right.normal.textColor;

        // Names stay on one line and are cut at the column edge instead of wrapping.
        _largeName = new GUIStyle(BlueStonez.label_interparkbold_16pt_left);
        _largeName.normal.textColor = BlueStonez.label_interparkbold_11pt_left_wrap.normal.textColor;
        _largeName.wordWrap = false;
        _largeName.clipping = TextClipping.Clip;

        _largeSmallHeader = new GUIStyle(BlueStonez.label_interparkbold_13pt_left);
        _largeSmallHeader.alignment = BlueStonez.label_interparkmed_11pt_left.alignment;
        _largeSmallHeader.wordWrap = false;

        _largeWeaponName = new GUIStyle(BlueStonez.label_interparkbold_13pt_left);
        _largeWeaponName.wordWrap = false;
        _largeWeaponName.clipping = TextClipping.Clip;
    }

    public void SetPlayerListRed(List<UberStrike.Realtime.Common.CharacterInfo> redPlayers)
    {
        _redTeam = redPlayers;
    }

    public void SetPlayerListBlue(List<UberStrike.Realtime.Common.CharacterInfo> bluePlayers)
    {
        _blueTeam = bluePlayers;
    }

    #region Properties

    public static bool Enabled
    {
        get { return _isEnabled; }
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                HudDrawFlagGroup.Instance.TuningTabScreen = value;
            }
        }
    }

    public int BluePlayerCount
    {
        get { return _blueTeam.Count; }
    }

    public int RedPlayerCount
    {
        get { return _redTeam.Count; }
    }

    public bool ForceShow { get; set; }

    #endregion

    #region Fields

    [SerializeField]
    private Texture2D _iconSplatted;

    // Gap (menu units) between the panel and the screen edges / ribbon on mobile.
    private const float MobileMargin = 8f;

    private static GUIStyle _largeName;
    private static GUIStyle _largeValue;
    private static GUIStyle _largeValueRight;
    private static GUIStyle _largeSmallHeader;
    private static GUIStyle _largeWeaponName;

    private Rect _rect;
    private Vector2 _panelSize;

    private string _gameName = string.Empty;
    private string _serverName = string.Empty;

    private Vector2 _scrollPos;
    private Vector2 _redScrollPos;
    private Vector2 _blueScrollPos;

    private int _redTeamScore;
    private int _blueTeamScore;

    private List<UberStrike.Realtime.Common.CharacterInfo> _redTeam;
    private List<UberStrike.Realtime.Common.CharacterInfo> _blueTeam;
    private List<UberStrike.Realtime.Common.CharacterInfo> _allPlayers;

    private static bool _isEnabled = false;
    #endregion
}