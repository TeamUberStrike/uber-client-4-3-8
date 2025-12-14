using System;
using UnityEngine;
using UberStrike.DataCenter.Common.Entities;

public class ExplosiveGrenadeQuickItem : BaseQuickItem, IGrenadeProjectile
{
    #region Fields

    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private ParticleSystem _smoke;

    [SerializeField]
    private ParticleSystem _deployedEffect;

    [SerializeField]
    private AudioClip _explosionSound;

    [SerializeField]
    private ExplosiveGrenadeConfiguration _config;

    private StateMachine machine = new StateMachine();

    private event Action<Collider> OnTriggerEnterEvent;
    private event Action<Collision> OnCollisionEnterEvent;

    public event Action<IGrenadeProjectile> OnProjectileExploded;

    #endregion

    #region Properties

    public ParticleSystem Smoke { get { return _smoke; } }

    public ParticleSystem DeployedEffect { get { return _deployedEffect; } }

    public Renderer Renderer { get { return _renderer; } }

    public override QuickItemConfiguration Configuration
    {
        get { return _config; }
        set { _config = (ExplosiveGrenadeConfiguration)value; }
    }

    #endregion

    protected override void OnActivated()
    {
        Vector3 origin = GameState.LocalCharacter.ShootingPoint + LocalPlayer.EyePosition;
        RaycastHit hit;

        Vector3 position = origin + GameState.LocalCharacter.ShootingDirection * 2;
        Vector3 velocity = GameState.LocalCharacter.ShootingDirection * _config.Speed;

        float minDistance = 2.0f;
        // can hit somewhere very close to us
        if (Physics.Raycast(origin, GameState.LocalCharacter.ShootingDirection * 2,
            out hit, minDistance, UberstrikeLayerMasks.LocalRocketMask))
        {
            var instance = Throw(hit.point, Vector3.zero) as ExplosiveGrenadeQuickItem;
            instance.machine.PopAllStates();
            instance.OnProjectileExploded += (p) =>
                {
                    ProjectileDetonator.Explode(p.Position, p.ID, _config.Damage, Vector3.up, _config.SplashRadius, 5, Configuration.ID, UberStrike.Core.Types.UberstrikeItemClass.WeaponLauncher);
                };

            ProjectileManager.Instance.RemoveProjectile(instance.ID, true);
            GameState.CurrentGame.RemoveProjectile(instance.ID, true);
        }
        else
        {
            var instance = Throw(position, velocity);
            instance.OnProjectileExploded += (p) =>
            {
                ProjectileDetonator.Explode(p.Position, p.ID, _config.Damage, Vector3.up, _config.SplashRadius, 5, Configuration.ID, UberStrike.Core.Types.UberstrikeItemClass.WeaponLauncher);
            };
        }
    }

    public IGrenadeProjectile Throw(Vector3 position, Vector3 velocity)
    {
        var instance = GameObject.Instantiate(this) as ExplosiveGrenadeQuickItem;
        instance.Position = position;
        instance.Velocity = velocity;
        instance.GetComponent<Collider>().material.bounciness = _config.Bounciness;

        instance.machine.RegisterState(1, new FlyingState(instance));
        instance.machine.RegisterState(2, new DeployedState(instance));

        instance.machine.PushState(1);

        if (OnProjectileEmitted != null)
            OnProjectileEmitted(instance);

        return instance;
    }

    public event Action<IGrenadeProjectile> OnProjectileEmitted;

    public void SetLayer(UberstrikeLayer layer)
    {
        LayerUtil.SetLayerRecursively(transform, layer);
    }

    private void Update()
    {
        machine.Update();
    }

    private void OnTriggerEnter(Collider c)
    {
        if (OnTriggerEnterEvent != null) OnTriggerEnterEvent(c);
    }

    private void OnCollisionEnter(Collision c)
    {
        if (OnCollisionEnterEvent != null) OnCollisionEnterEvent(c);
    }

    public Vector3 Explode()
    {
        Vector3 point = Vector3.zero;
        try
        {
            if (_explosionSound != null)
                SfxManager.Play3dAudioClip(_explosionSound, transform.position);

            ParticleEffectController.ShowExplosionEffect(ParticleConfigurationType.LauncherDefault, SurfaceEffectType.None, transform.position, Vector3.up);

            if (OnProjectileExploded != null)
            {
                OnProjectileExploded(this);
            }

            point = transform.position;

            Destroy();
        }
        catch
        {
            Debug.LogWarning("ExplosiveGrenade not exploded because it was already destroyed.");
        }

        return point;
    }

    public int ID { get; set; }

    private bool _isDestroyed;

    public void Destroy()
    {
        if (!_isDestroyed)
        {
            _isDestroyed = true;
            gameObject.SetActiveRecursively(false);
            GameObject.Destroy(gameObject);
        }
    }

    private class FlyingState : IState
    {
        ExplosiveGrenadeQuickItem behaviour;

        float _timeOut;

        public FlyingState(ExplosiveGrenadeQuickItem behaviour)
        {
            this.behaviour = behaviour;
        }

