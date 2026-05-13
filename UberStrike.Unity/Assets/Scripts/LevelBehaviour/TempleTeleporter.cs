using Cmune.Realtime.Common;
using Cmune.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class TempleTeleporter : SecretDoor
{
    private void Awake()
    {
        _audios = GetComponents<AudioSource>();

        EnsurePortalParticles();

        if (_particles != null)
        {
            var emission = _particles.emission;
            emission.enabled = false;
        }
        if (_visuals != null)
        {
            foreach(Renderer r in _visuals)
                r.enabled = false;
        }

        _doorID = transform.position.GetHashCode();
    }

    private void OnEnable()
    {
        CmuneEventHandler.AddListener<DoorOpenedEvent>(OnDoorOpenedEvent);
    }

    private void OnDisable()
    {
        CmuneEventHandler.RemoveListener<DoorOpenedEvent>(OnDoorOpenedEvent);
    }

    private void Update()
    {
        if (_timeOut < Time.time)
        {
            foreach (AudioSource s in _audios) s.Stop();

            if (_particles != null)
            {
                var emission = _particles.emission;
                emission.enabled = false;
            }
            if (_visuals != null)
            {
                foreach (Renderer r in _visuals)
                    r.enabled = false;
            }

            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Player" && _timeOut > Time.time)
        {
            _timeOut = 0;

            // 2-second screen-space diffusion fade on teleport — matches UB6's
            // SecretDoor → SecretTemple transition. Also plays while the player
            // falls through the air since TempleTeleporter is the trigger for
            // both directions on the Temple map.
            TempleTeleportFade.Instance.Fade();

            GameState.LocalPlayer.SpawnPlayerAt(_spawnpoint.position, _spawnpoint.rotation);
        }
    }

    private void OnDoorOpenedEvent(DoorOpenedEvent ev)
    {
        if (DoorID == ev.DoorID)
        {
            OpenDoor();
        }
    }

    public override void Open()
    {
        if (GameState.HasCurrentGame)
        {
            GameState.CurrentGame.OpenDoor(DoorID);
        }

        OpenDoor();
    }

    private void OpenDoor()
    {
        enabled = true;

        if (_particles != null)
        {
            var emission = _particles.emission;
            emission.enabled = true;
        }
        if (_visuals != null)
        {
            foreach (Renderer r in _visuals)
                r.enabled = true;
        }

        _timeOut = Time.time + _activationTime;

        foreach (AudioSource s in _audios) s.Play();
    }

    public int DoorID
    {
        get { return _doorID; }
    }

    // Builds the SecretDoor portal's LightningParticles child if the Unity 3.5 →
    // 2022 prefab migration stripped its EllipsoidParticleEmitter / ParticleAnimator /
    // ParticleRenderer components. The child GameObject + Transform survive but
    // emit nothing without us recreating the modern ParticleSystem stack here.
    //
    // No-op if _particles is already wired (serialised reference still good) or
    // the prefab is structured without a LightningParticles child.
    private void EnsurePortalParticles()
    {
        if (_particles != null) return;

        Transform lightning = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i);
            if (c.name == "LightningParticles") { lightning = c; break; }
        }
        if (lightning == null) return;

        // Authored m_IsActive=0 in 3.5 — activate so the PS runs once OpenDoor
        // flips emission on. Activation alone does not start emission.
        lightning.gameObject.SetActive(true);

        var ps = lightning.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            var bluePlasma = Resources.Load<Material>("BluePlasmaLightning");
            if (bluePlasma == null)
                Debug.LogWarning("[TempleTeleporter] BluePlasmaLightning.mat missing from Resources — portal lightning will render untinted.");
            ps = BuildLightningPS(lightning.gameObject, bluePlasma, small: true);
        }

        _particles = ps;
    }

    // Builds a Unity-2022 ParticleSystem matching the legacy 3.5 lightning emitter
    // parameters. Two variants:
    //   small=true  → SecretDoor portal lightning (10/s, ellipsoid 1×2×0, local space, size 1)
    //   small=false → standalone TeleporterParticles inside the Secret Temple
    //                 (100/s, ellipsoid 5×20×20, world space, size 4-5.5)
    // Material lookup is the caller's job — passed in as `mat` (BluePlasmaLightning).
    private static ParticleSystem BuildLightningPS(GameObject host, Material mat, bool small)
    {
        var ps = host.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = 0f;
        // 3.5 small variant: minSize=maxSize=1. Big variant: 4-5.5 from 3.5
        // TeleporterParticles emitter.
        main.startSize = small ? (ParticleSystem.MinMaxCurve)1f
                               : new ParticleSystem.MinMaxCurve(4f, 5.5f);
        main.startColor = Color.white; // material _TintColor bakes the blue
        main.maxParticles = small ? 30 : 150;
        // 3.5 "Simulate in Worldspace": SecretDoor=false, TeleporterParticles=true.
        main.simulationSpace = small ? ParticleSystemSimulationSpace.Local
                                     : ParticleSystemSimulationSpace.World;
        // Particle size in absolute world units (predictable across SecretDoor
        // transforms 2.96×5.86×0.61 etc.). Hierarchy mode multiplies through the
        // whole parent chain and produced wildly inconsistent sizes.
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = false; // Awake/OpenDoor toggles emission for the portal variant.
        // 3.5 EllipsoidParticleEmitter min=max emission: small=10/s, big=100/s.
        emission.rateOverTime = small ? 10f : 100f;

        // Box shape as stand-in for the legacy ellipsoid volume. Dimensions match
        // the 3.5 m_Ellipsoid field; Unity 2022 dropped the ellipsoid primitive.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = small ? new Vector3(1f, 2f, 0.01f) : new Vector3(5f, 20f, 20f);
        shape.randomDirectionAmount = 0f;

        // rndVelocity (7,7,7) in 3.5 means each particle gets random [-7,7] per
        // axis in world space. Big variant only.
        if (!small)
        {
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-7f, 7f);
            velocity.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
            velocity.z = new ParticleSystem.MinMaxCurve(-7f, 7f);
        }

        // ParticleAnimator damping=0.7 → LimitVelocityOverLifetime.dampen.
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 50f;
        limit.dampen = 0.7f;

        // Alpha fades faint→bright→bright→dim→faint, matching the 3.5
        // ParticleAnimator colorAnimation pattern. Material holds the blue tint.
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

        // sizeGrow=2 in 3.5 is additive growth per second; over ~1s lifetime
        // particles ~triple. Emulate with curve 1 → 3.
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 3f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 4×4 UV grid, 1 cycle — matches the source ParticleRenderer UV Animation.
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.numTilesX = 4;
        tsa.numTilesY = 4;
        tsa.cycleCount = 1;
        tsa.animation = ParticleSystemAnimationType.WholeSheet;

        var renderer = host.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // 3.5 ParticleRenderer m_MaxParticleSize=0.25. Higher values produced the
        // "huge blue smoke" look; cap at 25% viewport height.
        renderer.maxParticleSize = 0.25f;
        if (mat != null) renderer.sharedMaterial = mat;

        return ps;
    }

    // Builds the inward-converging spark shell for the standalone TeleporterParticles
    // prefab inside the Secret Temple. Emission stays disabled until the SecretDoor
    // ritual completes — TempleSecretRitualActivator handles that gating.
    private static ParticleSystem BuildSparkGatherPS(GameObject host, Material mat)
    {
        var ps = host.AddComponent<ParticleSystem>();

        // Reuse the sphere mesh already on SparkGather's MeshFilter — same
        // spherefbx.fbx the 3.5 MeshParticleEmitter sampled from.
        var meshFilter = host.GetComponent<MeshFilter>();
        Mesh sphereMesh = meshFilter != null ? meshFilter.sharedMesh : null;

        var main = ps.main;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 1f);
        // Negative startSpeed inverts shape-normal emission direction — sparks
        // spawn on the sphere shell and travel INWARD, matching the 3.5
        // MeshParticleEmitter min/maxNormalVelocity = -30.
        main.startSpeed = -30f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = Color.white;
        main.maxParticles = 1200;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        // Shape-only scaling so transform scale (9.85, 22.23, 20.72) drives
        // emission volume but NOT individual spark sizes.
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

        // BlueLongSpark holds the tint; fade alpha in/out so particles don't pop.
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

    // Sets up the standalone TeleporterParticles prefab instance(s) inside the
    // Temple of the Raven scene: builds the LightningParticles big-variant PS,
    // builds the SparkGather PS (gated off until ritual completes), wires a
    // realtime point light, and attaches a TempleSecretRitualActivator. Runs
    // at most once per session.
    //
    // Lives next to TempleTeleporter because the prefab in question doesn't
    // own a MonoBehaviour of its own — there's no natural script host for it
    // and the Temple scene needs its setup wired somewhere.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterTempleSceneSetup()
    {
        SceneManager.sceneLoaded += OnTempleSceneLoaded;
        ApplyStandaloneTeleporterParticles(SceneManager.GetActiveScene());
    }

    private static bool _standaloneAppliedThisSession;
    private const string TempleSceneName = "LevelTempleOfTheRaven";

    private static void OnTempleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyStandaloneTeleporterParticles(scene);
    }

    private static void ApplyStandaloneTeleporterParticles(Scene scene)
    {
        if (!scene.IsValid() || scene.name != TempleSceneName) return;
        if (_standaloneAppliedThisSession) return;

        var bluePlasma = Resources.Load<Material>("BluePlasmaLightning");
        var blueSpark  = Resources.Load<Material>("BlueLongSpark");
        if (bluePlasma == null)
            Debug.LogWarning("[TempleTeleporter] BluePlasmaLightning.mat missing from Resources — standalone lightning untinted.");
        if (blueSpark == null)
            Debug.LogWarning("[TempleTeleporter] BlueLongSpark.mat missing from Resources — sparks untinted.");

        int count = 0;
        foreach (var t in Object.FindObjectsOfType<Transform>(includeInactive: true))
        {
            if (t == null || t.name != "TeleporterParticles") continue;

            // Activate the root + the two particle children only. CRITICAL: do
            // NOT activate the prefab's authored "Point light" child — it has
            // m_Lightmapping=1 (Baked) serialised on it, and activating a
            // baked light at runtime in Unity 2022 triggers internal GI updates
            // that clobber the surrounding lightmap contribution (Temple walls
            // rendered flat-lit once TeleporterParticles came online — regression
            // from 2026-04-21 when the glowing sparks landed).
            t.gameObject.SetActive(true);

            Transform lightning = FindChildByName(t, "LightningParticles");
            if (lightning != null)
            {
                lightning.gameObject.SetActive(true);
                if (lightning.GetComponent<ParticleSystem>() == null)
                {
                    var ps = BuildLightningPS(lightning.gameObject, bluePlasma, small: false);
                    var em = ps.emission; em.enabled = true; // no MB controls this — turn on directly
                }
            }

            Transform spark = FindChildByName(t, "SparkGather");
            ParticleSystem sparkPS = null;
            if (spark != null)
            {
                spark.gameObject.SetActive(true);
                sparkPS = spark.GetComponent<ParticleSystem>();
                if (sparkPS == null) sparkPS = BuildSparkGatherPS(spark.gameObject, blueSpark);
                var em = sparkPS.emission; em.enabled = false;
            }

            // Build a SEPARATE realtime point light for the ritual visual —
            // realtime-only means Unity never mixes it with baked GI, so toggling
            // it can't invalidate surrounding lightmap contribution. The prefab's
            // baked Point light is left inactive and untouched.
            var ritualLightGO = new GameObject("BlueSparkRitualLight");
            ritualLightGO.transform.SetParent(t, false);
            ritualLightGO.transform.localPosition = Vector3.zero;
            var ritualLight = ritualLightGO.AddComponent<Light>();
            ritualLight.type = LightType.Point;
            ritualLight.color = new Color(0.574f, 0.608f, 0.778f, 1f); // matches prefab m_Color
            ritualLight.intensity = 2f;
            ritualLight.range = 50f;
            ritualLight.shadows = LightShadows.None;
            ritualLight.renderMode = LightRenderMode.Auto;
#if UNITY_EDITOR
            ritualLight.lightmapBakeType = LightmapBakeType.Realtime;
#endif
            ritualLight.enabled = false; // gated on ritual

            if (sparkPS != null || ritualLight != null)
            {
                var activator = t.GetComponent<TempleSecretRitualActivator>()
                             ?? t.gameObject.AddComponent<TempleSecretRitualActivator>();
                activator.Setup(sparkPS, ritualLight);
            }

            count++;
        }

        Debug.Log("[TempleTeleporter] Standalone TeleporterParticles set up: " + count + " instance(s).");
        _standaloneAppliedThisSession = true;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }

    #region Fields
    [SerializeField]
    private float _activationTime = 15;
    [SerializeField]
    private Renderer[] _visuals;
    [SerializeField]
    private Transform _spawnpoint;
    [SerializeField]
    private ParticleSystem _particles;

    private int _doorID;
    private float _timeOut = 0;
    private AudioSource[] _audios;
    #endregion
}

