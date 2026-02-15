using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restores Fort Winter's snow particle system at runtime.
///
/// The Unity 3.5.5 → 2022 migration stripped the legacy particle system ("Snow")
/// from the scene. This script recreates it using the modern ParticleSystem API.
///
/// Parameters sourced from UberStrike 4.3.8 original scene data (identical to 4.8).
///
/// NOTE: UberStrike 4.3.8 did NOT have pickup prefabs (HP/AP/Ammo) on Fort Winter.
/// Those were added in later versions (4.7/4.8). Only snow restoration is needed.
/// </summary>
public static class FortWinterRestorer
{
    const string SceneName = "LevelFortWinter";
    static GameObject snowInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneName)
        {
            SpawnSnow();
        }
        else
        {
            // Any other scene loaded — destroy snow if it exists
            ClearSnow();
        }
    }

    static void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == SceneName)
            ClearSnow();
    }

    public static void ClearSnow()
    {
        if (snowInstance != null)
        {
            Object.Destroy(snowInstance);
            snowInstance = null;
            Debug.Log("[FortWinterRestorer] Snow destroyed");
        }
    }

    static void SpawnSnow()
    {
        if (snowInstance != null) return;

        var snowGO = new GameObject("Snow_restored");
        snowInstance = snowGO;
        snowGO.transform.position = new Vector3(13.809407f, 37.749527f, -0.5827613f);
        snowGO.transform.rotation = Quaternion.identity;

        // Self-destruct: polls every second to check if Fort Winter is still loaded.
        // This catches the lobby transition where no sceneLoaded/sceneUnloaded fires.
        snowGO.AddComponent<FortWinterSnowGuard>();

        var ps = snowGO.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Main module — from EllipsoidParticleEmitter
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.duration = 15f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 15f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = Color.white;
        main.maxParticles = 15000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(600f, 1000f);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1f;
        shape.scale = new Vector3(50f, 3f, 50f);
        shape.radiusThickness = 1f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = 0f;
        vel.y = -5f;
        vel.z = 0f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(10f / 255f, 0f),
                new GradientAlphaKey(180f / 255f, 0.25f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(180f / 255f, 0.75f),
                new GradientAlphaKey(10f / 255f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = new ParticleSystem.MinMaxCurve(3f);
        noise.strengthY = new ParticleSystem.MinMaxCurve(0.5f);
        noise.strengthZ = new ParticleSystem.MinMaxCurve(3f);
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.5f;
        noise.damping = false;
        noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.High;

        var rot = ps.rotationOverLifetime;
        rot.enabled = false;
        var forceOverLifetime = ps.forceOverLifetime;
        forceOverLifetime.enabled = false;
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = false;
        var limitVelocity = ps.limitVelocityOverLifetime;
        limitVelocity.enabled = false;
        var inheritVelocity = ps.inheritVelocity;
        inheritVelocity.enabled = false;

        var renderer = snowGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.25f;
        renderer.minParticleSize = 0f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.material = CreateSnowMaterial();

        ps.Play();
        Debug.Log("[FortWinterRestorer] Snow created at " + snowGO.transform.position);
    }

    static Material CreateSnowMaterial()
    {
        var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Particles/Alpha Blended");
        if (shader == null)
        {
            Debug.LogWarning("[FortWinterRestorer] Alpha Blended shader not found");
            return null;
        }

        var mat = new Material(shader);
        mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.7f));
        mat.mainTexture = CreateSoftCircleTexture();
        return mat;
    }

    static Texture2D CreateSoftCircleTexture()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - d);
                alpha *= alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}

/// <summary>
/// Self-destruct guard: destroys the snow GameObject when player leaves Fort Winter.
/// Uses GameState.CurrentSpace instead of scene checks because maps are loaded additively
/// and never unloaded — SceneManager.GetSceneByName("LevelFortWinter").isLoaded always
/// returns true, and sceneLoaded/sceneUnloaded never fire for lobby transitions.
/// </summary>
public class FortWinterSnowGuard : MonoBehaviour
{
    float checkInterval = 1f;
    float timer;
    float gracePeriod = 3f;

    void Update()
    {
        // Grace period: allow GameState to initialize on first map load
        if (gracePeriod > 0f)
        {
            gracePeriod -= Time.deltaTime;
            return;
        }

        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        // GameState tracks which map is active via SetCurrentSpace().
        // When player leaves Fort Winter (to lobby or another map), CurrentSpace changes.
        if (!GameState.Exists) return;
        if (!GameState.HasCurrentSpace) return;

        var space = GameState.CurrentSpace;
        if (space == null || space.gameObject == null) return;

        // Check by MapId (5 = Fort Winter) or GameObject name.
        // Cannot use space.gameObject.scene.name because MapConfiguration objects
        // live in the persistent "Latest" scene, not in their Level scenes.
        if (space.MapId != 5)
        {
            Debug.Log("[FortWinterRestorer] Guard: Left Fort Winter (now on " +
                      space.name + " MapId=" + space.MapId + "), destroying snow");
            FortWinterRestorer.ClearSnow();
            if (gameObject != null)
                Destroy(gameObject);
        }
    }
}
