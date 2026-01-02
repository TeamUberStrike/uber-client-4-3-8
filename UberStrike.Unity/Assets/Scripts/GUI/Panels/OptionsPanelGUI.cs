using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using UnityEngine;
using Cmune.Util;

public class OptionsPanelGUI : PanelGuiBase
{
    #region Fields

    bool showResolutions = false;
    bool graphicsChanged = false;

    string[] qualitySet;
    string[] vsyncSet = new string[] { "Off", "Low", "High" };
    string[] antiAliasingSet = new string[] { "Off", "2x", "4x", "8x" };
    string[] waterSet = new string[] { "Low", "Medium", "High" };

    const int MasterTextureLimit = 5;

    int _currentQuality = 0;
    float _targetFrameRate = -1;
    float _textureQuality = 0;
    float _queuedFrames = 0;
    int _vsync = 0;
    int _antiAliasing = 0;
    int _waterQuality = 0;

    private Rect _rect;

    private Vector2 _scrollVideo;
    private Vector2 _scrollControls;

    private int _desiredWidth = 0;
    private int _selectedOptionsTab = TAB_VIDEO;
    private GUIContent[] _optionsTabs;

    private UserInputMap _targetMap = null;

    private const int TAB_GENERAL = 0;
    private const int TAB_CONTROL = 1;
    private const int TAB_AUDIO = 2;
    private const int TAB_VIDEO = 3;    
    private const int TAB_SYSINFO = 4;

    private bool _showWaterModeMenu = false;

    private int _keyCount;

    private string[] _screenResText;

    private bool INSTANT_SCREEN_RES_CHANGE = true;
    private bool _isFullscreenBefore = false;
    private float _screenResChangeDelay = 0;
    private int _newScreenResIndex = 0;

    #endregion

    private void Awake()
    {
        List<string> resText = new List<string>();

        int i = 0;
        string suffix = string.Empty;

        foreach (var r in ScreenResolutionManager.Resolutions)
        {
            if (++i == ScreenResolutionManager.Resolutions.Count)
                suffix = string.Format("({0})", "fullscreen only");

            resText.Add(string.Format("{0} X {1} {2}", r.width, r.height, suffix));
        }

        qualitySet = new string[QualitySettings.names.Length + 1];
        for (int j = 0; j < qualitySet.Length; j++)
        {
            if (j < QualitySettings.names.Length)
                qualitySet[j] = QualitySettings.names[j];
            else
                qualitySet[j] = "Custom";
        }

        _screenResText = resText.ToArray();
    }

    private void OnEnable()
    {
        SyncGraphicsSettings();
    }

    private void Start()
    {
        if (ApplicationDataManager.IsMobile)
        {
            _optionsTabs = new GUIContent[] { 
                new GUIContent(LocalizedStrings.GeneralCaps), 
                new GUIContent(LocalizedStrings.ControlsCaps),
                new GUIContent(LocalizedStrings.AudioCaps) 
            };

            _selectedOptionsTab = TAB_CONTROL;
        }
        else
        {
            _optionsTabs = new GUIContent[] { 
                new GUIContent(LocalizedStrings.GeneralCaps), 
                new GUIContent(LocalizedStrings.ControlsCaps), 
                new GUIContent(LocalizedStrings.AudioCaps), 
                new GUIContent(LocalizedStrings.VideoCaps), 
                new GUIContent(LocalizedStrings.SysInfoCaps) 
            };
            _keyCount = InputManager.Instance.KeyMapping.Values.Count;
        }
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.Panel - 100;

        _rect = new Rect((Screen.width - 528) / 2, (Screen.height - 300) / 2, 528, 300);

        GUI.BeginGroup(_rect, GUIContent.none, BlueStonez.window_standard_grey38);
        {
            if (_screenResChangeDelay > 0)
                DrawScreenResChangePanel();
            else
                DrawOptionsPanel();
        }
        GUI.EndGroup();

        GuiManager.DrawTooltip();
    }

