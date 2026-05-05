using UnityEngine;

/// <summary>
/// Fixes the SpawnParticles (pickup effect) ParticleSystem at runtime.
///
/// The Unity 3.5.5 → 2022 migration corrupted the SpawnParticles particle system:
/// - startSize was changed from 0.05-0.15 to 0.1-0.2 (particles ~2x bigger)
/// - startLifetime was changed from 0.1-3.0 to 0.5-1.0 (shorter, less variation)
/// - ParticleAnimator color fade animation was lost (particles stay at full alpha)
/// - Emission shape/velocity may be wrong (original: flat disk + upward + random spread)
/// - Render mode may have changed from Billboard
///
/// Original values re-extracted from Desktop/uber-client-4-3-8 Latest.unity:
///   EllipsoidParticleEmitter &209:
///     minSize=0.05, maxSize=0.15, minEnergy=0.1, maxEnergy=3
///     localVelocity=(0,1,0), rndVelocity=(2,2,2)
///     m_Ellipsoid=(0.5,0,0.5), m_OneShot=1, m_MinEmitterRange=1
///   ParticleAnimator &180:
///     damping=0.1   <- the "tucked" knob: per-frame velocity *= 0.9
///     force=(0,1,0), rndForce=(4,4,4), sizeGrow=0.1
///     colorAnimation = alpha 0->50%->50%->35%->0% (ABGR packed)
///   ParticleRenderer &239:
///     maxParticleSize=0.25, StretchParticles=0 (Billboard)
///   Material: SpawnParticles.mat (Particles/Alpha Blended, _TintColor white 50% alpha)
///
/// Why earlier passes regressed: header was incomplete — only the
/// EllipsoidParticleEmitter values were copied, not the ParticleAnimator's
/// damping/force/rndForce. Without damping, rndVelocity 2 m/s travels 6m in
/// 3s — clearly NOT "tucked". With damping 0.1 per-frame at 60fps, velocity
/// decays to ~0 inside 1s, total travel ~0.3m (the "tucked" look).
/// </summary>
public static class SpawnParticlesFixer
{
    static bool _hasRun;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _hasRun = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // Register for scene loads so we retry if SpawnParticles isn't available yet.
        // In standalone builds, the ParticleEffectController hierarchy (which contains
        // SpawnParticles) may not be loaded when AfterSceneLoad first fires.
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        TryRun();
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!_hasRun)
        {
            Debug.Log("[SpawnParticlesFixer] Scene loaded: " + scene.name + " — attempting fix");
            TryRun();
        }
    }

    static void TryRun()
    {
        if (_hasRun) return;

        // SpawnParticles is a child of the ParticleEffectController hierarchy in Latest scene.
        // It's on layer 12 with tag "Weapon".
        var all = Resources.FindObjectsOfTypeAll<ParticleSystem>();
        foreach (var ps in all)
        {
            if (ps == null || ps.gameObject == null) continue;
            if (ps.gameObject.name != "SpawnParticles") continue;
            // Prefer scene instances over prefab assets
            if (string.IsNullOrEmpty(ps.gameObject.scene.name)) continue;

            FixParticleSystem(ps);
            _hasRun = true;
            Debug.Log("[SpawnParticlesFixer] Fixed SpawnParticles pickup effect (scene=" + ps.gameObject.scene.name + ")");
            return;
        }

        Debug.LogWarning("[SpawnParticlesFixer] SpawnParticles not found yet, will retry on next scene load");
    }

    static void FixParticleSystem(ParticleSystem ps)
    {
        // --- Main module: restore original size, lifetime, velocity, and rotation ---
        var main = ps.main;
        // Original: minSize=0.05, maxSize=0.15 (was migrated to 0.1-0.2)
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        // Original: minEnergy=0.1, maxEnergy=3 (was migrated to 0.5-1.0)
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 3f);
        // 3.5 EllipsoidParticleEmitter has no startSpeed concept — initial velocity
        // comes entirely from localVelocity + rndVelocity, set in velocityOverLifetime
        // below. Leaving startSpeed > 0 made particles get an additional radial push
        // along the shape direction, on top of the rnd velocity, doubling the spread.
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.maxParticles = 200;
        // No rotation for pickup sparkles
        main.startRotation3D = false;
        main.startRotation = 0f;

        // --- Shape module: flat XZ plane emission (original Ellipsoid 0.5, 0, 0.5) ---
        // m_Ellipsoid=(0.5, 0, 0.5) is a flat 1x0x1 plane in XZ — particles spawn
        // anywhere within that plane at Y=0. Hemisphere (used previously) is a 3D
        // dome with Y > 0, which gave particles a head-start in the vertical
        // direction and visibly widened the cluster. Box with scale (1, 0, 1)
        // matches the original flat-plane spawn exactly.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1f, 0f, 1f);

        // --- Velocity over lifetime: localVelocity(0,1,0) + rndVelocity(±2,±2,±2) ---
        // In Unity 2022, MinMaxCurve(min, max) creates a Random-Between-Two-Constants
        // curve where each particle picks a single value at spawn — that matches the
        // 3.5 ParticleEmitter rndVelocity semantics (random initial velocity, fixed
        // for the particle's lifetime, not continuously varying).
        // Y range = localVelocity.y ± rndVelocity.y = 1 ± 2 = (-1, 3).
        // Previous code set y=1 as a single constant — every particle got the same
        // upward bias with no random component, and the missing rnd was masked by
        // an over-tuned hemisphere + startSpeed combo on the spawn side.
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
        vel.y = new ParticleSystem.MinMaxCurve(-1f, 3f);
        vel.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
        vel.space = ParticleSystemSimulationSpace.World;

        // --- Disable emission module (we use ps.Emit(count) from ShowPickUpEffect) ---
        var emission = ps.emission;
        emission.enabled = false;

        // --- Renderer: Billboard mode (original StretchParticles=0) ---
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.25f;
        }

        // --- Limit Velocity over Lifetime: replicate ParticleAnimator damping=0.1 ---
        // 3.5 damping: per-frame velocity *= (1 - damping). Unity 2022's
        // limitVelocityOverLifetime.dampen is documented as the same per-frame
        // fraction. Setting limit=0 means "any velocity over zero gets damped" —
        // i.e. all of it. This is the single most important parameter for the
        // "tucked" pickup look: without it, rndVelocity 2 m/s × lifetime 3s = 6m
        // spread; with it, total travel collapses to ~0.3m as velocity decays.
        var lim = ps.limitVelocityOverLifetime;
        lim.enabled = true;
        lim.limit = new ParticleSystem.MinMaxCurve(0f);
        lim.dampen = 0.2f;
        lim.separateAxes = false;

        // --- Force over Lifetime: replicate ParticleAnimator force=(0,1,0) ---
        // 1 m/s² constant upward acceleration. Combined with damping=0.1, this
        // produces a steady-state upward drift of ~0.15 m/s — the gentle rise
        // visible in the original. rndForce(4,4,4) is per-frame random in 3.5;
        // skipped for now (Unity 2022 MinMaxCurve random would be per-particle
        // not per-frame, semantically different — would need Noise module).
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.x = new ParticleSystem.MinMaxCurve(0f);
        force.y = new ParticleSystem.MinMaxCurve(1f);
        force.z = new ParticleSystem.MinMaxCurve(0f);
        force.space = ParticleSystemSimulationSpace.World;

        // --- Disable unnecessary modules ---
        var noise = ps.noise; noise.enabled = false;
        var rotOL = ps.rotationOverLifetime; rotOL.enabled = false;
        var rotBS = ps.rotationBySpeed; rotBS.enabled = false;
        var szOL = ps.sizeOverLifetime; szOL.enabled = false;
        var szBS = ps.sizeBySpeed; szBS.enabled = false;
        var colBS = ps.colorBySpeed; colBS.enabled = false;
        var extF = ps.externalForces; extF.enabled = false;
        var inhV = ps.inheritVelocity; inhV.enabled = false;
        var tsa = ps.textureSheetAnimation; tsa.enabled = false;

        // --- Color over lifetime: restore ParticleAnimator fade animation ---
        // Original ParticleAnimator colorAnimation (5-key, ABGR packed):
        //   [0] rgba=16777215    -> 0x00FFFFFF -> alpha=0     (start: invisible)
        //   [1] rgba=2164260863  -> 0x80FFFFFF -> alpha=0.502 (fade in to ~50%)
        //   [2] rgba=2164260863  -> 0x80FFFFFF -> alpha=0.502 (hold ~50%)
        //   [3] rgba=1509949439  -> 0x59FFFFFF -> alpha=0.349 (fade down to ~35%)
        //   [4] rgba=16777215    -> 0x00FFFFFF -> alpha=0     (end: invisible)
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
