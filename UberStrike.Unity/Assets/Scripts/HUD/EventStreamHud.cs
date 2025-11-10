using Cmune.Util;
using UberStrike.Realtime.Common;
using UnityEngine;

public class EventStreamHud : Singleton<EventStreamHud>
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

    public void Draw()
    {
        _entireGroup.Draw();
    }

    public void Update()
    {
        if (_textGroup.Group.Count > 0 && Time.time > _nextRemoveEventTime)
        {
            DequeueEvent();
            ResetTransform();
        }
    }

    public void AddEventText(string subjective, TeamID subTeamId, string verb, string objective = "", TeamID objTeamId = TeamID.NONE)
    {
        if (_textGroup.Group.Count == 0)
        {
            UpdateNextRemoveEventTime();
        }
        if (ReachMaxEventNum())
        {
            DequeueEvent();
        }

        Animatable2DGroup eventTextGroup = GenerateEventText(subjective, subTeamId, verb, objective, objTeamId);
        _textGroup.Group.Add(eventTextGroup);
        ResetTransform();
    }

    public void ClearAllEvents()
    {
        _textGroup.ClearAndFree();
        ResetTransform();
    }

    #region Private
    private EventStreamHud()
    {
        _maxEventNum = 8;
        _maxDisplayTime = 5.0f;
        _textScale = 0.20f;
        _vertGapBetweenText = 5.0f;
        _horzGapBetweenText = 5.0f;
        _glowBlur = new MeshGUIQuad(HudTextures.WhiteBlur128);
        _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
        _glowBlur.Name = "EventStreamHudGlow";
        _glowBlur.Depth = 1.0f;
        _textGroup = new Animatable2DGroup();
        _entireGroup = new Animatable2DGroup();
        _entireGroup.Group.Add(_textGroup);
        _entireGroup.Group.Add(_glowBlur);
        Enabled = false;

        CmuneEventHandler.AddListener<OnSetPlayerTeamEvent>(OnTeamChange);
        CmuneEventHandler.AddListener<ScreenResolutionEvent>(OnScreenResolutionChange);
    }

    private void DequeueEvent()
    {
        _textGroup.RemoveAndFree(0);
        UpdateNextRemoveEventTime();
    }

    private bool ReachMaxEventNum()
    {
        return _textGroup.Group.Count == _maxEventNum && _maxEventNum > 0;
    }

    private void UpdateNextRemoveEventTime()
    {
        _nextRemoveEventTime = Time.time + _maxDisplayTime;
    }

    private Animatable2DGroup GenerateEventText(string subjective, TeamID subTeamId, string verb, string objective = "", TeamID objTeamId = TeamID.NONE)
    {
        MeshGUIText subjectiveText = new MeshGUIText(subjective, HudAssets.Instance.InterparkBitmapFont, TextAnchor.UpperRight);
        subjectiveText.NamePrefix = "EventStreamHud";
        MeshGUIText verbText = new MeshGUIText(verb, HudAssets.Instance.InterparkBitmapFont, TextAnchor.UpperRight);
        verbText.NamePrefix = "EventStreamHud";
        MeshGUIText objectiveText = new MeshGUIText(objective, HudAssets.Instance.InterparkBitmapFont, TextAnchor.UpperRight);
        objectiveText.NamePrefix = "EventStreamHud";

        SetTextStyleByTeamId(subjectiveText, subTeamId);
        SetTextStyleByTeamId(verbText, TeamID.NONE);
        SetTextStyleByTeamId(objectiveText, objTeamId);

        float xOffset = 0.0f;
        objectiveText.Position = Vector2.zero;
        objectiveText.Scale = Vector2.one * _textScale;
        xOffset -= (objectiveText.Rect.width + _horzGapBetweenText);
        verbText.Position = new Vector2(xOffset, 0.0f);
        verbText.Scale = Vector2.one * _textScale;
        xOffset -= (verbText.Rect.width + _horzGapBetweenText);
        subjectiveText.Position = new Vector2(xOffset, 0.0f);
        subjectiveText.Scale = Vector2.one * _textScale;

        Animatable2DGroup eventTextGroup = new Animatable2DGroup();
        eventTextGroup.Group.Add(subjectiveText);
        eventTextGroup.Group.Add(verbText);
        eventTextGroup.Group.Add(objectiveText);

        return eventTextGroup;
    }

    private void SetTextStyleByTeamId(MeshGUIText text, TeamID teamId)
    {
        HudStyleUtility.Instance.SetNoShadowStyle(text);
        switch (teamId)
        {
            case TeamID.RED:
                text.BitmapMeshText.ShadowColor = HudStyleUtility.DEFAULT_RED_COLOR;
                break;
            case TeamID.BLUE:
                text.BitmapMeshText.ShadowColor = HudStyleUtility.DEFAULT_BLUE_COLOR;
                break;
        }
    }

    private void OnTeamChange(OnSetPlayerTeamEvent ev)
    {
        if (ev.TeamId == TeamID.RED)
        {
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_RED_COLOR;
        }
        else
        {
            _glowBlur.Color = HudStyleUtility.GLOW_BLUR_BLUE_COLOR;
        }
    }

    private void OnScreenResolutionChange(ScreenResolutionEvent ev)
    {
        ResetTransform();
    }

    private void ResetTransform()
    {
        ResetTextGroupTransform();
        ResetBlurTransform();
        _entireGroup.Position = new Vector2(Screen.width * 0.95f, Screen.height * 0.02f);
    }

    private void ResetTextGroupTransform()
    {
        for (int i = 0; i < _textGroup.Group.Count; i++)
        {
            Animatable2DGroup eventTextGroup = _textGroup.Group[i] as Animatable2DGroup;
            ResetEventTextTransform(eventTextGroup);
            eventTextGroup.Position = new Vector2(0.0f,
                i * (eventTextGroup.Rect.height + _vertGapBetweenText));
            eventTextGroup.UpdateMeshGUIPosition();
        }
    }

    private void ResetEventTextTransform(Animatable2DGroup eventTextGroup)
    {
        MeshGUIText subjectiveText = eventTextGroup.Group[0] as MeshGUIText;
        MeshGUIText verbText = eventTextGroup.Group[1] as MeshGUIText;
        MeshGUIText objectiveText = eventTextGroup.Group[2] as MeshGUIText;

        float xOffset = 0.0f;
        objectiveText.Position = Vector2.zero;
        objectiveText.Scale = Vector2.one * _textScale;
        xOffset -= (objectiveText.Rect.width + _horzGapBetweenText);
        verbText.Position = new Vector2(xOffset, 0.0f);
        verbText.Scale = Vector2.one * _textScale;
        verbText.BitmapMeshText.ShadowColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        xOffset -= (verbText.Rect.width + _horzGapBetweenText);
        subjectiveText.Position = new Vector2(xOffset, 0.0f);
        subjectiveText.Scale = Vector2.one * _textScale;
    }

    private void ResetBlurTransform()
    {
        float blurWidth = _textGroup.Rect.width * HudStyleUtility.BLUR_WIDTH_SCALE_FACTOR * 0.8f;
        float blurHeight = _textGroup.Rect.height * HudStyleUtility.BLUR_HEIGHT_SCALE_FACTOR;
        _glowBlur.Scale = new Vector2(blurWidth / HudTextures.WhiteBlur128.width,
            blurHeight / HudTextures.WhiteBlur128.height);
        _glowBlur.Position = new Vector2((-blurWidth - _textGroup.Rect.width) / 2,
            (_textGroup.Rect.height - blurHeight) / 2);
    }

    private Animatable2DGroup _textGroup;
    private Animatable2DGroup _entireGroup;
    private MeshGUIQuad _glowBlur;
    private float _textScale;
    private float _vertGapBetweenText;
    private float _horzGapBetweenText;
    private float _maxDisplayTime;
    private int _maxEventNum;
    private float _nextRemoveEventTime;
    #endregion
}