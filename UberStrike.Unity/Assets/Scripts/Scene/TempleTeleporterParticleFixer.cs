using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Restores the legacy Unity-3.5.5 particle visuals on Temple's SecretDoor portal
// and its TeleporterParticles effect inside the Secret Temple. Unity 5+ stripped
// the 3.5-era EllipsoidParticleEmitter / MeshParticleEmitter / ParticleAnimator /
// ParticleRenderer components during the port's prefab migration — the GameObjects
// and their Transforms survive in SecretDoor.prefab + TeleporterParticles.prefab
// but emit nothing. We rebuild equivalent modern ParticleSystem components at
// runtime, following the same pattern as ForceField.SpawnJumpPadParticlesOn.
//
// Three emitters to recreate (parameters taken from the 3.5.5 source prefabs):
//
// 1. SecretDoor / LightningParticles — small calm blue lightning on the portal
//    door. Emission 10/s, ellipsoid 1×2×0 (vertical sheet), local space, size 1,
//    lifetime 0.5-1s, sizeGrow 2, damping 0.7, 4x4 UV animation, billboard.
//    Material: BluePlasmaLightning (tint baked blue at 65% alpha).
//
// 2. TeleporterParticles / LightningParticles — big blue lightning volume inside
//    the Secret Temple. Emission 100/s, ellipsoid 5×20×20, world space, size
//    4-5.5, rndVelocity ±7 on every axis, same colors and UV anim as #1.
//    Material: BluePlasmaLightning.
//
// 3. TeleporterParticles / SparkGather — 1000 bright sparks/sec converging from
//    the sphere-mesh shell inward (startSpeed -30 along mesh normals). Local
//    space, stretched render (lengthScale 12), size 0.1-0.3, damping 0.05, no
//    sizeGrow. Material: BlueLongSpark (tint baked bright blue).
//
// All emitters start with emission.enabled=false because TempleTeleporter.Awake
// disables particles on startup (door is closed); emission is flipped on inside
// TempleTeleporter.OpenDoor when the player activates the portal. We wire the
// generated ParticleSystem onto TempleTeleporter._particles via reflection so
// the existing door logic keeps working untouched.
public class TempleTeleporterParticleFixer : MonoBehaviour
{
    const string SceneName = "LevelTempleOfTheRaven";

    // One-shot per session. The scene reloads on re-entry but objects authored
    // inside it (SecretDoor, TeleporterParticles) are the same instances — adding
    // PS components again on re-entry would duplicate them. Guard both by
    // session-once AND by component-exists check before AddComponent.
    static bool _appliedThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryApply(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryApply(scene);

    static void TryApply(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName) return;
        if (_appliedThisSession) return;

        var bluePlasma = Resources.Load<Material>("BluePlasmaLightning");
        var blueSpark = Resources.Load<Material>("BlueLongSpark");
        if (bluePlasma == null)
            Debug.LogWarning("[TempleTeleporterParticleFixer] Failed to load BluePlasmaLightning.mat from Resources — lightning will render untinted.");
        if (blueSpark == null)
            Debug.LogWarning("[TempleTeleporterParticleFixer] Failed to load BlueLongSpark.mat from Resources — sparks will render untinted.");

        int fixedDoors = 0;
        foreach (var teleporter in Object.FindObjectsOfType<TempleTeleporter>(includeInactive: true))
        {
            if (teleporter == null) continue;
            FixSecretDoorPortal(teleporter, bluePlasma);
            fixedDoors++;
        }

        int fixedVisuals = FixStandaloneTeleporterParticles(bluePlasma, blueSpark);

        Debug.Log($"[TempleTeleporterParticleFixer] Applied: {fixedDoors} SecretDoor(s), {fixedVisuals} TeleporterParticles visual(s).");
        _appliedThisSession = true;
    }

