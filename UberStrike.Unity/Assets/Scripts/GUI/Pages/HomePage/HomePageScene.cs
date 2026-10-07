using UnityEngine;

public class HomePageScene : PageScene
{
    private bool _lastIsPlayerInClan;
    private string _lastClanTag;

    public override PageType PageType
    {
        get { return PageType.Home; }
    }

    protected override void OnLoad()
    {
        Vector3 position;
        Quaternion rotation;
        if (MenuConfiguration.Instance.GetPageAnchorPoint(PageType, out position, out rotation))
        {
            GameState.LocalDecorator.SetPosition(position, rotation);
        }

        if (GameState.LocalDecorator != null)
            GameState.LocalDecorator.HideWeapons();

        AvatarAnimationManager.Instance.ResetAnimationState(PageType);

        EventPopupManager.Instance.ShowNextPopup(1);

        // Classic ring lobby camera: (1) SNAP it to the tuned framing immediately, and (2) keep it locked
        // at RENDER time via ClassicCameraLock.OnPreCull (which runs after all Update/LateUpdate/coroutines)
        // so nothing — ShipBob, the page-transition lerp, idle movers — can drift it. The snap matters for
        // the avatar name tag: it projects through Camera.main, so coming from a page with a different
        // camera it would briefly project through the OLD camera and appear to fly in from behind the
        // avatar. Snapping here puts Camera.main at the correct framing on the first frame on Home. The
        // lock self-gates to classic-Home, so it no-ops on the column lobby and other pages; add it once.
        if (ApplicationDataManager.ApplicationOptions.UseClassicLobby
            && GameState.CurrentSpace != null && GameState.CurrentSpace.Camera != null)
        {
            Camera cam = GameState.CurrentSpace.Camera;
            // Add the lock FIRST so its Awake captures the camera's DEFAULT FOV before we snap the classic
            // (wide) FOV below — the lock restores that default when off-Home / classic-off.
            if (cam.GetComponent<ClassicCameraLock>() == null)
                cam.gameObject.AddComponent<ClassicCameraLock>();

            Vector3 vp;
            Quaternion vr;
            if (MenuConfiguration.Instance.GetPageViewPoint(PageType, out vp, out vr))
            {
                cam.transform.position = vp;
                cam.transform.rotation = vr;
                cam.fieldOfView = MenuConfiguration.Instance.GetPageFov(PageType);
            }
        }
    }

    //TODO: needs to be fixed
    private void Update()
    {
        if (_lastIsPlayerInClan != PlayerDataManager.IsPlayerInClan || _lastClanTag != PlayerDataManager.ClanTag)
        {
            GameState.LocalDecorator.HudInformation.SetAvatarLabel(PlayerDataManager.IsPlayerInClan ?
                string.Format("[{0}] {1}", PlayerDataManager.ClanTag, PlayerDataManager.Name) : PlayerDataManager.Name);
            _lastIsPlayerInClan = PlayerDataManager.IsPlayerInClan;
            _lastClanTag = PlayerDataManager.ClanTag;
        }
    }
}