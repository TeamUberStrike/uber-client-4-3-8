using System.Collections;
using UnityEngine;

public class QuickItemHud
{
    #region Fields

    private const float EmptyAlpha = 0.3f;
    private const float BackgroundAlpha = 0.3f;
    private const float CooldownAlpha = 1.0f;
    private const float RechargeAlpha = 0.5f;
    private const int AngleOffset = 0;
    private const int TotalFillAngle = 360;

    private MeshGUICircle _recharge;
    private MeshGUICircle _cooldown;
    private MeshGUIQuad _cooldownFlash;
    private MeshGUIQuad _background;
    private MeshGUIQuad _selection;
    private MeshGUIQuad _icon;
    private MeshGUIText _countText;
    private MeshGUIText _descriptionText;

    private Animatable2DGroup _quickItemGroup;
    private int _amount;
    private float _cooldownTime;
    private float _cooldownMax;
    private float _rechargeTime;
    private float _rechargeTimeMax;
    private bool _isAnimating;
    private bool _isCooliingDown;
    private Vector2 _expandGroupPos;
    private Vector2 _collapseGroupPos;

    #endregion

    public string Name
    {
        get;
        private set;
    }

    public bool IsEmpty
    {
        get;
        private set;
    }

    public int Amount
    {
        get { return _amount; }
        set
        {
            bool isDecreasing = value < _amount;
            _amount = value > 0 ? value : 0;
            UpdateSpringCountText(isDecreasing);
        }
    }

    public float Cooldown
    {
        set
        {
            if (_cooldownTime != value)
            {
                _cooldownTime = value;
                UpdateCooldownAngle();
            }
        }
    }

    public float CooldownMax
    {
        set
        {
            _cooldownMax = value;
            UpdateCooldownAngle();
        }
    }

    public float Recharging
    {
        set
        {
            if (_rechargeTime != value)
            {
                _rechargeTime = value;
                UpdateRechargingAngle();
            }
        }
    }

    public float RechargingMax
    {
        set
        {
            _rechargeTimeMax = value;
            UpdateRechargingAngle();
        }
    }

    public Animatable2DGroup Group
    {
        get { return _quickItemGroup; }
    }

    public QuickItemHud(string name)
    {
        Name = name;

        _amount = 0;

        _recharge = new MeshGUICircle(ConsumableHudTextures.CircleBlue)
        {
            Name = name + "Recharging",
            Alpha = RechargeAlpha,
            Depth = 2,
        };
        _recharge.CircleMesh.StartAngle = AngleOffset;

        _cooldown = new MeshGUICircle(ConsumableHudTextures.CircleBlue)
        {
            Name = name + "Cooldown",
            Alpha = CooldownAlpha,
            Depth = 3,
        };
        _cooldown.CircleMesh.StartAngle = AngleOffset;

        _cooldownFlash = new MeshGUIQuad(ConsumableHudTextures.CircleWhite, TextAnchor.MiddleCenter)
        {
            Name = name + "Flash",
            Alpha = 0,
            Depth = 3,
        };

        _background = new MeshGUIQuad(ConsumableHudTextures.CircleBlue, TextAnchor.MiddleCenter)
        {
            Name = name + "CooldownBackground",
            Alpha = BackgroundAlpha,
            Depth = 6,
        };

        _countText = new MeshGUIText(_amount.ToString(), HudAssets.Instance.HelveticaBitmapFont, TextAnchor.LowerCenter)
        {
            NamePrefix = name,
            Depth = 7,
        };

        if (!ApplicationDataManager.IsMobile)
        {
            _descriptionText = new MeshGUIText("Key: 7", HudAssets.Instance.HelveticaBitmapFont, TextAnchor.LowerCenter)
            {
                NamePrefix = name,
                Depth = 8,
            };

            _selection = new MeshGUIQuad(ConsumableHudTextures.CircleBlue, TextAnchor.MiddleCenter)
            {
                Name = name + "Selection",
                Depth = 4,
            };
        }

        _icon = new MeshGUIQuad(ConsumableHudTextures.AmmoBlue, TextAnchor.MiddleCenter)
        {
            Name = name + "Icon",
            Depth = 1,
            Alpha = EmptyAlpha,
        };

        _quickItemGroup = new Animatable2DGroup();
        _quickItemGroup.Group.Add(_recharge);
        _quickItemGroup.Group.Add(_cooldown);
        _quickItemGroup.Group.Add(_cooldownFlash);
        _quickItemGroup.Group.Add(_background);
        _quickItemGroup.Group.Add(_icon);
        _quickItemGroup.Group.Add(_countText);
        if (!ApplicationDataManager.IsMobile)
        {
            _quickItemGroup.Group.Add(_descriptionText);
            _quickItemGroup.Group.Add(_selection);
        }
    }

