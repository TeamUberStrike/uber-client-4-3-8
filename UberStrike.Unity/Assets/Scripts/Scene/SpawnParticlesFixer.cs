using UnityEngine;

/// <summary>
/// Fixes the SpawnParticles (pickup effect) ParticleSystem at runtime.
///
/// The Unity 3.5.5 → 2022 migration corrupted the SpawnParticles particle system:
/// - startSize was changed from 0.05-0.15 to 0.1-0.2 (particles ~2x bigger)
/// - startLifetime was changed from 0.1-3.0 to 0.5-1.0 (shorter, less variation)
/// - ParticleAnimator color fade animation was lost (particles stay at full alpha)
///
/// Original values extracted from git commit d11b013b (Unity 3.5.5 scene data):
///   EllipsoidParticleEmitter &209: minSize=0.05, maxSize=0.15, minEnergy=0.1, maxEnergy=3
///   ParticleAnimator &180: colorAnimation = alpha 0→50%→50%→35%→0% (ABGR packed)
///   ParticleRenderer &239: maxParticleSize=0.25, StretchParticles=0 (Billboard)
/// </summary>
public static class SpawnParticlesFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // SpawnParticles is a child of the ParticleEffectController hierarchy in Latest scene.
        // It's on layer 12 with tag "Weapon".
        var all = Resources.FindObjectsOfTypeAll<ParticleSystem>();
        foreach (var ps in all)
        {
            if (ps == null || ps.gameObject == null) continue;
            if (ps.gameObject.name != "SpawnParticles") continue;

            FixParticleSystem(ps);
            Debug.Log("[SpawnParticlesFixer] Fixed SpawnParticles pickup effect");
            return;
        }
    }

    static void FixParticleSystem(ParticleSystem ps)
    {
        // --- Main module: restore original size and lifetime ---
        var main = ps.main;
        // Original: minSize=0.05, maxSize=0.15 (was migrated to 0.1-0.2)
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        // Original: minEnergy=0.1, maxEnergy=3 (was migrated to 0.5-1.0)
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 3f);

        // --- Color over lifetime: restore ParticleAnimator fade animation ---
        // Original ParticleAnimator colorAnimation (5-key, ABGR packed):
        //   [0] rgba=16777215    → 0x00FFFFFF → alpha=0     (start: invisible)
        //   [1] rgba=2164260863  → 0x80FFFFFF → alpha=0.502 (fade in to ~50%)
        //   [2] rgba=2164260863  → 0x80FFFFFF → alpha=0.502 (hold ~50%)
        //   [3] rgba=1509949439  → 0x59FFFFFF → alpha=0.349 (fade down to ~35%)
        //   [4] rgba=16777215    → 0x00FFFFFF → alpha=0     (end: invisible)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),           // [0] alpha=0
                new GradientAlphaKey(0.502f, 0.25f),    // [1] alpha=128/255
                new GradientAlphaKey(0.502f, 0.50f),    // [2] alpha=128/255
                new GradientAlphaKey(0.349f, 0.75f),    // [3] alpha=89/255
                new GradientAlphaKey(0f, 1f)            // [4] alpha=0
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);
    }
}
