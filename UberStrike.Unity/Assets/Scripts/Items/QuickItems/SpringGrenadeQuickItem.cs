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

        // QuickItem.Instantiate builds the template with SetActiveRecursively(false) then
        // only re-activates the root. Clones inherit that (invisible children, no collider).
        // Reactivate the whole hierarchy and wake the Rigidbody so Velocity actually applies.
        instance.gameObject.SetActive(true);
        for (int i = 0; i < instance.gameObject.transform.childCount; i++)
            instance.gameObject.transform.GetChild(i).gameObject.SetActive(true);
        if (instance.GetComponent<Rigidbody>())
            instance.GetComponent<Rigidbody>().isKinematic = false;

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

            if (behaviour.GetComponent<Rigidbody>()) behaviour.GetComponent<Rigidbody>().isKinematic = true;
            if (behaviour.GetComponent<Collider>()) GameObject.Destroy(behaviour.GetComponent<Collider>());
            behaviour.gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;

            if (behaviour.DeployedEffect)
            {
                var emission = behaviour.DeployedEffect.emission;
                emission.enabled = true;
            }
            else
            {
                // Legacy EllipsoidParticleEmitter was stripped during the Unity 2022 port, so
                // prefab's _deployedEffect is null. Author a modern ring-style ParticleSystem
                // sized slightly larger than the grenade mesh (mesh ~0.5u → ring radius 0.7u).
                BuildFallbackDeployedEffect(behaviour);
            }
        }

        private static void BuildFallbackDeployedEffect(SpringGrenadeQuickItem behaviour)
        {
            // Stacked rings rising floor-by-floor: particles spawn on a small horizontal
            // circle at the grenade and travel straight up, rendered as horizontal
            // billboards so each one reads as a flat ring-floor. High emission rate packs
            // adjacent floors tight so they mesh into a continuous tower.
            var anchor = new GameObject("DeployedEffect");
            anchor.transform.SetParent(behaviour.transform, false);
            anchor.transform.localPosition = Vector3.zero;
            anchor.layer = behaviour.gameObject.layer;

            Material mat = Resources.Load<Material>("JumpPadParticles");
            Gradient fadeInOut = BuildJumpPadFadeGradient();

            // Reference look: ~3 concurrent particles, building from bottom up — each
            // particle starts small (V-glyph at the grenade) and grows into a bigger
            // ring as it rises. Size raised further; upSpeed halved so the three
            // floors sit tight (≈0.22 u spacing) instead of spaced with air gaps.
            BuildStackedRingLayer(anchor.transform, "Particle System",
                startSize: 2.5f, rate: 2.3f,
                upSpeed: 0.5f, lifetime: 1.3f, maxParticles: 8,
                renderMode: ParticleSystemRenderMode.HorizontalBillboard, maxParticleSize: 1f,
                material: mat, colorGradient: fadeInOut, growOverLife: true);

            behaviour._deployedEffect = anchor.GetComponentInChildren<ParticleSystem>();
        }

        private static Gradient BuildJumpPadFadeGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] {
                    new GradientAlphaKey(0.04f, 0f),
                    new GradientAlphaKey(0.71f, 0.25f),
                    new GradientAlphaKey(1f,    0.5f),
                    new GradientAlphaKey(0.71f, 0.75f),
                    new GradientAlphaKey(0.04f, 1f)
                });
            return g;
        }

        private static void BuildStackedRingLayer(Transform parent, string name,
            float startSize, float rate,
            float upSpeed, float lifetime, int maxParticles,
            ParticleSystemRenderMode renderMode, float maxParticleSize,
            Material material, Gradient colorGradient,
            bool shrinkOverLife = false, bool growOverLife = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // Mirror the pad's 0.121 lift: puts the first floor a hair above the
            // grenade body instead of intersecting it.
            go.transform.localPosition = new Vector3(0f, 0.121f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = startSize;
            main.startColor = Color.white;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;

            // Disabled shape = point emission at the GameObject origin → every floor
            // lands on the same central axis, identical to the jump pad setup.
            var shape = ps.shape;
            shape.enabled = false;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = upSpeed;
            velocity.z = 0f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = colorGradient;

            if (shrinkOverLife || growOverLife)
            {
                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                var curve = new AnimationCurve();
                if (growOverLife)
                {
                    curve.AddKey(0f, 0.25f);  // emit: small V glyph at the grenade
                    curve.AddKey(1f, 1.6f);   // end: oversized top ring (60% above startSize)
                }
                else
                {
                    curve.AddKey(0f, 1f);     // emit: full size (big ring)
                    curve.AddKey(1f, 0.25f);  // end: small V glyph
                }
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = renderMode;
            renderer.maxParticleSize = maxParticleSize;
            if (material != null) renderer.material = material;

            // Pre-roll one full lifetime so particles are already in steady state the
            // frame the grenade deploys — no "naked" grenade while the system ramps up.
            // prewarm alone proved unreliable with world-space simulation; Simulate +
            // Play is the established pattern for cold-start elimination.
            ps.Simulate(lifetime, true, true);
            ps.Play();
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
            else if (behaviour.GetComponent<Collider>().gameObject.layer == (int)UberstrikeLayer.RemotePlayer)
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
        get { return GetComponent<Rigidbody>() ? GetComponent<Rigidbody>().velocity : Vector3.zero; }
        private set { if (GetComponent<Rigidbody>()) GetComponent<Rigidbody>().velocity = value; }
    }
}