using UnityEngine;
using System.Collections;

public class ShipBob : MonoBehaviour
{
    [SerializeField]
    private float rotateAmount = 1.0f;
    [SerializeField]
    private float moveAmount = 0.005f;

    private Transform _transform;
    private Vector3 shipRotation;
    private Vector3 _basePosition;

    void Awake()
    {
        _transform = this.transform;
        shipRotation = _transform.localRotation.eulerAngles;
        // Capture the rest position ONCE. The original code did position.y += sin(...) every frame, reading
        // its own already-moved position — a slow accumulating drift. Bobbing around a fixed base instead
        // keeps the ship (and the camera/avatar that ride it) stable over time.
        _basePosition = _transform.position;
    }

    void Update()
    {
        // Classic ring lobby: hold the ship at its rest position/rotation so the locked retail camera shows
        // a static shot (no bobbing/drifting avatar or name). Classic Home only — the column lobby bobs.
        if (ApplicationDataManager.ApplicationOptions.UseClassicLobby && MenuPageManager.IsCurrentPage(PageType.Home))
        {
            _transform.position = _basePosition;
            _transform.localRotation = Quaternion.Euler(shipRotation);
            return;
        }

        _transform.position = new Vector3(_basePosition.x, _basePosition.y + (Mathf.Sin(Time.time) * moveAmount), _basePosition.z);
        float bobMotion = Mathf.Sin(Time.time) * rotateAmount;
        _transform.localRotation = Quaternion.Euler(shipRotation + new Vector3(bobMotion, bobMotion, bobMotion));
    }
}
