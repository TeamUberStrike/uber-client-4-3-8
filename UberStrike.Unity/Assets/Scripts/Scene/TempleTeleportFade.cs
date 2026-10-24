using UnityEngine;

// Disabled per user 2026-04-21: the blue-diffusion fade that originally covered
// the teleport transition ("2s blue fade") was deemed visually bad and removed.
// Fade() is now a no-op; TempleTeleporter still calls it (unchanged), but
// nothing is drawn. Kept as a shell so TempleTeleporter.cs continues to compile
// against the existing API surface.
public class TempleTeleportFade : MonoBehaviour
{
    public const float DefaultDuration = 2f;

    private static TempleTeleportFade _instance;
    public static TempleTeleportFade Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("TempleTeleportFade");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<TempleTeleportFade>();
            }
            return _instance;
        }
    }

    public void Fade(float duration = DefaultDuration) { /* no-op */ }
}
