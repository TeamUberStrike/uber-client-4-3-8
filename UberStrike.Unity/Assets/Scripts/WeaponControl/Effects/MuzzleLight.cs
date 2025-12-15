using UnityEngine;
using System.Collections;

public class MuzzleLight : BaseWeaponEffect
{
    private Animation _shootAnimation;

    private void Awake()
    {
        _shootAnimation = GetComponent<Animation>();

        if (GetComponent<Light>()) GetComponent<Light>().intensity = 0;
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
    }
}