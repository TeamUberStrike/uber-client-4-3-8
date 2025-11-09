using UberStrike.Core.Types;
using UberStrike.Helper;
using UberStrike.Realtime.Common;
using UnityEngine;
using Cmune.Util;

public class CreateGamePanelGUI : MonoBehaviour, IPanelGui
{
    private void Awake()
    {
        _gameName = string.Empty;
    }

    private void Start()
    {
        _dmDescMsg = LocalizedStrings.DMModeDescriptionMsg;
        _tdmDescMsg = LocalizedStrings.TDMModeDescriptionMsg;
        _elmDescMsg = LocalizedStrings.ELMModeDescriptionMsg;

        _modeSelection.Add(GameModeType.TeamDeathMatch, new GUIContent(LocalizedStrings.TeamDeathMatch));
        _modeSelection.Add(GameModeType.EliminationMode, new GUIContent(LocalizedStrings.TeamElimination));
        _modeSelection.Add(GameModeType.DeathMatch, new GUIContent(LocalizedStrings.DeathMatch));
        _modeSelection.OnSelectionChange += (mode) => { };
    }

    private void Update()
    {
        if ((_windowRect.width != MAX_WIDTH && Screen.width >= ApplicationDataManager.MinimalWidth) ||
            (_windowRect.width != MIN_WIDTH && Screen.width < ApplicationDataManager.MinimalWidth))
            _animatingWidth = true;

        if (_animatingWidth)
        {
            if (Screen.width < ApplicationDataManager.MinimalWidth)
            {
                _sliderWidth = Mathf.Lerp(_sliderWidth, 160, Time.deltaTime * 8);
                _textFieldWidth = Mathf.Lerp(_textFieldWidth, MAX_NAME_FIELD_WIDTH, Time.deltaTime * 8);
                _windowRect.width = Mathf.Lerp(_windowRect.width, MIN_WIDTH, Time.deltaTime * 8);

                if (Mathf.Approximately(_windowRect.width, MIN_WIDTH))
                {
                    _animatingWidth = false;

                    _sliderWidth = 160;
                    _textFieldWidth = MAX_NAME_FIELD_WIDTH;
                    _windowRect.width = MIN_WIDTH;
                }
            }
            else
            {
                _sliderWidth = Mathf.Lerp(_sliderWidth, 130, Time.deltaTime * 8);
                _textFieldWidth = Mathf.Lerp(_textFieldWidth, MIN_NAME_FIELD_WIDTH, Time.deltaTime * 8);
                _windowRect.width = Mathf.Lerp(_windowRect.width, MAX_WIDTH, Time.deltaTime * 8);

                if (Mathf.Approximately(_windowRect.width, MAX_WIDTH))
                {
                    _animatingWidth = false;

                    _sliderWidth = 130;
                    _textFieldWidth = MIN_NAME_FIELD_WIDTH;
                    _windowRect.width = MAX_WIDTH;
                }
            }
        }

        if (_animatingIndex)
        {
            if (_viewingLeft)
            {
                _xOffset = Mathf.Lerp(_xOffset, LEFT_X, Time.deltaTime * 8);
                if (Mathf.Abs(_xOffset - LEFT_X) < 2)
                {
                    _xOffset = LEFT_X;
                    _animatingIndex = false;
                }
            }
            else
            {
                _xOffset = Mathf.Lerp(_xOffset, RIGHT_X, Time.deltaTime * 8);
                if (Mathf.Abs(RIGHT_X - _xOffset) < 2)
                {
                    _xOffset = RIGHT_X;
                    _animatingIndex = false;
                }
            }
        }

        _windowRect.x = (Screen.width - _windowRect.width) * 0.5f;
        _windowRect.y = (Screen.height - _windowRect.height) * 0.5f + 25;
    }

    private void OnGUI()
    {
        GUI.BeginGroup(_windowRect, GUIContent.none, BlueStonez.window);
        DrawCreateGamePanel();
        GUI.EndGroup();

        GuiManager.DrawTooltip();
    }

