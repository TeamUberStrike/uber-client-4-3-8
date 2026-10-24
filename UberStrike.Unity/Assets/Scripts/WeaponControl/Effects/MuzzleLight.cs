using UnityEngine;
using System.Collections;

public class MuzzleLight : BaseWeaponEffect
{
    private Animation _shootAnimation;
    private Light _light;

    private void Awake()
    {
        _shootAnimation = GetComponent<Animation>();
        _light = GetComponent<Light>();

        if (_light) _light.intensity = 0;
    }

    public override void OnShoot()
    {
        if (_shootAnimation)
        {
            _shootAnimation.Play(PlayMode.StopSameLayer);
        }
    }

    public override void OnPostShoot() { }

    public override void Hide()
    {
        if (_shootAnimation)
        {
            _shootAnimation.Stop();
        }

        // stop keeps last key. switch mid flash = light stuck on
        if (_light) _light.intensity = 0;
    }
}