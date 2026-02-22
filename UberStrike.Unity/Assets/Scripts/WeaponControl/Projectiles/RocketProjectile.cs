using System.Collections;

using UnityEngine;

public class RocketProjectile : Projectile
{
    #region Fields

    [SerializeField]
    private ParticleSystem _smokeRenderer;

    [SerializeField]
    private ParticleSystem _smokeEmitter;

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
                var main = _smokeRenderer.main;
                main.startColor = _smokeColor;
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
                var emission = _smokeEmitter.emission;
                emission.rateOverTime = _smokeAmount * 15;
            }
        }
    }

    #endregion

    protected override void Awake()
    {
        base.Awake();

        // Fix missing smoke trail (Unity 3.5->2022 migration stripped EllipsoidParticleEmitter)
        if (_smokeRenderer == null || _smokeEmitter == null)
        {
            FixMissingSmoke();
        }

        SmokeColor = _smokeColor;
        SmokeAmount = _smokeAmount;

        if (_light != null)
        {
            _light.enabled = ApplicationDataManager.ApplicationOptions.VideoQualityLevel == 2;
        }
    }

    private void FixMissingSmoke()
    {
        Transform smokeChild = transform.Find("MissileSmoke");
        if (smokeChild == null) return;

        ParticleSystem ps = smokeChild.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = smokeChild.gameObject.AddComponent<ParticleSystem>();

            // Stop auto-play so we can configure before it starts emitting
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Original 4.8 CNMissile EllipsoidParticleEmitter values:
            // minSize=0.8, maxSize=1.2, minEnergy=1.8, maxEnergy=2.2
            // minEmission=10, maxEmission=10, localVelocity=(0,0,1)
            // rndVelocity=(0.15,0.15,0.15), ellipsoid=(0.1,0.1,0.05)
            // emitterVelocityScale=0.05, sizeGrow=0
            // ParticleAnimator: color fade in→white→white→fade→out
            // ParticleRenderer: UV 4x4 grid, 1 cycle, maxParticleSize=0.25
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.startSpeed = 1.0f; // localVelocity Z=1 (forward along missile)
            main.startColor = _smokeColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.283f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = _smokeAmount * 15;

            // Ellipsoid shape (0.1, 0.1, 0.05)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            // Original had sizeGrow=0 — no size change over lifetime

            // Original ParticleAnimator color animation: fade in → white → white → fade → out
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.27f, 0f),
                    new GradientAlphaKey(1.0f, 0.25f),
                    new GradientAlphaKey(1.0f, 0.5f),
                    new GradientAlphaKey(0.7f, 0.75f),
                    new GradientAlphaKey(0.06f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            // Small random scatter (rndVelocity 0.15 each axis)
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 1.0f;
            noise.octaveCount = 1;

            // Original UV animation: 4x4 grid, 1 cycle over lifetime
            var textureSheet = ps.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.numTilesX = 4;
            textureSheet.numTilesY = 4;
            textureSheet.cycleCount = 1;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;

            // Set up renderer (maxParticleSize=0.25 in original)
            var renderer = smokeChild.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortMode = ParticleSystemSortMode.Distance;
                renderer.maxParticleSize = 0.25f;

                Material smokeMat = Resources.Load<Material>("ParticleMaterials/MissileSmoke");
                if (smokeMat != null)
                {
                    renderer.material = smokeMat;
                }
                else
                {
                    Shader shader = Shader.Find("Legacy Shaders/Particles/Additive (Soft)");
                    if (shader != null)
                    {
                        renderer.material = new Material(shader);
                        renderer.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.196f));
                    }
                }
            }

            ps.Play();
            Debug.Log("[MissileSmokeFix] Added ParticleSystem to MissileSmoke on " + gameObject.name +
                      " color=" + _smokeColor + " rate=" + (_smokeAmount * 15) +
                      " size=0.8-1.2 life=1.8-2.2 speed=1 TSA=4x4");
        }

        _smokeRenderer = ps;
        _smokeEmitter = ps;
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