// Gates the TeleporterParticles / SparkGather effect (1000 sparks converging on
// the sphere center) + its realtime point light on the SecretDoor's open/closed state.
//
// Polls TempleTeleporter.enabled instead of subscribing to DoorOpenedEvent because
// that event routes through GameState.CurrentGame.OpenDoor(DoorID) — a multiplayer
// round-trip that NEVER FIRES in offline/testing sessions where
// GameState.HasCurrentGame is false. teleporter.enabled is set locally by
// SecretBehaviour when all triggers are lit, regardless of network state.
//
// One-shot: first time the door opens, sparks + light come on and stay on for the
// rest of the session. No off-transition (otherwise sparks die mid-teleport when
// OnTriggerEnter sets _timeOut=0).
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
        // SecretDoor may be m_IsActive=0 until late in scene init. Re-scan every
        // 0.5s until we find it, then stop.
        if (Time.time < _nextScanTime) return;
        _nextScanTime = Time.time + 0.5f;

        foreach (var t in Object.FindObjectsOfType<TempleTeleporter>(includeInactive: true))
        {
            if (t == null) continue;
            _teleporter = t;
            break;
        }
    }

    private void Update()
    {
        if (_ritualEverCompleted) return;

        ResolveTeleporter();
        if (_teleporter == null) return;

        if (_teleporter.enabled)
        {
            _ritualEverCompleted = true;
            if (_sparkPS != null)
            {
                var em = _sparkPS.emission; em.enabled = true;
            }
            if (_pointLight != null) _pointLight.enabled = true;
        }
    }
}
