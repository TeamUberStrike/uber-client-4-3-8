using System.Collections;
using UnityEngine;

public class EnergyProjectile : Projectile
{
    #region Fields

    [SerializeField]
    private MeshRenderer _trailRenderer;

    [SerializeField]
    private MeshRenderer _headRenderer;

    [SerializeField]
    private Light _light;

    [SerializeField]
    private Color _energyColor = Color.white;

    [SerializeField]
    private float _afterGlowDuration = 2;

    #endregion

    #region Properties

    public Color EnergyColor
    {
        get { return _energyColor; }
        set
        {
            _energyColor = value;

            _headRenderer.material.SetColor("_TintColor", _energyColor);
            _trailRenderer.material.SetColor("_TintColor", _energyColor);
        }
    }

    public float AfterGlowDuration
    {
        get { return _afterGlowDuration; }
        set { _afterGlowDuration = value; }
    }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        if (_light != null)
            _light.enabled = ApplicationDataManager.ApplicationOptions.VideoQualityLevel == 2;
    }

    protected override void OnTriggerEnter(Collider c)
    {
        if (!IsProjectileExploded)
        {
            if (LayerUtil.IsLayerInMask(CollisionMask, c.gameObject.layer))
            {
                Explode();
            }
        }
    }

    protected override void OnCollisionEnter(Collision c)
    {
        if (!IsProjectileExploded)
        {
            if (LayerUtil.IsLayerInMask(CollisionMask, c.gameObject.layer))
            {
                if (c.contacts.Length > 0)
                    Explode(c.contacts[0].point, c.contacts[0].normal, TagUtil.GetTag(c.collider));
                else
                    Explode();
            }
        }
    }

    //protected override void Explode(Vector3 point, Vector3 normal, string tag)
    //{
    //    base.Explode(point, normal, tag);

    //    if (_headRenderer)
    //    {
    //        _headRenderer.transform.position = point;
    //    }

    //    StartCoroutine(StartAfterGlow(_afterGlowDuration));
    //}

    //private IEnumerator StartAfterGlow(float time)
    //{
    //    Destroy(gameObject, time);

    //    if (_trailRenderer && _headRenderer && _light)
    //    {
    //        _trailRenderer.enabled = false;
    //        Color c = _headRenderer.material.GetColor("_TintColor");
    //        float r = _light.range;
    //        float maxTime = time;
    //        while (time > 0)
    //        {
    //            c.a = time / maxTime;
    //            _light.range = r * time / maxTime;
    //            _headRenderer.material.SetColor("_TintColor", c);

    //            yield return new WaitForEndOfFrame();
    //            time -= Time.deltaTime;
    //        }
    //    }
    //}
}
