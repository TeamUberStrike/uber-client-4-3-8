using System.Collections;
using UberStrike.Realtime.Common;
using UnityEngine;

//TODO: this file has lots of duplicated and messy code, clean it.

//TODO: move level up HUD from EventFeedbackGUI to this file

public enum PopupType
{
    None = 0,

    DoubleKill = 2,
    TripleKill = 3,
    QuadKill = 4,
    MegaKill = 5,
    UberKill = 6,

    RoundStart,
    GameOver,

    BlueTeamWins,
    RedTeamWins,
    Draw,
};

public class PopupAnim : AbstractAnim
{
    public PopupAnim(Animatable2DGroup popupGroup, MeshGUIQuad glowBlur,
        MeshGUIText multiKillText, Vector2 spawnPosition,
        Vector2 destBlurScale, Vector2 destMultiKillScale,
        float displayTime, float fadeOutTime, SoundEffectType sound)
    {
        _popupGroup = popupGroup;
        _glowBlur = glowBlur;
        _popupText = multiKillText;
        _spawnPosition = spawnPosition;
        _destBlurScale = destBlurScale;
        _destTextScale = destMultiKillScale;
        _berpTime = 0.1f;
        _displayTime = displayTime;
        _fadeOutAnimTime = fadeOutTime;
        _sound = sound;
        Duration = _berpTime + _displayTime + _fadeOutAnimTime;
    }

    protected override void OnStart()
    {
        DoBerpAnim();
        if (_sound != SoundEffectType.None)
        {
            SfxManager.Play2dAudioClip(_sound);
        }
    }

    protected override void OnStop()
    {
        _popupGroup.RemoveAndFree(_glowBlur);
        _popupGroup.RemoveAndFree(_popupText);
        _isFading = false;
    }

    protected override void OnUpdate()
    {
        if (IsAnimating && Time.time > StartTime + _berpTime + _displayTime && !_isFading)
        {
            DoFadeOutAnim();
        }
    }

    private void DoBerpAnim()
    {
        _popupText.ScaleTo(_destTextScale, _berpTime, EaseType.Berp);
        _popupText.FadeAlphaTo(1.0f, _berpTime, EaseType.Berp);
        _glowBlur.FadeAlphaTo(1.0f, _berpTime, EaseType.Berp);
        _glowBlur.ScaleToAroundPivot(_destBlurScale, _spawnPosition, _berpTime, EaseType.Berp);
    }

    private void DoFadeOutAnim()
    {
        if (IsAnimating)
        {
            _isFading = true;
            _popupText.FadeAlphaTo(0.0f, _fadeOutAnimTime, EaseType.Out);
            _glowBlur.FadeAlphaTo(0.0f, _fadeOutAnimTime, EaseType.Out);
        }
    }

    private Animatable2DGroup _popupGroup;
    private MeshGUIQuad _glowBlur;
    protected MeshGUIText _popupText;
    private Vector2 _spawnPosition;
    private Vector2 _destBlurScale;
    private Vector2 _destTextScale;
    private SoundEffectType _sound;

    private float _berpTime;
    private float _displayTime;
    private float _fadeOutAnimTime;
    private bool _isFading;
}

public class RoundStartAnim : PopupAnim
{
    public RoundStartAnim(Animatable2DGroup popupGroup, MeshGUIQuad glowBlur,
        MeshGUIText multiKillText, Vector2 spawnPosition,
        Vector2 destBlurScale, Vector2 destMultiKillScale,
        float displayTime, float fadeOutTime, SoundEffectType sound)
        : base(popupGroup, glowBlur, multiKillText, spawnPosition,
        destBlurScale, destMultiKillScale, displayTime, fadeOutTime, sound)
    { }

