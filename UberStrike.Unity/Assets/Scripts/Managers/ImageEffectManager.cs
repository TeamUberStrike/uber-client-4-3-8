using System.Collections.Generic;
using UnityEngine;

public class ImageEffectManager
{
    public enum ImageEffectType
    {
        None,
        ColorCorrectionCurves,
        BloomAndLensFlares,
        MotionBlur
    }

    public void ApplyMotionBlur(float time)
    {
        if (_effects.ContainsKey(ImageEffectType.MotionBlur))
        {
            EnableEffect(ImageEffectType.MotionBlur, time);
        }
    }

    public void ApplyMotionBlur(float time, float intensity)
    {
        if (_effects.ContainsKey(ImageEffectType.MotionBlur))
        {
            EnableEffect(ImageEffectType.MotionBlur, time, intensity);
        }
    }

    public void ApplyWhiteout(float time)
    {
        if (_effects.ContainsKey(ImageEffectType.BloomAndLensFlares))
        {
            EnableEffect(ImageEffectType.BloomAndLensFlares, time);
        }
    }

    public void AddEffect(ImageEffectType imageEffectType, MonoBehaviour monoBehaviour)
    {
        _effects[imageEffectType] = monoBehaviour;
        _effectsParameters[imageEffectType] = new ImageEffectParameters();
    }

    public void Clear()
    {
        _effects.Clear();
    }

    public void Update()
    {
        if (ApplicationDataManager.Exists && ApplicationDataManager.ApplicationOptions.VideoMotionBlur)
        {
            ImageEffectParameters parameter;
            if (_effectsParameters.TryGetValue(ImageEffectType.MotionBlur, out parameter) && parameter != null && parameter.EffectEnable)
            {
                if (parameter.ActiveTime > 0)
                {
                    parameter.ChangeActiveTime(-Time.deltaTime);
                    if (parameter.ActiveTime < 0)
                    {
                        parameter.SetTimedEnable(false);
                    }
                }
                if (parameter.PermanentEnable)
                {
                    ((MotionBlur)_effects[ImageEffectType.MotionBlur]).blurAmount = _motionBlurMaxValue;
                }
                else if (parameter.TimedEnable)
                {
                    float intensity = _effectsParameters[ImageEffectType.MotionBlur].BaseIntencity;
                    intensity = (intensity > 0) ? intensity : _motionBlurMaxValue;
                    ((MotionBlur)_effects[ImageEffectType.MotionBlur]).blurAmount = (_effectsParameters[ImageEffectType.MotionBlur].ActiveTime / _effectsParameters[ImageEffectType.MotionBlur].TotalTime) * intensity;
                }
            }
        }
        else if (_effects.ContainsKey(ImageEffectType.MotionBlur))
        {
            // If we have motion blur, but it's disabled in the options panel, always disable the component
            _effects[ImageEffectType.MotionBlur].enabled = false;
        }

        if (ApplicationDataManager.Exists)
        {
            if (_effects.ContainsKey(ImageEffectType.ColorCorrectionCurves))
                _effects[ImageEffectType.ColorCorrectionCurves].enabled = ApplicationDataManager.ApplicationOptions.VideoVignetting;
            if (_effects.ContainsKey(ImageEffectType.BloomAndLensFlares))
                _effects[ImageEffectType.BloomAndLensFlares].enabled = ApplicationDataManager.ApplicationOptions.VideoBloomAndFlares;

            //if (ApplicationDataManager.Settings.VideoHardcoreMode && QualitySettings.masterTextureLimit != 9)
            //{
            //    QualitySettings.masterTextureLimit = 9;
            //}
            //else if (!ApplicationDataManager.Settings.VideoHardcoreMode && QualitySettings.masterTextureLimit != 0)
            //{
            //    QualitySettings.masterTextureLimit = 0;
            //}
        }
    }

    public void EnableEffect(ImageEffectType imageEffectType)
    {
        EnableEffect(imageEffectType, -1f, -1f);
    }

    public void EnableEffect(ImageEffectType imageEffectType, float time)
    {
        EnableEffect(imageEffectType, time, -1f);
    }

    /// <summary>
    /// Enables given effect with time limitation
    /// </summary>
    /// <param name="imageEffectType">Effect to enable</param>
    /// <param name="duration">Effect length in seconds (if want no time limit use -1 or just ignore this parameter)</param>
    public void EnableEffect(ImageEffectType imageEffectType, float duration, float intensity)
    {
        if (_effects.ContainsKey(imageEffectType) && _effectsParameters.ContainsKey(imageEffectType))
        {
            _effects[imageEffectType].enabled = true;
            if (imageEffectType == ImageEffectType.BloomAndLensFlares)
            {
#if UNITY_3_5 && !UNITY_IPHONE
                _effectsParameters[imageEffectType].SetBaseIntensity(((BloomAndLensFlares)_effects[ImageEffectType.BloomAndLensFlares]).bloomIntensity);
#endif
            }
            // set the intensity if greater than zero
            if (intensity > 0)
            {
                _effectsParameters[imageEffectType].SetBaseIntensity(intensity);
            }

            // set timed if we have any time
            if (duration > 0)
            {
                _effectsParameters[imageEffectType].SetTotalAndActiveTime(duration);
                _effectsParameters[imageEffectType].SetTimedEnable(true);
            }
            else // if not, then set permanent
            {
                _effectsParameters[imageEffectType].SetPermanentEnable(true);
            }
            _currentEffect = imageEffectType;
        }
        else
        {
            Debug.LogError("You're trying to enable an effect that hasn't been initialized. Check the components on MainCamera in the level.");
        }
    }