    private void OnEnable()
    {
        _windowRect.width = (Screen.width < ApplicationDataManager.MinimalWidth) ? MIN_WIDTH : MAX_WIDTH;
        _windowRect.height = 420;

        _password = string.Empty;

        if (Screen.width < ApplicationDataManager.MinimalWidth)
        {
            _sliderWidth = 160;
            _windowRect.width = MIN_WIDTH;
            _textFieldWidth = MAX_NAME_FIELD_WIDTH;
        }
        else
        {
            _sliderWidth = 130;
            _windowRect.width = MAX_WIDTH;
            _textFieldWidth = MIN_NAME_FIELD_WIDTH;
        }
    }

    public void Show()
    {
        enabled = true;

        //always start at the left
        _viewingLeft = true;

        //can't use localized game name because not sure if the regex is passed
        _gameName = PlayerDataManager.Name;
        if (_gameName.Length > 18)
        {
            _gameName = _gameName.Remove(18);
        }
    }

    public void Hide()
    {
        enabled = false;
    }

    #region Private Methods

    private void DrawCreateGamePanel()
    {
        GUI.skin = BlueStonez.Skin;
        GUI.depth = (int)GuiDepth.Panel;
        GUI.Label(new Rect(0, 0, _windowRect.width, 56), LocalizedStrings.CreateGameCaps, BlueStonez.tab_strip);

        Rect rect = new Rect(0, 60, _windowRect.width, _windowRect.height - 60);

        if (Screen.width < ApplicationDataManager.MinimalWidth)
            DrawRestrictedPanel(rect);
        else
            DrawFullPanel(rect);
    }

    private void SelectMap(UberstrikeMap map)
    {
        _mapSelected = map;

        // update GameMode restriction per map
        foreach (GameModeType mode in _modeSelection.Items)
        {
            if (_mapSelected.IsGameModeSupported(mode))
            {
                _modeSelection.Select(mode);
                break;
            }
        }
    }

    private void DrawMapSelection(Rect rect)
    {
        float buttonWidth = (LevelManager.Instance.Count > 8) ? (rect.width - 18) : rect.width;

        _scroll = GUI.BeginScrollView(rect, _scroll, new Rect(0, 0, rect.width - 18, 10 + LevelManager.Instance.Count * 35));
        {
            int i = 0;
            foreach (var map in LevelManager.Instance.AllMaps)
            {
                if (!map.IsEnabled) continue;

                /* Select first space if null */
                if (_mapSelected == null)
                {
                    SelectMap(map);
                }

                GUIContent content = new GUIContent(map.Name, map.IsBluebox ? LocalizedStrings.BlueLevelTooltip : map.Name);
                if (GUI.Toggle(new Rect(0, i * 35, buttonWidth, 35), (map == _mapSelected), content, BlueStonez.tab_large))
                {
                    if (_mapSelected != map)
                    {
                        SfxManager.Play2dAudioClip(SoundEffectType.UICreateGame);

                        SelectMap(map);
                    }
                }
                if (map.IsBluebox)
                    GUI.Label(new Rect(6, i * 35 + 5, 32, 32), UberstrikeIcons.BlueBox);

                i++;
            }
        }
        GUI.EndScrollView();
    }

    private void DrawGameModeSelection(Rect rect)
    {
        GUI.BeginGroup(rect);
        for (int i = 0; i < _modeSelection.Items.Length; i++)
        {
            GUITools.PushGUIState();

            if (_mapSelected != null && !_mapSelected.IsGameModeSupported(_modeSelection.Items[i]))
            {
                GUI.enabled = false;
            }

            if (GUI.Toggle(new Rect(0, i * 20, rect.width, 20), (i == _modeSelection.Index), _modeSelection.GuiContent[i], BlueStonez.tab_medium))
            {
                if (_modeSelection.Index != i)
                {
                    _modeSelection.SetIndex(i);

                    if (GUI.changed)
                    {
                        GUI.changed = false;
                        SfxManager.Play2dAudioClip(SoundEffectType.UICreateGame);
                    }
                }
            }
            GUI.enabled = true;
            GUITools.PopGUIState();
        }
        GUI.EndGroup();
    }