    public void SetKeyBinding(string binding)
    {
        if (!ApplicationDataManager.IsMobile)
        {
            _descriptionText.Text = "Key: " + binding;
        }
    }

    public void SetRechargeBarVisible(bool isVisible)
    {
        _recharge.IsEnabled = isVisible;
    }

    public void Expand(Vector2 destPos, float delay = 0)
    {
        ResetHud();
        _expandGroupPos = destPos;
        Vector2 descriptionTextPos = new Vector2(_background.Size.x / 2, _background.Size.y + _countText.Size.y * 1.5f);
        if (_quickItemGroup.IsVisible)
        {
            _quickItemGroup.MoveTo(_expandGroupPos, 0.3f, EaseType.Berp, delay);
            _descriptionText.FadeAlphaTo(1, 0.3f, EaseType.In);
            _descriptionText.MoveTo(descriptionTextPos, 0.3f, EaseType.Berp, delay);
        }
        else
        {
            _quickItemGroup.Position = _expandGroupPos;
            _descriptionText.Alpha = 1;
            _descriptionText.Position = descriptionTextPos;
        }
        IsExpanded = true;
    }

    public void Expand(bool moveNext = true)
    {
        ResetHud();

        float startPos = 40;
        if (moveNext) startPos = -40;

        _quickItemGroup.Position = new Vector2(startPos, CollapsedHeight);
        _quickItemGroup.MoveTo(new Vector2(0, CollapsedHeight), 0.3f, EaseType.Berp, 0.0f);
        _quickItemGroup.FadeAlphaTo(1.0f, 0.3f);
        _cooldown.StopFading();
        _cooldown.Alpha = 0;
        _cooldownFlash.StopFading();
        _cooldownFlash.Alpha = 0;

        IsExpanded = true;
    }

    public void Collapse(Vector2 destPos, float delay = 0)
    {
        ResetHud();
        _collapseGroupPos = destPos;
        Vector2 descriptionTextPos = new Vector2(_background.Size.x / 2, _background.Size.y + 5);
        if (_quickItemGroup.IsVisible)
        {
            _quickItemGroup.MoveTo(_collapseGroupPos, 0.3f, EaseType.Berp, delay);
            _descriptionText.FadeAlphaTo(0, 0.3f, EaseType.Out);
            _descriptionText.MoveTo(descriptionTextPos, 0.3f, EaseType.Berp, delay);
        }
        else
        {
            _quickItemGroup.Position = _collapseGroupPos;
            _descriptionText.Alpha = 0;
            _descriptionText.Position = descriptionTextPos;
        }
        IsExpanded = false;
    }

    public void Collapse(bool moveNext = true)
    {
        ResetHud();

        float startPos = -40;
        if (moveNext) startPos = 40;

        _quickItemGroup.Position = new Vector2(0, CollapsedHeight);
        _quickItemGroup.MoveTo(new Vector2(startPos, CollapsedHeight), 0.3f, EaseType.Berp, 0.0f);
        _quickItemGroup.FadeAlphaTo(0.0f, 0.3f);
        

        IsExpanded = false;
    }