    protected override void OnStart()
    {
        base.OnStart();
        _countdown5 = false;
        _countdown4 = false;
        _countdown3 = false;
        _countdown2 = false;
        _countdown1 = false;
        _countdown0 = false;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        if (IsAnimating)
        {
            int countdown = 6 - Mathf.CeilToInt(Time.time - StartTime);
            if (countdown == 5 && _countdown5 == false)
            {
                OnRoundStartCountdown("Round Starts In 5", SoundEffectType.GameMatchEndingCountdown5);
                _countdown5 = true;
            }
            else if (countdown == 4 && _countdown4 == false)
            {
                OnRoundStartCountdown("Round Starts In 4", SoundEffectType.GameMatchEndingCountdown4);
                _countdown4 = true;
            }
            else if (countdown == 3 && _countdown3 == false)
            {
                OnRoundStartCountdown("Round Starts In 3", SoundEffectType.GameMatchEndingCountdown3);
                _countdown3 = true;
            }
            else if (countdown == 2 && _countdown2 == false)
            {
                OnRoundStartCountdown("Round Starts In 2", SoundEffectType.GameMatchEndingCountdown2);
                _countdown2 = true;
            }
            else if (countdown == 1 && _countdown1 == false)
            {
                OnRoundStartCountdown("Round Starts In 1", SoundEffectType.GameMatchEndingCountdown1);
                _countdown1 = true;
            }
            else if (countdown == 0 && _countdown0 == false)
            {
                OnRoundStartCountdown("Fight", SoundEffectType.GameFight);
                _countdown0 = true;
            }
        }
    }

    private void OnRoundStartCountdown(string textStr, SoundEffectType sound)
    {
        _popupText.Text = textStr;
        SfxManager.Play2dAudioClip(sound);
    }

    //private int _remainingSeconds;
    private bool _countdown5;
    private bool _countdown4;
    private bool _countdown3;
    private bool _countdown2;
    private bool _countdown1;
    private bool _countdown0;
}

public class PopupHud : Singleton<PopupHud>
{
    public bool Enabled
    {
        get { return _popupGroup.IsVisible; }
        set
        {
            if (value) _popupGroup.Show();
            else _popupGroup.Hide();
        }
    }

    public void Draw()
    {
        _popupGroup.Draw();
    }

    public void PopupMultiKill(int killCount)
    {
        PopupType type = (PopupType)killCount;
        if (type >= PopupType.DoubleKill && type <= PopupType.UberKill)
        {
            DoPopup(type);
        }
    }

    public void PopupRoundStart()
    {
        PopupType type = PopupType.RoundStart;
        DoPopup(type);
    }

    public void PopupWinTeam(TeamID teamId)
    {
        PopupType type = PopupType.None;
        switch (teamId)
        {
            case TeamID.BLUE:
                type = PopupType.BlueTeamWins;
                SfxManager.Play2dAudioClip(SoundEffectType.GameBlueWins);
                break;
            case TeamID.RED:
                type = PopupType.RedTeamWins;
                SfxManager.Play2dAudioClip(SoundEffectType.GameRedWins);
                break;
            default:
                type = PopupType.Draw;
                SfxManager.Play2dAudioClip(SoundEffectType.GameDraw);
                break;
        }

        DoPopup(type);
    }

    public void PopupMatchOver()
    {
        PopupType type = PopupType.GameOver;
        DoPopup(type);
    }

    #region Private
    private PopupHud()
    {
        _spawnPosition = new Vector2(Screen.width / 2, 200.0f);
        _doubleKillColor = new Color((float)0xc7 / 0xff, (float)0x9b / 0xff, 0.0f);
        _tripleKillColor = new Color((float)0xc0 / 0xff, (float)0x96 / 0xff, 0.0f);
        _quadKillColor = new Color((float)0xc0 / 0xff, (float)0x76 / 0xff, 0.0f);
        _megaKillColor = new Color((float)0xc0 / 0xff, (float)0x56 / 0xff, 0.0f);
        _uberKillColor = new Color((float)0xc0 / 0xff, (float)0x36 / 0xff, 0.0f);
        _defaultColor = Color.white;

        _scaleEnlargeFactor = 1.1f;
        _doubleKillScale = new Vector2(0.8f, 0.8f);
        _tripleKillScale = _doubleKillScale * _scaleEnlargeFactor;
        _quadKillScale = _tripleKillScale * _scaleEnlargeFactor;
        _megaKillScale = _quadKillScale * _scaleEnlargeFactor;
        _uberKillScale = _megaKillScale * _scaleEnlargeFactor;
        _defaultScale = _doubleKillScale;

        _popupGroup = new Animatable2DGroup();
    }