    private void DrawGameDescription(Rect rect)
    {
        string modeDesc = string.Empty;

        switch (_modeSelection.Current)
        {
            case GameModeType.DeathMatch:
                modeDesc = _dmDescMsg;
                break;
            case GameModeType.TeamDeathMatch:
                modeDesc = _tdmDescMsg;
                break;
            case GameModeType.EliminationMode:
                modeDesc = _elmDescMsg;
                break;
        }

        GUI.BeginGroup(rect);

        if (_mapSelected != null)
        {
            int y = 0;

            _mapSelected.Icon.Draw(new Rect(0, OFFSET, rect.width, rect.width * _mapSelected.Icon.Aspect));
            y += OFFSET + Mathf.RoundToInt(rect.width * _mapSelected.Icon.Aspect);

            GUI.Label(new Rect(OFFSET, y, rect.width - 2 * OFFSET, 20), "Mission", BlueStonez.label_interparkbold_11pt_left);

            y += 20;

            GUI.Label(new Rect(OFFSET, y, rect.width - 2 * OFFSET, 60), modeDesc, BlueStonez.label_itemdescription);

            y += 36;

            GUI.Label(new Rect(OFFSET, y, rect.width - 2 * OFFSET, 20), "Location", BlueStonez.label_interparkbold_11pt_left);

            y += 20;

            GUI.Label(new Rect(OFFSET, y, rect.width - 2 * OFFSET, 100), _mapSelected.Description, BlueStonez.label_itemdescription);
        }
        else
        {
            GUI.Label(new Rect(OFFSET, 100, rect.width - 2 * OFFSET, 100), "Please select a map", BlueStonez.label_interparkbold_16pt);
        }

        GUI.EndGroup();
    }

