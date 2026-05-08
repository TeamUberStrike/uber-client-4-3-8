using System;
using UnityEngine;
using System.Collections.Generic;

public class ParticleEffectController : MonoSingleton<ParticleEffectController>
{
    #region Fields
    [SerializeField]
    private ParticleConfiguration[] _allWeaponData;
    [SerializeField]
    private ParticleSystem _pickupParticleEmitter;
    [SerializeField]
    private HeatWave _heatWavePrefab;
    [SerializeField]
    private ParticleSystem _heatWave;

    private Dictionary<ParticleConfigurationType, ParticleCobfigurationPerWeapon> _allConfigurations;

    private static Dictionary<Vector3, float> _effects = new Dictionary<Vector3, float>();
    private static float _nextCleanup;

    #endregion

    private ExplosionController _explosionParticleSystem;

    private void Awake()
    {
        _explosionParticleSystem = new ExplosionController();

        _allConfigurations = new Dictionary<ParticleConfigurationType, ParticleCobfigurationPerWeapon>();

        foreach (ParticleConfiguration particleConfiguration in _allWeaponData)
        {
            _allConfigurations[particleConfiguration.Type] = particleConfiguration.Configuration;
        }

        ExplosionManager.Instance.HeatWavePrefab = _heatWavePrefab;

        if (_pickupParticleEmitter != null)
            ConfigurePickupParticles(_pickupParticleEmitter);

        ParticleEffectControllerMigrator.Migrate(this);
    }

    // The Unity 3.5.5 → 2022 migration auto-converted the pickup effect's
    // EllipsoidParticleEmitter + ParticleAnimator + ParticleRenderer to a
    // ParticleSystem with default settings, losing the size/lifetime curves,
    // ParticleAnimator damping/force, and the alpha-fade colorAnimation. Apply
    // the original values from the 3.5.5 reference at startup so the pickup
    // sparkle matches the legacy "tucked" look (1:1 confirmed).
    //
    // Source values (Desktop/uber-client-4-3-8 Latest.unity):
    //   EllipsoidParticleEmitter &209: minSize=0.05, maxSize=0.15,
    //     minEnergy=0.1, maxEnergy=3, localVelocity=(0,1,0), rndVelocity=(2,2,2),
    //     m_Ellipsoid=(0.5,0,0.5), m_OneShot=1, m_MinEmitterRange=1
    //   ParticleAnimator &180: damping=0.1 (per-frame velocity *= 0.9),
    //     force=(0,1,0), rndForce=(4,4,4), sizeGrow=0.1,
    //     colorAnimation alpha 0 → 50% → 50% → 35% → 0
    //   ParticleRenderer &239: maxParticleSize=0.25, StretchParticles=0 (Billboard)
    //
    // The single most important parameter is limitVelocityOverLifetime.dampen
    // (0.2). Without it, rndVelocity 2 m/s × lifetime 3s = 6m spread; with it,
    // total travel collapses to ~0.3m as velocity decays.
    private static void ConfigurePickupParticles(ParticleSystem ps)
    {
        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 3f);
        // 3.5 EllipsoidParticleEmitter has no startSpeed concept — initial
        // velocity comes entirely from localVelocity + rndVelocity, set in
        // velocityOverLifetime below. A non-zero startSpeed would add a radial
        // push along the shape direction on top of the rnd velocity, doubling
        // the spread.
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.maxParticles = 200;
        main.startRotation3D = false;
        main.startRotation = 0f;

        // m_Ellipsoid=(0.5, 0, 0.5) is a flat 1×0×1 plane in XZ — particles
        // spawn anywhere within that plane at Y=0. Hemisphere (used previously)
        // is a 3D dome with Y > 0 that gave particles a vertical head-start
        // and visibly widened the cluster. Box scale (1, 0, 1) matches the
        // original flat-plane spawn exactly.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1f, 0f, 1f);

        // localVelocity(0,1,0) + rndVelocity(±2,±2,±2). MinMaxCurve(min, max)
        // is Random-Between-Two-Constants — each particle picks a single value
        // at spawn, fixed for its lifetime, matching 3.5 ParticleEmitter
        // rndVelocity semantics.
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
        vel.y = new ParticleSystem.MinMaxCurve(-1f, 3f);
        vel.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
        vel.space = ParticleSystemSimulationSpace.World;

