using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(AudioSource))]
public class TutorialShootingTarget : BaseGameProp
{
    [SerializeField]
    private GameObject Body;

    [SerializeField]
    private PlayerDamageEffect _damageEffect;

    private Vector3 _initialPos = new Vector3(0, -1.72f, 0);

    private bool _isHit = false;

    private Dictionary<Rigidbody, Vector3> _bodyPos;

    public bool IsHit { get { return _isHit; } }

    public bool UseTimer { get; set; }
    public bool IsMoving { get; set; }
    public float MaxTime { get; set; }
    public Vector3 Direction { get; set; }

    public Action OnHitCallback;

    public override void ApplyDamage(DamageInfo shot)
    {
        _isHit = true;
        gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;

        if (OnHitCallback != null) OnHitCallback();

        base.ApplyDamage(shot);

        foreach (Rigidbody r in _bodyPos.Keys)
        {
            r.isKinematic = false;
            r.AddExplosionForce(1000, shot.Hitpoint, 5f);
        }

        if (_damageEffect)
        {
            PlayerDamageEffect effect = GameObject.Instantiate(_damageEffect, shot.Hitpoint, Quaternion.LookRotation(shot.Force)) as PlayerDamageEffect;
            if (effect)
            {
                effect.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                effect.Show(shot);
            }
        }
        else
        {
            Debug.LogWarning("No damage effect is attached!");
        }

        SfxManager.Play2dAudioClip(SoundEffectType.PropsTargetDamage);
    }

    public void Reset()
    {
        _isHit = false;

        Body.transform.localPosition = _initialPos;

        foreach (Rigidbody r in _bodyPos.Keys)
        {
            r.isKinematic = true;
            r.transform.localPosition = _bodyPos[r];
            r.transform.localRotation = Quaternion.identity;
        }

        StartCoroutine(StartPopup());
    }

    private void Awake()
    {
        Body.transform.localPosition = _initialPos;

        gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;

        _bodyPos = new Dictionary<Rigidbody, Vector3>();

        foreach (Transform t in Body.GetComponentsInChildren<Transform>(true))
        {
            if (t == Body.transform) continue;

            MeshCollider m = t.gameObject.AddComponent<MeshCollider>();
            if (m)
            {
                m.convex = true;
            }

            Rigidbody r = t.gameObject.AddComponent<Rigidbody>();
            if (r)
            {
                r.isKinematic = true;
                r.gameObject.layer = (int)UberstrikeLayer.RemotePlayer;
                _bodyPos.Add(r, r.transform.localPosition);
            }
        }
    }

    private IEnumerator StartPopup()
    {
        Transform t = Body.transform;

        SfxManager.Play2dAudioClip(SoundEffectType.PropsTargetPopup);

        while (Vector3.Distance(t.localPosition, Vector3.zero) > 0.1f)
        {
            t.localPosition = Vector3.Lerp(t.localPosition, Vector3.zero, Time.deltaTime * 3);

            yield return new WaitForEndOfFrame();
        }

        gameObject.layer = (int)UberstrikeLayer.RemotePlayer;
    }

    private IEnumerator StartSelfDestroy()
    {
        yield return new WaitForSeconds(2);

        if (_transform)
            Destroy(_transform.gameObject);
    }
}
