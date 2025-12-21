using UnityEngine;
using System.Collections;

public class MuzzleSmoke : BaseWeaponEffect
{
    private ParticleSystem _particleEmitter;

    private void Awake()
    {
        _particleEmitter = GetComponentInChildren<ParticleSystem>();
    }

    public override void OnShoot()
    {
        if (_particleEmitter)
        {
            gameObject.SetActive(true);

            _particleEmitter.Emit(1);
        }
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
    }
}