using UnityEngine;
using System.Collections;

class LocalShotFeedbackAnim : AbstractAnim
{
    public LocalShotFeedbackAnim(Animatable2DGroup textGroup, MeshGUIText meshText,
        float displayTime, float fadeOutAnimTime, SoundEffectType sound)
    {
        _textGroup = textGroup;
        _text = meshText;
        _displayTime = displayTime;
        _fadeOutAnimTime = fadeOutAnimTime;
        _sound = sound;
        Duration = _displayTime + _fadeOutAnimTime;
    }

    protected override void OnStart()
    {
        _textGroup.Group.Add(_text);
        _text.FadeAlphaTo(1.0f, 0.0f, EaseType.None);
        SfxManager.Play2dAudioClip(_sound);
    }

    protected override void OnStop()
    {
        _text.FadeAlphaTo(0.0f, 0.0f, EaseType.None);
        _text.StopFading();
        _isFading = false;
        _textGroup.RemoveAndFree(_text);
    }

    protected override void OnUpdate()
    {
        if (IsAnimating && Time.time > StartTime + _displayTime && !_isFading)
        {
            DoFadeoutAnim();
        }
        _text.ShadowColorAnim.Alpha = 0.0f;
    }

    private void DoFadeoutAnim()
    {
        if (IsAnimating)
        {
            _isFading = true;
            _text.FadeAlphaTo(0.0f, _fadeOutAnimTime, EaseType.Out);
        }
    }

    private Animatable2DGroup _textGroup;
    private MeshGUIText _text;
    private float _displayTime;
    private float _fadeOutAnimTime;
    private bool _isFading = false;
    private SoundEffectType _sound;
}

public class LocalShotFeedbackHud : Singleton<LocalShotFeedbackHud>
{
    public void Update()
    {
        _textGroup.Draw();
    }

    public void DisplayLocalShotFeedback(InGameEventFeedbackType type)
    {
        MeshGUIText text = new MeshGUIText("", HudAssets.Instance.InterparkBitmapFont,
            TextAnchor.MiddleCenter);
        SoundEffectType sound = SoundEffectType.None;
        switch (type)
        {
            case InGameEventFeedbackType.NutShot:
                text.Text = "Nut Shot";
                sound = SoundEffectType.UINutShot;
                break;

            case InGameEventFeedbackType.HeadShot:
                text.Text = "Head Shot";
                sound = SoundEffectType.UIHeadShot;
                break;

            case InGameEventFeedbackType.Humiliation:
                text.Text = "Smackdown";
                sound = SoundEffectType.UISmackdown;
                break;
        }
        ResetTransform(text);
        ResetStyle(text);

        LocalShotFeedbackAnim anim = new LocalShotFeedbackAnim(_textGroup, text, 1.0f, 1.0f, sound);
        InGameFeatHud.Instance.AnimationScheduler.EnqueueAnim(anim);
    }

    #region Private
    private LocalShotFeedbackHud() 
    {
        _textGroup = new Animatable2DGroup();
    }

    private void ResetTransform(MeshGUIText text)
    {
        float textScale = InGameFeatHud.Instance.TextHeight / text.TextBounds.y;
        text.Scale = new Vector2(textScale, textScale);
        text.Position = InGameFeatHud.Instance.AnchorPoint;
    }

    private void ResetStyle(MeshGUIText text)
    {
        HudStyleUtility.Instance.SetNoShadowStyle(text);
        text.Alpha = 0.0f;
        text.ShadowColorAnim.Alpha = 0.0f;
    }

    private Animatable2DGroup _textGroup;
    #endregion
}