    // ---- SecretDoor portal (small variant) ----
    static void FixSecretDoorPortal(TempleTeleporter teleporter, Material bluePlasmaMat)
    {
        var lightning = FindChildByName(teleporter.transform, "LightningParticles");
        if (lightning == null)
        {
            Debug.LogWarning($"[TempleTeleporterParticleFixer] SecretDoor '{teleporter.name}' has no LightningParticles child — skipping.");
            return;
        }

        // Child is authored m_IsActive=0 in 3.5 — activate so the PS runs when
        // the parent door activates. TempleTeleporter.Awake still controls
        // emission on/off; activation itself doesn't start emission.
        lightning.gameObject.SetActive(true);

        var ps = lightning.GetComponent<ParticleSystem>();
        if (ps == null) ps = BuildLightningPS(lightning.gameObject, bluePlasmaMat, small: true);

        // Wire the private _particles field on TempleTeleporter so the existing
        // Awake/Open/Update logic can toggle emission as the door opens/times out.
        var field = typeof(TempleTeleporter).GetField("_particles",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(teleporter, ps);
        else
            Debug.LogWarning("[TempleTeleporterParticleFixer] TempleTeleporter._particles field not found via reflection.");
    }

    // ---- Standalone TeleporterParticles prefab instance (big variant + sparks) ----
    static int FixStandaloneTeleporterParticles(Material bluePlasmaMat, Material blueSparkMat)
    {
        int count = 0;
        // The TeleporterParticles prefab has no MonoBehaviour — find it by name.
        // Root + all three children are authored m_IsActive=0 in both the 3.5
        // source and the port; 3.5 had some other game script activating them.
        // We take the explicit-activation route: turn on root + children + set
        // emission.enabled=true on the PS so particles run as soon as the
        // scene spawns the prefab in its physical Secret Temple location.
        foreach (var t in Object.FindObjectsOfType<Transform>(includeInactive: true))
        {
            if (t == null || t.name != "TeleporterParticles") continue;

            // Activate root and the TWO particle children only. CRITICAL: do
            // NOT activate the prefab's "Point light" child — it has
            // m_Lightmapping=1 (Baked) serialised on it, and activating a
            // baked light at runtime in Unity 2022 triggers internal GI
            // state updates that clobber the baked-lightmap contribution on
            // surrounding renderers (Temple walls rendered flat-lit once the
            // TeleporterParticles hierarchy came online — regression
            // introduced 2026-04-21 when the glowing sparks landed).
            t.gameObject.SetActive(true);

            var lightning = FindChildByName(t, "LightningParticles");
            if (lightning != null)
            {
                lightning.gameObject.SetActive(true);
                if (lightning.GetComponent<ParticleSystem>() == null)
                {
                    var ps = BuildLightningPS(lightning.gameObject, bluePlasmaMat, small: false);
                    var em = ps.emission; em.enabled = true; // no MB controls this — turn on directly
                }
            }

            // SparkGather is gated on the SecretDoor ritual (shoot all RavenEyes
            // within the activation window → door opens → sparks flow). Build the
            // PS with emission disabled and let the activator flip it on when
            // the teleporter enables.
            var spark = FindChildByName(t, "SparkGather");
            ParticleSystem sparkPS = null;
            if (spark != null)
            {
                spark.gameObject.SetActive(true);
                sparkPS = spark.GetComponent<ParticleSystem>();
                if (sparkPS == null) sparkPS = BuildSparkGatherPS(spark.gameObject, blueSparkMat);
                var em = sparkPS.emission; em.enabled = false;
            }

            // Leave the prefab's baked Point light GameObject inactive and
            // untouched. Build a SEPARATE realtime point light for the ritual
            // visual — realtime-only means Unity never mixes it with baked GI,
            // so toggling it can't invalidate the surrounding lightmap
            // contribution. Parented to the root so it follows the teleporter.
            var ritualLightGO = new GameObject("BlueSparkRitualLight");
            ritualLightGO.transform.SetParent(t, false);
            ritualLightGO.transform.localPosition = Vector3.zero;
            var ritualLight = ritualLightGO.AddComponent<Light>();
            ritualLight.type = LightType.Point;
            ritualLight.color = new Color(0.574f, 0.608f, 0.778f, 1f); // matches prefab's m_Color
            ritualLight.intensity = 2f;
            ritualLight.range = 50f;
            ritualLight.shadows = LightShadows.None;
            ritualLight.renderMode = LightRenderMode.Auto;
#if UNITY_EDITOR
            ritualLight.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            ritualLight.enabled = false; // gated on ritual

            // Wire up the ritual activator on the root. Idempotent — if it's
            // already there from a previous scene apply, reuse.
            if (sparkPS != null || ritualLight != null)
            {
                var activator = t.GetComponent<TempleSecretRitualActivator>()
                             ?? t.gameObject.AddComponent<TempleSecretRitualActivator>();
                activator.Setup(sparkPS, ritualLight);
            }

            count++;
        }
        return count;
    }

    // ---- Builders ----

    static ParticleSystem BuildLightningPS(GameObject host, Material mat, bool small)
    {
        var ps = host.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = 0f;
        // Match 3.5 exactly: small variant = flat size 1 (no variation — the
        // 3.5 EllipsoidParticleEmitter had minSize=maxSize=1). Big variant
        // keeps its 4-5.5 range from the 3.5 TeleporterParticles emitter.
        main.startSize = small ? (ParticleSystem.MinMaxCurve)1f
                               : new ParticleSystem.MinMaxCurve(4f, 5.5f);
        main.startColor = Color.white; // material _TintColor bakes the blue
        main.maxParticles = small ? 30 : 150;
        // 3.5 "Simulate in Worldspace": SecretDoor=false, TeleporterParticles=true.
        main.simulationSpace = small ? ParticleSystemSimulationSpace.Local
                                     : ParticleSystemSimulationSpace.World;
        // Shape scaling — particle SIZE is absolute world units (predictable),
        // while the emission shape still follows the transform. Previously used
        // Hierarchy which multiplied particle size through the whole parent
        // chain (SecretDoor 2.96×5.86×0.61 × LightningParticles 0.408×0.248×0.05)
        // and produced wildly inconsistent visual sizes across door transforms.
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = false; // TempleTeleporter.Awake expects this; door opens via OpenDoor()
        // Exact match to 3.5: small variant = 10/s (EllipsoidParticleEmitter
        // minEmission=maxEmission=10), big variant = 100/s.
        emission.rateOverTime = small ? 10f : 100f;

        // Box shape as stand-in for legacy ellipsoid volume. Dimensions match
        // the 3.5 Ellipsoid field; Unity 2022 dropped the ellipsoid primitive.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = small ? new Vector3(1f, 2f, 0.01f) : new Vector3(5f, 20f, 20f);
        shape.randomDirectionAmount = 0f;

        // rndVelocity (7,7,7) in 3.5 means each particle gets random [-7,7] per
        // axis in world space. Only applies to the big TeleporterParticles variant.
        if (!small)
        {
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-7f, 7f);
            velocity.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
            velocity.z = new ParticleSystem.MinMaxCurve(-7f, 7f);
        }

        // ParticleAnimator damping=0.7 → LimitVelocityOverLifetime.dampen
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 50f;
        limit.dampen = 0.7f;

        // Alpha fades in and out; material holds the blue tint. Matches the
        // 3.5 colorAnimation pattern (faint→bright→bright→dim→faint).
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[] {
                new GradientAlphaKey(0.1f,  0f),
                new GradientAlphaKey(0.9f,  0.25f),
                new GradientAlphaKey(1f,    0.5f),
                new GradientAlphaKey(0.7f,  0.75f),
                new GradientAlphaKey(0f,    1f)
            });
        colorOverLife.color = gradient;

