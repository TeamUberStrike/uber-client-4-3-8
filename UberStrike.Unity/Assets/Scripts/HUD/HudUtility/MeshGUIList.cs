using System;
using System.Collections;
using System.Collections.Generic;
using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class MeshGUIList
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

    public MeshGUIList(Action onDrawMeshGUIList = null)
    {
        _maxUnilateralSlotCount = 1;
        _onUpdate = onDrawMeshGUIList;
        _listItemsGroup = new Animatable2DGroup();
        _glowBlur = new MeshGUIQuad(HudTextures.WhiteBlur128);
        _glowBlur.Name = "MeshGUIList Glow";
        _glowBlur.Depth = 2.0f;
        _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
        _entireGroup = new Animatable2DGroup();
        _entireGroup.Group.Add(_listItemsGroup);
        _entireGroup.Group.Add(_glowBlur);

        _currentVisibleGroup = new Animatable2DGroup();
        _currentDisplayIndex = 0;

        _sizeAttenuationFactor = 0.7f;
        _alphaAttenuationFactor = 0.5f;

        _isCircular = true;
        ResetHud();
        Enabled = false;

        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
        CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
    }

    public void Draw()
    {
        _entireGroup.Draw();
    }

    public void Update()
    {
        _entireGroup.Draw();
        if (_onUpdate != null)
        {
            _onUpdate();
        }
    }

    public bool HasItem(int index)
    {
        return index >= 0 && index < _listItemsGroup.Group.Count;
    }

    public void SetItemText(int index, string text)
    {
        if (HasItem(index))
        {
            MeshGUIText menuText = _listItemsGroup.Group[index] as MeshGUIText;
            menuText.Text = text;
        }
        StopAnimToIndexCoroutine();
    }

    public void InsertItem(int index, string text)
    {
        MeshGUIText meshText3D = CreateListItem(text);
        _listItemsGroup.Group.Insert(index, meshText3D);
        StopAnimToIndexCoroutine();
    }

    public void AddItem(string text)
    {
        MeshGUIText meshText3D = CreateListItem(text);
        _listItemsGroup.Group.Add(meshText3D);
        StopAnimToIndexCoroutine();
    }

    public void RemoveItem(int index)
    {
        _listItemsGroup.RemoveAndFree(index);
        StopAnimToIndexCoroutine();
    }

    public void ClearAllItems()
    {
        _listItemsGroup.ClearAndFree();
        StopAnimToIndexCoroutine();
    }

    public void FadeOut(float time, EaseType easeType)
    {
        _entireGroup.FadeAlphaTo(0.0f, time, easeType);
    }

    public void AnimUpward()
    {
        StopAnimToIndexCoroutine();
        if (_currentDisplayIndex > 0)
        {
            _currentDisplayIndex--;
            UpdateGroupDisplay(_currentDisplayIndex, _animTime);
        }
        else if (_isCircular == true)
        {
            _currentDisplayIndex = _listItemsGroup.Group.Count - 1;
            UpdateGroupDisplay(_currentDisplayIndex, _animTime);
        }
    }

    public void AnimDownward()
    {
        StopAnimToIndexCoroutine();
        if (_currentDisplayIndex < _listItemsGroup.Group.Count - 1)
        {
            _currentDisplayIndex++;
            UpdateGroupDisplay(_currentDisplayIndex, _animTime);
        }
        else if (_isCircular == true)
        {
            _currentDisplayIndex = 0;
            UpdateGroupDisplay(_currentDisplayIndex, _animTime);
        }
    }

    public void AnimToIndex(int destIndex, float time)
    {
        _destIndex = destIndex;
        _animTime = time;
        UpdateGroupDisplay(_currentDisplayIndex);
        MonoRoutine.Start(AnimToIndexCoroutine());
    }

    #region Private

    private void StopAnimToIndexCoroutine()
    {
        PreemptiveCoroutineManager.Instance.IncrementId(AnimToIndexCoroutine);
        UpdateGroupDisplay(_currentDisplayIndex);
    }

    private IEnumerator AnimToIndexCoroutine()
    {
        if (_destIndex == _currentDisplayIndex)
        {
            yield break;
        }

        int coroutineId = PreemptiveCoroutineManager.Instance.IncrementId(AnimToIndexCoroutine);

        int indexDelta = _destIndex - _currentDisplayIndex;
        int absIndexDelta = indexDelta > 0 ? indexDelta : -indexDelta;

        int step = indexDelta > 0 ? 1 : -1;
        if (_isCircular == true && absIndexDelta > _listItemsGroup.Group.Count / 2)
        {
            step = -step;
            absIndexDelta = _listItemsGroup.Group.Count - absIndexDelta;
        }
        float animTime = _animTime / absIndexDelta;
        while (_currentDisplayIndex != _destIndex)
        {
            _currentDisplayIndex += step;
            UpdateGroupDisplay(_currentDisplayIndex, animTime);

            yield return new WaitForSeconds(animTime);
            if (!PreemptiveCoroutineManager.Instance.IsCurrent(AnimToIndexCoroutine, coroutineId))
            {
                yield break;
            }

            if (_isCircular == true)
            {
                _currentDisplayIndex = CirculateSpriteIndex(_currentDisplayIndex);
            }
        }
    }

    private void ResetHud()
    {
        ResetStyle();
        ResetTransform();
    }

    private void ResetStyle()
    {
        foreach (IAnimatable2D animatable in _listItemsGroup.Group)
        {
            MeshGUIText meshText = animatable as MeshGUIText;
            HudStyleUtility.Instance.SetTeamStyle(meshText);
        }
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        ResetStyle();
        if (ev.TeamId == TeamID.RED)
        {
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_RED_COLOR;
        }
        else
        {
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
        }
        UpdateGroupDisplay(_currentDisplayIndex);
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        _scaleFactor = 0.45f;
        _itemHeight = Screen.height * 0.055f;
        UpdateGroupDisplay(_currentDisplayIndex);
        _entireGroup.Position = new Vector2(Screen.width / 2, Screen.height * 0.74f);
    }

    private MeshGUIText CreateListItem(string text)
    {
        MeshGUIText meshText3D = new MeshGUIText(text, HudAssets.Instance.InterparkBitmapFont, TextAnchor.UpperLeft);
        HudStyleUtility.Instance.SetTeamStyle(meshText3D);
        meshText3D.Alpha = 0.0f;
        if (Enabled == false)
        {
            meshText3D.Hide();
        }
        return meshText3D;
    }

    private float GetItemYOffset(float itemHeight, int slotIndex)
    {
        float yOffset = 0.0f;
        if (slotIndex == 0)
        {
            yOffset = -itemHeight / 2;
        }
        else if (slotIndex > 0)
        {
            yOffset = itemHeight / 2 + _gapBetweenItems;
            for (int i = 2; i < slotIndex + 1; i++)
            {
                float attenuatedItemHeight = itemHeight * Mathf.Pow(_sizeAttenuationFactor, i - 1);
                yOffset += attenuatedItemHeight + _gapBetweenItems - i * 2.0f * _scaleFactor;
            }
        }
        else if (slotIndex < 0)
        {
            yOffset = -itemHeight / 2;
            for (int i = 1; i < -slotIndex + 1; i++)
            {
                float attenuatedItemHeight = itemHeight * Mathf.Pow(_sizeAttenuationFactor, i);
                yOffset -= (attenuatedItemHeight + _gapBetweenItems - i * 2.0f * _scaleFactor);
            }
        }
        return yOffset;
    }

    private void UpdateGroupDisplay(int currentDisplayIndex, float time = 0.0f)
    {
        ResetListGroupDisplay(currentDisplayIndex, time);
        ResetBlurDisplay();
    }

    private void ResetListGroupDisplay(int currentDisplayIndex, float time = 0.0f)
    {
        List<int> slotIndexGroup = new List<int>();
        for (int i = 0; i < _listItemsGroup.Group.Count; i++)
        {
            int slotIndex = i - currentDisplayIndex;
            if (_isCircular == true)
            {
                if (slotIndex < -_maxUnilateralSlotCount)
                {
                    slotIndex += _listItemsGroup.Group.Count;
                }
                else if (slotIndex > _maxUnilateralSlotCount)
                {
                    slotIndex -= _listItemsGroup.Group.Count;
                }
            }
            slotIndexGroup.Add(slotIndex);
        }

        for (int i = 0; i < _listItemsGroup.Group.Count; i++)
        {
            MeshGUIText meshText = (MeshGUIText)_listItemsGroup.Group[i];
            int slotIndex = slotIndexGroup[i];
            int absSlotIndex = slotIndex > 0 ? slotIndex : -slotIndex;

            float destAlpha;
            Vector2 destScale;
            Vector2 destPosition;
            if (slotIndex <= _maxUnilateralSlotCount && slotIndex >= -_maxUnilateralSlotCount)
            {
                float alphaAttenuation = Mathf.Pow(_alphaAttenuationFactor, absSlotIndex);
                destAlpha = 1.0f * alphaAttenuation;
            }
            else
            {
                destAlpha = 0.0f;
            }

            float sizeAttenuation = Mathf.Pow(_sizeAttenuationFactor, absSlotIndex);
            float destScaleFloat = 1.0f * sizeAttenuation * _scaleFactor;
            destScale = new Vector2(destScaleFloat, destScaleFloat);
            destPosition = new Vector2(-meshText.TextBounds.x * destScaleFloat / 2,
                GetItemYOffset(_itemHeight, slotIndex));

            meshText.StopFading();
            meshText.StopMoving();
            meshText.StopScaling();
            meshText.FadeAlphaTo(destAlpha, time, EaseType.Out);
            meshText.MoveTo(destPosition, time, EaseType.Out);
            meshText.ScaleTo(destScale, time, EaseType.Out);
        }
    }

    private void ResetBlurDisplay()
    {
        _currentVisibleGroup.Group.Clear();
        foreach (IAnimatable2D anim in _listItemsGroup.Group)
        {
            MeshGUIText meshText = anim as MeshGUIText;
            if (meshText.Alpha > 0.0f)
            {
                _currentVisibleGroup.Group.Add(meshText);
            }
        }

        float blurWidth = _currentVisibleGroup.Rect.width * HudStyleUtility.BLUR_WIDTH_SCALE_FACTOR;
        float blurHeight = _currentVisibleGroup.Rect.height * HudStyleUtility.BLUR_HEIGHT_SCALE_FACTOR;
        _glowBlur.Scale = new Vector2(blurWidth / HudTextures.WhiteBlur128.width,
            blurHeight / HudTextures.WhiteBlur128.height);
        _glowBlur.Position = new Vector2(-blurWidth / 2, -blurHeight / 2);

        _glowBlur.StopFading();
        _glowBlur.Alpha = 1.0f;
    }

    private int CirculateSpriteIndex(int index)
    {
        if (index < 0)
        {
            index += _listItemsGroup.Group.Count;
        }
        else if (index >= _listItemsGroup.Group.Count)
        {
            index -= _listItemsGroup.Group.Count;
        }
        return index;
    }

    private float _sizeAttenuationFactor;
    private float _alphaAttenuationFactor;
    private float _gapBetweenItems = 1.0f;
    private float _animTime = 0.2f;
    private int _destIndex;

    private bool _isCircular;
    private int _maxUnilateralSlotCount;

    private int _currentDisplayIndex;
    private Animatable2DGroup _listItemsGroup;
    private MeshGUIQuad _glowBlur;
    private Animatable2DGroup _entireGroup;
    private Animatable2DGroup _currentVisibleGroup;

    private Action _onUpdate;

    private float _scaleFactor;
    private float _itemHeight;
    #endregion
}
