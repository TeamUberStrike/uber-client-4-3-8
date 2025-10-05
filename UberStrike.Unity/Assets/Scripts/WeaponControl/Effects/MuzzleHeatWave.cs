using Cmune.Util;
using UnityEngine;

public class MuzzleHeatWave : BaseWeaponEffect
{
    [SerializeField]
    private float _startSize = 0;
    [SerializeField]
    private float _maxSize = 0.05f;
    [SerializeField]
    private float _duration = 0.25f;
    [SerializeField]
    private float _distortion = 64;


    private Transform _transform;
    private Renderer _renderer;
    private float _elapsedTime;
    private float _normalizedTime;
    private float _s;
    

    private void Awake()
    {
        _transform = transform;
        _renderer = renderer;

        CmuneDebug.Assert(_renderer, "No Renderer attached to HeatWave script on GameObject " + gameObject.name);
    }

    private void Start()
    {
        _renderer.enabled = false;
        this.enabled = false;
    }

    private void Update()
    {
        if (_transform && _renderer)
        {
            _elapsedTime = _elapsedTime + Time.deltaTime;
            _normalizedTime = _elapsedTime / _duration;

            //thought about this, and really, the wave would move linearly, fading in amplitude. 
            _s = Mathf.Lerp(_startSize, _maxSize, _normalizedTime);

            _renderer.material.SetFloat("_BumpAmt", ((1 - _normalizedTime) * _distortion));

            _transform.localScale = new Vector3(_s, _s, _s);

            // always follow player camera
            //_transform.rotation = Quaternion.FromToRotation(Vector3.up, Camera.main.transform.position - _transform.position);
            _transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - _transform.position);

            if (_elapsedTime > _duration)
            {
                _transform.localScale = new Vector3(0, 0, 0);
                _renderer.enabled = false;
                this.enabled = false;
            }
        }
    }

    public override void OnShoot()
    {
        if (SystemInfo.supportsImageEffects)
        {
            _elapsedTime = 0f;
            _transform.rotation = Quaternion.FromToRotation(Vector3.up, Camera.main.transform.position - _transform.position);

            _renderer.enabled = true;
            this.enabled = true;
        }
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
        if (!_renderer)
            _renderer = renderer;

        if (_renderer)
            _renderer.enabled = false;
    }
}