
using UnityEngine;

public class ExplosionManager : Singleton<ExplosionManager>
{
    public HeatWave HeatWavePrefab { get; set; }

    private ExplosionManager() { }

    public void ShowHeatWave(Vector3 point)
    {
        if (SystemInfo.supportsImageEffects && HeatWavePrefab)
        {
            GameObject.Instantiate(HeatWavePrefab, point, Camera.main.transform.rotation);
        }
    }

    public void PlayExplosionSound(Vector3 point, AudioClip clip)
    {
        //modify clip if under water
        if (GameState.HasCurrentSpace && GameState.CurrentSpace.HasWaterPlane && GameState.CurrentSpace.WaterPlaneHeight > point.y)
        {
            if (Random.Range(0, 2) == 0)
                clip = SfxManager.GetAudioClip(SoundEffectType.WeaponUnderwaterExplosion1);
            else
                clip = SfxManager.GetAudioClip(SoundEffectType.WeaponUnderwaterExplosion2);
        }

        if (clip != null)
        {
            SfxManager.Play3dAudioClip(clip, point);
        }
    }

    public void ShowExplosionEffect(Vector3 point, Vector3 normal, string tag, ParticleConfigurationType effectType)
    {
        switch (tag)
        {
            case "Wood":
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.WoodEffect, point, normal);
                break;
            case "Stone":
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.StoneEffect, point, normal);
                break;
            case "Metal":
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.MetalEffect, point, normal);
                break;
            case "Sand":
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.SandEffect, point, normal);
                break;
            case "Grass":
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.GrassEffect, point, normal);
                break;
            case "Avatar":
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.Splat, point, normal);
                break;
            case "Water":
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.WaterEffect, point, normal);
                break;
            default:
                ParticleEffectController.ShowExplosionEffect(effectType, SurfaceEffectType.Default, point, normal);
                break;
        }
    }
}