        // sizeGrow=2 in 3.5 is an additive growth rate per second. Over the max
        // ~1s lifetime particles ~triple; emulate with curve 1 → 3.
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 3f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 4x4 UV grid, 1 cycle — matches the source ParticleRenderer.UV Animation.
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.numTilesX = 4;
        tsa.numTilesY = 4;
        tsa.cycleCount = 1;
        tsa.animation = ParticleSystemAnimationType.WholeSheet;

        var renderer = host.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // Exact match to 3.5: the ParticleRenderer authored m_MaxParticleSize=0.25.
        // Previously raised this to 1.0 which produced the "huge blue smoke" look
        // the user flagged — reverting so particles stay capped at 25% viewport
        // height on close-up views.
        renderer.maxParticleSize = 0.25f;
        if (mat != null) renderer.sharedMaterial = mat;

        return ps;
    }

    static ParticleSystem BuildSparkGatherPS(GameObject host, Material mat)
    {
        var ps = host.AddComponent<ParticleSystem>();

        // Reuse the sphere mesh already sitting on SparkGather's MeshFilter —
        // no Resources lookup needed. This is the same spherefbx.fbx the 3.5
        // MeshParticleEmitter sampled from.
        var meshFilter = host.GetComponent<MeshFilter>();
        Mesh sphereMesh = meshFilter != null ? meshFilter.sharedMesh : null;

        var main = ps.main;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 1f);
        // Negative startSpeed inverts shape-normal emission direction — sparks
        // spawn on the sphere shell and travel INWARD, matching the 3.5
        // MeshParticleEmitter's min/maxNormalVelocity = -30.
        main.startSpeed = -30f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = Color.white;
        main.maxParticles = 1200;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        // Shape-only scaling — transform scale (9.85, 22.23, 20.72) applies to
        // the sphere-shell emission volume (as intended) but NOT to individual
        // spark sizes, which stay 0.1-0.3 world units.
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = false;
        emission.rateOverTime = 1000f;

        var shape = ps.shape;
        shape.enabled = true;
        if (sphereMesh != null)
        {
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.mesh = sphereMesh;
        }
        else
        {
            // Fallback if the MeshFilter is missing for some reason.
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            shape.radiusThickness = 0f;
        }
        shape.randomDirectionAmount = 0f;

        // ParticleAnimator damping=0.05 — very light velocity decay.
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 50f;
        limit.dampen = 0.05f;

        // BlueLongSpark material holds the blue tint already; use a white→white
        // with alpha fade-in/out so particles don't pop on spawn or death.
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(1f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = gradient;

        var renderer = host.GetComponent<ParticleSystemRenderer>();
        // StretchParticles=3 + lengthScale=12 + velocityScale=0 in 3.5 maps to
        // Stretch render mode with the same scale constants.
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 12f;
        renderer.velocityScale = 0f;
        renderer.maxParticleSize = 0.025f;
        if (mat != null) renderer.sharedMaterial = mat;

        return ps;
    }

    static Transform FindChildByName(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }
}

