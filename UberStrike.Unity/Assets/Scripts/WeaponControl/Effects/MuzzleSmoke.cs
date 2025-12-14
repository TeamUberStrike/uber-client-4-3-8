using UnityEngine;
using System.Collections;

public class MuzzleSmoke : BaseWeaponEffect
{
    private ParticleEmitter _particleEmitter;

    private void Awake()
    {
        _particleEmitter = GetComponentInChildren<ParticleEmitter>();
    }

    public override void OnShoot()
    {
        if (_particleEmitter)
        {
            gameObject.active = true;

            _particleEmitter.Emit();
        }
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
    }
}