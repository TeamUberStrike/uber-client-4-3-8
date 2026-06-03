using Cmune.Util;
using UnityEngine;

/// <summary>
/// Creates the on-screen touch control system entirely from code.
///
/// On the mobile branch <see cref="TouchInput"/> and <see cref="MobileIcons"/> were
/// Inspector-wired scene objects, but <see cref="MonoSingleton{T}"/> does NOT auto-create, and
/// current <c>main</c> has no scene wiring for them. This bootstrap spawns a persistent host
/// GameObject and attaches the singletons once a game/HUD is up, so the feature drops onto main
/// with zero scene edits.
///
/// Activation is gated on <see cref="ApplicationDataManager.IsMobile"/> (the codebase's canonical
/// platform gate, which also covers WebGL). In the Editor it can be forced on for previewing in
/// the Unity Device Simulator without a mobile channel — see <see cref="ForcePreviewInEditor"/>.
/// </summary>
public class MobileControlsBootstrap : MonoBehaviour
{
    private static MobileControlsBootstrap _instance;
    private bool _created;

#if UNITY_EDITOR
    // Editor-only: flip on (Tools ▸ Mobile ▸ Toggle Touch Controls Preview) to drive the on-screen
    // controls in the Game view / Device Simulator without switching the channel. Never built.
    public static bool ForcePreviewInEditor = false;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("MobileControlsBootstrap");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<MobileControlsBootstrap>();
    }

    private static bool ShouldActivate()
    {
        if (ApplicationDataManager.IsMobile) return true;
#if UNITY_EDITOR
        if (ForcePreviewInEditor) return true;
#endif
        return false;
    }

    private void OnEnable()
    {
        CmuneEventHandler.AddListener<OnModeInitializedEvent>(OnModeInitialized);
    }

    private void OnDisable()
    {
        CmuneEventHandler.RemoveListener<OnModeInitializedEvent>(OnModeInitialized);
    }

    private void OnModeInitialized(OnModeInitializedEvent ev)
    {
        EnsureControls();
    }

    private void Update()
    {
        // Fallback creation path in case the mode-init event fired before this listener was wired
        // (or the HUD singletons weren't ready yet on that event).
        if (!_created && ShouldActivate() && GameState.HasCurrentGame)
            EnsureControls();
    }

    private void EnsureControls()
    {
        if (_created) return;
        if (!ShouldActivate()) return;

        // Already present (e.g. created by a previous match) — nothing to do.
        if (TouchInput.Exists)
        {
            _created = true;
            return;
        }

        // TouchInput.Start() reads HudAssets.InterparkBitmapFont (a MonoSingleton placed by the HUD
        // scene), so wait until the HUD is up. HudStyleUtility is a plain auto-creating Singleton.
        if (!HudAssets.Exists)
            return;

        GameObject host = new GameObject("MobileTouchControls");
        DontDestroyOnLoad(host);

        // Order matters: MobileIcons.Awake loads the icon textures before TouchInput.Start reads them.
        host.AddComponent<MobileIcons>();
        host.AddComponent<TouchInput>();
        host.AddComponent<MobileControlLayoutEditor>();

        _created = true;
        Debug.Log("[MobileControlsBootstrap] On-screen touch controls created.");
    }
}
