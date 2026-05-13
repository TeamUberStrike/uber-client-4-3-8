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
                // 3.5.5 set material _TintColor, not ParticleSystem startColor
                var r = _smokeRenderer.GetComponent<ParticleSystemRenderer>();
                if (r != null && r.material != null)
                    r.material.SetColor("_TintColor", _smokeColor);
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
        // Only standard cannon missiles have trail smoke in 3.5.5.
        // Enigma Cannon, Enigma Cannon Dragon, Lovewalking Gun do NOT.
        string missileName = gameObject.name.Replace("(Clone)", "").Trim();
        bool hasTrailSmoke = missileName == "CNMissile" ||
                             missileName == "CN_Painzerfaust_Missile" ||
                             missileName == "CN_ForceCannonPlus_Missile" ||
                             missileName == "CN_ForceCannon_Missile";
        if (!hasTrailSmoke)
            return;

        Transform smokeChild = transform.Find("MissileSmoke");
        if (smokeChild == null) return;

        // Move to Default layer — original layer 12 (GloballyLit_DynamicReflectRefract)
        // may not be in the main camera's culling mask in Unity 2022.
        smokeChild.gameObject.layer = 0;

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
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);  // smaller trail smoke
            main.startSpeed = 0f; // no initial speed — velocity set via VelocityOverLifetime
            main.startColor = Color.white;  // color controlled by material _TintColor like 3.5.5
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.283f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = _smokeAmount * 15;  // original rate

            // Ellipsoid shape (0.1, 0.1, 0.05)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            // Original 3.5.5 localVelocity=(0,0,1) but MissileSmoke has 180° X rotation,
            // so Z+ in local space = BACKWARD toward the player. This creates the
            // "smoke floating in your face" effect at the muzzle.
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = 0f;
            vel.y = 0f;
            vel.z = 1f;  // backward in MissileSmoke's rotated local space

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
                renderer.maxParticleSize = 0.25f;  // original value

                // Original 3.5.5 used Alpha Blended shader (NOT Additive).
                // Force Alpha Blended — create material with correct shader and texture.
                Shader abShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                if (abShader == null) abShader = Shader.Find("Particles/Alpha Blended");
                Material smokeMat = Resources.Load<Material>("ParticleMaterials/MissileSmoke");
                if (smokeMat != null && abShader != null)
                {
                    renderer.material = new Material(abShader);
                    renderer.material.mainTexture = smokeMat.mainTexture;
                    renderer.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.1f));
                }
                else if (abShader != null)
                {
                    renderer.material = new Material(abShader);
                    renderer.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.1f));
                }
                Debug.Log("[MissileSmokeFix] Material=" + (renderer.material != null ? renderer.material.name + " shader=" + renderer.material.shader.name : "NULL"));
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
