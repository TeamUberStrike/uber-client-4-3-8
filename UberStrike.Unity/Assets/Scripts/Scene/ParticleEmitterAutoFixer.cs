using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Restores weapon impact/explosion particle effects broken by Unity 3.5.5 -> 2022 migration.
///
/// ROOT CAUSE: Migration stripped EllipsoidParticleEmitter + ParticleAnimator + ParticleRenderer,
/// auto-converting to ParticleSystem + ParticleSystemRenderer with DEFAULT settings.
/// 18/20 renderers got wrong materials, ALL lost their animator settings (UV animation,
/// sizeGrow, damping, colorAnimation, force, stretch modes).
///
/// DATA SOURCE: All settings extracted from compiled UberStrike 4.3.8 game data
/// (UberStrikeMain.unity3d) using UnityPy cab-aware pathID resolution.
///
/// FIXES APPLIED:
/// 1. REFERENCES: Wire ParticleSystem components into all 15 weapon configs
/// 2. MATERIALS: Verify correct materials + shaders + _TintColor on renderers
/// 3. COLOR: Fix ParticleColor (0,0,0,0) -> white for Emit() visibility
/// 4. TEXTURE SHEET: Configure TextureSheetAnimation for sprite sheet materials (4x4, 4x3, 2x2)
/// 5. RENDER MODE: Set Billboard/Stretch/Horizontal/Vertical per original ParticleRenderer
/// 6. COLOR OVER LIFETIME: Restore 5-key alpha fade animation from ParticleAnimator
/// 7. SIZE GROWTH: Additive per-frame sizeGrow via LegacySizeGrow component (exact legacy match)
/// 8. FORCE OVER LIFETIME: Restore gravity and forces from ParticleAnimator
/// </summary>
public static class ParticleEmitterAutoFixer
{
    static bool _hasRun;

    // Shader types matched EXACTLY to Unity 3.5.5 particle_diagnostic.txt:
    //   0 = "Particles/Alpha Blended"      → "Legacy Shaders/Particles/Alpha Blended"
    //   1 = "Particles/Additive"           → "Legacy Shaders/Particles/Additive"
    //   2 = "Particles/Additive (Soft)"    → "Particles/Additive (Soft) Legacy" (custom, matches 3.5 behavior)
    //   3 = "Particles/Additive (Soft)"    → "Legacy Shaders/Particles/Additive (Soft)" (built-in fallback)
    //
    // Type 2 custom shader matches Unity 3.5 exactly: 2x brightness, Blend One OneMinusSrcColor,
    // alpha does NOT affect RGB blending (same as original fixed-function).
    // Additional smoothstep(0, 0.5, texel.a) kills PSD import edge artifacts.
    static readonly string[] ShaderNames =
    {
        "Legacy Shaders/Particles/Alpha Blended",       // 0
        "Legacy Shaders/Particles/Additive",             // 1
        "Particles/Additive (Soft) Legacy",              // 2: custom, exact 3.5 match
        "Legacy Shaders/Particles/Additive (Soft)",      // 3: built-in fallback
        "HeatDistort_3.0",                               // 4: GrabPass heat distortion
        "Cmune/Projectile Additive"                      // 5: UberStrike custom, Blend SrcAlpha One, 2x * _TintColor
    };

    // GO name, material name, shader type, _TintColor RGBA
    // ALL values from Unity 3.5.5 particle_diagnostic.txt (the canonical source)
    static readonly string[][] MaterialMapping =
    {
        // --- Particles/Alpha Blended (type 0) ---
        new[] { "Wood",           "WoodAnimated",    "0", "0.5,0.5,0.5,0.5" },
        new[] { "Stone",          "Stone",           "0", "0.5,0.5,0.5,0.5" },
        new[] { "Grass",          "Grass",           "0", "0.5,0.5,0.5,0.5" },
        new[] { "Sand",           "Sand",            "0", "0.914,0.802,0.462,0.345" },
        new[] { "Splat",          "Sand",            "0", "0.914,0.802,0.462,0.345" },
        new[] { "WaterExtra",     "WaterExtraSplat", "0", "1.0,1.0,1.0,0.643" },
        new[] { "WaterDrops",     "WaterDrops",      "0", "0.374,0.879,0.954,0.502" },
        new[] { "PaintOrange",    "PaintOrange",     "0", "1.0,0.647,0.0,1.0" },
        new[] { "PaintGreen",     "PaintGreen",      "0", "0.0,0.832,1.0,1.0" },
        new[] { "PaintBlue",      "PaintGreen",      "0", "0.0,0.832,1.0,1.0" },  // SPDefault child, same mat as PaintGreen
        new[] { "PaintRed",       "PaintRed",        "0", "1.0,0.0,0.0,1.0" },    // MagmaRifle child
        // --- Particles/Additive (type 1) ---
        new[] { "Trail",          "Trail",           "1", "1.0,1.0,1.0,0.502" },
        new[] { "PlayerStar",     "BloodCross",      "1", "0.336,0.336,0.336,0.514" },
        new[] { "WaterCircle",    "WaterCircle",     "1", "0.380,0.345,0.302,0.514" },
        // --- Particles/Additive (Soft) (type 2 = custom matching 3.5 behavior) ---
        // Original 3.5 had no _TintColor property. Our custom shader multiplies _TintColor
        // in vertex shader, so (0.5,0.5,0.5,0.5) gives 2*0.5=1x. But original was 2x.
        // Use (1.0,1.0,1.0,0.5) to get the correct 2x brightness. Alpha 0.5 is neutral
        // (custom shader ignores alpha in blending via Blend One OneMinusSrcColor).
        new[] { "Metal",          "Spark",           "2", "1.0,1.0,1.0,0.5" },
        new[] { "Fire",           "Fire2Smoke",      "2", "1.0,1.0,1.0,0.5" },
        new[] { "ExplosionBlast", "Blast",           "2", "1.0,1.0,1.0,0.5" },
        new[] { "ExplosionDust",  "Dust",            "2", "1.0,1.0,1.0,0.5" },
        new[] { "ExplosionRing",  "Ring",            "2", "1.0,1.0,1.0,0.5" },
        new[] { "ExplosionSmoke", "Smoke",           "2", "1.0,1.0,1.0,0.5" },
        new[] { "ExplosionSpark", "Spark",           "2", "1.0,1.0,1.0,0.5" },
        new[] { "ExplosionTrail", "TrailExplosion",  "2", "1.0,1.0,1.0,0.5" },
        // --- HeatDistort_3.0 (type 4) — GrabPass heat distortion ---
        new[] { "HeatWave",       "HeatWave",        "4", "0.5,0.5,0.5,0.5" },  // _TintColor N/A (shader has no _TintColor)
    };

    // Original ParticleAnimator + ParticleRenderer settings from compiled UberStrike 4.3.8
    // Format: goName, sizeGrow, damping,
    //         uvTilesX, uvTilesY, uvCycles,
    //         legacyStretchMode (0=Billboard,3=Stretched,4=Horizontal,5=Vertical),
    //         lengthScale, velocityScale,
    //         forceY,
    //         color0_A, color1_A, color2_A, color3_A, color4_A  (alpha 0-255),
    //         maxParticleSize (screen-space fraction, 0.25 = 25% of screen height),
    //         color0_R, color0_G, color0_B, color1_R, color1_G, color1_B, ...
    //         (RGB values 0-255, only present for particles with non-white color animation)
    static readonly string[][] AnimatorData =
    {
        //                    sGrow   damp  uvX uvY cyc  str  len   vel   fY        a0   a1   a2   a3   a4     maxPS
        new[] { "Wood",          "0",   "1",  "4","4","3", "0","2","0",     "-15",  "180","180","255","180","10", "0.25" },
        new[] { "Stone",         "0",   "1",  "4","4","3", "0","2","0",     "-15",  "180","180","255","180","10", "0.25" },
        new[] { "Metal",        "-0.1", "1",  "1","1","1", "3","4","0.01",  "-10",  "180","180","255","180","10", "0.25" },
        new[] { "Grass",         "0",   "1",  "4","4","1", "0","2","0",     "-15",  "180","180","255","180","10", "0.25" },
        new[] { "Sand",          "0",   "1",  "4","4","1", "0","2","0",     "-15",  "180","180","255","180","10", "0.25" },
        new[] { "PlayerStar",    "0",   "1",  "1","1","1", "0","2","0",     "-1",   "180","180","255","180","10", "0.25" },
        new[] { "Splat",         "0",   "1",  "4","4","1", "0","2","0",     "-15",  "180","180","255","180","10", "0.25" },
        // Fire has warm RGB gradient: white→(255,221,221)→(255,203,203)→(255,174,174)
        new[] { "Fire",         "0.3",  "1",  "4","4","1", "0","2","0",     "1",    "128","125","255","128","10", "0.25",
                "255","255","255", "255","255","255", "255","221","221", "255","203","203", "255","174","174" },
        new[] { "Trail",         "0",   "1",  "1","1","1", "3","1","0",     "0",    "255","255","255","255","255","0.25" },
        new[] { "WaterCircle",  "1.5", "0.3", "1","1","1", "4","2","0",     "0",    "10","180","255","180","10", "0.25" },
        new[] { "WaterExtra",    "2",  "0.1", "1","1","1", "5","2","0",     "-4",   "3","180","162","70","10",  "0.25" },
        new[] { "WaterDrops",    "1",   "1",  "4","4","4", "0","2","0",     "-5",   "180","180","255","180","10","1000" },
        new[] { "PaintOrange",   "2",   "1",  "4","4","4", "0","2","0",     "-5",   "180","180","255","180","10", "0.25" },
        new[] { "PaintGreen",    "2",   "1",  "4","4","4", "0","2","0",     "-5",   "180","180","255","180","10", "0.25" },
        new[] { "PaintBlue",     "2",   "1",  "4","4","4", "0","2","0",     "-5",   "180","180","255","180","10", "0.25" },
        new[] { "PaintRed",      "2",   "1",  "4","4","4", "0","2","0",     "-5",   "180","180","255","180","10", "0.25" },
        new[] { "ExplosionBlast","-0.8","1",  "2","2","1", "0","0.5","0",   "0",    "10","180","255","180","10","1000" },
        new[] { "ExplosionDust", "0",   "1",  "1","1","1", "0","0.5","0",   "0.52", "10","180","255","180","10","1000" },
        new[] { "ExplosionRing","50",   "1",  "1","1","1", "4","0.5","0",   "0",    "255","204","153","0","10","1000" },
        new[] { "ExplosionSmoke","1",   "1",  "4","4","1", "0","2","0",     "0.53", "10","100","180","100","10","1000" },
        new[] { "ExplosionSpark","-0.1","1",  "4","3","1", "3","4","0",     "0",    "10","180","255","180","10", "0.25" },
        new[] { "ExplosionTrail","-0.1","1",  "4","3","1", "3","4","0",     "-1",   "10","180","255","180","10", "0.25" },
        new[] { "HeatWave",      "2",   "1",  "2","2","1", "0","1","0",     "0",    "10","180","255","180","10","1000" },
    };

