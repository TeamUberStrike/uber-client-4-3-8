using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the spin of weapon head.
/// Alex - 2011 Nov 29
/// </summary>
[RequireComponent(typeof(Animation))]
public class WeaponHeadAnimation : BaseWeaponEffect
{
    private Animation _animation;
    private AnimationState _animState;
    private float _speed = 0;

    private void Awake()
    {
        _animation = GetComponent<Animation>();
        if (_animation && _animation.clip)
        {
            _animation.playAutomatically = false;
            _animState = _animation[_animation.clip.name];
        }
        else
        {
            Debug.LogError("Failed to get weapon head sound!");
        }
    }

    private void Update()
    {
        if (_speed > 0)
        {
            if (_animState)
                _animState.speed = _speed;

            _speed = Mathf.Lerp(_speed, -0.1f, Time.deltaTime);
        }
        else if (_animation.isPlaying)
        {
            _animation.Stop();
        }
    }

    public override void OnShoot()
    {
        if (_animation)
        {
            _speed = 1;
            _animation.Play();
        }
        else
        {
            Debug.LogError("No animation for weapon head!");
        }
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
        if (_animation && _animation.isPlaying)
            _animation.Stop();
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }
}