    private void DrawOptionsPanel()
    {
        GUI.SetNextControlName("OptionPanelHeading");
        GUI.Label(new Rect(0, 0, _rect.width, 56), LocalizedStrings.OptionsCaps, BlueStonez.tab_strip);

        if (GUI.GetNameOfFocusedControl() != "OptionPanelHeading")
        {
            GUI.FocusControl("OptionPanelHeading");
        }

        //Draw Stats Tabs
        if (ApplicationDataManager.IsMobile)
        {
            _selectedOptionsTab = GUI.SelectionGrid(new Rect(2, 31, _rect.width - 5, 22), _selectedOptionsTab, _optionsTabs, 3, BlueStonez.tab_medium);
        }
        else
        {
            _selectedOptionsTab = GUI.SelectionGrid(new Rect(2, 31, _rect.width - 5, 22), _selectedOptionsTab, _optionsTabs, 5, BlueStonez.tab_medium);
        }
        if (GUI.changed)
        {
            GUI.changed = false;
            SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
        }

        // Do Tabs
        GUI.BeginGroup(new Rect(16, 55, _rect.width - 32, _rect.height - 56 - 44), string.Empty, BlueStonez.window_standard_grey38);
        switch (_selectedOptionsTab)
        {
            case TAB_GENERAL:
                DoGeneralGroup();
                break;

            case TAB_CONTROL:
                DoControlsGroup();
                break;

            case TAB_VIDEO:
                DoVideoGroup();
                break;

            case TAB_AUDIO:
                DoAudioGroup();
                break;

            case TAB_SYSINFO:
                DoSysInfoGroup();
                break;
        }

        GUI.EndGroup();

        GUI.enabled = !_showWaterModeMenu;

        if (GUI.Button(new Rect(_rect.width - 136, _rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button))
        {
            ApplicationDataManager.ApplicationOptions.SaveApplicationOptions();
            PanelManager.Instance.ClosePanel(PanelType.Options);
        }

        if (InputManager.Instance.HasUnassignedKeyMappings)
        {
            GUI.contentColor = Color.red;
            GUI.Label(new Rect(150 + 16, _rect.height - 40, _rect.width - 136 - 166, 32), LocalizedStrings.UnassignedKeyMappingsWarningMsg, BlueStonez.label_interparkmed_11pt);
            GUI.contentColor = Color.white;
        }

        if (_selectedOptionsTab == TAB_CONTROL && !ApplicationDataManager.IsMobile && GUITools.Button(new Rect(16, _rect.height - 40, 150, 32), new GUIContent(LocalizedStrings.ResetDefaults), BlueStonez.button))
        {
            InputManager.Instance.Reset();
        }
        else if (_selectedOptionsTab == TAB_VIDEO)
        {
            GUI.Label(new Rect(16, _rect.height - 40, 150, 32), "FPS: " + (1 / Time.smoothDeltaTime).ToString("F1"), BlueStonez.label_interparkbold_16pt_left);
        }
        GUI.enabled = true;
    }

    private void DrawScreenResChangePanel()
    {
        GUI.depth = (int)GuiDepth.Popup;
        GUI.Label(new Rect(0, 0, _rect.width, 56), "Changing Screen Resolution...", BlueStonez.tab_strip);

        GUI.BeginGroup(new Rect(16, 55, _rect.width - 32, _rect.height - 56 - 54), string.Empty, BlueStonez.window_standard_grey38);
        {
            GUI.Label(new Rect(24, 18, 460, 20), "Do you want to choose new resolution: " + _screenResText[_newScreenResIndex] + " ?", BlueStonez.label_interparkbold_16pt_left);
            GUI.Label(new Rect(0, 0, _rect.width - 32, _rect.height - 56 - 54), ((int)_screenResChangeDelay).ToString(), BlueStonez.label_interparkbold_48pt);
        }
        GUI.EndGroup();

        if (GUITools.Button(new Rect(_rect.width - 136 - 140, _rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.OkCaps), BlueStonez.button))
        {
            ScreenResolutionManager.SetResolution(_newScreenResIndex, true);

            _screenResChangeDelay = 0;
            GuiLockController.ReleaseLock(GuiDepth.Popup);
        }

        if (GUITools.Button(new Rect(_rect.width - 136, _rect.height - 40, 120, 32), new GUIContent(LocalizedStrings.CancelCaps), BlueStonez.button))
        {
            _screenResChangeDelay = 0;
            GuiLockController.ReleaseLock(GuiDepth.Popup);

            if (_isFullscreenBefore)
                ScreenResolutionManager.IsFullScreen = true;
        }
    }

    private void DoGeneralGroup()
    {
        float height = 100;
        float width = (_rect.height - 55 - 46) < height ? _rect.width - 65 : _rect.width - 50;
        _scrollControls = GUITools.BeginScrollView(new Rect(1, 1, _rect.width - 33, _rect.height - 55 - 46), _scrollControls, new Rect(0, 0, _rect.width - 50, height));
        {
            DrawGroupControl(new Rect(GroupMarginX, 20, width, 85), LocalizedStrings.Misc, BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(new Rect(GroupMarginX, 20, width, 85));
            {
                ApplicationDataManager.ApplicationOptions.GameplayAutoPickupEnabled = GUI.Toggle(new Rect(12, 15, 200, 20), ApplicationDataManager.ApplicationOptions.GameplayAutoPickupEnabled, LocalizedStrings.AutoPickupWeapons, BlueStonez.toggle);
                ApplicationDataManager.ApplicationOptions.GameplayAutoEquipEnabled = GUI.Toggle(new Rect(12, 35, 200, 20), ApplicationDataManager.ApplicationOptions.GameplayAutoEquipEnabled, LocalizedStrings.AutoEquipWeapons, BlueStonez.toggle);
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
    }

    private const int GroupMarginX = 8;

    private void Update()
    {
        if (_screenResChangeDelay > 0)
        {
            _screenResChangeDelay -= Time.deltaTime;

            if (_screenResChangeDelay <= 0)
                GuiLockController.ReleaseLock(GuiDepth.Popup);
        }

        if (Input.GetMouseButtonUp(0) && graphicsChanged)
        {
            UpdateApplicationFrameRate();
            UpdateMaxQueuedFrames();
            UpdateTextureQuality();
            UpdateVSyncCount();
            UpdateAntiAliasing();

            graphicsChanged = false;
        }
    }

    private void SyncGraphicsSettings()
    {
        _currentQuality = QualitySettings.GetQualityLevel();

        _targetFrameRate = Application.targetFrameRate;
        _textureQuality = MasterTextureLimit - QualitySettings.masterTextureLimit;
        _queuedFrames = QualitySettings.maxQueuedFrames;

        switch (_currentQuality)
        {
            case 1: QualitySettings.antiAliasing = 0; break;
            case 2: QualitySettings.antiAliasing = 0; break;
            case 3: QualitySettings.antiAliasing = 2; break;
        }

        switch (QualitySettings.antiAliasing)
        {
            case 2: _antiAliasing = 1; break;
            case 4: _antiAliasing = 2; break;
            case 8: _antiAliasing = 3; break;
            default: _antiAliasing = 0; break;
        }

        _waterQuality = ApplicationDataManager.ApplicationOptions.VideoWaterMode;
        _vsync = QualitySettings.vSyncCount;
    }

    public static bool HorizontalScrollbar(Rect rect, string title, ref float value, float min, float max)
    {
        var v = value;
        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(0, 4, rect.width, rect.height), title, BlueStonez.label_interparkbold_11pt_left);
            value = GUI.HorizontalSlider(new Rect(150, 10, rect.width - 200, 30), value, min, max, BlueStonez.horizontalSlider, BlueStonez.horizontalSliderThumb);
            GUI.Label(new Rect(rect.width - 40, 4, 50, rect.height), value < 0 ? "Auto" : Mathf.RoundToInt(value).ToString(), BlueStonez.label_interparkbold_11pt_left);
        }
        GUI.EndGroup();
        return value != v;
    }

    public static bool HorizontalGridbar(Rect rect, string title, ref int value, string[] set)
    {
        var v = value;
        GUI.BeginGroup(rect);
        {
            GUI.Label(new Rect(0, 5, rect.width, rect.height), title, BlueStonez.label_interparkbold_11pt_left);
            value = GUI.SelectionGrid(new Rect(150, 5, rect.width - 200, 30), value, set, set.Length, BlueStonez.tab_medium);
        }
        GUI.EndGroup();
        return value != v;
    }

    private void DoVideoGroup()
    {
        GUI.skin = BlueStonez.Skin;

        Rect videoRect = new Rect(1, 1, _rect.width - 33, _rect.height - 55 - 47);
        Rect contentRect = new Rect(0, 0, _desiredWidth, _rect.height + 200 - 55 - 46 - 20);

        int spacing = 10;
        int boxWidth = 150;
        int resHeight = _screenResText.Length * 16 + 16;

        float width = videoRect.width - GroupMarginX - GroupMarginX - 20;

        if (!Application.isWebPlayer || showResolutions)
        {
            contentRect.height += _screenResText.Length * 16;
        }

        _scrollVideo = GUITools.BeginScrollView(videoRect, _scrollVideo, contentRect);
        {
            GUI.enabled = true;

            int qindex = GUI.SelectionGrid(new Rect(0, 5, videoRect.width - 10, 22), _currentQuality, qualitySet, qualitySet.Length, BlueStonez.tab_medium);
            if (qindex != _currentQuality)
            {
                SetCurrentQuality(qindex);
                SfxManager.Play2dAudioClip(SoundEffectType.UIButtonClick);
            }

            if (HorizontalScrollbar(new Rect(GroupMarginX, 30, width, 30), "Target Framerate:", ref _targetFrameRate, -1, 200))
            {
                _vsync = 0;
                graphicsChanged = true;
            }
            if (HorizontalScrollbar(new Rect(GroupMarginX, 60, width, 30), "Max Queued Frames:", ref _queuedFrames, 0, 10))
            {
                graphicsChanged = true;
            }

            GUI.Label(new Rect(GroupMarginX, 90, width, 30), new GUIContent(string.Empty, "This setting will take effect after reloading"));
            if (HorizontalScrollbar(new Rect(GroupMarginX, 90, width, 30), "Texture Quality:", ref _textureQuality, 0, MasterTextureLimit))
            {
                graphicsChanged = true;
                SetCurrentQuality(qualitySet.Length - 1);
            }
            if (HorizontalGridbar(new Rect(GroupMarginX, 120, width, 30), "VSync:", ref _vsync, vsyncSet))
            {
                _targetFrameRate = -1;
                graphicsChanged = true;
                SetCurrentQuality(qualitySet.Length - 1);
            }
            if (HorizontalGridbar(new Rect(GroupMarginX, 150, width, 30), "Anti Aliasing:", ref _antiAliasing, antiAliasingSet))
            {
                graphicsChanged = true;
                SetCurrentQuality(qualitySet.Length - 1);
            }
            if (HorizontalGridbar(new Rect(GroupMarginX, 180, width, 30), "Water Quality:", ref _waterQuality, waterSet))
            {
                ApplicationDataManager.ApplicationOptions.VideoWaterMode = _waterQuality;
                SetCurrentQuality(qualitySet.Length - 1);
            }

            //// EFFECTS
            //DrawGroupControl(new Rect(GroupMarginX, 230, width, 45), LocalizedStrings.Effects, BlueStonez.label_group_interparkbold_18pt);
            //GUI.BeginGroup(new Rect(GroupMarginX, 230, width, 45));
            //{
            //    GUI.changed = false;
            //    ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares = GUI.Toggle(new Rect(10, 15, 100, 20), ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares, "Bloom effect", BlueStonez.toggle);
            //    if (!Application.isWebPlayer)
            //    {
            //        ApplicationDataManager.ApplicationOptions.VideoVignetting = GUI.Toggle(new Rect(120, 15, 100, 20), ApplicationDataManager.ApplicationOptions.VideoVignetting, "Vignetting", BlueStonez.toggle);
            //    }
            //    ApplicationDataManager.ApplicationOptions.VideoMotionBlur = GUI.Toggle(new Rect(230, 15, 100, 20), ApplicationDataManager.ApplicationOptions.VideoMotionBlur, "Motion Blur", BlueStonez.toggle);
            //    if (GUI.changed)
            //    {
            //        CmuneEventHandler.Route(new ImageEffectsUpdate());
            //    }
            //}
            //GUI.EndGroup();
            int resolutionY = 240;
            //RESOLUTIONS (standalone)
            if (!Application.isWebPlayer || showResolutions)
            {
                DrawGroupControl(new Rect(GroupMarginX, resolutionY, width, resHeight), LocalizedStrings.ScreenResolution, BlueStonez.label_group_interparkbold_18pt);
                GUI.BeginGroup(new Rect(GroupMarginX, resolutionY, width, resHeight));
                {
                    GUI.changed = false;
                    Rect grid = new Rect(10, 10, spacing + boxWidth * 2, resHeight);
                    int index = GUI.SelectionGrid(grid, ScreenResolutionManager.CurrentResolutionIndex, _screenResText, 1, BlueStonez.radiobutton);
                    if (index != ScreenResolutionManager.CurrentResolutionIndex)
                    {
                        if (INSTANT_SCREEN_RES_CHANGE)
                        {
                            ScreenResolutionManager.SetResolution(index, Screen.fullScreen);
                        }
                        else
                        {
                            ShowScreenResChangeConfirmation(ScreenResolutionManager.CurrentResolutionIndex, index);
                        }
                    }
                }
                GUI.EndGroup();
            }
        }
        GUITools.EndScrollView();
    }

    private void DoAudioGroup()
    {
        float height = 130;
        float width = (_rect.height - 55 - 46) < height ? _rect.width - 65 : _rect.width - 50;
        _scrollControls = GUITools.BeginScrollView(new Rect(1, 1, _rect.width - 33, _rect.height - 55 - 46), _scrollControls, new Rect(0, 0, _rect.width - 50, height));
        {
            DrawGroupControl(new Rect(GroupMarginX, 20, width, 130), LocalizedStrings.Volume, BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(new Rect(GroupMarginX, 20, width, 130));
            {
                ApplicationDataManager.ApplicationOptions.AudioEnabled = !GUI.Toggle(new Rect(15, 105, 100, 30), !ApplicationDataManager.ApplicationOptions.AudioEnabled, LocalizedStrings.Mute, BlueStonez.toggle);
                if (GUI.changed)
                {
                    GUI.changed = false;
                    SfxManager.EnableAudio(ApplicationDataManager.ApplicationOptions.AudioEnabled);
                }

                GUITools.PushGUIState();
                GUI.enabled = ApplicationDataManager.ApplicationOptions.AudioEnabled;

                GUI.Label(new Rect(15, 10, 100, 30), LocalizedStrings.MasterVolume, BlueStonez.label_interparkbold_11pt_left);
                ApplicationDataManager.ApplicationOptions.AudioMasterVolume = GUI.HorizontalSlider(new Rect(115, 17, 200, 30), Mathf.Clamp01(ApplicationDataManager.ApplicationOptions.AudioMasterVolume), 0, 1, BlueStonez.horizontalSlider, BlueStonez.horizontalSliderThumb);
                if (GUI.changed)
                {
                    GUI.changed = false;
                    SfxManager.UpdateMasterVolume();
                    //PlayAudioVolumnChangeSound(ApplicationDataManager.Settings.AudioMasterVolume);
                }
                GUI.Label(new Rect(320, 10, 100, 30), (ApplicationDataManager.ApplicationOptions.AudioMasterVolume * 100).ToString("f0") + " %", BlueStonez.label_interparkbold_11pt_left);

                GUI.Label(new Rect(15, 40, 100, 30), LocalizedStrings.MusicVolume, BlueStonez.label_interparkbold_11pt_left);
                ApplicationDataManager.ApplicationOptions.AudioMusicVolume = GUI.HorizontalSlider(new Rect(115, 47, 200, 30), Mathf.Clamp01(ApplicationDataManager.ApplicationOptions.AudioMusicVolume), 0, 1, BlueStonez.horizontalSlider, BlueStonez.horizontalSliderThumb);
                if (GUI.changed)
                {
                    GUI.changed = false;
                    SfxManager.UpdateMusicVolume();
                    //PlayAudioVolumnChangeSound(ApplicationDataManager.Settings.AudioMusicVolume);
                }
                GUI.Label(new Rect(320, 40, 100, 30), (ApplicationDataManager.ApplicationOptions.AudioMusicVolume * 100).ToString("f0") + " %", BlueStonez.label_interparkbold_11pt_left);

                GUI.Label(new Rect(15, 70, 100, 30), LocalizedStrings.EffectsVolume, BlueStonez.label_interparkbold_11pt_left);
                ApplicationDataManager.ApplicationOptions.AudioEffectsVolume = GUI.HorizontalSlider(new Rect(115, 77, 200, 30), Mathf.Clamp01(ApplicationDataManager.ApplicationOptions.AudioEffectsVolume), 0, 1, BlueStonez.horizontalSlider, BlueStonez.horizontalSliderThumb);
                if (GUI.changed)
                {
                    GUI.changed = false;
                    SfxManager.UpdateEffectsVolume();
                    //PlayAudioVolumnChangeSound(ApplicationDataManager.Settings.AudioEffectsVolume);
                }
                GUI.Label(new Rect(320, 70, 100, 30), (ApplicationDataManager.ApplicationOptions.AudioEffectsVolume * 100).ToString("f0") + " %", BlueStonez.label_interparkbold_11pt_left);

                GUITools.PopGUIState();
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
    }

    private void DoControlsGroup()
    {
        GUITools.PushGUIState();
        GUI.enabled = _targetMap == null;

        GUI.skin = BlueStonez.Skin;
#if !UNITY_ANDROID && !UNITY_IPHONE
        _scrollControls = GUITools.BeginScrollView(new Rect(1, 3, _rect.width - 33, _rect.height - 55 - 50), _scrollControls, new Rect(0, 0, _rect.width - 50, 210 + _keyCount * 21)); //720 + 15));
        {
            DrawGroupControl(new Rect(GroupMarginX, 20, _rect.width - 65, 65), LocalizedStrings.Mouse, BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(new Rect(GroupMarginX, 20, _rect.width - 65, 65));
            {
                GUI.Label(new Rect(15, 10, 130, 30), LocalizedStrings.MouseSensitivity, BlueStonez.label_interparkbold_11pt_left);
                float s = GUI.HorizontalSlider(new Rect(155, 17, 200, 30), ApplicationDataManager.ApplicationOptions.InputXMouseSensitivity, 1, 10, BlueStonez.horizontalSlider, BlueStonez.horizontalSliderThumb);
                GUI.Label(new Rect(370, 10, 100, 30), ApplicationDataManager.ApplicationOptions.InputXMouseSensitivity.ToString("N1"), BlueStonez.label_interparkbold_11pt_left);
                if (s != ApplicationDataManager.ApplicationOptions.InputXMouseSensitivity)
                {
                    ApplicationDataManager.ApplicationOptions.InputXMouseSensitivity = s;
                }

                bool invert = GUI.Toggle(new Rect(15, 38, 200, 30), ApplicationDataManager.ApplicationOptions.InputInvertMouse, LocalizedStrings.InvertMouseButtons, BlueStonez.toggle);
                if (invert != ApplicationDataManager.ApplicationOptions.InputInvertMouse)
                {
                    ApplicationDataManager.ApplicationOptions.InputInvertMouse = invert;
                }

            }
            GUI.EndGroup();

            int yOffset = 105;
            if (Input.GetJoystickNames().Length > 0)
            {
                DrawGroupControl(new Rect(GroupMarginX, 105, _rect.width - 65, 50), LocalizedStrings.Gamepad, BlueStonez.label_group_interparkbold_18pt);
                GUI.BeginGroup(new Rect(GroupMarginX, 105, _rect.width - 65, 50));
                {
                    bool b = GUI.Toggle(new Rect(15, 15, 400, 30), InputManager.Instance.IsGamepadEnabled, Input.GetJoystickNames()[0], BlueStonez.toggle);
                    if (b != InputManager.Instance.IsGamepadEnabled)
                    {
                        InputManager.Instance.IsGamepadEnabled = b;
                    }
                }
                GUI.EndGroup();
                yOffset += 70;
            }
            else if (InputManager.Instance.IsGamepadEnabled)
            {
                //turn off IsGamepadEnabled dynamically if joystick is removed during session
                InputManager.Instance.IsGamepadEnabled = false;
            }

            DrawGroupControl(new Rect(GroupMarginX, yOffset, _rect.width - 65, _keyCount * 21 + 20), LocalizedStrings.Keyboard, BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(new Rect(8, yOffset, _rect.width - 65, _keyCount * 21 + 20));
            {
                DoInputControlMapping(new Rect(5, 5, _rect.width - 60, _keyCount * 21 + 20));
            }
            GUI.EndGroup();
        }
        GUITools.EndScrollView();
#else
        _scrollControls = GUITools.BeginScrollView(new Rect(1, 3, _rect.width - 33, _rect.height - 55 - 50), _scrollControls, new Rect(0, 0, _rect.width - 50, 100));
        {
            GUI.BeginGroup(new Rect(8, 20, _rect.width - 65, 95), string.Empty, BlueStonez.group_grey81);
            {
                bool multiTouch = GUI.Toggle(new Rect(15, 17, 200, 30), ApplicationDataManager.ApplicationOptions.UseMultiTouch, LocalizedStrings.UseMultiTouchInput, BlueStonez.toggle);
                if (multiTouch != ApplicationDataManager.ApplicationOptions.UseMultiTouch)
                {
                    if (multiTouch)
                    {
                        PanelManager.Instance.ClosePanel(PanelType.Options);
                        PopupSystem.ShowMessage("Multi-touch hints", "To use the multi-touch interface:\n* Touch anywhere to aim\n* While aiming, tap a second finger to shoot\n* In iPad Settings under General, disable 'Multitasking Gestures' as they can interrupt the game", PopupSystem.AlertType.OK, UseMultiTouch);
                    }
                    else
                    {
                        ApplicationDataManager.ApplicationOptions.UseMultiTouch = multiTouch;
                    }
                }

                GUI.Label(new Rect(15, 30, 130, 30), LocalizedStrings.LookSensitivity, BlueStonez.label_interparkbold_11pt_left);
                float s = GUI.HorizontalSlider(new Rect(155, 38, 200, 30), ApplicationDataManager.ApplicationOptions.TouchLookSensitivity, 0.5f, 3.0f, BlueStonez.horizontalSlider, BlueStonez.horizontalSliderThumb);
                GUI.Label(new Rect(370, 30, 100, 30), ApplicationDataManager.ApplicationOptions.TouchLookSensitivity.ToString("N1"), BlueStonez.label_interparkbold_11pt_left);
                if (s != ApplicationDataManager.ApplicationOptions.TouchLookSensitivity)
                {
                    ApplicationDataManager.ApplicationOptions.TouchLookSensitivity = s;
                }

                if (!multiTouch)
                {
                    GUI.Label(new Rect(15, 60, 130, 30), LocalizedStrings.JoystickSensitivity, BlueStonez.label_interparkbold_11pt_left);
                    s = GUI.HorizontalSlider(new Rect(155, 69, 200, 30), ApplicationDataManager.ApplicationOptions.TouchMoveSensitivity, 0.5f, 3.0f, BlueStonez.horizontalSlider, BlueStonez.horizontalSliderThumb);
                    GUI.Label(new Rect(370, 60, 100, 30), ApplicationDataManager.ApplicationOptions.TouchMoveSensitivity.ToString("N1"), BlueStonez.label_interparkbold_11pt_left);
                    if (s != ApplicationDataManager.ApplicationOptions.TouchMoveSensitivity)
                    {
                        ApplicationDataManager.ApplicationOptions.TouchMoveSensitivity = s;
                    }

                }
            }
            GUI.EndGroup();
            GUI.Label(new Rect(8 + 18, 20 - 8, GetWidth(LocalizedStrings.TouchInput), 16), LocalizedStrings.TouchInput, BlueStonez.label_group_interparkbold_18pt);
        }
        GUITools.EndScrollView();
#endif
        GUITools.PopGUIState();
    }

    private void UseMultiTouch()
    {
        ApplicationDataManager.ApplicationOptions.UseMultiTouch = true;
        PanelManager.Instance.OpenPanel(PanelType.Options);
    }

    private void DoSysInfoGroup()
    {
        GUI.skin = BlueStonez.Skin;
        float height = 1450 + ((GameServerManager.Instance.PhotonServerCount > 0) ? (GameServerManager.Instance.PhotonServerCount * 20) + 60 : 80);
        float width = Mathf.Max(_rect.width, BlueStonez.label_interparkbold_11pt_left.CalcSize(new GUIContent("Absolute URL : " + ApplicationDataManager.Instance.LocalSystemInfo.AbsoluteURL)).x) + 100;

        _scrollControls = GUITools.BeginScrollView(new Rect(1, 1, _rect.width - 33, _rect.height - 55 - 46), _scrollControls, new Rect(0, 0, width + 15, height));//new Rect(0, 0, _rect.width - 50, height));
        {
            //System
            Rect systemGroupRect = new Rect(8, 20, width, 340);
            DrawGroupControl(systemGroupRect, "System", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(systemGroupRect);
            {
                DrawContent(new Rect(16, 20, 400, 20), "Operating System", ApplicationDataManager.Instance.LocalSystemInfo.OperatingSystem);
                DrawContent(new Rect(16, 40, 400, 20), "Processor Type", ApplicationDataManager.Instance.LocalSystemInfo.ProcessorType);
                DrawContent(new Rect(16, 60, 400, 20), "Processor Count", ApplicationDataManager.Instance.LocalSystemInfo.ProcessorCount);
                DrawContent(new Rect(16, 80, 400, 20), "System Memory Size", ApplicationDataManager.Instance.LocalSystemInfo.SystemMemorySize);
                DrawContent(new Rect(16, 120, 400, 20), "Graphics Device Name", ApplicationDataManager.Instance.LocalSystemInfo.GraphicsDeviceName);
                DrawContent(new Rect(16, 140, 400, 20), "Graphics Device Vendor", ApplicationDataManager.Instance.LocalSystemInfo.GraphicsDeviceVendor);
                DrawContent(new Rect(16, 160, 400, 20), "Graphics Device Version", ApplicationDataManager.Instance.LocalSystemInfo.GraphicsDeviceVersion);
                DrawContent(new Rect(16, 180, 400, 20), "Graphics Memory Size", ApplicationDataManager.Instance.LocalSystemInfo.GraphicsMemorySize);
                DrawContent(new Rect(16, 200, 400, 20), "Graphics Shader Level", ApplicationDataManager.Instance.LocalSystemInfo.GraphicsShaderLevel);
                DrawContent(new Rect(16, 220, 400, 20), "Graphics Pixel Fill Rate", ApplicationDataManager.Instance.LocalSystemInfo.GraphicsPixelFillRate + " Megapixels/Sec");
                DrawContent(new Rect(16, 240, 400, 20), "Supports Image Effects", ApplicationDataManager.Instance.LocalSystemInfo.SupportsImageEffects);
                DrawContent(new Rect(16, 260, 400, 20), "Supports Render Textures", ApplicationDataManager.Instance.LocalSystemInfo.SupportsRenderTextures);
                DrawContent(new Rect(16, 280, 400, 20), "Supports Shadows", ApplicationDataManager.Instance.LocalSystemInfo.SupportsShadows);
                DrawContent(new Rect(16, 300, 400, 20), "Supports Vertex Programs", ApplicationDataManager.Instance.LocalSystemInfo.SupportsVertexPrograms);
            }
            GUI.EndGroup();

            //Application
            Rect applicationGroupRect = new Rect(8, 376, width, 220);
            DrawGroupControl(applicationGroupRect, "Application", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(applicationGroupRect);
            {
                //DrawGroupLabel(new Rect(16, 20, 400, 20), "Platform", string.Format("{0}, Build ({1}), Channel ({2})", ApplicationDataManager.Instance.LocalSystemInfo.Platform, ApplicationDataManager.BuildType.ToString(), ApplicationDataManager.Channel.ToString()));
                DrawContent(new Rect(16, 20, 400, 20), "Platform", string.Format("{0}, Build ({1}), Channel ({2})", ApplicationDataManager.Instance.LocalSystemInfo.Platform, ApplicationDataManager.BuildType, ApplicationDataManager.Channel));
                DrawContent(new Rect(16, 40, 400, 20), "Run In Background", ApplicationDataManager.Instance.LocalSystemInfo.RunInBackground);
                DrawContent(new Rect(16, 60, width, 20), "Absolute URL", ApplicationDataManager.Instance.LocalSystemInfo.AbsoluteURL);
                DrawContent(new Rect(16, 80, width, 20), "Data Path", ApplicationDataManager.Instance.LocalSystemInfo.DataPath);
                DrawContent(new Rect(16, 100, 400, 20), "Background Loading Priority", ApplicationDataManager.Instance.LocalSystemInfo.BackgroundLoadingPriority);
                DrawContent(new Rect(16, 120, width, 20), "Src Value", ApplicationDataManager.Instance.LocalSystemInfo.SrcValue);
                DrawContent(new Rect(16, 140, 400, 20), "System Language", ApplicationDataManager.Instance.LocalSystemInfo.SystemLanguage);
                DrawContent(new Rect(16, 160, 400, 20), "Target Frame Rate", ApplicationDataManager.Instance.LocalSystemInfo.TargetFrameRate);
                DrawContent(new Rect(16, 180, 400, 20), "Unity Version", ApplicationDataManager.Instance.LocalSystemInfo.UnityVersion);
            }
            GUI.EndGroup();

            //Physics
            Rect phsyicsGroupRect = new Rect(8, 616, width, 200);
            DrawGroupControl(phsyicsGroupRect, "Physics", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(phsyicsGroupRect);
            {
                DrawContent(new Rect(16, 20, 400, 20), "Gravity", ApplicationDataManager.Instance.LocalSystemInfo.Gravity);
                DrawContent(new Rect(16, 40, 400, 20), "Bounce Threshold", ApplicationDataManager.Instance.LocalSystemInfo.BounceThreshold);
                DrawContent(new Rect(16, 60, 400, 20), "Max Angular Velocity", ApplicationDataManager.Instance.LocalSystemInfo.MaxAngularVelocity);
                DrawContent(new Rect(16, 80, 400, 20), "Min Penetration For Penalty", ApplicationDataManager.Instance.LocalSystemInfo.MinPenetrationForPenalty);
                DrawContent(new Rect(16, 100, 400, 20), "Penetration Penalty Force", ApplicationDataManager.Instance.LocalSystemInfo.PenetrationPenaltyForce);
                DrawContent(new Rect(16, 120, 400, 20), "Sleep Angular Velocity", ApplicationDataManager.Instance.LocalSystemInfo.SleepAngularVelocity);
                DrawContent(new Rect(16, 140, 400, 20), "Sleep Velocity", ApplicationDataManager.Instance.LocalSystemInfo.SleepVelocity);
                DrawContent(new Rect(16, 160, 400, 20), "Solver Iteration Count", ApplicationDataManager.Instance.LocalSystemInfo.SolverIterationCount);
            }
            GUI.EndGroup();

            //Render Settings
            Rect rendersettingsGroupRect = new Rect(8, 836, width, 180);
            DrawGroupControl(rendersettingsGroupRect, "Render Settings", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(rendersettingsGroupRect);
            {
                DrawContent(new Rect(16, 20, 400, 20), "Current Resolution", ApplicationDataManager.Instance.LocalSystemInfo.CurrentResolution);
                DrawContent(new Rect(16, 40, 400, 20), "Ambient Light", ApplicationDataManager.Instance.LocalSystemInfo.AmbientLight);
                DrawContent(new Rect(16, 60, 400, 20), "Flare Strength", ApplicationDataManager.Instance.LocalSystemInfo.FlareStrength);
                DrawContent(new Rect(16, 80, 400, 20), "Fog Enabled", ApplicationDataManager.Instance.LocalSystemInfo.FogEnabled);
                DrawContent(new Rect(16, 100, 400, 20), "Fog Color", ApplicationDataManager.Instance.LocalSystemInfo.FogColor);
                DrawContent(new Rect(16, 120, 400, 20), "Fog Density", ApplicationDataManager.Instance.LocalSystemInfo.FogDensity);
                DrawContent(new Rect(16, 140, 400, 20), "Halo Strength", ApplicationDataManager.Instance.LocalSystemInfo.HaloStrength);
            }
            GUI.EndGroup();

            //Quality Settings
            Rect qualitysettingsGroupRect = new Rect(8, 1036, width, 200);
            DrawGroupControl(qualitysettingsGroupRect, "Quality Settings", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(qualitysettingsGroupRect);
            {
                DrawContent(new Rect(16, 20, 400, 20), "Current Quality Level", ApplicationDataManager.Instance.LocalSystemInfo.CurrentQualityLevel);
                DrawContent(new Rect(16, 40, 400, 20), "Anisotropic Filtering", ApplicationDataManager.Instance.LocalSystemInfo.AnisotropicFiltering);
                DrawContent(new Rect(16, 60, 400, 20), "Master Texture Limit", ApplicationDataManager.Instance.LocalSystemInfo.MasterTextureLimit);
                DrawContent(new Rect(16, 80, 400, 20), "Max Queued Frames", ApplicationDataManager.Instance.LocalSystemInfo.MaxQueuedFrames);
                DrawContent(new Rect(16, 100, 400, 20), "Pixel Light Count", ApplicationDataManager.Instance.LocalSystemInfo.PixelLightCount);
                DrawContent(new Rect(16, 120, 400, 20), "Shadow Cascades", ApplicationDataManager.Instance.LocalSystemInfo.ShadowCascades);
                DrawContent(new Rect(16, 140, 400, 20), "Shadow Distance", ApplicationDataManager.Instance.LocalSystemInfo.ShadowDistance);
                DrawContent(new Rect(16, 160, 400, 20), "Soft Vegetation Enabled", ApplicationDataManager.Instance.LocalSystemInfo.SoftVegetationEnabled);
            }
            GUI.EndGroup();

            //Browser Info
            Rect browserInfoGroupRect = new Rect(8, 1256, width, 180);
            DrawGroupControl(browserInfoGroupRect, "Browser Information", BlueStonez.label_group_interparkbold_18pt);
            GUI.BeginGroup(browserInfoGroupRect);
            {
                DrawContent(new Rect(16, 20, 400, 20), "Browser Identifier", ApplicationDataManager.Instance.LocalSystemInfo.BrowserIdentifier);
                DrawContent(new Rect(16, 40, 400, 20), "Browser Version", ApplicationDataManager.Instance.LocalSystemInfo.BrowserVersion);
                DrawContent(new Rect(16, 60, 400, 20), "Browser Major Version", ApplicationDataManager.Instance.LocalSystemInfo.BrowserMajorVersion);
                DrawContent(new Rect(16, 80, 400, 20), "Browser Minor Version", ApplicationDataManager.Instance.LocalSystemInfo.BrowserMinorVersion);
                DrawContent(new Rect(16, 100, 400, 20), "Browser Engine", ApplicationDataManager.Instance.LocalSystemInfo.BrowserEngine);
                DrawContent(new Rect(16, 120, 400, 20), "Browser Engine Version", ApplicationDataManager.Instance.LocalSystemInfo.BrowserEngineVersion);
                DrawContent(new Rect(16, 140, 400, 20), "Browser User Agent", ApplicationDataManager.Instance.LocalSystemInfo.BrowserUserAgent);
            }
            GUI.EndGroup();

            //Game Server Ping Info
            Rect pingGameServerGroupRect;
            if (GameServerManager.Instance.PhotonServerCount > 0)
            {
                pingGameServerGroupRect = new Rect(8, 1456, width, GameServerManager.Instance.PhotonServerCount * 20 + 40);
                DrawGroupControl(pingGameServerGroupRect, "Game Servers", BlueStonez.label_group_interparkbold_18pt);
                GUI.BeginGroup(pingGameServerGroupRect);
                int i = 0;
                foreach (var gs in GameServerManager.Instance.PhotonServerList)
                {
                    DrawContent(new Rect(16, ++i * 20, 400, 20), "Server IP", string.Format("{0} Ping: {1}", gs.ConnectionString, gs.Latency));
                }
                GUI.EndGroup();
            }
            else
            {
                pingGameServerGroupRect = new Rect(8, 1456, width, 60);
                DrawGroupControl(pingGameServerGroupRect, "Game Servers", BlueStonez.label_group_interparkbold_18pt);
                GUI.BeginGroup(pingGameServerGroupRect);
                DrawContent(new Rect(16, 20, 400, 20), "Error", "No game servers currently available.");
                GUI.EndGroup();
            }
        }
        GUITools.EndScrollView();
    }

    private void DoInputControlMapping(Rect rect)
    {
        int i = 0;

        GUI.Label(new Rect(20, 13, 150, 20), LocalizedStrings.Movement, BlueStonez.label_interparkbold_11pt_left);
        GUI.Label(new Rect(220, 13, 150, 20), LocalizedStrings.KeyButton, BlueStonez.label_interparkbold_11pt_left);

        foreach (UserInputMap m in InputManager.Instance.KeyMapping.Values)
        {
            bool isEditing = m == _targetMap;
            GUI.Label(new Rect(20, 35 + (i * 20), 140, 20), m.Description, BlueStonez.label_interparkmed_10pt_left);

            if (m.IsConfigurable)
            {
                if (GUI.Toggle(new Rect(180, 35 + (i * 20), 20, 20), isEditing, string.Empty, BlueStonez.radiobutton))
                {
                    _targetMap = m;
                    Screen.lockCursor = true;
                }
            }

            if (isEditing)
            {
                GUI.enabled = true;
                GUI.TextField(new Rect(220, 35 + (i * 20), 100, 20), string.Empty);
                GUI.enabled = false;
            }
            else
            {
                GUI.contentColor = (m.Channel != null) ? Color.white : Color.red;
                GUI.Label(new Rect(220, 35 + (i * 20), 150, 20), m.Assignment, BlueStonez.label_interparkmed_10pt_left);
                GUI.contentColor = Color.white;
            }
            i++;
        }

        if (_targetMap != null)
        {
            if (Event.current.type == EventType.layout && InputManager.Instance.ListenForNewKeyAssignment(_targetMap))
            {
                _targetMap = null;
                Screen.lockCursor = false;

                //avoid that the GUI event trigers any other kind of action
                Event.current.Use();
            }
        }
    }

    private void DrawGroupLabel(Rect position, string label, string text)
    {
        GUI.Label(position, label + ": " + text, BlueStonez.label_interparkbold_16pt_left);
    }

    private void DrawContent(Rect position, string label, string text)
    {
        GUI.Label(position, label + ": " + text, BlueStonez.label_interparkbold_11pt_left);
    }

    private void DrawGroupLabelWithWidth(Rect position, string label, string text)
    {
        string content = label + ": " + text;
        //BlueStonez.label_interparkbold_16pt_left.wordWrap = true;
        int width = Mathf.RoundToInt(BlueStonez.label_interparkbold_16pt.CalcSize(new GUIContent(content)).x);
        GUI.Label(new Rect(position.x, position.y, width, position.height), content, BlueStonez.label_interparkbold_16pt_left);
        _desiredWidth = (width > _desiredWidth) ? width : _desiredWidth;
        //BlueStonez.label_interparkbold_16pt_left.wordWrap = false;
    }

    private void DrawGroupControl(Rect rect, string title, GUIStyle style)
    {
        GUI.BeginGroup(rect, string.Empty, BlueStonez.group_grey81);
        GUI.EndGroup();
        GUI.Label(new Rect(rect.x + 18, rect.y - 8, GetWidth(title, style), 16), title, style);
    }

    private float GetWidth(string content)
    {
        return GetWidth(content, BlueStonez.label_group_interparkbold_18pt);
    }

    private float GetWidth(string content, GUIStyle style)
    {
        return style.CalcSize(new GUIContent(content)).x + 10;
    }

    private void ShowScreenResChangeConfirmation(int oldRes, int newRes)
    {
        _screenResChangeDelay = 15;

        _newScreenResIndex = newRes;
        _isFullscreenBefore = ScreenResolutionManager.IsFullScreen;

        ScreenResolutionManager.IsFullScreen = false;
    }

    private void SetCurrentQuality(int qualityLevel)
    {
        _currentQuality = qualityLevel;
        if (_currentQuality < QualitySettings.names.Length)
        {
            ApplicationDataManager.ApplicationOptions.IsUsingCustom = false;
            GraphicSettings.SetQualityLevel(_currentQuality);
            SyncGraphicsSettings();
        }
        else
        {
            ApplicationDataManager.ApplicationOptions.IsUsingCustom = true;
        }
    }

    private void UpdateApplicationFrameRate()
    {
        _targetFrameRate = Mathf.RoundToInt(_targetFrameRate);
        if (_targetFrameRate >= 0) _targetFrameRate = Mathf.Max(_targetFrameRate, 20);
        Application.targetFrameRate = (int)_targetFrameRate;
        ApplicationDataManager.ApplicationOptions.GeneralTargetFrameRate = Application.targetFrameRate;
    }

    private void UpdateMaxQueuedFrames()
    {
        _queuedFrames = Mathf.RoundToInt(_queuedFrames);
        QualitySettings.maxQueuedFrames = (int)_queuedFrames;
        ApplicationDataManager.ApplicationOptions.VideoMaxQueuedFrames = QualitySettings.maxQueuedFrames;
    }

    private void UpdateTextureQuality()
    {
        _textureQuality = Mathf.RoundToInt(_textureQuality);
        QualitySettings.masterTextureLimit = MasterTextureLimit - (int)_textureQuality;
        ApplicationDataManager.ApplicationOptions.VideoTextureQuality = QualitySettings.masterTextureLimit;
    }

    private void UpdateVSyncCount()
    {
        ApplicationDataManager.ApplicationOptions.VideoVSyncCount = _vsync;
        QualitySettings.vSyncCount = _vsync;
    }

    private void UpdateAntiAliasing()
    {
        switch (_antiAliasing)
        {
            case 1: QualitySettings.antiAliasing = 2; break;
            case 2: QualitySettings.antiAliasing = 4; break;
            case 3: QualitySettings.antiAliasing = 8; break;
            default: QualitySettings.antiAliasing = 0; break;
        }
        ApplicationDataManager.ApplicationOptions.VideoAntiAliasing = QualitySettings.antiAliasing;
    }

    public override void Show()
    {
        base.Show();

        if (ApplicationDataManager.ApplicationOptions.IsUsingCustom)
        {
            _currentQuality = qualitySet.Length - 1;
        }
        else
        {
            _currentQuality = ApplicationDataManager.ApplicationOptions.VideoQualityLevel;
        }
    }

    private class ScreenRes
    {
        public int Index;
        public string Resolution;

        public ScreenRes(int index, string res)
        {
            Index = index;
            Resolution = res;
        }
    }
}

