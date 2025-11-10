using UnityEngine;
using System.Collections.Generic;

public class MuzzleFlash : BaseWeaponEffect
{
    private float _muzzleFlashEnd = 0;
    private Animation _animation;
    private AnimationState _clip;
    private float _flashDuration = 0.1f;

    private List<Renderer> _renderers = new List<Renderer>();

    void Awake()
    {
        _animation = GetComponentInChildren<Animation>();

        if (_animation)
        {
            _clip = _animation[_animation.clip.name];

            _clip.wrapMode = WrapMode.Once;
            _flashDuration = _clip.length;

            _animation.playAutomatically = false;
        }

        _muzzleFlashEnd = 0;

        _renderers.AddRange(GetComponentsInChildren<Renderer>());
        foreach (Renderer r in _renderers)
            r.enabled = false;
    }

    public override void Hide()
    {
        _muzzleFlashEnd = 0;

        if (_clip)
            _clip.normalizedTime = 1;

        foreach (Renderer r in _renderers)
            r.enabled = false;
    }

    public override void OnShoot()
    {
        foreach (Renderer r in _renderers)
            r.enabled = true;

        //gameObject.SetActiveRecursively(true);
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        if (_animation)
        {
            float factor = _clip.length / _flashDuration;

            _clip.speed = factor;

            _clip.time = 0;

            _animation.Play();
        }

        _muzzleFlashEnd = Time.time + _flashDuration;
    }

    public override void OnPostShoot()
    {
    }

    private void Update()
    {
        if (_muzzleFlashEnd < Time.time)
        {
            foreach (Renderer r in _renderers)
                r.enabled = false;
        }
    }
}