// Gates the TeleporterParticles / SparkGather effect (1000 sparks converging on
// the sphere center) + its Point light on the SecretDoor's open/closed state.
//
// First attempt subscribed to DoorOpenedEvent via CmuneEventHandler. That event
// is routed through GameState.CurrentGame.OpenDoor(DoorID) — a multiplayer
// round-trip that NEVER FIRES in offline/testing sessions where
// GameState.HasCurrentGame is false. User reported sparks didn't show up even
// after shooting the ritual triggers.
//
// Pivoted to polling TempleTeleporter.enabled: the MB enables itself inside
// OpenDoor() (called locally by SecretBehaviour when all triggers are lit,
// regardless of network state) and disables itself in Update() once the
// activation window lapses. So `teleporter.enabled` is a reliable proxy for
// "door currently open" in both single-player and networked play.
//
// Matches user intent 2026-04-21: "SparkGather should only be activated when
// a player performs the SecretDoor ritual". The Point light follows the same
// gating so the volumetric glow only appears during the ritual window.
public class TempleSecretRitualActivator : MonoBehaviour
{
    private ParticleSystem _sparkPS;
    private Light _pointLight;
    private TempleTeleporter _teleporter;
    private bool _ritualEverCompleted;
    private float _nextScanTime;

    public void Setup(ParticleSystem sparkPS, Light pointLight)
    {
        _sparkPS = sparkPS;
        _pointLight = pointLight;
    }

    private void ResolveTeleporter()
    {
        if (_teleporter != null) return;
        // Throttle the scan — SecretDoor may be m_IsActive=0 until late in
        // scene init. Re-scan every 0.5s until we find it, then stop scanning.
        if (Time.time < _nextScanTime) return;
        _nextScanTime = Time.time + 0.5f;

        // includeInactive — SecretDoor is authored m_IsActive=0 until ritual opens it.
        foreach (var t in Object.FindObjectsOfType<TempleTeleporter>(includeInactive: true))
        {
            if (t == null) continue;
            _teleporter = t;
            Debug.Log($"[TempleSecretRitualActivator] Bound to TempleTeleporter on '{t.gameObject.name}' (DoorID={t.DoorID}, initial enabled={t.enabled}).");
            break;
        }
    }

    private void Update()
    {
        if (_ritualEverCompleted) return; // one-shot; sparks stay on for the session

        ResolveTeleporter();
        if (_teleporter == null) return;

        // TempleTeleporter.enabled flips true in OpenDoor() when the 3-eye
        // ritual completes. Previous version mirrored the enabled state both
        // ways, which meant sparks turned OFF the instant the player entered
        // the portal (OnTriggerEnter sets _timeOut=0 → Update disables
        // teleporter → activator sees false → sparks die mid-teleport).
        //
        // User wants sparks visible AFTER teleporting into the Secret Temple,
        // so this is one-shot: first time the door opens, sparks + light come
        // on and stay on for the rest of the session. No off-transition.
        if (_teleporter.enabled)
        {
            _ritualEverCompleted = true;
            if (_sparkPS != null)
            {
                var em = _sparkPS.emission; em.enabled = true;
            }
            if (_pointLight != null) _pointLight.enabled = true;
            Debug.Log("[TempleSecretRitualActivator] Ritual complete — sparks + light ON (one-shot, stays on for session).");
        }
    }
}
