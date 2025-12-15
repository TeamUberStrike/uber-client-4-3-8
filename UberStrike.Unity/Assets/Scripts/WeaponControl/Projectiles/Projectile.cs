using System.Collections;
using Cmune.Util;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public abstract class Projectile : MonoBehaviour, IProjectile
{
    [SerializeField]
    private bool _haveTimeOut = true;
    [SerializeField]
    private Collider _trigger;
    [SerializeField]
    private Collider _collider;
    [SerializeField]
    private bool _showHeatwave;
    [SerializeField]
    private GameObject _explosionEffect;

    #region Fields

    private Rigidbody _rigidbody;
    protected AudioSource _source;
    private float _positionSign = 0;
    private Transform _transform;
    protected AudioClip _explosionSound;

    #endregion

    #region Properties

    public ParticleConfigurationType ExplosionEffect { get; set; }

    public Rigidbody Rigidbody { get { return _rigidbody; } }

    public ProjectileDetonator Detonator { get; set; }

    public bool IsProjectileExploded { get; protected set; }

    public float TimeOut { get; set; }

    public int ID { get; set; }

    #endregion

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _source = GetComponent<AudioSource>();

        TimeOut = 5;

        CmuneDebug.Assert(_collider != null || _trigger != null, "The Projectile " + gameObject.name + " has not assigned Collider or Trigger! Check your Inspector settings.");

        if (_collider && _collider.isTrigger) CmuneDebug.LogError("The Projectile " + gameObject.name + " has a Collider attached that is configured as Trigger! Check your Inspector settings.");
        if (_trigger && !_trigger.isTrigger) CmuneDebug.LogError("The Projectile " + gameObject.name + " has a Trigger attached that is configured as Collider! Check your Inspector settings.");

        _transform = transform;
        _positionSign = Mathf.Sign(_transform.position.y);
    }

    protected virtual void Start()
    {
        if (GameState.HasCurrentSpace && GameState.CurrentSpace.HasWaterPlane)
        {
            _positionSign = Mathf.Sign(_transform.position.y - GameState.CurrentSpace.WaterPlaneHeight);
        }

        if (_haveTimeOut)
        {
            StartCoroutine(StartTimeout());
        }
    }

    public void MoveInDirection(Vector3 direction)
    {
        Rigidbody.isKinematic = false;
        Rigidbody.linearVelocity = direction;
    }

    protected virtual IEnumerator StartTimeout()
    {
        yield return new WaitForSeconds(TimeOut);

        ProjectileManager.Instance.RemoveProjectile(ID, true);
    }

    protected abstract void OnTriggerEnter(Collider c);

    protected abstract void OnCollisionEnter(Collision c);

    protected virtual void Update()
    {
        if (GameState.HasCurrentSpace && GameState.CurrentSpace.HasWaterPlane)
        {
            if (_positionSign != Mathf.Sign(_transform.position.y - GameState.CurrentSpace.WaterPlaneHeight))
            {
                _positionSign = Mathf.Sign(_transform.position.y - GameState.CurrentSpace.WaterPlaneHeight);
                ParticleEffectController.ProjectileWaterRipplesEffect(ExplosionEffect, _transform.position);
            }
        }
    }

    protected void Explode(Vector3 point, Vector3 normal, string tag)
    {
        Destroy();

        if (Detonator != null)
        {
            Detonator.Explode(point);
        }

        ExplosionManager.Instance.PlayExplosionSound(point, _explosionSound);
        ExplosionManager.Instance.ShowExplosionEffect(point, normal, tag, ExplosionEffect);

        if (_showHeatwave)
            ParticleEffectController.ShowHeatwaveEffect(transform.position);

        if (_explosionEffect)
        {
            GameObject.Instantiate(_explosionEffect, point, Quaternion.LookRotation(normal));
        }
    }

    public void Destroy()
    {
        if (!IsProjectileExploded)
        {
            IsProjectileExploded = true;
            gameObject.SetActiveRecursively(false);
            GameObject.Destroy(gameObject);
        }
    }

    protected int CollisionMask
    {
        get
        {
            if (gameObject.layer == (int)UberstrikeLayer.RemoteProjectile)
                return UberstrikeLayerMasks.RemoteRocketMask;
            else
                return UberstrikeLayerMasks.LocalRocketMask;
        }
    }

    public void SetExplosionSound(AudioClip clip)
    {
        _explosionSound = clip;
    }

    protected void PlayBounceSound(Vector3 position)
    {
        SoundEffectType sound = SoundEffectType.WeaponLauncherBounce1;

        int r = Random.Range(0, 2);

        if (r > 0) sound = SoundEffectType.WeaponLauncherBounce2;

        SfxManager.Play3dAudioClip(sound, position);
    }

    public Vector3 Explode()
    {
        Vector3 pos = Vector3.zero;

        try
        {
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(transform.position - transform.forward, transform.forward, out hit, 2, CollisionMask))
            {
                pos = hit.point - transform.forward * 0.01f;

                Explode(pos, hit.normal, TagUtil.GetTag(hit.collider));
            }
            else
            {
                pos = transform.position;
                Explode(pos, -transform.forward, string.Empty);
            }
        }
        catch
        {
            Debug.LogWarning("Grenade not exploded because it was already destroyed.");
        }

        return pos;
    }
}