    private void DoPopup(PopupType type)
    {
        CreateNewPopupGroup();
        ResetStyleAndTransform(type);
        IAnim anim;
        if (type == PopupType.RoundStart)
        {
            anim = new RoundStartAnim(_popupGroup, _glowBlur, _popupText, _spawnPosition,
                _destBlurScale, _destTextScale, _displayTime, _fadeOutTime, _sound);
        }
        else
        {
            anim = new PopupAnim(_popupGroup, _glowBlur, _popupText, _spawnPosition,
                _destBlurScale, _destTextScale, _displayTime, _fadeOutTime, _sound);
        }
        if (type >= PopupType.RoundStart)
        {
            InGameFeatHud.Instance.AnimationScheduler.ClearAll();
        }
        InGameFeatHud.Instance.AnimationScheduler.EnqueueAnim(anim);
    }

    private void ResetStyleAndTransform(PopupType type)
    {
        HudStyleUtility.Instance.SetDefaultStyle(_popupText);
        UpdateScales();
        _spawnPosition = InGameFeatHud.Instance.AnchorPoint;
        _displayTime = 1.0f;
        _fadeOutTime = 1.0f;
        _popupText.BitmapMeshText.ShadowColor = Color.white;
        _destTextScale = _defaultScale;
        _glowBlur.Color = _defaultColor;
        _sound = SoundEffectType.None;
        switch (type)
        {
            case PopupType.DoubleKill:
                _popupText.Text = "DOUBLE KILL";
                _popupText.BitmapMeshText.ShadowColor = _doubleKillColor;
                _destTextScale = _doubleKillScale;
                _glowBlur.Color = _doubleKillColor;
                _sound = SoundEffectType.UIDoubleKill;
                break;
            case PopupType.TripleKill:
                _popupText.Text = "TRIPLE KILL";
                _popupText.BitmapMeshText.ShadowColor = _tripleKillColor;
                _destTextScale = _tripleKillScale;
                _glowBlur.Color = _tripleKillColor;
                _sound = SoundEffectType.UITripleKill;
                break;
            case PopupType.QuadKill:
                _popupText.Text = "QUAD KILL";
                _popupText.BitmapMeshText.ShadowColor = _quadKillColor;
                _destTextScale = _quadKillScale;
                _glowBlur.Color = _quadKillColor;
                _sound = SoundEffectType.UIQuadKill;
                break;
            case PopupType.MegaKill:
                _popupText.Text = "MEGA KILL";
                _popupText.BitmapMeshText.ShadowColor = _megaKillColor;
                _destTextScale = _megaKillScale;
                _glowBlur.Color = _megaKillColor;
                _sound = SoundEffectType.UIMegaKill;
                break;
            case PopupType.UberKill:
                _popupText.Text = "UBER KILL";
                _popupText.BitmapMeshText.ShadowColor = _uberKillColor;
                _destTextScale = _uberKillScale;
                _glowBlur.Color = _uberKillColor;
                _sound = SoundEffectType.UIUberKill;
                _fadeOutTime = 5.0f;
                break;
            case PopupType.RoundStart:
                _popupText.Text = "Round Starts In";
                _displayTime = 5.0f;
                break;
            case PopupType.GameOver:
                _popupText.Text = "Game Over";
                _displayTime = 2.0f;
                _spawnPosition += new Vector2(0.0f, Screen.height * 0.3f);
                break;
            case PopupType.RedTeamWins:
                _popupText.Text = "Red Team Wins";
                _popupText.BitmapMeshText.ShadowColor = HudStyleUtility.DEFAULT_RED_COLOR;
                _glowBlur.Color = HudStyleUtility.DEFAULT_RED_COLOR;
                _displayTime = 2.0f;
                _spawnPosition += new Vector2(0.0f, Screen.height * 0.3f);
                break;
            case PopupType.BlueTeamWins:
                _popupText.Text = "Blue Team Wins";
                _popupText.BitmapMeshText.ShadowColor = HudStyleUtility.DEFAULT_BLUE_COLOR;
                _glowBlur.Color = HudStyleUtility.DEFAULT_BLUE_COLOR;
                _displayTime = 2.0f;
                _spawnPosition += new Vector2(0.0f, Screen.height * 0.3f);
                break;
            case PopupType.Draw:
                _popupText.Text = "Draw!";
                _displayTime = 2.0f;
                _spawnPosition += new Vector2(0.0f, Screen.height * 0.3f);
                break;
        }

        _popupText.Scale = _destTextScale; //setting scale is only for calcuation
        float blurWidth = _popupText.Size.x * HudStyleUtility.BLUR_WIDTH_SCALE_FACTOR;
        float blurHeight = _popupText.Size.y * HudStyleUtility.BLUR_HEIGHT_SCALE_FACTOR;
        _destBlurScale = new Vector2(blurWidth / HudTextures.WhiteBlur128.width,
            blurHeight / HudTextures.WhiteBlur128.height);
        _glowBlur.Scale = Vector2.zero;
        _glowBlur.Position = _spawnPosition;

        _popupText.Position = _spawnPosition;
        _popupText.Alpha = 0.0f;
        _popupText.Scale = Vector2.zero;
    }

