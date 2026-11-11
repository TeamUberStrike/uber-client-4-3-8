using System;
using System.Collections.Generic;
using Cmune.DataCenter.Common.Entities;
using Cmune.Util;
using UberStrike.DataCenter.Common.Entities;
using UnityEngine;

public class GlobalUIRibbon : MonoSingleton<GlobalUIRibbon>
{
    public const int HEIGHT = NEWSFEED_HEIGHT + PAGETABS_HEIGHT + STATUSBAR_HEIGHT;

#if !UNITY_ANDROID && !UNITY_IPHONE
    private const int NEWSFEED_HEIGHT = 28;
#else
    private const int NEWSFEED_HEIGHT = 0;
#endif
    private const int PAGETABS_HEIGHT = 37;
    private const int STATUSBAR_HEIGHT = 25;

    private Dictionary<PageType, GUIContent> _pageList;
    private bool _showModerationPanel = false;
    private GuiDropDown _dropDown;

    private void Awake()
    {
        _yOffset = -HEIGHT;

        _liveFeed = new LiveFeed();

        _pageList = new Dictionary<PageType, GUIContent>()
        {
              { PageType.Home , new GUIContent(LocalizedStrings.HomeCaps) },
              { PageType.Play , new GUIContent(LocalizedStrings.PlayCaps) },
              { PageType.Shop , new GUIContent(LocalizedStrings.ShopCaps) },
              { PageType.Clans , new GUIContent(LocalizedStrings.ClansCaps) },
              { PageType.Inbox , new GUIContent(LocalizedStrings.InboxCaps) },
              { PageType.Stats , new GUIContent(LocalizedStrings.ProfileCaps) },
              { PageType.Chat , new GUIContent(LocalizedStrings.ChatCaps) },
        };

        _dropDown = new GuiDropDown();
        _dropDown.Caption = new GUIContent(_panelQuad_Options);
        _dropDown.Add(new GUIContent(" Help", _panelQuad_help), () =>
        {
            PanelManager.Instance.OpenPanel(PanelType.Help);
        });
        _dropDown.Add(new GUIContent(" Options", _panelQuad_Options), () =>
        {
            PanelManager.Instance.OpenPanel(PanelType.Options);
        });
        _dropDown.Add(new GUIContent(" Audio", _panelQuad_Sound_On), new GUIContent(" Audio", _panelQuad_Sound_Off), () => ApplicationDataManager.ApplicationOptions.AudioEnabled, () =>
        {
            ApplicationDataManager.ApplicationOptions.AudioEnabled = !ApplicationDataManager.ApplicationOptions.AudioEnabled;
            SfxManager.EnableAudio(ApplicationDataManager.ApplicationOptions.AudioEnabled);
            ApplicationDataManager.ApplicationOptions.SaveApplicationOptions();
        });
#if !UNITY_ANDROID && !UNITY_IPHONE
        _dropDown.Add(new GUIContent(" Windowed", _panelQuad_Fullscreen_Off), new GUIContent(" Full Screen", _panelQuad_Fullscreen_On), () =>
            Screen.fullScreen, () =>
            {
                ScreenResolutionManager.IsFullScreen = !Screen.fullScreen;
            });
#endif
        _dropDown.Add(new GUIContent(" Report", _panelQuad_ReportPlayer), () =>
        {
            PanelManager.Instance.OpenPanel(PanelType.ReportPlayer);
        });

        CmuneEventHandler.AddListener<LoginEvent>((ev) => 
        { 
            _showModerationPanel = (ev.AccessLevel > 0);
            if (_showModerationPanel)
            {
                _dropDown.Add(new GUIContent(" Moderate", _panelQuad_Open_Moderator), () =>
                {
                    if (PlayerDataManager.AccessLevelSecure > 0)
                        PanelManager.Instance.OpenPanel(PanelType.Moderation);
                });
            }
        });

        enabled = false;
    }

