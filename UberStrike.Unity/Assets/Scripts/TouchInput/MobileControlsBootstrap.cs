using UnityEngine;

/// <summary>
/// Creates the on-screen touch control system entirely from code.
///
/// On the mobile branch <see cref="TouchInput"/> and <see cref="MobileIcons"/> were
/// Inspector-wired scene objects, but <see cref="MonoSingleton{T}"/> does NOT auto-create, and
/// current <c>main</c> has no scene wiring for them. This bootstrap spawns one persistent host
/// GameObject and attaches the singletons, so the feature drops onto main with zero scene edits.
///
/// Activation is gated on <see cref="ApplicationDataManager.IsMobile"/> (the codebase's canonical
/// platform gate, which also covers WebGL). In the Editor it can be forced on for previewing
/// (<see cref="ForcePreviewInEditor"/>):
///  - The host + <see cref="MobileIcons"/> + the layout editor are created immediately, so the
///    drag/scale layout editor is usable in plain Editor Play mode (Game view) with the mouse,
///    with NO server and NO match required (standalone preview).
///  - The full <see cref="TouchInput"/> driver is added once an actual match is running and the HUD
///    is up (it needs HudAssets), upgrading the same host in place.
/// </summary>
public class MobileControlsBootstrap : MonoBehaviour
{
    private static MobileControlsBootstrap _instance;

    private GameObject _host;
    private bool _touchInputCreated;

#if UNITY_EDITOR
    // Editor-only: flip on (Tools ▸ Mobile ▸ Toggle Touch Controls Preview) to render + arrange the
    // controls in the Editor Game view without a mobile channel / server / match. Never built.
    // Backed by EditorPrefs so it SURVIVES the domain reload that entering Play mode triggers
    // (a plain static would reset to false before the [RuntimeInitializeOnLoadMethod] runs).
    public const string PreviewPrefKey = "MobileControls.ForcePreviewInEditor";
    public static bool ForcePreviewInEditor
    {
        get { return UnityEditor.EditorPrefs.GetBool(PreviewPrefKey, false); }
        set { UnityEditor.EditorPrefs.SetBool(PreviewPrefKey, value); }
    }
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

    private void Update()
    {
        if (!ShouldActivate())
        {
            MobileControlLayout.PreviewStandalone = false;
            return;
        }

        EnsureHost();

        // Upgrade to the full input driver once a match + HUD are up (TouchInput.Start needs HudAssets).
        if (!_touchInputCreated && !TouchInput.Exists && GameState.HasCurrentGame && HudAssets.Exists)
        {
            _host.AddComponent<TouchInput>();
            _touchInputCreated = true;
            Debug.Log("[MobileControlsBootstrap] TouchInput input driver created (match started).");
        }

        // Standalone (no-match) layout preview is only offered in the Editor force-preview case, so
        // the on-device "Edit Controls" button still appears only during a match.
        bool standalone = !TouchInput.Exists && !GameState.HasCurrentGame;
#if UNITY_EDITOR
        MobileControlLayout.PreviewStandalone = ForcePreviewInEditor && standalone;
#else
        MobileControlLayout.PreviewStandalone = false;
#endif
    }

    private void EnsureHost()
    {
        if (_host != null) return;

        _host = new GameObject("MobileTouchControls");
        DontDestroyOnLoad(_host);

        // MobileIcons.Awake loads the icon textures from Resources (needed by both the live driver
        // and the standalone layout preview). The editor draws + persists the customizable layout.
        _host.AddComponent<MobileIcons>();
        _host.AddComponent<MobileControlLayoutEditor>();

        Debug.Log("[MobileControlsBootstrap] On-screen controls host created.");
    }
}