    public void SetSelected(bool selected, bool moveNext = true)
    {
        if (IsEmpty) selected = false;

        if (ApplicationDataManager.IsMobile)
        {
            if (!selected)
            {
                Collapse(moveNext);
            }
            else
            {
                Expand(moveNext);
            }
        }
        else
        {
            _selection.IsEnabled = selected;
        }
    }

    public float ExpandedHeight { get; private set; }

    public float CollapsedHeight { get; private set; }

    public bool IsExpanded
    {
        get;
        private set;
    }

    public void ConfigureEmptySlot()
    {
        _quickItemGroup.Hide();
        IsEmpty = true;
        ResetHud();
    }

    public void ConfigureSlot(Color textColor, Texture rechargingTexture,
        Texture cooldownTexture, Texture backgroundTexture,
        Texture selectionTexture, Texture2D icon)
    {
        HudStyleUtility.Instance.SetDefaultStyle(_countText);
        if (!ApplicationDataManager.IsMobile)
            HudStyleUtility.Instance.SetDefaultStyle(_descriptionText);

        _recharge.Texture = rechargingTexture;
        _cooldown.Texture = cooldownTexture;
        _cooldown.Alpha = 0;
        _cooldownFlash.Texture = cooldownTexture;
        _cooldownFlash.Alpha = 0;
        _background.Texture = backgroundTexture;
        _background.Alpha = BackgroundAlpha;
        
        _icon.Texture = icon;

        _countText.BitmapMeshText.ShadowColor = textColor;
        _countText.BitmapMeshText.AlphaMin = 0.40f;
        if (!ApplicationDataManager.IsMobile)
        {
            _selection.Texture = selectionTexture;
            _descriptionText.BitmapMeshText.ShadowColor = textColor;
            _descriptionText.BitmapMeshText.AlphaMin = 0.40f;
            _descriptionText.BitmapMeshText.MainColor = _descriptionText.BitmapMeshText.MainColor.SetAlpha(0.5f);
        }
        _quickItemGroup.Show();

        IsEmpty = false;
        ResetHud();
    }

    public void ResetHud()
    {
        _quickItemGroup.StopScaling();
        _quickItemGroup.StopMoving();
        _cooldownFlash.StopScaling();
        _cooldownFlash.StopFading();
        _isAnimating = false;
        ResetTransform();
    }


    #region Private

    private void ResetTransform()
    {
        const float textScaleFactor = 0.25f;

        Vector2 scale = Vector2.one * 0.8f;
        _background.Scale = scale;
        _icon.Scale = scale * 0.9f;
        _recharge.Scale = scale * 0.8f;
        _cooldown.Scale = scale;
        _cooldownFlash.Scale = scale;
        _countText.Scale = Vector2.one * textScaleFactor;
        if (!ApplicationDataManager.IsMobile)
        {
            _selection.Scale = scale;
            _descriptionText.Scale = Vector2.one * textScaleFactor;
        }

        Vector2 iconPos = _background.Size / 2;
        _icon.Position = iconPos;
        _recharge.Position = _background.Size / 2;
        _cooldown.Position = _background.Size / 2;
        _cooldownFlash.Position = _background.Size / 2;
        _background.Position = _background.Size / 2;
        _countText.Position = new Vector2(_background.Size.x * 0.95f, _background.Size.y + _countText.Size.y * 0.5f);
        if (!ApplicationDataManager.IsMobile)
        {
            _selection.Position = _selection.Size / 2;
        }
        UpdateExpandedHeight();
    }

    private void UpdateCollapsedHeight()
    {
        if (ApplicationDataManager.IsMobile) return;
        
        Vector2 pos = _descriptionText.Position;
        _descriptionText.Position = new Vector2(_background.Size.x / 2, _background.Size.y + 5);
        float h = Group.Rect.height;
        _descriptionText.Position = pos;
        CollapsedHeight = h;
    }

