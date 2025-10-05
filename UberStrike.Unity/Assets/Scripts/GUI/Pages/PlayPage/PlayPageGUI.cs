using System.Collections;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Realtime.Common;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class PlayPageGUI : MonoSingleton<PlayPageGUI>
{
    private string[] _mapsFilter;
    private string[] _modesFilter;

    private void Awake()
    {
        _filterSavedData = new FilterSavedData();
        _cachedGameList = new List<GameMetaData>();

        _sortGamesAscending = false;
        _gameSortingMethod = new GameDataPlayerComparer();
        _lastSortedColumn = GameListColumns.PlayerCount;
        _searchBar = new SearchBarGUI("SearchGame");

        CmuneDebug.Assert(_privateGameIcon != null, "_privateGameIcon not assigned");
    }

    private void Start()
    {
        _weaponClassTexts = new string[] { 
            LocalizedStrings.Handguns, 
            LocalizedStrings.Machineguns, 
            LocalizedStrings.SniperRifles, 
            LocalizedStrings.Shotguns, 
            LocalizedStrings.Launchers
        };

        _modesFilter = new string[] { 
            LocalizedStrings.All + " Modes" , 
            LocalizedStrings.DeathMatch, 
            LocalizedStrings.TeamDeathMatch, 
            LocalizedStrings.TeamElimination
        };

        List<string> allMaps = new List<string>();
        allMaps.Add(LocalizedStrings.All + " Maps");
        foreach (var m in LevelManager.Instance.AllMaps)
        {
            if (m.Id != 0)
                allMaps.Add(m.Name);
        }

        allMaps.RemoveAll(s => string.IsNullOrEmpty(s));
        _mapsFilter = allMaps.ToArray();
    }

    #region Unity

    private void OnEnable()
    {
        _showFilters = false;
        ResetFilters();

        _unFocus = true;
    }

    private void OnDisable()
    {

    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Page;
        GUI.skin = BlueStonez.Skin;

        //what is this?
        if (_unFocus)
        {
            if (GUIUtility.keyboardControl != 0) GUIUtility.keyboardControl = 0;
            _unFocus = false;
        }

        Rect rect = new Rect(0, GlobalUIRibbon.Instance.GetHeight(), Screen.width, Screen.height - GlobalUIRibbon.Instance.GetHeight());
        GUI.Box(rect, string.Empty, BlueStonez.box_grey31);

        if (!_isConnectedToGameServer || GameServerController.Instance.SelectedServer == null)
        {
            DoServerPage(rect);
        }
        else if (_isConnectedToGameServer && GameServerController.Instance.SelectedServer != null)
        {
            DoGamePage(rect);
        }

        if (_checkForPassword)
        {
            PasswordCheck(new Rect((Screen.width - 280) / 2, (Screen.height - 200) / 2, 280, 200));
        }
        GuiManager.DrawTooltip();
    }

    #endregion

    private void ResetFilters()
    {
        _currentMap = 0;
        _currentMode = 0;
        _currentWeapon = 0;

        _noFriendFire = false;
        _gameNotFull = false;
        _noPrivateGames = false;

        _instasplat = false;
        _lowGravity = false;
        _justForFun = false;

        _singleWeapon = false;

        _searchBar.ClearFilter();
    }

    #region Server Selection

    /// <summary>
    /// Server selection main function (including screen header and calls for DoServerSelectionList(), DoServerHelpText(), 
    /// DrawServerListLowerPart())
    /// </summary>
    private void DoServerPage(Rect rect)
    {
        float helpPartWidth = 200;
        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(0, 0, rect.width, 56), LocalizedStrings.ChooseYourRegionCaps, BlueStonez.tab_strip);
            GUI.Box(new Rect(0, 55, rect.width, rect.height - 57), string.Empty, BlueStonez.window_standard_grey38);
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(0, 28, rect.width - 5, 28), string.Format("{0} {1}, {2} {3} ", GameServerManager.Instance.AllPlayersCount, LocalizedStrings.PlayersOnline, GameServerManager.Instance.AllGamesCount, LocalizedStrings.Games), BlueStonez.label_interparkbold_18pt_right);
            GUI.color = Color.white;

            //if (Input.GetKey(KeyCode.Alpha1))
            //    DoServerList(new Rect(10, 55, rect.width - helpPartWidth - 10, rect.height - 49));
            //else
            //    _serverList.Draw(new Rect(10, 55, rect.width - helpPartWidth - 10, rect.height - 105));

            DoServerList(new Rect(10, 55, rect.width - helpPartWidth - 10, rect.height - 49));

            DoServerHelpText(new Rect(rect.width - helpPartWidth, 55, helpPartWidth - 10, rect.height - 49));

            DrawServerListButtons(rect, helpPartWidth);
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Server selection screen help column
    /// </summary>
    private void DoServerHelpText(Rect position)
    {
        GUI.BeginGroup(position);
        {
            GUI.Box(new Rect(0, 0, position.width, 32), LocalizedStrings.HelpCaps, BlueStonez.box_grey50);
            GUI.Box(new Rect(0, 31, position.width, position.height - 31 - 55), string.Empty, BlueStonez.box_grey50);
            _serverSelectionHelpScrollBar = GUI.BeginScrollView(new Rect(0, 33, position.width, position.height - 31 - 60), _serverSelectionHelpScrollBar, new Rect(0, 0, position.width - 20, 400));
            {
                DrawGroupLabel(new Rect(5, 5, position.width - 25, 100), "1. " + LocalizedStrings.ServerName, LocalizedStrings.ServerNameDesc);
                DrawGroupLabel(new Rect(5, 105, position.width - 25, 70), "2. " + LocalizedStrings.Capacity, LocalizedStrings.CapacityDesc);
                DrawGroupLabel(new Rect(5, 180, position.width - 25, 180), "3. " + LocalizedStrings.Speed, LocalizedStrings.SpeedDesc);
            }
            GUI.EndScrollView();
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Server selection table headers (including call to DrawServerList())
    /// </summary>
    private void DoServerList(Rect position)
    {
        _serverNameColumnWidth = position.width - _serverCapacityColumnWidth - _serverSpeedColumnWidth + 1;
        GUI.BeginGroup(position);
        {
            GUI.Box(new Rect(0, 0, _serverNameColumnWidth + 1, 32), string.Empty, BlueStonez.box_grey50); // Name
            GUI.Box(new Rect(_serverNameColumnWidth, 0, _serverCapacityColumnWidth + 1, 32), string.Empty, BlueStonez.box_grey50); // Capacity
            GUI.Box(new Rect(_serverNameColumnWidth + _serverCapacityColumnWidth, 0, _serverSpeedColumnWidth, 32), string.Empty, BlueStonez.box_grey50); // Speed
            GUI.Box(new Rect(0, 31, position.width + 1, position.height - 31 - 55), string.Empty, BlueStonez.box_grey50); // Server list

            if (_lastSortedServerColumn == ServerListColumns.ServerName)
            {
                if (_sortServerAscending)
                    GUI.Label(new Rect(5, 0, _serverNameColumnWidth + 1 - 5, 32), new GUIContent(LocalizedStrings.ServerName, _sortUpArrow), BlueStonez.label_interparkbold_18pt_left);
                else
                    GUI.Label(new Rect(5, 0, _serverNameColumnWidth + 1 - 5, 32), new GUIContent(LocalizedStrings.ServerName, _sortDownArrow), BlueStonez.label_interparkbold_18pt_left);
            }
            else
            {
                GUI.Label(new Rect(12, 0, _serverNameColumnWidth + 1 - 5, 32), LocalizedStrings.ServerName, BlueStonez.label_interparkbold_18pt_left);
            }

            if (GUI.Button(new Rect(0, 0, _serverNameColumnWidth + 1 - 5, 32), GUIContent.none, BlueStonez.label_interparkbold_11pt_left))
            {
                SortServerList(ServerListColumns.ServerName);
            }
            if (_lastSortedServerColumn == ServerListColumns.ServerCapacity)
            {
                if (_sortServerAscending)
                    GUI.Label(new Rect(5 + _serverNameColumnWidth, 0, _serverCapacityColumnWidth + 1 - 5, 32), new GUIContent(LocalizedStrings.Capacity, _sortUpArrow), BlueStonez.label_interparkbold_18pt_left);
                else
                    GUI.Label(new Rect(5 + _serverNameColumnWidth, 0, _serverCapacityColumnWidth + 1 - 5, 32), new GUIContent(LocalizedStrings.Capacity, _sortDownArrow), BlueStonez.label_interparkbold_18pt_left);
            }
            else
                GUI.Label(new Rect(_serverNameColumnWidth + 12, 0, _serverCapacityColumnWidth + 1 - 5, 32), LocalizedStrings.Capacity, BlueStonez.label_interparkbold_18pt_left);
            if (GUI.Button(new Rect(_serverNameColumnWidth, 0, _serverCapacityColumnWidth + 1 - 5, 32), GUIContent.none, BlueStonez.label_interparkbold_11pt_left))
            {
                SortServerList(ServerListColumns.ServerCapacity);
            }

            if (_lastSortedServerColumn == ServerListColumns.ServerSpeed)
            {
                if (_sortServerAscending)
                    GUI.Label(new Rect(5 + _serverNameColumnWidth + _serverCapacityColumnWidth, 0, _serverSpeedColumnWidth - 5, 32), new GUIContent(LocalizedStrings.Speed, _sortUpArrow), BlueStonez.label_interparkbold_18pt_left);
                else
                    GUI.Label(new Rect(5 + _serverNameColumnWidth + _serverCapacityColumnWidth, 0, _serverSpeedColumnWidth - 5, 32), new GUIContent(LocalizedStrings.Speed, _sortDownArrow), BlueStonez.label_interparkbold_18pt_left);
            }
            else
            {
                GUI.Label(new Rect(_serverNameColumnWidth + _serverCapacityColumnWidth + 12, 0, _serverSpeedColumnWidth - 5, 32), LocalizedStrings.Speed, BlueStonez.label_interparkbold_18pt_left);
            }

            if (GUI.Button(new Rect(_serverNameColumnWidth + _serverCapacityColumnWidth, 0, _serverSpeedColumnWidth - 5, 32), GUIContent.none, BlueStonez.label_interparkbold_11pt_left))
            {
                SortServerList(ServerListColumns.ServerSpeed);
            }
            DrawAllServers(position);
        }
        GUI.EndGroup();
    }

    private void DrawProgressBarLarge(Rect position, float amount)
    {
        amount = Mathf.Clamp01(amount);
        GUI.Box(new Rect(position.x, position.y, position.width, 23), GUIContent.none, BlueStonez.progressbar_large_background);

        GUI.color = ColorScheme.ProgressBar;
        GUI.Box(new Rect(position.x + 2,position.y + 2, Mathf.RoundToInt((position.width - 4) * amount), 19), GUIContent.none, BlueStonez.progressbar_large_thumb);
        GUI.color = Color.white;
    }

    /// <summary>
    /// Server selection actual server list drawing
    /// </summary>
    private void DrawAllServers(Rect pos)
    {
        int length = GameServerManager.Instance.PhotonServerCount * 48;

        GUI.color = Color.white;
        _serverSelectionScrollBar = GUI.BeginScrollView(new Rect(0, 31, pos.width + 1, pos.height - 31 - 55), _serverSelectionScrollBar, new Rect(0, 0, pos.width - 20, length));

        int i = 0;
        string text = string.Empty;
        foreach (var server in GameServerManager.Instance.PhotonServerList)
        {
            // draw server selection
            GUI.BeginGroup(new Rect(0, i * 48, pos.width + 2, 49), BlueStonez.box_grey50);
            {
                if (GameServerController.Instance.SelectedServer != null && GameServerController.Instance.SelectedServer == server)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.1f);
                    GUI.DrawTexture(new Rect(1, 0, pos.width + 1, 49), UberstrikeIcons.White);
                    GUI.color = Color.white;
                }

                server.Flag.Draw(new Rect(5, 8, 32, 32));
                GUI.Label(new Rect(42, 1, _serverNameColumnWidth + 1 - 42, 48), server.Name, BlueStonez.label_interparkbold_16pt_left);

                if (server.Data.State == ServerLoadData.Status.Alive)
                {
                    // server load
                    GUI.BeginGroup(new Rect(5 + _serverNameColumnWidth, 0, _serverCapacityColumnWidth + 1 - 5, 48));
                    {
                        // Display the player count, with cleaned data (show true count for admins, cleaned count for players)
                        int playerCount = 0;
                        if (PlayerDataManager.AccessLevel == MemberAccessLevel.Admin)
                            playerCount = server.Data.PlayersConnected;
                        else
                            playerCount = Mathf.Clamp(server.Data.PlayersConnected, 0, (int)server.Data.MaxPlayerCount);

                        // Cet the capacity as a cleaned percentage
                        float capacityPercent = 0.0f;
                        if (playerCount >= server.Data.MaxPlayerCount)
                            capacityPercent = 1.0f;
                        else
                            capacityPercent = (float)playerCount / server.Data.MaxPlayerCount;

                        //int blockCount = Mathf.CeilToInt((float)playerCount / (server.Data.MaxPlayerCount / 4));

                        //// If we have 199/200 players we don't want to show 4 red blocks, so minus one from blockCount
                        //if (playerCount < server.Data.MaxPlayerCount && blockCount == 4) blockCount = 3;

                        //switch (blockCount)
                        //{
                        //    case 1: GUI.color = ColorConverter.RgbToColor(146, 208, 80); break;
                        //    case 2: GUI.color = ColorConverter.RgbToColor(84, 139, 212); break;
                        //    case 3: GUI.color = ColorConverter.RgbToColor(234, 112, 13); break;
                        //    case 4: GUI.color = ColorConverter.RgbToColor(192, 80, 70); break;
                        //    default: GUI.color = Color.white; break;
                        //}

                        //for (int j = 0; j < 4; j++)
                        //{
                        //    if (j == blockCount)
                        //        GUI.color = ColorConverter.RgbToColor(63, 63, 63);

                        //    GUI.DrawTexture(new Rect(4 + j * 15, 14, 12, 20), UberstrikeIcons.White);
                        //}

                        //GUI.color = Color.white;

                        DrawProgressBarLarge(new Rect(2, 12, 58, 20), capacityPercent);

                        text = string.Format("{0}/{1}", playerCount, server.Data.MaxPlayerCount);

                        GUI.Label(new Rect(64, 14, 60, 20), text, BlueStonez.label_interparkmed_10pt_left);
                    }
                    GUI.EndGroup();

                    // Ping
                    GUI.BeginGroup(new Rect(5 + _serverNameColumnWidth + _serverCapacityColumnWidth, 0, _serverSpeedColumnWidth - 5 - (length > (pos.height - 31) ? 21 : 0), 48));
                    {
                        int latency = server.Latency;
                        text = string.Empty;

                        if (latency < (int)ServerLatency.Fast)
                        {
                            GUI.color = ColorConverter.RgbToColor(80, 99, 42);
                            text = LocalizedStrings.FastCaps;
                        }
                        else if (latency < (int)ServerLatency.Med)
                        {
                            GUI.color = ColorConverter.RgbToColor(234, 112, 13);
                            text = LocalizedStrings.MedCaps;
                        }
                        else
                        {
                            GUI.color = ColorConverter.RgbToColor(192, 80, 70);
                            text = LocalizedStrings.SlowCaps;
                        }

                        GUI.DrawTexture(new Rect(0, 14, 45, 20), UberstrikeIcons.White);
                        GUI.color = Color.white;
                        GUI.Label(new Rect(2, 14, 40, 20), text, BlueStonez.label_interparkbold_16pt);
                        GUI.Label(new Rect(48, 4, 40, 40), string.Format("{0}ms", latency), BlueStonez.label_interparkmed_10pt_left);
                    }
                    GUI.EndGroup();
                }
                else // show wait for server animation
                {
                    if (server.Data.State == ServerLoadData.Status.None)
                    {
                        Rect refreshErrorRect = new Rect(5 + _serverNameColumnWidth, 0, _serverCapacityColumnWidth + 1 - 5 + _serverSpeedColumnWidth - (length > (pos.height - 31) ? 21 : 0), 48);
                        GUI.BeginGroup(refreshErrorRect);
                        {
                            GUI.Label(new Rect(0, 0, refreshErrorRect.width, 48), LocalizedStrings.RefreshingServer, BlueStonez.label_interparkbold_16pt);
                        }
                        GUI.EndGroup();
                    }
                    else
                    {
                        if (server.Data.State == ServerLoadData.Status.NotReachable)
                        {
                            Rect refreshErrorRect = new Rect(5 + _serverNameColumnWidth, 0, _serverCapacityColumnWidth + 1 - 5 + _serverSpeedColumnWidth - (length > (pos.height - 31) ? 21 : 0), 48);
                            GUI.BeginGroup(refreshErrorRect);
                            {
                                GUI.Label(new Rect(0, 0, refreshErrorRect.width, 48), LocalizedStrings.ServerIsNotReachable, BlueStonez.label_interparkbold_16pt);
                            }
                            GUI.EndGroup();
                        }
                    }
                }
                if (GUI.Button(new Rect(0, 0, pos.width + 1, 49), GUIContent.none, GUIStyle.none) && server.Data.State != ServerLoadData.Status.NotReachable)
                {
                    //join by double click
                    if (GameServerController.Instance.SelectedServer == server && _serverJoinDoubleClick > Time.time)
                    {
                        _serverJoinDoubleClick = 0;
                        SelectedServerUpdated();
                        SfxManager.Play2dAudioClip(SoundEffectType.UIJoinServer);
                    }
                    else
                    {
                        _serverJoinDoubleClick = Time.time + _doubleClickFrame;
                    }

                    GameServerController.Instance.SelectedServer = server;
                }

            }
            GUI.EndGroup();
            i++;
        }
        GUI.EndScrollView();
    }

    /// <summary>
    /// Server selection screen lower part with "JOIN" button (TryConnectToSelectedServer()) and 
    /// "REFRESH" button (UpdateAllServerLoad())
    /// </summary>
    private void DrawServerListButtons(Rect rect, float helpPartWidth)
    {
        bool tmp = GUI.enabled;
        GUI.enabled = tmp && (GameServerController.Instance.SelectedServer != null);

        if (GUITools.Button(new Rect(rect.width - 140, rect.height - 42, 120, 32), new GUIContent(LocalizedStrings.JoinCaps), BlueStonez.button_green, SoundEffectType.UIJoinServer))
        {
            SelectedServerUpdated();
        }

        GUI.enabled = tmp && (Time.time > _nextServerCheckTime);
        if (GUITools.Button(new Rect(rect.width - 263, rect.height - 42, 120, 32), new GUIContent(LocalizedStrings.RefreshCaps), BlueStonez.button))
        {
            RefreshServerLoad();
        }

        GUI.enabled = tmp;
    }

    /// <summary>
    /// Check if player can actually connect to selected server including warning dialogs 
    /// in case it is not reccomended or server is full (include call to ShowGameSelection() if successful)
    /// </summary>
    public void SelectedServerUpdated()
    {
        if (GameServerController.Instance.SelectedServer != null && GameServerController.Instance.SelectedServer.Data.State == ServerLoadData.Status.Alive)
        {
            if (PlayerDataManager.AccessLevelSecure == MemberAccessLevel.Admin)
            {
                ShowGameSelection();
            }
            else if (GameServerController.Instance.SelectedServer.Data.PlayersConnected >= GameServerController.Instance.SelectedServer.Data.MaxPlayerCount)
            {
                PopupSystem.ShowMessage(LocalizedStrings.ServerFull, LocalizedStrings.ServerFullMsg);
            }
            else if (!GameServerController.Instance.SelectedServer.LatencyCheck())
            {
                PopupSystem.ShowMessage(LocalizedStrings.Warning, "Your connection to this server is too slow.", PopupSystem.AlertType.OK, null);
            }
            else if (GameServerController.Instance.SelectedServer.Latency >= (int)ServerLatency.Med)
            {
                PopupSystem.ShowMessage(LocalizedStrings.Warning, LocalizedStrings.ConnectionSlowMsg, PopupSystem.AlertType.OKCancel, ShowGameSelection, LocalizedStrings.OkCaps, null, LocalizedStrings.CancelCaps);
            }
            else
            {
                ShowGameSelection();
            }
        }
        else
        {
            CmuneDebug.LogError("Couldn't connect to server!");
        }
    }

    /// <summary>
    /// Draw text with title on selected location
    /// </summary>
    private void DrawGroupLabel(Rect position, string header, string text)
    {
        //GUI.color = new Color(0.87f, 0.64f, 0.035f, 1);
        GUI.color = Color.white;
        GUI.Label(new Rect(position.x, position.y, position.width, 16), header, BlueStonez.label_interparkbold_13pt);
        GUI.color = new Color(1, 1, 1, 0.8f);
        GUI.Label(new Rect(position.x, position.y + 16, position.width, position.height - 16), text, BlueStonez.label_interparkbold_11pt_left_wrap);
        GUI.color = Color.white;
    }

    #endregion

    /// <summary>
    /// Game selection main function (including screen header and calls for cahnging server (ShowServerSelection()), 
    /// quick search bar (DrawQuickSearch()), game list (DoGameList()), bottom area with buttons (DoBottomArea()) and 
    /// filter area (DoFilterArea()))
    /// </summary>
    private void DoGamePage(Rect rect)
    {
        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(0, 0, rect.width, 56), LocalizedStrings.ChooseAGameCaps, BlueStonez.tab_strip);

            GUI.color = new Color(1f, 1f, 1f, 0.5f);

            GUI.Label(new Rect(10, 28, rect.width - 37, 28), string.Format("{0} ({1}ms)", GameServerController.Instance.SelectedServer.Name, GameServerController.Instance.SelectedServer.Latency.ToString()), BlueStonez.label_interparkbold_18pt_left);

            GUI.Label(new Rect(0, 28, rect.width - 5, 28), string.Format("{0} {1}, {2} {3} ", GameListManager.PlayersCount, LocalizedStrings.PlayersOnline, GameListManager.GamesCount, LocalizedStrings.Games), BlueStonez.label_interparkbold_18pt_right);

            GUI.color = Color.white;

            GUI.Box(new Rect(0, 55, rect.width, rect.height - 57), string.Empty, BlueStonez.window_standard_grey38);

            // Draw the quick search text field
            DrawQuickSearch(rect);

            if (GUITools.Button(new Rect(rect.width - 300, 6, 140, 23), new GUIContent(LocalizedStrings.Refresh), BlueStonez.buttondark_medium))
            {
                if (!LobbyConnectionManager.IsConnected)
                {
                    ResetLobbyDisconnectTimeout();

                    LobbyConnectionManager.StartConnection();
                }

                RefreshGameList();
            }

            bool tmp = GUI.enabled;
            GUI.enabled &= (_dropDownList == 0) && !_checkForPassword;
            if (_showFilters)
            {
                DoGameList(new Rect(25, 73, Screen.width - 0, Screen.height - 105));
            }
            else
            {
                DoGameList(rect);
            }

            DoBottomArea(rect);
            GUI.enabled = tmp;

            if (_showFilters)
            {
                DoFilterArea(rect);
            }
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Game list table headers (including UpdateColumnWidth(), call to actual game list drawing function 
    /// (DoServerListItem()) and game list sorting with SortGameList())
    /// </summary>
    private void DoGameList(Rect rect)
    {
        UpdateColumnWidth();

        //Server list
        int length = Mathf.RoundToInt(rect.height) - 73 - 104;
        if (!_showFilters)
        {
            length += 73;
        }

        Rect serverRect = new Rect(10, 55, rect.width - 20, length);
        GUI.Box(serverRect, string.Empty, BlueStonez.box_grey50);
        GUI.BeginGroup(serverRect);
        {
            if (!LobbyConnectionManager.IsConnected)
            {
                GUI.Label(new Rect(0, serverRect.height * 0.5f, serverRect.width, 23), LocalizedStrings.PressRefreshToSeeCurrentGames, BlueStonez.label_interparkmed_11pt);

                if (GUITools.Button(new Rect(serverRect.width * 0.5f - 70, serverRect.height * 0.5f - 30, 140, 23), new GUIContent(LocalizedStrings.Refresh), BlueStonez.buttondark_medium))
                {
                    if (!LobbyConnectionManager.IsConnected)
                    {
                        ResetLobbyDisconnectTimeout();

                        LobbyConnectionManager.StartConnection();
                    }

                    RefreshGameList();
                }
            }
            else if (LobbyConnectionManager.IsConnecting)
            {
                GUI.Label(new Rect(0, 0, serverRect.width, serverRect.height - 1), LocalizedStrings.ConnectingToLobby, BlueStonez.label_interparkmed_11pt);
            }
            else if (LobbyConnectionManager.IsInLobby && _cachedGameList.Count == 0)
            {
                GUI.Label(new Rect(0, 0, serverRect.width, serverRect.height - 1), LocalizedStrings.LoadingGameList, BlueStonez.label_interparkmed_11pt);
            }

            if (GameServerController.Instance.SelectedServer != null)
            {
                length = 48 * (((_filteredActiveRoomCount >= 0 && _filteredActiveRoomCount <= _cachedGameList.Count) ? _filteredActiveRoomCount : _cachedGameList.Count)) + 5;
            }
            else
            {
                length = 0;
            }

            // draw head of game selection window
            int x = 0;
            int offset = 0;
            Texture2D sortIcon = null;

            //PRIVATE GAMES
            x = 5;
            sortIcon = (_lastSortedColumn == GameListColumns.Lock) ? _sortGamesAscending ? _sortUpArrow : _sortDownArrow : null;
            offset = (_lastSortedColumn == GameListColumns.Lock) ? 5 : 12;
            GUI.Box(new Rect(x, 0, _privateGameWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x, 0, _privateGameWidth, 25), new GUIContent(string.Empty, sortIcon), BlueStonez.label_interparkbold_16pt_left);
            if (GUI.Button(new Rect(x, 0, _privateGameWidth, 25), GUIContent.none, BlueStonez.label_interparkbold_11pt_left))
            {
                SortGameList(GameListColumns.Lock);
            }

            //MODERATOR INGAME
            x = _privateGameWidth - 1;
            sortIcon = (_lastSortedColumn == GameListColumns.Star) ? _sortGamesAscending ? _sortUpArrow : _sortDownArrow : null;
            offset = (_lastSortedColumn == GameListColumns.Star) ? 5 : 12;
            GUI.Box(new Rect(x, 0, _moderatorInGameWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + offset, 0, _moderatorInGameWidth, 25), new GUIContent(string.Empty, sortIcon), BlueStonez.label_interparkbold_16pt_left);
            if (GUI.Button(new Rect(x, 0, _moderatorInGameWidth, 25), GUIContent.none, BlueStonez.label_interparkbold_11pt_left))
            {
                SortGameList(GameListColumns.Star);
            }

            //GAME NAME
            x = _privateGameWidth + _moderatorInGameWidth - 2;
            sortIcon = (_lastSortedColumn == GameListColumns.GameName) ? _sortGamesAscending ? _sortUpArrow : _sortDownArrow : null;
            offset = (_lastSortedColumn == GameListColumns.GameName) ? 5 : 12;
            GUI.Box(new Rect(x, 0, _gameNameWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + offset, 0, _gameNameWidth, 25), new GUIContent(LocalizedStrings.Name, sortIcon), BlueStonez.label_interparkbold_16pt_left);
            if (GUI.Button(new Rect(x, 0, _gameNameWidth, 25), GUIContent.none, BlueStonez.label_interparkbold_11pt_left))
            {
                SortGameList(GameListColumns.GameName);
            }

            //MAP NAME
            x = _privateGameWidth + _moderatorInGameWidth + _gameNameWidth - 3;
            sortIcon = (_lastSortedColumn == GameListColumns.GameMap) ? _sortGamesAscending ? _sortUpArrow : _sortDownArrow : null;
            offset = (_lastSortedColumn == GameListColumns.GameMap) ? 5 : 12;
            GUI.Box(new Rect(x, 0, _mapNameWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + offset, 0, _mapNameWidth, 25), new GUIContent(LocalizedStrings.Map, sortIcon), BlueStonez.label_interparkbold_16pt_left);
            if (GUI.Button(new Rect(x, 0, _mapNameWidth, 25), string.Empty, BlueStonez.label_interparkbold_11pt_left))
            {
                SortGameList(GameListColumns.GameMap);
            }

            //GAME MODE
            x = _privateGameWidth + _moderatorInGameWidth + _gameNameWidth + _mapNameWidth - 4;
            sortIcon = (_lastSortedColumn == GameListColumns.GameMode) ? _sortGamesAscending ? _sortUpArrow : _sortDownArrow : null;
            offset = (_lastSortedColumn == GameListColumns.GameMode) ? 5 : 12;
            GUI.Box(new Rect(x, 0, _gameModeWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + offset, 0, _gameModeWidth, 25), new GUIContent(LocalizedStrings.Mode, sortIcon), BlueStonez.label_interparkbold_16pt_left);
            if (GUI.Button(new Rect(x, 0, _gameModeWidth, 25), string.Empty, BlueStonez.label_interparkbold_11pt_left))
            {
                SortGameList(GameListColumns.GameMode);
            }

            //GAME TIME
            x = _privateGameWidth + _moderatorInGameWidth + _gameNameWidth + _mapNameWidth + _gameModeWidth - 10;
            sortIcon = (_lastSortedColumn == GameListColumns.GameTime) ? _sortGamesAscending ? _sortUpArrow : _sortDownArrow : null;
            offset = (_lastSortedColumn == GameListColumns.GameTime) ? 5 : 12;
            GUI.Box(new Rect(x, 0, _gameTimeWidth + 5, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + offset, 0, _gameTimeWidth, 25), new GUIContent(LocalizedStrings.Minutes, sortIcon), BlueStonez.label_interparkbold_16pt_left);
            if (GUI.Button(new Rect(x, 0, _gameTimeWidth, 25), string.Empty, BlueStonez.label_interparkbold_11pt_left))
            {
                SortGameList(GameListColumns.GameTime);
            }

            //PLAYER COUNT
            x = _privateGameWidth + _moderatorInGameWidth + _gameNameWidth + _mapNameWidth + _gameModeWidth + _gameTimeWidth - 6;
            sortIcon = (_lastSortedColumn == GameListColumns.PlayerCount) ? _sortGamesAscending ? _sortUpArrow : _sortDownArrow : null;
            offset = (_lastSortedColumn == GameListColumns.PlayerCount) ? 5 : 12;
            GUI.Box(new Rect(x, 0, _playerCountWidth, 25), string.Empty, BlueStonez.box_grey50);
            GUI.Label(new Rect(x + offset, 0, _playerCountWidth, 25), new GUIContent(LocalizedStrings.Players, sortIcon), BlueStonez.label_interparkbold_16pt_left);
            if (GUI.Button(new Rect(x, 0, _playerCountWidth, 25), string.Empty, BlueStonez.label_interparkbold_11pt_left))
            {
                SortGameList(GameListColumns.PlayerCount);
            }

            //NOW DRAW LIST
            if (LobbyConnectionManager.IsConnected)
            {
                Vector2 tmp = _serverScroll;
                _serverScroll = GUI.BeginScrollView(new Rect(0, 25, serverRect.width, serverRect.height - 1 - 25), _serverScroll, new Rect(0, 0, serverRect.width - 60, length), BlueStonez.horizontalScrollbar, BlueStonez.verticalScrollbar);
                {
                    _filteredActiveRoomCount = DrawAllGames(serverRect, serverRect.height <= length);
                }
                GUI.EndScrollView();
                if (tmp != _serverScroll)
                {
                    ResetLobbyDisconnectTimeout();
                }
            }
        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Runs SortServerList() with given sortedColumn and true as second parameter.
    /// </summary>
    private void SortServerList(ServerListColumns sortedColumn)
    {
        SortServerList(sortedColumn, true);
    }

    /// <summary>
    /// Sorting for server list
    /// </summary>
    private void SortServerList(ServerListColumns sortedColumn, bool changeDirection)
    {
        if (changeDirection && sortedColumn == _lastSortedServerColumn)
            _sortServerAscending = !_sortServerAscending;

        _lastSortedServerColumn = sortedColumn;
        switch (sortedColumn)
        {
            case ServerListColumns.ServerName:
                GameServerManager.Instance.SortServers(new GameServerNameComparer(), _sortServerAscending);
                break;
            case ServerListColumns.ServerCapacity:
                GameServerManager.Instance.SortServers(new GameServerPlayerCountComparer(), _sortServerAscending);
                break;
            case ServerListColumns.ServerSpeed:
                GameServerManager.Instance.SortServers(new GameServerLatencyComparer(), _sortServerAscending);
                break;
            default:
                GameServerManager.Instance.SortServers(new GameServerLatencyComparer(), _sortServerAscending);
                break;
        }
    }

    /// <summary>
    /// Runs SortGameList() with given sortedColumn and true as second parameter.
    /// </summary>
    private void SortGameList(GameListColumns sortedColumn)
    {
        if (sortedColumn == _lastSortedColumn)
            _sortGamesAscending = !_sortGamesAscending;

        _lastSortedColumn = sortedColumn;

        switch (sortedColumn)
        {
            case GameListColumns.Lock:
                SortGameList(new GameDataAccessComparer());
                break;
            case GameListColumns.GameName:
                SortGameList(new GameDataNameComparer());
                break;
            case GameListColumns.GameMap:
                SortGameList(new GameDataMapComparer());
                break;
            case GameListColumns.GameMode:
                SortGameList(new GameDataRuleComparer());
                break;
            case GameListColumns.PlayerCount:
                SortGameList(new GameDataPlayerComparer());
                break;
            case GameListColumns.GameTime:
                SortGameList(new GameDataTimeComparer());
                break;
            default:
                SortGameList(new GameDataLatencyComparer());
                break;
        }
    }

    /// <summary>
    /// Sorting for game list
    /// </summary>
    private void SortGameList(IComparer<GameMetaData> sortedColumn)
    {
        _gameSortingMethod = sortedColumn;

        RefreshGameList();
    }

    /// <summary>
    /// Game selection screen filter area draw function
    /// </summary>
    private void DoFilterArea(Rect rect)
    {
        bool tmp = GUI.enabled;

        // filters
        Rect filterRect = new Rect(10, rect.height - 73 - 50, rect.width - 20, 74);
        GUI.Box(filterRect, string.Empty, BlueStonez.box_grey50);
        GUI.BeginGroup(new Rect(filterRect.x, filterRect.y, filterRect.width, filterRect.width + 60));
        {
            // map
            GUI.enabled = tmp && (_dropDownList == 0 || _dropDownList == _dropDownListMap);
            GUI.Label(new Rect(10, 10, 115, 21), _mapsFilter[_currentMap], BlueStonez.label_dropdown);
            if (GUI.Button(new Rect(123, 9, 21, 21), GUIContent.none, BlueStonez.dropdown_button))
            {
                _dropDownList = _dropDownList == 0 ? _dropDownListMap : 0;
                _dropDownRect = new Rect(10, 31, 133, 80);
            }

            // mode
            GUI.enabled = tmp && (_dropDownList == 0 || _dropDownList == _dropDownListGameMode);
            GUI.Label(new Rect(10, 42, 115, 21), _modesFilter[_currentMode], BlueStonez.label_dropdown);
            if (GUI.Button(new Rect(123, 41, 21, 21), GUIContent.none, BlueStonez.dropdown_button))
            {
                _dropDownList = _dropDownList == 0 ? _dropDownListGameMode : 0;
                _dropDownRect = new Rect(10, 63, 133, 60);
            }

            // weapon type
            GUI.enabled = tmp && (_dropDownList == 0 || _dropDownList == _dropDownListSingleWeapon) && _singleWeapon;
            //GUI.Label(new Rect(475, 26, 140, 21), _weaponClassTexts[_currentWeapon], BlueStonez.label_dropdown);
            //if (GUI.Button(new Rect(475 + 160 - 22, 26, 21, 21), string.Empty, BlueStonez.dropdown_button))
            //{
            //    _dropDownList = _dropDownList == 0 ? _dropDownListSingleWeapon : 0;
            //    _dropDownRect = new Rect(475, 47, 138, 30);
            //}

            GUI.enabled = tmp && (_dropDownList == 0);
            _gameNotFull = GUI.Toggle(new Rect(165, 7, 170, 16), _gameNotFull, LocalizedStrings.GameNotFull, BlueStonez.toggle);
            _noPrivateGames = GUI.Toggle(new Rect(165, 28, 170, 16), _noPrivateGames, LocalizedStrings.NotPasswordProtected, BlueStonez.toggle);
            // _noFriendFire = GUI.Toggle(new Rect(165, 49, 170, 16), _noFriendFire, LocalizedStrings.NoFriendlyFire, BlueStonez.toggle);

            GUI.enabled = false;
            //_instasplat = GUI.Toggle(new Rect(355, 7, 100, 16), _instasplat, LocalizedStrings.Instakill, BlueStonez.toggle);
            //_lowGravity = GUI.Toggle(new Rect(355, 28, 100, 16), _lowGravity, LocalizedStrings.LowGravity, BlueStonez.toggle);
            //_justForFun = GUI.Toggle(new Rect(355, 49, 100, 16), _justForFun, LocalizedStrings.JustForFun, BlueStonez.toggle);

            //_singleWeapon = GUI.Toggle(new Rect(475, 7, 160, 16), _singleWeapon, LocalizedStrings.SingleWeaponLimit, BlueStonez.toggle);
            //if (CheckChangesInFilter())
            //{
            //    _serverScroll = new Vector2(0, 0);
            //    if (!CanPassFilter(_selectedGame)) _selectedGame = null;
            //    RefreshGameList();
            //}
            GUI.enabled = tmp;

            if (_dropDownList != 0)
                DoDropDownList();

        }
        GUI.EndGroup();
    }

    /// <summary>
    /// Check if there is any changes in filter selection
    /// </summary>
    private bool CheckChangesInFilter()
    {
        bool isChanged = false;

        if (_filterSavedData.UseFilter != _showFilters)
        {
            _filterSavedData.UseFilter = _showFilters;
            isChanged = true;
        }
        if (_filterSavedData.MapName != _mapsFilter[_currentMap])
        {
            _filterSavedData.MapName = _mapsFilter[_currentMap];
            isChanged = true;
        }
        if (_filterSavedData.GameMode != _modesFilter[_currentMode])
        {
            _filterSavedData.GameMode = _modesFilter[_currentMode];
            isChanged = true;
        }
        if (_filterSavedData.NoFriendlyFire != _noFriendFire)
        {
            _filterSavedData.NoFriendlyFire = _noFriendFire;
            isChanged = true;
        }
        if (_filterSavedData.ISGameNotFull != _gameNotFull)
        {
            _filterSavedData.ISGameNotFull = _gameNotFull;
            isChanged = true;
        }
        if (_filterSavedData.NoPasswordProtection != _noPrivateGames)
        {
            _filterSavedData.NoPasswordProtection = _noPrivateGames;
            isChanged = true;
        }
        return isChanged;
    }

    /// <summary>
    /// Game selection screen lower part with "JOIN" button (JoinGame()), 
    /// "REFRESH" button (ResetLobbyDisconnectTimeout() and RefreshLocalGameListFromServer()), 
    /// "CREATE GAME" button (GUIManager.Instance.OpenPanel(Panel.CreateGame)), 
    /// "RESET FILTERS" button (ResetFilters()) and toggle "FILTERS" button
    /// </summary>
    private void DoBottomArea(Rect rect)
    {
        GUITools.PushGUIState();

        GUI.enabled = _dropDownList == 0;

        if (GUITools.Button(new Rect(20, rect.height - 42, 120, 32), new GUIContent("< BACK", LocalizedStrings.ChangeServer), BlueStonez.button, SoundEffectType.UILeaveServer))
        {
            ShowServerSelection();
        }

        bool tmp = _showFilters;
        _showFilters = GUI.Toggle(new Rect(147, rect.height - 42, 120, 32), _showFilters, LocalizedStrings.FiltersCaps, BlueStonez.button);

        if (tmp != _showFilters)
        {
            ResetLobbyDisconnectTimeout();
        }

        //only show button if filters have changed
        if (_showFilters && AreFiltersActive && GUITools.Button(new Rect(273, rect.height - 42, 145, 32), new GUIContent(LocalizedStrings.ResetFiltersCaps), BlueStonez.button))
        {
            ResetLobbyDisconnectTimeout();
            ResetFilters();
        }
        if (_showFilters == false && _filterSavedData.UseFilter == true)
        {
            _filterSavedData.UseFilter = false;
        }

        if (!_refreshGameListOnFilterChange && AreFiltersActive)
        {
            RefreshGameList();
            _refreshGameListOnFilterChange = true;
        }
        if (_refreshGameListOnFilterChange && !AreFiltersActive)
        {
            RefreshGameList();
            _refreshGameListOnFilterChange = false;
        }

        GUI.enabled = true;

        if (GUITools.Button(new Rect(rect.width - 306, rect.height - 42, 140, 32), new GUIContent(LocalizedStrings.CreateGameCaps), BlueStonez.button))
        {
            ResetLobbyDisconnectTimeout();

            PanelManager.Instance.OpenPanel(PanelType.CreateGame);
        }

        GUI.enabled = LobbyConnectionManager.IsConnected &&
            _selectedGame != null && GameServerController.Instance.SelectedServer != null &&
            GameServerController.Instance.SelectedServer.Data.RoomsCreated != 0 &&
            !PanelManager.Instance.IsPanelOpen(PanelType.CreateGame);

        GUI.Label(new Rect(0, Screen.height - 20, Screen.width, 20), "lobby: " + LobbyConnectionManager.IsConnected +
            " sel game: " + (_selectedGame != null) + " sel srv: " + (GameServerController.Instance.SelectedServer != null) + " room: " + GameServerController.Instance.SelectedServer.Data.RoomsCreated);

        if (GUITools.Button(new Rect(rect.width - 160, rect.height - 42, 140, 32), new GUIContent(LocalizedStrings.JoinCaps), BlueStonez.button_green, SoundEffectType.UIJoinGame))
        {
            if ((_selectedGame != null && _selectedGame.IsPublic) || PlayerDataManager.AccessLevelSecure >= MemberAccessLevel.SeniorModerator)
            {
                JoinGame();
            }
            else
            {
                _checkForPassword = true;
                _nextPasswordCheck = Time.time;
            }
        }

        GUITools.PopGUIState();
    }

    /// <summary>
    /// Quick search window draw function with call to RefreshLocalGameListFromServer()
    /// </summary>
    private void DrawQuickSearch(Rect position)
    {
        _searchBar.Draw(new Rect(position.width - 154, 8, 142, 20));

        if (!_refreshGameListOnSortChange && _searchBar.FilterText.Length > 0)
        {
            RefreshGameList();
            _refreshGameListOnSortChange = true;
        }
        if (_refreshGameListOnSortChange && _searchBar.FilterText.Length == 0)
        {
            RefreshGameList();
            _refreshGameListOnSortChange = false;
        }
    }

    /// <summary>
    /// Drop down list for filter area
    /// </summary>
    private void DoDropDownList()
    {
        string[] contents = null;

        switch (_dropDownList)
        {
            case _dropDownListMap:
                contents = _mapsFilter;
                break;

            case _dropDownListGameMode:
                contents = _modesFilter;
                break;

            case _dropDownListSingleWeapon:
                contents = _weaponClassTexts;
                break;

            default:
                CmuneDebug.LogError("Nondefined drop down list: " + _dropDownList);
                return;
        }

        GUI.Box(_dropDownRect, string.Empty, BlueStonez.window);
        _filterScroll = GUI.BeginScrollView(_dropDownRect, _filterScroll, new Rect(0, 0, _dropDownRect.width - 20, 20 * contents.Length));
        for (int i = 0; i < contents.Length; i++)
        {
            GUI.Label(new Rect(2, 20 * i, _dropDownRect.width, 20), contents[i], BlueStonez.dropdown_list);
            if (GUI.Button(new Rect(2, 20 * i, _dropDownRect.width, 20), string.Empty, BlueStonez.dropdown_list))
            {
                switch (_dropDownList)
                {
                    case _dropDownListMap:
                        _currentMap = i;
                        break;

                    case _dropDownListGameMode:
                        _currentMode = i;
                        break;

                    case _dropDownListSingleWeapon:
                        _currentWeapon = i;
                        break;
                }

                _dropDownList = 0;
                _filterScroll.y = 0;
            }
        }
        GUI.EndScrollView();
    }

    /// <summary>
    /// Check if player can actually connect to selected game, and connect to it, 
    /// including warning dialogs in case it is not possible
    /// </summary>
    private void JoinGame()
    {
        if ((_selectedGame.ConnectedPlayers < _selectedGame.MaxPlayers &&
            (_selectedGame.ConnectedPlayers > 0 || _selectedGame.HasLevelRestriction)) ||
            PlayerDataManager.AccessLevelSecure == MemberAccessLevel.Admin)
        {
            //MenuPageManager.Instance.UnloadCurrentPage();

            GameLoader.Instance.JoinGame(_selectedGame);
        }
        else
        {
            if (_selectedGame.ConnectedPlayers == 0)
            {
                PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ThisGameNoLongerExists);
            }
            else if (_selectedGame.ConnectedPlayers == _selectedGame.MaxPlayers)
            {
                PopupSystem.ShowMessage(LocalizedStrings.Error, LocalizedStrings.ThisGameIsFull);
            }
        }
    }

    /// <summary>
    /// Ask for a password if user tried to joi n a protected game
    /// </summary>
    private void PasswordCheck(Rect position)
    {
        GUITools.PushGUIState();
        GUI.BeginGroup(position, GUIContent.none, BlueStonez.window);
        {
            GUI.Label(new Rect(0, 0, position.width, 56), LocalizedStrings.EnterPassword, BlueStonez.tab_strip);

            GUI.Box(new Rect(16, 55, position.width - 32, position.height - 56 - 64), GUIContent.none, BlueStonez.window_standard_grey38);

            GUI.SetNextControlName("EnterPassword");
            Rect nameRect = new Rect((position.width - 188) / 2, 56 + 24, 188, 24);

            _inputedPassword = GUI.PasswordField(nameRect, _inputedPassword, '*', 18, BlueStonez.textField);
            _inputedPassword = _inputedPassword.Trim(new char[] { '\n' });

            if (string.IsNullOrEmpty(_inputedPassword) && GUI.GetNameOfFocusedControl() != "EnterPassword")
            {
                GUI.color = new Color(1, 1, 1, 0.3f);
                GUI.Label(nameRect, LocalizedStrings.TypePasswordHere, BlueStonez.label_interparkmed_11pt);
                GUI.color = Color.white;
            }

            GUI.enabled = Time.time > _nextPasswordCheck;
            if (GUITools.Button(new Rect((position.width - 100 - 10), 200 - 32 - 16, 100, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button) ||
                (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.Layout && Time.time > _nextPasswordCheck))
            {
                if (_selectedGame != null && _inputedPassword == _selectedGame.Password)
                {
                    _checkForPassword = false;
                    _inputedPassword = string.Empty;
                    _isPasswordOk = true;
                    JoinGame();
                }
                else
                {
                    _inputedPassword = string.Empty;
                    _isPasswordOk = false;
                    _nextPasswordCheck = Time.time + _timeDelayOnCheckPassword;
                }

                //what is this doing here?
                Input.ResetInputAxes();
            }
            GUI.enabled = true;
            if (GUITools.Button(new Rect(10, 200 - 32 - 16, 100, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                _isPasswordOk = true;
                _checkForPassword = false;
                _inputedPassword = string.Empty;
            }

            if (!_isPasswordOk && string.IsNullOrEmpty(_inputedPassword))
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(((position.width - 188) / 2), 56 + 54, 188, 24), LocalizedStrings.PasswordIncorrect, BlueStonez.label_interparkbold_11pt);
                GUI.color = Color.white;
            }
        }
        GUI.EndGroup();
        GUITools.PopGUIState();
    }

    /// <summary>
    /// Game filter cehck using quick search bar and filter panel options
    /// </summary>
    private bool CanPassFilter(GameMetaData game)
    {
        if (game == null) return false;

        GameFlags flag = new GameFlags();
        flag.SetFlags(game.GameModifierFlags);

        bool quickSearch = _searchBar.CheckIfPassFilter(game.RoomName);
        bool selectedMap = true;
        bool selectedMode = true;
        bool gameNotFull = true;
        bool noPassword = true;

        if (_mapsFilter[_currentMap] == LocalizedStrings.All + " Maps")
            selectedMap = true;
        else
            selectedMap = LevelManager.Instance.GetMapName(game.MapID) == _mapsFilter[_currentMap];

        if (_modesFilter[_currentMode] == LocalizedStrings.All + " Modes")
            selectedMode = true;
        else
            selectedMode = GameModes.GetModeName((GameMode)game.GameMode) == _modesFilter[_currentMode];

        if (_gameNotFull)
            gameNotFull = !game.IsFull;

        if (_noPrivateGames)
            noPassword = game.IsPublic;

        if (_showFilters)
            return quickSearch && selectedMap && selectedMode && gameNotFull && noPassword && _showFilters;
        else
            return quickSearch;
    }

    /// <summary>
    /// Display each server's information
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="hasVScroll"></param>
    /// <returns></returns>
    private int DrawAllGames(Rect rect, bool hasVScroll)
    {
        int playerLevel = PlayerDataManager.PlayerLevelSecure;

        int i = 0;
        foreach (GameMetaData game in _cachedGameList)
        {
            if (CanPassFilter(game))
            {
                bool tmp = GUI.enabled;
                bool canJoinGame = (!game.IsFull && game.IsLevelAllowed(playerLevel) && (game.ConnectedPlayers > 0 || game.HasLevelRestriction))
                    || PlayerDataManager.AccessLevel >= MemberAccessLevel.SeniorModerator;

                canJoinGame &= LevelManager.Instance.HasMapWithId(game.MapID);

                GUI.enabled = tmp && canJoinGame && (_dropDownList == 0) && !_checkForPassword;

                int y = 48 * i - 1;

                string tooltip = LocalizedStrings.PlayCaps;
                if (!game.IsLevelAllowed(playerLevel) && game.LevelMin > playerLevel) tooltip = string.Format(LocalizedStrings.YouHaveToReachLevelNToJoinThisGame, game.LevelMin);
                else if (!game.IsLevelAllowed(playerLevel) && game.LevelMax < playerLevel) tooltip = string.Format(LocalizedStrings.YouAlreadyMasteredThisLevel);
                else if (game.IsFull) tooltip = string.Format(LocalizedStrings.ThisGameIsFull);
                else if (!game.HasLevelRestriction && game.ConnectedPlayers == 0) tooltip = string.Format(LocalizedStrings.ThisGameNoLongerExists);

                GUI.Box(new Rect(0, y, rect.width, 49), new GUIContent(string.Empty, tooltip), BlueStonez.box_grey50);
                if (_selectedGame != null && _selectedGame.RoomID == game.RoomID)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.1f);
                    GUI.DrawTexture(new Rect(1, y, rect.width + 1, 48), UberstrikeIcons.White);
                    GUI.color = Color.white;
                }
                int length = 48;

                GUIStyle style = canJoinGame ? BlueStonez.label_interparkbold_11pt_left : BlueStonez.label_interparkmed_10pt_left;

                int x = 0;
                if (!game.IsPublic)
                {
                    GUI.DrawTexture(new Rect(x, y + 12, 25, 25), _privateGameIcon);
                }
                else if (game.HasLevelRestriction)
                {
                    if (game.LevelMax <= 2) GUI.DrawTexture(new Rect(x + 5, y + 5, 40, 40), _level1GameIcon);
                    else if (game.LevelMax == 3) GUI.DrawTexture(new Rect(x + 5, y + 5, 40, 40), _level2GameIcon);
                    else if (game.LevelMax == 4) GUI.DrawTexture(new Rect(x + 5, y + 5, 40, 40), _level3GameIcon);
                    else if (game.LevelMin >= 20) GUI.DrawTexture(new Rect(x + 5, y + 5, 40, 40), _level20GameIcon);
                    else if (game.LevelMin >= 10) GUI.DrawTexture(new Rect(x + 5, y + 5, 40, 40), _level10GameIcon);
                    else if (game.LevelMin >= 5) GUI.DrawTexture(new Rect(x + 5, y + 5, 40, 40), _level5GameIcon);

                    if (playerLevel > game.LevelMax)
                    {
                        GUI.DrawTexture(new Rect(x, y, 50, 50), UberstrikeIcons.CheckMark);
                    }
                }
                else if (LevelManager.Instance.IsBlueBox(game.MapID))
                {
                    GUI.Label(new Rect(10, y + 6, 64, 64), new GUIContent(UberstrikeIcons.BlueBox, LocalizedStrings.BlueLevelTooltip));
                }

                GUI.color = (game.HasLevelRestriction) ? new Color(1, 0.7f, 0) : Color.white;

                //moderator
                x = 12 + _privateGameWidth - 1;

                //name
                x = 12 + _privateGameWidth + _moderatorInGameWidth - 2;
                GUI.Label(new Rect(x, y, _gameNameWidth, 48), game.RoomName, style);
                GUI.Label(new Rect(x + 5, y + 15, _gameNameWidth - 20, 48), LevelRestrictionText(game), BlueStonez.label_interparkmed_10pt_left);

                //map
                x = 12 + _privateGameWidth + _moderatorInGameWidth + _gameNameWidth - 3;
                GUI.Label(new Rect(x, y, _mapNameWidth, 48), LevelManager.Instance.GetMapName(game.MapID), style);

                //mode
                x = 12 + _privateGameWidth + _moderatorInGameWidth + _gameNameWidth + _mapNameWidth - 4;
                GUI.Label(new Rect(x, y, _gameModeWidth, 48), GameModes.GetModeName(game.GameMode), style);
                GUI.Label(new Rect(x, y + 12, _gameModeWidth, 48), FpsGameMode.GetGameFlagText(game), style);

                //time
                x = 12 + _privateGameWidth + _moderatorInGameWidth + _gameNameWidth + _mapNameWidth + _gameModeWidth - 5;
                GUI.Label(new Rect(x, y, _gameTimeWidth, 48), CmunePrint.Time(game.RoundTime), style);

                //player count
                x = 12 + _privateGameWidth + _moderatorInGameWidth + _gameNameWidth + _mapNameWidth + _gameModeWidth + _gameTimeWidth - 6;
                GUI.Label(new Rect(x, y, _playerCountWidth, 48), game.ConnectedPlayersString, style);

                if (GUI.Button(new Rect(0, y, rect.width, length), string.Empty, BlueStonez.label_interparkbold_11pt_left))
                {
                    //reset lobby timeout if we detect user interaction
                    ResetLobbyDisconnectTimeout();

                    //join by double click
                    if (_selectedGame != null && _selectedGame.RoomID == game.RoomID && _gameJoinDoubleClick > Time.time)
                    {
                        _gameJoinDoubleClick = 0;

                        if (_selectedGame.IsPublic || PlayerDataManager.AccessLevelSecure >= MemberAccessLevel.SeniorModerator)
                        {
                            JoinGame();
                            SfxManager.Play2dAudioClip(SoundEffectType.UIJoinGame);
                        }
                        else
                        {
                            _checkForPassword = true;
                            _nextPasswordCheck = Time.time;
                        }
                    }
                    else
                    {
                        _gameJoinDoubleClick = Time.time + _doubleClickFrame;
                    }

                    _selectedGame = game;
                }
                i++;
                GUI.color = Color.white;
                GUI.enabled = tmp;
            }
        }
        if (i == 0 && GameServerController.Instance.SelectedServer != null && GameServerController.Instance.SelectedServer.Data.RoomsCreated > 0 && _cachedGameList.Count > 0)
        {
            GUI.Label(new Rect(0, 0, rect.width, rect.height), "There are no games matching your filter", BlueStonez.label_interparkmed_11pt);
        }
        return i;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private string LevelRestrictionText(GameMetaData m)
    {
        if (m.HasLevelRestriction)
        {
            if (m.LevelMax == m.LevelMin)
            {
                return string.Format(LocalizedStrings.PlayerLevelNRestriction, m.LevelMin);
            }
            else if (m.LevelMax == byte.MaxValue)
            {
                return string.Format(LocalizedStrings.PlayerLevelNPlusRestriction, m.LevelMin);
            }
            else if (m.LevelMin == 0)
            {
                return string.Format(LocalizedStrings.PlayerLevelNMinusRestriction, m.LevelMax + 1);
            }
            else
            {
                return string.Format(LocalizedStrings.PlayerLevelNToNRestriction, m.LevelMin, m.LevelMax);
            }
        }
        else
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Only executed when screen size changes or enter/exit fullscreen
    /// </summary>
    private void UpdateColumnWidth()
    {
        int maxWidth = GUITools.ScreenWidth - 20;
        int spaceLeft = maxWidth - _moderatorInGameWidth - _privateGameWidth - _gameTimeWidth - _playerCountWidth;

        _gameModeWidth = Mathf.Clamp(Mathf.CeilToInt(spaceLeft * 25f / 100f), 100, 200);
        _mapNameWidth = Mathf.Clamp(Mathf.CeilToInt(spaceLeft * 25f / 100f), 100, 250);
        _gameNameWidth = spaceLeft - _gameModeWidth - _mapNameWidth + 6;
    }

    /// <summary>
    /// Start all routines that are neccessary to run the game OR server selection screen
    /// depending on the setting of <ref>IsConnectedToGameServer</ref>
    /// </summary>
    public void Show()
    {
        //update game list of currently selected server
        if (_isConnectedToGameServer)
        {
            ShowGameSelection();
        }
        //update game server list
        else
        {
            ShowServerSelection();
        }
    }

    /// <summary>
    /// Start all routines that are neccessary to run the game selection screen
    /// </summary>
    private void ShowGameSelection()
    {
        if (GameServerController.Instance.SelectedServer != null)
        {
            _isConnectedToGameServer = true;

            _cachedGameList.Clear();

            //GoogleAnalytics.Instance.LogEvent("ui-gameserver-click", GameServerController.Instance.SelectedServer.Region + "-" + GameServerController.Instance.SelectedServer.Name, GameServerController.Instance.SelectedServer.Latency, true);

            CmuneNetworkManager.CurrentGameServer = GameServerController.Instance.SelectedServer;
            CmuneNetworkManager.CurrentLobbyServer = GameServerController.Instance.SelectedServer;
            LobbyConnectionManager.StartConnection();

            CoroutineManager.StartCoroutine(StartUpdatingGameListFromServer);
            CoroutineManager.StartCoroutine(StartDisconnectFormServerAfterTimeout);
        }
    }

    /// <summary>
    /// Start all routines that are neccessary to run the server selection screen
    /// </summary>
    private void ShowServerSelection()
    {
        _isConnectedToGameServer = false;

        if (_lastSortedServerColumn == ServerListColumns.None)
        {
            _lastSortedServerColumn = ServerListColumns.ServerSpeed;

            SortServerList(_lastSortedServerColumn, false);
        }

        StopGameSelection();
        RefreshServerLoad();
    }

    /// <summary>
    /// Stop all routines that are neccessary to run the game & server selection screen
    /// </summary>
    public void Hide()
    {
        StopGameSelection();
    }

    /// <summary>
    /// Stop all routines that are neccessary to run the game selection screen
    /// </summary>
    private void StopGameSelection()
    {
        LobbyConnectionManager.Stop();

        //stop updateting game list
        CoroutineManager.StopCoroutine(StartUpdatingGameListFromServer);
        CoroutineManager.StopCoroutine(StartDisconnectFormServerAfterTimeout);

        //close password window
        CheckForPassword = false;
    }

    /// <summary>
    /// Refresh local cached game list with values from GameListManager
    /// </summary>
    public void RefreshGameList()
    {
        bool isSelectedGameFound = false;
        _cachedGameList.Clear();

        if (GameListManager.GamesCount > 0)
        {
            foreach (GameMetaData game in GameListManager.GameList)
            {
                _cachedGameList.Add(game);

                if (_selectedGame != null && game.RoomID == _selectedGame.RoomID)
                {
                    isSelectedGameFound = true;
                }
            }

            SortGames(_gameSortingMethod);

            ResetLobbyDisconnectTimeout();
        }
        else
        {
            Debug.LogWarning("Failed to sort game list because games count is zero!");
        }

        if (!isSelectedGameFound)
            _selectedGame = null;
    }

    private void SortGames(IComparer<GameMetaData> method)
    {
        GameDataComparer.SortAscending = _sortGamesAscending;
        _cachedGameList.Sort(method);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartUpdatingGameListFromServer()
    {
        int routineID = CoroutineManager.Begin(StartUpdatingGameListFromServer);

        GameMetaData game;
        while (CoroutineManager.IsCurrent(StartUpdatingGameListFromServer, routineID))
        {
            foreach (GameMetaData g in _cachedGameList)
            {
                if (GameListManager.TryGetGame(g.RoomID, out game))
                {
                    g.ConnectedPlayers = game.ConnectedPlayers;
                }
                else
                {
                    g.ConnectedPlayers = 0;
                }
                //yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(2);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    private IEnumerator StartDisconnectFormServerAfterTimeout()
    {
        int routineId = CoroutineManager.Begin(StartDisconnectFormServerAfterTimeout);

        ResetLobbyDisconnectTimeout();

        while (CoroutineManager.IsCurrent(StartDisconnectFormServerAfterTimeout, routineId))
        {
            if (_timeToLobbyDisconnect < Time.time)
            {
                if (LobbyConnectionManager.IsConnected)
                    LobbyConnectionManager.Stop();
            }

            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private void ResetLobbyDisconnectTimeout()
    {
        _timeToLobbyDisconnect = Time.time + LobbyConnectionMaxTime;
    }

    /// <summary>
    /// Request the server load data for all servers
    /// </summary>
    private void RefreshServerLoad()
    {
        if (_nextServerCheckTime < Time.time)
        {
            _nextServerCheckTime = Time.time + ServerCheckDelay;
            //GameServerManager.Instance.UpdateLatency();
            StartCoroutine(GameServerManager.Instance.StartUpdatingServerLoads());
        }
    }

    #region Inspector

    [SerializeField]
    private Texture2D _level1GameIcon;
    [SerializeField]
    private Texture2D _level2GameIcon;
    [SerializeField]
    private Texture2D _level3GameIcon;
    [SerializeField]
    private Texture2D _level5GameIcon;
    [SerializeField]
    private Texture2D _level10GameIcon;
    [SerializeField]
    private Texture2D _level20GameIcon;

    [SerializeField]
    private Texture2D _privateGameIcon;
    [SerializeField]
    private Texture2D _sortUpArrow;
    [SerializeField]
    private Texture2D _sortDownArrow;

    #endregion

    #region Properties

    public bool CheckForPassword
    {
        set { _checkForPassword = value; }
    }

    public bool IsConnectedToGameServer
    {
        get { return _isConnectedToGameServer; }
    }

    private bool AreFiltersActive
    {
        get
        {
            if (_currentMap != 0) return true;
            if (_currentMode != 0) return true;
            if (_currentWeapon != 0) return true;
            if (_noFriendFire) return true;
            if (_gameNotFull) return true;
            if (_noPrivateGames) return true;
            if (_instasplat) return true;
            if (_lowGravity) return true;
            if (_justForFun) return true;
            if (_singleWeapon) return true;

            return false;
        }
    }

    #endregion

    #region Fields

    private float _gameJoinDoubleClick = 0;
    private float _serverJoinDoubleClick;
    private const float _doubleClickFrame = 0.5f;

    private bool _isConnectedToGameServer = false;
    private bool _refreshGameListOnFilterChange = false;
    private bool _refreshGameListOnSortChange = false;

    private Vector2 _serverScroll;
    private Vector2 _filterScroll;

    private bool _gameNotFull = false;
    private bool _noPrivateGames = false;
    private bool _instasplat = false;
    private bool _lowGravity = false;
    private bool _justForFun = false;
    private bool _singleWeapon = false;
    private bool _noFriendFire = false;
    private bool _showFilters = false;

    private string[] _weaponClassTexts;

    private const int _dropDownListMap = 0x1;
    private const int _dropDownListGameMode = 0x2;
    private const int _dropDownListSingleWeapon = 0x4;

    private int _dropDownList = 0;
    private Rect _dropDownRect;

    private int _currentMap = 0;
    private int _currentMode = 0;
    private int _currentWeapon = 0;

    private GameMetaData _selectedGame = null;

    private const int _privateGameWidth = 25;
    private const int _moderatorInGameWidth = 25;
    private const int _gameTimeWidth = 80;
    private const int _playerCountWidth = 80;
    private int _mapNameWidth = 135;
    private int _gameModeWidth = 80;
    private int _gameNameWidth = 200;

    private const float _serverSpeedColumnWidth = 110;
    private const float _serverCapacityColumnWidth = 130;
    private float _serverNameColumnWidth;

    private string _inputedPassword = string.Empty;

    private bool _unFocus = false;
    private bool _checkForPassword = false;
    private bool _isPasswordOk = true;

    private float _timeDelayOnCheckPassword = 2f;
    private float _nextPasswordCheck;
    private bool _sortServerAscending = false;
    private bool _sortGamesAscending = false;
    private int _filteredActiveRoomCount = 0;

    private GameListColumns _lastSortedColumn = GameListColumns.None;

    private FilterSavedData _filterSavedData;
    private List<GameMetaData> _cachedGameList;
    private IComparer<GameMetaData> _gameSortingMethod;

    private Vector2 _serverSelectionHelpScrollBar;
    private Vector2 _serverSelectionScrollBar;

    private ServerListColumns _lastSortedServerColumn = ServerListColumns.None;

    private float _nextServerCheckTime = 0f;

    private float _timeToLobbyDisconnect = 0;
    private const int LobbyConnectionMaxTime = 30;
    private const int ServerCheckDelay = 5;

    private SearchBarGUI _searchBar;
    //private ServerGuiList _serverList = new ServerGuiList();

    #endregion

    #region Enums

    private enum ServerLatency
    {
        Fast = 100,
        Med = 300,
    }

    private enum GameListColumns
    {
        None,
        Lock,
        Star,
        GameName,
        GameMap,
        GameMode,
        PlayerCount,
        GameServerPing,
        GameTime,
    }

    private enum ServerListColumns
    {
        None,
        ServerName,
        ServerCapacity,
        ServerSpeed,
    }

    #endregion

    private class FilterSavedData
    {
        public bool UseFilter = false;
        public string MapName = string.Empty;
        public string GameMode = string.Empty;
        public bool NoFriendlyFire = false;
        public bool ISGameNotFull = false;
        public bool NoPasswordProtection = false;
    }
}