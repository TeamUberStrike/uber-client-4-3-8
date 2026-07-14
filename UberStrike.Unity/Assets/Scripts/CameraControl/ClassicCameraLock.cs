using UnityEngine;

// Owns the lobby camera's framing for the classic ring lobby. In OnPreCull (which runs right before the
// camera culls/renders — AFTER every Update, LateUpdate, coroutine and WaitForEndOfFrame) it:
//   • on the classic Home page: forces the tuned retail position/rotation + the wide classic FOV, so
//     nothing (ShipBob, the page-transition lerp, idle movers) can drift the framing;
//   • elsewhere in the menu: restores the camera's ORIGINAL FOV (captured once in Awake) so leaving Home
//     or toggling classic off never leaves the wide FOV applied — and it does NOT touch position/rotation
//     there, leaving the normal menu camera system in charge.
// It self-gates, so the column lobby's framing/FOV are unchanged. HomePageScene adds it once to the camera.
[RequireComponent(typeof(Camera))]
public class ClassicCameraLock : MonoBehaviour
{
    private Camera _camera;
    private float _defaultFov;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        // Capture the camera's default FOV before any classic modification (HomePageScene adds this
        // component BEFORE it snaps the classic FOV, so this is the genuine default).
        if (_camera != null)
            _defaultFov = _camera.fieldOfView;
    }

    private void OnPreCull()
    {
        if (_camera == null)
            return;

        if (ApplicationDataManager.ApplicationOptions.UseClassicLobby && MenuPageManager.IsCurrentPage(PageType.Home))
        {
            Vector3 position;
            Quaternion rotation;
            if (MenuConfiguration.Instance.GetPageViewPoint(PageType.Home, out position, out rotation))
            {
                transform.position = position;
                transform.rotation = rotation;
                _camera.fieldOfView = MenuConfiguration.ClassicHomeFov;
            }
        }
        else if (!GameState.HasCurrentGame)
        {
            // In the menu but not classic-Home: make sure the FOV is back to the captured default (don't
            // touch position/rotation — the normal menu camera system owns those).
            if (_camera.fieldOfView != _defaultFov)
                _camera.fieldOfView = _defaultFov;
        }
    }
}
