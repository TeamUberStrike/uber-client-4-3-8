using UnityEngine;

// Fullscreen "energy wipe" transition played when the player switches between the 4.3.8 column lobby and
// the 4.3.10 classic ring lobby (via the Menu-UI dropdown in the top ribbon). Self-instantiating
// (AutoMonoBehaviour) and drawn frontmost (GUI.depth far below everything — lobby Page=11, ribbon=7,
// Sfx=-1) so the wipe covers the whole screen. The actual UseClassicLobby flip + a forced Home reload
// happen at the wipe's MIDPOINT — when the screen is fully covered — so the UI/camera swap is hidden
// behind the curtain and only the finished result is revealed.
public class LobbyUiSwitchFx : AutoMonoBehaviour<LobbyUiSwitchFx>
{
    private const float Duration = 1.0f;

    private bool _active;
    private float _t;          // 0..1 progress
    private bool _toClassic;   // target mode
    private bool _applied;     // has the midpoint swap run yet

    public bool IsPlaying { get { return _active; } }

    // Kick off the 1s switch to the given mode (true = 4.3.10 classic ring, false = 4.3.8 column).
    public void Begin(bool toClassic)
    {
        if (_active) return;   // ignore re-triggers mid-transition
        _toClassic = toClassic;
        _t = 0f;
        _applied = false;
        _active = true;
        SfxManager.Play2dAudioClip(SoundEffectType.UIRibbonClick);
    }

    private void Update()
    {
        if (!_active) return;

        _t += Time.deltaTime / Duration;

        if (!_applied && _t >= 0.5f)
        {
            _applied = true;
            ApplicationDataManager.ApplicationOptions.UseClassicLobby = _toClassic;
            ApplicationDataManager.ApplicationOptions.SaveApplicationOptions();
            // Force-reload Home so the camera framing + scene set-up re-applies for the new mode (the two
            // lobbies use different camera framing). Hidden under the fully-covered curtain.
            if (MenuPageManager.IsCurrentPage(PageType.Home))
                MenuPageManager.Instance.LoadPage(PageType.Home, true);
        }

        if (_t >= 1f)
        {
            _t = 1f;
            _active = false;
        }
    }

    private void OnGUI()
    {
        if (!_active) return;
        GUI.depth = -100;   // in front of everything

        float w = Screen.width, h = Screen.height;
        float t = Mathf.Clamp01(_t);
        Texture2D white = Texture2D.whiteTexture;
        Texture2D glow = GlowTex();
        Color prev = GUI.color;

        // Eased motion + an eased dark fade that PEAKS (fully covers) at the midpoint — no hard wipe edge,
        // so the whole thing reads smoothly. The cover plateaus at 1 for a short window around the swap.
        float ease = Mathf.SmoothStep(0f, 1f, t);
        float bell = Mathf.Sin(t * Mathf.PI);                       // 0 -> 1 -> 0
        float cover = Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, bell) * 1.15f);

        // Smooth full-screen darken (hides the swap at the midpoint).
        GUI.color = new Color(0.02f, 0.05f, 0.09f, cover);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), white);

        // Luminous cyan sweep that glides across once (eased), with a soft bloom from the bell-curve glow
        // texture — one stretched draw per layer = smooth, no banding. Brightest mid-sweep, fades at ends.
        float x = ease * w;
        float a = Mathf.Clamp01(bell * 1.4f);

        GUI.color = new Color(0.28f, 0.78f, 1f, a * 0.50f);         // wide outer bloom
        GUI.DrawTexture(new Rect(x - w * 0.30f, 0f, w * 0.60f, h), glow);

        GUI.color = new Color(0.55f, 0.90f, 1f, a * 0.65f);         // tighter inner bloom
        GUI.DrawTexture(new Rect(x - w * 0.07f, 0f, w * 0.14f, h), glow);

        float core = Mathf.Max(2f, w * 0.0035f);                   // bright core line
        GUI.color = new Color(0.92f, 0.98f, 1f, a * 0.95f);
        GUI.DrawTexture(new Rect(x - core * 0.5f, 0f, core, h), white);

        GUI.color = prev;
    }

    // Horizontal bell-curve (cos^2) glow texture: opaque at the centre, smoothly fading to transparent at
    // both edges. Stretched to a width and tinted at draw time it gives a soft, band-free glow. Cached.
    private Texture2D _glowTex;

    private Texture2D GlowTex()
    {
        if (_glowTex != null) return _glowTex;

        const int n = 128;
        _glowTex = new Texture2D(n, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "LobbySwitchGlow"
        };
        Color32[] px = new Color32[n];
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)(n - 1);                  // 0..1 across
            float d = Mathf.Abs(u - 0.5f) * 2f;            // 0 centre .. 1 edge
            float c = Mathf.Cos(d * Mathf.PI * 0.5f);      // 1 centre .. 0 edge
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(c * c) * 255f);
            px[i] = new Color32(255, 255, 255, alpha);
        }
        _glowTex.SetPixels32(px);
        _glowTex.Apply(false);
        return _glowTex;
    }
}