    /// <summary>
    /// Disable Permanent part of efect
    /// </summary>
    /// <param name="imageEffectType"></param>
    public void DisableEffect(ImageEffectType imageEffectType)
    {
        if (_effects.ContainsKey(imageEffectType) && _effectsParameters.ContainsKey(imageEffectType))
        {
            _effects[imageEffectType].enabled = false;
            _effectsParameters[imageEffectType].SetPermanentEnable(false);
            _currentEffect = ImageEffectType.None;
        }
    }

    /// <summary>
    /// Disable effect completelly (Permanent and timed)
    /// </summary>
    /// <param name="imageEffectType"></param>
    public void DisableEffectInstant(ImageEffectType imageEffectType)
    {
        if (_effects.ContainsKey(imageEffectType) && _effectsParameters.ContainsKey(imageEffectType))
        {
            _effects[imageEffectType].enabled = false;
            _effectsParameters[imageEffectType].SetPermanentEnable(false);
            _effectsParameters[imageEffectType].SetTimedEnable(false);
            _currentEffect = ImageEffectType.None;
        }
    }

    public void DisableAllEffects()
    {
        foreach (ImageEffectType effect in _effectsParameters.Keys)
        {
            DisableEffectInstant(effect);
        }
    }

    #region Properties
    public ImageEffectType CurrentEffect
    {
        get { return _currentEffect; }
    }
    #endregion

    #region Fields
    private ImageEffectType _currentEffect = ImageEffectType.None;
    private Dictionary<ImageEffectType, MonoBehaviour> _effects = new Dictionary<ImageEffectType, MonoBehaviour>();
    private Dictionary<ImageEffectType, ImageEffectParameters> _effectsParameters = new Dictionary<ImageEffectType, ImageEffectParameters>();
    private const float _motionBlurMaxValue = 0.5f;
    #endregion

    private class ImageEffectParameters
    {
        #region Functions

        /// <summary>
        /// Sets effect enable permanent state
        /// </summary>
        /// <param name="value"></param>
        public void SetPermanentEnable(bool value)
        {
            _permanentEnable = value;
        }

        /// <summary>
        /// Sets effect enable permanent state
        /// </summary>
        /// <param name="value"></param>
        public void SetTimedEnable(bool value)
        {
            _timedEnable = value;
        }

        /// <summary>
        /// Set Base Intencity of effect
        /// </summary>
        /// <param name="value"></param>
        public void SetBaseIntensity(float value)
        {
            _baseIntencity = value;
        }

        /// <summary>
        /// Set Total time of effect
        /// </summary>
        /// <param name="value"></param>
        public void SetTotalTime(float value)
        {
            _totalTime = value;
        }

        /// <summary>
        /// Set Active time of effect
        /// </summary>
        /// <param name="value"></param>
        public void SetActiveTime(float value)
        {
            _activeTime = value;
        }

        /// <summary>
        /// Set both total and active time (in case of same value) with one function
        /// </summary>
        /// <param name="time"></param>
        public void SetTotalAndActiveTime(float time)
        {
            SetActiveTime(time);
            SetTotalTime(time);
        }

        /// <summary>
        /// Change Active time base on difference
        /// </summary>
        /// <param name="change"></param>
        public void ChangeActiveTime(float change)
        {
            _activeTime = _activeTime + change;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Is this effect enabled with no time delay
        /// </summary>
        public bool PermanentEnable
        {
            get { return _permanentEnable; }
        }

        /// <summary>
        /// Is this effect enabled with time delay
        /// </summary>
        public bool TimedEnable
        {
            get { return _timedEnable; }
        }

        /// <summary>
        /// Is this effect enabled
        /// </summary>
        public bool EffectEnable
        {
            get { return _permanentEnable || _timedEnable; }
        }

        /// <summary>
        /// Base intencity of effect
        /// </summary>
        public float BaseIntencity
        {
            get { return _baseIntencity; }
        }

        /// <summary>
        /// Total time of effect
        /// </summary>
        public float TotalTime
        {
            get { return _totalTime; }
        }

        /// <summary>
        /// Active time of effect
        /// </summary>
        public float ActiveTime
        {
            get { return _activeTime; }
        }

        #endregion

        #region Fields

        private bool _permanentEnable = false;

        private bool _timedEnable = false;

        private float _baseIntencity = 0f;

        private float _totalTime = 0f;

        private float _activeTime = 0f;

        #endregion
    }
}