        public void OnEnter()
        {
            _timeOut = Time.time + behaviour._config.LifeTime;

            behaviour.OnCollisionEnterEvent += OnCollisionEnterEvent;
            if (!behaviour._config.IsSticky)
                behaviour.OnTriggerEnterEvent += OnTriggerEnterEvent;

            GameObject gameObject = behaviour.gameObject;
            if (gameObject)
            {
                //ignore self collision for local & remote player
                if (GameState.LocalDecorator && gameObject.GetComponent<Collider>())
                {
                    Collider c = gameObject.GetComponent<Collider>();

                    foreach (CharacterHitArea a in GameState.LocalDecorator.HitAreas)
                    {
                        if (gameObject.active && a.gameObject.active)
                            Physics.IgnoreCollision(c, a.GetComponent<Collider>());
                    }
                }
            }
        }

        public void OnExit()
        {
            behaviour.OnCollisionEnterEvent -= OnCollisionEnterEvent;
            if (!behaviour._config.IsSticky)
                behaviour.OnTriggerEnterEvent -= OnTriggerEnterEvent;
        }

        public void OnUpdate()
        {
            if (_timeOut < Time.time)
            {
                behaviour.machine.PopState();
                ProjectileManager.Instance.RemoveProjectile(behaviour.ID, true);
            }
        }

        public void OnGUI() { }

        private void OnCollisionEnterEvent(Collision c)
        {
            if (LayerUtil.IsLayerInMask(UberstrikeLayerMasks.GrenadeCollisionMask, c.gameObject.layer))
            {
                behaviour.machine.PopState();
                // here we catapult target UP with remote player knockback function
                ProjectileManager.Instance.RemoveProjectile(behaviour.ID, true);
                GameState.CurrentGame.RemoveProjectile(behaviour.ID, true);
            }
            else if (behaviour._config.IsSticky)
            {
                if (c.contacts.Length > 0)
                {
                    behaviour.transform.position = c.contacts[0].point + c.contacts[0].normal * behaviour.GetComponent<Collider>().bounds.extents.sqrMagnitude;
                }

                behaviour.machine.PopState();
                behaviour.machine.PushState(2);
            }

            PlayBounceSound(c.transform.position);
        }

        private void OnTriggerEnterEvent(Collider c)
        {
            if (LayerUtil.IsLayerInMask(UberstrikeLayerMasks.GrenadeCollisionMask, c.gameObject.layer))
            {
                behaviour.machine.PopState();
                // here we catapult target UP with remote player knockback function
                ProjectileManager.Instance.RemoveProjectile(behaviour.ID, true);
                GameState.CurrentGame.RemoveProjectile(behaviour.ID, true);
            }
        }

        protected void PlayBounceSound(Vector3 position)
        {
            SoundEffectType sound = SoundEffectType.WeaponLauncherBounce1;

            int r = UnityEngine.Random.Range(0, 2);

            if (r > 0) sound = SoundEffectType.WeaponLauncherBounce2;

            SfxManager.Play3dAudioClip(sound, position);
        }
    }

    private class DeployedState : IState
    {
        ExplosiveGrenadeQuickItem behaviour;

        float _timeOut;

        public DeployedState(ExplosiveGrenadeQuickItem behaviour)
        {
            this.behaviour = behaviour;
        }

        public void OnEnter()
        {
            _timeOut = Time.time + behaviour._config.LifeTime;

            behaviour.OnTriggerEnterEvent += OnTriggerEnterEvent;

            if (behaviour.GetComponent<Rigidbody>()) behaviour.GetComponent<Rigidbody>().isKinematic = true;
            if (behaviour.GetComponent<Collider>()) GameObject.Destroy(behaviour.GetComponent<Collider>());
            behaviour.gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;

            if (behaviour.DeployedEffect)
            {
                behaviour.DeployedEffect.emit = true;
            }
        }

        public void OnExit()
        {
            behaviour.OnTriggerEnterEvent -= OnTriggerEnterEvent;
        }

        private void OnTriggerEnterEvent(Collider c)
        {
            if (LayerUtil.IsLayerInMask(UberstrikeLayerMasks.GrenadeCollisionMask, c.gameObject.layer))
            {
                behaviour.machine.PopState();
                // here we catapult target UP with remote player knockback function
                ProjectileManager.Instance.RemoveProjectile(behaviour.ID, true);
                GameState.CurrentGame.RemoveProjectile(behaviour.ID, true);
            }
        }

        public void OnUpdate()
        {
            if (_timeOut < Time.time)
            {
                behaviour.machine.PopState();
                ProjectileManager.Instance.RemoveProjectile(behaviour.ID, true);
            }
        }

        public void OnGUI() { }
    }

    public Vector3 Position
    {
        get { return transform ? transform.position : Vector3.zero; }
        private set { if (transform)transform.position = value; }
    }

    public Vector3 Velocity
    {
        get { return GetComponent<Rigidbody>() ? GetComponent<Rigidbody>().linearVelocity : Vector3.zero; }
        private set { if (GetComponent<Rigidbody>()) GetComponent<Rigidbody>().linearVelocity = value; }
    }
}