    static readonly HashSet<string> KnownParticleNames = new HashSet<string>
    {
        "Wood", "Stone", "Metal", "Grass", "Sand", "Splat", "PlayerStar",
        "Fire", "Trail", "WaterCircle", "WaterExtra", "WaterDrops",
        "PaintOrange", "PaintGreen", "PaintBlue", "PaintRed",
        "ExplosionBlast", "ExplosionDust", "ExplosionRing",
        "ExplosionSmoke", "ExplosionSpark", "ExplosionTrail",
        "HeatWave"
    };

    public static bool IsKnownParticle(string name) { return KnownParticleNames.Contains(name); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _hasRun = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // Register for ALL scene loads — the controller may not exist in the first scene
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        // Also try immediately in case Latest is the initial scene
        TryRun();
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!_hasRun)
        {
            Debug.Log("[ParticleAutoFixer] Scene loaded: " + scene.name + " — attempting fix");
            TryRun();
        }
    }

    static void TryRun()
    {
        if (_hasRun) return;

        var controller = Object.FindObjectOfType<ParticleEffectController>();
        if (controller == null)
        {
            Debug.LogWarning("[ParticleAutoFixer] ParticleEffectController not found yet, will retry on next scene load");
            return;
        }
        _hasRun = true;
        Run(controller);
    }

    static void Run(ParticleEffectController controller)
    {
        Debug.Log("[ParticleAutoFixer] ========== STARTING ==========");
        Debug.Log("[ParticleAutoFixer] Controller found on: " + controller.gameObject.name +
            " scene=" + controller.gameObject.scene.name +
            " active=" + controller.gameObject.activeInHierarchy);

        // Step 1: Build GO name -> ParticleSystem map
        // CRITICAL FIX: Resources.FindObjectsOfTypeAll returns BOTH scene instances AND prefab
        // assets. Prefab assets are NOT rendered — configuring their modules has zero visual
        // effect. We MUST use scene instances (which have a valid scene) for changes to be visible.
        var psMap = new Dictionary<string, ParticleSystem>();
        int prefabSkipped = 0;
        foreach (var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())
        {
            if (ps == null || ps.gameObject == null) continue;
            string name = ps.gameObject.name;
            if (!KnownParticleNames.Contains(name)) continue;

            string sceneName = ps.gameObject.scene.name;
            bool isSceneInstance = !string.IsNullOrEmpty(sceneName);

            if (!psMap.ContainsKey(name))
            {
                psMap[name] = ps;
                Debug.Log("[ParticleAutoFixer] PS found: " + name +
                    " scene=\"" + sceneName + "\"" +
                    " id=" + ps.GetInstanceID() +
                    (isSceneInstance ? " [SCENE]" : " [ASSET]"));
            }
            else if (isSceneInstance && string.IsNullOrEmpty(psMap[name].gameObject.scene.name))
            {
                // Replace asset/prefab with scene instance — only scene instances are rendered
                var oldPS = psMap[name];
                psMap[name] = ps;
                Debug.Log("[ParticleAutoFixer] PS REPLACED: " + name +
                    " was ASSET id=" + oldPS.GetInstanceID() +
                    " -> now SCENE=\"" + sceneName + "\" id=" + ps.GetInstanceID());
            }
            else
            {
                prefabSkipped++;
            }
        }
        if (prefabSkipped > 0)
            Debug.Log("[ParticleAutoFixer] Skipped " + prefabSkipped + " duplicate PS (assets or extra scene copies)");

        if (psMap.Count == 0)
        {
            Debug.LogWarning("[ParticleAutoFixer] No impact particle systems found");
            return;
        }

        Debug.Log("[ParticleAutoFixer] Found " + psMap.Count + " particle systems: " +
                  string.Join(", ", psMap.Keys));

        // Step 2: Build material name -> Material map
        var matMap = new Dictionary<string, Material>();
        foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (mat == null || string.IsNullOrEmpty(mat.name)) continue;
            if (!matMap.ContainsKey(mat.name))
                matMap[mat.name] = mat;
        }

        // Step 2b: Fallback — load corrected materials from Resources folder
        // FindObjectsOfTypeAll won't find materials that aren't referenced by any loaded scene object.
        // Our corrected .mat files in Assets/Resources/ParticleMaterials/ are always loadable.
        int resourceLoaded = 0;
        foreach (var entry in MaterialMapping)
        {
            string matName = entry[1];
            if (!matMap.ContainsKey(matName))
            {
                Material loaded = Resources.Load<Material>("ParticleMaterials/" + matName);
                if (loaded != null)
                {
                    matMap[matName] = loaded;
                    resourceLoaded++;
                    Debug.Log("[ParticleAutoFixer] Loaded material from Resources: " + matName);
                }
                else
                {
                    Debug.LogWarning("[ParticleAutoFixer] Material NOT FOUND anywhere: " + matName);
                }
            }
        }
        if (resourceLoaded > 0)
            Debug.Log("[ParticleAutoFixer] Loaded " + resourceLoaded + " materials from Resources/ParticleMaterials/");

        // Step 3: Cache legacy shaders
        var shaderCache = new Shader[ShaderNames.Length];
        var shaderFallbacks = new string[]
        {
            "Particles/Alpha Blended",                          // 0: fallback for Alpha Blended
            "Particles/Additive",                               // 1: fallback for Additive
            "Particles/Additive (Soft) Legacy",                 // 2: same as primary (custom shader)
            "Legacy Shaders/Particles/Additive (Soft)",         // 3: same as primary (built-in)
            "HeatDistort_3.0",                                  // 4: same as primary (heat distortion)
            "Cmune/Projectile Additive"                         // 5: same as primary (UberStrike custom)
        };
        for (int i = 0; i < ShaderNames.Length; i++)
        {
            shaderCache[i] = Shader.Find(ShaderNames[i]);
            if (shaderCache[i] == null && i < shaderFallbacks.Length)
                shaderCache[i] = Shader.Find(shaderFallbacks[i]);
            if (shaderCache[i] == null)
                Debug.LogError("[ParticleAutoFixer] Shader NOT FOUND: " + ShaderNames[i]);
        }

        // Step 4: Fix materials, shaders, _TintColor
        int matsFixed = 0, shadersFixed = 0, tintsFixed = 0;
        var fixedShaderMats = new HashSet<string>();
        int tintColorID = Shader.PropertyToID("_TintColor");

        foreach (var entry in MaterialMapping)
        {
            string goName = entry[0], matName = entry[1];
            int shaderType = int.Parse(entry[2]);
            Color originalTint = ParseColor(entry[3]);

            if (!psMap.TryGetValue(goName, out ParticleSystem ps)) continue;

            // Configure main module
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;
            main.startSpeed = 0f;
            main.startSize = 1f; // safe default — overridden per-particle by EmitParams
            main.startLifetime = 10f; // safe default — overridden per-particle by EmitParams
            main.startColor = Color.white;
            main.gravityModifier = 0f; // gravity handled by ForceOverLifetime
            main.maxParticles = 1000;
            main.simulationSpeed = 1f;
            main.scalingMode = ParticleSystemScalingMode.Local;
            // CRITICAL: Zero out rotation — migration may have left random startRotation values.
            // In Stretch mode, non-zero startRotation causes particles to spin around the velocity axis,
            // making bullet trails look like spinning sticks instead of smooth streaks.
            main.startRotation3D = false;
            main.startRotation = 0f;

            // CRITICAL: Disable shape module — the deprecated Emit(pos,vel,size,life,color)
            // internally sets applyShapeToPosition=true, which adds a random position offset
            // from the migration-inherited shape (Sphere radius~1). This makes surface impact
            // particles spawn up to 1m away from the actual hit point.
            var shape = ps.shape;
            shape.enabled = false;

            // Fully disable emission module
            var emission = ps.emission;
            emission.enabled = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) continue;

            if (matMap.TryGetValue(matName, out Material correctMat))
            {
                // Fix shader
                if (shaderCache[shaderType] != null && !fixedShaderMats.Contains(matName))
                {
                    string curShader = correctMat.shader != null ? correctMat.shader.name : "NULL";
                    if (curShader != ShaderNames[shaderType])
                    {
                        correctMat.shader = shaderCache[shaderType];
                        shadersFixed++;
                    }
                    fixedShaderMats.Add(matName);

                    // Fix _TintColor
                    if (correctMat.HasProperty(tintColorID))
                    {
                        Color cur = correctMat.GetColor(tintColorID);
                        if (!ColorsClose(cur, originalTint))
                        {
                            correctMat.SetColor(tintColorID, originalTint);
                            tintsFixed++;
                        }
                    }
                }

                // Assign material
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.name != matName)
                {
                    renderer.sharedMaterial = correctMat;
                    matsFixed++;
                }
            }
        }

        // Step 5: Apply ParticleAnimator + ParticleRenderer settings from compiled game
        int animFixed = 0;
        foreach (var entry in AnimatorData)
        {
            string goName = entry[0];
            if (!psMap.TryGetValue(goName, out ParticleSystem ps)) continue;

            float sizeGrow = float.Parse(entry[1]);
            float damping = float.Parse(entry[2]);
            int uvX = int.Parse(entry[3]);
            int uvY = int.Parse(entry[4]);
            float uvCycles = float.Parse(entry[5]);
            int legacyStretch = int.Parse(entry[6]);
            float lenScale = float.Parse(entry[7]);
            float velScale = float.Parse(entry[8]);
            float forceY = float.Parse(entry[9]);
            float a0 = float.Parse(entry[10]) / 255f;
            float a1 = float.Parse(entry[11]) / 255f;
            float a2 = float.Parse(entry[12]) / 255f;
            float a3 = float.Parse(entry[13]) / 255f;
            float a4 = float.Parse(entry[14]) / 255f;
            float maxPS = float.Parse(entry[15]);

            // --- Disable ALL non-essential modules to eliminate migration artifacts ---
            // Migration from EllipsoidParticleEmitter may have enabled various modules
            // with incorrect settings. Only our explicitly configured modules should be active.
            {
                var main2 = ps.main;
                main2.simulationSpace = ParticleSystemSimulationSpace.World;
                main2.playOnAwake = false;
                main2.loop = false;
                main2.startSpeed = 0f;
                main2.startSize = 1f; // safe default — overridden per-particle by EmitParams
                main2.startLifetime = 10f; // safe default — overridden per-particle by EmitParams
                main2.startColor = Color.white; // safe default — overridden per-particle by EmitParams
                main2.gravityModifier = 0f;
                main2.maxParticles = 1000;
                main2.simulationSpeed = 1f;
                main2.scalingMode = ParticleSystemScalingMode.Local; // legacy particles used world-space sizes
                // CRITICAL: Zero out rotation in BOTH passes (MaterialMapping + AnimatorData).
                // In standalone builds, serialized scene data may re-apply after the first pass,
                // restoring migration-inherited random startRotation values that cause Stretched
                // particles (bullet trails) to render as horizontal/spinning lines.
                main2.startRotation3D = false;
                main2.startRotation = 0f;

                var sh = ps.shape; sh.enabled = false;
                var noise = ps.noise; noise.enabled = false;
                var velOL = ps.velocityOverLifetime; velOL.enabled = false;
                var rotOL = ps.rotationOverLifetime; rotOL.enabled = false;
                var rotBS = ps.rotationBySpeed; rotBS.enabled = false;
                var colBS = ps.colorBySpeed; colBS.enabled = false;
                var szBS = ps.sizeBySpeed; szBS.enabled = false;
                var extF = ps.externalForces; extF.enabled = false;
                var inhV = ps.inheritVelocity; inhV.enabled = false;
                var subE = ps.subEmitters; subE.enabled = false;
                var coll = ps.collision; coll.enabled = false;
                var lts = ps.lights; lts.enabled = false;
                var trl = ps.trails; trl.enabled = false;
                var trg = ps.trigger; trg.enabled = false;
                // Fully disable emission module — eliminates any migration burst interference
                var em = ps.emission; em.enabled = false;
            }

            // Ensure GO is active so ParticleSystemRenderer can render
            if (!ps.gameObject.activeInHierarchy)
            {
                ps.gameObject.SetActive(true);
            }

            // --- Render Mode ---
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                switch (legacyStretch)
                {
                    case 0:
                        renderer.renderMode = ParticleSystemRenderMode.Billboard;
                        break;
                    case 3: // Legacy Stretched -> Stretch
                        renderer.renderMode = ParticleSystemRenderMode.Stretch;
                        renderer.lengthScale = lenScale;
                        renderer.velocityScale = velScale;
                        break;
                    case 4: // Legacy HorizontalBillboard
                        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                        break;
                    case 5: // Legacy VerticalBillboard
                        renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
                        break;
                }
                renderer.maxParticleSize = maxPS;

                // Debug log for Stretched particles (Trail, Metal, sparks) to verify fix in builds
                if (legacyStretch == 3)
                {
                    Debug.Log("[ParticleAutoFixer] STRETCH '" + goName + "': renderMode=" + renderer.renderMode
                        + " lengthScale=" + renderer.lengthScale + " velocityScale=" + renderer.velocityScale
                        + " maxPS=" + renderer.maxParticleSize
                        + " startRotation=" + ps.main.startRotation.constant
                        + " startRotation3D=" + ps.main.startRotation3D);
                }
            }

            // --- Texture Sheet Animation (UV Animation) ---
            if (uvX > 1 || uvY > 1)
            {
                var tsa = ps.textureSheetAnimation;
                tsa.enabled = true;
                tsa.mode = ParticleSystemAnimationMode.Grid;
                tsa.numTilesX = uvX;
                tsa.numTilesY = uvY;
                tsa.cycleCount = Mathf.RoundToInt(uvCycles);
                tsa.animation = ParticleSystemAnimationType.WholeSheet;
                tsa.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
                // All particles start at frame 0 — matches original legacy ParticleRenderer
                // where UV animation always began at the first tile and progressed sequentially.
                tsa.startFrame = new ParticleSystem.MinMaxCurve(0f);
                // Frame over time: linear 0→1 over particle lifetime
                // Linear tangents — default Keyframe uses S-curve (tangent=0) which is wrong
                tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f,
                    new AnimationCurve(
                        new Keyframe(0f, 0f, 0f, 1f),
                        new Keyframe(1f, 1f, 1f, 0f)
                    ));
            }
            else
            {
                var tsa = ps.textureSheetAnimation;
                tsa.enabled = false;
            }

            // --- Color Over Lifetime (alpha fade + optional RGB from ParticleAnimator colorAnimation) ---
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient grad = new Gradient();
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(a0, 0.00f),
                    new GradientAlphaKey(a1, 0.25f),
                    new GradientAlphaKey(a2, 0.50f),
                    new GradientAlphaKey(a3, 0.75f),
                    new GradientAlphaKey(a4, 1.00f),
                };
                GradientColorKey[] colorKeys;
                // Check if entry has per-key RGB data (16+ elements means RGB present after index 15)
                if (entry.Length >= 31) // 16 base + 15 RGB values (5 keys * 3 channels)
                {
                    // Per-key RGB gradient (e.g., Fire warms from white to pink)
                    colorKeys = new GradientColorKey[5];
                    for (int k = 0; k < 5; k++)
                    {
                        float r = float.Parse(entry[16 + k * 3]) / 255f;
                        float g = float.Parse(entry[17 + k * 3]) / 255f;
                        float b = float.Parse(entry[18 + k * 3]) / 255f;
                        colorKeys[k] = new GradientColorKey(new Color(r, g, b), k * 0.25f);
                    }
                }
                else
                {
                    // All-white RGB (most particles)
                    colorKeys = new GradientColorKey[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f),
                    };
                }
                grad.SetKeys(colorKeys, alphaKeys);
                col.color = new ParticleSystem.MinMaxGradient(grad);
            }

            // --- Size Growth (additive, exactly replicates legacy ParticleAnimator.sizeGrow) ---
            // Legacy sizeGrow adds units/second to particle size. SizeOverLifetime is multiplicative
            // and can't replicate this because startSize varies per Emit() call (0.04 to 8.0).
            // Instead, use a per-frame component that modifies startSize via GetParticles/SetParticles.
            {
                var sol = ps.sizeOverLifetime;
                sol.enabled = false; // disable multiplicative module
            }
            if (Mathf.Abs(sizeGrow) > 0.001f)
            {
                var grow = ps.gameObject.GetComponent<LegacySizeGrow>();
                if (grow == null) grow = ps.gameObject.AddComponent<LegacySizeGrow>();
                grow.sizeGrow = sizeGrow;
            }

            // --- Force Over Lifetime (gravity from ParticleAnimator force.y) ---
            if (Mathf.Abs(forceY) > 0.001f)
            {
                var fol = ps.forceOverLifetime;
                fol.enabled = true;
                fol.x = 0f;
                fol.y = forceY;
                fol.z = 0f;
                fol.space = ParticleSystemSimulationSpace.World;
            }
            else
            {
                var fol = ps.forceOverLifetime;
                fol.enabled = false;
            }

            // --- Damping (VelocityOverLifetime speed modifier) ---
            if (damping < 0.99f)
            {
                var lv = ps.limitVelocityOverLifetime;
                lv.enabled = true;
                lv.dampen = 1f - damping; // dampen=0 means no slowdown, =1 means instant stop
            }

            // Start the particle system so manual Emit() calls work
            if (!ps.isPlaying) ps.Play();

            animFixed++;
        }

        // Step 0B: Create weapon-specific particle systems on SPDefault/MagmaRifle children
        // These children had their EllipsoidParticleEmitters stripped during migration without
        // ParticleSystem replacements. We add PS, configure materials (blue/red), and apply
        // the same animator settings as their DefaultPS counterparts.
        var weaponPsMap = SetupWeaponChildren(controller.transform, matMap, shaderCache);

        // Step 0C: Wire _heatWave field on ParticleEffectController
        // This serialized reference was likely broken during migration (EllipsoidParticleEmitter → ParticleSystem).
        // Without it, ShowHeatwaveEffect() silently skips → no heat distortion on Enigma Cannon explosions.
        ParticleSystem heatWavePS;
        if (psMap.TryGetValue("HeatWave", out heatWavePS))
        {
            var heatWaveField = typeof(ParticleEffectController).GetField("_heatWave", BindingFlags.NonPublic | BindingFlags.Instance);
            if (heatWaveField != null)
            {
                var currentVal = heatWaveField.GetValue(controller) as ParticleSystem;
                if (currentVal == null)
                {
                    heatWaveField.SetValue(controller, heatWavePS);
                    Debug.Log("[ParticleAutoFixer] Wired _heatWave → HeatWave PS (id=" + heatWavePS.GetInstanceID() + ")");
                }
                else
                {
                    Debug.Log("[ParticleAutoFixer] _heatWave already set (id=" + currentVal.GetInstanceID() + ")");
                }
            }

            // Validate HeatWave material textures at runtime
            var hwPS = heatWavePS;
            if (hwPS != null)
            {
                var hwRend = hwPS.GetComponent<ParticleSystemRenderer>();
                if (hwRend != null && hwRend.sharedMaterial != null)
                {
                    var hwMat = hwRend.sharedMaterial;
                    string bumpName = "NONE";
                    string bumpSize = "";
                    if (hwMat.HasProperty("_BumpMap"))
                    {
                        var bumpTex = hwMat.GetTexture("_BumpMap");
                        bumpName = bumpTex != null ? bumpTex.name : "NULL";
                        bumpSize = bumpTex != null ? " " + bumpTex.width + "x" + bumpTex.height : "";
                    }
                    float bumpAmt = hwMat.HasProperty("_BumpAmt") ? hwMat.GetFloat("_BumpAmt") : -1;
                    Debug.Log("[ParticleAutoFixer] HeatWave mat=" + hwMat.name +
                        " shader=" + (hwMat.shader != null ? hwMat.shader.name : "NULL") +
                        " _BumpMap=" + bumpName + bumpSize +
                        " _BumpAmt=" + bumpAmt +
                        " renderQueue=" + hwMat.renderQueue);

                    // Strong boost: original _BumpAmt=24.6 is too weak in linear pipeline.
                    // 64 gives strong concentrated distortion while keeping particle size small.
                    if (bumpAmt < 64f && hwMat.HasProperty("_BumpAmt"))
                    {
                        hwRend.material.SetFloat("_BumpAmt", 64f);
                        Debug.Log("[ParticleAutoFixer] HeatWave _BumpAmt boosted: " + bumpAmt + " -> 64");
                    }
                }
            }
        }

        // Step 6: Wire weapon config references + fix ParticleColor
        int refsFixed = WireAllConfigs(controller, psMap, weaponPsMap);

        // Create F11 debug handler
        CreateDebugHandler();

        // Summary log
        Debug.Log("[ParticleAutoFixer] === SUMMARY ===" +
                  "\n  Refs wired: " + refsFixed +
                  "\n  Materials fixed: " + matsFixed +
                  "\n  Shaders fixed: " + shadersFixed +
                  "\n  TintColors fixed: " + tintsFixed +
                  "\n  Animator settings applied: " + animFixed +
                  "\n  ParticleSystems found: " + psMap.Count +
                  "\n  Weapon-specific PS: " + weaponPsMap.Count);

        // Step 0D: Swap explosion materials to Legacy Clean (Blend One OneMinusSrcColor).
        // The self-saturating blend mode is CRITICAL for correct explosion visuals:
        //   framebuffer = src + dst * (1 - src)
        // Overlapping particles converge smoothly toward white, hiding individual sprite
        // edges and giving the characteristic soft/round glow of the original 3.5 explosions.
        // Blend SrcAlpha One (Projectile Additive) was tried but stacks linearly — makes
        // the center overbright, shows sharp sprite sheet edges, and kills dust/spark layers.
        //
        // _TintColor controls brightness for the linear pipeline. The original gamma pipeline
        // used 2.0x (no _TintColor). In linear with sRGBTexture=0, gamma-encoded texture
        // values appear brighter, so we use _TintColor=(0.5,0.5,0.5,0.5) for 1.0x brightness.
        // Use psr.material (auto-instance) to avoid affecting shared materials
        // (e.g., Spark is shared between Metal surface hits and ExplosionSpark).
        Shader cleanShader = Shader.Find("Particles/Additive (Soft) Legacy Clean");
        if (cleanShader != null)
        {
            int shaderSwaps = 0;
            string[] explosionPSNames = { "ExplosionBlast", "ExplosionDust", "ExplosionSmoke",
                "ExplosionSpark", "ExplosionRing", "ExplosionTrail" };
            foreach (string psName in explosionPSNames)
            {
                ParticleSystem targetPS;
                if (psMap.TryGetValue(psName, out targetPS))
                {
                    var psr = targetPS.GetComponent<ParticleSystemRenderer>();
                    if (psr != null && psr.sharedMaterial != null)
                    {
                        // Use .material to auto-create instance (won't affect shared materials)
                        Material matInstance = psr.material;
                        string oldShader = matInstance.shader.name;
                        matInstance.shader = cleanShader;
                        // Override _TintColor for linear pipeline brightness compensation.
                        // Step 4 set (1,1,1,0.5) for 2x brightness (correct for original gamma
                        // pipeline). In linear with sRGBTexture=0, gamma-encoded values appear
                        // brighter after display gamma. 0.7x (via 0.35 * 2.0) balances brightness
                        // with the new alpha modulation in the shader (col.rgb *= vertex.a).
                        matInstance.SetColor("_TintColor", new Color(0.35f, 0.35f, 0.35f, 0.5f));
                        shaderSwaps++;
                        Debug.Log("[ParticleAutoFixer] Explosion shader fix: " + psName +
                            " mat=" + matInstance.name +
                            " was \"" + oldShader + "\" -> Legacy Clean (OneMinusSrcColor, 1x tint)");
                    }
                }
            }
            Debug.Log("[ParticleAutoFixer] Explosion shader fix: " + shaderSwaps +
                " materials swapped to Legacy Clean");
        }
        else
        {
            Debug.LogWarning("[ParticleAutoFixer] Clean shader not found: Particles/Additive (Soft) Legacy Clean");
        }

        // Step 7: Pink materials cleanup — disable renderers on ALL unfixed particle systems
        // that still have InternalErrorShader or Smoke2 from migration. Catches:
        // - MGUMG/Trail (not yet handled — UMG config uses DefaultPS/Trail)
        // - Any other particle system with broken materials
        int pinkDisabled = 0;
        foreach (var ps2 in Resources.FindObjectsOfTypeAll<ParticleSystem>())
        {
            if (ps2 == null || ps2.gameObject == null) continue;
            if (string.IsNullOrEmpty(ps2.gameObject.scene.name)) continue; // skip prefabs

            var rend2 = ps2.GetComponent<ParticleSystemRenderer>();
            if (rend2 == null) continue;

            string shaderName = rend2.sharedMaterial != null && rend2.sharedMaterial.shader != null
                ? rend2.sharedMaterial.shader.name : "";
            string matName2 = rend2.sharedMaterial != null ? rend2.sharedMaterial.name : "";

            bool isBroken = shaderName == "Hidden/InternalErrorShader" ||
                            shaderName.Contains("InternalError") ||
                            matName2 == "Smoke2";

            if (isBroken)
            {
                // Disable renderer so it can't produce pink boxes
                rend2.enabled = false;
                // Also disable emission and playOnAwake to prevent zombie emissions
                var main2b = ps2.main;
                main2b.playOnAwake = false;
                main2b.loop = false;
                var em2 = ps2.emission;
                em2.enabled = false;
                if (ps2.isPlaying) ps2.Stop();

                pinkDisabled++;
                Debug.LogWarning("[ParticleAutoFixer] PINK CLEANUP: Disabled " +
                    ps2.gameObject.name + " (mat=" + matName2 + " shader=" + shaderName +
                    " parent=" + (ps2.transform.parent != null ? ps2.transform.parent.name : "none") +
                    " scene=" + ps2.gameObject.scene.name + ")");
            }
        }
        if (pinkDisabled > 0)
            Debug.Log("[ParticleAutoFixer] Pink cleanup: disabled " + pinkDisabled + " broken particle renderers");

        // Per-particle diagnostic — focus on rendering-critical settings
        foreach (var entry in MaterialMapping)
        {
            string goName = entry[0];
            if (!psMap.TryGetValue(goName, out ParticleSystem ps)) continue;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend == null) { Debug.LogWarning("[ParticleAutoFixer] " + goName + " HAS NO RENDERER!"); continue; }

            var grow = ps.gameObject.GetComponent<LegacySizeGrow>();
            string growInfo = grow != null ? " sizeGrow=" + grow.sizeGrow : "";

            // TSA details
            var tsa = ps.textureSheetAnimation;
            string tsaInfo = tsa.enabled ?
                " TSA[mode=" + tsa.mode + " " + tsa.numTilesX + "x" + tsa.numTilesY +
                " cyc=" + tsa.cycleCount + " anim=" + tsa.animation +
                " time=" + tsa.timeMode + "]" : " TSA=off";

            string matName = rend.sharedMaterial != null ? rend.sharedMaterial.name : "NULL_MAT";
            string shaderName = rend.sharedMaterial?.shader != null ? rend.sharedMaterial.shader.name : "NULL_SHADER";
            string texName = rend.sharedMaterial?.mainTexture != null ? rend.sharedMaterial.mainTexture.name : "NULL_TEX";

            // Check transform scale — if zero, particles are invisible
            Vector3 localScale = ps.transform.localScale;
            Vector3 lossyScale = ps.transform.lossyScale;
            string scaleInfo = " localScale=" + localScale.ToString("F2") + " worldScale=" + lossyScale.ToString("F2");

            string psScene = ps.gameObject.scene.name;
            Debug.Log("[ParticleAutoFixer] " + goName +
                " scene=\"" + psScene + "\"" +
                " id=" + ps.GetInstanceID() +
                " active=" + ps.gameObject.activeInHierarchy +
                " layer=" + ps.gameObject.layer +
                " rendEnabled=" + rend.enabled +
                " mat=" + matName +
                " shader=" + shaderName +
                " tex=" + texName +
                " render=" + rend.renderMode +
                " maxPS=" + rend.maxParticleSize +
                scaleInfo +
                tsaInfo + growInfo +
                " emit=" + ps.emission.enabled +
                " shape=" + ps.shape.enabled +
                " playing=" + ps.isPlaying);
        }

        // Post-fix verification: emit a test particle from Stone to confirm pipeline
        if (psMap.TryGetValue("Stone", out ParticleSystem testPS))
        {
            if (!testPS.isPlaying) testPS.Play();
            var ep = new ParticleSystem.EmitParams();
            ep.position = Vector3.zero;
            ep.velocity = Vector3.up;
            ep.startSize = 1f;
            ep.startLifetime = 3f;
            ep.startColor = new Color32(255, 255, 255, 255);
            testPS.Emit(ep, 1);
            Debug.Log("[ParticleAutoFixer] POST-FIX TEST: Emitted 1 white particle from Stone, count=" + testPS.particleCount);
        }

        // CRITICAL: In standalone builds, asset bundle scene data may re-apply serialized
        // ParticleSystem values AFTER sceneLoaded fires, overwriting our startRotation=0 fix.
        // Spawn a guard that re-applies the fix for several frames to catch any late overwrites.
        // Same pattern as BeastLightmapLoader's frame-delayed assignment.
        var allStretched = new List<ParticleSystem>();
        foreach (var entry in AnimatorData)
        {
            int legacyStr = int.Parse(entry[6]);
            if (legacyStr == 3 && psMap.TryGetValue(entry[0], out ParticleSystem stretchPS))
                allStretched.Add(stretchPS);
        }
        if (allStretched.Count > 0)
        {
            ParticleRotationGuard.Create(allStretched.ToArray());
        }
    }

    static Color ParseColor(string rgba)
    {
        string[] parts = rgba.Split(',');
        if (parts.Length == 4)
            return new Color(float.Parse(parts[0]), float.Parse(parts[1]),
                             float.Parse(parts[2]), float.Parse(parts[3]));
        return new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }

    static bool ColorsClose(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.02f &&
               Mathf.Abs(a.g - b.g) < 0.02f &&
               Mathf.Abs(a.b - b.b) < 0.02f &&
               Mathf.Abs(a.a - b.a) < 0.02f;
    }

    static int WireAllConfigs(ParticleEffectController controller, Dictionary<string, ParticleSystem> psMap, Dictionary<string, ParticleSystem> weaponPsMap)
    {
        int totalFixed = 0;

        FieldInfo dataField = typeof(ParticleEffectController).GetField("_allWeaponData",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (dataField == null)
        {
            Debug.LogError("[ParticleAutoFixer] Cannot find _allWeaponData field");
            return 0;
        }

        System.Array allData = dataField.GetValue(controller) as System.Array;
        if (allData == null || allData.Length == 0)
        {
            Debug.LogError("[ParticleAutoFixer] _allWeaponData is null or empty (length=" +
                (allData != null ? allData.Length.ToString() : "null") + ")");
            return 0;
        }

        Debug.Log("[ParticleAutoFixer] _allWeaponData has " + allData.Length + " entries");

        FieldInfo impactField = typeof(ParticleCobfigurationPerWeapon).GetField(
            "_weaponImpactEffectConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);
        if (impactField == null)
        {
            Debug.LogError("[ParticleAutoFixer] Cannot find _weaponImpactEffectConfiguration field");
            return 0;
        }

        int configIndex = 0;
        foreach (object configObj in allData)
        {
            configIndex++;
            if (configObj == null) { Debug.LogWarning("[ParticleAutoFixer] Config entry " + configIndex + " is NULL"); continue; }

            FieldInfo cfgField = configObj.GetType().GetField("Configuration");
            if (cfgField == null) { Debug.LogWarning("[ParticleAutoFixer] Config " + configIndex + " has no Configuration field"); continue; }

            var perWeapon = cfgField.GetValue(configObj) as ParticleCobfigurationPerWeapon;
            if (perWeapon == null) { Debug.LogWarning("[ParticleAutoFixer] Config " + configIndex + " Configuration is null"); continue; }

            var impact = impactField.GetValue(perWeapon) as WeaponImpactEffectConfiguration;
            if (impact == null) { Debug.LogWarning("[ParticleAutoFixer] Config " + configIndex + " impact is null"); continue; }

            // Log type for tracing
            FieldInfo typeField = configObj.GetType().GetField("Type");
            string typeStr = typeField != null ? typeField.GetValue(configObj).ToString() : "?";
            Debug.Log("[ParticleAutoFixer] Wiring config " + configIndex + " Type=" + typeStr +
                " surface=" + (impact.SurfaceParameterSet != null) +
                " fire=" + (impact.FireParticleConfigurationForInstantHit != null) +
                " explosion=" + (impact.ExplosionParameterSet != null));

            // Wire surface effects
            if (impact.SurfaceParameterSet != null)
            {
                totalFixed += WirePC(impact.SurfaceParameterSet.WoodEffect, psMap, "Wood");
                totalFixed += WirePC(impact.SurfaceParameterSet.StoneEffect, psMap, "Stone");
                totalFixed += WirePC(impact.SurfaceParameterSet.MetalEffect, psMap, "Metal");
                totalFixed += WirePC(impact.SurfaceParameterSet.GrassEffect, psMap, "Grass");
                totalFixed += WirePC(impact.SurfaceParameterSet.SandEffect, psMap, "Sand");
                totalFixed += WirePC(impact.SurfaceParameterSet.Splat, psMap, "Splat");
                totalFixed += WireFPC(impact.SurfaceParameterSet.WaterCircleEffect, psMap, "WaterCircle");
                totalFixed += WireFPC(impact.SurfaceParameterSet.WaterExtraSplashEffect, psMap, "WaterExtra");
            }

            totalFixed += WireFPC(impact.FireParticleConfigurationForInstantHit, psMap, "Fire");
            totalFixed += WireTPC(impact.TrailParticleConfigurationForInstantHit, psMap, "Trail");

            if (impact.ExplosionParameterSet != null)
            {
                var exp = impact.ExplosionParameterSet;
                totalFixed += WireEBP(exp.BlastParameters, psMap, "ExplosionBlast");
                totalFixed += WireEDP(exp.DustParameters, psMap, "ExplosionDust");
                totalFixed += WireERP(exp.RingParameters, psMap, "ExplosionRing");
                totalFixed += WireEBP(exp.SmokeParameters, psMap, "ExplosionSmoke");
                totalFixed += WireESP(exp.SparkParameters, psMap, "ExplosionSpark");
                totalFixed += WireESP(exp.TrailParameters, psMap, "ExplosionTrail");
            }
        }

        // Second pass: overwrite explosion emitters for weapon-specific configs
        // SplattergunDefault uses SPDefault children (blue particles)
        // MagmaRifle uses MagmaRifle children (red particles)
        if (weaponPsMap != null && weaponPsMap.Count > 0)
        {
            foreach (object configObj2 in allData)
            {
                if (configObj2 == null) continue;
                FieldInfo typeField2 = configObj2.GetType().GetField("Type");
                if (typeField2 == null) continue;
                string typeStr2 = typeField2.GetValue(configObj2).ToString();

                string prefix = null;
                if (typeStr2 == "SplattergunDefault") prefix = "SPDefault";
                else if (typeStr2 == "MagmaRifle") prefix = "MagmaRifle";
                else if (typeStr2 == "EnigmaCannon") prefix = "CNEnigmaCannon";
                else continue;

                FieldInfo cfgField2 = configObj2.GetType().GetField("Configuration");
                if (cfgField2 == null) continue;
                var perWeapon2 = cfgField2.GetValue(configObj2) as ParticleCobfigurationPerWeapon;
                if (perWeapon2 == null) continue;
                var impact2 = impactField.GetValue(perWeapon2) as WeaponImpactEffectConfiguration;
                if (impact2 == null || impact2.ExplosionParameterSet == null) continue;

                var exp2 = impact2.ExplosionParameterSet;
                int rewired = 0;
                ParticleSystem wps;

                if (exp2.BlastParameters != null && weaponPsMap.TryGetValue(prefix + "/ExplosionBlast", out wps))
                { exp2.BlastParameters.ParticleEmitter = wps; rewired++; }
                if (exp2.DustParameters != null && weaponPsMap.TryGetValue(prefix + "/ExplosionDust", out wps))
                { exp2.DustParameters.ParticleEmitter = wps; rewired++; }
                if (exp2.SmokeParameters != null && weaponPsMap.TryGetValue(prefix + "/ExplosionSmoke", out wps))
                { exp2.SmokeParameters.ParticleEmitter = wps; rewired++; }
                if (exp2.SparkParameters != null && weaponPsMap.TryGetValue(prefix + "/ExplosionSpark", out wps))
                { exp2.SparkParameters.ParticleEmitter = wps; rewired++; }
                if (exp2.TrailParameters != null && weaponPsMap.TryGetValue(prefix + "/ExplosionTrail", out wps))
                { exp2.TrailParameters.ParticleEmitter = wps; rewired++; }

                totalFixed += rewired;
                Debug.Log("[ParticleAutoFixer] WEAPON OVERRIDE: " + typeStr2 + " -> " + prefix + " (" + rewired + " explosion refs rewired)");
            }
        }

        return totalFixed;
    }

    static int WirePC(ParticleConfiguration c, Dictionary<string, ParticleSystem> m, string n)
    {
        if (c == null) return 0;
        if (!m.TryGetValue(n, out ParticleSystem ps)) return 0;
        // ALWAYS wire + fix color — even if ref was already set, color may still be (0,0,0,0)
        bool wasNull = c.ParticleEmitter == null;
        c.ParticleEmitter = ps;
        if (c.ParticleColor.a < 0.01f) c.ParticleColor = Color.white;

        // Sand particle size fix: serialized data has 0.05m (5cm) which is an extreme outlier
        // compared to Stone (0.3m), Wood (0.2m), Grass (0.1-0.6m). At 5cm, particles are nearly
        // invisible and appear as hard dots. Boost undersized Sand configs to match Stone/Wood range.
        // Only override configs with clearly too-small sizes (< 0.15) to preserve the few weapon
        // configs that intentionally use larger Sand sizes (0.5m on some weapons).
        if (n == "Sand" && c.ParticleMaxSize < 0.15f)
        {
            c.ParticleMinSize = 0.25f;
            c.ParticleMaxSize = 0.35f;
            c.ParticleMinLiveTime = 0.8f;
            c.ParticleMaxLiveTime = 1.5f;
        }

        return wasNull ? 1 : 0;
    }

    static int WireFPC(FireParticleConfiguration c, Dictionary<string, ParticleSystem> m, string n)
    {
        if (c == null) return 0;
        if (!m.TryGetValue(n, out ParticleSystem ps)) return 0;
        bool wasNull = c.ParticleEmitter == null;
        c.ParticleEmitter = ps;
        if (c.ParticleColor.a < 0.01f) c.ParticleColor = Color.white;
        return wasNull ? 1 : 0;
    }

    static int WireTPC(TrailParticleConfiguration c, Dictionary<string, ParticleSystem> m, string n)
    {
        if (c == null) return 0;
        if (!m.TryGetValue(n, out ParticleSystem ps)) return 0;
        bool wasNull = c.ParticleEmitter == null;
        c.ParticleEmitter = ps;
        if (c.ParticleColor.a < 0.01f) c.ParticleColor = Color.white;
        return wasNull ? 1 : 0;
    }

    static int WireEBP(ExplosionBaseParameters c, Dictionary<string, ParticleSystem> m, string n)
    {
        if (c == null) return 0;
        if (!m.TryGetValue(n, out ParticleSystem ps)) return 0;
        bool wasNull = c.ParticleEmitter == null;
        c.ParticleEmitter = ps;
        return wasNull ? 1 : 0;
    }

    static int WireEDP(ExplosionDustParameters c, Dictionary<string, ParticleSystem> m, string n)
    {
        if (c == null) return 0;
        if (!m.TryGetValue(n, out ParticleSystem ps)) return 0;
        bool wasNull = c.ParticleEmitter == null;
        c.ParticleEmitter = ps;
        return wasNull ? 1 : 0;
    }

    static int WireERP(ExplosionRingParameters c, Dictionary<string, ParticleSystem> m, string n)
    {
        if (c == null) return 0;
        if (!m.TryGetValue(n, out ParticleSystem ps)) return 0;
        bool wasNull = c.ParticleEmitter == null;
        c.ParticleEmitter = ps;
        return wasNull ? 1 : 0;
    }

    static int WireESP(ExplosionSphericParameters c, Dictionary<string, ParticleSystem> m, string n)
    {
        if (c == null) return 0;
        if (!m.TryGetValue(n, out ParticleSystem ps)) return 0;
        bool wasNull = c.ParticleEmitter == null;
        c.ParticleEmitter = ps;
        return wasNull ? 1 : 0;
    }

    // Create debug key handler for particle visualization
    static void CreateDebugHandler()
    {
        var go = new GameObject("[ParticleDebug]");
        go.AddComponent<ParticleDebugController>();
        Object.DontDestroyOnLoad(go);
    }

    /// <summary>
    /// Creates ParticleSystem components on weapon-specific children (SPDefault=blue, MagmaRifle=red)
    /// that lost their EllipsoidParticleEmitter during migration. Assigns correct materials/shaders
    /// and applies the same animator settings as their DefaultParticleSystems counterparts.
    /// Returns a dictionary keyed by "ParentName/ChildName" for use in weapon config wiring.
    /// </summary>
    static Dictionary<string, ParticleSystem> SetupWeaponChildren(
        Transform controllerRoot,
        Dictionary<string, Material> matMap,
        Shader[] shaderCache)
    {
        var weaponPs = new Dictionary<string, ParticleSystem>();

        // Weapon-specific material definitions from Unity 3.5.5 particle_diagnostic.txt
        // Format: parentName, childName, materialName, shaderType, tintColor
        // Animator settings are IDENTICAL to DefaultPS counterparts (looked up by childName from AnimatorData)
        string[][] defs =
        {
            // SPDefault (Splattergun blue explosions — Cmune/Projectile Additive shader)
            new[] { "SPDefault", "ExplosionBlast", "BlastBlue",  "5", "0.5,0.5,0.5,0.5" },
            new[] { "SPDefault", "ExplosionDust",  "DustBlue",   "5", "0.5,0.5,0.5,0.5" },
            new[] { "SPDefault", "ExplosionSmoke", "SmokeBlue",  "5", "0.5,0.5,0.5,0.5" },
            new[] { "SPDefault", "ExplosionSpark", "SparkBlue",  "0", "0.5,0.5,0.5,0.5" },
            new[] { "SPDefault", "ExplosionTrail", "TrailBlue",  "5", "0.5,0.5,0.5,0.5" },
            new[] { "SPDefault", "PaintBlue",      "PaintGreen", "0", "0.0,0.832,1.0,1.0" },
            // MagmaRifle (red explosions — ALL red materials, mirrors SPDefault blue pattern)
            new[] { "MagmaRifle", "ExplosionBlast", "BlastRed",  "5", "0.5,0.5,0.5,0.5" },
            new[] { "MagmaRifle", "ExplosionDust",  "DustRed",   "5", "0.5,0.1,0.05,0.5" },
            new[] { "MagmaRifle", "ExplosionSmoke", "SmokeRed",  "5", "0.5,0.1,0.05,0.5" },
            new[] { "MagmaRifle", "ExplosionSpark", "SparkRed",  "0", "0.5,0.5,0.5,0.5" },
            new[] { "MagmaRifle", "ExplosionTrail", "TrailRed",  "5", "0.5,0.5,0.5,0.5" },
            new[] { "MagmaRifle", "PaintRed",       "PaintRed",  "0", "1.0,0.0,0.0,1.0" },
            // CNEnigmaCannon (Enigma Cannon + Dragon blue/cyan explosions — same blue materials as SPDefault)
            // Original game had blue-tinted explosions distinct from default orange cannons.
            // Uses Cmune/Projectile Additive (type 5) for blast/dust/smoke/trail, Alpha Blended (type 0) for spark.
            new[] { "CNEnigmaCannon", "ExplosionBlast", "BlastBlue",  "5", "0.5,0.5,0.5,0.5" },
            new[] { "CNEnigmaCannon", "ExplosionDust",  "DustBlue",   "5", "0.5,0.5,0.5,0.5" },
            new[] { "CNEnigmaCannon", "ExplosionSmoke", "SmokeBlue",  "5", "0.5,0.5,0.5,0.5" },
            new[] { "CNEnigmaCannon", "ExplosionSpark", "SparkBlue",  "0", "0.5,0.5,0.5,0.5" },
            new[] { "CNEnigmaCannon", "ExplosionTrail", "TrailBlue",  "5", "0.5,0.5,0.5,0.5" },
        };

        int tintColorID = Shader.PropertyToID("_TintColor");

        foreach (var def in defs)
        {
            string parentName = def[0];
            string childName = def[1];
            string matName = def[2];
            int shaderType = int.Parse(def[3]);
            Color tint = ParseColor(def[4]);

            Transform parent = controllerRoot.Find(parentName);
            if (parent == null)
            {
                // Create parent GO — CNEnigmaCannon may not exist as direct child if
                // it was stripped during migration or never instantiated from prefab
                var parentGO = new GameObject(parentName);
                parentGO.transform.SetParent(controllerRoot, false);
                parent = parentGO.transform;
                Debug.Log("[ParticleAutoFixer] Created weapon parent GO: " + parentName);
            }

            Transform child = parent.Find(childName);
            if (child == null)
            {
                // Create child GO — CNEnigmaCannon had no particle children (migration
                // stripped EllipsoidParticleEmitters entirely, leaving empty parent)
                var childGO = new GameObject(childName);
                childGO.transform.SetParent(parent, false);
                child = childGO.transform;
                Debug.Log("[ParticleAutoFixer] Created weapon child GO: " + parentName + "/" + childName);
            }

            // Add ParticleSystem if missing (migration stripped EllipsoidParticleEmitter without replacement)
            var ps = child.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = child.gameObject.AddComponent<ParticleSystem>();
            }

            var rend = child.GetComponent<ParticleSystemRenderer>();
            if (rend == null)
            {
                rend = child.gameObject.AddComponent<ParticleSystemRenderer>();
            }

            if (!child.gameObject.activeInHierarchy)
                child.gameObject.SetActive(true);

            // Look up AnimatorData by childName (settings identical to DefaultPS counterpart)
            string[] animEntry = null;
            foreach (var ae in AnimatorData)
            {
                if (ae[0] == childName) { animEntry = ae; break; }
            }

            if (animEntry != null)
            {
                ApplyPSSettings(ps, rend, animEntry);
            }
            else
            {
                // Minimal config for particles without AnimatorData entry
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.playOnAwake = false;
                main.loop = false;
                main.startSpeed = 0f;
                main.startSize = 1f;
                main.startLifetime = 10f;
                main.startColor = Color.white;
                main.gravityModifier = 0f;
                main.maxParticles = 1000;
                var shape = ps.shape; shape.enabled = false;
                var emission = ps.emission; emission.enabled = false;
            }

            // Assign material with correct shader and tint
            // For shared materials (Dust, Smoke), the shader is already set by the main code path
            Material mat = null;
            if (!matMap.TryGetValue(matName, out mat))
            {
                mat = Resources.Load<Material>("ParticleMaterials/" + matName);
                if (mat != null)
                {
                    matMap[matName] = mat;
                    Debug.Log("[ParticleAutoFixer] Weapon: loaded material from Resources: " + matName);
                }
            }

            if (mat != null)
            {
                if (shaderType < shaderCache.Length && shaderCache[shaderType] != null)
                {
                    if (mat.shader == null || mat.shader.name != ShaderNames[shaderType])
                        mat.shader = shaderCache[shaderType];
                }
                if (mat.HasProperty(tintColorID))
                    mat.SetColor(tintColorID, tint);
                rend.sharedMaterial = mat;
                rend.enabled = true;
            }
            else
            {
                rend.enabled = false; // no material — hide to prevent gray squares
                Debug.LogWarning("[ParticleAutoFixer] Weapon mat not found: " + matName + " — renderer disabled");
            }

            if (!ps.isPlaying) ps.Play();

            string key = parentName + "/" + childName;
            weaponPs[key] = ps;
            Debug.Log("[ParticleAutoFixer] Weapon PS: " + key +
                " mat=" + (mat != null ? mat.name : "NULL") +
                " shader=" + (mat != null && mat.shader != null ? mat.shader.name : "NULL") +
                " id=" + ps.GetInstanceID());
        }

        Debug.Log("[ParticleAutoFixer] Weapon children: " + weaponPs.Count + " PS created");
        return weaponPs;
    }

    /// <summary>
    /// Applies ParticleAnimator + ParticleRenderer settings from AnimatorData to a ParticleSystem.
    /// Extracted from Step 5 to be reusable for both DefaultPS and weapon-specific particles.
    /// </summary>
    static void ApplyPSSettings(ParticleSystem ps, ParticleSystemRenderer renderer, string[] animEntry)
    {
        float sizeGrow = float.Parse(animEntry[1]);
        float damping = float.Parse(animEntry[2]);
        int uvX = int.Parse(animEntry[3]);
        int uvY = int.Parse(animEntry[4]);
        float uvCycles = float.Parse(animEntry[5]);
        int legacyStretch = int.Parse(animEntry[6]);
        float lenScale = float.Parse(animEntry[7]);
        float velScale = float.Parse(animEntry[8]);
        float forceY = float.Parse(animEntry[9]);
        float a0 = float.Parse(animEntry[10]) / 255f;
        float a1 = float.Parse(animEntry[11]) / 255f;
        float a2 = float.Parse(animEntry[12]) / 255f;
        float a3 = float.Parse(animEntry[13]) / 255f;
        float a4 = float.Parse(animEntry[14]) / 255f;
        float maxPS = float.Parse(animEntry[15]);

        // Main module
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = false;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startLifetime = 10f;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.maxParticles = 1000;
        main.simulationSpeed = 1f;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.startRotation3D = false;
        main.startRotation = 0f;

        // Disable all non-essential modules
        var sh = ps.shape; sh.enabled = false;
        var noise = ps.noise; noise.enabled = false;
        var velOL = ps.velocityOverLifetime; velOL.enabled = false;
        var rotOL = ps.rotationOverLifetime; rotOL.enabled = false;
        var rotBS = ps.rotationBySpeed; rotBS.enabled = false;
        var colBS = ps.colorBySpeed; colBS.enabled = false;
        var szBS = ps.sizeBySpeed; szBS.enabled = false;
        var extF = ps.externalForces; extF.enabled = false;
        var inhV = ps.inheritVelocity; inhV.enabled = false;
        var subE = ps.subEmitters; subE.enabled = false;
        var coll = ps.collision; coll.enabled = false;
        var lts = ps.lights; lts.enabled = false;
        var trl = ps.trails; trl.enabled = false;
        var trg = ps.trigger; trg.enabled = false;
        var em = ps.emission; em.enabled = false;

        // Render mode
        if (renderer != null)
        {
            switch (legacyStretch)
            {
                case 0: renderer.renderMode = ParticleSystemRenderMode.Billboard; break;
                case 3:
                    renderer.renderMode = ParticleSystemRenderMode.Stretch;
                    renderer.lengthScale = lenScale;
                    renderer.velocityScale = velScale;
                    break;
                case 4: renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; break;
                case 5: renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard; break;
            }
            renderer.maxParticleSize = maxPS;
        }

        // Texture sheet animation
        if (uvX > 1 || uvY > 1)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = uvX;
            tsa.numTilesY = uvY;
            tsa.cycleCount = Mathf.RoundToInt(uvCycles);
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
            tsa.startFrame = new ParticleSystem.MinMaxCurve(0f);
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 1f),
                    new Keyframe(1f, 1f, 1f, 0f)
                ));
        }
        else
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = false;
        }

        // Color over lifetime
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(a0, 0.00f),
                new GradientAlphaKey(a1, 0.25f),
                new GradientAlphaKey(a2, 0.50f),
                new GradientAlphaKey(a3, 0.75f),
                new GradientAlphaKey(a4, 1.00f),
            };
            GradientColorKey[] colorKeys;
            if (animEntry.Length >= 31)
            {
                colorKeys = new GradientColorKey[5];
                for (int k = 0; k < 5; k++)
                {
                    float r = float.Parse(animEntry[16 + k * 3]) / 255f;
                    float g = float.Parse(animEntry[17 + k * 3]) / 255f;
                    float b = float.Parse(animEntry[18 + k * 3]) / 255f;
                    colorKeys[k] = new GradientColorKey(new Color(r, g, b), k * 0.25f);
                }
            }
            else
            {
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                };
            }
            grad.SetKeys(colorKeys, alphaKeys);
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        // Size growth
        var sol = ps.sizeOverLifetime;
        sol.enabled = false;
        if (Mathf.Abs(sizeGrow) > 0.001f)
        {
            var grow = ps.gameObject.GetComponent<LegacySizeGrow>();
            if (grow == null) grow = ps.gameObject.AddComponent<LegacySizeGrow>();
            grow.sizeGrow = sizeGrow;
        }

        // Force over lifetime
        if (Mathf.Abs(forceY) > 0.001f)
        {
            var fol = ps.forceOverLifetime;
            fol.enabled = true;
            fol.x = 0f;
            fol.y = forceY;
            fol.z = 0f;
            fol.space = ParticleSystemSimulationSpace.World;
        }
        else
        {
            var fol = ps.forceOverLifetime;
            fol.enabled = false;
        }

        // Damping
        if (damping < 0.99f)
        {
            var lv = ps.limitVelocityOverLifetime;
            lv.enabled = true;
            lv.dampen = 1f - damping;
        }
    }
}

