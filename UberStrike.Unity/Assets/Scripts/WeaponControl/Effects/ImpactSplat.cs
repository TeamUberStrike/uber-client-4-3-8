using UnityEngine;
using System.Collections;

public class ImpactSplat : BaseWeaponEffect
{
    //private void Awake()
    //{
    //    _animation = GetComponentInChildren<Animation>();

    //    if (_animation)
    //    {
    //        _clip = _animation[_animation.clip.name];

    //        _clip.wrapMode = WrapMode.Once;

    //        if (_splatDuration == 0)
    //            _splatDuration = _clip.length;
    //    }

    //    //gameObject.SetActiveRecursively(false);
    //}

    public override void OnShoot()
    {
        //    gameObject.SetActiveRecursively(true);
        //    transform.localRotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);

        //    if (_animation)
        //    {
        //        float factor = _clip.length / _splatDuration;

        //        _clip.speed = factor;

        //        _clip.time = 0;

        //        _animation.Play();
        //    }

        //    StartCoroutine(StartSplatEffect(_splatDuration));
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
    }

    //private IEnumerator StartSplatEffect(float time)
    //{
    //    float lightRange = 0;
    //    if (_light) lightRange = _light.range;

    //    float maxTime = time;
    //    while (time > 0)
    //    {
    //        if (_light) _light.range = lightRange * time / maxTime;
    //        yield return new WaitForEndOfFrame();
    //        time -= Time.deltaTime;
    //    }

    //    if (maxTime < _lifeTime)
    //    {
    //        yield return new WaitForSeconds(_lifeTime - maxTime);
    //    }

    //    //Destroy(gameObject);
    //    gameObject.SetActiveRecursively(false);
    //}

    //#region INSPECTOR
    //[SerializeField]
    //private float _splatDuration;
    //[SerializeField]
    //private Light _light;
    //#endregion

    //#region FIELDS
    //private Animation _animation;
    //private AnimationState _clip;
    //private float _lifeTime;
    //#endregion
}
