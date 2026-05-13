using UnityEngine;

/// <summary>
/// Per-scene setup for Fort Winter's snow particle system.
///
/// The Unity 3.5.5 → 2022 migration stripped the legacy "Snow" particle system
/// from the scene. This script recreates it using the modern ParticleSystem API
/// each time LevelFortWinter is the active map, with parameters sourced from the
/// UberStrike 4.3.8 original scene data (identical to 4.8).
///
/// Drives the snow lifecycle from GameState.CurrentSpace.MapId rather than from
/// SceneManager.sceneLoaded — Unity 2022 maps are loaded additively and never
/// unloaded between visits, so sceneLoaded only fires on the first visit per
/// scene container. Polling GameState matches how BeastLightmapLoader detects
/// "the player is currently playing this map" and works correctly across
/// lobby → map → lobby → same-map cycles.
///
/// NOTE: UberStrike 4.3.8 did NOT have pickup prefabs (HP/AP/Ammo) on Fort Winter
/// — those were added in later versions (4.7/4.8). Only the snow setup is needed.
/// </summary>
public static class FortWinterSnowSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var host = new GameObject("FortWinterSnowController");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<FortWinterSnowController>();
    }
}

/// <summary>
/// Persistent controller MB that owns the snow lifecycle for Fort Winter.
/// Polls GameState.CurrentSpace.MapId at 10 Hz:
///   MapId == 5 (Fort Winter) and snow missing → spawn
///   MapId != 5 and snow present              → destroy immediately
/// Idempotent across all entry/exit transitions; no SceneManager.sceneLoaded
/// dependency.
///
/// 10 Hz keeps the perceived spawn/destroy latency under ~0.1s without
/// burdening the frame budget — the per-tick work is a few field accesses
/// and an int compare, much cheaper than the snow particle simulation
/// already running each frame.
/// </summary>
public class FortWinterSnowController : MonoBehaviour
{
    private const int FortWinterMapId = 5;
    private const float CheckInterval = 0.1f;

    private GameObject _snow;
    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < CheckInterval) return;
        _timer = 0f;

        if (!GameState.Exists) return;

        bool onFortWinter = GameState.HasCurrentSpace
                         && GameState.CurrentSpace != null
                         && GameState.CurrentSpace.MapId == FortWinterMapId;

        if (onFortWinter && _snow == null)
        {
            _snow = SpawnSnow();
            Debug.Log("[FortWinterSetup] Snow created at " + _snow.transform.position);
        }
        else if (!onFortWinter && _snow != null)
        {
            // No grace period — destroy immediately. The previous 3-second
            // grace caused a brief snow flash on the lobby HUD when leaving FW.
            Object.Destroy(_snow);
            _snow = null;
            Debug.Log("[FortWinterSetup] Snow destroyed (left Fort Winter)");
        }
    }

    private static GameObject SpawnSnow()
    {
        var snowGO = new GameObject("Snow_restored");
        snowGO.transform.position = new Vector3(13.809407f, 37.749527f, -0.5827613f);
        snowGO.transform.rotation = Quaternion.identity;

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
        return snowGO;
    }

    private static Material CreateSnowMaterial()
    {
        var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Particles/Alpha Blended");
        if (shader == null)
        {
            Debug.LogWarning("[FortWinterSetup] Alpha Blended shader not found");
            return null;
        }

        var mat = new Material(shader);
        mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.7f));
        mat.mainTexture = CreateSoftCircleTexture();
        return mat;
    }

    private static Texture2D CreateSoftCircleTexture()
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
