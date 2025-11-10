using System.Collections;

using UnityEngine;

public class RocketProjectile : Projectile
{
    #region Fields

    [SerializeField]
    private ParticleRenderer _smokeRenderer;

    [SerializeField]
    private ParticleEmitter _smokeEmitter;

    [SerializeField]
    private Color _smokeColor = Color.white;

    [SerializeField]
    private float _smokeAmount = 1;

    [SerializeField]
    private Light _light;

    #endregion

    #region Properties

    public Color SmokeColor
    {
        get { return _smokeColor; }
        set
        {
            _smokeColor = value;

            if (_smokeRenderer)
            {
                _smokeRenderer.material.SetColor("_TintColor", _smokeColor);
            }
        }
    }

    public float SmokeAmount
    {
        get { return _smokeAmount; }
        set
        {
            _smokeAmount = value;

            if (_smokeEmitter)
            {
                _smokeEmitter.minEmission = _smokeAmount * 10;
                _smokeEmitter.maxEmission = _smokeAmount * 20;
            }
        }
    }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        SmokeColor = _smokeColor;
        SmokeAmount = _smokeAmount;

        if (_light != null)
        {
            _light.enabled = ApplicationDataManager.ApplicationOptions.VideoQualityLevel == 2;
        }
    }

    protected override void OnTriggerEnter(Collider c)
    {
        if (!IsProjectileExploded)
        {
            if (LayerUtil.IsLayerInMask(CollisionMask, c.gameObject.layer))
            {
                ProjectileManager.Instance.RemoveProjectile(ID, true);
                GameState.CurrentGame.RemoveProjectile(ID, true);
            }
        }
    }

    protected override void OnCollisionEnter(Collision c)
    {
        if (!IsProjectileExploded)
        {
            if (LayerUtil.IsLayerInMask(CollisionMask, c.gameObject.layer))
            {
                ProjectileManager.Instance.RemoveProjectile(ID, true);
                GameState.CurrentGame.RemoveProjectile(ID, true);
            }
        }
    }
}