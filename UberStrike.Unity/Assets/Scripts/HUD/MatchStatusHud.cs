using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class MatchStatusHud : Singleton<MatchStatusHud>
{
    public bool Enabled
    {
        get { return _entireGroup.IsVisible; }
        set
        {
            if (value) _entireGroup.Show();
            else _entireGroup.Hide();
        }
    }

    public bool IsScoreEnabled
    {
        get { return _scoreGroup.IsVisible; }
        set
        {
            if (value) _scoreGroup.Show();
            else _scoreGroup.Hide();
        }
    }

    public bool IsClockEnabled
    {
        get { return _clockText.IsVisible; }
        set
        {
            if (value) _clockText.Show();
            else _clockText.Hide();
        }
    }

    public bool IsRemainingKillEnabled
    {
        get { return _remainingKillText.IsVisible; }
        set
        {
            if (value) _remainingKillText.Show();
            else _remainingKillText.Hide();
        }
    }

    public int RemainingSeconds
    {
        get { return _remainingSeconds; }
        set
        {
            if (_remainingSeconds != value)
            {
                _remainingSeconds = value > 0 ? value : 0;
                _clockText.Text = GetClockString(_remainingSeconds);
                OnUpdateRemainingSeconds();
            }
        }
    }

    public int RemainingKills
    {
        get { return _remainingKills; }
        set
        {
            if (_remainingKills != value)
            {
                _remainingKills = value > 0 ? value : 0;
                _remainingKillText.Text = GetRemainingKillString(_remainingKills);
            }
        }
    }

    public string RemainingRoundsText
    {
        set
        {
            _remainingKillText.Text = value;
        }
    }

    public int BlueTeamScore
    {
        get { return _curLeftTeamScore; }
        set
        {
            _curLeftTeamScore = value;
            _teamScoreLeft.Text = _curLeftTeamScore.ToString();
            ResetTeamScoreGroupTransform();
        }
    }

    public int RedTeamScore
    {
        get { return _curRightTeamScore; }
        set
        {
            _curRightTeamScore = value;
            _teamScoreRight.Text = _curRightTeamScore.ToString();
            ResetTeamScoreGroupTransform();
        }
    }

    public void Draw()
    {
        _entireGroup.Draw();
    }

    #region Private
    private MatchStatusHud()
    {
        _remainingSeconds = 0;
        _teamScoreLeft = new MeshGUIText("0", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        _teamScoreLeft.NamePrefix = "TeamScore";
        _boxOverlayBlue = new Sprite2DGUI(new GUIContent(), StormFront.BlueBox);
        _leftScoreGroup = new Animatable2DGroup();
        _leftScoreGroup.Group.Add(_teamScoreLeft);
        _leftScoreGroup.Group.Add(_boxOverlayBlue);
        _teamScoreRight = new MeshGUIText("0", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleCenter);
        _teamScoreRight.NamePrefix = "TeamScore";
        _boxOverlayRed = new Sprite2DGUI(new GUIContent(), StormFront.RedBox);
        _rightScoreGroup = new Animatable2DGroup();
        _rightScoreGroup.Group.Add(_teamScoreRight);
        _rightScoreGroup.Group.Add(_boxOverlayRed);
        _scoreGroup = new Animatable2DGroup();
        _scoreGroup.Group.Add(_leftScoreGroup);
        _scoreGroup.Group.Add(_rightScoreGroup);
        _clockText = new MeshGUIText(GetClockString(_remainingSeconds),
            HudAssets.Instance.InterparkBitmapFont, TextAnchor.UpperCenter);
        _clockText.NamePrefix = "Clock";
        _remainingKillText = new MeshGUIText(GetRemainingKillString(_remainingKills),
            HudAssets.Instance.InterparkBitmapFont, TextAnchor.UpperCenter);
        _entireGroup = new Animatable2DGroup();
        _entireGroup.Group.Add(_scoreGroup);
        _entireGroup.Group.Add(_clockText);
        _entireGroup.Group.Add(_remainingKillText);

        _killLeftAudioMap = new Dictionary<int, SoundEffectType>(5);

        ResetHud();
        Enabled = true;
        IsClockEnabled = false;
        IsScoreEnabled = false;
        IsRemainingKillEnabled = false;

        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
        CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
    }

    private void ResetHud()
    {
        ResetStyle();
        ResetTransform();
    }

    private void ResetStyle()
    {
        HudStyleUtility.Instance.SetDefaultStyle(_teamScoreLeft);
        HudStyleUtility.Instance.SetDefaultStyle(_teamScoreRight);
        _teamScoreRight.BitmapMeshText.ShadowColor = HudStyleUtility.DEFAULT_RED_COLOR;
        HudStyleUtility.Instance.SetDefaultStyle(_clockText);
        HudStyleUtility.Instance.SetNoShadowStyle(_remainingKillText);
        _remainingKillText.BitmapMeshText.ShadowColor = Color.black;
        _remainingKillText.BitmapMeshText.AlphaMin = 0.1f;
        _remainingKillText.BitmapMeshText.AlphaMax = 0.6f;
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        if (ev.TeamId == TeamID.RED)
        {
            HudStyleUtility.Instance.SetRedStyle(_clockText);
        }
        else
        {
            HudStyleUtility.Instance.SetBlueStyle(_clockText);
        }
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        _textScale = 0.45f;
        ResetClockText();
        ResetRemainingKillText();
        ResetTeamScoreGroupTransform();
        _entireGroup.Position = new Vector2(Screen.width / 2, Screen.height * 0.02f);
    }

    private void ResetClockText()
    {
        _clockText.Scale = new Vector2(_textScale, _textScale);
        _clockText.Position = Vector2.zero;
        //normalHalfClockWidth = _clockText.Rect.width / 2;
        normalHalfClockHeight = _clockText.Rect.height / 2;
    }

    private void ResetRemainingKillText()
    {
        _remainingKillText.Scale = new Vector2(_textScale * 0.4f, _textScale * 0.4f);
        _remainingKillText.Position = new Vector2(_clockText.Position.x, _clockText.Position.y + _clockText.Size.y);
    }

    private void ResetTeamScoreGroupTransform()
    {
        _teamScoreLeft.Scale = _teamScoreRight.Scale = new Vector2(_textScale * 1.2f, _textScale * 1.2f);
        int maxDigitNum = _teamScoreLeft.Text.Length > _teamScoreRight.Text.Length ?
            _teamScoreLeft.Text.Length : _teamScoreRight.Text.Length;
        float height = _teamScoreLeft.Size.y;
        float width = height * 1.2f;
        width = maxDigitNum <= 2 ? width : height * maxDigitNum * 0.5f;

        float scaleAdjustFactor = 1.3f;
        _boxOverlayBlue.Scale = new Vector2(width / _boxOverlayBlue.GUIBounds.x,
            height / _boxOverlayBlue.GUIBounds.y) * scaleAdjustFactor;
        _boxOverlayRed.Scale = new Vector2(width / _boxOverlayRed.GUIBounds.x,
            height / _boxOverlayRed.GUIBounds.y) * scaleAdjustFactor;

        _boxOverlayRed.Position = -_boxOverlayRed.Size / 2;
        _boxOverlayBlue.Position = -_boxOverlayBlue.Size / 2;

        _leftScoreGroup.Position = new Vector2(-width / 2 - Screen.height * 0.09f, normalHalfClockHeight * 1.5f);
        _rightScoreGroup.Position = new Vector2(width / 2 + Screen.height * 0.09f, normalHalfClockHeight * 1.5f);
    }

    private IEnumerator PulseClock()
    {
        int emissionId = PreemptiveCoroutineManager.Instance.IncrementId(PulseClock);

        ResetClockText();
        _clockText.StopScaling();
        float sizeIncreaseTime = 0.1f;
        float sizeDecreaseTime = 0.5f;
        float sizeIncreaseScale = 1.2f;
        Vector2 pivot = _clockText.Center;
        _clockText.ScaleAroundPivot(new Vector2(sizeIncreaseScale, sizeIncreaseScale), pivot, sizeIncreaseTime);
        yield return new WaitForSeconds(sizeIncreaseTime);

        if (PreemptiveCoroutineManager.Instance.IsCurrent(PulseClock, emissionId))
        {
            _clockText.ScaleAroundPivot(new Vector2(1.0f / sizeIncreaseScale, 1.0f / sizeIncreaseScale), pivot, sizeDecreaseTime);
        }
    }

    private void OnUpdateRemainingSeconds()
    {
        if (_remainingSeconds <= WARNING_TIME_LOW_VALUE)
        {
            if (_remainingSeconds != 0)
            {
                MonoRoutine.Start(PulseClock());
            }
            else
            {
                StopClockPulsing();
            }
        }

        switch (_remainingSeconds)
        {
            case 5: SfxManager.Play2dAudioClip(SoundEffectType.GameMatchEndingCountdown5); break;
            case 4: SfxManager.Play2dAudioClip(SoundEffectType.GameMatchEndingCountdown4); break;
            case 3: SfxManager.Play2dAudioClip(SoundEffectType.GameMatchEndingCountdown3); break;
            case 2: SfxManager.Play2dAudioClip(SoundEffectType.GameMatchEndingCountdown2); break;
            case 1: SfxManager.Play2dAudioClip(SoundEffectType.GameMatchEndingCountdown1); break;
        }
    }

    private void StopClockPulsing()
    {
        PreemptiveCoroutineManager.Instance.IncrementId(PulseClock);
        _clockText.StopScaling();
        ResetClockText();
    }

    public void PlayKillsLeftAudio(int killsLeft)
    {
        SoundEffectType sound;

        if (_killLeftAudioMap.TryGetValue(killsLeft, out sound))
        {
            //if (_remainingSeconds > 2)
            SfxManager.Play2dAudioClip(sound, 2);

            _killLeftAudioMap.Remove(killsLeft);
        }
    }

    public void ResetKillsLeftAudio()
    {
        SoundEffectType[] sounds = new SoundEffectType[]
        {
            SoundEffectType.UIKillLeft1,
            SoundEffectType.UIKillLeft2,
            SoundEffectType.UIKillLeft3,
            SoundEffectType.UIKillLeft4,
            SoundEffectType.UIKillLeft5
        };

        for (int i = 0; i < sounds.Length; i++)
            _killLeftAudioMap[i + 1] = sounds[i];
    }

    private string GetClockString(int remainingSeconds)
    {
        int minute = remainingSeconds / 60;
        int second = remainingSeconds % 60;
        string strMinute = minute >= 10 ? minute.ToString() : "0" + minute;
        string strSecond = second >= 10 ? second.ToString() : "0" + second;
        return strMinute + ":" + strSecond;
    }

    private string GetRemainingKillString(int remainingKills)
    {
        if (remainingKills > 1)
        {
            return remainingKills + " Kills Left";
        }
        else
        {
            return remainingKills + " Kill Left";
        }
    }

    private float _textScale;
    private MeshGUIText _teamScoreLeft;
    private Sprite2DGUI _boxOverlayRed;
    private Animatable2DGroup _leftScoreGroup;
    private MeshGUIText _teamScoreRight;
    private Sprite2DGUI _boxOverlayBlue;
    private Animatable2DGroup _rightScoreGroup;
    private Animatable2DGroup _scoreGroup;
    private MeshGUIText _remainingKillText;
    private MeshGUIText _clockText;
    private Animatable2DGroup _entireGroup;

    private int _curLeftTeamScore;
    private int _curRightTeamScore;
    private int _remainingSeconds;
    private int _remainingKills;
    //private int _remainingRounds;

    //private float normalHalfClockWidth;
    private float normalHalfClockHeight;

    private Dictionary<int, SoundEffectType> _killLeftAudioMap;

    private static int WARNING_TIME_LOW_VALUE = 30;
    #endregion
}