    private void DrawGameConfiguration(Rect rect)
    {
        if (IsModeSupported)
        {
            var settings = _mapSelected.View.Settings[_modeSelection.Current];

            GUI.BeginGroup(rect);
            {
                /* Game Name */
                GUI.Label(new Rect(OFFSET, 3, 100, 30), LocalizedStrings.GameName, BlueStonez.label_interparkbold_18pt_left);
                GUI.SetNextControlName("GameName");
                _gameName = GUI.TextField(new Rect(120, 8, _textFieldWidth, 24), _gameName, 18, BlueStonez.textField);

                if (string.IsNullOrEmpty(_gameName) && !GUI.GetNameOfFocusedControl().Equals("GameName"))
                {
                    GUI.color = new Color(1, 1, 1, 0.3f);
                    GUI.Label(new Rect(128, 15, 200, 24), LocalizedStrings.EnterGameName, BlueStonez.label_interparkmed_11pt_left);
                    GUI.color = Color.white;
                }
                if (_gameName.Length > 18)
                {
                    _gameName = _gameName.Remove(18);
                }

                GUI.Label(new Rect(120 + _textFieldWidth + 16, 8, 100, 24), "(" + _gameName.Length + "/18)", BlueStonez.label_interparkbold_11pt_left);

                /* Game Passwd */
                GUI.Label(new Rect(OFFSET, 36, 100, 30), LocalizedStrings.Password, BlueStonez.label_interparkbold_18pt_left);
                GUI.SetNextControlName("GamePasswd");
                _password = GUI.PasswordField(new Rect(120, 38, _textFieldWidth, 24), _password, '*', 8);
                _password = _password.Trim(new char[] { '\n' });

                if (string.IsNullOrEmpty(_password) && !GUI.GetNameOfFocusedControl().Equals("GamePasswd"))
                {
                    GUI.color = new Color(1, 1, 1, 0.3f);
                    GUI.Label(new Rect(128, 45, 200, 24), "No password", BlueStonez.label_interparkmed_11pt_left);
                    GUI.color = Color.white;
                }
                if (_password.Length > 8)
                    _password = _password.Remove(8);

                GUI.Label(new Rect(120 + _textFieldWidth + 16, 38, 100, 24), "(" + _password.Length + "/8)", BlueStonez.label_interparkbold_11pt_left);

                /* Players Limit */
                GUI.Label(new Rect(OFFSET, 70, 100, 30), LocalizedStrings.MaxPlayers, BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(120, 73, 33, 20), Mathf.RoundToInt(settings.PlayersCurrent).ToString(), BlueStonez.label_dropdown);
                settings.PlayersCurrent = (int)GUI.HorizontalSlider(new Rect(160, 77, _sliderWidth, 20), settings.PlayersCurrent, settings.PlayersMin, settings.PlayersMax);

                /* Time Limit */
                GUI.Label(new Rect(OFFSET, 70 + 30, 100, 30), LocalizedStrings.TimeLimit, BlueStonez.label_interparkbold_18pt_left);
                GUI.Label(new Rect(120, 103, 33, 20), Mathf.RoundToInt((settings.TimeCurrent / 60)).ToString("N0"), BlueStonez.label_dropdown);
                settings.TimeCurrent = (int)GUI.HorizontalSlider(new Rect(160, 77 + 30, _sliderWidth, 20), settings.TimeCurrent, settings.TimeMin, settings.TimeMax);
                //settings.TimeCurrent = settings.TimeCurrent - (settings.TimeCurrent % 60);

                /* Splat Limit */
                GUI.Label(new Rect(OFFSET, 70 + 62, 100, 30), (_modeSelection.Current == GameModeType.EliminationMode) ? LocalizedStrings.MaxRounds : LocalizedStrings.MaxKills, BlueStonez.label_interparkbold_18pt_left); //LOCALE
                GUI.Label(new Rect(120, 135, 33, 20), Mathf.RoundToInt(settings.KillsCurrent).ToString(), BlueStonez.label_dropdown);
                settings.KillsCurrent = (int)GUI.HorizontalSlider(new Rect(160, 77 + 62, _sliderWidth, 20), settings.KillsCurrent, settings.KillsMin, settings.KillsMax);

                //ToggleGameFlag(GameFlags.GAME_FLAGS.LowGravity, 206, LocalizedStrings.LowGravity);
                //ToggleGameFlag(GameFlags.GAME_FLAGS.Instakill, 226, LocalizedStrings.Instakill);
                //ToggleGameFlag(GameFlags.GAME_FLAGS.NinjaArena, 246, LocalizedStrings.NinjaArena);
                //ToggleGameFlag(GameFlags.GAME_FLAGS.SniperArena, 266, LocalizedStrings.SniperArena);
                //ToggleGameFlag(GameFlags.GAME_FLAGS.CannonArena, 286, LocalizedStrings.CannonArena);
            }
            GUI.EndGroup();
        }
        else
        {
            GUI.Label(rect, "Unsupported Game Mode!", BlueStonez.label_interparkbold_18pt);
        }
    }

    private void ToggleGameFlag(GameFlags.GAME_FLAGS flag, int y, string content)
    {
        bool result = GUI.Toggle(new Rect(OFFSET, y, 160, 16), _gameFlags == flag, content, BlueStonez.toggle);

        if (result)
            _gameFlags = flag;
        else if (_gameFlags == flag)
            _gameFlags = GameFlags.GAME_FLAGS.None;
    }

    private bool IsModeSupported
    {
        get
        {
            return _mapSelected != null && _mapSelected.IsGameModeSupported(_modeSelection.Current);
        }
    }

