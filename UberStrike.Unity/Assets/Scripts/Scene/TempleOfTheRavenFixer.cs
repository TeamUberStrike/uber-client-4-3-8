using UnityEngine;
using UnityEngine.SceneManagement;

// Runtime fix-ups that only apply when LevelTempleOfTheRaven is loaded:
//
// 1. Skybox: the ported scene's m_SkyboxMaterial reference is stale in the
//    AssetBundle-loaded path, so the Temple sky doesn't actually appear at
//    runtime even though the scene file references the right material. We
//    reassign RenderSettings.skybox from Resources so the bundle-build state
//    doesn't matter.
//
// 2. MeshCollider trigger warning: `StaticContent/WaterEnvironment/Tile` has a
//    non-convex MeshCollider with isTrigger=true, which Unity 5+ rejects and
//    spams a warning about. Flipping the collider to convex would distort the
//    water surface collision; instead, clear isTrigger — the WaterGate
//    (BoxCollider) script is the actual trigger for water traversal, the
//    WaterPlane itself just needs blocking/none collision.
//
// Kept in one script so future Temple-only runtime tweaks have an obvious home.
public class TempleOfTheRavenFixer : MonoBehaviour
{
    private const string SceneName = "LevelTempleOfTheRaven";
    private const string SkyboxResourcePath = "TempleOfTheRaven/SkyboxV2";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryApplyIn(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryApplyIn(scene);
    }

    private static void TryApplyIn(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName) return;

        ApplySkybox();
        FixNonConvexTriggerMeshColliders(scene);
    }

    private static void ApplySkybox()
    {
        // Multiple discovery paths — Unity sometimes fails to surface a just-
        // added Resources asset without a manual reimport, so we try a few ways
        // before giving up. In order of preference:
        //   1. Resources.Load — works once Unity has indexed the folder.
        //   2. FindObjectsOfTypeAll — finds the material by name anywhere in
        //      the project if it's been loaded into memory at all.
        //   3. Reconstruct from Shader.Find + a cubemap in Resources/Scene.
        Material mat = Resources.Load<Material>(SkyboxResourcePath);

        if (mat == null)
        {
            var all = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == "SkyboxV2") { mat = all[i]; break; }
            }
        }

        if (mat == null)
        {
            var shader = Shader.Find("RenderFX/Skybox Cubed");
            if (shader != null)
            {
                // Find the TOR-Skybox cubemap by name among loaded Cubemaps.
                Cubemap cube = null;
                var cubes = Resources.FindObjectsOfTypeAll<Cubemap>();
                for (int i = 0; i < cubes.Length; i++)
                {
                    if (cubes[i] != null && cubes[i].name.IndexOf("TOR-Skybox", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    { cube = cubes[i]; break; }
                }
                if (cube != null)
                {
                    mat = new Material(shader);
                    mat.hideFlags = HideFlags.HideAndDontSave;
                    mat.name = "SkyboxV2_Runtime";
                    if (mat.HasProperty("_Tex")) mat.SetTexture("_Tex", cube);
                    if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", new Color(0.784f, 0.784f, 0.784f, 1f));
                }
            }
        }

        if (mat == null)
        {
            Debug.LogWarning("[TempleFixer] Could not locate SkyboxV2 material — sky will use previous/default.");
            return;
        }

        // Session-wide idempotency. The previous version used
        // `RenderSettings.skybox == mat` which was expected to return true on
        // re-entry (Resources.Load caches the asset, so the reference should
        // be stable). In practice that check was still failing on re-entry
        // (log: `[TempleFixer] Skybox applied: SkyboxV2` on EVERY Temple
        // entry, including repeated ones), which means something is mutating
        // RenderSettings.skybox between visits — likely Unity restoring the
        // scene's baked RenderSettings during additive load.
        //
        // Screenshot diff (2026-04-21) showed Temple rendering correctly on
        // first entry (deep lightmapped shadows, moss hidden in gloom) and
        // COMPLETELY FLAT-LIT on re-entry (no lightmap darkening, moss fully
        // visible). That's a classic signature of DynamicGI.UpdateEnvironment
        // being triggered by the skybox reassignment and invalidating the
        // baked-lightmap contribution path even with ambientMode=Flat.
        //
        // Fix: apply the skybox AT MOST ONCE per session. If the skybox gets
        // mutated to something else between visits, we accept that — the
        // trade-off is: better to have the correct baked lighting (the
        // user-facing big issue) than a guaranteed-correct sky texture
        // (cosmetic, and only briefly wrong during the short transition).
        if (_skyboxAppliedOnce)
        {
            Debug.Log("[TempleFixer] Skybox already applied in this session — skipped (anti-GI-reset guard).");
            return;
        }

        RenderSettings.skybox = mat;
        _skyboxAppliedOnce = true;
        Debug.Log("[TempleFixer] Skybox applied: " + mat.name);
    }

    private static bool _skyboxAppliedOnce;

    private static void FixNonConvexTriggerMeshColliders(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var colliders = roots[i].GetComponentsInChildren<MeshCollider>(true);
            for (int j = 0; j < colliders.Length; j++)
            {
                var mc = colliders[j];
                if (mc.isTrigger && !mc.convex)
                {
                    mc.isTrigger = false;
                }
            }
        }
    }
}
