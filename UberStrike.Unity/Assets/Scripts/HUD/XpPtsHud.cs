using System.Collections;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class XpPtsHud : Singleton<XpPtsHud>
{
    public bool Enabled
    {
        get { return _entireGroup.IsVisible; }
        set
        {
            if (value)
            {
                _entireGroup.Show();
                if (IsXpPtsTextVisible) _textGroup.Show();
                else _textGroup.Hide();

                if (IsNextLevelVisible) _nextLevelText.Show();
                else _nextLevelText.Hide();
            }
            else
            {
                _entireGroup.Hide();

            }
        }
    }

    public bool IsXpPtsTextVisible { get; set; }
    public bool IsNextLevelVisible { get; set; }

    public void OnGameStart(int currentLevel)
    {
        IsXpPtsTextVisible = true;
        PopdownOffScreen();
        ResetXp(currentLevel);
    }

    public void ResetXp(int currentLevel, int xp = 0, int points = 0)
    {
        SetXp(xp);
        SetPts(points);
        TotalXpOnGameStart = PlayerDataManager.PlayerExperienceSecure;
        CurrentLevel = currentLevel;
    }

    public void GainXp(int gainXp)
    {
        if (gainXp != 0)
        {
            Animatable2DGroup xpGroup = GenerateXp(gainXp);
            string[] args = new string[] { gainXp.ToString() };
            _animScheduler.AddAnimation(xpGroup, EmitSprite, null, OnGetXP, args);

            SfxManager.Play2dAudioClip(SoundEffectType.GameGetXP);
        }
    }

    public void GainPoints(int gainPts)
    {
        if (gainPts != 0)
        {
            Animatable2DGroup xpGroup = GeneratePts(gainPts);
            string[] args = new string[] { gainPts.ToString() };
            _animScheduler.AddAnimation(xpGroup, EmitSprite, null, OnGetPts, args);

            SfxManager.Play2dAudioClip(SoundEffectType.GameGetPoints);
        }
    }

    public void Update()
    {
        if (_isOnScreen && _isTemporaryDisplay && Time.time > _xpBarHideTime)
        {
            PopdownOffScreen();
        }
        _animScheduler.Draw();
    }

    public void Draw()
    {
        _entireGroup.Draw();
    }

    public void DisplayPermanently()
    {
        _isTemporaryDisplay = false;
        _textGroup.Hide();
        _screenYOffset = 50.0f;
        _entireGroup.MoveTo(_groupPosition);
        _isOnScreen = true;
    }

    public void PopupTemporarily()
    {
        _isTemporaryDisplay = true;
        _screenYOffset = 0.0f;
        _entireGroup.MoveTo(_groupPosition, 0.1f, EaseType.InOut);
        _isOnScreen = true;
        float xpBarDisplayTime = 3.0f;
        _xpBarHideTime = Time.time + xpBarDisplayTime;
    }

    public void PopdownOffScreen(float animTime = 0.1f)
    {
        _entireGroup.MoveTo(new Vector2(_groupPosition.x, _groupPosition.y + _translationDistance),
            animTime, EaseType.InOut);
        _isOnScreen = false;
    }

    #region Private
    private XpPtsHud()
    {
        TextAnchor textAnchor = TextAnchor.UpperLeft;
        _xpDigits = new MeshGUIText(_curXp.ToString(), HudAssets.Instance.InterparkBitmapFont, textAnchor);
        _xpDigits.NamePrefix = "XP";
        _xpText = new MeshGUIText("XP", HudAssets.Instance.HelveticaBitmapFont, textAnchor);
        _ptsDigits = new MeshGUIText(_curPts.ToString(), HudAssets.Instance.InterparkBitmapFont, textAnchor);
        _ptsDigits.NamePrefix = "PTS";
        _ptsText = new MeshGUIText("PTS", HudAssets.Instance.HelveticaBitmapFont, textAnchor);
        _xpBarEmptySprite = new MeshGUIQuad(HudTextures.XPBarEmptyBlue);
        _xpBarFullSprite = new MeshGUIQuad(HudTextures.XPBarFull);
        _curLevelText = new MeshGUIText("Lvl 5", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleRight);
        _nextLevelText = new MeshGUIText("Lvl 6", HudAssets.Instance.InterparkBitmapFont, TextAnchor.MiddleLeft);
        _glowBlur = new MeshGUIQuad(HudTextures.WhiteBlur128);
        _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
        _glowBlur.Name = "XpPtsHudGlow";
        _glowBlur.Depth = 1.0f;

        _xpGroup = new Animatable2DGroup();
        _ptsGroup = new Animatable2DGroup();
        _textGroup = new Animatable2DGroup();
        _xpBarGroup = new Animatable2DGroup();
        _entireGroup = new Animatable2DGroup();

        _xpGroup.Group.Add(_xpDigits);
        _xpGroup.Group.Add(_xpText);
        _ptsGroup.Group.Add(_ptsDigits);
        _ptsGroup.Group.Add(_ptsText);
        _textGroup.Group.Add(_xpGroup);
        _textGroup.Group.Add(_ptsGroup);

        _xpBarGroup.Group.Add(_curLevelText);
        _xpBarGroup.Group.Add(_nextLevelText);
        _xpBarGroup.Group.Add(_xpBarEmptySprite);
        _xpBarGroup.Group.Add(_xpBarFullSprite);

        _entireGroup.Group.Add(_glowBlur);
        _entireGroup.Group.Add(_textGroup);
        _entireGroup.Group.Add(_xpBarGroup);

        _animScheduler = new AnimationScheduler();
        _translationDistance = 100.0f;
        _isOnScreen = false;

        ResetHud();
        Enabled = false;

        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
        CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResoulutionChange);
    }

    private void ResetHud()
    {
        OnTeamChange(new OnSetPlayerTeamEvent { TeamId = TeamID.NONE });
        ResetTransform();
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        switch (ev.TeamId)
        {
            case TeamID.BLUE:
            case TeamID.NONE:
                HudStyleUtility.Instance.SetBlueStyle(_xpDigits);
                HudStyleUtility.Instance.SetBlueStyle(_ptsDigits);
                HudStyleUtility.Instance.SetBlueStyle(_xpText);
                HudStyleUtility.Instance.SetBlueStyle(_ptsText);
                HudStyleUtility.Instance.SetBlueStyle(_curLevelText);
                HudStyleUtility.Instance.SetBlueStyle(_nextLevelText);
                _xpBarEmptySprite.Texture = HudTextures.XPBarEmptyBlue;
                _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
                break;
            case TeamID.RED:
                HudStyleUtility.Instance.SetRedStyle(_xpDigits);
                HudStyleUtility.Instance.SetRedStyle(_ptsDigits);
                HudStyleUtility.Instance.SetRedStyle(_xpText);
                HudStyleUtility.Instance.SetRedStyle(_ptsText);
                HudStyleUtility.Instance.SetRedStyle(_curLevelText);
                HudStyleUtility.Instance.SetRedStyle(_nextLevelText);
                _xpBarEmptySprite.Texture = HudTextures.XPBarEmptyRed;
                _glowBlur.Color = HudStyleUtility.GLOW_BLUR_RED_COLOR;
                break;
        }

        _xpText.BitmapMeshText.AlphaMin = 0.40f;
        _ptsText.BitmapMeshText.AlphaMin = 0.40f;
    }

    private void OnScreenResoulutionChange(ScreenResolutionEvent ev)
    {
        _curScreenWidth = Screen.width;
        _curScreenHeight = Screen.height;
        ResetTransform();
    }

    private Animatable2DGroup GenerateXp(int xp)
    {
        float scaleFactor = 0.3f;
        float gap = 10.0f;
        Animatable2DGroup xpGroup = new Animatable2DGroup();
        BitmapFont bitmapFont = HudAssets.Instance.InterparkBitmapFont;
        TextAnchor textAnchor = TextAnchor.UpperLeft;
        MeshGUIText plus = new MeshGUIText(xp > 0 ? "+" : "", bitmapFont, textAnchor);
        MeshGUIText xpText = new MeshGUIText(xp + "XP", bitmapFont, textAnchor);
        HudStyleUtility.Instance.SetTeamStyle(plus);
        HudStyleUtility.Instance.SetTeamStyle(xpText);
        xpGroup.Group.Add(plus);
        xpGroup.Group.Add(xpText);
        plus.Position = new Vector2(0.0f, 0.0f);
        plus.Scale = new Vector2(scaleFactor, scaleFactor);
        xpText.Position = new Vector2(plus.Size.x + gap, 0.0f);
        xpText.Scale = new Vector2(scaleFactor, scaleFactor);

        xpGroup.Position = new Vector2(_xpPtsSpawnPoint.x - xpGroup.GetRect().width / 2, _xpPtsSpawnPoint.y);
        xpGroup.UpdateMeshGUIPosition();
        return xpGroup;
    }

    private Animatable2DGroup GeneratePts(int pts)
    {
        float scaleFactor = 0.3f;
        float gap = 10.0f;
        Animatable2DGroup ptsGroup = new Animatable2DGroup();
        BitmapFont bitmapFont = HudAssets.Instance.InterparkBitmapFont;
        TextAnchor textAnchor = TextAnchor.UpperLeft;
        MeshGUIText plus = new MeshGUIText(pts > 0 ? "+" : "", bitmapFont, textAnchor);
        MeshGUIText ptsText = new MeshGUIText(pts + "PTS", bitmapFont, textAnchor);
        HudStyleUtility.Instance.SetTeamStyle(plus);
        HudStyleUtility.Instance.SetTeamStyle(ptsText);
        ptsGroup.Group.Add(plus);
        ptsGroup.Group.Add(ptsText);
        plus.Position = new Vector2(0.0f, 0.0f);
        plus.Scale = new Vector2(scaleFactor, scaleFactor);
        ptsText.Position = new Vector2(plus.Size.x + gap, 0.0f);
        ptsText.Scale = new Vector2(scaleFactor, scaleFactor);

        ptsGroup.Position = new Vector2(_xpPtsSpawnPoint.x - ptsGroup.GetRect().width / 2, _xpPtsSpawnPoint.y);
        ptsGroup.UpdateMeshGUIPosition();
        return ptsGroup;
    }

    private IEnumerator OnGetXP(IAnimatable2D animatable, string[] args)
    {
        int xpDelta = int.Parse(args[0]);
        SetXp(_curXp + xpDelta);

        Animatable2DGroup text = animatable as Animatable2DGroup;
        text.FreeObject();

        yield break;
    }

    private IEnumerator OnGetPts(IAnimatable2D animatable, string[] args)
    {
        int ptsDelta = int.Parse(args[0]);
        SetPts(_curPts + ptsDelta);

        Animatable2DGroup text = animatable as Animatable2DGroup;
        text.FreeObject();

        yield break;
    }

    private IEnumerator EmitSprite(IAnimatable2D animatable, string[] args)
    {
        PopupTemporarily();

        float flickerTime = 0.5f;
        float fallDownTime = 0.3f;
        animatable.Flicker(flickerTime);
        animatable.ScaleAroundPivot(new Vector2(2.0f, 2.0f), animatable.GetCenter(), flickerTime, EaseType.Berp);
        yield return new WaitForSeconds(flickerTime);

        animatable.Move(new Vector2(0.0f, _xpPtsDiePoint.y - _xpPtsSpawnPoint.y), fallDownTime, EaseType.In);
        animatable.FadeAlphaTo(0.0f, fallDownTime, EaseType.In);
        yield return new WaitForSeconds(fallDownTime);
    }

    private void ResetStyle()
    {
        HudStyleUtility.Instance.SetTeamStyle(_xpDigits);
        HudStyleUtility.Instance.SetTeamStyle(_ptsDigits);
        HudStyleUtility.Instance.SetTeamStyle(_xpText);
        HudStyleUtility.Instance.SetTeamStyle(_ptsText);

        _xpText.BitmapMeshText.AlphaMin = 0.40f;
        _ptsText.BitmapMeshText.AlphaMin = 0.40f;
    }

    private void ResetTransform()
    {
        ResetXpPtsTransform();
        ResetXpBarTransform();
        ResetLevelTxtTransform();
        ResetBlurTransform();
        ResetGroupTransform();
        _xpPtsSpawnPoint = new Vector2(_curScreenWidth / 2, _curScreenHeight / 2);
        _xpPtsDiePoint = new Vector2(_curScreenWidth / 2, _curScreenHeight * 0.90f);
    }

    private void ResetXpPtsTransform()
    {
        _curScaleFactor = 0.65f;

        _xpDigits.Text = _curXp.ToString();
        _xpDigits.Position = new Vector2(0.0f, 0.0f);
        _xpDigits.Scale = new Vector2(0.7f * _curScaleFactor, 0.7f * _curScaleFactor);

        float gap = 3.0f;

        float offsetX = 0.0f;
        offsetX += _xpDigits.Size.x + gap;
        _xpText.Scale = new Vector2(0.35f * _curScaleFactor, 0.35f * _curScaleFactor);
        _xpText.Position = new Vector2(offsetX, _xpDigits.Size.y - _xpText.Size.y * 1.2f);

        offsetX += _xpText.Size.x + gap;
        _ptsDigits.Text = _curPts.ToString();
        _ptsDigits.Scale = new Vector2(0.7f * _curScaleFactor, 0.7f * _curScaleFactor);
        _ptsDigits.Position = new Vector2(offsetX, 0.0f);

        offsetX += _ptsDigits.Size.x + gap;
        _ptsText.Scale = new Vector2(0.35f * _curScaleFactor, 0.35f * _curScaleFactor);
        _ptsText.Position = new Vector2(offsetX, _xpText.Position.y);
    }

    private void ResetXpBarTransform()
    {
        float xpBarWidth = Screen.width * HudStyleUtility.XP_BAR_WIDTH_PROPORTION_IN_SCREEN;
        _oriXpBarEmptyScale.x = _oriXpBarEmptyScale.y = xpBarWidth / HudTextures.XPBarEmptyBlue.width;
        _oriXpBarFillScale.y = _oriXpBarEmptyScale.y;
        _oriXpBarFillScale.x = _oriXpBarFillScale.y * _curXpPercentage;
        _oriXpBarPos.x = -(xpBarWidth - _textGroup.Rect.width) / 2;
        _oriXpBarPos.y = _xpDigits.Size.y;

        float border = 8.5f;
        _xpBarEmptySprite.Scale = _oriXpBarEmptyScale;
        _xpBarFullSprite.Scale = _oriXpBarFillScale;
        _xpBarEmptySprite.Position = _oriXpBarPos;
        _xpBarFullSprite.Position = _oriXpBarPos;
        _xpBarFullSprite.Scale = new Vector2(_xpBarFullSprite.Scale.y * (512.0f - border * 2) / 512.0f * _curXpPercentage,
            _xpBarFullSprite.Scale.y * 0.95f);
        Vector2 position = _xpBarEmptySprite.Position;
        position.x += border * _xpBarEmptySprite.Scale.x;
        position.y += border * _xpBarEmptySprite.Scale.x;
        _xpBarFullSprite.Position = position;
    }

    private void ResetLevelTxtTransform()
    {
        float levelTextSclFactor = 0.5f;
        _curLevelText.Text = "Lvl " + CurrentLevel;
        _curLevelText.Position = new Vector2(_xpBarEmptySprite.Position.x - 20.0f, _xpBarEmptySprite.Position.y);
        _curLevelText.Scale = new Vector2(levelTextSclFactor * _curScaleFactor, levelTextSclFactor * _curScaleFactor);

        if (CurrentLevel < PlayerXpUtil.MaxPlayerLevel)
        {
            IsNextLevelVisible = true;
            int nextLevel = CurrentLevel + 1;
            _nextLevelText.Show();
            _nextLevelText.Text = "Lvl " + nextLevel;
            _nextLevelText.Position = new Vector2(_xpBarEmptySprite.Position.x + _xpBarEmptySprite.Rect.width + 20.0f,
                _xpBarEmptySprite.Position.y);
            _nextLevelText.Scale = new Vector2(levelTextSclFactor * _curScaleFactor, levelTextSclFactor * _curScaleFactor);
        }
        else
        {
            IsNextLevelVisible = false;
        }
    }

    private void ResetBlurTransform()
    {
        float blurWidth = _xpBarEmptySprite.Rect.width * HudStyleUtility.BLUR_WIDTH_SCALE_FACTOR;
        float blurHeight = _xpBarGroup.Rect.height * HudStyleUtility.BLUR_HEIGHT_SCALE_FACTOR;
        _oriBlurScale.x = blurWidth / HudTextures.WhiteBlur128.width;
        _oriBlurScale.y = blurHeight / HudTextures.WhiteBlur128.height;
        _oriBlurPos.x = (_textGroup.Rect.width - blurWidth) / 2;
        _oriBlurPos.y = (_textGroup.Rect.height - blurHeight) / 2;
        if (!IsXpPtsTextVisible)
        {
            _oriBlurPos.y += blurHeight * 0.1f;
        }

        _glowBlur.Scale = _oriBlurScale;
        _glowBlur.Position = _oriBlurPos;
    }

    private void ResetGroupTransform()
    {
        _translationDistance = (_entireGroup.Rect.height + _screenYOffset) * 1.5f;
        _groupPosition = new Vector2(_curScreenWidth / 2 - _textGroup.Rect.width / 2,
            _curScreenHeight * 0.97f - _textGroup.Rect.height - _xpBarEmptySprite.Rect.height - _screenYOffset);
        if (_isOnScreen == false)
        {
            _entireGroup.Position = new Vector2(_groupPosition.x, _groupPosition.y + _translationDistance);
        }
        else
        {
            _entireGroup.Position = _groupPosition;
        }
    }

    private IEnumerator OnXpIncrease()
    {
        float scaleFactor = 1.2f;
        float scaleUpTime = 0.02f;
        float flickerTime = 0.1f;
        float scaleDownTime = 0.1f;
        Vector2 pivot = _xpDigits.Center;
        _xpDigits.ScaleAroundPivot(new Vector2(scaleFactor, scaleFactor), pivot, scaleUpTime);
        yield return new WaitForSeconds(scaleUpTime);
        _xpDigits.Flicker(flickerTime);
        yield return new WaitForSeconds(flickerTime);
        _xpDigits.ScaleAroundPivot(new Vector2(1 / scaleFactor, 1 / scaleFactor), pivot, scaleDownTime);
    }

    private void UpdateXpPercentage()
    {
        if (CurrentLevel == PlayerXpUtil.MaxPlayerLevel)
        {
            SetXpPercentage(1.0f);
            return;
        }
        if (TotalXpOnGameStart + _curXp > CurrentLevelMaxXp)
        {
            CurrentLevel += 1;
        }
        int xpScope = CurrentLevelMaxXp - CurrentLevelMinXp;
        if (xpScope != 0)
        {
            SetXpPercentage((float)(TotalXpOnGameStart + _curXp - CurrentLevelMinXp) / xpScope);
        }
    }

    private void SetXp(int xp)
    {
        bool isXpIncrease = xp > _curXp;
        _curXp = xp < 0 ? 0 : xp;
        UpdateXpPercentage();
        ResetTransform();
        if (isXpIncrease)
        {
            MonoRoutine.Start(OnXpIncrease());
        }
    }

    private void SetPts(int pts)
    {
        _curPts = pts < 0 ? 0 : pts;
        ResetTransform();
    }

    private void SetXpPercentage(float xpPercentage)
    {
        _curXpPercentage = Mathf.Clamp01(xpPercentage);
        ResetXpBarTransform();
    }

    private int CurrentLevel
    {
        get { return _curLevel; }
        set
        {
            _curLevel = Mathf.Clamp(value, 1, PlayerXpUtil.MaxPlayerLevel);
            int levelMinXp, levelMaxXp;
            PlayerXpUtil.GetXpRangeForLevel(_curLevel, out levelMinXp, out levelMaxXp);
            CurrentLevelMinXp = levelMinXp;
            CurrentLevelMaxXp = levelMaxXp;
            UpdateXpPercentage();
        }
    }

    private int CurrentLevelMinXp { get; set; }

    private int CurrentLevelMaxXp { get; set; }

    private int TotalXpOnGameStart { get; set; }

    private float _screenYOffset;
    private MeshGUIText _xpDigits;
    private MeshGUIText _ptsDigits;
    private MeshGUIText _xpText;
    private MeshGUIText _ptsText;
    private MeshGUIText _curLevelText;
    private MeshGUIText _nextLevelText;
    private MeshGUIQuad _xpBarEmptySprite;
    private MeshGUIQuad _xpBarFullSprite;
    private MeshGUIQuad _glowBlur;
    private Animatable2DGroup _xpGroup;
    private Animatable2DGroup _ptsGroup;
    private Animatable2DGroup _textGroup;
    private Animatable2DGroup _xpBarGroup;
    private Animatable2DGroup _entireGroup;

    private Vector2 _oriBlurPos;
    private Vector2 _oriBlurScale;
    private Vector2 _oriXpBarPos;
    private Vector2 _oriXpBarEmptyScale;
    private Vector2 _oriXpBarFillScale;

    private int _curLevel;
    private int _curXp;
    private int _curPts;
    private float _curXpPercentage;

    private Vector2 _groupPosition;
    private float _curScaleFactor;

    private Vector2 _xpPtsSpawnPoint;
    private Vector2 _xpPtsDiePoint;

    AnimationScheduler _animScheduler;

    private bool _isOnScreen;
    private float _translationDistance;
    private float _xpBarHideTime;

    private bool _isTemporaryDisplay;

    private float _curScreenWidth;
    private float _curScreenHeight;
    #endregion
}