    private void DrawFullPanel(Rect rect)
    {
        int x = OFFSET;
        int height = (int)rect.height - BUTTON_HEIGHT;

        GUI.BeginGroup(rect);
        {
            GUI.Box(new Rect(OFFSET, 0, rect.width - OFFSET * 2, height), GUIContent.none, BlueStonez.window_standard_grey38);

            DrawMapSelection(new Rect(x, 0, MAP_WIDTH, height));

            x += MAP_WIDTH + OFFSET;

            DrawVerticalLine(x - 3, 2, 300);
            DrawGameModeSelection(new Rect(x, 0, MODE_WIDTH, height));

            x += MODE_WIDTH + OFFSET;

            DrawVerticalLine(x - 3, 2, 300);
            DrawGameDescription(new Rect(x, 0, DESC_WIDTH, height));

            x += DESC_WIDTH + OFFSET;

            DrawVerticalLine(x - 3, 2, 300);
            DrawGameConfiguration(new Rect(x, 0, MODS_WIDTH, height));

            if (GUITools.Button(new Rect(rect.width - 138, rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                PanelManager.Instance.ClosePanel(PanelType.CreateGame);
            }

            GUITools.PushGUIState();
            
            // Debug: Print all condition variables
            bool isModeSupported = IsModeSupported;
            bool isServerValid = CmuneNetworkManager.CurrentGameServer.IsValid;
            bool isNameValid = LocalizationHelper.ValidateMemberName(_gameName, ApplicationDataManager.CurrentLocale);
            bool isPasswordValid = string.IsNullOrEmpty(_password) || ValidateGamePassword(_password);
            
            Debug.Log("=== CREATE BUTTON DEBUG (DrawFullPanel) ===");
            Debug.Log("IsModeSupported: " + isModeSupported);
            Debug.Log("CurrentGameServer.IsValid: " + isServerValid);
            Debug.Log("ValidateMemberName(_gameName='" + _gameName + "', Locale=" + ApplicationDataManager.CurrentLocale + "): " + isNameValid);
            Debug.Log("Password (isEmpty=" + string.IsNullOrEmpty(_password) + ", ValidateGamePassword='" + _password + "'): " + isPasswordValid);
            Debug.Log("Final GUI.enabled: " + (isModeSupported && isServerValid && isNameValid && isPasswordValid));
            
            GUI.enabled = isModeSupported && isServerValid && isNameValid && isPasswordValid;
            if (GUITools.Button(new Rect(rect.width - 138 - 125, rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.CreateCaps), BlueStonez.button_green, SoundEffectType.UIJoinGame))
            {
                PanelManager.Instance.ClosePanel(PanelType.CreateGame);

                var settings = _mapSelected.View.Settings[_modeSelection.Current];
                _gameName = TextUtilities.Trim(_gameName);
                GameLoader.Instance.CreateGame(_mapSelected, _gameName, _password, settings.TimeCurrent, settings.KillsCurrent, settings.PlayersCurrent, _modeSelection.Current, _gameFlags);
            }
            GUITools.PopGUIState();
        }
        GUI.EndGroup();
    }

    private void DrawRestrictedPanel(Rect rect)
    {
        float x = OFFSET - _xOffset;
        int height = (int)rect.height - BUTTON_HEIGHT;

        GUI.BeginGroup(rect);
        {
            GUI.Box(new Rect(OFFSET, 0, rect.width - OFFSET * 2, height), GUIContent.none, BlueStonez.window_standard_grey38);

            if (_animatingIndex || _viewingLeft)
            {
                DrawMapSelection(new Rect(x, 0, MAP_WIDTH, height));
            }

            x += MAP_WIDTH + OFFSET;

            if (_animatingIndex || _viewingLeft)
            {
                DrawVerticalLine(x - 3, 2, 300);
                DrawGameModeSelection(new Rect(x, 0, MODE_WIDTH, height));
            }

            x += MODE_WIDTH + OFFSET;

            if (_animatingIndex || _viewingLeft)
                DrawVerticalLine(x - 3, 2, 300);

            DrawGameDescription(new Rect(x, 0, DESC_WIDTH, height));

            x += DESC_WIDTH + OFFSET;

            if (_animatingIndex || !_viewingLeft)
                DrawVerticalLine(x - 3, 2, 300);

            DrawGameConfiguration(new Rect(x, 0, MODS_WIDTH, height));

            if (GUITools.Button(new Rect(rect.width - 138, rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
            {
                PanelManager.Instance.ClosePanel(PanelType.CreateGame);
            }

            GUITools.PushGUIState();
            GUI.enabled = !_animatingIndex && !_animatingWidth;
            string button = _viewingLeft ? "Customize" : "Back";
            if (GUITools.Button(new Rect(rect.width - 138 - 125, rect.height - 40, 120, 32), new GUIContent(button), BlueStonez.button))
            {
                _animatingIndex = true;
                _viewingLeft = !_viewingLeft;
            }
            GUITools.PopGUIState();

            GUITools.PushGUIState();
            
            // Debug: Print all condition variables
            bool isModeSupported = IsModeSupported;
            bool isServerValid = CmuneNetworkManager.CurrentGameServer.IsValid;
            bool isNameValid = LocalizationHelper.ValidateMemberName(_gameName, ApplicationDataManager.CurrentLocale);
            bool isPasswordValid = string.IsNullOrEmpty(_password) || ValidateGamePassword(_password);
            
            Debug.Log("=== CREATE BUTTON DEBUG (DrawRestrictedPanel) ===");
            Debug.Log("IsModeSupported: " + isModeSupported);
            Debug.Log("CurrentGameServer.IsValid: " + isServerValid);
            Debug.Log("ValidateMemberName(_gameName='" + _gameName + "', Locale=" + ApplicationDataManager.CurrentLocale + "): " + isNameValid);
            Debug.Log("Password (isEmpty=" + string.IsNullOrEmpty(_password) + ", ValidateGamePassword='" + _password + "'): " + isPasswordValid);
            Debug.Log("Final GUI.enabled: " + (isModeSupported && isServerValid && isNameValid && isPasswordValid));
            
            GUI.enabled = isModeSupported && isServerValid && isNameValid && isPasswordValid;
            if (GUITools.Button(new Rect(rect.width - 138 - 250, rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.CreateCaps), BlueStonez.button_green))
            {
                PanelManager.Instance.ClosePanel(PanelType.CreateGame);

                var settings = _mapSelected.View.Settings[_modeSelection.Current];
                GameLoader.Instance.CreateGame(_mapSelected, _gameName, _password, settings.TimeCurrent, settings.KillsCurrent, settings.PlayersCurrent, _modeSelection.Current, _gameFlags);
            }
            GUITools.PopGUIState();
        }
        GUI.EndGroup();
    }

    private void DrawVerticalLine(float x, float y, float height)
    {
        GUI.Label(new Rect(x, y, 1, height), GUIContent.none, BlueStonez.vertical_line_grey95);
    }

    private bool ValidateGamePassword(string psv)
    {
        bool result = false;

        if (!string.IsNullOrEmpty(psv) && psv.Length <= 8)
            result = true;

        return result;
    }

    public bool IsEnabled
    {
        get { return enabled; }
    }
    #endregion

    #region Fields

    private const int OFFSET = 6;
    private const int BUTTON_HEIGHT = 50;
    private const int MAP_WIDTH = 200;
    private const int MODE_WIDTH = 160;
    private const int DESC_WIDTH = 255;
    private const int MODS_WIDTH = 360;

    private const int MIN_WIDTH = 640;
    private const int MAX_WIDTH = 960;

    private const int MIN_NAME_FIELD_WIDTH = 115;
    private const int MAX_NAME_FIELD_WIDTH = 150;

    private bool _animatingWidth = false;
    private bool _animatingIndex = false;

    private const int LEFT_X = 0;
    private const int RIGHT_X = 370;

    private float _xOffset;
    private bool _viewingLeft = true;

    //private bool _lowGravity = false;
    //private bool _instakill = false;
    //private bool _ninjaArena = false;
    //private bool _sniperArena = false;
    //private bool _cannonArena = false;

    private GameFlags.GAME_FLAGS _gameFlags = GameFlags.GAME_FLAGS.None;

    /* Map */
    private UberstrikeMap _mapSelected = null;
    SelectionGroup<GameModeType> _modeSelection = new SelectionGroup<GameModeType>();

    private Rect _windowRect;
    private Vector2 _scroll = Vector2.zero;

    private string _gameName = string.Empty;
    private string _password = string.Empty;

    private float _textFieldWidth = 170;
    private float _sliderWidth = 130;

    /* Mode descriptions */
    private string _dmDescMsg = string.Empty;
    private string _tdmDescMsg = string.Empty;
    private string _elmDescMsg = string.Empty;
    #endregion
}