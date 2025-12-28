

using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Player")
        {
            _lastPosition = transform.position;

            GameState.LocalPlayer.MoveController.Platform = this;
        }
    }

    void OnTriggerExit(Collider c)
    {
        if (c.tag == "Player")
        {
            GameState.LocalPlayer.MoveController.Platform = null;
        }
    }

    public Vector3 LastMovement
    {
        get { return _lastMovement; }
    }

    public Vector3 GetMovementDelta()
    {
        _lastMovement = transform.position - _lastPosition;

        _lastPosition = transform.position;

        return _lastMovement;
    }

    private Vector3 _lastPosition;
    private Vector3 _lastMovement;
}