/// <summary>
/// Debug controller:
/// F9  = Diagnostic test shot: raycast + ShowHitEffect to test complete chain
/// F10 = Visual self-test: emits large bright particles from every system at camera pos
/// F11 = Toggle 10x particle size
/// F12 = Dump complete weapon config state + live particle counts to log
/// </summary>
public class ParticleDebugController : MonoBehaviour
{
    static SurfaceEffectType _testSurface = SurfaceEffectType.StoneEffect;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            DiagnosticTestShot();
        }
        if (Input.GetKeyDown(KeyCode.F10))
        {
            TestAllParticleSystems();
        }
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ParticleEmissionSystem.DebugLargeParticles = !ParticleEmissionSystem.DebugLargeParticles;
            Debug.Log("[ParticleDebug] Large particles: " + ParticleEmissionSystem.DebugLargeParticles +
                " (size 10x " + (ParticleEmissionSystem.DebugLargeParticles ? "ON" : "OFF") + ")");
        }
        if (Input.GetKeyDown(KeyCode.F12))
        {
            DumpWeaponConfigState();
        }
    }

    void DiagnosticTestShot()
    {
        Camera cam = FindCamera();
        if (cam == null) { Debug.LogError("[ParticleDebug] No camera found!"); return; }

        // Raycast from camera to find a surface
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        Vector3 hitPoint, hitNormal;

        if (Physics.Raycast(ray, out hit, 500f))
        {
            hitPoint = hit.point;
            hitNormal = hit.normal;
            string tag = TagUtil.GetTag(hit.collider);
            Debug.Log("[ParticleDebug F9] Raycast hit: " + hit.collider.gameObject.name +
                " tag=" + tag + " point=" + hitPoint.ToString("F2") + " normal=" + hitNormal.ToString("F2"));
        }
        else
        {
            // No surface hit — use a point 10m in front of camera
            hitPoint = cam.transform.position + cam.transform.forward * 10f;
            hitNormal = -cam.transform.forward;
            Debug.Log("[ParticleDebug F9] No raycast hit, using point 10m ahead: " + hitPoint.ToString("F2"));
        }

        // Cycle through surface types: Stone→Wood→Metal→Sand→Grass→Water
        Debug.Log("[ParticleDebug F9] Testing surface=" + _testSurface + " — press F9 again to cycle");

        // Get particle counts BEFORE emission
        var before = GetParticleCounts();

        // Trigger the ACTUAL ShowHitEffect through the game's code path
        MoveTrailrendererObject dummyTrail = null;
        ParticleEffectController.ShowHitEffect(
            ParticleConfigurationType.HandgunDefault,  // most common weapon type
            _testSurface,
            cam.transform.forward,
            hitPoint, hitNormal,
            cam.transform.position,
            Vector3.Distance(cam.transform.position, hitPoint),
            ref dummyTrail, null);

        // Also trigger explosion effect to compare
        ParticleEffectController.ShowExplosionEffect(
            ParticleConfigurationType.CannonDefault,
            _testSurface,
            hitPoint + Vector3.up * 3f, hitNormal);

        // Get particle counts AFTER emission
        var after = GetParticleCounts();

        // Log the difference — shows exactly which systems received particles
        Debug.Log("[ParticleDebug F9] === EMISSION RESULT (surface=" + _testSurface + ") ===");
        foreach (var kvp in after)
        {
            int beforeCount = before.ContainsKey(kvp.Key) ? before[kvp.Key] : 0;
            int delta = kvp.Value - beforeCount;
            if (delta > 0)
                Debug.Log("[ParticleDebug F9]   " + kvp.Key + ": +" + delta + " particles (total=" + kvp.Value + ")");
        }
        Debug.Log("[ParticleDebug F9] Quality level=" + QualitySettings.GetQualityLevel() +
            " (dust/smoke requires >0)");

        // Cycle surface type for next F9 press
        switch (_testSurface)
        {
            case SurfaceEffectType.StoneEffect: _testSurface = SurfaceEffectType.WoodEffect; break;
            case SurfaceEffectType.WoodEffect: _testSurface = SurfaceEffectType.MetalEffect; break;
            case SurfaceEffectType.MetalEffect: _testSurface = SurfaceEffectType.SandEffect; break;
            case SurfaceEffectType.SandEffect: _testSurface = SurfaceEffectType.GrassEffect; break;
            case SurfaceEffectType.GrassEffect: _testSurface = SurfaceEffectType.WaterEffect; break;
            case SurfaceEffectType.WaterEffect: _testSurface = SurfaceEffectType.Splat; break;
            default: _testSurface = SurfaceEffectType.StoneEffect; break;
        }
    }

    Dictionary<string, int> GetParticleCounts()
    {
        var counts = new Dictionary<string, int>();
        var allPS = Resources.FindObjectsOfTypeAll<ParticleSystem>();
        foreach (var ps in allPS)
        {
            if (ps == null || ps.gameObject == null) continue;
            if (!ParticleEmitterAutoFixer.IsKnownParticle(ps.gameObject.name)) continue;
            // Only count scene instances, not prefab assets
            if (string.IsNullOrEmpty(ps.gameObject.scene.name)) continue;
            counts[ps.gameObject.name] = ps.particleCount;
        }
        return counts;
    }

    Camera FindCamera()
    {
        // Camera.main returns null in UberStrike — use LevelCamera instead
        if (LevelCamera.Exists && LevelCamera.Instance.MainCamera != null)
            return LevelCamera.Instance.MainCamera;
        Camera cam = Camera.main;
        if (cam == null)
        {
            var allCams = FindObjectsOfType<Camera>();
            foreach (var c in allCams)
                if (c.isActiveAndEnabled && c.gameObject.activeInHierarchy) { cam = c; break; }
        }
        return cam;
    }

    void TestAllParticleSystems()
    {
        Camera cam = FindCamera();
        if (cam == null) { Debug.LogError("[ParticleDebug] No camera found!"); return; }

        Debug.Log("[MODULE TEST F10] ===== DEFINITIVE MODULE EFFECT TEST =====");
        Debug.Log("[MODULE TEST F10] Setting EXTREME RED ColorOverLifetime + Y=50 force on ALL PS");
        Debug.Log("[MODULE TEST F10] Emitting WHITE particles — if modules work, GetCurrentColor=RED");

        Vector3 testPos = cam.transform.position + cam.transform.forward * 5f;
        int tested = 0;
        int modulesWork = 0, modulesBroken = 0;

        // Build extreme RED gradient (impossible to miss)
        Gradient redGrad = new Gradient();
        redGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.red, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        var allPS = Resources.FindObjectsOfTypeAll<ParticleSystem>();
        foreach (var ps in allPS)
        {
            if (ps == null || ps.gameObject == null) continue;
            string name = ps.gameObject.name;
            if (!ParticleEmitterAutoFixer.IsKnownParticle(name)) continue;
            if (string.IsNullOrEmpty(ps.gameObject.scene.name)) continue;

            // --- SET EXTREME COLOR: solid RED ---
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(redGrad);

            // --- SET EXTREME FORCE: strong upward ---
            var fol = ps.forceOverLifetime;
            fol.enabled = true;
            fol.x = 0f;
            fol.y = 50f;
            fol.z = 0f;
            fol.space = ParticleSystemSimulationSpace.World;

            // Ensure playing
            if (!ps.isPlaying) ps.Play();

            // Emit a WHITE test particle (large, long-lived)
            var ep = new ParticleSystem.EmitParams();
            ep.position = testPos;
            ep.velocity = Vector3.zero;
            ep.startSize = 3f;
            ep.startLifetime = 10f;
            ep.startColor = new Color32(255, 255, 255, 255); // WHITE
            ps.Emit(ep, 1);

            // Read back and compare startColor vs GetCurrentColor
            int count = ps.particleCount;
            string result = "NO_PARTICLES";
            if (count > 0)
            {
                var buf = new ParticleSystem.Particle[count];
                ps.GetParticles(buf, count);
                var p = buf[count - 1]; // last emitted = our test particle
                Color startCol = p.startColor;
                Color currentCol = p.GetCurrentColor(ps);
                float startSz = p.startSize;
                float currentSz = p.GetCurrentSize(ps);

                // If ColorOverLifetime works: green channel should be ~0 (red gradient kills it)
                bool colWorks = currentCol.g < 0.1f;
                if (colWorks) modulesWork++; else modulesBroken++;

                result = "start=" + startCol.ToString("F2") +
                    " current=" + currentCol.ToString("F2") +
                    " sz=" + startSz.ToString("F1") + "→" + currentSz.ToString("F1") +
                    (colWorks ? " ✓WORKS" : " ✗BROKEN(still white!)");
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            string matName = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "NULL";

            Debug.Log("[MODULE TEST F10] " + name + " " + result +
                " rend=" + (renderer != null && renderer.enabled) + " mat=" + matName);

            testPos += cam.transform.right * 2f;
            tested++;
        }

        Debug.Log("[MODULE TEST F10] ===== RESULTS: " + modulesWork + " WORK, " + modulesBroken + " BROKEN out of " + tested + " =====");
        Debug.Log("[MODULE TEST F10] All PS now have RED ColorOverLifetime + upward force.");
        Debug.Log("[MODULE TEST F10] NOW SHOOT WALLS — if impacts turn RED & fly UP → modules work.");
        Debug.Log("[MODULE TEST F10] If impacts UNCHANGED → modules broken, need manual color/force in EmitSafe.");
    }

    void DumpWeaponConfigState()
    {
        // Dump live particle counts — show ALL instances (scene + asset) to diagnose split
        Debug.Log("[ParticleDebug F12] === ALL PS INSTANCES (scene + asset) ===");
        var allPS = Resources.FindObjectsOfTypeAll<ParticleSystem>();
        foreach (var ps in allPS)
        {
            if (ps == null || ps.gameObject == null) continue;
            if (!ParticleEmitterAutoFixer.IsKnownParticle(ps.gameObject.name)) continue;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            string sceneName = ps.gameObject.scene.name;
            bool isScene = !string.IsNullOrEmpty(sceneName);
            // Show module state to verify config was applied to the RIGHT instance
            var tsa = ps.textureSheetAnimation;
            var col = ps.colorOverLifetime;
            var fol = ps.forceOverLifetime;
            var shape = ps.shape;
            string parentInfo = ps.transform.parent != null ? ps.transform.parent.name : "root";
            string matInfo = rend != null && rend.sharedMaterial != null ? rend.sharedMaterial.name : "NULL";
            Debug.Log("[ParticleDebug F12]   " + parentInfo + "/" + ps.gameObject.name +
                (isScene ? " [SCENE:" + sceneName + "]" : " [ASSET]") +
                " id=" + ps.GetInstanceID() +
                " count=" + ps.particleCount +
                " playing=" + ps.isPlaying +
                " active=" + ps.gameObject.activeInHierarchy +
                " rend=" + (rend != null && rend.enabled) +
                " mat=" + matInfo +
                " shape=" + shape.enabled +
                " TSA=" + (tsa.enabled ? tsa.numTilesX + "x" + tsa.numTilesY : "off") +
                " colOL=" + col.enabled +
                " forceOL=" + fol.enabled);
        }

        Debug.Log("[ParticleDebug F12] Quality=" + QualitySettings.GetQualityLevel() +
            " (0=dust/smoke hidden in explosions)");

        // Dump weapon config state via reflection
        var controller = Object.FindObjectOfType<ParticleEffectController>();
        if (controller == null) { Debug.LogError("[ParticleDebug] ParticleEffectController NOT FOUND!"); return; }

        var dataField = typeof(ParticleEffectController).GetField("_allWeaponData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField == null) return;

        System.Array allData = dataField.GetValue(controller) as System.Array;
        if (allData == null) return;

        var impactField = typeof(ParticleCobfigurationPerWeapon).GetField(
            "_weaponImpactEffectConfiguration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Debug.Log("[ParticleDebug F12] === WEAPON CONFIGS (" + allData.Length + " entries) ===");
        foreach (object configObj in allData)
        {
            if (configObj == null) continue;
            var typeField = configObj.GetType().GetField("Type");
            var cfgField = configObj.GetType().GetField("Configuration");
            if (typeField == null || cfgField == null) continue;

            object typeVal = typeField.GetValue(configObj);
            var perWeapon = cfgField.GetValue(configObj) as ParticleCobfigurationPerWeapon;
            if (perWeapon == null) continue;

            var impact = impactField != null ? impactField.GetValue(perWeapon) as WeaponImpactEffectConfiguration : null;
            if (impact == null) continue;

            // Trail config
            string trail = "useRenderer=" + impact.UseTrailrendererForTrail +
                " prefab=" + (impact.TrailrendererTrailPrefab != null);
            if (impact.TrailParticleConfigurationForInstantHit != null)
            {
                var t = impact.TrailParticleConfigurationForInstantHit;
                trail += " particleTrail:emit=" + (t.ParticleEmitter != null) + " color=" + t.ParticleColor;
            }

            // Surface effect sample (Stone) — now with instanceID for cross-reference
            string stone = "N/A";
            if (impact.SurfaceParameterSet != null && impact.SurfaceParameterSet.StoneEffect != null)
            {
                var s = impact.SurfaceParameterSet.StoneEffect;
                string emitId = s.ParticleEmitter != null ? "id=" + s.ParticleEmitter.GetInstanceID() + " name=" + s.ParticleEmitter.gameObject.name : "NULL";
                stone = emitId + " color=" + s.ParticleColor +
                    " size=" + s.ParticleMinSize + "-" + s.ParticleMaxSize + " count=" + s.ParticleCount;
            }

            // Also log Wood and Fire for cross-reference
            string wood = "N/A";
            if (impact.SurfaceParameterSet != null && impact.SurfaceParameterSet.WoodEffect != null)
            {
                var w = impact.SurfaceParameterSet.WoodEffect;
                string wId = w.ParticleEmitter != null ? "id=" + w.ParticleEmitter.GetInstanceID() : "NULL";
                wood = wId + " color=" + w.ParticleColor + " size=" + w.ParticleMinSize + "-" + w.ParticleMaxSize;
            }
            string fireStr = "N/A";
            if (impact.FireParticleConfigurationForInstantHit != null)
            {
                var f2 = impact.FireParticleConfigurationForInstantHit;
                string fId = f2.ParticleEmitter != null ? "id=" + f2.ParticleEmitter.GetInstanceID() : "NULL";
                fireStr = fId + " color=" + f2.ParticleColor + " size=" + f2.ParticleMinSize + "-" + f2.ParticleMaxSize;
            }

            // Explosion sample (Blast) — with instanceID and parent for weapon-specific verification
            string blast = "N/A";
            if (impact.ExplosionParameterSet != null && impact.ExplosionParameterSet.BlastParameters != null)
            {
                var b = impact.ExplosionParameterSet.BlastParameters;
                string bId = b.ParticleEmitter != null
                    ? "id=" + b.ParticleEmitter.GetInstanceID() +
                      " parent=" + (b.ParticleEmitter.transform.parent != null ? b.ParticleEmitter.transform.parent.name : "?") +
                      " mat=" + (b.ParticleEmitter.GetComponent<ParticleSystemRenderer>() != null && b.ParticleEmitter.GetComponent<ParticleSystemRenderer>().sharedMaterial != null
                          ? b.ParticleEmitter.GetComponent<ParticleSystemRenderer>().sharedMaterial.name : "?")
                    : "NULL";
                blast = bId + " size=" + b.MinSize + "-" + b.MaxSize + " count=" + b.ParticleCount;
            }

            Debug.Log("[ParticleDebug F12]   Type=" + typeVal +
                "\n    stone={" + stone + "}" +
                "\n    wood={" + wood + "}" +
                "\n    fire={" + fireStr + "}" +
                "\n    trail={" + trail + "}" +
                "\n    blast={" + blast + "}");
        }
    }
}

/// <summary>
/// Replicates legacy ParticleAnimator.sizeGrow behavior exactly.
/// Legacy sizeGrow adds a fixed number of world-space units per second to particle size.
/// Modern SizeOverLifetime is multiplicative and can't replicate this when startSize varies per Emit().
/// This component uses GetParticles/SetParticles to modify startSize each frame additively.
/// </summary>
public class LegacySizeGrow : MonoBehaviour
{
    public float sizeGrow;

    ParticleSystem _ps;
    ParticleSystem.Particle[] _particles;

    void LateUpdate()
    {
        if (_ps == null) _ps = GetComponent<ParticleSystem>();
        if (_ps == null) return;

        int count = _ps.particleCount;
        if (count == 0) return;

        if (_particles == null || _particles.Length < count)
            _particles = new ParticleSystem.Particle[count * 2]; // over-allocate to reduce reallocs

        _ps.GetParticles(_particles, count);

        float delta = sizeGrow * Time.deltaTime;
        for (int i = 0; i < count; i++)
        {
            float newSize = _particles[i].startSize + delta;
            if (newSize < 0f) newSize = 0f;
            _particles[i].startSize = newSize;
        }

        _ps.SetParticles(_particles, count);
    }
}

/// <summary>
/// Runs for several frames after ParticleEmitterAutoFixer to guard against standalone build
/// asset bundle deserialization overwriting startRotation on Stretched particles.
/// In builds, scene data from .unity3d bundles may re-apply serialized values after sceneLoaded,
/// restoring the migration's random startRotation that causes trails to render as horizontal lines.
/// </summary>
public class ParticleRotationGuard : MonoBehaviour
{
    private ParticleSystem[] _stretchedParticles;
    private int _framesRemaining = 30;
    private int _overwrites = 0;

    public static void Create(ParticleSystem[] particles)
    {
        var go = new GameObject("ParticleRotationGuard");
        Object.DontDestroyOnLoad(go);
        var guard = go.AddComponent<ParticleRotationGuard>();
        guard._stretchedParticles = particles;
        Debug.Log("[ParticleRotationGuard] Watching " + particles.Length + " stretched particles for 30 frames");
    }

    void LateUpdate()
    {
        if (_stretchedParticles == null || _framesRemaining <= 0)
        {
            if (_overwrites > 0)
                Debug.LogWarning("[ParticleRotationGuard] Fixed " + _overwrites + " startRotation overwrites across 30 frames");
            else
                Debug.Log("[ParticleRotationGuard] No overwrites detected — fix held cleanly");
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < _stretchedParticles.Length; i++)
        {
            var ps = _stretchedParticles[i];
            if (ps == null) continue;

            var main = ps.main;
            float rot = main.startRotation.constant;
            bool rot3d = main.startRotation3D;

            if (rot3d || Mathf.Abs(rot) > 0.001f)
            {
                main.startRotation3D = false;
                main.startRotation = 0f;
                _overwrites++;

                if (_overwrites <= 5) // limit log spam
                {
                    Debug.LogWarning("[ParticleRotationGuard] Frame " + (30 - _framesRemaining) +
                        ": '" + ps.gameObject.name + "' had startRotation=" + rot.ToString("F4") +
                        " rot3D=" + rot3d + " — reset to 0");
                }
            }

            // Also ensure Stretch render mode hasn't been overwritten
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            if (rend != null && rend.renderMode != ParticleSystemRenderMode.Stretch)
            {
                rend.renderMode = ParticleSystemRenderMode.Stretch;
                _overwrites++;
                Debug.LogWarning("[ParticleRotationGuard] Frame " + (30 - _framesRemaining) +
                    ": '" + ps.gameObject.name + "' renderMode was overwritten — restored to Stretch");
            }
        }

        _framesRemaining--;
    }
}
