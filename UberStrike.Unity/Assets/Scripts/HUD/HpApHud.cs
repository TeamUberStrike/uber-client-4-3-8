using System.Collections;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class HpApHud : Singleton<HpApHud>
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

    public int HP
    {
        get
        {
            return _curHP;
        }
        set
        {
            SetHealthPoint(value);
        }
    }

    public int AP
    {
        get
        {
            return _curAP;
        }
        set
        {
            SetArmorPoint(value);
        }
    }

    public void Update()
    {
        _entireGroup.Draw();
        if (_isLowHealth && _curHP != 0 &&
            _nextWarningTime > 0 && Time.time > _nextWarningTime)
        {
            MonoRoutine.Start(OnHealthLow());
            _nextWarningTime = -1.0f;
        }
    }

    public void Draw()
    {
        _entireGroup.Draw();
    }

    #region Private
    private HpApHud()
    {
        _curAP = 0;
        _curHP = 0;

        TextAnchor textAnchor = TextAnchor.LowerLeft;
        _hpDigitsText = new MeshGUIText(_curHP.ToString(), HudAssets.Instance.InterparkBitmapFont, textAnchor);
        _hpDigitsText.NamePrefix = "HP";
        _apDigitsText = new MeshGUIText(_curAP.ToString(), HudAssets.Instance.InterparkBitmapFont, textAnchor);
        _apDigitsText.NamePrefix = "AP";
        _hpText = new MeshGUIText("HP", HudAssets.Instance.HelveticaBitmapFont, textAnchor);
        _apText = new MeshGUIText("AP", HudAssets.Instance.HelveticaBitmapFont, textAnchor);
        _hpApGroup = new Animatable2DGroup();
        _hpApGroup.Group.Add(_hpDigitsText);
        _hpApGroup.Group.Add(_apDigitsText);
        _hpApGroup.Group.Add(_hpText);
        _hpApGroup.Group.Add(_apText);
        _glowBlur = new MeshGUIQuad(HudTextures.WhiteBlur128);
        _glowBlur.Name = "HpApHudGlow";
        _glowBlur.Depth = 1.0f;
        _entireGroup = new Animatable2DGroup();
        _entireGroup.Group.Add(_hpApGroup);
        _entireGroup.Group.Add(_glowBlur);

        ResetHud();
        Enabled = false;

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
        HudStyleUtility.Instance.SetDefaultStyle(_hpDigitsText);
        HudStyleUtility.Instance.SetDefaultStyle(_apDigitsText);
        HudStyleUtility.Instance.SetDefaultStyle(_hpText);
        HudStyleUtility.Instance.SetDefaultStyle(_apText);
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        if (ev.TeamId == TeamID.RED)
        {
            HudStyleUtility.Instance.SetRedStyle(_hpDigitsText);
            HudStyleUtility.Instance.SetRedStyle(_apDigitsText);
            HudStyleUtility.Instance.SetRedStyle(_hpText);
            HudStyleUtility.Instance.SetRedStyle(_apText);
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_RED_COLOR;
        }
        else
        {
            HudStyleUtility.Instance.SetBlueStyle(_hpDigitsText);
            HudStyleUtility.Instance.SetBlueStyle(_apDigitsText);
            HudStyleUtility.Instance.SetBlueStyle(_hpText);
            HudStyleUtility.Instance.SetBlueStyle(_apText);
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
        }
        _hpText.BitmapMeshText.AlphaMin = 0.40f;
        _apText.BitmapMeshText.AlphaMin = 0.40f;
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        _curScaleFactor = 0.65f;
        ResetHpApTransform();
        ResetBlurTransform();
#if !UNITY_ANDROID && !UNITY_IPHONE
        _entireGroup.Position = new Vector2(Screen.width * 0.05f, Screen.height * 0.95f);
#else
        _entireGroup.Position = new Vector2(Screen.width * 0.04f, Screen.height * 0.08f);
#endif
        _entireGroup.UpdateMeshGUIPosition();
    }

    private void ResetHpApTransform()
    {
        float adjustFactor = 0.07f;

        _hpDigitsText.Text = _curHP.ToString();
        _hpDigitsText.Scale = new Vector2(1.0f * _curScaleFactor, 1.0f * _curScaleFactor);
        _hpDigitsText.Position = new Vector2(0.0f, adjustFactor * _hpDigitsText.Size.y);

        float gap = 3.0f;

        float offsetX = 0.0f;
        offsetX += _hpDigitsText.Size.x + gap;
        _hpText.Scale = new Vector2(0.35f * _curScaleFactor, 0.35f * _curScaleFactor);
        _hpText.Position = new Vector2(offsetX, 0.0f);

        offsetX += Screen.width * 0.030f;
        _apDigitsText.Text = _curAP.ToString();
        _apDigitsText.Scale = new Vector2(0.7f * _curScaleFactor, 0.7f * _curScaleFactor);
        _apDigitsText.Position = new Vector2(offsetX, adjustFactor * _apDigitsText.Size.y);

        offsetX += _apDigitsText.Size.x + gap;
        _apText.Scale = new Vector2(0.35f * _curScaleFactor, 0.35f * _curScaleFactor);
        _apText.Position = new Vector2(offsetX, 0.0f);
    }

    private void ResetBlurTransform()
    {
        float blurWidth = _hpApGroup.Rect.width * HudStyleUtility.BLUR_WIDTH_SCALE_FACTOR;
        float blurHeight = _hpApGroup.Rect.height * HudStyleUtility.BLUR_HEIGHT_SCALE_FACTOR;
        _glowBlur.Scale = new Vector2(blurWidth / HudTextures.WhiteBlur128.width,
            blurHeight / HudTextures.WhiteBlur128.height);
        _glowBlur.Position = new Vector2((_hpApGroup.Rect.width - blurWidth) / 2,
            (-_hpApGroup.Rect.height - blurHeight) / 2);
    }

    private void SetHealthPoint(int hp)
    {
        bool isHealthIncrease = hp > _curHP;
        bool isHealthDecrease = hp < _curHP;
        _curHP = hp < 0 ? 0 : hp;
        ResetTransform();
        if (isHealthIncrease)
        {
            OnHealthIncrease();
        }
        if (isHealthDecrease)
        {
            MonoRoutine.Start(OnHealthDecrease());
        }
        bool isLowHealth = _curHP < WARNING_HEALTH_LOW_VALUE;
        if (isLowHealth != _isLowHealth)
        {
            _isLowHealth = isLowHealth;
            _nextWarningTime = Time.time + WARNING_HEALTH_LOW_INTERVAL;
        }
    }

    private void SetArmorPoint(int ap)
    {
        bool isArmorDecrease = ap < _curAP;
        _curAP = ap < 0 ? 0 : ap;
        ResetTransform();
        if (isArmorDecrease)
        {
            MonoRoutine.Start(OnArmorDecrease());
        }
    }

    private void OnHealthIncrease()
    {
        float flickerTime = 0.1f;
        _hpDigitsText.Flicker(flickerTime);
        _apDigitsText.Flicker(flickerTime);
    }

    private IEnumerator OnHealthDecrease()
    {
        int berpAnimId = PreemptiveCoroutineManager.Instance.IncrementId(OnHealthDecrease);

        float berpTime = 0.1f;
        float berpScale = 0.8f;
        Vector2 pivot = _hpDigitsText.Center;
        _hpDigitsText.ScaleAroundPivot(new Vector2(berpScale, berpScale), pivot, berpTime, EaseType.Berp);
        yield return new WaitForSeconds(berpTime);
        if (PreemptiveCoroutineManager.Instance.IsCurrent(OnHealthDecrease, berpAnimId))
        {
            _hpDigitsText.ScaleAroundPivot(new Vector2(1.0f / berpScale, 1.0f / berpScale),
                pivot, berpTime, EaseType.Berp);
        }
    }

    private IEnumerator OnHealthLow()
    {
        int berpAnimId = PreemptiveCoroutineManager.Instance.IncrementId(OnHealthDecrease);

        float sizeIncreaseTime = 0.02f;
        float sizeDecreaseTime = 0.2f;
        float sizeIncreaseScale = 0.8f;
        Vector2 pivot = _hpDigitsText.Center;
        Vector2 oriScale = _hpDigitsText.Scale;
        _hpDigitsText.ScaleAroundPivot(new Vector2(sizeIncreaseScale, sizeIncreaseScale), pivot, sizeIncreaseTime);
        yield return new WaitForSeconds(sizeIncreaseTime);

        if (!PreemptiveCoroutineManager.Instance.IsCurrent(OnHealthDecrease, berpAnimId))
        {
            _nextWarningTime = Time.time + WARNING_HEALTH_LOW_INTERVAL;
            yield break;
        }
        _hpDigitsText.ScaleToAroundPivot(oriScale, pivot, sizeDecreaseTime);
        yield return new WaitForSeconds(sizeDecreaseTime);
        _nextWarningTime = Time.time + WARNING_HEALTH_LOW_INTERVAL;
    }

    private IEnumerator OnArmorDecrease()
    {
        float berpTime = 0.05f;
        float berpScale = 0.8f;
        Vector2 pivot = _apDigitsText.Center;
        _apDigitsText.ScaleAroundPivot(new Vector2(berpScale, berpScale), pivot, berpTime, EaseType.Berp);
        yield return new WaitForSeconds(berpTime);
        _apDigitsText.ScaleAroundPivot(new Vector2(1.0f / berpScale, 1.0f / berpScale), pivot, berpTime, EaseType.Berp);
    }

    private float _curScaleFactor;
    private MeshGUIText _hpDigitsText;
    private MeshGUIText _apDigitsText;
    private MeshGUIText _hpText;
    private MeshGUIText _apText;
    private Animatable2DGroup _hpApGroup;
    private MeshGUIQuad _glowBlur;
    private Animatable2DGroup _entireGroup;

    private int _curHP;
    private int _curAP;

    private static int WARNING_HEALTH_LOW_VALUE = 25;
    private static float WARNING_HEALTH_LOW_INTERVAL = 0.8f;
    private bool _isLowHealth;
    private float _nextWarningTime;
    #endregion
}