        // Emission disabled — ShowPickUpEffect calls Emit(count) on demand.
        var emission = ps.emission;
        emission.enabled = false;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.25f;
        }

        // Replicates 3.5 ParticleAnimator damping=0.1. limit=0 + dampen=0.2
        // means "any velocity over zero gets damped" — i.e. all of it.
        var lim = ps.limitVelocityOverLifetime;
        lim.enabled = true;
        lim.limit = new ParticleSystem.MinMaxCurve(0f);
        lim.dampen = 0.2f;
        lim.separateAxes = false;

        // ParticleAnimator force=(0,1,0): 1 m/s² constant upward acceleration.
        // Combined with the limit/dampen above, produces a steady-state upward
        // drift of ~0.15 m/s — the gentle rise visible in the original.
        // 3.5's rndForce(4,4,4) was per-frame random; skipped because Unity
        // 2022 MinMaxCurve random is per-particle (different semantics).
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.x = new ParticleSystem.MinMaxCurve(0f);
        force.y = new ParticleSystem.MinMaxCurve(1f);
        force.z = new ParticleSystem.MinMaxCurve(0f);
        force.space = ParticleSystemSimulationSpace.World;

        var noise = ps.noise; noise.enabled = false;
        var rotOL = ps.rotationOverLifetime; rotOL.enabled = false;
        var rotBS = ps.rotationBySpeed; rotBS.enabled = false;
        var szOL = ps.sizeOverLifetime; szOL.enabled = false;
        var szBS = ps.sizeBySpeed; szBS.enabled = false;
        var colBS = ps.colorBySpeed; colBS.enabled = false;
        var extF = ps.externalForces; extF.enabled = false;
        var inhV = ps.inheritVelocity; inhV.enabled = false;
        var tsa = ps.textureSheetAnimation; tsa.enabled = false;

        // ParticleAnimator colorAnimation (5-key, ABGR packed):
        //   [0] 0x00FFFFFF -> alpha=0     (start: invisible)
        //   [1] 0x80FFFFFF -> alpha=0.502 (fade in to ~50%)
        //   [2] 0x80FFFFFF -> alpha=0.502 (hold ~50%)
        //   [3] 0x59FFFFFF -> alpha=0.349 (fade down to ~35%)
        //   [4] 0x00FFFFFF -> alpha=0     (end: invisible)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.502f, 0.25f),
                new GradientAlphaKey(0.502f, 0.50f),
                new GradientAlphaKey(0.349f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);
    }


    public static void ShowPickUpEffect(Vector3 pos, int count)
    {
        if (Exists)
        {
            Instance._pickupParticleEmitter.transform.position = pos;
            Instance._pickupParticleEmitter.Emit(count);
        }
    }

    public static void ShowHeatwaveEffect(Vector3 pos)
    {
        // Original 3.5.5 values.
        ShowHeatwaveEffect(pos, 1f, 1f);
    }

    public static void ShowHeatwaveEffect(Vector3 pos, float size, float life)
    {
        if (Exists && Instance._heatWave)
        {
            ParticleEmissionSystem.EmitSafe(Instance._heatWave, pos, Vector3.zero, size, life, Color.white);
        }
    }

    public static void ShowHitEffect(ParticleConfigurationType effectType, SurfaceEffectType surface, Vector3 direction, Vector3 hitPoint, Vector3 hitNormal, Vector3 muzzlePosition, float distance, ref MoveTrailrendererObject trailRenderer, Transform parent)
    {
        ShowHitEffect(effectType, surface, direction, hitPoint, hitNormal, muzzlePosition, distance, ref  trailRenderer, parent, 0);
    }

    public static void ShowHitEffect(ParticleConfigurationType effectType, SurfaceEffectType surface, Vector3 direction, Vector3 hitPoint, Vector3 hitNormal, Vector3 muzzlePosition, float distance, ref MoveTrailrendererObject trailRenderer, Transform parent, int damage)
    {
        if (Exists)
        {
            ParticleCobfigurationPerWeapon effect = Instance._allConfigurations[effectType];

            if (effect != null)
            {
                ShowTrailEffect(effect, trailRenderer, parent, hitPoint, muzzlePosition, distance, direction);

                switch (surface)
                {
                    case SurfaceEffectType.WoodEffect:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.HitMaterialParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WoodEffect);
                            ParticleEmissionSystem.FireParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.FireParticleConfigurationForInstantHit);
                        }
                        break;

                    case SurfaceEffectType.StoneEffect:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.HitMaterialParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.StoneEffect);
                            ParticleEmissionSystem.FireParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.FireParticleConfigurationForInstantHit);
                        }
                        break;

                    case SurfaceEffectType.MetalEffect:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.HitMaterialParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.MetalEffect);
                            ParticleEmissionSystem.FireParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.FireParticleConfigurationForInstantHit);
                        }
                        break;

                    case SurfaceEffectType.WaterEffect:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.WaterCircleParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WaterCircleEffect);
                        }
                        break;

                    case SurfaceEffectType.GrassEffect:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.HitMaterialParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.GrassEffect);
                            ParticleEmissionSystem.FireParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.FireParticleConfigurationForInstantHit);
                        }
                        break;

                    case SurfaceEffectType.SandEffect:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.HitMaterialParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.SandEffect);
                            ParticleEmissionSystem.FireParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.FireParticleConfigurationForInstantHit);
                        }
                        break;

                    case SurfaceEffectType.Splat:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.HitMaterialRotatingParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.Splat);
                        }
                        break;

                    case SurfaceEffectType.Default:
                        if (CheckVisibility(hitPoint))
                        {
                            ParticleEmissionSystem.FireParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.FireParticleConfigurationForInstantHit);
                        }
                        break;
                }
            }
            else
            {
                //Debug.Log("No effect type specified for " + effect);
            }
        }
        else
        {
            Debug.LogError("ParticleEffectController is not attached to a gameobject in scene!");
        }
    }

    private static void ShowTrailEffect(ParticleCobfigurationPerWeapon effect, MoveTrailrendererObject trailRenderer, Transform parent, Vector3 hitPoint, Vector3 muzzlePosition, float distance, Vector3 direction)
    {
        if (effect.WeaponImpactEffectConfiguration.UseTrailrendererForTrail)
        {
            if (effect.WeaponImpactEffectConfiguration.TrailrendererTrailPrefab != null)
            {
                if (trailRenderer == null)
                {
                    trailRenderer = GameObject.Instantiate(effect.WeaponImpactEffectConfiguration.TrailrendererTrailPrefab, muzzlePosition, Quaternion.identity) as MoveTrailrendererObject;
                    trailRenderer.gameObject.transform.parent = parent;
                }
                trailRenderer.MoveTrail(hitPoint, muzzlePosition, distance);
            }
        }
        else
        {
            ParticleEmissionSystem.TrailParticles(hitPoint, direction, effect.WeaponImpactEffectConfiguration.TrailParticleConfigurationForInstantHit, muzzlePosition, distance);
        }
    }

    public static void ShowExplosionEffect(ParticleConfigurationType effectType, SurfaceEffectType surface, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (Exists)
        {
            if (CheckVisibility(hitPoint))
            {
                ParticleCobfigurationPerWeapon effect = Instance._allConfigurations[effectType];
                bool splatterGun = false;

                if (effect != null)
                {
                    switch (surface)
                    {
                        case SurfaceEffectType.None:
                            break;
                        case SurfaceEffectType.WoodEffect:
                            ParticleEmissionSystem.HitMateriaHalfSphericParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WoodEffect);
                            break;
                        case SurfaceEffectType.WaterEffect:
                            ParticleEmissionSystem.WaterCircleParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WaterCircleEffect);
                            ParticleEmissionSystem.WaterSplashParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WaterExtraSplashEffect);
                            break;
                        case SurfaceEffectType.StoneEffect:
                            ParticleEmissionSystem.HitMateriaHalfSphericParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.StoneEffect);
                            break;
                        case SurfaceEffectType.MetalEffect:
                            ParticleEmissionSystem.HitMateriaHalfSphericParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.MetalEffect);
                            break;
                        case SurfaceEffectType.GrassEffect:
                            ParticleEmissionSystem.HitMateriaHalfSphericParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.GrassEffect);
                            break;
                        case SurfaceEffectType.SandEffect:
                            ParticleEmissionSystem.HitMateriaHalfSphericParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.SandEffect);
                            break;
                        case SurfaceEffectType.Splat:
                            ParticleEmissionSystem.HitMateriaFullSphericParticles(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.Splat);
                            break;
                        default:
                            break;
                    }

                    // don't display Dust and smoke on Fast and Fastest graphics
                    bool showDustAndTrails = QualitySettings.GetQualityLevel() > 0;
                    if (showDustAndTrails)
                    {
                        Instance._explosionParticleSystem.EmitDust(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.ExplosionParameterSet.DustParameters);
                        Instance._explosionParticleSystem.EmitSmoke(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.ExplosionParameterSet.SmokeParameters);
                    }
                    // if that was SplatterGun, then show Trails even on Fast and Fastest graphics
                    if (showDustAndTrails || splatterGun)
                    {
                        Instance._explosionParticleSystem.EmitTrail(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.ExplosionParameterSet.TrailParameters);
                    }

                    Instance._explosionParticleSystem.EmitBlast(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.ExplosionParameterSet.BlastParameters);
                    Instance._explosionParticleSystem.EmitRing(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.ExplosionParameterSet.RingParameters);
                    Instance._explosionParticleSystem.EmitSpark(hitPoint, hitNormal, effect.WeaponImpactEffectConfiguration.ExplosionParameterSet.SparkParameters);
                }
            }
        }
    }


    private static void WaterRipplesEffect(ParticleCobfigurationPerWeapon effect, Vector3 hitPoint, Vector3 direction, Vector3 muzzlePosition, float distance)
    {
        float newDistance = Math.Abs(muzzlePosition.y) * distance / (Math.Abs(hitPoint.y) + Math.Abs(muzzlePosition.y));
        Vector3 newHitPoint = direction * newDistance + muzzlePosition;

        // also check, if fog will create black effect
        if (CanPlayEffectAt(newHitPoint) && CheckVisibility(newHitPoint))
        {
            //splashes
            ParticleEmissionSystem.WaterSplashParticles(newHitPoint, Vector3.up, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WaterExtraSplashEffect);

            //ripples
            ParticleEmissionSystem.WaterCircleParticles(newHitPoint, Vector3.up, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WaterCircleEffect);
        }
    }

    private static Vector3 PositionRaster(Vector3 v)
    {
        return new Vector3(Mathf.RoundToInt(v[0]), Mathf.RoundToInt(v[1]), Mathf.RoundToInt(v[2]));
    }

    private static bool CanPlayEffectAt(Vector3 v)
    {
        //make sure the dictionary is not growing unlimited
        if (_nextCleanup < Time.time)
        {
            _nextCleanup = Time.time + 30;
            _effects.Clear();
        }

        //check if there was another effect already at the same position within the last second
        Vector3 v1 = PositionRaster(v);
        float time;
        if (!_effects.TryGetValue(v1, out time) || time < Time.time)
        {
            _effects[v1] = Time.time + 1;
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void ProjectileWaterRipplesEffect(ParticleConfigurationType effectType, Vector3 hitPosition)
    {
        if (Exists)
        {
            if (GameState.HasCurrentSpace) // && LevelFXController.Instance.IsWaterEnabled)
            {
                ParticleCobfigurationPerWeapon effect = Instance._allConfigurations[effectType];
                //ParticleCobfigurationPerWeapon tempData = null;

                //switch (weapon)
                //{
                //    case ImpactEffectType.CNDefault:
                //    case ImpactEffectType.CNForceCannon:
                //    case ImpactEffectType.CNPaintzerfaust:
                //        tempData = Instance._allWeaponData.CNDefault;
                //        break;
                //    case ImpactEffectType.CNEnigmaCannon:
                //        tempData = Instance._allWeaponData.CNEnigmaCannon;
                //        break;
                //    case ImpactEffectType.LRDefault:
                //    case ImpactEffectType.LREnamelator:
                //    case ImpactEffectType.LRMortalExporter:
                //    case ImpactEffectType.LRTheFinalWord:
                //        tempData = Instance._allWeaponData.LRDefault;
                //        break;
                //    case ImpactEffectType.SPDefault:
                //    case ImpactEffectType.SPMadSplatter:
                //    case ImpactEffectType.SPMagmaRifle:
                //    case ImpactEffectType.SPVandalizer:
                //        tempData = Instance._allWeaponData.SPDefault;
                //        break;
                //    default:
                //        tempData = Instance._allWeaponData.None;
                //        break;
                //}

                if (effect != null)
                {
                    Vector3 newHitPoint = hitPosition;

                    //splashes
                    ParticleEmissionSystem.WaterSplashParticles(newHitPoint, Vector3.up, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WaterExtraSplashEffect);

                    //ripples
                    // newHitPoint.y = (LevelFXController.Instance && GameState.CurrentPlayer.IsUnderWater) ? -0.01f : 0.01f;
                    ParticleEmissionSystem.WaterCircleParticles(newHitPoint, Vector3.up, effect.WeaponImpactEffectConfiguration.SurfaceParameterSet.WaterCircleEffect);
                }
            }
        }
    }

    private static bool CheckVisibility(Vector3 hitPoint)
    {
        bool result = true;
        //if (LevelFXController.Instance && GameState.CurrentPlayer.IsUnderWater && GameState.HasCurrentGame && GameState.HasCurrentPlayer)
        //{
        //    result = false;
        //}
        return result;
    }


    [Serializable]
    private class ParticleConfiguration
    {
        [HideInInspector]
        public string Name = "Effect";
        public ParticleConfigurationType Type;
        public ParticleCobfigurationPerWeapon Configuration;

        public ParticleConfiguration(string name, ParticleConfigurationType type, ParticleCobfigurationPerWeapon configuration)
        {
            Name = name;
            Type = type;
            Configuration = configuration;
        }
    }
}

[System.Serializable]
public class WeaponImpactEffectConfiguration
{
    public ExplosionParameterSet ExplosionParameterSet;
    public FireParticleConfiguration FireParticleConfigurationForInstantHit;
    public TrailParticleConfiguration TrailParticleConfigurationForInstantHit;
    public SurfaceParameters SurfaceParameterSet;
    public MoveTrailrendererObject TrailrendererTrailPrefab;
    public bool UseTrailrendererForTrail;
}

[System.Serializable]
public class SurfaceParameters
{
    public ParticleConfiguration WoodEffect;
    public FireParticleConfiguration WaterCircleEffect;
    public FireParticleConfiguration WaterExtraSplashEffect;
    public ParticleConfiguration StoneEffect;
    public ParticleConfiguration MetalEffect;
    public ParticleConfiguration GrassEffect;
    public ParticleConfiguration SandEffect;
    public ParticleConfiguration Splat;
}

[System.Serializable]
public class ParticleConfiguration
{
    public float ParticleMinSize;
    public float ParticleMaxSize;
    public int ParticleCount;
    public float ParticleMinSpeed;
    public float ParticleMaxSpeed;
    public float ParticleMinLiveTime;
    public float ParticleMaxLiveTime;
    public float ParticleMinZVelocity;
    public float ParticleMaxZVelocity;
    public Color ParticleColor;
    public ParticleSystem ParticleEmitter;
}

[System.Serializable]
public class FireParticleConfiguration
{
    public float ParticleMinSize;
    public float ParticleMaxSize;
    public int ParticleCount;
    public float ParticleMinLiveTime;
    public float ParticleMaxLiveTime;
    public Color ParticleColor;
    public ParticleSystem ParticleEmitter;
}

[System.Serializable]
public class TrailParticleConfiguration
{
    public float ParticleMinSize;
    public float ParticleMaxSize;
    public float ParticleMinLiveTime;
    public float ParticleMaxLiveTime;
    public Color ParticleColor;
    public ParticleSystem ParticleEmitter;
}

[System.Serializable]
public class ExplosionBaseParameters
{
    public int ParticleCount;
    public float MinLifeTime;
    public float MaxLifeTime;
    public float MinSize;
    public float MaxSize;
    public ParticleSystem ParticleEmitter;
}

[System.Serializable]
public class ExplosionDustParameters
{
    public int ParticleCount;
    public float MinStartPositionSize;
    public float MaxStartPositionSize;
    public float MinLifeTime;
    public float MaxLifeTime;
    public float MinSize;
    public float MaxSize;
    public ParticleSystem ParticleEmitter;
}

[System.Serializable]
public class ExplosionRingParameters
{
    public float StartSize;
    public float MinLifeTime;
    public float MaxLifeTime;
    public ParticleSystem ParticleEmitter;
}

[System.Serializable]
public class ExplosionSphericParameters
{
    public int ParticleCount;
    public float MinLifeTime;
    public float MaxLifeTime;
    public float MinSize;
    public float MaxSize;
    public float Speed;
    public ParticleSystem ParticleEmitter;
}

[System.Serializable]
public class ExplosionParameterSet
{
    public ExplosionBaseParameters BlastParameters;
    public ExplosionDustParameters DustParameters;
    public ExplosionRingParameters RingParameters;
    public ExplosionBaseParameters SmokeParameters;
    public ExplosionSphericParameters SparkParameters;
    public ExplosionSphericParameters TrailParameters;
}
