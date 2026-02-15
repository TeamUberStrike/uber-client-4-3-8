using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class ForceField : MonoBehaviour
{
    #region Fields

    [SerializeField]
    private Vector3 _direction;

    [SerializeField]
    private int _force = 1000;

    private float gizmofactor = 0.0055f;

    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        DecorateOrphanedJumpPads();
        SceneManager.sceneLoaded += OnSceneLoadedForParticles;
    }

    private static void OnSceneLoadedForParticles(Scene scene, LoadSceneMode mode)
    {
        // Only decorate JumpPads when a Level map scene loads (not lobby, not UI)
        if (!scene.name.StartsWith("Level") || scene.name == "LevelSpaceship")
            return;
        DecorateOrphanedJumpPads();
    }

    private static void DecorateOrphanedJumpPads()
    {
        // Find disabled ForceField instances and spawn particles at their positions.
        // On TempleOfTheRaven, the ForceField prefab instances start disabled but get
        // enabled by MapConfiguration.SetEnabled(). We parent the particles to the
        // ForceField so Awake() detects them via GetComponentInChildren<ParticleSystem>()
        // and skips creating duplicates.
        foreach (var ff in Resources.FindObjectsOfTypeAll<ForceField>())
        {
            if (ff == null) continue;
            if (ff.gameObject == null) continue;
            if (ff.gameObject.activeSelf)
                continue;
            if (!ff.gameObject.scene.IsValid())
                continue;
            if (ff.gameObject.name.IndexOf("accel", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            // Skip if already decorated
            if (ff.GetComponentInChildren<ParticleSystem>() != null)
                continue;

            var anchor = new GameObject("JumpPadParticles");
            anchor.transform.SetParent(ff.transform, false);
            anchor.transform.position = ff.transform.position;
            SpawnJumpPadParticlesOn(anchor.transform);
        }
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        gameObject.layer = (int)UberstrikeLayer.IgnoreRaycast;

        // Only spawn on JumpPads, not Accelerator pads (accel, AcceleratorPad, etc.)
        if (gameObject.name.IndexOf("accel", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return;

        // Skip if this object already has working particle systems
        if (GetComponentInChildren<ParticleSystem>() != null)
            return;

        // Original 3.5.5 hierarchy: jumpPad root -> JumpPad mesh child -> particle children
        Transform meshParent = transform;
        if (transform.childCount > 0 && transform.GetChild(0).gameObject.activeInHierarchy)
        {
            meshParent = transform.GetChild(0);
        }

        SpawnJumpPadParticlesOn(meshParent);
    }

    private static void SpawnJumpPadParticlesOn(Transform parent)
    {
        Material jumpPadMat = Resources.Load<Material>("JumpPadParticles");

        // ---- Particle System 1: Big glowing orbs (original "Particle System" child) ----
        var glowGO = new GameObject("Particle System");
        glowGO.transform.SetParent(parent, false);
        glowGO.transform.localPosition = new Vector3(0f, 0.121f, 0f);

        var glowPS = glowGO.AddComponent<ParticleSystem>();
        var glowMain = glowPS.main;
        glowMain.loop = true;
        glowMain.prewarm = true;
        glowMain.playOnAwake = true;
        glowMain.startLifetime = 1f;
        glowMain.startSpeed = 0f;
        glowMain.startSize = 7f;
        glowMain.startColor = Color.white;
        glowMain.maxParticles = 20;
        glowMain.simulationSpace = ParticleSystemSimulationSpace.World;
        glowMain.gravityModifier = 0f;

        var glowEmission = glowPS.emission;
        glowEmission.enabled = true;
        glowEmission.rateOverTime = 4f;

        var glowShape = glowPS.shape;
        glowShape.enabled = false;

        // Original 3.5.5 worldVelocity = (0, 2.5, 0) — always straight up in world space
        var glowVelocity = glowPS.velocityOverLifetime;
        glowVelocity.enabled = true;
        glowVelocity.space = ParticleSystemSimulationSpace.World;
        glowVelocity.x = 0f;
        glowVelocity.y = 2.5f;
        glowVelocity.z = 0f;

        // Color animation: alpha fades in then out
        var glowColor = glowPS.colorOverLifetime;
        glowColor.enabled = true;
        var glowGradient = new Gradient();
        glowGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.04f, 0f),
                new GradientAlphaKey(0.71f, 0.25f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0.71f, 0.75f),
                new GradientAlphaKey(0.04f, 1f)
            }
        );
        glowColor.color = glowGradient;

        // Renderer: Horizontal Billboard (original StretchParticles=4) — flat, facing ceiling
        var glowRenderer = glowGO.GetComponent<ParticleSystemRenderer>();
        glowRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        glowRenderer.maxParticleSize = 1f;
        if (jumpPadMat != null)
            glowRenderer.material = jumpPadMat;

        // ---- Particle System 2: Small sparkle dots (original "F_JP_particle" child) ----
        var sparkleGO = new GameObject("F_JP_particle");
        sparkleGO.transform.SetParent(parent, false);
        sparkleGO.transform.localPosition = Vector3.zero;

        var sparklePS = sparkleGO.AddComponent<ParticleSystem>();
        var sparkleMain = sparklePS.main;
        sparkleMain.loop = true;
        sparkleMain.prewarm = true;
        sparkleMain.playOnAwake = true;
        sparkleMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        sparkleMain.startSpeed = 0f;
        sparkleMain.startSize = 0.1f;
        sparkleMain.startColor = Color.white;
        sparkleMain.maxParticles = 50;
        sparkleMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sparkleMain.gravityModifier = 0f;

        var sparkleEmission = sparklePS.emission;
        sparkleEmission.enabled = true;
        sparkleEmission.rateOverTime = 20f;

        var sparkleShape = sparklePS.shape;
        sparkleShape.enabled = true;
        sparkleShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkleShape.radius = 1f;
        sparkleShape.randomDirectionAmount = 0f;

        // Original 3.5.5 worldVelocity = (0, 4, 0) — always straight up in world space
        var sparkleVelocity = sparklePS.velocityOverLifetime;
        sparkleVelocity.enabled = true;
        sparkleVelocity.space = ParticleSystemSimulationSpace.World;
        sparkleVelocity.x = 0f;
        sparkleVelocity.y = 4f;
        sparkleVelocity.z = 0f;

        var sparkleColor = sparklePS.colorOverLifetime;
        sparkleColor.enabled = true;
        sparkleColor.color = glowGradient;

        var sparkleRenderer = sparkleGO.GetComponent<ParticleSystemRenderer>();
        sparkleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        sparkleRenderer.maxParticleSize = 0.25f;
        if (jumpPadMat != null)
            sparkleRenderer.material = jumpPadMat;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player")
        {
            GameState.LocalPlayer.MoveController.ApplyForce(_direction.normalized * _force, CharacterMoveController.ForceType.Exclusive);

            SfxManager.Play2dAudioClip(SoundEffectType.PropsJumpPad2D);
        }
        else if (collider.gameObject.layer == (int)UberstrikeLayer.RemotePlayer)
        {
            SfxManager.Play3dAudioClip(SoundEffectType.PropsJumpPad, 1.0f, 0.1f, 10.0f, AudioRolloffMode.Linear, transform.position);
        }

        //else if (collider.tag == "Prop")
        //{
        //    BaseGameProp prop = collider.GetComponent<BaseGameProp>();
        //    if (prop != null)
        //    {
        //        prop.ApplyForce(Vector3.zero, _direction.normalized * _force * 2);
        //    }
        //}
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.localPosition, 0.2f);
        Vector3 v = _direction.normalized;
        v.y *= 0.6f;
        Gizmos.DrawLine(transform.localPosition, transform.localPosition + v * Mathf.Log(_force) * _force * gizmofactor);//);
    }
}
