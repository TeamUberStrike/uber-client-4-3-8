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
        _panelSize.x = Screen.width * 7 / 8;
        _panelSize.y = Screen.height * 8 / 9;

        _rect.x = (Screen.width - _panelSize.x) * 0.5f;
        _rect.y = (Screen.height - _panelSize.y) * 0.5f;
        _rect.width = _panelSize.x;
        _rect.height = _panelSize.y;

        if (GlobalUIRibbon.Instance.IsEnabled)
            _rect.y += 45;
    }

    private void OnGUI()
    {
        if (!GameState.HasCurrentGame || !GameState.HasCurrentPlayer) return;

        bool showTabScreen =
            (InputManager.Instance.RawValue(GameInputKey.Tabscreen) > 0 || ForceShow) &&
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

            DrawTabScreen();
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
        int offset = 8;
        int iconWidth = 25;
        int levelWidth = 25;
        int splatsWidth = 30;
        int weaponWidth = 32;
        int descriptionWidth = position.width > 540 ? 150 : 0;
        int killedWidth = position.width > 420 ? 50 : 0;
        int kdrWidth = position.width > 450 ? 30 : 0;
        int pointsWidth = position.width > 490 ? 40 : 0;
        int deadWidth = 30;
        int pingWidth = 50;
        int nameWidth = Mathf.Clamp(Mathf.RoundToInt(position.width - 30 - offset - iconWidth - levelWidth - splatsWidth - weaponWidth - descriptionWidth - deadWidth - pingWidth - killedWidth - kdrWidth - pointsWidth), 110, 300);

        GUI.BeginGroup(position, GUIContent.none, BlueStonez.box_overlay);
        {
            int x = 10 + offset + iconWidth;

            GUI.Label(new Rect(x, 10, nameWidth, 18), LocalizedStrings.Name, BlueStonez.label_interparkmed_18pt_left);
            x += nameWidth;

            GUI.Label(new Rect(x, 15, splatsWidth, 18), LocalizedStrings.Kills, BlueStonez.label_interparkmed_11pt_left);
            x += splatsWidth;

            if (killedWidth > 0)
            {
                GUI.Label(new Rect(x, 15, killedWidth, 18), LocalizedStrings.Deaths, BlueStonez.label_interparkmed_11pt_left);
                x += killedWidth;
            }

            if (kdrWidth > 0)
            {
                GUI.Label(new Rect(x, 15, kdrWidth, 18), LocalizedStrings.KDR, BlueStonez.label_interparkmed_11pt_left);
                x += kdrWidth;
            }

            //if (pointsWidth > 0)
            //{
            //    GUI.Label(new Rect(x, 15, pointsWidth, 18), LocalizedStrings.Bounty, BlueStonez.label_interparkmed_11pt_left);
            //    x += pointsWidth;
            //}

            GUI.Label(new Rect(x, 10, deadWidth, 18), GUIContent.none, BlueStonez.label_interparkbold_16pt_left);
            x += deadWidth;

            GUI.Label(new Rect(x, 15, levelWidth + 10, 18), LocalizedStrings.Level, BlueStonez.label_interparkmed_11pt_left);
            x += levelWidth;

            GUI.Label(new Rect(x, 10, weaponWidth + descriptionWidth, 18), GUIContent.none, BlueStonez.label_interparkbold_16pt_left);
            x += weaponWidth + descriptionWidth;

            GUI.Label(new Rect(position.width - pingWidth, 10, pingWidth, 18), LocalizedStrings.Ping, BlueStonez.label_interparkmed_18pt_left);

            // Seperating line
            GUI.Label(new Rect(10, 32, position.width - 20, 1), GUIContent.none, BlueStonez.horizontal_line_grey95);

            scroll = GUITools.BeginScrollView(new Rect(10, 36, position.width - 20, position.height - 45), scroll, new Rect(0, 0, position.width - 40, players.Count * 36));
            {
                int i = 0;

                foreach (UberStrike.Realtime.Common.CharacterInfo ui in players)
                {
                    x = offset;
                    GUI.BeginGroup(new Rect(0, i * 36, position.width, 36));
                    {
                        if (ui.ActorId == GameState.CurrentPlayerID)
                        {
                            GUI.color = new Color(1, 1, 1, 0.3f);
                            GUI.Box(new Rect(0, 0, position.width - 21, 36), GUIContent.none, BlueStonez.box_white_rounded);
                            GUI.color = Color.white;
                        }

                        // icon
                        GUI.DrawTexture(new Rect(x, 10, 16, 16), UberstrikeIcons.GetIconForChannel(ui.Channel));
                        x += iconWidth;

                        // name
                        Color tmp = GUI.contentColor;
                        GUI.color = Color.white;
                        if (!GameState.CurrentGame.HasAvatarLoaded(ui.ActorId)) GUI.color = Color.gray; //currently only spectator
                        else if (ui.TeamID == TeamID.BLUE) GUI.color = ColorScheme.HudTeamBlue;
                        else if (ui.TeamID == TeamID.RED) GUI.color = ColorScheme.HudTeamRed;

                        GUI.Label(new Rect(x, 0, nameWidth, 36), ui.PlayerName, BlueStonez.label_interparkbold_11pt_left_wrap);
                        GUI.color = tmp;
                        x += nameWidth;

                        // kills
                        GUI.Label(new Rect(x, 0, splatsWidth, 36), ui.Kills.ToString(), BlueStonez.label_interparkbold_11pt_left);
                        x += splatsWidth;

                        if (killedWidth > 0)
                        {
                            GUI.Label(new Rect(x, 0, killedWidth, 36), ui.Deaths.ToString("N0"), BlueStonez.label_interparkbold_11pt_left);
                            x += killedWidth;
                        }
                        if (kdrWidth > 0)
                        {
                            GUI.Label(new Rect(x, 0, kdrWidth, 36), ui.KDR.ToString("N1"), BlueStonez.label_interparkbold_11pt_left);
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
                            GUI.Label(new Rect(x, 6, 25, 25), _iconSplatted, BlueStonez.label_interparkbold_11pt_right);
                        x += deadWidth;

                        // level
                        //if (!isMinimal)
                        GUI.Label(new Rect(x + 5, 0, levelWidth, 36), ui.Level.ToString(), BlueStonez.label_interparkbold_11pt_left);
                        x += levelWidth + 5;

                        //weapon
                        WeaponItem weapon = ItemManager.Instance.GetWeaponItemInShop(ui.CurrentWeaponID);
                        if (weapon != null)
                        {
                            // weapon icon
                            GUI.Label(new Rect(x, 2, 32, 32), weapon.Icon, BlueStonez.box_black);
                            x += weaponWidth;

                            // weapon name
                            if (descriptionWidth > 0)
                            {
                                GUI.Label(new Rect(x + 10, 0, descriptionWidth, 36), weapon.Name, BlueStonez.label_interparkbold_11pt_left);
                                x += descriptionWidth;
                            }
                        }
                        else
                        {
                            x += weaponWidth;
                        }

                        // ping
                        GUI.Label(new Rect(position.width - 40 - pingWidth, 0, pingWidth, 36), ui.Ping.ToString(), BlueStonez.label_interparkbold_11pt_right);
                    }
                    GUI.EndGroup();
                    i++;
                }
            }
            GUITools.EndScrollView();
        }
        GUI.EndGroup();

        return scroll;
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