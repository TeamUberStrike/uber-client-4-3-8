using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BulletTrail : BaseWeaponEffect
{
    private Animation _animation;
    private AnimationState _clip;
    private float _trailDuration = 0.1f;

    private Renderer[] _renderers = new Renderer[0];

    private void Awake()
    {
        _animation = GetComponentInChildren<Animation>();

        if (_animation)
        {
            _clip = _animation[_animation.clip.name];

            _clip.wrapMode = WrapMode.Once;
            _trailDuration = _clip.length;

            _animation.playAutomatically = false;
        }

        _renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in _renderers)
            r.enabled = false;
    }

    public override void OnShoot()
    {
        foreach (Renderer r in _renderers)
            r.enabled = true;

        if (_animation)
        {
            float factor = _trailDuration / _clip.length;

            _clip.speed = factor;

            _animation.Play();
        }

        StartCoroutine(StartTrailEffect(_trailDuration));
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
    }

    private IEnumerator StartTrailEffect(float time)
    {
        yield return new WaitForSeconds(time);

        foreach (Renderer r in _renderers)
            r.enabled = false;
    }
}
