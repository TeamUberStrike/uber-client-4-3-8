//using UnityEngine;
//using System.Collections;
//using System;

//public class EffectManager : MonoSingleton<EffectManager>
//{
//    public TwirlEffect twirlEffect;
//    public GlowEffect glowEffect;
//    public NoiseEffect noiseEffect;
//    private float _glowLevel = 1;

//    void Awake()
//    {
//        Instance = this;
//    }

//    public void SetGlowLevel(float level)
//    {
//        _glowLevel = level;
//        glowEffect.glowIntensity = _glowLevel;
//    }

//    public void StartFlashEffect(float start, float time)
//    {
//        StartCoroutine(startFlashEffect(start, time));
//    }

//    private IEnumerator startFlashEffect(float start, float time)
//    {
//        float t = 0;
//        while (t < time)
//        {
//            glowEffect.glowIntensity = Mathf.Lerp(start, _glowLevel, t / time);
//            t += Time.deltaTime;
//            yield return new WaitForEndOfFrame();
//        }

//        glowEffect.glowIntensity = _glowLevel;
//    }

//    public void StartTwirlEffect(float start, float time)
//    {
//        StartCoroutine(startTwirlEffect(start, time));
//    }

//    private IEnumerator startTwirlEffect(float start, float time)
//    {
//        twirlEffect.enabled = true;

//        float t = 0;
//        while (t < time)
//        {
//            twirlEffect.angle = Mathf.Lerp(start, 0, t / time);
//            t += Time.deltaTime;
//            yield return new WaitForEndOfFrame();
//        }

//        twirlEffect.angle = 0;
//        twirlEffect.enabled = false;
//    }

//    public void StartNoiseEffect(float delay)
//    {
//        StartCoroutine(startNoiseEffect(delay));
//    }

//    public void StopNoiseEffect()
//    {
//        noiseEffect.enabled = false;
//    }

//    private IEnumerator startNoiseEffect(float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        noiseEffect.enabled = true;
//    }
//}
