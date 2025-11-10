using UnityEngine;
using System.Collections;

public class CameraCollisionDetector
{
    private bool _lHitResult;
    private bool _rHitResult;

    private RaycastHit _lRaycastInfo;
    private RaycastHit _rRaycastInfo;

    private float _collidedDistance;

    public float Distance
    {
        get { return _collidedDistance; }
    }

    public float Offset;

    // set the layer mask to ignore specific collision detections
    public int LayerMask;

    /// <summary>
    /// Detect collisions at left and right side of the given point
    /// </summary>
    /// <param name="from">The look at point</param>
    /// <param name="to">The point to detect collisions</param>
    /// <param name="right">The right direction of the given point</param>
    /// <returns></returns>
    public bool Detect(Vector3 from, Vector3 to, Vector3 right)
    {
        _collidedDistance = Vector3.Distance(from, to);

        // do ray cast to left and right every other frame
        if ((Time.frameCount & 0x1) == 0)
        {
            to -= right * Offset;
            _lHitResult = Physics.Linecast(from, to, out _lRaycastInfo, LayerMask);
        }
        else
        {
            to += right * Offset;
            _rHitResult = Physics.Linecast(from, to, out _rRaycastInfo, LayerMask);
        }

        if (_lHitResult)
        {
            float dis = Vector3.Distance(_lRaycastInfo.point, from);
            if (dis < _collidedDistance) _collidedDistance = dis;
        }

        if (_rHitResult)
        {
            float dis = Vector3.Distance(_rRaycastInfo.point, from);
            if (dis < _collidedDistance) _collidedDistance = dis;
        }

        return _lHitResult || _rHitResult;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        if (_lHitResult) Gizmos.DrawWireSphere(_lRaycastInfo.point, 0.1f);
        if (_rHitResult) Gizmos.DrawWireSphere(_rRaycastInfo.point, 0.1f);
    }

    public void OnGUI()
    {
        GUI.Label(new Rect(200, 200, 200, 20), "Left HitResult: " + _lHitResult);
        if (_lHitResult)
        {
            GUI.Label(new Rect(200, 220, 200, 20), "Hit point: " + _lRaycastInfo.point);
        }

        GUI.Label(new Rect(400, 200, 200, 20), "Right HitResult: " + _rHitResult);
        if (_rHitResult)
        {
            GUI.Label(new Rect(400, 220, 200, 20), "Hit point: " + _rRaycastInfo.point);
        }
    }
}
