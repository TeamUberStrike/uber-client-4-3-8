using System;
using UnityEngine;
using UberStrike.DataCenter.Common.Entities;
using System.Collections;

public class SpringGrenadeQuickItem : BaseQuickItem, IGrenadeProjectile
{
    #region Fields

    [SerializeField]
    private AudioClip _sound;

    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private ParticleSystem _smoke;

    [SerializeField]
    private ParticleSystem _deployedEffect;

    [SerializeField]
    private SpringGrenadeConfiguration _config;

    private StateMachine machine = new StateMachine();

    private event Action<Collider> OnTriggerEnterEvent;
    private event Action<Collision> OnCollisionEnterEvent;

    public event Action<IGrenadeProjectile> OnProjectileExploded;

    private enum SpringGrenadeState
    {
        Flying = 1,
        Deployed = 2,
    }

    #endregion

    #region Properties

    public ParticleSystem Smoke { get { return _smoke; } }

    public ParticleSystem DeployedEffect { get { return _deployedEffect; } }

    public Renderer Renderer { get { return _renderer; } }

    public override QuickItemConfiguration Configuration
    {
        get { return _config; }
        set { _config = (SpringGrenadeConfiguration)value; }
    }

    public AudioClip ExplosionSound { get; set; }

    public AudioClip JumpSound { get { return _sound; } }

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
            var instance = Throw(hit.point, Vector3.zero) as SpringGrenadeQuickItem;
            instance.machine.PopAllStates();

            GameState.LocalPlayer.MoveController.ApplyForce(_config.JumpDirection.normalized * _config.Force, CharacterMoveController.ForceType.Additive);
            SfxManager.Play2dAudioClip(JumpSound);

            StartCoroutine(DestroyDelayed(instance.ID));
        }
        else
        {
            var instance = Throw(position, velocity);
            instance.OnProjectileExploded += (p) =>
            {
                Collider[] colliders = Physics.OverlapSphere(p.Position, 2, UberstrikeLayerMasks.ExplosionMask);
                foreach (Collider c in colliders)
                {
                    var hitArea = c.gameObject.GetComponent<CharacterHitArea>();
                    if (hitArea != null && hitArea.RecieveProjectileDamage)
                    {
                        hitArea.Shootable.ApplyForce(hitArea.transform.position, _config.JumpDirection.normalized * _config.Force);
                    }
                }
            };
        }
    }

    private IEnumerator DestroyDelayed(int projectileId)
    {
        yield return new WaitForSeconds(0.2f);
        ProjectileManager.Instance.RemoveProjectile(projectileId, true);
        GameState.CurrentGame.RemoveProjectile(projectileId, true);
    }

    public IGrenadeProjectile Throw(Vector3 position, Vector3 velocity)
    {
        //Debug.LogError("Throw " + position);

        var instance = GameObject.Instantiate(this) as SpringGrenadeQuickItem;
        instance.Position = position;
        instance.Velocity = velocity;

        instance.machine.RegisterState((int)SpringGrenadeState.Flying, new FlyingState(instance));
        instance.machine.RegisterState((int)SpringGrenadeState.Deployed, new DeployedState(instance));
        instance.machine.PushState((int)SpringGrenadeState.Flying);

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
            if (OnExploded != null)
            {
                OnExploded(ID, transform.position);
            }

            if (OnProjectileExploded != null)
            {
                OnProjectileExploded(this);
            }

            point = transform.position;

            Destroy();
        }
        catch
        {
            Debug.LogWarning("SpringGrenade not exploded because it was already destroyed.");
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

    public event Action<int, Vector3> OnExploded;

    private class FlyingState : IState
    {
        SpringGrenadeQuickItem behaviour;

        float _timeOut;

        public FlyingState(SpringGrenadeQuickItem behaviour)
        {
            this.behaviour = behaviour;
        }

        public void OnEnter()
        {
            _timeOut = Time.time + behaviour._config.LifeTime;

            behaviour.OnCollisionEnterEvent += OnCollisionEnterEvent;

            GameObject gameObject = behaviour.gameObject;
            if (gameObject)
            {
                //ignore self collision for local & remote player
                if (GameState.LocalDecorator && gameObject.collider)
                {
                    Collider c = gameObject.collider;

                    foreach (CharacterHitArea a in GameState.LocalDecorator.HitAreas)
                    {
                        if (gameObject.active && a.gameObject.active)
                            Physics.IgnoreCollision(c, a.collider);
                    }
                }
            }
        }
        public void OnExit()
        {
            behaviour.OnCollisionEnterEvent -= OnCollisionEnterEvent;
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
                    behaviour.transform.position = c.contacts[0].point + c.contacts[0].normal * behaviour.collider.bounds.extents.sqrMagnitude;
                }

                behaviour.machine.PopState();
                behaviour.machine.PushState(2);
            }

            PlayBounceSound(c.transform.position);
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
        SpringGrenadeQuickItem behaviour;

        float _timeOut;

        public DeployedState(SpringGrenadeQuickItem behaviour)
        {
            this.behaviour = behaviour;
            behaviour.OnProjectileExploded = null;
        }

        public void OnEnter()
        {
            _timeOut = Time.time + behaviour._config.LifeTime;

            behaviour.OnTriggerEnterEvent += OnTriggerEnterEvent;

            if (behaviour.rigidbody) behaviour.rigidbody.isKinematic = true;
            if (behaviour.collider) GameObject.Destroy(behaviour.collider);
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

        public void OnTriggerEnterEvent(Collider c)
        {
            //here people get catapulted up - only with tag 'player'.
            if (TagUtil.GetTag(c) == "Player")
            {
                behaviour.machine.PopState();

                GameState.LocalPlayer.MoveController.ApplyForce(behaviour._config.JumpDirection.normalized * behaviour._config.Force, CharacterMoveController.ForceType.Additive);

                SfxManager.Play2dAudioClip(behaviour.JumpSound);
                ProjectileManager.Instance.RemoveProjectile(behaviour.ID, true);
                GameState.CurrentGame.RemoveProjectile(behaviour.ID, true);
            }
            else if (behaviour.collider.gameObject.layer == (int)UberstrikeLayer.RemotePlayer)
            {
                SfxManager.Play3dAudioClip(SoundEffectType.PropsJumpPad, 1.0f, 0.1f, 10.0f, AudioRolloffMode.Linear, behaviour.transform.position);
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
        get { return rigidbody ? rigidbody.velocity : Vector3.zero; }
        private set { if (rigidbody) rigidbody.velocity = value; }
    }
}