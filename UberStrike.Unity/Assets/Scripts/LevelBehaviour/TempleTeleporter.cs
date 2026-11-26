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

        // The portal's old LightningParticles child stays inactive: the veil's own
        // swirl replaces it (the particles looked like smoke from the side).
        EnsurePortalVeil();

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

    // The temple wall surface is at door-local z 0.379 (the box face at 0.5 stuck out
    // ~7 cm). The veil sits ~1 cm in front of the stones so the arch reads as an
    // opening in the wall, not a plate on it.
    private const float VeilPlaneZ = 0.4f;

    // Door width/height in metres (the SecretDoor is a unit cube scaled to the opening).
    private Vector4 DoorSize
    {
        get { return new Vector4(transform.lossyScale.x, transform.lossyScale.y, 0f, 0f); }
    }

    // Compiles the portal shader on map load instead of the frame the portal opens.
    private static void WarmUp(Shader shader)
    {
        try
        {
            var svc = new ShaderVariantCollection();
            svc.Add(new ShaderVariantCollection.ShaderVariant(shader, UnityEngine.Rendering.PassType.Normal));
            svc.WarmUp();
        }
        catch (System.ArgumentException) { } // variant lookup failed: first use compiles it instead
    }

    // Replaces the plain black box with an arched, swirling veil (pointed arch with
    // a glowing edge). The veil takes the box's place in _visuals, so it appears and
    // vanishes instantly with the portal. The BoxCollider trigger is unchanged.
    private void EnsurePortalVeil()
    {
        var shader = Resources.Load<Shader>("Shaders/TemplePortalVeil");
        if (shader == null || !shader.isSupported) return; // keep the original box look
        WarmUp(shader);

        var go = new GameObject("PortalVeil");
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false); // unit quads, scaled to the opening by the door

        // One quad flush on the temple wall. There is no wall (and no player) behind
        // the door, so no back quad.
        const float face = VeilPlaneZ;
        var mesh = new Mesh();
        mesh.name = "PortalVeilQuad";
        mesh.vertices = new[] {
            new Vector3(-0.5f, -0.5f, face), new Vector3(0.5f, -0.5f, face),
            new Vector3(-0.5f,  0.5f, face), new Vector3(0.5f,  0.5f, face) };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        var renderer = go.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        var mat = new Material(shader);
        mat.SetVector("_DoorSize", DoorSize);
        renderer.sharedMaterial = mat;

        // The old box look is replaced by the veil.
        if (_visuals != null)
        {
            foreach (Renderer r in _visuals)
                if (r != null) r.enabled = false;
        }
        _visuals = new Renderer[] { renderer };
    }

    // Builds a Unity-2022 ParticleSystem matching the legacy 3.5 lightning emitter
    // parameters of the standalone TeleporterParticles inside the Secret Temple
    // (100/s, ellipsoid 5×20×20, world space, size 4-5.5).
    // Material lookup is the caller's job — passed in as `mat` (BluePlasmaLightning).
    private static ParticleSystem BuildLightningPS(GameObject host, Material mat)
    {
        var ps = host.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.prewarm = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startSpeed = 0f;
        // 4-5.5 from the 3.5 TeleporterParticles emitter.
        main.startSize = new ParticleSystem.MinMaxCurve(4f, 5.5f);
        main.startColor = Color.white; // material _TintColor bakes the blue
        main.maxParticles = 150;
        // 3.5 "Simulate in Worldspace" = true for TeleporterParticles.
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Particle size in absolute world units (predictable across SecretDoor
        // transforms 2.96×5.86×0.61 etc.). Hierarchy mode multiplies through the
        // whole parent chain and produced wildly inconsistent sizes.
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = false; // the caller turns emission on
        // 3.5 EllipsoidParticleEmitter min=max emission: 100/s.
        emission.rateOverTime = 100f;

        // Box shape as stand-in for the legacy ellipsoid volume. Dimensions match
        // the 3.5 m_Ellipsoid field; Unity 2022 dropped the ellipsoid primitive.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5f, 20f, 20f);
        shape.randomDirectionAmount = 0f;

        // rndVelocity (7,7,7) in 3.5 means each particle gets random [-7,7] per
        // axis in world space.
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-7f, 7f);
        velocity.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
        velocity.z = new ParticleSystem.MinMaxCurve(-7f, 7f);

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
                    var ps = BuildLightningPS(lightning.gameObject, bluePlasma);
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
