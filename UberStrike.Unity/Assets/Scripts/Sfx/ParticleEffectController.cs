using System;
using UnityEngine;
using System.Collections.Generic;

public class ParticleEffectController : MonoSingleton<ParticleEffectController>
{
    #region Fields
    [SerializeField]
    private ParticleConfiguration[] _allWeaponData;
    [SerializeField]
    private ParticleEmitter _pickupParticleEmitter;
    [SerializeField]
    private HeatWave _heatWavePrefab;
    [SerializeField]
    private ParticleEmitter _heatWave;

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
        if (Exists && Instance._heatWave)
        {
            Instance._heatWave.Emit(pos, Vector3.zero, 1, 1, Color.white);
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
    public ParticleEmitter ParticleEmitter;
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
    public ParticleEmitter ParticleEmitter;
}

[System.Serializable]
public class TrailParticleConfiguration
{
    public float ParticleMinSize;
    public float ParticleMaxSize;
    public float ParticleMinLiveTime;
    public float ParticleMaxLiveTime;
    public Color ParticleColor;
    public ParticleEmitter ParticleEmitter;
}

[System.Serializable]
public class ExplosionBaseParameters
{
    public int ParticleCount;
    public float MinLifeTime;
    public float MaxLifeTime;
    public float MinSize;
    public float MaxSize;
    public ParticleEmitter ParticleEmitter;
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
    public ParticleEmitter ParticleEmitter;
}

[System.Serializable]
public class ExplosionRingParameters
{
    public float StartSize;
    public float MinLifeTime;
    public float MaxLifeTime;
    public ParticleEmitter ParticleEmitter;
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
    public ParticleEmitter ParticleEmitter;
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