    private IEnumerator EmitPopup()
    {
        int emissionId = PreemptiveCoroutineManager.Instance.IncrementId(EmitPopup);

        float berpTime = 0.1f;
        float displayTime = 1.0f;
        _popupText.ScaleTo(_destTextScale, berpTime, EaseType.Berp);
        _popupText.FadeAlphaTo(1.0f, berpTime, EaseType.Berp);
        _glowBlur.FadeAlphaTo(1.0f, berpTime, EaseType.Berp);
        _glowBlur.ScaleToAroundPivot(_destBlurScale, _spawnPosition, berpTime, EaseType.Berp);
        yield return new WaitForSeconds(displayTime);

        if (PreemptiveCoroutineManager.Instance.IsCurrent(EmitPopup, emissionId))
        {
            _popupText.FadeAlphaTo(0.0f, _fadeOutTime, EaseType.Out);
            _glowBlur.FadeAlphaTo(0.0f, _fadeOutTime, EaseType.Out);
            yield return new WaitForSeconds(_fadeOutTime);
        }
    }

    private void CreateNewPopupGroup()
    {
        _popupText = new MeshGUIText("",
            HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        _popupText.NamePrefix = "Popup";
        _glowBlur = new MeshGUIQuad(HudTextures.WhiteBlur128);
        _glowBlur.Name = "PopupHudGlow";
        _popupGroup.Group.Add(_popupText);
        _popupGroup.Group.Add(_glowBlur);
    }

    private void UpdateScales()
    {
        float scaleFactor = InGameFeatHud.Instance.TextHeight / _popupText.TextBounds.y;
        _doubleKillScale = new Vector2(scaleFactor, scaleFactor);
        _tripleKillScale = _doubleKillScale * _scaleEnlargeFactor;
        _quadKillScale = _tripleKillScale * _scaleEnlargeFactor;
        _megaKillScale = _quadKillScale * _scaleEnlargeFactor;
        _uberKillScale = _megaKillScale * _scaleEnlargeFactor;
    }

    private Color _doubleKillColor;
    private Color _tripleKillColor;
    private Color _quadKillColor;
    private Color _megaKillColor;
    private Color _uberKillColor;
    private Color _defaultColor;

    private MeshGUIQuad _glowBlur;
    private MeshGUIText _popupText;
    private Animatable2DGroup _popupGroup;

    private Vector2 _spawnPosition;
    private float _scaleEnlargeFactor;
    private Vector2 _doubleKillScale;
    private Vector2 _tripleKillScale;
    private Vector2 _quadKillScale;
    private Vector2 _megaKillScale;
    private Vector2 _uberKillScale;
    private Vector2 _defaultScale;
    private Vector2 _destBlurScale;
    private Vector2 _destTextScale;
    private float _displayTime;
    private float _fadeOutTime;
    private SoundEffectType _sound;
    #endregion
}