    private void Update()
    {
        if (_ribbonEvents.Count > 0)
        {
            RibbonEvent e;
            foreach (EventType v in Enum.GetValues(typeof(EventType)))
            {
                if (_ribbonEvents.TryGetValue(v, out e))
                {
                    if (e.IsDone()) _ribbonEvents.Remove(v);
                    else e.Animate();
                }
            }
        }

        if (_yOffset < 0)
            _yOffset = Mathf.Lerp(_yOffset, 0.1f, Time.deltaTime * 8.0f);
        else
            _yOffset = 0;

        _liveFeed.Update();
        PlayerXpUtil.GetXpRangeForLevel(PlayerDataManager.PlayerLevel, out _minXPforThisLevel, out _maxXPforThisLevel);

        _alphaIcons = Mathf.Clamp01(0.5f + ((Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 2.0f))) * 0.5f));
    }

    private void OnGUI()
    {
        GUI.depth = (int)GuiDepth.GlobalRibbon;
        // Scale the ribbon for mobile. Content anchors against VirtualWidth (the safe menu area); the
        // backgrounds are extended to the real screen edges.
        Matrix4x4 scaleMatrix = MobileMenuScale.Begin();
        MobileMenuScale.DrawBackground(new Rect(0, _yOffset, MobileMenuScale.VirtualWidth, PAGETABS_HEIGHT + STATUSBAR_HEIGHT + 4), BlueStonez.tab_strip_large);

        if (ApplicationDataManager.Channel != ChannelType.MacAppStore 
            && ApplicationDataManager.Channel != ChannelType.OSXStandalone 
            && ApplicationDataManager.Channel != ChannelType.Android 
            && ApplicationDataManager.Channel != ChannelType.IPad 
            && ApplicationDataManager.Channel != ChannelType.IPhone)
        {
            DoLiveFeeds();
        }

        DoPages();

        if (PlayerDataManager.IsPlayerLoggedIn)
        {
            DoStatusBar(new Rect(0, _yOffset + NEWSFEED_HEIGHT + PAGETABS_HEIGHT, MobileMenuScale.VirtualWidth, STATUSBAR_HEIGHT));
        }

        if (_ribbonEvents.Count > 0)
        {
            RibbonEvent e;
            foreach (EventType v in Enum.GetValues(typeof(EventType)))
            {
                if (_ribbonEvents.TryGetValue(v, out e))
                {
                    e.Draw();
                }
            }
        }

        // Draw the build info if not in game
        string versionText = string.IsNullOrEmpty(ApplicationDataManager.BuildVersion) ? ApplicationDataManager.VersionLong : ApplicationDataManager.BuildVersion;

        if (_buildInfoStyle == null)
        {
            _buildInfoStyle = new GUIStyle(BlueStonez.label_interparkmed_11pt_right);
            _buildInfoStyle.wordWrap = false;
            _buildInfoStyle.clipping = TextClipping.Clip;
        }

        if (_copyButtonStyle == null)
        {
            _copyButtonStyle = new GUIStyle(BlueStonez.buttondark_small);
            _copyButtonStyle.fontSize = 11;
            _copyButtonStyle.wordWrap = false;
            _copyButtonStyle.clipping = TextClipping.Clip;
        }

        GUIContent copyContent = new GUIContent("Copy", "Copy the build version to the clipboard");
        GUIContent fpsContent = new GUIContent(ApplicationDataManager.FrameRate);
        GUIContent versionContent = new GUIContent(versionText);

        float copyButtonWidth = _copyButtonStyle.CalcSize(copyContent).x + 8;
        _buildInfoStyle.fontSize = 11;
        float fpsWidth = _buildInfoStyle.CalcSize(fpsContent).x;
        float maxVersionWidth = Mathf.Max(0, MobileMenuScale.VirtualWidth - copyButtonWidth - 10 - fpsWidth - 6);
        float versionWidth = _buildInfoStyle.CalcSize(versionContent).x;
        if (versionWidth > maxVersionWidth && versionWidth > 0)
        {
            _buildInfoStyle.fontSize = Mathf.Max(6, Mathf.FloorToInt(_buildInfoStyle.fontSize * maxVersionWidth / versionWidth));
            versionWidth = Mathf.Min(_buildInfoStyle.CalcSize(versionContent).x, maxVersionWidth);
        }
        else
        {
            versionWidth = Mathf.Min(versionWidth, maxVersionWidth);
        }

        GUI.color = Color.white.SetAlpha(0.3f);
        GUI.Label(new Rect(MobileMenuScale.VirtualWidth - copyButtonWidth - 5 - versionWidth, 5, versionWidth, 20), versionContent, _buildInfoStyle);
        _buildInfoStyle.fontSize = 11;
        GUI.Label(new Rect(MobileMenuScale.VirtualWidth - copyButtonWidth - 5 - versionWidth - fpsWidth - 6, 5, fpsWidth, 20), fpsContent, _buildInfoStyle);
        GUI.color = Color.white;

        if (GUITools.Button(new Rect(MobileMenuScale.VirtualWidth - copyButtonWidth, 5, copyButtonWidth, 20), copyContent, _copyButtonStyle))
        {
            GUIUtility.systemCopyBuffer = versionText;
        }

        DoSettingsDropDown();

        MobileMenuScale.End(scaleMatrix);

        GuiManager.DrawTooltip();
    }

    // Settings wheel + popup, drawn inside the menu scale block in the same virtual space as the
    // status bar, so the wheel, its icon and the popup all follow MobileMenuScale.Scale.
    private void DoSettingsDropDown()
    {
        _dropDown.SetLocalRect(new Rect(
            MobileMenuScale.VirtualWidth - STATUSBAR_HEIGHT,
            _yOffset + NEWSFEED_HEIGHT + PAGETABS_HEIGHT,
            STATUSBAR_HEIGHT, STATUSBAR_HEIGHT));

        // Mobile: popup rows at least DROPDOWN_MIN_ROW_MM tall physically (1 on PC), and a wider
        // invisible hit area for the wheel - to the left over the non-interactive credits label, and
        // to the right across the safe-area strip up to the screen edge.
        _dropDown.PopupScale = MobileMenuScale.TouchBoost(_dropDown.ButtonHeight, DROPDOWN_MIN_ROW_MM, DROPDOWN_MAX_BOOST);
        _dropDown.HitPaddingLeft = MobileMenuScale.Active ? WHEEL_HIT_PADDING_LEFT : 0f;
        _dropDown.HitPaddingRight = MobileMenuScale.BleedRight;

        _dropDown.Draw();
    }

    private void DoPages()
    {
        _pageGroupRect = new Rect(2, _yOffset + NEWSFEED_HEIGHT, MobileMenuScale.VirtualWidth, PAGETABS_HEIGHT);
        float tabWidth = Mathf.Clamp(MobileMenuScale.VirtualWidth - 200, _pageList.Count * 80, 600) / (float)_pageList.Count;

        GUI.BeginGroup(_pageGroupRect);
        {
            int i = 0;
            foreach (var p in _pageList)
            {
                _pageToggleRect = new Rect(tabWidth * i, 0, tabWidth, 37);

                const float clickDelay = 0;
                bool show = MenuPageManager.IsCurrentPage(p.Key);
                bool result = GUI.Toggle(_pageToggleRect, show, p.Value, BlueStonez.tab_large);

                if (result && !show && GUITools.SaveClickIn(clickDelay))
                {
                    //GoogleAnalytics.Instance.LogPageView(p.Key.ToString());
                    SfxManager.Play2dAudioClip(SoundEffectType.UIRibbonClick);
                    MenuPageManager.Instance.LoadPage(p.Key);
                    GUITools.ClickAndUse();
                }

                i++;
            }

            //BUY CREDITS 
            if (GUITools.Button(new Rect(MobileMenuScale.VirtualWidth - 136, 2, 130, 33), new GUIContent(LocalizedStrings.BuyCreditsCaps, LocalizedStrings.ClickHereBuyCreditsMsg), BlueStonez.buttongold_large, SoundEffectType.UIGetCredits))
            {
                //GoogleAnalytics.Instance.LogEvent("ui-globaluiribbon-click", "Get Credits", true);
                ApplicationDataManager.Instance.OpenBuyCredits();
            }

        }
        GUI.EndGroup();






        if (InboxManager.Instance.HasUnreadMessages || InboxManager.Instance.HasUnreadRequests)
        {
            GUI.color = new Color(1, 1, 1, _alphaIcons);
            GUI.DrawTexture(new Rect(2 + ((tabWidth * 5) - 31), (ButtonY + _yOffset) + 15, 28, 23), _newMessageIcon);
            GUI.color = Color.white;
        }

        if (ChatManager.Instance.MarkPrivateMessageTab || ChatManager.Instance.MarkClanMessageTab)
        {
            GUI.color = new Color(1, 1, 1, _alphaIcons);
            GUI.DrawTexture(new Rect(2 + ((tabWidth * 7) - 31), (ButtonY + _yOffset) + 15, 28, 23), _newMessageIcon);
            GUI.color = Color.white;
        }
    }

    private void DoStatusBar(Rect rect)
    {
        // Background reaches the real screen edges; content stays inside the safe menu area.
        MobileMenuScale.DrawBackground(rect, BlueStonez.window);
        GUI.BeginGroup(rect);
        {
            //NAME
            GUI.DrawTexture(new Rect(7, 5, 16, 16), UberstrikeIcons.GetIconForChannel(ApplicationDataManager.Channel));
            string name = PlayerDataManager.IsPlayerInClan ? string.Format(" [{0}] {1}", PlayerDataManager.ClanTag, PlayerDataManager.Name) : string.Format(" {0}", PlayerDataManager.Name);
            GUI.Label(new Rect(25, 2, 200, 20), name, BlueStonez.label_interparkbold_13pt_left);

            //XP BAR
            DrawXPMeter(new Rect(200, 0, 350, 20));

            //POINTS
            Rect rectPoints = new Rect(rect.width - 220, 2, 100, 20);
            GUIContent points = new GUIContent(PointsValue.ToString("N0"), UberstrikeTextures.IconPoints20x20);
            GUI.color = new Color(1, 1, 1, PointsAlpha);
            GUI.Label(rectPoints, points, BlueStonez.label_interparkbold_13pt);

            //CREDITS
            Rect rectCredits = new Rect(rect.width - 130, 2, 100, 20);
            GUIContent credits = new GUIContent(CreditsValue.ToString("N0"), UberstrikeTextures.IconCredits20x20);
            GUI.color = new Color(1, 1, 1, CreditsAlpha);
            GUI.Label(rectCredits, credits, BlueStonez.label_interparkbold_13pt);
            GUI.color = Color.white;
        }
        GUI.EndGroup();
    }

    private void DoLiveFeeds()
    {
        Rect rect = new Rect(0, _yOffset, MobileMenuScale.VirtualWidth - 100, NEWSFEED_HEIGHT);
        GUI.BeginGroup(rect);
        {
            GUI.DrawTexture(new Rect(4, 6, 24, 24), _livefeedTexture);
            _liveFeed.Draw(new Rect(30, 8, rect.width - 30, rect.height - 12));
        }
        GUI.EndGroup();
    }

    private void DrawXPMeter(Rect position)
    {
        GUI.BeginGroup(position);
        {
            if (PlayerDataManager.PlayerLevel < PlayerXpUtil.MaxPlayerLevel)
            {
                GUI.Label(new Rect(0, 4, 50, 16), PlayerXpUtil.GetLevelDescription(PlayerDataManager.PlayerLevel), BlueStonez.label_interparkbold_13pt_left);
                GUI.Label(new Rect(position.width - 150, 4, 150, 16), XpValue.ToString("N0") + " XP", BlueStonez.label_interparkbold_13pt_left);

                float range = _maxXPforThisLevel - _minXPforThisLevel;
                float progress = range > 0 ? Mathf.Clamp01((XpValue - _minXPforThisLevel) / range) : 0;
                GUI.Label(new Rect(42, 6, position.width - 200, 12), GUIContent.none, BlueStonez.progressbar_background);
                GUI.color = ColorScheme.ProgressBar;
                GUI.Label(new Rect(44, 8, (position.width - 200) * progress, 8), string.Empty, BlueStonez.progressbar_thumb);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = ColorScheme.UberStrikeYellow;
                GUI.Label(new Rect(0, 3, 150, 16), PlayerXpUtil.GetLevelDescription(PlayerDataManager.PlayerLevel) + " (Lvl " + PlayerXpUtil.MaxPlayerLevel + ")", BlueStonez.label_interparkbold_13pt_left);
                GUI.color = Color.white;
            }
        }
        GUI.EndGroup();
    }

    public void Show()
    {
        enabled = true && IsVisible;
        //if (!IsVisible)
        //    Debug.LogWarning("GlobalUIRibbon.Show() ignored because GlobalUIRibbon.IsVisible is false");
    }

    public void Hide()
    {
        // The dropdown is not drawn while hidden, so it could never close itself and would leave
        // MouseOrbit.Disable stuck on.
        if (_dropDown != null)
            _dropDown.Close();

        enabled = false;
        _yOffset = -HEIGHT;
    }

    public void UpdateLevelBounds(int level)
    {
        int lower, upper;
        PlayerXpUtil.GetXpRangeForLevel(level, out lower, out upper);
        _maxXPforThisLevel = upper;
        _minXPforThisLevel = lower;
    }

    public void AddXPEvent(int deltaXP)
    {
        if (deltaXP > 0)
        {
            _ribbonEvents[EventType.XpEvent] = new GainEvent(370, Color.white, deltaXP, PlayerDataManager.PlayerExperienceSecure);
        }
        else if (deltaXP < 0)
        {
            _ribbonEvents[EventType.XpEvent] = new LoseEvent(370, Color.white, deltaXP, PlayerDataManager.PlayerExperienceSecure);
        }
    }

    public void AddCreditsEvent(int deltaCredits)
    {
        if (deltaCredits > 0)
        {
            _ribbonEvents[EventType.CreditEvent] = new GainEvent((int)(MobileMenuScale.VirtualWidth - 130), Color.white, deltaCredits, PlayerDataManager.CreditsSecure);
            SfxManager.Play2dAudioClip(SoundEffectType.GameGetCredits);
        }
        else if (deltaCredits < 0)
        {
            _ribbonEvents[EventType.CreditEvent] = new LoseEvent((int)(MobileMenuScale.VirtualWidth - 130), Color.white, deltaCredits, PlayerDataManager.CreditsSecure);
        }
    }

    public void AddPointsEvent(int deltaPoints)
    {
        if (deltaPoints > 0)
        {
            _ribbonEvents[EventType.PointEvent] = new GainEvent((int)(MobileMenuScale.VirtualWidth - 220), Color.white, deltaPoints, PlayerDataManager.PointsSecure);
            SfxManager.Play2dAudioClip(SoundEffectType.GameGetPoints);
        }
        else if (deltaPoints < 0)
        {
            _ribbonEvents[EventType.PointEvent] = new LoseEvent((int)(MobileMenuScale.VirtualWidth - 220), Color.white, deltaPoints, PlayerDataManager.PointsSecure);
        }
    }

    public void SetLiveFeeds(List<LiveFeedView> feeds)
    {
        if (_liveFeed != null && feeds != null)
            _liveFeed.SetContent(feeds);
        //else
        //    CmuneDebug.LogError("SetLiveFeeds failed: _liveFeed = " + (_liveFeed != null) + " feeds = " + (feeds != null));
    }

    #region Fields

    [SerializeField]
    private Texture _panelQuad_Fullscreen_On;

    [SerializeField]
    private Texture _panelQuad_Fullscreen_Off;

    [SerializeField]
    private Texture _panelQuad_Open_Moderator;
    [SerializeField]
    private Texture _panelQuad_Sound_On;
    [SerializeField]
    private Texture _panelQuad_Sound_Off;
    [SerializeField]
    private Texture _panelQuad_ReportPlayer;
    [SerializeField]
    private Texture _panelQuad_Options;
    [SerializeField]
    private Texture _panelQuad_help;

    [SerializeField]
    private Texture _livefeedTexture;

    [SerializeField]
    private Texture2D _newMessageIcon;

    private int _maxXPforThisLevel;
    private int _minXPforThisLevel;

    private const int ButtonY = 0;

    // Mobile settings dropdown sizing (see DoSettingsDropDown).
    private const float DROPDOWN_MIN_ROW_MM = 2.5f;
    private const float DROPDOWN_MAX_BOOST = 1.25f;
    private const float WHEEL_HIT_PADDING_LEFT = 20f;
    private float _yOffset;

    private float _alphaIcons = 0.0f;

    private Rect _pageGroupRect;
    private Rect _pageToggleRect;

    private Dictionary<EventType, RibbonEvent> _ribbonEvents = new Dictionary<EventType, RibbonEvent>();

    private static GUIStyle _buildInfoStyle;
    private static GUIStyle _copyButtonStyle;

    private LiveFeed _liveFeed;

    #endregion

    #region Properties

    private float CreditsAlpha
    {
        get
        {
            RibbonEvent e;
            if (_ribbonEvents.TryGetValue(EventType.CreditEvent, out e))
                return e.Alpha;
            else
                return 1;
        }
    }

    private int CreditsValue
    {
        get
        {
            RibbonEvent e;
            if (_ribbonEvents.TryGetValue(EventType.CreditEvent, out e))
                return e.Value;
            else
                return PlayerDataManager.Credits;
        }
    }

    private float PointsAlpha
    {
        get
        {
            RibbonEvent e;
            if (_ribbonEvents.TryGetValue(EventType.PointEvent, out e))
                return e.Alpha;
            else
                return 1;
        }
    }

    private int PointsValue
    {
        get
        {
            RibbonEvent e;
            if (_ribbonEvents.TryGetValue(EventType.PointEvent, out e))
                return e.Value;
            else
                return PlayerDataManager.Points;
        }
    }

    private float XpAlpha
    {
        get
        {
            RibbonEvent e;
            if (_ribbonEvents.TryGetValue(EventType.XpEvent, out e))
                return e.Alpha;
            else
                return 1;
        }
    }

    private int XpValue
    {
        get
        {
            RibbonEvent e;
            if (_ribbonEvents.TryGetValue(EventType.XpEvent, out e))
                return e.Value;
            else
                return PlayerDataManager.PlayerExperience;
        }
    }

    public bool IsEnabled
    {
        get { return enabled; }
    }

    public static bool IsVisible { get; set; }

    public int GetHeight()
    {
        return (int)_yOffset + HEIGHT - 1;
    }

    #endregion

    /// <summary>
    /// An event to do some animation on status bar.
    /// There are three actions when an event is generated:
    /// 1) Change the size of the delta value;
    /// 2) Change the position of the delta value;
    /// 3) Count up/down the remaining value.
    /// </summary>
    private abstract class RibbonEvent
    {
        protected float _alpha;
        protected float _scale;
        protected float _timer;
        protected float _speed;
        protected float _duration = 8;
        protected float _height = 68;

        protected float _value;
        protected float _delta;

        protected Rect _rect;
        protected Color _color;
        protected GUIStyle _style;

        protected string _content;

        protected const float _timeStage1 = 1;
        protected const float _timeStage2 = 6;

        public abstract int Value { get; }
        public abstract float Alpha { get; }

        public RibbonEvent(int horizontalPosition, Color color, int deltaValue, int currentValue)
        {
            _value = currentValue - deltaValue;
            _delta = deltaValue;

            _timer = 0;
            _alpha = 1;
            _scale = 1;
            _color = color;
            _speed = Mathf.Sign(deltaValue) * 20;
            _style = BlueStonez.label_interparkbold_32pt;

            if (_speed > 0)
            {
                _content = string.Format("+{0}", deltaValue.ToString("N0"));
            }
            else
            {
                _content = string.Format("-{0}", Mathf.Abs(deltaValue).ToString("N0"));
            }

            Vector2 size = _style.CalcSize(new GUIContent(_content));
            _rect = new Rect(horizontalPosition, _height, size.x, size.y);
        }

        public void Draw()
        {
            // Restore (not reset to identity) afterwards: this runs inside the mobile menu scale block,
            // and resetting would drop the menu scale for everything drawn after it in the frame.
            Matrix4x4 previousMatrix = GUI.matrix;
            MobileMenuScale.ScaleAroundLocalPivot(new Vector2(_scale, _scale), new Vector2(_rect.x + _rect.width / 2, _rect.y + _rect.height / 2));

            GUI.contentColor = new Color(0, 0, 0, _alpha);
            GUI.Label(new Rect(_rect.x + 1, _rect.y + 1, _rect.width, _rect.height), _content, _style);
            GUI.contentColor = new Color(_color.r, _color.g, _color.b, _alpha);
            GUI.Label(_rect, _content, _style);
            GUI.contentColor = Color.white;

            GUI.matrix = previousMatrix;
        }

        public abstract void Animate();
        public abstract bool IsDone();
    }

    /// <summary>
    /// Add this event when player gains XP/Points/Credits
    /// </summary>
    private class GainEvent : RibbonEvent
    {
        private float _moveTime = 0.3f;
        private float _stayTime = 0.5f;
        private float _scaleTime = 0.3f;

        public GainEvent(int x, Color color, int deltaValue, int currentValue)
            : base(x, color, deltaValue, currentValue)
        {
            _alpha = 0;
            _scale = 1f;
            _rect.y = 0;
        }

        public override void Animate()
        {
            if (_timer < _moveTime)
            {
                _rect.y = Mathfx.Berp(0, _height, _timer / _moveTime);
                _alpha = Mathf.Lerp(_alpha, 1, 8 * Time.deltaTime);
            }
            else if (_timer > _moveTime + _scaleTime && _timer < _moveTime + _stayTime + _scaleTime)
            {
                _scale = Mathf.Lerp(_scale, 0.5f, 15 * Time.deltaTime);
                _alpha = Mathf.Lerp(_alpha, 0.2f, 10 * Time.deltaTime);
            }

            _timer += Time.deltaTime;
        }

        public override int Value { get { return Mathf.RoundToInt(_value + _delta * _timer / (_moveTime + _stayTime + _scaleTime)); } }
        public override float Alpha { get { return 1 - _alpha; } }

        public override bool IsDone()
        {
            return _timer >= (_moveTime + _scaleTime + _stayTime);
        }
    }

    /// <summary>
    /// Add this event when player loses Points/Credits
    /// </summary>
    private class LoseEvent : RibbonEvent
    {
        private float _scaleTime = 0.5f;
        private float _moveTime = 0.5f;

        public LoseEvent(int x, Color color, int deltaValue, int currentValue)
            : base(x, color, deltaValue, currentValue)
        {
            _scale = 0.3f;
        }

        public override void Animate()
        {
            if (_timer < _scaleTime)
            {
                _scale = Mathfx.Berp(0.3f, 1, _timer / _scaleTime * 2);
            }
            else if (_timer < _scaleTime + _moveTime)
            {
                _rect.y = Mathf.Lerp(_rect.y, 0, 10 * Time.deltaTime);
                _alpha = Mathf.Lerp(_alpha, 0, 10 * Time.deltaTime);

                //_callbackLerpText(_timer / (_moveTime + _scaleTime));
            }

            _timer += Time.deltaTime;
            //_callbackSetAlpha(1 - _alpha);
        }

        public override int Value { get { return Mathf.RoundToInt(_value + _delta * _timer / (_moveTime + _scaleTime)); } }
        public override float Alpha { get { return 1 - _alpha; } }

        public override bool IsDone()
        {
            return _timer >= (_moveTime + _scaleTime);
        }
    }

    private enum EventType
    {
        XpEvent,
        PointEvent,
        CreditEvent,
    }

    private class LiveFeed
    {
        private List<FeedItem> _content;

        private int _index = 0;

        private bool _isRotating;

        private float _rotateY;

        public LiveFeed()
        {
            _content = new List<FeedItem>(10);
        }

        public void SetContent(List<LiveFeedView> feeds)
        {
            _content.Clear();

            foreach (LiveFeedView view in feeds)
            {
                FeedItem item = new FeedItem();

                item.Timer = 0;
                item.View = view;
                item.Date = view.Date.ToShortDateString();
                item.Length = BlueStonez.label_interparkbold_11pt_left.CalcSize(new GUIContent(view.Description)).x;

                if (view.Priority == 0) _content.Insert(0, item); else _content.Add(item);
            }
        }

        public void Update()
        {
            if (_content.Count == 0 || _content[0].View.Priority == 0) return;

            if (_isRotating)
            {
                _rotateY = Mathf.Clamp(_rotateY + Time.deltaTime * 10, 0, 16);
                if (_rotateY == 16)
                {
                    _isRotating = false;
                    _index = (_index + 1) % _content.Count;
                }
            }
            else
            {
                FeedItem item = _content[_index];
                if (item.Timer > 5)
                {
                    item.Timer = 0;

                    _rotateY = 0;
                    _isRotating = true;
                }
                else
                {
                    item.Timer += Time.deltaTime;
                }
            }
        }

        public void Draw(Rect rect)
        {
            if (_content.Count == 0) return;

            FeedItem item = _content[_index];

            GUI.BeginGroup(rect);
            if (_isRotating)
            {
                FeedItem next = _content[(_index + 1) % _content.Count];
                item.Draw(new Rect(0, -_rotateY, rect.width, rect.height));
                next.Draw(new Rect(0, 16 - _rotateY, rect.width, rect.height));
            }
            else
            {
                item.Draw(new Rect(0, 0, rect.width, rect.height));
            }
            GUI.EndGroup();
        }

        private class FeedItem
        {
            public float Timer;
            public string Date;
            public float Length;
            public LiveFeedView View;

            public void Draw(Rect rect)
            {
                GUI.Label(new Rect(8, rect.y + 1, 160, 14), Date, BlueStonez.label_interparkmed_11pt_left);

                if (View.Priority == 0) GUI.color = Color.red;
                GUI.Label(new Rect(80, rect.y, Length, 14), View.Description, BlueStonez.label_interparkbold_11pt_left);
                GUI.color = Color.white;

                GUI.contentColor = (View.Priority == 0) ? Color.red : ColorScheme.UberStrikeYellow;
                if (!string.IsNullOrEmpty(View.Url) && GUITools.Button(new Rect(90 + Length, rect.y, 78, 16), new GUIContent(LocalizedStrings.MoreInfo, LocalizedStrings.OpenThisLinkInANewBrowserWindow), BlueStonez.buttondark_medium))
                {
                    //GoogleAnalytics.Instance.LogEvent("ui-globaluiribbon-click", "Live Feed (" + View.Description + ")");
                    ScreenResolutionManager.IsFullScreen = false;
                    ApplicationDataManager.OpenUrl(View.Description, View.Url);
                }
                GUI.contentColor = Color.white;
            }
        }
    }
}
