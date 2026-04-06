
using UnityEngine;

public class ExplosionManager : Singleton<ExplosionManager>
{
    public HeatWave HeatWavePrefab { get; set; }

    private ExplosionManager() { }

    public void ShowHeatWave(Vector3 point)
    {
        if (HeatWavePrefab)
        {
            Camera cam = null;
            if (LevelCamera.Exists && LevelCamera.Instance.MainCamera != null)
                cam = LevelCamera.Instance.MainCamera;
            else
                cam = Camera.main;
            if (cam != null)
            {
                HeatWave hw = GameObject.Instantiate(HeatWavePrefab, point, cam.transform.rotation);
                // Enigma Cannon HeatWave — only Enigma Cannon has _showHeatwave=1.
                // Distortion cranked to 128 (shader max) because GrabPass refraction
                // is weaker in Unity 2022 than 3.5.5's fixed-function equivalent.
                hw.InitExplosion(5.0f, 0.8f, 128f);
                // Move to layer 0 so main camera renders it (GrabPass works on main camera).
                // Weapon camera GrabPass doesn't work in Unity 2022.
                hw.gameObject.layer = 0;
                Debug.Log("[HeatWave] Explosion mesh HeatWave spawned at " + point +
                    " layer=" + hw.gameObject.layer);
            }
        }
        else
        {
            Debug.LogWarning("[HeatWave] ShowHeatWave called but HeatWavePrefab is NULL");
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