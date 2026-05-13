using UnityEngine;
using System.Collections;

public class MuzzleSmoke : BaseWeaponEffect
{
    private ParticleSystem _particleEmitter;

    private void Awake()
    {
        _particleEmitter = GetComponentInChildren<ParticleSystem>();

        // Fix: Unity 3.5->2022 migration stripped EllipsoidParticleEmitter.
        // Recreate as modern ParticleSystem with original 4.8 values.
        // Different weapons have different muzzle smoke — detect by GO name.
        if (_particleEmitter == null)
        {
            string goName = gameObject.name;

            if (goName.Contains("MGUMG") || goName.Contains("UMG"))
            {
                // UMG: Bright additive flash that grows quickly
                CreateUMGMuzzleFlash();
            }
            else if (goName.StartsWith("CN"))
            {
                // Cannon weapons: Subtle Alpha Blended smoke puff at barrel
                CreateCannonMuzzleSmoke();
            }
            else
            {
                // Generic fallback: small Alpha Blended smoke puff
                CreateCannonMuzzleSmoke();
            }
        }
    }

    /// <summary>
    /// UMG-specific bright additive muzzle flash.
    /// Original 4.8 MGUMGMuzzleSmoke EllipsoidParticleEmitter:
    ///   minSize=0.15, maxSize=0.2, minEnergy=0.1, maxEnergy=0.12
    ///   sizeGrow=40, OneShot=true, Ellipsoid=(0.02,0.02,0.01)
    ///   Color: white alpha 10->180->255->180->10
    ///   Material: UMGMuzzleSmoke (Particles/Additive)
    /// </summary>
    private void CreateUMGMuzzleFlash()
    {
        _particleEmitter = gameObject.AddComponent<ParticleSystem>();
        _particleEmitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _particleEmitter.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.2f);
        main.startSpeed = 0f;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 10;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.283f);

        var emission = _particleEmitter.emission;
        emission.enabled = false;

        var shape = _particleEmitter.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;

        // SizeOverLifetime: grow from tiny seed to flash.
        // Original sizeGrow=40 over 0.11s → 4.575 world units.
        // Original maxParticleSize=50 (uncapped), but actual visible size in footage ~10-15% viewport.
        // Tuned: 25x way too big, 15x still too big, 8x still too big, 4x too small, 6x middle ground.
        var sizeOverLifetime = _particleEmitter.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.02f);  // Start as tiny seed (sizeGrow starts from near-zero)
        sizeCurve.AddKey(1f, 1f);     // Grow to max
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(6f, sizeCurve);

        // Color animation: white alpha 10->180->255->180->10
        var colorOverLifetime = _particleEmitter.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.04f, 0f),     // alpha 10/255
                new GradientAlphaKey(0.71f, 0.25f),   // alpha 180/255
                new GradientAlphaKey(1.0f, 0.5f),     // alpha 255/255
                new GradientAlphaKey(0.71f, 0.75f),   // alpha 180/255
                new GradientAlphaKey(0.04f, 1f)        // alpha 10/255
            }
        );
        colorOverLifetime.color = grad;

        // Renderer with UMGMuzzleSmoke additive material
        // maxParticleSize=0.18 caps at 18% of viewport height (original footage ~10-15% viewport)
        var renderer = gameObject.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.18f;
            renderer.material = LoadMaterial("UMGMuzzleSmoke", "Legacy Shaders/Particles/Additive");
        }

        Debug.Log("[MuzzleSmokeFix] UMG flash on " + gameObject.name +
                  " weapon=" + (transform.parent != null ? transform.parent.name : "unknown") +
                  " size=0.15-0.2 life=0.1-0.12 sizeMultiplier=6 maxPS=0.18" +
                  " mat=" + (renderer != null && renderer.material != null ? renderer.material.name : "none"));
    }

    /// <summary>
    /// Cannon-specific muzzle smoke: Alpha Blended smoke puff at barrel.
    /// Original ForceCannon CNMuzzleSmoke EllipsoidParticleEmitter (git d11b013b):
    ///   minSize=0.1, maxSize=0.3, minEnergy=1, maxEnergy=1
    ///   sizeGrow=0, OneShot=true, Ellipsoid=(0.05,0.05,0.05)
    ///   localVelocity=(0, 0.3, 0), emission=5-10
    ///   ParticleRenderer: maxParticleSize=0.25, UV Animation 4x2 tiles, 1 cycle
    ///   ParticleAnimator colors (gray, alpha 10->180->255->180->10):
    ///     [0] (0.37, 0.37, 0.37, 0.04)
    ///     [1] (0.42, 0.42, 0.42, 0.71)
    ///     [2] (0.53, 0.53, 0.53, 1.00)
    ///     [3] (0.24, 0.24, 0.24, 0.71)
    ///     [4] (0.39, 0.39, 0.39, 0.04)
    ///   Material: CNMuzzleSmoke (Alpha Blended, MuzzleSmoke.PNG, tint 0.5/0.5/0.5/0.5)
    /// </summary>
    private void CreateCannonMuzzleSmoke()
    {
        _particleEmitter = gameObject.AddComponent<ParticleSystem>();
        _particleEmitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _particleEmitter.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 1f;                                         // minEnergy=1, maxEnergy=1
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);    // minSize=0.1, maxSize=0.3
        main.startSpeed = 0f;                                              // no random direction speed
        main.startColor = new Color(0.45f, 0.45f, 0.45f, 1f);          // mid-gray (color animated below)
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 10;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.283f);

        // OneShot=true with emission 5-10 → emit burst via OnShoot(), disable continuous emission
        var emission = _particleEmitter.emission;
        emission.enabled = false;

        // Shape: small sphere for spawn position scatter only (not direction)
        var shape = _particleEmitter.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;                                            // Ellipsoid=(0.05,0.05,0.05)

        // Original localVelocity=(0, 0.3, 0) — smoke drifts UPWARD only in local space
        var vel = _particleEmitter.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = 0f;
        vel.y = 0.3f;
        vel.z = 0f;

        // sizeGrow=0 → no size change over lifetime (disabled)

        // Texture sheet animation: 4x2 grid, 1 cycle (original UV Animation)
        var texSheet = _particleEmitter.textureSheetAnimation;
        texSheet.enabled = true;
        texSheet.numTilesX = 4;
        texSheet.numTilesY = 2;
        texSheet.cycleCount = 1;
        texSheet.animation = ParticleSystemAnimationType.WholeSheet;

        // Color animation from ParticleAnimator (5-key gray with alpha fade):
        //   (0.37,0.37,0.37,0.04) → (0.42,0.42,0.42,0.71) → (0.53,0.53,0.53,1.0)
        //   → (0.24,0.24,0.24,0.71) → (0.39,0.39,0.39,0.04)
        var colorOverLifetime = _particleEmitter.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.37f, 0.37f, 0.37f), 0f),
                new GradientColorKey(new Color(0.42f, 0.42f, 0.42f), 0.25f),
                new GradientColorKey(new Color(0.53f, 0.53f, 0.53f), 0.5f),
                new GradientColorKey(new Color(0.24f, 0.24f, 0.24f), 0.75f),
                new GradientColorKey(new Color(0.39f, 0.39f, 0.39f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.04f, 0f),      // alpha 10/255
                new GradientAlphaKey(0.71f, 0.25f),    // alpha 180/255
                new GradientAlphaKey(1.0f, 0.5f),      // alpha 255/255
                new GradientAlphaKey(0.71f, 0.75f),    // alpha 180/255
                new GradientAlphaKey(0.04f, 1f)         // alpha 10/255
            }
        );
        colorOverLifetime.color = grad;

        // Renderer with CNMuzzleSmoke Alpha Blended material
        var renderer = gameObject.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.25f;                            // original maxParticleSize

            // Load original CNMuzzleSmoke material (Alpha Blended, MuzzleSmoke.PNG, tint 0.5/0.5/0.5/0.5)
            Material smokeMat = LoadMaterial("CNMuzzleSmoke", "Legacy Shaders/Particles/Alpha Blended");
            renderer.material = smokeMat;
        }

        Debug.Log("[MuzzleSmokeFix] Cannon smoke on " + gameObject.name +
                  " weapon=" + (transform.parent != null ? transform.parent.name : "unknown") +
                  " size=0.1-0.3 life=1.0 speed=0.3 sizeGrow=0 texSheet=4x2" +
                  " mat=" + (renderer != null && renderer.material != null ? renderer.material.name : "none"));
    }

    /// <summary>
    /// Loads a material by name from Resources or scene, with shader fallback.
    /// </summary>
    private Material LoadMaterial(string matName, string fallbackShaderName)
    {
        // Try Resources paths
        string[] paths = {
            "ParticleMaterials/" + matName,
            "Artwork/WeaponEffects/Muzzle/MG/" + matName,
            "Artwork/WeaponEffects/Muzzle/CN/" + matName,
            "Artwork/WeaponEffects/Trails/MissileSmoke/" + matName
        };

        Material mat = null;
        foreach (string path in paths)
        {
            mat = Resources.Load<Material>(path);
            if (mat != null) return mat;
        }

        // Fallback: search loaded materials
        foreach (var r in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (r.name == matName)
                return r;
        }

        // Last resort: create material with fallback shader
        Shader shader = Shader.Find(fallbackShaderName);
        if (shader != null)
        {
            mat = new Material(shader);
            mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            return mat;
        }

        return null;
    }

    public override void OnShoot()
    {
        if (_particleEmitter)
        {
            gameObject.SetActive(true);

            // UMG muzzle flash: single bright particle
            // Cannon muzzle smoke: burst of 5-10 (original emission range)
            string goName = gameObject.name;
            if (goName.Contains("MGUMG") || goName.Contains("UMG"))
                _particleEmitter.Emit(1);
            else
                _particleEmitter.Emit(Random.Range(5, 11));
        }
    }

    public override void OnPostShoot()
    {
    }

    public override void Hide()
    {
    }
}