    private void UpdateExpandedHeight()
    {
        UpdateCollapsedHeight();
        ExpandedHeight = CollapsedHeight + _countText.Size.y;
    }

    private void UpdateSpringCountText(bool isDecreasing)
    {
        if (_amount <= 0)
        {
            _icon.Alpha = EmptyAlpha;
            _cooldown.Alpha = 0;
        }
        else
        {
            if (!ApplicationDataManager.IsMobile || IsExpanded)
                _icon.Alpha = 1;
            else
                _icon.Alpha = 0;
        }
        _countText.Text = _amount.ToString();
        if (isDecreasing)
        {
            MonoRoutine.Start(OnQuickItemDecrement());
        }
    }

    private IEnumerator OnQuickItemDecrement()
    {
        ResetHud();

        _isAnimating = true;
        float sizeIncreaseTime = 0.05f;
        float sizeIncreaseScale = 1.20f;
        float waitTime = 0.3f;
        float sizeDecreaseTime = 0.05f;
        Vector2 pivot = _quickItemGroup.Center;
        _quickItemGroup.ScaleAroundPivot(new Vector2(sizeIncreaseScale, sizeIncreaseScale), pivot, sizeIncreaseTime);
        yield return new WaitForSeconds(sizeIncreaseTime + waitTime);
        if (_isAnimating == true)
        {
            _quickItemGroup.ScaleAroundPivot(new Vector2(1.0f / sizeIncreaseScale, 1.0f / sizeIncreaseScale),
                   pivot, sizeDecreaseTime);
            yield return new WaitForSeconds(sizeDecreaseTime);
        }
        _isAnimating = false;
    }

    private void UpdateCooldownAngle()
    {
        float newCooldownAngle = _cooldownMax == 0 ? TotalFillAngle : (_cooldownMax - _cooldownTime) * TotalFillAngle / _cooldownMax;
        if (newCooldownAngle < TotalFillAngle)
        {
            if (_cooldown.Angle == TotalFillAngle)
            {
                OnCooldownStart();
            }
            if (_isCooliingDown && _amount > 0 && (!ApplicationDataManager.IsMobile || IsExpanded)) _cooldown.Alpha = CooldownAlpha;
        }
        else
        {
            if (_cooldown.Angle < TotalFillAngle && _amount > 0)
            {
                OnCooldownFinish();
            }
            if (!_isCooliingDown) _cooldown.Alpha = 0;
        }
        _cooldown.Angle = newCooldownAngle;
    }

    private void OnCooldownStart()
    {
        _isCooliingDown = true;
    }

    private void OnCooldownFinish()
    {
        _isCooliingDown = false;
        MonoRoutine.Start(DoFlashAnim());
    }

    private void UpdateRechargingAngle()
    {
        float newRechargeAngle = _rechargeTimeMax == 0 ? TotalFillAngle : (_rechargeTimeMax - _rechargeTime) * TotalFillAngle / _rechargeTimeMax;
        _recharge.Angle = newRechargeAngle;
    }

    private IEnumerator DoFlashAnim()
    {
        ResetHud();

        _isAnimating = true;

        _cooldownFlash.StopScaling();
        _cooldownFlash.StopFading();
        _cooldownFlash.StopFading();
        ResetCooldownFlashView(1);

        float animTime = 0.2f;
        _cooldownFlash.ScaleTo(Vector2.one * 1.5f, animTime);
        _cooldownFlash.Flicker(animTime);
        _cooldownFlash.FadeAlphaTo(0.0f, animTime);
        yield return new WaitForSeconds(animTime + 0.1f);
        
        if (_isAnimating == true)
        {
            ResetCooldownFlashView(0);
        }
    }

    private void ResetCooldownFlashView(float alpha)
    {
        if (ApplicationDataManager.IsMobile && !IsExpanded) alpha = 0;
        _cooldownFlash.Alpha = alpha;
        _cooldownFlash.Scale = Vector2.one * 0.8f;
        _cooldownFlash.Position = _background.Size / 2;
    }

    #endregion
}